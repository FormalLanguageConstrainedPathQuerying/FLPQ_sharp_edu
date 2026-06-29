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

/// Action in an LR parsing table.
type LRAction =
    | Shift of int
    | Reduce of int
    | Accept

/// Conflict detected during LR table construction.
type LRConflict<'t, 'nt> =
    | ShiftReduce of state: int * symbol: Symbol<'t, 'nt> * shiftTo: int * reduceRule: int
    | ReduceReduce of state: int * symbol: Symbol<'t, 'nt> * rule1: int * rule2: int

/// LR parsing table with action and goto maps, plus detected conflicts.
type LRTable<'t, 'nt when 't: comparison and 'nt: comparison> =
    { action: Map<int * Symbol<'t, 'nt>, LRAction>
      goto: Map<int * Nonterminal<'nt>, int>
      conflicts: LRConflict<'t, 'nt> list }

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
                if item.Dot < item.Rhs.Length then
                    match item.Rhs.[item.Dot] with
                    | N nt ->
                        let newItems =
                            rules
                            |> List.filter (fun r -> r.lhs = nt)
                            |> List.map (fun r ->
                                { Lhs = r.lhs
                                  Rhs = Rhs.toSymbols r.rhs
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
                    | N nt ->
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
                                |> Set.filter (fun s -> s <> [ Epsilon ])

                        let newItems =
                            rules
                            |> List.filter (fun r -> r.lhs = nt)
                            |> List.collect (fun r ->
                                lookaheads
                                |> Set.toList
                                |> List.map (fun la ->
                                    { Lhs = r.lhs
                                      Rhs = Rhs.toSymbols r.rhs
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

    /// Build the LR(0) automaton for an augmented grammar.
    /// States are sets of LR(0) items. Transitions are labeled with grammar symbols.
    let buildLR0 (aug: Grammar<'t, 'nt>) : Automaton<Symbol<'t, 'nt>, Set<LR0Item<'t, 'nt>>> =
        let augmentedRule = aug.rules.[0]

        let startItems =
            closureLR0
                aug.rules
                (set
                    [ { Lhs = augmentedRule.lhs
                        Rhs = Rhs.toSymbols augmentedRule.rhs
                        Dot = 0 } ])

        let mutable states = [ startItems ]
        let mutable transitions: (int * Symbol<'t, 'nt> * int) list = []
        let mutable queue = [ startItems ]

        while not (List.isEmpty queue) do
            let state = queue.Head
            let stateIdx = states |> List.findIndex (fun s -> s = state)
            queue <- queue.Tail

            let symbols =
                state
                |> Set.toSeq
                |> Seq.choose (fun item ->
                    if item.Dot < item.Rhs.Length then
                        Some item.Rhs.[item.Dot]
                    else
                        None)
                |> Seq.distinct
                |> Seq.toList

            for sym in symbols do
                let target = gotoLR0 aug.rules state sym

                if not (Set.isEmpty target) then
                    let targetIdx =
                        match states |> List.tryFindIndex (fun s -> s = target) with
                        | Some idx -> idx
                        | None ->
                            let idx = List.length states
                            states <- states @ [ target ]
                            queue <- queue @ [ target ]
                            idx

                    transitions <- (stateIdx, sym, targetIdx) :: transitions

        let finalStates =
            let acceptItem =
                { Lhs = augmentedRule.lhs
                  Rhs = Rhs.toSymbols augmentedRule.rhs
                  Dot = Rhs.toSymbols augmentedRule.rhs |> List.length }

            states
            |> List.indexed
            |> List.choose (fun (idx, s) -> if Set.contains acceptItem s then Some idx else None)
            |> Set.ofList

        Automaton.fromTransitions states (List.rev transitions) Set.empty (set [ 0 ]) finalStates

    /// Build the LR(1) automaton for an augmented grammar.
    /// States are sets of LR(1) items. Transitions are labeled with grammar symbols.
    let buildLR1 (aug: Grammar<'t, 'nt>) : Automaton<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>> =
        let augmentedRule = aug.rules.[0]
        let firstMap = FirstFollow.firstK aug 1

        let startItems =
            closureLR1
                aug.rules
                (set
                    [ { Lhs = augmentedRule.lhs
                        Rhs = Rhs.toSymbols augmentedRule.rhs
                        Dot = 0
                        Lookahead = Epsilon } ])
                firstMap

        let mutable states = [ startItems ]
        let mutable transitions: (int * Symbol<'t, 'nt> * int) list = []
        let mutable queue = [ startItems ]

        while not (List.isEmpty queue) do
            let state = queue.Head
            let stateIdx = states |> List.findIndex (fun s -> s = state)
            queue <- queue.Tail

            let symbols =
                state
                |> Set.toSeq
                |> Seq.choose (fun item ->
                    if item.Dot < item.Rhs.Length then
                        Some item.Rhs.[item.Dot]
                    else
                        None)
                |> Seq.distinct
                |> Seq.toList

            for sym in symbols do
                let target = gotoLR1 aug.rules state sym firstMap

                if not (Set.isEmpty target) then
                    let targetIdx =
                        match states |> List.tryFindIndex (fun s -> s = target) with
                        | Some idx -> idx
                        | None ->
                            let idx = List.length states
                            states <- states @ [ target ]
                            queue <- queue @ [ target ]
                            idx

                    transitions <- (stateIdx, sym, targetIdx) :: transitions

        let finalStates =
            let acceptItem =
                { Lhs = augmentedRule.lhs
                  Rhs = Rhs.toSymbols augmentedRule.rhs
                  Dot = Rhs.toSymbols augmentedRule.rhs |> List.length
                  Lookahead = Epsilon }

            states
            |> List.indexed
            |> List.choose (fun (idx, s) -> if Set.contains acceptItem s then Some idx else None)
            |> Set.ofList

        Automaton.fromTransitions states (List.rev transitions) Set.empty (set [ 0 ]) finalStates

/// LR parsing table construction and parser.
/// All table builders and parse take an already-augmented grammar.
module LRParser =

    let private isCompleted (item: LR0Item<'t, 'nt>) : bool = item.Dot = item.Rhs.Length

    let private isCompleted1 (item: LR1Item<'t, 'nt>) : bool = item.Dot = item.Rhs.Length

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
                  for sym in Rhs.toSymbols rule.rhs do
                      match sym with
                      | T _ as tSym -> yield tSym
                      | _ -> () ]
            |> List.distinct

        for i in 0 .. lr0.transitions.rows - 1 do
            for j in 0 .. lr0.transitions.cols - 1 do
                match lr0.transitions.data.[i, j] with
                | Some symbols ->
                    for sym in NonEmptySet.toSeq symbols do
                        match sym with
                        | T _ as tSym ->
                            let key = (i, tSym)

                            match Map.tryFind key action with
                            | Some(Reduce r) -> conflicts <- ShiftReduce(i, tSym, j, r) :: conflicts
                            | Some(Shift _) -> ()
                            | Some Accept -> conflicts <- ShiftReduce(i, tSym, j, -1) :: conflicts
                            | None -> action <- Map.add key (Shift j) action
                        | N nt -> goto <- Map.add (i, nt) j goto
                        | _ -> ()
                | None -> ()

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompleted item then
                    if item.Lhs = augmentedRule.lhs && item.Dot = 1 then
                        let key = (stateIdx, Epsilon)

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, Epsilon, s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, Epsilon, r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules
                            |> List.findIndex (fun r -> r.lhs = item.Lhs && Rhs.toSymbols r.rhs = item.Rhs)

                        for t in Epsilon :: allTerminals do
                            let key = (stateIdx, t)

                            match Map.tryFind key action with
                            | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, t, s, ruleIdx) :: conflicts
                            | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, t, r, ruleIdx) :: conflicts
                            | Some Accept -> conflicts <- ShiftReduce(stateIdx, t, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (Reduce ruleIdx) action

        { action = action
          goto = goto
          conflicts = List.rev conflicts }

    /// Build the SLR(1) parsing table from an augmented grammar.
    let buildSLR1Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.rules.[0]
        let lr0 = LRAutomaton.buildLR0 aug
        let states = lr0.states
        let followMap = FirstFollow.followK aug 1

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        for i in 0 .. lr0.transitions.rows - 1 do
            for j in 0 .. lr0.transitions.cols - 1 do
                match lr0.transitions.data.[i, j] with
                | Some symbols ->
                    for sym in NonEmptySet.toSeq symbols do
                        match sym with
                        | T _ as tSym ->
                            let key = (i, tSym)

                            match Map.tryFind key action with
                            | Some(Reduce r) -> conflicts <- ShiftReduce(i, tSym, j, r) :: conflicts
                            | Some(Shift _) -> ()
                            | Some Accept -> conflicts <- ShiftReduce(i, tSym, j, -1) :: conflicts
                            | None -> action <- Map.add key (Shift j) action
                        | N nt -> goto <- Map.add (i, nt) j goto
                        | _ -> ()
                | None -> ()

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompleted item then
                    if item.Lhs = augmentedRule.lhs && item.Dot = 1 then
                        let key = (stateIdx, Epsilon)

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, Epsilon, s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, Epsilon, r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules
                            |> List.findIndex (fun r -> r.lhs = item.Lhs && Rhs.toSymbols r.rhs = item.Rhs)

                        let followSet = followMap |> Map.find item.Lhs

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
          conflicts = List.rev conflicts }

    /// Build the CLR(1) (canonical LR(1)) parsing table from an augmented grammar.
    let buildCLR1Table (aug: Grammar<'t, 'nt>) : LRTable<'t, 'nt> =
        let augmentedRule = aug.rules.[0]
        let lr1 = LRAutomaton.buildLR1 aug
        let states = lr1.states

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict<'t, 'nt> list = []

        for i in 0 .. lr1.transitions.rows - 1 do
            for j in 0 .. lr1.transitions.cols - 1 do
                match lr1.transitions.data.[i, j] with
                | Some symbols ->
                    for sym in NonEmptySet.toSeq symbols do
                        match sym with
                        | T _ as tSym ->
                            let key = (i, tSym)

                            match Map.tryFind key action with
                            | Some(Reduce r) -> conflicts <- ShiftReduce(i, tSym, j, r) :: conflicts
                            | Some(Shift _) -> ()
                            | Some Accept -> conflicts <- ShiftReduce(i, tSym, j, -1) :: conflicts
                            | None -> action <- Map.add key (Shift j) action
                        | N nt -> goto <- Map.add (i, nt) j goto
                        | _ -> ()
                | None -> ()

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if isCompleted1 item then
                    if item.Lhs = augmentedRule.lhs && item.Dot = 1 then
                        let key = (stateIdx, Epsilon)

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, Epsilon, s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, Epsilon, r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules
                            |> List.findIndex (fun r -> r.lhs = item.Lhs && Rhs.toSymbols r.rhs = item.Rhs)

                        let t = item.Lookahead

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
          conflicts = List.rev conflicts }

    /// Parse pre-tokenized input using an LR parsing table, building a derivation tree.
    /// Takes an augmented grammar for correct rule reference.
    /// Returns Some(tree) on successful parse, None on failure.
    let parse
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (tokens: Symbol<'t, 'nt> list)
        : Option<DerivationTree<'t, 'nt>> =
        let mutable stateStack: int list = [ 0 ]
        let mutable treeStack: DerivationTree<'t, 'nt> list = []
        let mutable pos = 0
        let mutable finished = false
        let mutable accepted = false
        let mutable steps = 0

        while not finished && steps < 10000 do
            steps <- steps + 1
            let currentState = stateStack.Head

            let lookahead = if pos < tokens.Length then tokens.[pos] else Epsilon

            match Map.tryFind (currentState, lookahead) table.action with
            | Some(Shift nextState) ->
                stateStack <- nextState :: stateStack
                treeStack <- Leaf(tokens.[pos]) :: treeStack
                pos <- pos + 1
            | Some(Reduce ruleIdx) ->
                let rule = aug.rules.[ruleIdx]
                let popCount = Rhs.toSymbols rule.rhs |> List.length

                let children = treeStack |> List.take popCount |> List.rev
                stateStack <- stateStack |> List.skip popCount
                treeStack <- treeStack |> List.skip popCount

                let newNode = Node(rule.lhs, children)

                let gotoState =
                    match Map.tryFind (stateStack.Head, rule.lhs) table.goto with
                    | Some gs -> gs
                    | None -> failwith "Goto not found"

                stateStack <- gotoState :: stateStack
                treeStack <- newNode :: treeStack
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

        if accepted && treeStack.Length = 1 then
            Some treeStack.Head
        else
            None
