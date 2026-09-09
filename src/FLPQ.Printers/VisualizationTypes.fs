namespace FLPQ.Printers

/// Visualization step shared by LL and LR parser visualizers.
/// TreeAndStack is a single combined DOT graph (tree with stack chain overlay).
/// TreeAndStackTikz is the same combined picture as a TikZ graphdrawing block
/// (layered layout with a same-layer constraint on the stack frontier).
/// Input is a TeX one-row pNiceMatrix with the current position underlined.
[<Struct>]
type VisualizationStep =
    { TreeAndStack: string
      TreeAndStackTikz: string
      Input: string }
