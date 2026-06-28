namespace FLPQ.Languages

module LLParser =

    /// Build an LL(k) parsing table.
    /// Returns Map from (nonterminal, lookahead) to rule index.
    /// Lookahead is a list of grammar symbols (terminals or epsilon for end-of-input).
    let buildTable (g: Grammar<string, string>) (k: int) : Map<Nonterminal<string> * Symbol<string, string> list, int> =
        let firstMap = FirstFollow.firstK g k
        let followMap = FirstFollow.followK g k

        let mutable table = Map.empty

        for ruleIdx in 0 .. g.rules.Length - 1 do
            let rule = g.rules.[ruleIdx]

            let lookahead =
                if rule.rhs = [ Epsilon ] then
                    followMap |> Map.find rule.lhs
                else
                    let firstOfRhs = FirstFollow.firstKOfString firstMap k rule.rhs

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

    let private tokenize (s: string) : Symbol<string, string> list = Tokenizer.tokenize s

    let private lookahead (input: Symbol<string, string> list) (pos: int) (k: int) : Symbol<string, string> list =
        if pos >= input.Length then
            [ Epsilon ]
        else
            let endIdx = min (pos + k) input.Length
            input.[pos .. endIdx - 1]

    /// Parse a string using an LL(k) parsing table, building a derivation tree.
    /// Returns Some(tree) on success, None on failure.
    let parse
        (g: Grammar<string, string>)
        (table: Map<Nonterminal<string> * Symbol<string, string> list, int>)
        (k: int)
        (input: string)
        : Option<DerivationTree<string, string>> =
        let tokens = tokenize input

        let rec parseLoop
            (stack: Symbol<string, string> list)
            (pos: int)
            (treeStack: DerivationTree<string, string> list)
            : Option<int * DerivationTree<string, string> list> =
            match stack with
            | [] -> if pos = tokens.Length then Some(pos, treeStack) else None
            | T(Terminal t) :: restStack ->
                if pos < tokens.Length && tokens.[pos] = T(Terminal t) then
                    parseLoop restStack (pos + 1) (treeStack @ [ Leaf(T(Terminal t)) ])
                else
                    None
            | Epsilon :: restStack -> parseLoop restStack pos (treeStack @ [ Leaf(Epsilon) ])
            | N nt :: restStack ->
                let la = lookahead tokens pos k
                let key = (nt, la)

                match Map.tryFind key table with
                | Some ruleIdx ->
                    let rule = g.rules.[ruleIdx]

                    let newStack = rule.rhs @ restStack
                    parseLoop newStack pos treeStack
                | None -> None

        match parseLoop ([ N g.start ]) 0 [] with
        | Some(finalPos, leafTrees) when finalPos = tokens.Length -> Some(Node(g.start, leafTrees))
        | _ -> None
