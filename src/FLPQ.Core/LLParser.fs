namespace FLPQ.Core

/// Derivation tree node for parsing.
type DerivationTree<'t, 'nt> =
    | Leaf of Terminal<'t>
    | Epsilon
    | Node of Nonterminal<'nt> * DerivationTree<'t, 'nt> list

module LLParser =

    /// Build an LL(k) parsing table.
    /// Returns Map from (nonterminal, lookahead string) to rule index.
    let buildTable (g: Grammar<string, string>) (k: int) : Map<Nonterminal<string> * string, int> =
        let firstMap = FirstFollow.firstK g k
        let followMap = FirstFollow.followK g k

        let mutable table = Map.empty

        for ruleIdx in 0 .. g.rules.Length - 1 do
            let rule = g.rules.[ruleIdx]

            let lookahead =
                if rule.rhs.IsEmpty then
                    followMap |> Map.find rule.lhs
                else
                    let firstOfRhs = FirstFollow.firstKOfString firstMap k rule.rhs

                    let withoutEps = Set.remove "" firstOfRhs
                    let followOfA = followMap |> Map.find rule.lhs

                    if Set.contains "" firstOfRhs then
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
                                "LL(%d) conflict: %A with lookahead %s has rules %d and %d"
                                k
                                rule.lhs
                                w
                                existing
                                ruleIdx
                    | None -> table <- Map.add key ruleIdx table

        table

    let private tokenize (s: string) : string list =
        s.ToCharArray() |> Array.map (fun c -> c.ToString()) |> Array.toList

    let private lookaheadStr (input: string list) (pos: int) (k: int) : string =
        let mutable result = ""

        for i in pos .. min (pos + k - 1) (input.Length - 1) do
            result <- result + input.[i]

        result

    /// Parse a string using an LL(k) parsing table, building a derivation tree.
    /// Returns Some(tree) on success, None on failure.
    let parse
        (g: Grammar<string, string>)
        (table: Map<Nonterminal<string> * string, int>)
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
                if pos < tokens.Length && tokens.[pos] = t then
                    parseLoop restStack (pos + 1) (treeStack @ [ Leaf(Terminal t) ])
                else
                    None
            | N nt :: restStack ->
                let la = lookaheadStr tokens pos k
                let key = (nt, la)

                match Map.tryFind key table with
                | Some ruleIdx ->
                    let rule = g.rules.[ruleIdx]

                    if rule.rhs.IsEmpty then
                        parseLoop restStack pos (treeStack @ [ Epsilon ])
                    else
                        let newStack = rule.rhs @ restStack
                        parseLoop newStack pos treeStack
                | None -> None

        match parseLoop ([ N g.start ]) 0 [] with
        | Some(finalPos, leafTrees) when finalPos = tokens.Length -> Some(Node(g.start, leafTrees))
        | _ -> None

    /// Collect all leaf terminals from a derivation tree (left-to-right).
    let rec leaves (tree: DerivationTree<string, string>) : string list =
        match tree with
        | Leaf(Terminal t) -> [ t ]
        | Epsilon -> []
        | Node(_, children) -> children |> List.collect leaves
