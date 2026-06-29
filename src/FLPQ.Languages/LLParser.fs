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
    /// Also collects visualization steps when withSteps is true.
    let parseWithSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (tokens: Symbol<'t, 'nt> list)
        : Option<DerivationTree<'t, 'nt>> * VisualizationStep list =

        let mutable steps = []

        let recordStep (stack: Symbol<'t, 'nt> list) (pos: int) (treeStack: DerivationTree<'t, 'nt> list) =
            let currentTree =
                match treeStack with
                | [ t ] -> t
                | [] -> Leaf(Epsilon)
                | _ -> Node(g.start, treeStack)

            let stackTeX =
                let cells =
                    stack
                    |> List.map (fun sym ->
                        match sym with
                        | T(Terminal _) -> string sym
                        | N(Nonterminal _) -> string sym
                        | Epsilon -> "\\varepsilon")
                    |> String.concat " & "

                @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"

            let inputTeX =
                let cells =
                    tokens
                    |> List.mapi (fun i sym ->
                        let s =
                            match sym with
                            | T(Terminal _) -> string sym
                            | N _ -> string sym
                            | Epsilon -> "\\varepsilon"

                        if i = pos then @"\underbar{" + s + "}" else s)
                    |> String.concat " & "

                @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"

            steps <-
                { tree = DerivationTreeVisualizer.toDot symbolVisualizer currentTree
                  stack = stackTeX
                  input = inputTeX }
                :: steps

        let rec parseLoop
            (stack: Symbol<'t, 'nt> list)
            (pos: int)
            (treeStack: DerivationTree<'t, 'nt> list)
            : Option<int * DerivationTree<'t, 'nt> list> =
            recordStep stack pos treeStack

            match stack with
            | [] -> if pos = tokens.Length then Some(pos, treeStack) else None
            | (T _ as sym) :: restStack ->
                if pos < tokens.Length && tokens.[pos] = sym then
                    parseLoop restStack (pos + 1) (treeStack @ [ Leaf(sym) ])
                else
                    None
            | Epsilon :: restStack -> parseLoop restStack pos (treeStack @ [ Leaf(Epsilon) ])
            | N nt :: restStack ->
                let la = lookahead tokens pos k
                let key = (nt, la)

                match Map.tryFind key table with
                | Some ruleIdx ->
                    let rule = g.rules.[ruleIdx]

                    let newStack = Rhs.toList rule.rhs @ restStack
                    parseLoop newStack pos treeStack
                | None -> None

        match parseLoop ([ N g.start ]) 0 [] with
        | Some(finalPos, leafTrees) when finalPos = tokens.Length -> Some(Node(g.start, leafTrees)), List.rev steps
        | _ -> None, List.rev steps

    /// Parse pre-tokenized input using an LL(k) parsing table, building a derivation tree.
    let parse
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (tokens: Symbol<'t, 'nt> list)
        : Option<DerivationTree<'t, 'nt>> =
        parseWithSteps string g table k tokens |> fst
