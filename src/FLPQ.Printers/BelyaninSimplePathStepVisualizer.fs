namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.BelyaninRPQ
open BelyaninStepCommon

/// Step-by-step visualization of Belyanin's RPQ evaluation over the path semiring (the
/// simple-path semantics; book: Chapter 11, 02_BFS.tex, algo:RPQ_BFS_semiring). Each step
/// renders the state at the start of the step (frontier F, visited V = P \ F, the query DFA
/// with the frontier states highlighted, and the input graph with green sources, lightblue
/// frontier vertices, and lightred frontier path edges), one pair of rows per active label
/// with the explicit propagation products (N^a)^T ⊗ F = F^a and F^a ⊗ G^a = Extend plus the
/// used transitions / followed edges highlighted red bold, and the state at the end of the
/// step (F, V). F and V are separate artifacts (the step template places them side by side);
/// each row is one substep. The Boolean (reachability) variant lives in
/// `BelyaninReachabilityStepVisualizer`.
module BelyaninSimplePathStepVisualizer =

    /// The first line of a label's propagation — the book's per-label term (N^a)^T ⊗ M ⊗ G^a
    /// split at F^a: (N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <F^a>, rendered as one horizontal line
    /// (all three matrices side by side in one math expression, wrapped once in an adjustbox).
    let private selectFormula
        (terminalPrinter: 't -> string)
        (f: Matrix<Set<int list>>)
        (ls: BelyaninLabelStep<'t>)
        : string =
        let l = labelToTeX terminalPrinter ls.Label

        selectFormulaWithBodies
            l
            (PathSemiringTeX.boolMatrixToTeXBody stateLabel stateLabel (Matrix.transpose ls.N))
            (PathSemiringTeX.matrixWithStateVertexLabelsBody f)
            (PathSemiringTeX.matrixWithStateVertexLabelsBody ls.Select)

    /// The second line of a label's propagation: F^a ⊗ G^a = <F^a> ⊗ <G^a> = <Extend>, also
    /// rendered as one horizontal line.
    let private extendFormula (terminalPrinter: 't -> string) (ls: BelyaninLabelStep<'t>) : string =
        let l = labelToTeX terminalPrinter ls.Label

        extendFormulaWithBodies
            l
            (PathSemiringTeX.matrixWithStateVertexLabelsBody ls.Select)
            (PathSemiringTeX.boolMatrixToTeXBody vertexLabel vertexLabel ls.G)
            (PathSemiringTeX.matrixWithStateVertexLabelsBody ls.Extend)

    /// The start-of-step frontier tile: F = M (the current frontier), top-aligned like every
    /// other step tile.
    let private renderFrontierStart (step: BelyaninTraceStep<'t>) : string =
        matrixTileWithBody @"\text{F}" (PathSemiringTeX.matrixWithStateVertexLabelsBody step.M)

    /// The start-of-step visited tile: V = P \ F (cell-wise set difference). V is the exact
    /// inverse of the trace's P <- P + F, so no trace change is needed.
    let private renderVisitedStart (step: BelyaninTraceStep<'t>) : string =
        let vBefore = Matrix.map2 Set.difference step.P step.M

        matrixTileWithBody @"\text{V}" (PathSemiringTeX.matrixWithStateVertexLabelsBody vBefore)

    /// The end-of-step frontier tile: F = NewM (the next frontier).
    let private renderFrontierEnd (step: BelyaninTraceStep<'t>) : string =
        matrixTileWithBody @"\text{F}" (PathSemiringTeX.matrixWithStateVertexLabelsBody step.NewM)

    /// The end-of-step visited tile: V = P (everything visited after the step).
    let private renderVisitedEnd (step: BelyaninTraceStep<'t>) : string =
        matrixTileWithBody @"\text{V}" (PathSemiringTeX.matrixWithStateVertexLabelsBody step.P)

    /// The query DFA with the frontier states of a path matrix highlighted (no target states,
    /// no edge highlights). Returns (dot, tikz).
    let private automatonWithFrontier
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (m: Matrix<Set<int list>>)
        : string * string =
        dfaFigures terminalPrinter dfa (frontierStates PathSemiring.isZero m) Set.empty Set.empty

    /// The input graph at a step boundary: green sources, lightblue frontier vertices of the
    /// path matrix, lightred edges of its paths; no red edges, no yellow vertices. Returns
    /// (dot, tikz).
    let private boundaryGraph
        (terminalPrinter: 't -> string)
        (graph: NFA<'t, int>)
        (m: Matrix<Set<int list>>)
        : string * string =
        RpqGraphViz.renderGraph
            terminalPrinter
            graph
            Set.empty
            graph.StartStates
            (RpqGraphViz.frontierVertices m)
            Set.empty
            (RpqGraphViz.pathEdges m)

    /// One label's propagation row: the two formulas and the automaton figure with the used
    /// a-transitions red bold and the step's target states (the states selected into F^a)
    /// lightyellow, and the graph figure with green sources, red followed edges, lightblue
    /// current vertices (the non-empty Select columns), lightyellow targets, and F's path edges
    /// lightred.
    let private renderLabel
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        (step: BelyaninTraceStep<'t>)
        (ls: BelyaninLabelStep<'t>)
        : BelyaninLabelVisual =
        let used = usedAutoEdges PathSemiring.isZero step.M ls.N
        let automatonTargets = frontierStates PathSemiring.isZero ls.Select

        let automatonDot, automatonTikz =
            dfaFigures terminalPrinter dfa Set.empty automatonTargets used

        let followed = followedEdges PathSemiring.isZero ls.G ls.Select

        // The current vertices are the vertices where F^a is selected (the non-empty columns of
        // Select), not just the sources of the followed edges: a selected vertex with no outgoing
        // a-edge must still be highlighted as current (the followed sources are a subset).
        let currentVertices = RpqGraphViz.frontierVertices ls.Select
        let targets = RpqGraphViz.frontierVertices ls.Extend

        let graphDot, graphTikz =
            RpqGraphViz.renderGraph
                terminalPrinter
                graph
                targets
                graph.StartStates
                currentVertices
                followed
                (RpqGraphViz.pathEdges step.M)

        { SelectFormula = selectFormula terminalPrinter step.M ls
          ExtendFormula = extendFormula terminalPrinter ls
          AutomatonDot = automatonDot
          AutomatonTikz = automatonTikz
          GraphDot = graphDot
          GraphTikz = graphTikz }

    /// Render every trace step to its visualization artifacts (both DOT and TikZ; the runner
    /// writes only one format per step).
    let renderSteps
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        (steps: BelyaninTraceStep<'t> list)
        : BelyaninVisualizationStep list =
        renderVisualizationSteps
            (fun (step: BelyaninTraceStep<'t>) -> step.IsInit)
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
