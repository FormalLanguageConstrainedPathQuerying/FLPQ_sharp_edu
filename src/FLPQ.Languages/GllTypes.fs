namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// Descriptor in the GLL worklist: current RSM state, input graph vertex,
/// current GSS node, and the range matched so far.
/// Book reference: sec:CFPQ_GLL, Listing lst:gll_rsm_cfpq.
[<Struct; CustomEquality; NoComparison>]
type Descriptor =
    { RsmState: int
      Vertex: int
      GssIdx: int
      MatchedRange: RangeDescriptor }

    override this.Equals(obj: obj) =
        match obj with
        | :? Descriptor as other ->
            this.RsmState = other.RsmState
            && this.Vertex = other.Vertex
            && this.GssIdx = other.GssIdx
            && this.MatchedRange = other.MatchedRange
        | _ -> false

    override this.GetHashCode() =
        hash (this.RsmState, this.Vertex, this.GssIdx, this.MatchedRange)

/// Vertex in the Graph-Structured Stack (GSS).
/// StoredPops holds ranges recognized at this vertex — mutable because it is populated
/// incrementally during execution.
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type GssVertexInfo =
    { State: int
      Vertex: int
      mutable StoredPops: Set<RangeDescriptor> }

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
/// Each vertex stores its own recognized ranges (StoredPops) directly in the vertex type.
/// Edges carry NonEmptySet because multiple edges between the same pair are possible.
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
                  for v in 0 .. vertexCount - 1 ->
                      { State = s
                        Vertex = v
                        StoredPops = Set.empty } ]

        let edges = Matrix.init k k None
        let pops = Array.create k Set.empty

        { Graph = Graph.fromEdges vertices edges
          StoredPops = pops }

    /// Adds an edge from source GSS vertex (callee) to target GSS vertex (caller).
    /// Returns the storedPops from the *source* (callee) vertex — ranges already
    /// recognized at that vertex from prior completions. Does NOT clear them because
    /// multiple callers may need the same pops.
    let addEdge (gss: GSS) (fromIdx: int) (toIdx: int) (edgeInfo: GssEdgeInfo) : Set<RangeDescriptor> =
        let current =
            match Matrix.get gss.Graph.Edges fromIdx toIdx with
            | Some nes -> NonEmptySet.add edgeInfo nes
            | None -> NonEmptySet.singleton edgeInfo

        Matrix.set gss.Graph.Edges fromIdx toIdx (Some current)

        gss.StoredPops.[fromIdx]

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

/// A single step snapshot during GLL execution.
/// Captures the descriptors queue, active GSS elements, and current input position.
type GLLParsingStep<'t, 'nt when 't: comparison and 'nt: comparison> =
    {
        /// Snapshot of remaining descriptors in the worklist queue.
        Queue: Descriptor list
        /// All currently active GSS vertices (those with outgoing edges).
        ActiveGssVertices: Set<int>
        /// All currently active GSS edges (sourceIdx, targetIdx).
        ActiveGssEdges: Set<int * int>
        /// GSS vertices that became active since the previous step (for highlighting).
        NewGssVertices: Set<int>
        /// GSS edges added since the previous step (for highlighting).
        NewGssEdges: Set<int * int>
        /// Current input position being processed (from the dequeued descriptor's Vertex).
        InputPosition: int
    }
