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

    /// All graph edges with their terminal labels (epsilon-only edges label as "ε").
    let private graphEdgeSet (graph: NFA<string, int>) : Set<int * int> =
        let t = graph.Transitions

        Set.ofList
            [ for i in 0 .. Matrix.rows t - 1 do
                  for j in 0 .. Matrix.cols t - 1 do
                      if t.[i, j].IsSome then
                          (i, j) ]

    let private graphEdgeLabel (graph: NFA<string, int>) (terminalPrinter: string -> string) (e: int * int) : string =
        let i, j = e

        match graph.Transitions.[i, j] with
        | Some symbols ->
            let terms =
                symbols
                |> NonEmptySet.toSeq
                |> Seq.choose (function
                    | ATerm t -> Some t
                    | AEpsilon -> None)
                |> List.ofSeq

            if List.isEmpty terms then
                "ε"
            else
                terms |> List.map terminalPrinter |> String.concat ", "
        | None -> ""

    /// Vertices and edges used by the paths stored in a result matrix.
    let private pathHighlights (result: Matrix<Set<int list>>) : Set<int> * Set<int * int> =
        PathSemiring.allPaths result
        |> Set.fold
            (fun (vAcc, eAcc) p ->
                let vAcc' = p |> List.fold (fun acc v -> Set.add v acc) vAcc

                let eAcc' =
                    p
                    |> List.windowed 2
                    |> List.map (fun w -> (w.[0], w.[1]))
                    |> List.fold (fun acc e -> Set.add e acc) eAcc

                (vAcc', eAcc'))
            (Set.empty, Set.empty)

    /// One path semiring matrix with vertex row/column labels.
    let private matrixBlock (m: Matrix<Set<int list>>) : string =
        PathSemiringTeX.matrixToTeX (fun i -> sprintf "v_%d" i) (fun j -> sprintf "v_%d" j) m

    /// Render the graph with the given highlighted vertices/edges (pass empty sets for the
    /// plain input graph). Returns (dot, tikz).
    let renderGraph
        (terminalPrinter: string -> string)
        (graph: NFA<string, int>)
        (highlightedVertices: Set<int>)
        (highlightedEdges: Set<int * int>)
        : string * string =
        let n = Nfa.stateCount graph

        let allVertices =
            Set.ofList
                [ for i in 0 .. n - 1 do
                      i ]

        let gEdges = graphEdgeSet graph
        let edgeLabel = graphEdgeLabel graph terminalPrinter

        let dot =
            GssDot.toDotFromSets
                (fun v -> sprintf "v_%d" v)
                edgeLabel
                allVertices
                gEdges
                highlightedVertices
                highlightedEdges
                Set.empty
                None
                None

        let tikz =
            GssTikz.toTikzFromSets
                (fun v -> sprintf "$v_%d$" v)
                (fun e -> AutomatonTikz.escapeLatex (edgeLabel e))
                allVertices
                gEdges
                highlightedVertices
                highlightedEdges
                Set.empty
                None
                "circle"
                true
                None

        (dot, tikz)

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

            let pathVerts, pathEdgeHl = pathHighlights step.Result
            let graphDot, graphTikz = renderGraph terminalPrinter graph pathVerts pathEdgeHl

            { TreeDot = treeDot
              TreeTikz = treeTikz
              Matrices = renderMatrices step
              GraphDot = graphDot
              GraphTikz = graphTikz })
