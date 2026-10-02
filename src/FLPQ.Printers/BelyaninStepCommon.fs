namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ

/// Shared record types and cell-agnostic helpers for the two Belyanin RPQ step visualizers:
/// the simple-path visualizer (`BelyaninSimplePathStepVisualizer`, path semiring) and the
/// reachability visualizer (`BelyaninReachabilityStepVisualizer`, Boolean). Both return the
/// same `BelyaninVisualizationStep` list so the CLI writer and the `-s` summary reuse one
/// pipeline.
module BelyaninStepCommon =

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
    /// start and at the end) and the automaton/graph figures with that boundary's frontier
    /// highlighted — each figure in DOT and TikZ form (the runner picks one). F and V are
    /// separate artifacts so the step template can place them side by side.
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
    let labelToTeX (terminalPrinter: 't -> string) (label: AutomatonLabel<'t>) : string =
        match label with
        | ATerm t -> AutomatonTikz.escapeLatex (terminalPrinter t)
        | AEpsilon -> @"\varepsilon"

    /// Automaton state label printer: q_i.
    let stateLabel (i: int) : string = sprintf "q_%d" i

    /// Graph vertex label printer: v_i.
    let vertexLabel (j: int) : string = sprintf "v_%d" j

    /// True when the row of a matrix holds at least one cell that is not the additive zero.
    let rowNonEmpty (isZero: 'a -> bool) (m: Matrix<'a>) (i: int) : bool =
        [ for j in 0 .. Matrix.cols m - 1 -> not (isZero m.[i, j]) ] |> List.exists id

    /// True when the column of a matrix holds at least one cell that is not the additive zero.
    let colNonEmpty (isZero: 'a -> bool) (m: Matrix<'a>) (j: int) : bool =
        [ for i in 0 .. Matrix.rows m - 1 -> not (isZero m.[i, j]) ] |> List.exists id

    /// The automaton states of a frontier: states q with a non-empty row.
    let frontierStates (isZero: 'a -> bool) (m: Matrix<'a>) : Set<int> =
        Set.ofList
            [ for q in 0 .. Matrix.rows m - 1 do
                  if rowNonEmpty isZero m q then
                      q ]

    /// The DFA transitions used by the label's backward step: q -> q' when N^a[q, q'] and
    /// the frontier holds a non-zero cell at q.
    let usedAutoEdges (isZero: 'a -> bool) (m: Matrix<'a>) (n: Matrix<bool>) : Set<int * int> =
        Set.ofList
            [ for q in 0 .. Matrix.rows n - 1 do
                  if rowNonEmpty isZero m q then
                      for qp in 0 .. Matrix.cols n - 1 do
                          if n.[q, qp] then
                              (q, qp) ]

    /// The graph edges followed by the label's forward step: u -> v when G^a[u, v] and the
    /// selected matrix holds a non-zero cell at u.
    let followedEdges (isZero: 'a -> bool) (g: Matrix<bool>) (select: Matrix<'a>) : Set<int * int> =
        Set.ofList
            [ for u in 0 .. Matrix.rows g - 1 do
                  if colNonEmpty isZero select u then
                      for v in 0 .. Matrix.cols g - 1 do
                          if g.[u, v] then
                              (u, v) ]

    /// Wrap `content` in a single shrink-only adjustbox with the given options.
    let wrapAdjustbox (options: string) (content: string) : string =
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
    let wrapFormula (expression: string) : string =
        wrapAdjustbox @"max width=\textwidth, valign=T" expression

    /// A boundary tile from an already-rendered matrix body: `$<title> = <body>$` in one
    /// top-aligned adjustbox, so every step tile lines up with its neighbours.
    let matrixTileWithBody (title: string) (body: string) : string =
        wrapAdjustbox @"max width=\textwidth, valign=T" ("$\n" + title + " =\n" + body + "\n$")

    /// The first per-label product line from already-rendered matrix bodies:
    /// `(N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <F^a>`.
    let selectFormulaWithBodies (l: string) (nTranspose: string) (f: string) (select: string) : string =
        sprintf @"$(N^{%s})^T \otimes F = " l
        + nTranspose
        + @" \otimes "
        + f
        + @" = "
        + select
        + @"$"
        |> wrapFormula

    /// The second per-label product line from already-rendered matrix bodies:
    /// `F^a ⊗ G^a = <F^a> ⊗ <G^a> = <Extend>`.
    let extendFormulaWithBodies (l: string) (select: string) (g: string) (extend: string) : string =
        sprintf @"$F^{%s} \otimes G^{%s} = " l l
        + select
        + @" \otimes "
        + g
        + @" = "
        + extend
        + @"$"
        |> wrapFormula

    /// A boundary visual from its rendered parts (F/V tiles and the automaton/graph figures).
    let boundaryVisual
        (frontier: string)
        (visited: string)
        (automaton: string * string)
        (graph: string * string)
        : BelyaninBoundaryVisual =
        let automatonDot, automatonTikz = automaton
        let graphDot, graphTikz = graph

        { Frontier = frontier
          Visited = visited
          AutomatonDot = automatonDot
          AutomatonTikz = automatonTikz
          GraphDot = graphDot
          GraphTikz = graphTikz }

    /// Assemble the rendered steps from per-step renderers. The init-step shape (no end
    /// boundary, no label rows) is handled once here so both visualizers share it.
    let renderVisualizationSteps
        (isInit: 'step -> bool)
        (startBoundary: 'step -> BelyaninBoundaryVisual)
        (labelRows: 'step -> BelyaninLabelVisual list)
        (endBoundary: 'step -> BelyaninBoundaryVisual)
        (steps: 'step list)
        : BelyaninVisualizationStep list =
        steps
        |> List.map (fun step ->
            let labels, end_ =
                if isInit step then
                    [], None
                else
                    labelRows step, Some(endBoundary step)

            { Start = startBoundary step
              Labels = labels
              End = end_ })

    /// The query DFA as (dot, tikz) figures with the given frontier states, target states,
    /// and transitions highlighted.
    let dfaFigures
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
