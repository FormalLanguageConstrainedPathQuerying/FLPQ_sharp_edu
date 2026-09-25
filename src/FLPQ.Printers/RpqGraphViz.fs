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

    /// Vertices used by the paths stored in a path semiring matrix.
    let pathVertices (result: Matrix<Set<int list>>) : Set<int> =
        PathSemiring.allPaths result
        |> Set.fold (fun acc p -> p |> List.fold (fun a v -> Set.add v a) acc) Set.empty

    /// Edges used by the paths stored in a path semiring matrix.
    let pathEdges (result: Matrix<Set<int list>>) : Set<int * int> =
        PathSemiring.allPaths result
        |> Set.fold
            (fun acc p ->
                p
                |> List.windowed 2
                |> List.map (fun w -> (w.[0], w.[1]))
                |> List.fold (fun a e -> Set.add e a) acc)
            Set.empty

    /// The last edge of every stored path with at least two vertices: the edge traversed
    /// when the path was extended to its final vertex. Trivial paths contribute nothing.
    let pathLastEdges (result: Matrix<Set<int list>>) : Set<int * int> =
        PathSemiring.allPaths result
        |> Set.fold
            (fun acc p ->
                match p with
                | []
                | [ _ ] -> acc
                | _ ->
                    let from = p.[List.length p - 2]
                    let to_ = p.[List.length p - 1]
                    Set.add (from, to_) acc)
            Set.empty

    /// Endpoints of a set of edges.
    let edgeEndpoints (edges: Set<int * int>) : Set<int> =
        edges
        |> Set.fold (fun acc (from, to_) -> Set.add from (Set.add to_ acc)) Set.empty

    /// Vertices and edges used by the paths stored in a path semiring matrix.
    let pathHighlights (result: Matrix<Set<int list>>) : Set<int> * Set<int * int> =
        (pathVertices result, pathEdges result)

    /// Render the graph with the given highlights (pass empty sets for the plain input
    /// graph): highlightedVertices get the vertex fill, pathEdges render light red,
    /// currentEdges render red and bold. Returns (dot, tikz).
    let renderGraph
        (terminalPrinter: 't -> string)
        (graph: NFA<'t, int>)
        (highlightedVertices: Set<int>)
        (pathEdges: Set<int * int>)
        (currentEdges: Set<int * int>)
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
                currentEdges
                pathEdges
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
                currentEdges
                pathEdges
                Set.empty
                None
                "circle"
                true
                None
                true

        (dot, tikz)
