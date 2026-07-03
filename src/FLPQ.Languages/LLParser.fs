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
                    let firstOfRhs = FirstFollow.firstKOfString firstMap k (Rhs.toList rule.rhs)

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
    /// Uses a unified stack with markers to track nonterminal boundaries and build properly nested trees.
    /// Also collects visualization steps as structured data.
    let parseWithSteps
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> * LLParsingStep<'t, 'nt> list =
        let tokens = terminals |> List.map (fun (Terminal t) -> T(Terminal t))

        let mutable steps: LLParsingStep<'t, 'nt> list = []

        let recordStep (stack: LLStackFrame<'t, 'nt> list) (pos: int) (completed: DerivationTree<'t, 'nt> list) =
            if not (List.isEmpty stack) then
                steps <-
                    { stack = stack
                      completed = completed
                      input = { tokens = terminals; position = pos } }
                    :: steps

        let expandNonterminal
            (nt: Nonterminal<'nt>)
            (restStack: LLStackFrame<'t, 'nt> list)
            (la: Symbol<'t, 'nt> list)
            : Option<LLStackFrame<'t, 'nt> list> =
            let key = (nt, la)

            match Map.tryFind key table with
            | Some ruleIdx ->
                let rule = g.rules.[ruleIdx]
                let rhsSyms = Rhs.toList rule.rhs
                let rhsFrames = rhsSyms |> List.map (fun sym -> LLTree(Leaf sym))
                let marker = LLMarker(nt, rhsSyms.Length)
                Some(rhsFrames @ (marker :: restStack))
            | None -> None

        let rec parseLoop
            (stack: LLStackFrame<'t, 'nt> list)
            (pos: int)
            (completed: DerivationTree<'t, 'nt> list)
            : Option<int * DerivationTree<'t, 'nt> list> =
            recordStep stack pos completed

            match stack with
            | [] -> if pos = tokens.Length then Some(pos, completed) else None

            | LLTree(Leaf(T _ as sym) as tree) :: restStack ->
                if pos < tokens.Length && tokens.[pos] = sym then
                    parseLoop restStack (pos + 1) (completed @ [ tree ])
                else
                    None

            | LLTree(Leaf(Epsilon) as tree) :: restStack -> parseLoop restStack pos (completed @ [ tree ])

            | LLTree(Leaf(N nt)) :: restStack
            | LLTree(Node(nt, _)) :: restStack ->
                let la = lookahead tokens pos k

                match expandNonterminal nt restStack la with
                | Some newStack -> parseLoop newStack pos completed
                | None -> None

            | LLMarker(nt, n) :: restStack ->
                let revCompleted = List.rev completed
                let children = revCompleted |> List.take n |> List.rev
                let restCompleted = revCompleted |> List.skip n |> List.rev
                let newNode = Node(nt, children)
                parseLoop restStack pos (restCompleted @ [ newNode ])

        match parseLoop ([ LLTree(Leaf(N g.start)) ]) 0 [] with
        | Some(finalPos, completedTrees) when finalPos = tokens.Length ->
            let tree = List.tryLast completedTrees
            tree, List.rev steps
        | _ -> None, List.rev steps

    /// Parse pre-tokenized input using an LL(k) parsing table, building a derivation tree.
    let parse
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (terminals: Terminal<'t> list)
        : Option<DerivationTree<'t, 'nt>> =
        parseWithSteps g table k terminals |> fst
