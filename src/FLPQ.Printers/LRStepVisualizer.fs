namespace FLPQ.Printers

open FLPQ.Languages

/// LR parser step-by-step visualization.
module LRStepVisualizer =

    /// Run the LR parser and produce step-by-step visualization.
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (tokens: Symbol<'t, 'nt> list)
        : VisualizationStep list =
        let _, steps = LRParser.parseWithSteps aug table tokens

        steps
        |> List.map (fun step ->
            let stateNums =
                step.stack
                |> List.choose (function
                    | LRState n -> Some n
                    | _ -> None)

            { tree = DerivationTreeDot.toDot symbolVisualizer step.tree
              stack = TeXRenderer.oneRowMatrix string stateNums
              input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position })
