namespace FLPQ.Languages

/// LR parser step-by-step visualization.
module LRVisualizer =

    /// Run the LR parser and produce step-by-step visualization.
    let visualizeSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        (tokens: Symbol<'t, 'nt> list)
        : VisualizationStep list =
        LRParser.parseWithSteps symbolVisualizer aug table tokens |> snd
