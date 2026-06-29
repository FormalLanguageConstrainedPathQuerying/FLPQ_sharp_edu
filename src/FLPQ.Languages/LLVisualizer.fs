namespace FLPQ.Languages

/// LL parser step-by-step visualization.
module LLVisualizer =

    /// Run the LL parser and produce step-by-step visualization.
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (g: Grammar<'t, 'nt>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        (k: int)
        (tokens: Symbol<'t, 'nt> list)
        : VisualizationStep list =
        let _, steps = LLParser.parseWithSteps g table k tokens

        steps
        |> List.map (fun step ->
            { tree = DerivationTreeVisualizer.toDot symbolVisualizer step.tree
              stack = TeXRenderer.oneRowMatrix symbolVisualizer step.stack
              input = TeXRenderer.inputRow symbolVisualizer step.input.tokens step.input.position })
