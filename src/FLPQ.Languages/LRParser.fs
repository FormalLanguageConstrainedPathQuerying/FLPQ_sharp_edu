namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

/// LR(0) item: A -> α·β
/// Dot position tracks how much of the RHS has been consumed.
type LR0Item<'t, 'nt> =
    { Lhs: Nonterminal<'nt>
      Rhs: Symbol<'t, 'nt> list
      Dot: int }

/// LR(1) item: A -> α·β, l
/// Adds a lookahead symbol to each item for more precise reduce decisions.
type LR1Item<'t, 'nt> =
    { Lhs: Nonterminal<'nt>
      Rhs: Symbol<'t, 'nt> list
      Dot: int
      Lookahead: Symbol<'t, 'nt> }

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
    { Action: Map<int * Symbol<'t, 'nt>, LRAction<int>>
      GoTo: Map<int * Nonterminal<'nt>, int>
      Conflicts: LRConflict<'t, 'nt> list
      Automaton: LRAutomaton<'t, 'nt> }

/// Frame on the unified LR parser stack.
/// Tree nodes are symbols: roots of partial trees are placed in stack and used as symbols.
[<Struct>]
type LRStackFrame<'t, 'nt> =
    | LRState of state: int
    | LRSymbol of tree: DerivationTree<'t, 'nt>

/// Data for a single LR parser visualization step.
[<Struct>]
type LRParsingStep<'t, 'nt> =
    { Stack: LRStackFrame<'t, 'nt> list
      Input: StepInput<'t> }

/// Construction of LR(0) and LR(1) automata as deterministic finite automata.
/// Functions take an already-augmented grammar (with fresh start nonterminal).
module LRAutomaton =

    /// Augment a grammar with a fresh start nonterminal.
    /// The augmented grammar has S' -> S as the first rule.
    let augmentGrammar (freshStart: Nonterminal<'nt>) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        { Rules =
            { Lhs = freshStart
              Rhs = NonEmptyList.create (Symbol.N g.Start) [] |> Symbols }
            :: g.Rules
          Start = freshStart }

    let private closureLR0 (rules: Rule<'t, 'nt> list) (items: Set<LR0Item<'t, 'nt>>) : Set<LR0Item<'t, 'nt>> =
        let mutable closure = items
        let mutable changed = true

        while changed do
            changed <- false

            for item in closure |> Set.toSeq |> Seq.toList do
                if item.Dot < item.Rhs.Length then
                    match item.Rhs.[item.Dot] with
                    | Symbol.N nt ->
                        let newItems =
                            rules
                            |> List.filter (fun r -> r.Lhs = nt)
                            |> List.map (fun r ->
                                { Lhs = r.Lhs
                                  Rhs = Rhs.toNonEpsilonList r.Rhs
                                  Dot = 0 })

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
        |> Set.filter (fun item -> item.Dot < item.Rhs.Length && item.Rhs.[item.Dot] = sym)
        |> Set.map (fun item -> { item with Dot = item.Dot + 1 })
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
                if item.Dot < item.Rhs.Length then
                    match item.Rhs.[item.Dot] with
                    | Symbol.N nt ->
                        let beta =
                            if item.Dot + 1 < item.Rhs.Length then
                                item.Rhs |> List.skip (item.Dot + 1)
                            else
                                []

                        let lookaheads =
                            if beta.IsEmpty then
                                set [ [ item.Lookahead ] ]
                            else
                                FirstFollow.firstKOfString firstMap 1 beta
                                |> Set.filter (fun s -> s <> [ Symbol.Epsilon ])

                        let newItems =
                            rules
                            |> List.filter (fun r -> r.Lhs = nt)
                            |> List.collect (fun r ->
                                lookaheads
                                |> Set.toList
                                |> List.map (fun la ->
                                    { Lhs = r.Lhs
                                      Rhs = Rhs.toNonEpsilonList r.Rhs
                                      Dot = 0
                                      Lookahead = List.head la }))

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
        |> Set.filter (fun item -> item.Dot < item.Rhs.Length && item.Rhs.[item.Dot] = sym)
        |> Set.map (fun item -> { item with Dot = item.Dot + 1 })
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
        let augmentedRule = aug.Rules.[0]

        buildLR
            aug.Rules
            augmentedRule
            (fun r ->
                { Lhs = r.Lhs
                  Rhs = Rhs.toNonEpsilonList r.Rhs
                  Dot = 0 })
            (fun r ->
                { Lhs = r.Lhs
                  Rhs = Rhs.toNonEpsilonList r.Rhs
                  Dot = Rhs.toNonEpsilonList r.Rhs |> List.length })
            (fun i -> i.Dot)
            (fun i -> i.Rhs)
            (closureLR0 aug.Rules)
            (gotoLR0 aug.Rules)

    /// Build the LR(1) automaton for an augmented grammar.
    /// States are sets of LR(1) items. Transitions are labeled with grammar symbols.
    let buildLR1 (aug: Grammar<'t, 'nt>) : DFA<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>> =
        let augmentedRule = aug.Rules.[0]
        let firstMap = FirstFollow.firstK aug 1

        buildLR
            aug.Rules
            augmentedRule
            (fun r ->
                { Lhs = r.Lhs
                  Rhs = Rhs.toNonEpsilonList r.Rhs
                  Dot = 0
                  Lookahead = Symbol.Epsilon })
            (fun r ->
                { Lhs = r.Lhs
                  Rhs = Rhs.toNonEpsilonList r.Rhs
                  Dot = Rhs.toNonEpsilonList r.Rhs |> List.length
                  Lookahead = Symbol.Epsilon })
            (fun i -> i.Dot)
            (fun i -> i.Rhs)
            (fun items -> closureLR1 aug.Rules items firstMap)
            (fun items sym -> gotoLR1 aug.Rules items sym firstMap)

/// LR parsing table construction and parser.
/// All table builders and parse take an already-augmented grammar.
module LRParser =

    let private isCompletedLR0 (item: LR0Item<'t, 'nt>) : bool = item.Dot = item.Rhs.Length

    let private isCompletedLR1 (item: LR1Item<'t, 'nt>) : bool = item.Dot = item.Rhs.Length

    let private populateShiftGoto
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<Symbol<'t, 'nt>>>>>)
        (action: byref<Map<int * Symbol<'t, 'nt>, LRAction<int>>>)
        (goto: byref<Map<int * Nonterminal<'nt>, int>>)
        (conflicts: byref<LRConflict<'t, 'nt> list>)
        : unit =
        for i in 0 .. Matrix.rows transitions - 1 do
            for j in 0 .. Matrix.cols transitions - 1 do
                match Matrix.get transitions i j with
                | Some symbols ->
                    for sym in NonEmptySet.toSeq symbols do
                        match sym with
                        | ATerm(Symbol.T _ as tSym) ->
                            let key = (i, tSym)

                            match Map.tryFind key action with
                            | Some(LRAction.Reduce r) -> conflicts <- ShiftReduce(i, tSym, j, r) :: conflicts
                            | Some(LRAction.Shift _) -> ()
                            | Some LRAction.Accept -> conflicts <- ShiftReduce(i, tSym, j, -1) :: conflicts
                            | None -> action <- Map.add key (LRAction.Shift j) action
                        | ATerm(Symbol.N nt) -> goto <- Map.add (i, nt) j goto
                        | _ -> ()
                | None -> ()

    /// Build the LR(0) parsing table from an augmented grammar.
    let buildLR0Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.Rules.[0]
        let lr0 = LRAutomaton.buildLR0 aug
        let states = lr0.States

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        let allTerminals =
            [ for rule in aug.Rules do
                  for sym in Rhs.toNonEpsilonList rule.Rhs do
                      match sym with
                      | Symbol.T _ as tSym -> yield tSym
                      | _ -> () ]
            |> List.distinct

        populateShiftGoto lr0.Transitions &action &goto &conflicts

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompletedLR0 item then
                    if item.Lhs = augmentedRule.Lhs && item.Dot = 1 then
                        let key = (stateIdx, Symbol.Epsilon)

                        match Map.tryFind key action with
                        | Some(LRAction.Shift s) -> conflicts <- ShiftReduce(stateIdx, Symbol.Epsilon, s, -1) :: conflicts
                        | Some(LRAction.Reduce r) -> conflicts <- ReduceReduce(stateIdx, Symbol.Epsilon, r, -1) :: conflicts
                        | Some LRAction.Accept -> ()
                        | None -> action <- Map.add key LRAction.Accept action
                    else
                        let ruleIdx =
                            aug.Rules
                            |> List.findIndex (fun r -> r.Lhs = item.Lhs && Rhs.toNonEpsilonList r.Rhs = item.Rhs)

                        for t in Symbol.Epsilon :: allTerminals do
                            let key = (stateIdx, t)

                            match Map.tryFind key action with
                            | Some(LRAction.Shift s) -> conflicts <- ShiftReduce(stateIdx, t, s, ruleIdx) :: conflicts
                            | Some(LRAction.Reduce r) -> conflicts <- ReduceReduce(stateIdx, t, r, ruleIdx) :: conflicts
                            | Some LRAction.Accept -> conflicts <- ShiftReduce(stateIdx, t, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (LRAction.Reduce ruleIdx) action

        { Action = action
          GoTo = goto
          Conflicts = List.rev conflicts
          Automaton = LR0 lr0 }

    /// Build the SLR(1) parsing table from an augmented grammar.
    let buildSLR1Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.Rules.[0]
        let lr0 = LRAutomaton.buildLR0 aug
        let states = lr0.States
        let followMap = FirstFollow.followK aug 1

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        populateShiftGoto lr0.Transitions &action &goto &conflicts

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompletedLR0 item then
                    if item.Lhs = augmentedRule.Lhs && item.Dot = 1 then
                        let key = (stateIdx, Symbol.Epsilon)

                        match Map.tryFind key action with
                        | Some(LRAction.Shift s) -> conflicts <- ShiftReduce(stateIdx, Symbol.Epsilon, s, -1) :: conflicts
                        | Some(LRAction.Reduce r) -> conflicts <- ReduceReduce(stateIdx, Symbol.Epsilon, r, -1) :: conflicts
                        | Some LRAction.Accept -> ()
                        | None -> action <- Map.add key LRAction.Accept action
                    else
                        let ruleIdx =
                            aug.Rules
                            |> List.findIndex (fun r -> r.Lhs = item.Lhs && Rhs.toNonEpsilonList r.Rhs = item.Rhs)

                        let followSet = followMap |> Map.find item.Lhs

                        for t in followSet do
                            let la = List.head t
                            let key = (stateIdx, la)

                            match Map.tryFind key action with
                            | Some(LRAction.Shift s) -> conflicts <- ShiftReduce(stateIdx, la, s, ruleIdx) :: conflicts
                            | Some(LRAction.Reduce r) -> conflicts <- ReduceReduce(stateIdx, la, r, ruleIdx) :: conflicts
                            | Some LRAction.Accept -> conflicts <- ShiftReduce(stateIdx, la, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (LRAction.Reduce ruleIdx) action

        { Action = action
          GoTo = goto
          Conflicts = List.rev conflicts
          Automaton = LR0 lr0 }

    /// Build the CLR(1) (canonical LR(1)) parsing table from an augmented grammar.
    let buildCLR1Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.Rules.[0]
        let lr1 = LRAutomaton.buildLR1 aug
        let states = lr1.States

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        populateShiftGoto lr1.Transitions &action &goto &conflicts

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompletedLR1 item then
                    if item.Lhs = augmentedRule.Lhs && item.Dot = 1 then
                        let key = (stateIdx, Symbol.Epsilon)

                        match Map.tryFind key action with
                        | Some(LRAction.Shift s) -> conflicts <- ShiftReduce(stateIdx, Symbol.Epsilon, s, -1) :: conflicts
                        | Some(LRAction.Reduce r) -> conflicts <- ReduceReduce(stateIdx, Symbol.Epsilon, r, -1) :: conflicts
                        | Some LRAction.Accept -> ()
                        | None -> action <- Map.add key LRAction.Accept action
                    else
                        let ruleIdx =
                            aug.Rules
                            |> List.findIndex (fun r -> r.Lhs = item.Lhs && Rhs.toNonEpsilonList r.Rhs = item.Rhs)

                        let t = item.Lookahead

                        let key = (stateIdx, t)

                        match Map.tryFind key action with
                        | Some existing when existing <> LRAction.Reduce ruleIdx ->
                            conflicts <-
                                (match existing with
                                 | LRAction.Shift s -> ShiftReduce(stateIdx, t, s, ruleIdx)
                                 | LRAction.Reduce r -> ReduceReduce(stateIdx, t, r, ruleIdx)
                                 | _ -> failwith "Unexpected")
                                :: conflicts
                        | Some _ -> ()
                        | None -> action <- Map.add key (LRAction.Reduce ruleIdx) action

        { Action = action
          GoTo = goto
          Conflicts = List.rev conflicts
          Automaton = LR1 lr1 }

    /// Parse pre-tokenized input using an LR parsing table, building a derivation tree.
    /// Also collects visualization steps as structured data.
    let parseWithSteps
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> * LRParsingStep<'t, 'nt> list =
        let tokens = terminals |> List.map (fun (Terminal t) -> Symbol.T(Terminal t))
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
                { Stack = stack
                  Input = { Tokens = terminals; Position = pos } }
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

            let lookahead = if pos < tokens.Length then tokens.[pos] else Symbol.Epsilon

            match Map.tryFind (currentState, lookahead) table.Action with
            | Some(LRAction.Shift nextState) ->
                stack <- LRSymbol(Leaf(tokens.[pos])) :: stack
                stack <- LRState nextState :: stack
                pos <- pos + 1
                recordStep ()
            | Some(LRAction.Reduce ruleIdx) ->
                let rule = aug.Rules.[ruleIdx]
                let popCount = Rhs.toNonEpsilonList rule.Rhs |> List.length

                let children = popFrames popCount
                let newNode = Node(rule.Lhs, children)

                let gotoState =
                    match Map.tryFind (topState (), rule.Lhs) table.GoTo with
                    | Some gs -> gs
                    | None -> failwith "Goto not found"

                stack <- LRSymbol(newNode) :: stack
                stack <- LRState gotoState :: stack
                recordStep ()
            | Some LRAction.Accept ->
                finished <- true
                accepted <- true
            | None ->
                if pos = tokens.Length && lookahead = Symbol.Epsilon then
                    match Map.tryFind (currentState, Symbol.Epsilon) table.Action with
                    | Some LRAction.Accept ->
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
