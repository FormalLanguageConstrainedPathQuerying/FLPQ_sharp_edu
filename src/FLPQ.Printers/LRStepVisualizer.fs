namespace FLPQ.Printers

open FLPQ.Languages

/// LR parser step-by-step visualization.
module LRStepVisualizer =

    /// Run the LR parser and produce step-by-step visualization.
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : VisualizationStep list =
        let _, steps = LRParser.parseWithSteps aug table terminals

        steps
        |> List.map (fun step ->
            let stackTrees =
                step.stack
                |> List.choose (function
                    | LRSymbol tree -> Some tree
                    | _ -> None)

            { treeAndStack = DerivationTreeDot.toDotWithStack symbolVisualizer step.tree stackTrees
              input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position })
