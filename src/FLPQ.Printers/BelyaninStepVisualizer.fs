namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.BelyaninRPQ

/// Step-by-step visualization of Belyanin's RPQ evaluation (book: Chapter 11, 02_BFS.tex,
/// algo:RPQ_BFS_semiring). Each step renders the state at the start of the step (frontier F,
/// visited V = P \ F, the query DFA with the frontier states highlighted, and the input graph
/// with green sources, lightblue frontier vertices, and lightred frontier path edges), one row
/// per active label with the explicit propagation products (N^a)^T ⊗ F = F^a and
/// F^a ⊗ G^a = Extend plus the used transitions / followed edges highlighted red bold, and the
/// state at the end of the step (New F, V).
module BelyaninStepVisualizer =

    /// One label's propagation row: the two explicit products as TeX and the automaton/graph
    /// figures with the label's used transitions / followed edges highlighted — each in DOT
    /// and TikZ form (the runner picks one).
    type BelyaninLabelVisual =
        { SelectFormula: string
          ExtendFormula: string
          AutomatonDot: string
          AutomatonTikz: string
          GraphDot: string
          GraphTikz: string }

    /// The state at a step boundary: the F/V matrices (F/V at the start, New F/V at the end)
    /// and the automaton/graph figures with that boundary's frontier highlighted — each figure
    /// in DOT and TikZ form (the runner picks one).
    type BelyaninBoundaryVisual =
        { Matrices: string
          AutomatonDot: string
          AutomatonTikz: string
          GraphDot: string
          GraphTikz: string }

    /// One rendered trace step: the start-of-step state, the per-label propagation rows in
    /// trace order, and the end-of-step state. The init step has no end state (End = None)
    /// and no label rows.
    type BelyaninVisualizationStep =
        { Start: BelyaninBoundaryVisual
          Labels: BelyaninLabelVisual list
          End: BelyaninBoundaryVisual option }

    /// A label as a TeX math symbol: terminals escaped, epsilon as \varepsilon.
    let private labelToTeX (terminalPrinter: 't -> string) (label: AutomatonLabel<'t>) : string =
        match label with
        | ATerm t -> AutomatonTikz.escapeLatex (terminalPrinter t)
        | AEpsilon -> @"\varepsilon"

    /// Automaton state label printer: q_i.
    let private stateLabel (i: int) : string = sprintf "q_%d" i

    /// Graph vertex label printer: v_i.
    let private vertexLabel (j: int) : string = sprintf "v_%d" j

    /// True when the row of a path semiring matrix holds at least one path.
    let private rowNonEmpty (m: Matrix<Set<int list>>) (i: int) : bool =
        [ for j in 0 .. Matrix.cols m - 1 do
              not (PathSemiring.isZero m.[i, j]) ]
        |> List.exists id

    /// The automaton states of the current frontier: q with a non-empty M[q, *] cell.
    let private frontierStates (m: Matrix<Set<int list>>) : Set<int> =
        Set.ofList
            [ for q in 0 .. Matrix.rows m - 1 do
                  if rowNonEmpty m q then
                      q ]

    /// True when the column of a path semiring matrix holds at least one path.
    let private colNonEmpty (m: Matrix<Set<int list>>) (j: int) : bool =
        [ for i in 0 .. Matrix.rows m - 1 do
              not (PathSemiring.isZero m.[i, j]) ]
        |> List.exists id

    /// The DFA transitions used by the label's backward step: q -> q' when N^a[q, q'] and
    /// the frontier holds a path at q.
    let private usedAutoEdges (m: Matrix<Set<int list>>) (n: Matrix<bool>) : Set<int * int> =
        Set.ofList
            [ for q in 0 .. Matrix.rows n - 1 do
                  if rowNonEmpty m q then
                      for qp in 0 .. Matrix.cols n - 1 do
                          if n.[q, qp] then
                              (q, qp) ]

    /// The graph edges followed by the label's forward step: u -> v when G^a[u, v] and the
    /// selected matrix holds a path at u.
    let private followedEdges (g: Matrix<bool>) (select: Matrix<Set<int list>>) : Set<int * int> =
        Set.ofList
            [ for u in 0 .. Matrix.rows g - 1 do
                  if colNonEmpty select u then
                      for v in 0 .. Matrix.cols g - 1 do
                          if g.[u, v] then
                              (u, v) ]

    /// The first line of a label's propagation — the book's per-label term (N^a)^T ⊗ M ⊗ G^a
    /// split at F^a: (N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <F^a>.
    let private selectFormula
        (terminalPrinter: 't -> string)
        (f: Matrix<Set<int list>>)
        (ls: BelyaninLabelStep<'t>)
        : string =
        let l = labelToTeX terminalPrinter ls.Label

        sprintf @"$(N^{%s})^T \otimes F =$" l
        + "\n"
        + PathSemiringTeX.boolMatrixToTeX stateLabel stateLabel (Matrix.transpose ls.N)
        + "\n"
        + @"$\otimes$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels f
        + "\n"
        + @"$=$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels ls.Select

    /// The second line of a label's propagation: F^a ⊗ G^a = <F^a> ⊗ <G^a> = <Extend>.
    let private extendFormula (terminalPrinter: 't -> string) (ls: BelyaninLabelStep<'t>) : string =
        let l = labelToTeX terminalPrinter ls.Label

        sprintf @"$F^{%s} \otimes G^{%s} =$" l l
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels ls.Select
        + "\n"
        + @"$\otimes$"
        + "\n"
        + PathSemiringTeX.boolMatrixToTeX vertexLabel vertexLabel ls.G
        + "\n"
        + @"$=$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels ls.Extend

    /// The start-of-step matrices as a vertical stack: F (frontier) and V (visited before
    /// the step). V = P \ F is the exact inverse of the trace's P <- P + F, so no trace
    /// change is needed.
    let private renderMatricesStart (step: BelyaninTraceStep<'t>) : string =
        let vBefore = Matrix.map2 Set.difference step.P step.M

        @"$\text{F}$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels step.M
        + "\n"
        + @"$\text{V}$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels vBefore

    /// The end-of-step matrices as a vertical stack: New F (next frontier) and V (visited).
    let private renderMatricesEnd (step: BelyaninTraceStep<'t>) : string =
        @"$\text{New } F =$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels step.NewM
        + "\n"
        + @"$\text{V}$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels step.P

    /// The query DFA as (dot, tikz) figures with the given states and transitions
    /// highlighted.
    let private dfaFigures
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (highlightedStates: Set<int>)
        (highlightedEdges: Set<int * int>)
        : string * string =
        let dot =
            AutomatonDot.dfaToDotWithHighlights
                terminalPrinter
                (fun idx _ -> sprintf "q_%d" idx)
                dfa
                highlightedStates
                highlightedEdges

        let tikz =
            AutomatonTikz.dfaToTikzWithHighlights
                terminalPrinter
                (fun idx _ -> sprintf "$q_%d$" idx)
                "circle"
                dfa
                highlightedStates
                highlightedEdges

        (dot, tikz)

    /// The query DFA with the frontier states of a path matrix highlighted (no edge
    /// highlights). Returns (dot, tikz).
    let private automatonWithFrontier
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (m: Matrix<Set<int list>>)
        : string * string =
        dfaFigures terminalPrinter dfa (frontierStates m) Set.empty

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
    /// a-transitions red bold (no state highlights) and the graph figure with green sources,
    /// red followed edges, lightblue from-endpoints, lightyellow targets, and F's path edges
    /// lightred.
    let private renderLabel
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        (step: BelyaninTraceStep<'t>)
        (ls: BelyaninLabelStep<'t>)
        : BelyaninLabelVisual =
        let used = usedAutoEdges step.M ls.N
        let automatonDot, automatonTikz = dfaFigures terminalPrinter dfa Set.empty used

        let followed = followedEdges ls.G ls.Select
        let fromEndpoints = followed |> Set.fold (fun acc (u, _) -> Set.add u acc) Set.empty
        let targets = RpqGraphViz.frontierVertices ls.Extend

        let graphDot, graphTikz =
            RpqGraphViz.renderGraph
                terminalPrinter
                graph
                targets
                graph.StartStates
                fromEndpoints
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
        steps
        |> List.map (fun step ->
            let automatonStartDot, automatonStartTikz =
                automatonWithFrontier terminalPrinter dfa step.M

            let graphStartDot, graphStartTikz = boundaryGraph terminalPrinter graph step.M

            let start =
                { Matrices = renderMatricesStart step
                  AutomatonDot = automatonStartDot
                  AutomatonTikz = automatonStartTikz
                  GraphDot = graphStartDot
                  GraphTikz = graphStartTikz }

            // The init step has no end state and no label rows (the trace guarantees
            // Labels = [] there; the guard keeps the invariant explicit).
            if step.IsInit then
                { Start = start
                  Labels = []
                  End = None }
            else
                let labels = step.Labels |> List.map (renderLabel terminalPrinter dfa graph step)

                let automatonEndDot, automatonEndTikz =
                    automatonWithFrontier terminalPrinter dfa step.NewM

                let graphEndDot, graphEndTikz = boundaryGraph terminalPrinter graph step.NewM

                let end_ =
                    { Matrices = renderMatricesEnd step
                      AutomatonDot = automatonEndDot
                      AutomatonTikz = automatonEndTikz
                      GraphDot = graphEndDot
                      GraphTikz = graphEndTikz }

                { Start = start
                  Labels = labels
                  End = Some end_ })
