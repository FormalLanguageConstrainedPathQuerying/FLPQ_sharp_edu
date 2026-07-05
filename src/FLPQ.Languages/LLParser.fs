namespace FLPQ.Languages

module LLParser =

    /// Build an LL(k) parsing table.
    /// Returns Map from (nonterminal, lookahead) to rule index.
    let buildTable (g: Grammar<'t, 'nt>) (k: int) : Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int> =
        let firstMap = FirstFollow.firstK g k
        let followMap = FirstFollow.followK g k

        let mutable table = Map.empty

        for ruleIdx in 0 .. g.rules.Length - 1 do
            let rule = g.rules.[ruleIdx]

            let lookahead =
                if Rhs.isEpsilon rule.rhs then
                    followMap |> Map.find rule.lhs
                else
                    let firstOfRhs =
                        FirstFollow.firstKOfString firstMap k (Rhs.toListWithEpsilon rule.rhs)

                    let withoutEps = Set.remove [ Epsilon ] firstOfRhs
                    let followOfA = followMap |> Map.find rule.lhs

                    if Set.contains [ Epsilon ] firstOfRhs then
                        Set.union withoutEps followOfA
                    else
                        withoutEps

            for w in lookahead do
                if w.Length <= k then
                    let key = (rule.lhs, w)

                    match Map.tryFind key table with
                    | Some existing ->
                        if existing <> ruleIdx then
                            failwithf
                                "LL(%d) conflict: %A with lookahead %A has rules %d and %d"
                                k
                                rule.lhs
                                w
                                existing
                                ruleIdx
                    | None -> table <- Map.add key ruleIdx table

        table

    let private lookahead (tokens: Symbol<'t, 'nt> list) (pos: int) (k: int) : Symbol<'t, 'nt> list =
        if pos >= tokens.Length then
            [ Epsilon ]
        else
            let endIdx = min (pos + k) tokens.Length
            tokens.[pos .. endIdx - 1]

    /// Parse pre-tokenized input using an LL(k) parsing table, building a derivation tree.
    /// Uses mutable tree nodes on the stack: when a nonterminal leaf is expanded,
    /// its children are set in-place and pushed onto the stack.
    /// Collects visualization steps as structured data.
    let parseWithSteps
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> * LLParsingStep<'t, 'nt> list =
        let tokens = terminals |> List.map (fun (Terminal t) -> T(Terminal t))

        let root = MutableTree(N g.start)
        let mutable steps: LLParsingStep<'t, 'nt> list = []

        let recordStep (stack: MutableTree<'t, 'nt> list) (pos: int) =
            if not (List.isEmpty stack) then
                let treeSnapshot = root.ToImmutable()

                let stackSnapshots =
                    stack
                    |> List.map (fun n ->
                        { tree = n.ToImmutable()
                          path = n.GetPath() })

                steps <-
                    { tree = treeSnapshot
                      stack = stackSnapshots
                      input = { tokens = terminals; position = pos } }
                    :: steps

        let expandNonterminal
            (nt: Nonterminal<'nt>)
            (stackTop: MutableTree<'t, 'nt>)
            (restStack: MutableTree<'t, 'nt> list)
            (la: Symbol<'t, 'nt> list)
            : Option<MutableTree<'t, 'nt> list> =
            let key = (nt, la)

            match Map.tryFind key table with
            | Some ruleIdx ->
                let rule = g.rules.[ruleIdx]
                let rhsSyms = Rhs.toListWithEpsilon rule.rhs
                let rhsNodes = rhsSyms |> List.map (fun sym -> MutableTree(sym))

                for child in rhsNodes do
                    child.Parent <- Some stackTop

                stackTop.Children <- rhsNodes
                Some(rhsNodes @ restStack)
            | None -> None

        let rec parseLoop (stack: MutableTree<'t, 'nt> list) (pos: int) : Option<int> =
            recordStep stack pos

            match stack with
            | [] -> if pos = tokens.Length then Some pos else None

            | top :: restStack ->
                match top.Symbol with
                | T _ when pos < tokens.Length && tokens.[pos] = top.Symbol -> parseLoop restStack (pos + 1)

                | Epsilon -> parseLoop restStack pos

                | N nt ->
                    let la = lookahead tokens pos k

                    match expandNonterminal nt top restStack la with
                    | Some newStack -> parseLoop newStack pos
                    | None -> None

                | _ -> None

        match parseLoop [ root ] 0 with
        | Some finalPos when finalPos = tokens.Length ->
            let tree = root.ToImmutable()
            Some tree, List.rev steps
        | _ -> None, List.rev steps

    /// Parse pre-tokenized input using an LL(k) parsing table, building a derivation tree.
    let parse
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> =
        parseWithSteps g table k terminals |> fst
