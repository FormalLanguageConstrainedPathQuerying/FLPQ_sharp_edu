namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.BelyaninRPQ

/// Step-by-step visualization of Belyanin's RPQ evaluation (book: Chapter 11, 02_BFS.tex,
/// algo:RPQ_BFS_semiring). Each step renders the state at the start of the step (frontier F,
/// visited V = P \ F, the query DFA with the frontier states highlighted, and the input graph
/// with green sources, lightblue frontier vertices, and lightred frontier path edges), one pair
/// of rows per active label with the explicit propagation products (N^a)^T ⊗ F = F^a and
/// F^a ⊗ G^a = Extend plus the used transitions / followed edges highlighted red bold, and the
/// state at the end of the step (F, V). F and V are separate artifacts (the step template
/// places them side by side); each row is one substep.
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

    /// The state at a step boundary: the F (frontier) and V (visited) matrices (F/V at the
    /// start and at the end) and the automaton/graph figures with that boundary's
    /// frontier highlighted — each figure in DOT and TikZ form (the runner picks one). F
    /// and V are separate artifacts so the step template can place them side by side.
    type BelyaninBoundaryVisual =
        { Frontier: string
          Visited: string
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

    /// Wrap `content` in a single shrink-only adjustbox with the given options.
    let private wrapAdjustbox (options: string) (content: string) : string =
        @"\begin{adjustbox}{"
        + options
        + "}"
        + "\n"
        + content
        + "\n"
        + @"\end{adjustbox}"

    /// Wrap a one-line formula expression in a single shrink-only adjustbox so the three
    /// side-by-side matrices fit the formula column. Top-aligned (`valign=T`) so the formula
    /// and its figure line up.
    let private wrapFormula (expression: string) : string =
        wrapAdjustbox @"max width=\textwidth, valign=T" expression

    /// The first line of a label's propagation — the book's per-label term (N^a)^T ⊗ M ⊗ G^a
    /// split at F^a: (N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <F^a>, rendered as one horizontal line
    /// (all three matrices side by side in one math expression, wrapped once in an adjustbox).
    let private selectFormula
        (terminalPrinter: 't -> string)
        (f: Matrix<Set<int list>>)
        (ls: BelyaninLabelStep<'t>)
        : string =
        let l = labelToTeX terminalPrinter ls.Label

        sprintf @"$(N^{%s})^T \otimes F = " l
        + PathSemiringTeX.boolMatrixToTeXBody stateLabel stateLabel (Matrix.transpose ls.N)
        + @" \otimes "
        + PathSemiringTeX.matrixWithStateVertexLabelsBody f
        + @" = "
        + PathSemiringTeX.matrixWithStateVertexLabelsBody ls.Select
        + @"$"
        |> wrapFormula

    /// The second line of a label's propagation: F^a ⊗ G^a = <F^a> ⊗ <G^a> = <Extend>, also
    /// rendered as one horizontal line.
    let private extendFormula (terminalPrinter: 't -> string) (ls: BelyaninLabelStep<'t>) : string =
        let l = labelToTeX terminalPrinter ls.Label

        sprintf @"$F^{%s} \otimes G^{%s} = " l l
        + PathSemiringTeX.matrixWithStateVertexLabelsBody ls.Select
        + @" \otimes "
        + PathSemiringTeX.boolMatrixToTeXBody vertexLabel vertexLabel ls.G
        + @" = "
        + PathSemiringTeX.matrixWithStateVertexLabelsBody ls.Extend
        + @"$"
        |> wrapFormula

    /// A boundary tile: the title and the matrix in one adjustbox/math, rendered as
    /// `$<title> = <matrix>$` so the label line reads `F = [F]` / `V = [V]`. `valignTop` adds
    /// adjustbox's `valign=T`; every step tile uses it except the start frontier F (the first
    /// box of the step), which anchors the line.
    let private matrixTile (valignTop: bool) (title: string) (m: Matrix<Set<int list>>) : string =
        let options =
            if valignTop then
                @"max width=\textwidth, valign=T"
            else
                @"max width=\textwidth"

        wrapAdjustbox
            options
            ("$\n"
             + title
             + " =\n"
             + PathSemiringTeX.matrixWithStateVertexLabelsBody m
             + "\n$")

    /// The start-of-step frontier tile: F = M (the current frontier). The first box of the
    /// step, so it has no `valign=T`.
    let private renderFrontierStart (step: BelyaninTraceStep<'t>) : string = matrixTile false @"\text{F}" step.M

    /// The start-of-step visited tile: V = P \ F (cell-wise set difference). V is the exact
    /// inverse of the trace's P <- P + F, so no trace change is needed.
    let private renderVisitedStart (step: BelyaninTraceStep<'t>) : string =
        let vBefore = Matrix.map2 Set.difference step.P step.M

        matrixTile true @"\text{V}" vBefore

    /// The end-of-step frontier tile: F = NewM (the next frontier).
    let private renderFrontierEnd (step: BelyaninTraceStep<'t>) : string = matrixTile true @"\text{F}" step.NewM

    /// The end-of-step visited tile: V = P (everything visited after the step).
    let private renderVisitedEnd (step: BelyaninTraceStep<'t>) : string = matrixTile true @"\text{V}" step.P

    /// The query DFA as (dot, tikz) figures with the given frontier states, target states, and
    /// transitions highlighted.
    let private dfaFigures
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (highlightedStates: Set<int>)
        (targetStates: Set<int>)
        (highlightedEdges: Set<int * int>)
        : string * string =
        let dot =
            AutomatonDot.dfaToDotWithHighlights
                terminalPrinter
                (fun idx _ -> sprintf "q_%d" idx)
                dfa
                highlightedStates
                targetStates
                highlightedEdges

        let tikz =
            AutomatonTikz.dfaToTikzWithHighlights
                terminalPrinter
                (fun idx _ -> sprintf "$q_%d$" idx)
                "circle"
                dfa
                highlightedStates
                targetStates
                highlightedEdges

        (dot, tikz)

    /// The query DFA with the frontier states of a path matrix highlighted (no target states,
    /// no edge highlights). Returns (dot, tikz).
    let private automatonWithFrontier
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (m: Matrix<Set<int list>>)
        : string * string =
        dfaFigures terminalPrinter dfa (frontierStates m) Set.empty Set.empty

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
    /// from-endpoints, lightyellow targets, and F's path edges lightred.
    let private renderLabel
        (terminalPrinter: 't -> string)
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        (step: BelyaninTraceStep<'t>)
        (ls: BelyaninLabelStep<'t>)
        : BelyaninLabelVisual =
        let used = usedAutoEdges step.M ls.N
        let automatonTargets = frontierStates ls.Select

        let automatonDot, automatonTikz =
            dfaFigures terminalPrinter dfa Set.empty automatonTargets used

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
                { Frontier = renderFrontierStart step
                  Visited = renderVisitedStart step
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
                    { Frontier = renderFrontierEnd step
                      Visited = renderVisitedEnd step
                      AutomatonDot = automatonEndDot
                      AutomatonTikz = automatonEndTikz
                      GraphDot = graphEndDot
                      GraphTikz = graphEndTikz }

                { Start = start
                  Labels = labels
                  End = Some end_ })
