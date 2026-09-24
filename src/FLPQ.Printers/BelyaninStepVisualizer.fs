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
    let private labelToTeX (label: AutomatonLabel<string>) : string =
        match label with
        | ATerm t -> AutomatonTikz.escapeLatex t
        | AEpsilon -> @"\varepsilon"

    /// One label's propagation as a TeX block: (N^a)^T (x) M = <Select>, then x G^a = <Extend>.
    let private labelBlock (ls: BelyaninLabelStep<string>) : string =
        let l = labelToTeX ls.Label

        sprintf @"$(N^{%s})^T \otimes M =$" l
        + "\n"
        + PathSemiringTeX.matrixWithVertexLabels ls.Select
        + "\n"
        + sprintf @"$\times G^{%s} =$" l
        + "\n"
        + PathSemiringTeX.matrixWithVertexLabels ls.Extend

    /// The step's matrices as a vertical stack: M and P, then per label the two
    /// propagation products, then New M.
    let private renderMatrices (step: BelyaninTraceStep<string>) : string =
        let mBlock = @"$\text{M}$" + "\n" + PathSemiringTeX.matrixWithVertexLabels step.M

        let pBlock = @"$\text{P}$" + "\n" + PathSemiringTeX.matrixWithVertexLabels step.P

        if step.IsInit then
            mBlock + "\n" + pBlock
        else
            let newMBlock =
                @"$\text{New } M =$" + "\n" + PathSemiringTeX.matrixWithVertexLabels step.NewM

            [ mBlock; pBlock ] @ (step.Labels |> List.map labelBlock) @ [ newMBlock ]
            |> String.concat "\n"

    /// Render every trace step to its visualization artifacts (both DOT and TikZ; the runner
    /// writes only one format per step).
    let renderSteps
        (terminalPrinter: string -> string)
        (dfa: DFA<string, int>)
        (graph: NFA<string, int>)
        (steps: BelyaninTraceStep<string> list)
        : BelyaninVisualizationStep list =
        steps
        |> List.map (fun step ->
            let frontier = frontierStates step.M

            let automatonDot =
                AutomatonDot.dfaToDotWithHighlights string (fun idx _ -> sprintf "q_%d" idx) dfa frontier

            let automatonTikz =
                AutomatonTikz.dfaToTikzWithHighlights string (fun idx _ -> sprintf "$q_%d$" idx) "circle" dfa frontier

            let pathVerts, pathEdgeHl = RpqGraphViz.pathHighlights step.M

            let graphDot, graphTikz =
                RpqGraphViz.renderGraph terminalPrinter graph pathVerts pathEdgeHl

            { AutomatonDot = automatonDot
              AutomatonTikz = automatonTikz
              Matrices = renderMatrices step
              GraphDot = graphDot
              GraphTikz = graphTikz })
