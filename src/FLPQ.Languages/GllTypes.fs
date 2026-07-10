namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// SPPF node — variant for different types of nodes in the Shared Packed Parse Forest.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex), paper DAMDID_GLL_CFPQ/sections/gll.tex.
[<RequireQualifiedAccess>]
type SppfNodeInfo<'t, 'nt when 't: comparison and 'nt: comparison> =
    | SppfTerminal of Terminal<'t> * leftPos: int * rightPos: int
    | SppfNonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int
    | SppfEpsilon of pos: int
    | SppfIntermediate of state: int * pos: int
    | SppfRange of fromState: int * fromPos: int * toState: int * toPos: int

/// Edge labels in the SPPF graph encoding the tree/forest structure.
/// Between a specific (parent, child) pair there cannot be two edges of different types.
[<RequireQualifiedAccess>]
type SppfEdgeLabel =
    | SingleChild
    | LeftChild
    | RightChild
    | PackedAlternative

/// Shared Packed Parse Forest — a graph encoding all possible parse trees for a given input.
/// Built once from the path index after GLL execution.
/// rootIndices points to SppfRange nodes corresponding to ranges of interest.
type SPPF<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Graph: Graph<SppfNodeInfo<'t, 'nt>, Option<SppfEdgeLabel>>
      RootIndices: int list }

/// A range in the path index: from (fromState, fromVertex) to (toState, toVertex).
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type RangeKey =
    { FromState: int
      FromVertex: int
      ToState: int
      ToVertex: int }

/// Describes a matched range (possibly empty) during GLL execution.
/// Book reference: sec:CFPQ_GLL.
[<RequireQualifiedAccess>]
type RangeDescriptor =
    | EmptyRange
    | NonEmptyRange of RangeKey

/// Entry stored in a path index cell — describes what was recognized in the corresponding range.
/// Book reference: sec:CFPQ_GLL.
[<RequireQualifiedAccess>]
type PathIndexEntry<'t, 'nt when 't: comparison and 'nt: comparison> =
    | PTerminal of Terminal<'t>
    | PNonterminal of Nonterminal<'nt>
    | PEpsilonNonterminal of Nonterminal<'nt>
    | PIntermediate of state: int * pos: int

/// Path index built by GLL: a K×K matrix where K = stateCount * vertexCount.
/// Cell (fromKey, toKey) stores the set of recognized entries for that range.
/// Linear index: idx(state, vertex) = state * vertexCount + vertex.
/// Book reference: sec:CFPQ_GLL.
type PathIndex<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Matrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      StateCount: int
      VertexCount: int }

module PathIndex =

    /// Maps (state, vertex) to a linear index in the PathIndex matrix.
    let linearIndex (pi: PathIndex<'t, 'nt>) (state: int) (vertex: int) : int = state * pi.VertexCount + vertex

    /// Adds an entry to the path index at range (fromState, fromVertex) → (toState, toVertex).
    let add
        (pi: PathIndex<'t, 'nt>)
        (fromState: int)
        (fromVertex: int)
        (toState: int)
        (toVertex: int)
        (entry: PathIndexEntry<'t, 'nt>)
        : unit =
        let fromIdx = linearIndex pi fromState fromVertex
        let toIdx = linearIndex pi toState toVertex
        let current = Matrix.get pi.Matrix fromIdx toIdx
        Matrix.set pi.Matrix fromIdx toIdx (Set.add entry current)

    /// Gets the set of entries at range (fromState, fromVertex) → (toState, toVertex).
    let get
        (pi: PathIndex<'t, 'nt>)
        (fromState: int)
        (fromVertex: int)
        (toState: int)
        (toVertex: int)
        : Set<PathIndexEntry<'t, 'nt>> =
        let fromIdx = linearIndex pi fromState fromVertex
        let toIdx = linearIndex pi toState toVertex
        Matrix.get pi.Matrix fromIdx toIdx

/// Vertex in the Graph-Structured Stack (GSS).
/// storedPops is stored in a separate array inside GSS to allow efficient mutation
/// (struct fields in immutable Maps cannot be mutated in-place).
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type GssVertexInfo = { State: int; Vertex: int }

/// Edge in the Graph-Structured Stack (GSS).
/// Records the return state, pre-call context, and the matched range before the nonterminal call.
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type GssEdgeInfo =
    { ReturnState: int
      PreCallState: int
      PreCallVertex: int
      MatchedRange: RangeDescriptor }

/// Graph-Structured Stack — a graph encoding all active call stacks during GLL execution.
/// Vertices are all possible (state, vertex) pairs (|Q|*|V| vertices), pre-allocated.
/// Edges carry NonEmptySet because multiple edges between the same pair are possible.
/// storedPops[i] holds the ranges recognized at GSS vertex i.
/// Book reference: sec:CFPQ_GLL.
type GSS =
    { Graph: Graph<GssVertexInfo, Option<NonEmptySet<GssEdgeInfo>>>
      StoredPops: Set<RangeDescriptor> array }

module GSS =

    /// Maps (state, vertex) to a linear index in the GSS.
    let linearIndex (vertexCount: int) (state: int) (vertex: int) : int = state * vertexCount + vertex

    /// Pre-allocates the GSS with all |Q|*|V| vertices and an empty edge matrix.
    let init (stateCount: int) (vertexCount: int) : GSS =
        let k = stateCount * vertexCount

        let vertices =
            [ for s in 0 .. stateCount - 1 do
                  for v in 0 .. vertexCount - 1 -> { State = s; Vertex = v } ]

        let edges = Matrix.init k k None
        let pops = Array.create k Set.empty

        { Graph = Graph.fromEdges vertices edges
          StoredPops = pops }

    /// Adds an edge from source GSS vertex to target GSS vertex.
    /// Returns the storedPops from the target vertex (ranges already recognized at that vertex),
    /// and clears them.
    let addEdge (gss: GSS) (fromIdx: int) (toIdx: int) (edgeInfo: GssEdgeInfo) : Set<RangeDescriptor> =
        let current =
            match Matrix.get gss.Graph.Edges fromIdx toIdx with
            | Some nes -> NonEmptySet.add edgeInfo nes
            | None -> NonEmptySet.singleton edgeInfo

        Matrix.set gss.Graph.Edges fromIdx toIdx (Some current)

        let pops = gss.StoredPops.[toIdx]
        gss.StoredPops.[toIdx] <- Set.empty
        pops

    /// Saves the recognized range to the storedPops of the GSS vertex.
    /// Returns all outgoing edges from this vertex.
    let pop (gss: GSS) (gssIdx: int) (recognizedRange: RangeDescriptor) : (int * GssEdgeInfo) list =
        gss.StoredPops.[gssIdx] <- Set.add recognizedRange gss.StoredPops.[gssIdx]

        let n = gss.Graph.VertexMap.Count

        [ for toIdx in 0 .. n - 1 do
              match Matrix.get gss.Graph.Edges gssIdx toIdx with
              | Some nes ->
                  for ei in NonEmptySet.toSeq nes do
                      (toIdx, ei)
              | None -> () ]

/// Shared graph utilities for GLL/RNGLR path-index construction.
module GraphHelpers =

    /// Collects outgoing edges from each vertex of an input graph into adjacency arrays.
    /// Returns an array of ResizeArray where edges.[i] contains (label, targetVertex) pairs
    /// for all outgoing edges from vertex i.
    let collectGraphEdges (g: Graph<int, Option<'t>>) : ResizeArray<'t * int>[] =
        let vc = Graph.vertexCount g
        let edges = Array.init vc (fun _ -> ResizeArray<'t * int>())

        for i in 0 .. vc - 1 do
            for j in 0 .. vc - 1 do
                match Matrix.get g.Edges i j with
                | Some t -> edges.[i].Add(t, j)
                | None -> ()

        edges
