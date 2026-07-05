namespace FLPQ.Printers

/// Visualization step shared by LL and LR parser visualizers.
/// treeAndStack is a single combined DOT graph (tree with stack chain overlay).
/// input is a TeX one-row pNiceMatrix with the current position underlined.
[<Struct>]
type VisualizationStep = { treeAndStack: string; input: string }
