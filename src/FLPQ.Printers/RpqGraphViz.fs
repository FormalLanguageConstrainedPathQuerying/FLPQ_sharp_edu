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

    let private graphEdgeLabel
        (graph: NFA<'t, int>)
        (terminalPrinter: 't -> string)
        (extraLabels: Map<int * int, string>)
        (e: int * int)
        : string =
        let i, j = e

        // The input-edge label (the terminal labels, or "ε" for an epsilon-only edge).
        let inputLabel =
            match graph.Transitions.[i, j] with
            | Some symbols ->
                symbols
                |> NonEmptySet.toSeq
                |> Seq.choose (function
                    | ATerm t -> Some t
                    | AEpsilon -> None)
                |> List.ofSeq
                |> (fun terms ->
                    if List.isEmpty terms then
                        "ε"
                    else
                        terms |> List.map terminalPrinter |> String.concat ", ")
            | None -> ""

        // Merge in the derived label (a nonterminal edge added on top of the input graph).
        match Map.tryFind e extraLabels with
        | Some lbl -> if inputLabel = "" then lbl else inputLabel + ", " + lbl
        | None -> inputLabel

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

    /// Vertices at which the paths stored in a path semiring matrix end — the frontier's
    /// positions in the graph (graph-side counterpart of the automaton's frontier states).
    let frontierVertices (result: Matrix<Set<int list>>) : Set<int> =
        PathSemiring.allPaths result
        |> Set.fold
            (fun acc p ->
                match p with
                | [] -> acc
                | _ -> Set.add (List.last p) acc)
            Set.empty

    /// Vertices and edges used by the paths stored in a path semiring matrix.
    let pathHighlights (result: Matrix<Set<int list>>) : Set<int> * Set<int * int> =
        (pathVertices result, pathEdges result)

    /// Render the graph with the given highlights (pass empty sets for the plain input
    /// graph): highlightedVertices get the yellow vertex fill, startVertices the green source
    /// fill, frontierVertices light blue, currentEdges render red and bold, pathEdges light
    /// red. `extraEdges` are vertex pairs drawn as edges even when not input edges (derived
    /// nonterminal edges, e.g. the Kronecker CFPQ per-step graph), each labeled from
    /// `extraEdgeLabels` (merged into the input-edge label when the pair is also an input
    /// edge). Delegates to the RPQ-dedicated RpqGraphDot / RpqGraphTikz renderers (independent
    /// of the GSS renderers). Returns (dot, tikz).
    let renderGraph
        (terminalPrinter: 't -> string)
        (graph: NFA<'t, int>)
        (highlightedVertices: Set<int>)
        (startVertices: Set<int>)
        (frontierVertices: Set<int>)
        (currentEdges: Set<int * int>)
        (pathEdges: Set<int * int>)
        (extraEdges: Set<int * int>)
        (extraEdgeLabels: Map<int * int, string>)
        : string * string =
        let n = Nfa.stateCount graph

        let allVertices =
            Set.ofList
                [ for i in 0 .. n - 1 do
                      i ]

        let gEdges = Set.union (graphEdgeSet graph) extraEdges
        let edgeLabel = graphEdgeLabel graph terminalPrinter extraEdgeLabels

        let dot =
            RpqGraphDot.toDot
                (fun v -> sprintf "v_%d" v)
                edgeLabel
                allVertices
                gEdges
                highlightedVertices
                startVertices
                frontierVertices
                currentEdges
                pathEdges

        // Reciprocal pairs (e.g. v2->v3 and v3->v2) are bent apart by RpqGraphTikz so TikZ
        // does not draw them fully overlapped.
        let tikz =
            RpqGraphTikz.toTikz
                (fun v -> sprintf "$v_%d$" v)
                (fun e -> AutomatonTikz.escapeLatex (edgeLabel e))
                allVertices
                gEdges
                highlightedVertices
                startVertices
                frontierVertices
                currentEdges
                pathEdges

        (dot, tikz)

    /// Render the graph with the original input edges plus derived (computed) edges. Every
    /// derived edge is labeled with the same `derivedTikzLabel` (`derivedDotLabel` in DOT) —
    /// the subexpression of the handled node — appended to the original label when the pair is
    /// also an input edge (labels merge, e.g. `"a, b | c"`). Exact repeated components are
    /// removed during the merge (`"X, X"` -> `"X"`), so no edge label repeats the same text.
    /// Only start vertices get a fill; no path or current-edge highlighting. `derivedTikzLabel`
    /// is already-rendered math (e.g. `"$E$"`), `derivedDotLabel` the plain label (e.g.
    /// `Regexp.toString`). Returns (dot, tikz).
    let renderGraphWithDerived
        (terminalPrinter: 't -> string)
        (graph: NFA<'t, int>)
        (derivedEdges: Set<int * int>)
        (derivedTikzLabel: string)
        (derivedDotLabel: string)
        (startVertices: Set<int>)
        : string * string =
        let n = Nfa.stateCount graph

        let allVertices =
            Set.ofList
                [ for i in 0 .. n - 1 do
                      i ]

        let gEdges = graphEdgeSet graph
        let edgeLabel = graphEdgeLabel graph terminalPrinter Map.empty
        let allEdges = Set.union gEdges derivedEdges

        // Join the input-edge label and the derived label, dropping exact repeated components
        // so a label never repeats the same text (e.g. "X, X" -> "X", "X, Y, X" -> "X, Y").
        // `original` is comma-joined already, `derived` is a single label and stays whole.
        let merge (original: string) (derived: string) =
            let parts =
                [ if original <> "" then
                      yield! original.Split(", ")
                  if derived <> "" then
                      yield derived ]

            parts |> List.distinct |> String.concat ", "

        let dotEdgeLabel (e: int * int) =
            let original = if Set.contains e gEdges then edgeLabel e else ""

            let derived = if Set.contains e derivedEdges then derivedDotLabel else ""

            merge original derived

        let tikzEdgeLabel (e: int * int) =
            let original =
                if Set.contains e gEdges then
                    AutomatonTikz.escapeLatex (edgeLabel e)
                else
                    ""

            let derived = if Set.contains e derivedEdges then derivedTikzLabel else ""

            merge original derived

        let dot =
            RpqGraphDot.toDot
                (fun v -> sprintf "v_%d" v)
                dotEdgeLabel
                allVertices
                allEdges
                Set.empty
                startVertices
                Set.empty
                derivedEdges
                Set.empty

        let tikz =
            RpqGraphTikz.toTikz
                (fun v -> sprintf "$v_%d$" v)
                tikzEdgeLabel
                allVertices
                allEdges
                Set.empty
                startVertices
                Set.empty
                derivedEdges
                Set.empty

        (dot, tikz)
