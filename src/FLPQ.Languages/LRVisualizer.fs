namespace FLPQ.Languages

/// LR parser step-by-step visualization.
module LRVisualizer =

    let private stackToTeX (stateStack: int list) : string =
        let cells = stateStack |> List.rev |> List.map string |> String.concat " & "

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

    /// Run the LR parser and produce step-by-step visualization.
    /// Returns a list of LRStep structs recording tree (dot), stack (TeX), and input (TeX).
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (tokens: Symbol<'t, 'nt> list)
        : VisualizationStep list =
        let mutable steps = []
        let mutable stateStack: int list = [ 0 ]
        let mutable treeStack: DerivationTree<'t, 'nt> list = []
        let mutable pos = 0
        let mutable finished = false
        let mutable stepsTaken = 0

        let recordStep () =
            let currentTree = treeFromStack aug.start treeStack

            steps <-
                { tree = DerivationTreeVisualizer.toDot symbolVisualizer currentTree
                  stack = stackToTeX stateStack
                  input = inputToTeX tokens pos }
                :: steps

        recordStep ()

        while not finished && stepsTaken < 10000 do
            stepsTaken <- stepsTaken + 1
            let currentState = stateStack.Head

            let lookahead = if pos < tokens.Length then tokens.[pos] else Epsilon

            match Map.tryFind (currentState, lookahead) table.action with
            | Some(Shift nextState) ->
                stateStack <- nextState :: stateStack
                treeStack <- Leaf(tokens.[pos]) :: treeStack
                pos <- pos + 1
                recordStep ()
            | Some(Reduce ruleIdx) ->
                let rule = aug.rules.[ruleIdx]
                let popCount = rule.rhs |> List.filter (fun s -> s <> Epsilon) |> List.length

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
                recordStep ()
            | Some Accept -> finished <- true
            | None ->
                if pos = tokens.Length && lookahead = Epsilon then
                    match Map.tryFind (currentState, Epsilon) table.action with
                    | Some Accept -> finished <- true
                    | _ -> finished <- true
                else
                    finished <- true

        steps |> List.rev
