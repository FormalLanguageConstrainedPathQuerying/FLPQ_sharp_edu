namespace FLPQ.Languages

/// Visualization step shared by LL and LR parser visualizers.
[<Struct>]
type VisualizationStep =
    { tree: string
      stack: string
      input: string }
