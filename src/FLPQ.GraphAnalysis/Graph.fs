namespace FLPQ.GraphAnalysis

open FLPQ.LinearAlgebra

/// Generic graph with vertices stored in a map and edges stored in a matrix.
/// Vertices are identified by integer indices. Edges are stored in a square matrix
/// where element [i, j] represents the edge from vertex i to vertex j.
type Graph<'v, 'e> =
    { VertexMap: Map<int, 'v>
      Edges: Matrix<'e> }

module Graph =

    /// Returns the number of vertices in the graph.
    let vertexCount (graph: Graph<'v, 'e>) = graph.VertexMap.Count

    /// Returns all vertices as (index, value) pairs sorted by index.
    let vertices (graph: Graph<'v, 'e>) : (int * 'v) list =
        graph.VertexMap |> Map.toList |> List.sortBy fst

    /// Tries to get the vertex value at the given index. Returns None if the index does not exist.
    let tryGetVertex idx (graph: Graph<'v, 'e>) = Map.tryFind idx graph.VertexMap

    /// Gets the vertex value at the given index. Throws KeyNotFoundException if the index does not exist.
    let getVertex idx (graph: Graph<'v, 'e>) = Map.find idx graph.VertexMap

    /// Returns the edge label between two vertices.
    let edge (graph: Graph<'v, 'e>) (fromIdx: int) (toIdx: int) = graph.Edges.[fromIdx, toIdx]

    /// Transforms vertex values using the given function, preserving edges.
    let mapVertices (f: 'v -> 'w) (graph: Graph<'v, 'e>) : Graph<'w, 'e> =
        { VertexMap = graph.VertexMap |> Map.map (fun _ v -> f v)
          Edges = graph.Edges }

    /// Transforms edge labels using the given function, preserving vertices.
    let mapEdges (f: 'e -> 'f) (graph: Graph<'v, 'e>) : Graph<'v, 'f> =
        { VertexMap = graph.VertexMap
          Edges = Matrix.map f graph.Edges }

    /// Creates a graph from a list of vertex labels and an edge matrix.
    /// Vertices are assigned indices 0..|states|-1 in list order.
    let fromEdges (states: 'v list) (edgeMatrix: Matrix<'e>) : Graph<'v, 'e> =
        { VertexMap = states |> List.indexed |> List.map (fun (i, v) -> (i, v)) |> Map.ofList
          Edges = edgeMatrix }

    /// Keep only the specified vertices and edges between them.
    /// Vertex indices are remapped to 0..|keep|-1 preserving ascending order.
    let keepVertices (keep: Set<int>) (graph: Graph<'v, 'e>) : Graph<'v, 'e> =
        let keepArr = keep |> Set.toArray |> Array.sort
        let newSize = keepArr.Length

        let newVertexMap =
            keepArr
            |> Array.mapi (fun newIdx oldIdx -> (newIdx, graph.VertexMap.[oldIdx]))
            |> Map.ofArray

        let newEdges =
            Matrix.create newSize newSize (fun i j -> graph.Edges.[keepArr.[i], keepArr.[j]])

        { VertexMap = newVertexMap
          Edges = newEdges }

    /// Generic filter: keep only outgoing edges from specified vertices.
    /// result = diagonal(selectedVertices) × edges using maskOp for multiplication
    /// and combineOp for addition. maskOp : bool -> 'e -> 'e determines whether
    /// the edge is kept (keep flag times edge). combineOp : 'e -> 'e -> 'e merges
    /// multiple edges between the same pair.
    let filterOutgoingGeneric
        (zero: 'e)
        (maskOp: bool -> 'e -> 'e)
        (combineOp: 'e -> 'e -> 'e)
        (selectedVertices: Set<int>)
        (graph: Graph<'v, 'e>)
        : Graph<'v, 'e> =
        let n = vertexCount graph
        let diag = Matrix.diagonal n selectedVertices true false
        let filtered = LinearAlgebra.mxm diag graph.Edges maskOp combineOp zero
        { graph with Edges = filtered }

    /// Generic filter: keep only incoming edges to specified vertices.
    /// result = edges × diagonal(selectedVertices) using maskOp and combineOp.
    let filterIncomingGeneric
        (zero: 'e)
        (maskOp: 'e -> bool -> 'e)
        (combineOp: 'e -> 'e -> 'e)
        (selectedVertices: Set<int>)
        (graph: Graph<'v, 'e>)
        : Graph<'v, 'e> =
        let n = vertexCount graph
        let diag = Matrix.diagonal n selectedVertices true false
        let filtered = LinearAlgebra.mxm graph.Edges diag maskOp combineOp zero
        { graph with Edges = filtered }

    /// Filter to keep only outgoing edges from specified vertices.
    /// Multiplies diagonal(selectedVertices) by edges in the Boolean semiring.
    let filterOutgoing (selectedVertices: Set<int>) (graph: Graph<'v, bool>) : Graph<'v, bool> =
        filterOutgoingGeneric false (&&) (||) selectedVertices graph

    /// Filter to keep only incoming edges to specified vertices.
    /// Multiplies edges by diagonal(selectedVertices) in the Boolean semiring.
    let filterIncoming (selectedVertices: Set<int>) (graph: Graph<'v, bool>) : Graph<'v, bool> =
        filterIncomingGeneric false (&&) (||) selectedVertices graph
