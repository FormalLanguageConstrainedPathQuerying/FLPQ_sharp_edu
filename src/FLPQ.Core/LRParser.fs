namespace FLPQ.Core

/// LR(0) item: A -> α·β
/// Dot position tracks how much of the RHS has been consumed.
type LR0Item =
    { Lhs: Nonterminal<string>
      Rhs: Symbol<string, string> list
      Dot: int }

/// LR(1) item: A -> α·β, l
/// Adds a lookahead terminal to each item for more precise reduce decisions.
type LR1Item =
    { Lhs: Nonterminal<string>
      Rhs: Symbol<string, string> list
      Dot: int
      Lookahead: Terminal<string> }

/// Action in an LR parsing table.
type LRAction =
    | Shift of int
    | Reduce of int
    | Accept

/// Conflict detected during LR table construction.
type LRConflict =
    | ShiftReduce of state: int * symbol: string * shiftTo: int * reduceRule: int
    | ReduceReduce of state: int * symbol: string * rule1: int * rule2: int

/// LR parsing table with action and goto maps, plus detected conflicts.
type LRTable =
    { action: Map<int * string, LRAction>
      goto: Map<int * Nonterminal<string>, int>
      conflicts: LRConflict list }

/// Construction of LR(0) and LR(1) automata as deterministic finite automata.
/// Each automaton state is a set of items; transitions are labeled with grammar symbols.
module LRAutomaton =

    let internal augmentGrammar (g: Grammar<string, string>) : Grammar<string, string> =
        let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")

        { rules =
            { lhs = freshStart
              rhs = [ N g.start ] }
            :: g.rules
          start = freshStart }

    let private closureLR0 (rules: Rule<string, string> list) (items: Set<LR0Item>) : Set<LR0Item> =
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
                            |> List.map (fun r -> { Lhs = r.lhs; Rhs = r.rhs; Dot = 0 })

                        for ni in newItems do
                            if not (Set.contains ni closure) then
                                closure <- Set.add ni closure
                                changed <- true
                    | _ -> ()

        closure

    let private gotoLR0
        (rules: Rule<string, string> list)
        (items: Set<LR0Item>)
        (sym: Symbol<string, string>)
        : Set<LR0Item> =
        items
        |> Set.filter (fun item -> item.Dot < item.Rhs.Length && item.Rhs.[item.Dot] = sym)
        |> Set.map (fun item -> { item with Dot = item.Dot + 1 })
        |> closureLR0 rules

    let private closureLR1
        (rules: Rule<string, string> list)
        (items: Set<LR1Item>)
        (firstMap: Map<Nonterminal<string>, Set<string>>)
        : Set<LR1Item> =
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
                                set [ item.Lookahead |> fun (Terminal t) -> t ]
                            else
                                FirstFollow.firstKOfString firstMap 1 beta |> Set.filter (fun s -> s <> "")

                        let newItems =
                            rules
                            |> List.filter (fun r -> r.lhs = nt)
                            |> List.collect (fun r ->
                                lookaheads
                                |> Set.toList
                                |> List.map (fun la ->
                                    { Lhs = r.lhs
                                      Rhs = r.rhs
                                      Dot = 0
                                      Lookahead = Terminal la }))

                        for ni in newItems do
                            if not (Set.contains ni closure) then
                                closure <- Set.add ni closure
                                changed <- true
                    | _ -> ()

        closure

    let private gotoLR1
        (rules: Rule<string, string> list)
        (items: Set<LR1Item>)
        (sym: Symbol<string, string>)
        (firstMap: Map<Nonterminal<string>, Set<string>>)
        : Set<LR1Item> =
        items
        |> Set.filter (fun item -> item.Dot < item.Rhs.Length && item.Rhs.[item.Dot] = sym)
        |> Set.map (fun item -> { item with Dot = item.Dot + 1 })
        |> (fun filtered -> closureLR1 rules filtered firstMap)

    /// Build the LR(0) automaton for a grammar.
    /// States are sets of LR(0) items. Transitions are labeled with grammar symbols.
    /// Returns a deterministic finite automaton.
    let buildLR0 (g: Grammar<string, string>) : Automaton<Symbol<string, string>, Set<LR0Item>> =
        let aug = augmentGrammar g
        let augmentedRule = aug.rules.[0]

        let startItems =
            closureLR0
                aug.rules
                (set
                    [ { Lhs = augmentedRule.lhs
                        Rhs = augmentedRule.rhs
                        Dot = 0 } ])

        let mutable states = [ startItems ]
        let mutable transitions: (int * Symbol<string, string> * int) list = []
        let mutable queue = [ startItems ]

        while not (List.isEmpty queue) do
            let state = queue.Head
            let stateIdx = states |> List.findIndex (fun s -> s = state)
            queue <- queue.Tail

            if states.Length > 500 then
                failwithf "LR states exceeded 500 — likely infinite loop"

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
                  Rhs = augmentedRule.rhs
                  Dot = augmentedRule.rhs.Length }

            states
            |> List.indexed
            |> List.choose (fun (idx, s) -> if Set.contains acceptItem s then Some idx else None)
            |> Set.ofList

        Automaton.fromTransitions states (List.rev transitions) (set [ 0 ]) finalStates

    /// Build the LR(1) automaton for a grammar.
    /// States are sets of LR(1) items (with lookahead terminals).
    /// Transitions are labeled with grammar symbols.
    /// Returns a deterministic finite automaton.
    let buildLR1 (g: Grammar<string, string>) : Automaton<Symbol<string, string>, Set<LR1Item>> =
        let aug = augmentGrammar g
        let augmentedRule = aug.rules.[0]
        let firstMap = FirstFollow.firstK aug 1

        let startItems =
            closureLR1
                aug.rules
                (set
                    [ { Lhs = augmentedRule.lhs
                        Rhs = augmentedRule.rhs
                        Dot = 0
                        Lookahead = Terminal "" } ])
                firstMap

        let mutable states = [ startItems ]
        let mutable transitions: (int * Symbol<string, string> * int) list = []
        let mutable queue = [ startItems ]

        while not (List.isEmpty queue) do
            let state = queue.Head
            let stateIdx = states |> List.findIndex (fun s -> s = state)
            queue <- queue.Tail

            if states.Length > 500 then
                failwithf "LR states exceeded 500 — likely infinite loop"

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
                  Rhs = augmentedRule.rhs
                  Dot = augmentedRule.rhs.Length
                  Lookahead = Terminal "" }

            states
            |> List.indexed
            |> List.choose (fun (idx, s) -> if Set.contains acceptItem s then Some idx else None)
            |> Set.ofList

        Automaton.fromTransitions states (List.rev transitions) (set [ 0 ]) finalStates

/// LR parsing table construction and parser.
module LRParser =

    let private augmentGrammar g = LRAutomaton.augmentGrammar g

    /// Build the LR(0) parsing table.
    /// Completed items cause reduce actions on all grammar terminals (*including* end-of-input "").
    /// No lookahead information is used — conflicts are expected for most non-trivial grammars.
    let buildLR0Table (g: Grammar<string, string>) : LRTable =
        let aug = augmentGrammar g
        let augmentedRule = aug.rules.[0]
        let lr0 = LRAutomaton.buildLR0 g
        let states = lr0.states

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict list = []

        let allTerminals =
            [ for rule in aug.rules do
                  for sym in rule.rhs do
                      match sym with
                      | T(Terminal t) -> yield t
                      | _ -> () ]
            |> List.distinct

        for i in 0 .. lr0.transitions.rows - 1 do
            for j in 0 .. lr0.transitions.cols - 1 do
                for sym in lr0.transitions.data.[i, j] do
                    match sym with
                    | T(Terminal t) ->
                        let key = (i, t)

                        match Map.tryFind key action with
                        | Some(Reduce r) -> conflicts <- ShiftReduce(i, t, j, r) :: conflicts
                        | Some(Shift _) -> ()
                        | Some Accept -> conflicts <- ShiftReduce(i, t, j, -1) :: conflicts
                        | None -> action <- Map.add key (Shift j) action
                    | N nt -> goto <- Map.add (i, nt) j goto

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if item.Dot = item.Rhs.Length then
                    if item.Lhs = augmentedRule.lhs && item.Dot = 1 then
                        let key = (stateIdx, "")

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, "", s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, "", r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules |> List.findIndex (fun r -> r.lhs = item.Lhs && r.rhs = item.Rhs)

                        for t in "" :: allTerminals do
                            let key = (stateIdx, t)

                            match Map.tryFind key action with
                            | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, t, s, ruleIdx) :: conflicts
                            | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, t, r, ruleIdx) :: conflicts
                            | Some Accept -> conflicts <- ShiftReduce(stateIdx, t, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (Reduce ruleIdx) action

        { action = action
          goto = goto
          conflicts = List.rev conflicts }

    /// Build the SLR(1) parsing table.
    /// Reduce actions are restricted to follow sets of the LHS nonterminal.
    /// Resolves many LR(0) conflicts.
    let buildSLR1Table (g: Grammar<string, string>) : LRTable =
        let aug = augmentGrammar g
        let augmentedRule = aug.rules.[0]
        let lr0 = LRAutomaton.buildLR0 g
        let states = lr0.states
        let followMap = FirstFollow.followK aug 1

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict list = []

        for i in 0 .. lr0.transitions.rows - 1 do
            for j in 0 .. lr0.transitions.cols - 1 do
                for sym in lr0.transitions.data.[i, j] do
                    match sym with
                    | T(Terminal t) ->
                        let key = (i, t)

                        match Map.tryFind key action with
                        | Some(Reduce r) -> conflicts <- ShiftReduce(i, t, j, r) :: conflicts
                        | Some(Shift _) -> ()
                        | Some Accept -> conflicts <- ShiftReduce(i, t, j, -1) :: conflicts
                        | None -> action <- Map.add key (Shift j) action
                    | N nt -> goto <- Map.add (i, nt) j goto

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if item.Dot = item.Rhs.Length then
                    if item.Lhs = augmentedRule.lhs && item.Dot = 1 then
                        let key = (stateIdx, "")

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, "", s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, "", r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules |> List.findIndex (fun r -> r.lhs = item.Lhs && r.rhs = item.Rhs)

                        let followSet = followMap |> Map.find item.Lhs

                        for t in followSet do
                            let key = (stateIdx, t)

                            match Map.tryFind key action with
                            | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, t, s, ruleIdx) :: conflicts
                            | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, t, r, ruleIdx) :: conflicts
                            | Some Accept -> conflicts <- ShiftReduce(stateIdx, t, -1, ruleIdx) :: conflicts
                            | None -> action <- Map.add key (Reduce ruleIdx) action

        { action = action
          goto = goto
          conflicts = List.rev conflicts }

    /// Build the CLR(1) (canonical LR(1)) parsing table.
    /// Uses lookahead from LR(1) items for precise reduce decisions.
    /// The most powerful LR construction — resolves conflicts that SLR(1) cannot.
    let buildCLR1Table (g: Grammar<string, string>) : LRTable =
        let aug = augmentGrammar g
        let augmentedRule = aug.rules.[0]
        let lr1 = LRAutomaton.buildLR1 g
        let states = lr1.states

        let mutable action = Map.empty
        let mutable goto = Map.empty
        let mutable conflicts: LRConflict list = []

        for i in 0 .. lr1.transitions.rows - 1 do
            for j in 0 .. lr1.transitions.cols - 1 do
                for sym in lr1.transitions.data.[i, j] do
                    match sym with
                    | T(Terminal t) ->
                        let key = (i, t)

                        match Map.tryFind key action with
                        | Some(Reduce r) -> conflicts <- ShiftReduce(i, t, j, r) :: conflicts
                        | Some(Shift _) -> ()
                        | Some Accept -> conflicts <- ShiftReduce(i, t, j, -1) :: conflicts
                        | None -> action <- Map.add key (Shift j) action
                    | N nt -> goto <- Map.add (i, nt) j goto

        for stateIdx in 0 .. states.Length - 1 do
            let state = states.[stateIdx]

            for item in state do
                if item.Dot = item.Rhs.Length then
                    if item.Lhs = augmentedRule.lhs && item.Dot = 1 then
                        let key = (stateIdx, "")

                        match Map.tryFind key action with
                        | Some(Shift s) -> conflicts <- ShiftReduce(stateIdx, "", s, -1) :: conflicts
                        | Some(Reduce r) -> conflicts <- ReduceReduce(stateIdx, "", r, -1) :: conflicts
                        | Some Accept -> ()
                        | None -> action <- Map.add key Accept action
                    else
                        let ruleIdx =
                            aug.rules |> List.findIndex (fun r -> r.lhs = item.Lhs && r.rhs = item.Rhs)

                        let (Terminal t) = item.Lookahead

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

    /// Parse an input string using an LR parsing table, building a derivation tree.
    /// Uses an augmented grammar internally for correct rule reference.
    /// Returns Some(tree) on successful parse, None on failure.
    let parse (g: Grammar<string, string>) (table: LRTable) (input: string) : Option<DerivationTree<string, string>> =
        let aug = augmentGrammar g

        let tokens =
            input.ToCharArray()
            |> Array.map (fun c -> Terminal(c.ToString()))
            |> Array.toList

        let mutable stateStack: int list = [ 0 ]
        let mutable treeStack: DerivationTree<string, string> list = []
        let mutable pos = 0
        let mutable finished = false
        let mutable accepted = false
        let mutable steps = 0

        while not finished && steps < 10000 do
            steps <- steps + 1
            let currentState = stateStack.Head

            let lookahead =
                if pos < tokens.Length then
                    (let (Terminal t) = tokens.[pos] in t)
                else
                    ""

            match Map.tryFind (currentState, lookahead) table.action with
            | Some(Shift nextState) ->
                stateStack <- nextState :: stateStack
                treeStack <- Leaf(tokens.[pos]) :: treeStack
                pos <- pos + 1
            | Some(Reduce ruleIdx) ->
                let rule = aug.rules.[ruleIdx]
                let popCount = rule.rhs.Length

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
                if pos = tokens.Length && lookahead = "" then
                    match Map.tryFind (currentState, "") table.action with
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

    /// Collect all leaf terminals from a derivation tree (left-to-right).
    /// Delegate to LLParser.leaves.
    let leaves: DerivationTree<string, string> -> string list = LLParser.leaves
