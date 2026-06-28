namespace FLPQ.Languages

/// LL parser step-by-step visualization.
module LLVisualizer =

    let private stackToTeX (stack: Symbol<'t, 'nt> list) : string =
        let cells =
            stack
            |> List.map (fun sym ->
                match sym with
                | T(Terminal _) -> string sym
                | N(Nonterminal _) -> string sym
                | Epsilon -> "\\varepsilon")
            |> String.concat " & "

        @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"

    let private inputToTeX (tokens: Symbol<'t, 'nt> list) (pos: int) : string =
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

    let private treeFromStack
        (start: Nonterminal<'nt>)
        (treeStack: DerivationTree<'t, 'nt> list)
        : DerivationTree<'t, 'nt> =
        match treeStack with
        | [ t ] -> t
        | [] -> Leaf(Epsilon)
        | _ -> Node(start, treeStack)

    /// Run the LL parser and produce step-by-step visualization.
    /// Returns a list of LLStep structs recording tree (dot), stack (TeX), and input (TeX).
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (tokens: Symbol<'t, 'nt> list)
        : VisualizationStep list =
        let mutable steps = []
        let mutable accepted = false

        let rec step (stack: Symbol<'t, 'nt> list) (pos: int) (treeStack: DerivationTree<'t, 'nt> list) : unit =
            let currentTree = treeFromStack g.start treeStack

            steps <-
                { tree = DerivationTreeVisualizer.toDot symbolVisualizer currentTree
                  stack = stackToTeX stack
                  input = inputToTeX tokens pos }
                :: steps

            match stack with
            | [] ->
                if pos = tokens.Length then
                    accepted <- true

            | (T _ as sym) :: restStack ->
                if pos < tokens.Length && tokens.[pos] = sym then
                    step restStack (pos + 1) (treeStack @ [ Leaf(sym) ])

            | Epsilon :: restStack -> step restStack pos (treeStack @ [ Leaf(Epsilon) ])

            | N nt :: restStack ->
                let la =
                    if pos >= tokens.Length then
                        [ Epsilon ]
                    else
                        let endIdx = min (pos + k) tokens.Length
                        tokens.[pos .. endIdx - 1]

                match Map.tryFind (nt, la) table with
                | Some ruleIdx ->
                    let rule = g.rules.[ruleIdx]

                    let newStack = rule.rhs @ restStack
                    step newStack pos treeStack
                | None -> ()

        step ([ N g.start ]) 0 []

        steps |> List.rev
