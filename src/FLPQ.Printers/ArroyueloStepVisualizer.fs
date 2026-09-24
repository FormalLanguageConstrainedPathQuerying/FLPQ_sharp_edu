namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.ArroyueloRPQ

/// Step-by-step visualization of Arroyuelo's RPQ evaluation (book: Chapter 11, 03_Arroyuelo.tex).
/// Each step renders the regexp tree with the current node highlighted, the matrix equation
/// for the step's operation, and the graph with the vertices/edges of the step's result paths.
module ArroyueloStepVisualizer =

    [<Struct>]
    type ArroyueloVisualizationStep =
        { TreeDot: string
          TreeTikz: string
          Matrices: string
          GraphDot: string
          GraphTikz: string }

    /// All regexp tree nodes (one per trace step; the steps are in post-order, so the list
    /// index is the NodeIndex).
    let private treeNodeSet (steps: ArroyueloTraceStep<string, string> list) : Set<int> =
        Set.ofList
            [ for i in 0 .. List.length steps - 1 do
                  i ]

    /// Parent -> child edges of the regexp tree.
    let private treeEdgeSet (steps: ArroyueloTraceStep<string, string> list) : Set<int * int> =
        steps
        |> List.collect (fun s -> s.Children |> List.map (fun c -> (s.NodeIndex, c)))
        |> Set.ofList

    /// The subexpression at a tree node, as a math-mode TeX formula.
    let private treeVertexLabel (steps: ArroyueloTraceStep<string, string> list) (idx: int) : string =
        RegexpTeX.toTeX (List.item idx steps).Expr

    /// Tree node label for TikZ: the formula is math-mode content, so it is wrapped in $...$
    /// (as={...} is text mode; see LRAutomatonTikz.stateContentToTikzAs).
    let private treeVertexLabelTikz (steps: ArroyueloTraceStep<string, string> list) (idx: int) : string =
        "$" + treeVertexLabel steps idx + "$"

    /// One path semiring matrix with vertex row/column labels.
    let private matrixBlock (m: Matrix<Set<int list>>) : string =
        PathSemiringTeX.matrixWithVertexLabels m

    /// The step's matrix equation as a vertical stack: Base shows the formula above the result
    /// matrix; Alt/Seq show operand matrices joined by \oplus / \times and =; Star shows
    /// TC( child ) = result.
    let private renderMatrices (step: ArroyueloTraceStep<string, string>) : string =
        let result = matrixBlock step.Result

        match step.Operation with
        | Base -> sprintf "$%s$\n%s" (RegexpTeX.toTeX step.Expr) result
        | Alt ->
            sprintf "%s\n$\\oplus$\n%s\n$=$\n%s" (matrixBlock step.Operands.[0]) (matrixBlock step.Operands.[1]) result
        | Seq ->
            sprintf "%s\n$\\times$\n%s\n$=$\n%s" (matrixBlock step.Operands.[0]) (matrixBlock step.Operands.[1]) result
        | Star -> sprintf "$\\mathrm{TC}($\n%s\n$)=$\n%s" (matrixBlock step.Operands.[0]) result

    /// Render every trace step to its visualization artifacts (both DOT and TikZ; the runner
    /// writes only one format per step).
    let renderSteps
        (terminalPrinter: string -> string)
        (graph: NFA<string, int>)
        (steps: ArroyueloTraceStep<string, string> list)
        : ArroyueloVisualizationStep list =
        steps
        |> List.map (fun step ->
            let treeNodes = treeNodeSet steps
            let tEdges = treeEdgeSet steps

            let treeDot =
                GssDot.toDotFromSets
                    (treeVertexLabel steps)
                    (fun _ -> "")
                    treeNodes
                    tEdges
                    Set.empty
                    Set.empty
                    Set.empty
                    (Some step.NodeIndex)
                    None

            let treeTikz =
                GssTikz.toTikzFromSets
                    (treeVertexLabelTikz steps)
                    (fun _ -> "")
                    treeNodes
                    tEdges
                    Set.empty
                    Set.empty
                    Set.empty
                    (Some step.NodeIndex)
                    "rectangle"
                    true
                    None

            let pathVerts, pathEdgeHl = RpqGraphViz.pathHighlights step.Result

            let graphDot, graphTikz =
                RpqGraphViz.renderGraph terminalPrinter graph pathVerts pathEdgeHl

            { TreeDot = treeDot
              TreeTikz = treeTikz
              Matrices = renderMatrices step
              GraphDot = graphDot
              GraphTikz = graphTikz })
