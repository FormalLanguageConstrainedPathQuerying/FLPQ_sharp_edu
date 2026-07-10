namespace FLPQ.Printers

/// Visualization step shared by LL and LR parser visualizers.
/// TreeAndStack is a single combined DOT graph (tree with stack chain overlay).
/// Input is a TeX one-row pNiceMatrix with the current position underlined.
[<Struct>]
type VisualizationStep = { TreeAndStack: string; Input: string }
