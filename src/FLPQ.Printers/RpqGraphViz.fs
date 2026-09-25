namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ

/// Shared rendering of the RPQ input graph with path highlights (used by the Arroyuelo and
/// Belyanin step visualizers and runners). Paths are vertex sequences from the path semiring.
module RpqGraphViz =

    /// All graph edges with their terminal labels (epsilon-only edges label as "ε").
    let private graphEdgeSet (graph: NFA<'t, int>) : Set<int * int> =
        let t = graph.Transitions

        Set.ofList
            [ for i in 0 .. Matrix.rows t - 1 do
                  for j in 0 .. Matrix.cols t - 1 do
                      if t.[i, j].IsSome then
                          (i, j) ]

    let private graphEdgeLabel (graph: NFA<'t, int>) (terminalPrinter: 't -> string) (e: int * int) : string =
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

    /// Vertices and edges used by the paths stored in a path semiring matrix.
    let pathHighlights (result: Matrix<Set<int list>>) : Set<int> * Set<int * int> =
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

    /// Render the graph with the given highlighted vertices/edges (pass empty sets for the
    /// plain input graph). Returns (dot, tikz).
    let renderGraph
        (terminalPrinter: 't -> string)
        (graph: NFA<'t, int>)
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

        // bendReciprocalEdges = true: the input graph may carry both directions of a pair
        // (e.g. v2->v3 and v3->v2), which TikZ would otherwise draw fully overlapped.
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
                true

        (dot, tikz)
