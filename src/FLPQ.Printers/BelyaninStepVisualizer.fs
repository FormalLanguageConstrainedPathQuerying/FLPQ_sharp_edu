namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.BelyaninRPQ

/// Step-by-step visualization of Belyanin's RPQ evaluation (book: Chapter 11, 02_BFS.tex).
/// Each step renders the query DFA with the current frontier states highlighted, the
/// frontier/accumulated matrices with the per-label propagation products, and the input
/// graph with the current frontier paths highlighted.
module BelyaninStepVisualizer =

    /// One rendered trace step: the query DFA with the frontier states highlighted, the
    /// frontier/accumulated matrices with the per-label propagation products, and the input
    /// graph with the current frontier paths highlighted — each in DOT and TikZ form (the
    /// runner picks one).
    [<Struct>]
    type BelyaninVisualizationStep =
        { AutomatonDot: string
          AutomatonTikz: string
          Matrices: string
          GraphDot: string
          GraphTikz: string }

    /// The automaton states of the current frontier: q with a non-empty M[q, *] cell.
    let private frontierStates (m: Matrix<Set<int list>>) : Set<int> =
        let rows = Matrix.rows m
        let cols = Matrix.cols m

        Set.ofList
            [ for q in 0 .. rows - 1 do
                  if
                      [ for v in 0 .. cols - 1 do
                            not (PathSemiring.isZero m.[q, v]) ]
                      |> List.exists id
                  then
                      q ]

    /// A label as a TeX math symbol: terminals escaped, epsilon as \varepsilon.
    let private labelToTeX (terminalPrinter: 't -> string) (label: AutomatonLabel<'t>) : string =
        match label with
        | ATerm t -> AutomatonTikz.escapeLatex (terminalPrinter t)
        | AEpsilon -> @"\varepsilon"

    /// Row/column label printers: q_i for automaton states, v_i for graph vertices.
    let private stateLabel (i: int) : string = sprintf "q_%d" i

    let private vertexLabel (j: int) : string = sprintf "v_%d" j

    /// One label's propagation as a TeX block with every matrix written explicitly:
    /// (N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <Select>, then × G^a = <G^a> = <Extend>.
    let private labelBlock
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
        + "\n"
        + sprintf @"$\times G^{%s} =$" l
        + "\n"
        + PathSemiringTeX.boolMatrixToTeX vertexLabel vertexLabel ls.G
        + "\n"
        + @"$=$"
        + "\n"
        + PathSemiringTeX.matrixWithStateVertexLabels ls.Extend

    /// The step's matrices as a vertical stack: F (frontier) and V (visited), then per
    /// label the explicit propagation products, then New F.
    let private renderMatrices (terminalPrinter: 't -> string) (step: BelyaninTraceStep<'t>) : string =
        let fBlock =
            @"$\text{F}$" + "\n" + PathSemiringTeX.matrixWithStateVertexLabels step.M

        let vBlock =
            @"$\text{V}$" + "\n" + PathSemiringTeX.matrixWithStateVertexLabels step.P

        if step.IsInit then
            fBlock + "\n" + vBlock
        else
            let newFBlock =
                @"$\text{New } F =$"
                + "\n"
                + PathSemiringTeX.matrixWithStateVertexLabels step.NewM

            [ fBlock; vBlock ]
            @ (step.Labels |> List.map (labelBlock terminalPrinter step.M))
            @ [ newFBlock ]
            |> String.concat "\n"

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
            let frontier = frontierStates step.M

            let automatonDot =
                AutomatonDot.dfaToDotWithHighlights terminalPrinter (fun idx _ -> sprintf "q_%d" idx) dfa frontier

            let automatonTikz =
                AutomatonTikz.dfaToTikzWithHighlights
                    terminalPrinter
                    (fun idx _ -> sprintf "$q_%d$" idx)
                    "circle"
                    dfa
                    frontier

            // The edges traversed on this step are the final edges of the paths produced
            // by it (NewM) — highlighting them here, not at the next step. The frontier
            // paths being processed (M) render as the light-red path tier; only the
            // endpoints of the traversed edges get the vertex highlight.
            let currentEdges = RpqGraphViz.pathLastEdges step.NewM
            let frontierPathEdges = RpqGraphViz.pathEdges step.M

            let graphDot, graphTikz =
                RpqGraphViz.renderGraph
                    terminalPrinter
                    graph
                    (RpqGraphViz.edgeEndpoints currentEdges)
                    frontierPathEdges
                    currentEdges

            { AutomatonDot = automatonDot
              AutomatonTikz = automatonTikz
              Matrices = renderMatrices terminalPrinter step
              GraphDot = graphDot
              GraphTikz = graphTikz })
