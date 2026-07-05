namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

/// LR(0) item: A -> α·β
/// Dot position tracks how much of the RHS has been consumed.
type LR0Item<'t, 'nt> =
    { lhs: Nonterminal<'nt>
      rhs: Symbol<'t, 'nt> list
      dot: int }

/// LR(1) item: A -> α·β, l
/// Adds a lookahead symbol to each item for more precise reduce decisions.
type LR1Item<'t, 'nt> =
    { lhs: Nonterminal<'nt>
      rhs: Symbol<'t, 'nt> list
      dot: int
      lookahead: Symbol<'t, 'nt> }

/// Action in an LR parsing table.
type LRAction =
    | Shift of int
    | Reduce of int
    | Accept

/// Conflict detected during LR table construction.
type LRConflict<'t, 'nt> =
    | ShiftReduce of state: int * symbol: Symbol<'t, 'nt> * shiftTo: int * reduceRule: int
    | ReduceReduce of state: int * symbol: Symbol<'t, 'nt> * rule1: int * rule2: int

/// Built LR automaton returned from table construction for reuse in rendering.
type LRAutomaton<'t, 'nt when 't: comparison and 'nt: comparison> =
    | LR0 of DFA<Symbol<'t, 'nt>, Set<LR0Item<'t, 'nt>>>
    | LR1 of DFA<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>>

/// LR parsing table with action and goto maps, detected conflicts, and the automaton used to build it.
type LRTable<'t, 'nt when 't: comparison and 'nt: comparison> =
    { action: Map<int * Symbol<'t, 'nt>, LRAction>
      goto: Map<int * Nonterminal<'nt>, int>
      conflicts: LRConflict<'t, 'nt> list
      automaton: LRAutomaton<'t, 'nt> }

/// Construction of LR(0) and LR(1) automata as deterministic finite automata.
/// Functions take an already-augmented grammar (with fresh start nonterminal).
module LRAutomaton =

    /// Augment a grammar with a fresh start nonterminal.
    /// The augmented grammar has S' -> S as the first rule.
    let augmentGrammar (freshStart: Nonterminal<'nt>) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        { rules =
            { lhs = freshStart
              rhs = NonEmptyList.create (N g.start) [] |> Symbols }
            :: g.rules
          start = freshStart }

    let private closureLR0 (rules: Rule<'t, 'nt> list) (items: Set<LR0Item<'t, 'nt>>) : Set<LR0Item<'t, 'nt>> =
        let mutable closure = items
        let mutable changed = true

        while changed do
            changed <- false

            for item in closure |> Set.toSeq |> Seq.toList do
                if item.dot < item.rhs.Length then
                    match item.rhs.[item.dot] with
                    | N nt ->
                        let newItems =
                            rules
                            |> List.filter (fun r -> r.lhs = nt)
                            |> List.map (fun r ->
                                { lhs = r.lhs
                                  rhs = Rhs.toNonEpsilonList r.rhs
                                  dot = 0 })

                        for ni in newItems do
                            if not (Set.contains ni closure) then
                                closure <- Set.add ni closure
                                changed <- true
                    | _ -> ()

        closure

    let private gotoLR0
        (rules: Rule<'t, 'nt> list)
        (items: Set<LR0Item<'t, 'nt>>)
        (sym: Symbol<'t, 'nt>)
        : Set<LR0Item<'t, 'nt>> =
        items
        |> Set.filter (fun item -> item.dot < item.rhs.Length && item.rhs.[item.dot] = sym)
        |> Set.map (fun item -> { item with dot = item.dot + 1 })
        |> closureLR0 rules

    let private closureLR1
        (rules: Rule<'t, 'nt> list)
        (items: Set<LR1Item<'t, 'nt>>)
        (firstMap: Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>)
        : Set<LR1Item<'t, 'nt>> =
        let mutable closure = items
        let mutable changed = true

        while changed do
            changed <- false

            for item in closure |> Set.toSeq |> Seq.toList do
                if item.dot < item.rhs.Length then
                    match item.rhs.[item.dot] with
                    | N nt ->
                        let beta =
                            if item.dot + 1 < item.rhs.Length then
                                item.rhs |> List.skip (item.dot + 1)
                            else
                                []

                        let lookaheads =
                            if beta.IsEmpty then
                                set [ [ item.lookahead ] ]
                            else
                                FirstFollow.firstKOfString firstMap 1 beta
                                |> Set.filter (fun s -> s <> [ Epsilon ])

                        let newItems =
                            rules
                            |> List.filter (fun r -> r.lhs = nt)
                            |> List.collect (fun r ->
                                lookaheads
                                |> Set.toList
                                |> List.map (fun la ->
                                    { lhs = r.lhs
                                      rhs = Rhs.toNonEpsilonList r.rhs
                                      dot = 0
                                      lookahead = List.head la }))

                        for ni in newItems do
                            if not (Set.contains ni closure) then
                                closure <- Set.add ni closure
                                changed <- true
                    | _ -> ()

        closure

    let private gotoLR1
        (rules: Rule<'t, 'nt> list)
        (items: Set<LR1Item<'t, 'nt>>)
        (sym: Symbol<'t, 'nt>)
        (firstMap: Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>)
        : Set<LR1Item<'t, 'nt>> =
        items
        |> Set.filter (fun item -> item.dot < item.rhs.Length && item.rhs.[item.dot] = sym)
        |> Set.map (fun item -> { item with dot = item.dot + 1 })
        |> (fun filtered -> closureLR1 rules filtered firstMap)

    let private getSymbolsOf
        (dotOf: 'item -> int)
        (rhsOf: 'item -> Symbol<'t, 'nt> list)
        (state: Set<'item>)
        : Symbol<'t, 'nt> list =
        state
        |> Set.toSeq
        |> Seq.choose (fun item ->
            if dotOf item < (rhsOf item).Length then
                Some((rhsOf item).[dotOf item])
            else
                None)
        |> Seq.distinct
        |> Seq.toList

    let private buildLR
        (rules: Rule<'t, 'nt> list)
        (firstRule: Rule<'t, 'nt>)
        (mkStartItem: Rule<'t, 'nt> -> 'item)
        (mkCompleteItem: Rule<'t, 'nt> -> 'item)
        (dotOf: 'item -> int)
        (rhsOf: 'item -> Symbol<'t, 'nt> list)
        (closure: Set<'item> -> Set<'item>)
        (gotoFn: Set<'item> -> Symbol<'t, 'nt> -> Set<'item>)
        : DFA<Symbol<'t, 'nt>, Set<'item>> =
        let startItem = mkStartItem firstRule
        let acceptItem = mkCompleteItem firstRule
        let startItems = closure (set [ startItem ])
        let getSymbols = getSymbolsOf dotOf rhsOf
        let isAcceptState state = Set.contains acceptItem state
        Automaton.buildAutomaton startItems getSymbols gotoFn isAcceptState

    /// Build the LR(0) automaton for an augmented grammar.
    /// States are sets of LR(0) items. Transitions are labeled with grammar symbols.
    let buildLR0 (aug: Grammar<'t, 'nt>) : DFA<Symbol<'t, 'nt>, Set<LR0Item<'t, 'nt>>> =
        let augmentedRule = aug.rules.[0]

        buildLR
            aug.rules
            augmentedRule
            (fun r ->
                { lhs = r.lhs
                  rhs = Rhs.toNonEpsilonList r.rhs
                  dot = 0 })
            (fun r ->
                { lhs = r.lhs
                  rhs = Rhs.toNonEpsilonList r.rhs
                  dot = Rhs.toNonEpsilonList r.rhs |> List.length })
            (fun i -> i.dot)
            (fun i -> i.rhs)
            (closureLR0 aug.rules)
            (gotoLR0 aug.rules)

    /// Build the LR(1) automaton for an augmented grammar.
    /// States are sets of LR(1) items. Transitions are labeled with grammar symbols.
    let buildLR1 (aug: Grammar<'t, 'nt>) : DFA<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>> =
        let augmentedRule = aug.rules.[0]
        let firstMap = FirstFollow.firstK aug 1

        buildLR
            aug.rules
            augmentedRule
            (fun r ->
                { lhs = r.lhs
                  rhs = Rhs.toNonEpsilonList r.rhs
                  dot = 0
                  lookahead = Epsilon })
            (fun r ->
                { lhs = r.lhs
                  rhs = Rhs.toNonEpsilonList r.rhs
                  dot = Rhs.toNonEpsilonList r.rhs |> List.length
                  lookahead = Epsilon })
            (fun i -> i.dot)
            (fun i -> i.rhs)
            (fun items -> closureLR1 aug.rules items firstMap)
            (fun items sym -> gotoLR1 aug.rules items sym firstMap)

/// LR parsing table construction and parser.
/// All table builders and parse take an already-augmented grammar.
module LRParser =

    let private isCompletedLR0 (item: LR0Item<'t, 'nt>) : bool = item.dot = item.rhs.Length

    let private isCompletedLR1 (item: LR1Item<'t, 'nt>) : bool = item.dot = item.rhs.Length

    let private populateShiftGoto
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<Symbol<'t, 'nt>>>>>)
        (action: byref<Map<int * Symbol<'t, 'nt>, LRAction>>)
        (goto: byref<Map<int * Nonterminal<'nt>, int>>)
        (conflicts: byref<LRConflict<'t, 'nt> list>)
        : unit =
        for i in 0 .. transitions.rows - 1 do
            for j in 0 .. transitions.cols - 1 do
                match transitions.data.[i, j] with
                | Some symbols ->
                    for sym in NonEmptySet.toSeq symbols do
                        match sym with
                        | ATerm(T _ as tSym) ->
                            let key = (i, tSym)

                            match Map.tryFind key action with
                            | Some(Reduce r) -> conflicts <- ShiftReduce(i, tSym, j, r) :: conflicts
                            | Some(Shift _) -> ()
                            | Some Accept -> conflicts <- ShiftReduce(i, tSym, j, -1) :: conflicts
                            | None -> action <- Map.add key (Shift j) action
                        | ATerm(N nt) -> goto <- Map.add (i, nt) j goto
                        | _ -> ()
                | None -> ()

    /// Build the LR(0) parsing table from an augmented grammar.
    let buildLR0Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.rules.[0]
        let lr0 = LRAutomaton.buildLR0 aug
        let states = lr0.states

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        let allTerminals =
            [ for rule in aug.rules do
                  for sym in Rhs.toNonEpsilonList rule.rhs do
                      match sym with
                      | T _ as tSym -> yield tSym
                      | _ -> () ]
            |> List.distinct

        populateShiftGoto lr0.transitions &action &goto &conflicts

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompletedLR0 item then
                    if item.lhs = augmentedRule.lhs && item.dot = 1 then
                        let key = (stateIdx, Epsilon)

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, Epsilon, s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, Epsilon, r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules
                            |> List.findIndex (fun r -> r.lhs = item.lhs && Rhs.toNonEpsilonList r.rhs = item.rhs)

                        for t in Epsilon :: allTerminals do
                            let key = (stateIdx, t)

                            match Map.tryFind key action with
                            | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, t, s, ruleIdx) :: conflicts
                            | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, t, r, ruleIdx) :: conflicts
                            | Some Accept -> conflicts <- ShiftReduce(stateIdx, t, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (Reduce ruleIdx) action

        { action = action
          goto = goto
          conflicts = List.rev conflicts
          automaton = LR0 lr0 }

    /// Build the SLR(1) parsing table from an augmented grammar.
    let buildSLR1Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.rules.[0]
        let lr0 = LRAutomaton.buildLR0 aug
        let states = lr0.states
        let followMap = FirstFollow.followK aug 1

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        populateShiftGoto lr0.transitions &action &goto &conflicts

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompletedLR0 item then
                    if item.lhs = augmentedRule.lhs && item.dot = 1 then
                        let key = (stateIdx, Epsilon)

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, Epsilon, s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, Epsilon, r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules
                            |> List.findIndex (fun r -> r.lhs = item.lhs && Rhs.toNonEpsilonList r.rhs = item.rhs)

                        let followSet = followMap |> Map.find item.lhs

                        for t in followSet do
                            let la = List.head t
                            let key = (stateIdx, la)

                            match Map.tryFind key action with
                            | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, la, s, ruleIdx) :: conflicts
                            | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, la, r, ruleIdx) :: conflicts
                            | Some Accept -> conflicts <- ShiftReduce(stateIdx, la, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (Reduce ruleIdx) action

        { action = action
          goto = goto
          conflicts = List.rev conflicts
          automaton = LR0 lr0 }

    /// Build the CLR(1) (canonical LR(1)) parsing table from an augmented grammar.
    let buildCLR1Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.rules.[0]
        let lr1 = LRAutomaton.buildLR1 aug
        let states = lr1.states

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        populateShiftGoto lr1.transitions &action &goto &conflicts

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompletedLR1 item then
                    if item.lhs = augmentedRule.lhs && item.dot = 1 then
                        let key = (stateIdx, Epsilon)

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, Epsilon, s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, Epsilon, r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules
                            |> List.findIndex (fun r -> r.lhs = item.lhs && Rhs.toNonEpsilonList r.rhs = item.rhs)

                        let t = item.lookahead

                        let key = (stateIdx, t)

                        match Map.tryFind key action with
                        | Some existing when existing <> Reduce ruleIdx ->
                            conflicts <-
                                (match existing with
                                 | Shift s -> ShiftReduce(stateIdx, t, s, ruleIdx)
                                 | Reduce r -> ReduceReduce(stateIdx, t, r, ruleIdx)
                                 | _ -> failwith "Unexpected")
                                :: conflicts
                        | Some _ -> ()
                        | None -> action <- Map.add key (Reduce ruleIdx) action

        { action = action
          goto = goto
          conflicts = List.rev conflicts
          automaton = LR1 lr1 }

    /// Parse pre-tokenized input using an LR parsing table, building a derivation tree.
    /// Also collects visualization steps as structured data.
    let parseWithSteps
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> * LRParsingStep<'t, 'nt> list =
        let tokens = terminals |> List.map (fun (Terminal t) -> T(Terminal t))
        let mutable stack: LRStackFrame<'t, 'nt> list = [ LRState 0 ]
        let mutable pos = 0
        let mutable finished = false
        let mutable accepted = false
        let mutable iteration = 0
        let mutable steps: LRParsingStep<'t, 'nt> list = []

        let topState () =
            match stack with
            | LRState s :: _ -> s
            | _ -> failwith "Expected state on top of LR stack"

        let recordStep () =
            steps <-
                { stack = stack
                  input = { tokens = terminals; position = pos } }
                :: steps

        let popFrames count =
            let mutable remaining = stack
            let mutable children = []

            for _ in 1..count do
                match remaining with
                | LRState _ :: LRSymbol tree :: rest ->
                    children <- tree :: children
                    remaining <- rest
                | _ -> failwith "Invalid LR stack frame"

            stack <- remaining
            children

        recordStep ()

        while not finished do
            iteration <- iteration + 1
            let currentState = topState ()

            let lookahead = if pos < tokens.Length then tokens.[pos] else Epsilon

            match Map.tryFind (currentState, lookahead) table.action with
            | Some(Shift nextState) ->
                stack <- LRSymbol(Leaf(tokens.[pos])) :: stack
                stack <- LRState nextState :: stack
                pos <- pos + 1
                recordStep ()
            | Some(Reduce ruleIdx) ->
                let rule = aug.rules.[ruleIdx]
                let popCount = Rhs.toNonEpsilonList rule.rhs |> List.length

                let children = popFrames popCount
                let newNode = Node(rule.lhs, children)

                let gotoState =
                    match Map.tryFind (topState (), rule.lhs) table.goto with
                    | Some gs -> gs
                    | None -> failwith "Goto not found"

                stack <- LRSymbol(newNode) :: stack
                stack <- LRState gotoState :: stack
                recordStep ()
            | Some Accept ->
                finished <- true
                accepted <- true
            | None ->
                if pos = tokens.Length && lookahead = Epsilon then
                    match Map.tryFind (currentState, Epsilon) table.action with
                    | Some Accept ->
                        finished <- true
                        accepted <- true
                    | _ -> finished <- true
                else
                    finished <- true

        let tree =
            let trees =
                stack
                |> List.choose (function
                    | LRSymbol t -> Some t
                    | _ -> None)

            if accepted && trees.Length = 1 then
                Some trees.Head
            else
                None

        tree, List.rev steps

    /// Parse pre-tokenized input using an LR parsing table, building a derivation tree.
    let parse
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> =
        parseWithSteps aug table terminals |> fst
