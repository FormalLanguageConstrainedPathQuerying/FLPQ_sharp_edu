namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.BelyaninRPQ
open BelyaninStepCommon

/// Step-by-step visualization of Belyanin's RPQ evaluation over the Boolean semiring (the
/// reachability semantics). No path information is collected, so the matrices are Boolean
/// (reachable / not reachable) and no path edges are drawn. Each step renders the
/// start-of-step state (frontier F, visited V = P \ F, the query DFA with the current
/// frontier states highlighted, and the input graph with green sources and lightblue
/// current vertices), one pair of rows per active label with the explicit Boolean products
/// (N^a)^T ⊗ F = F^a and F^a ⊗ G^a = Extend plus the used transitions / followed edges
/// highlighted red bold and the target states / vertices lightyellow, and the end-of-step
/// state (F, V). F and V are separate artifacts; each row is one substep. The path-based
/// variant lives in `BelyaninSimplePathStepVisualizer`.
module BelyaninReachabilityStepVisualizer =

    /// Whether a Boolean cell is the additive zero (false).
    let private isZero (b: bool) : bool = not b

    /// The Boolean body of a `q_i`/`v_j` labeled matrix.
    let private booleanBody (m: Matrix<bool>) : string =
        PathSemiringTeX.boolMatrixToTeXBody stateLabel vertexLabel m

    /// The start-of-step frontier tile: F = M (the current frontier).
    let private renderFrontierStart (step: BelyaninReachabilityTraceStep<'t>) : string =
        matrixTileWithBody false @"\text{F}" (booleanBody step.M)

    /// The start-of-step visited tile: V = P \ F, i.e. P ∧ ¬M cell-wise.
    let private renderVisitedStart (step: BelyaninReachabilityTraceStep<'t>) : string =
        matrixTileWithBody true @"\text{V}" (booleanBody (Matrix.map2 (fun pv mv -> pv && not mv) step.P step.M))

    /// The end-of-step frontier tile: F = NewM (the next frontier).
    let private renderFrontierEnd (step: BelyaninReachabilityTraceStep<'t>) : string =
        matrixTileWithBody true @"\text{F}" (booleanBody step.NewM)

    /// The end-of-step visited tile: V = P (everything visited after the step).
    let private renderVisitedEnd (step: BelyaninReachabilityTraceStep<'t>) : string =
        matrixTileWithBody true @"\text{V}" (booleanBody step.P)

    /// The first line of a label's propagation: (N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <F^a>, one
    /// horizontal line of Boolean matrices.
    let private selectFormula
        (terminalPrinter: 't -> string)
        (f: Matrix<bool>)
        (ls: BelyaninReachabilityLabelStep<'t>)
        : string =
        selectFormulaWithBodies
            (labelToTeX terminalPrinter ls.Label)
            (PathSemiringTeX.boolMatrixToTeXBody stateLabel stateLabel (Matrix.transpose ls.N))
            (booleanBody f)
            (booleanBody ls.Select)

    /// The second line of a label's propagation: F^a ⊗ G^a = <F^a> ⊗ <G^a> = <Extend>.
    let private extendFormula (terminalPrinter: 't -> string) (ls: BelyaninReachabilityLabelStep<'t>) : string =
        extendFormulaWithBodies
            (labelToTeX terminalPrinter ls.Label)
            (booleanBody ls.Select)
            (PathSemiringTeX.boolMatrixToTeXBody vertexLabel vertexLabel ls.G)
            (booleanBody ls.Extend)

    /// Graph vertices at which the Boolean frontier has a non-empty column.
    let private frontierVertices (m: Matrix<bool>) : Set<int> =
        Set.ofList
            [ for v in 0 .. Matrix.cols m - 1 do
                  if colNonEmpty isZero m v then
                      v ]

    /// The query DFA with the frontier states of a Boolean matrix highlighted (no target
    /// states, no edge highlights). Returns (dot, tikz).
    let private automatonWithFrontier
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (m: Matrix<bool>)
        : string * string =
        dfaFigures terminalPrinter dfa (frontierStates isZero m) Set.empty Set.empty

    /// The input graph at a step boundary: green sources, lightblue frontier vertices of the
    /// Boolean matrix; no red edges, no yellow vertices, no path edges. Returns (dot, tikz).
    let private boundaryGraph
        (terminalPrinter: 't -> string)
        (graph: NFA<'t, int>)
        (m: Matrix<bool>)
        : string * string =
        RpqGraphViz.renderGraph
            terminalPrinter
            graph
            Set.empty
            graph.StartStates
            (frontierVertices m)
            Set.empty
            Set.empty

    /// One label's propagation row: the two formulas and the automaton figure with the
    /// current frontier states lightblue, the target states (selected into F^a) lightyellow,
    /// and the used a-transitions red bold; the graph figure with green sources, lightblue
    /// from-endpoints, lightyellow targets, and the followed edges red bold. No path edges
    /// are drawn (the reachability trace collects no path information).
    let private renderLabel
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        (step: BelyaninReachabilityTraceStep<'t>)
        (ls: BelyaninReachabilityLabelStep<'t>)
        : BelyaninLabelVisual =
        let used = usedAutoEdges isZero step.M ls.N
        let automatonFrontier = frontierStates isZero step.M
        let automatonTargets = frontierStates isZero ls.Select

        let automatonDot, automatonTikz =
            dfaFigures terminalPrinter dfa automatonFrontier automatonTargets used

        let followed = followedEdges isZero ls.G ls.Select
        let fromEndpoints = followed |> Set.fold (fun acc (u, _) -> Set.add u acc) Set.empty
        let targets = frontierVertices ls.Extend

        let graphDot, graphTikz =
            RpqGraphViz.renderGraph terminalPrinter graph targets graph.StartStates fromEndpoints followed Set.empty

        { SelectFormula = selectFormula terminalPrinter step.M ls
          ExtendFormula = extendFormula terminalPrinter ls
          AutomatonDot = automatonDot
          AutomatonTikz = automatonTikz
          GraphDot = graphDot
          GraphTikz = graphTikz }

    /// Render every Boolean reachability trace step to its visualization artifacts (both DOT
    /// and TikZ; the runner writes only one format per step).
    let renderSteps
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        (steps: BelyaninReachabilityTraceStep<'t> list)
        : BelyaninVisualizationStep list =
        renderVisualizationSteps
            (fun (step: BelyaninReachabilityTraceStep<'t>) -> step.IsInit)
            (fun step ->
                boundaryVisual
                    (renderFrontierStart step)
                    (renderVisitedStart step)
                    (automatonWithFrontier terminalPrinter dfa step.M)
                    (boundaryGraph terminalPrinter graph step.M))
            (fun step -> step.Labels |> List.map (renderLabel terminalPrinter dfa graph step))
            (fun step ->
                boundaryVisual
                    (renderFrontierEnd step)
                    (renderVisitedEnd step)
                    (automatonWithFrontier terminalPrinter dfa step.NewM)
                    (boundaryGraph terminalPrinter graph step.NewM))
            steps
