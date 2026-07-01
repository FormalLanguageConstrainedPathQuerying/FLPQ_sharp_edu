namespace FLPQ.Printers

open FLPQ.Languages

/// LL parser step-by-step visualization.
module LLStepVisualizer =

    /// Run the LL parser and produce step-by-step visualization.
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (terminals: Terminal<'t> list)
        : VisualizationStep list =
        let _, steps = LLParser.parseWithSteps g table k terminals

        steps
        |> List.map (fun step ->
            let stackTrees = step.stack |> List.map LLStackFrame.tree

            { treeAndStack = DerivationTreeDot.toDotWithStack symbolVisualizer step.tree stackTrees
              input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position })
