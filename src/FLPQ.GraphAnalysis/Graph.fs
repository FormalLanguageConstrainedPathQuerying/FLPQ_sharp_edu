namespace FLPQ.GraphAnalysis

open FLPQ.LinearAlgebra

/// Generic graph with vertices stored in a map and edges stored in a matrix.
/// Vertices are identified by integer indices. Edges are stored in a square matrix
/// where element [i, j] represents the edge from vertex i to vertex j.
type Graph<'v, 'e> =
    { vertexMap: Map<int, 'v>
      edges: Matrix<'e> }

module Graph =

    let vertexCount (graph: Graph<'v, 'e>) = graph.vertexMap.Count

    let vertices (graph: Graph<'v, 'e>) : (int * 'v) list =
        graph.vertexMap |> Map.toList |> List.sortBy fst

    let tryGetVertex idx (graph: Graph<'v, 'e>) = Map.tryFind idx graph.vertexMap

    let getVertex idx (graph: Graph<'v, 'e>) = Map.find idx graph.vertexMap

    let edge (graph: Graph<'v, 'e>) (fromIdx: int) (toIdx: int) = graph.edges.data.[fromIdx, toIdx]

    let mapVertices (f: 'v -> 'w) (graph: Graph<'v, 'e>) : Graph<'w, 'e> =
        { vertexMap = graph.vertexMap |> Map.map (fun _ v -> f v)
          edges = graph.edges }

    let mapEdges (f: 'e -> 'f) (graph: Graph<'v, 'e>) : Graph<'v, 'f> =
        { vertexMap = graph.vertexMap
          edges = Matrix.map f graph.edges }

    let fromEdges (states: 'v list) (edgeMatrix: Matrix<'e>) : Graph<'v, 'e> =
        { vertexMap = states |> List.indexed |> List.map (fun (i, v) -> (i, v)) |> Map.ofList
          edges = edgeMatrix }

    /// Keep only the specified vertices and edges between them.
    /// Vertex indices are remapped to 0..|keep|-1 preserving ascending order.
    let keepVertices (keep: Set<int>) (graph: Graph<'v, 'e>) : Graph<'v, 'e> =
        let keepArr = keep |> Set.toArray |> Array.sort
        let newSize = keepArr.Length

        let newVertexMap =
            keepArr
            |> Array.mapi (fun newIdx oldIdx -> (newIdx, graph.vertexMap.[oldIdx]))
            |> Map.ofArray

        let newEdges =
            Matrix.create newSize newSize (fun i j -> graph.edges.data.[keepArr.[i], keepArr.[j]])

        { vertexMap = newVertexMap
          edges = newEdges }

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
        let filtered = LinearAlgebra.mxm diag graph.edges maskOp combineOp zero
        { graph with edges = filtered }

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
        let filtered = LinearAlgebra.mxm graph.edges diag maskOp combineOp zero
        { graph with edges = filtered }

    /// Filter to keep only outgoing edges from specified vertices.
    /// Multiplies diagonal(selectedVertices) by edges in the Boolean semiring.
    let filterOutgoing (selectedVertices: Set<int>) (graph: Graph<'v, bool>) : Graph<'v, bool> =
        filterOutgoingGeneric false (&&) (||) selectedVertices graph

    /// Filter to keep only incoming edges to specified vertices.
    /// Multiplies edges by diagonal(selectedVertices) in the Boolean semiring.
    let filterIncoming (selectedVertices: Set<int>) (graph: Graph<'v, bool>) : Graph<'v, bool> =
        filterIncomingGeneric false (fun e k -> e && k) (||) selectedVertices graph
