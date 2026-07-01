namespace FLPQ.GraphAnalysis

open FLPQ.LinearAlgebra

/// Generic graph with vertices stored in a map and edges stored in a matrix.
/// Vertices are identified by integer indices. Edges are stored in a square matrix
/// where element [i, j] represents the edge from vertex i to vertex j.
type Graph<'v, 'e> =
    { vertexMap: Map<int, 'v>
      edges: Matrix<'e> }

module Graph =

    let vertexCount (g: Graph<'v, 'e>) = g.vertexMap.Count

    let vertices (g: Graph<'v, 'e>) : (int * 'v) list =
        g.vertexMap |> Map.toList |> List.sortBy fst

    let tryGetVertex idx (g: Graph<'v, 'e>) = Map.tryFind idx g.vertexMap

    let getVertex idx (g: Graph<'v, 'e>) = Map.find idx g.vertexMap

    let edge (g: Graph<'v, 'e>) (fromIdx: int) (toIdx: int) = g.edges.data.[fromIdx, toIdx]

    let mapVertices (f: 'v -> 'w) (g: Graph<'v, 'e>) : Graph<'w, 'e> =
        { vertexMap = g.vertexMap |> Map.map (fun _ v -> f v)
          edges = g.edges }

    let mapEdges (f: 'e -> 'f) (g: Graph<'v, 'e>) : Graph<'v, 'f> =
        { vertexMap = g.vertexMap
          edges = Matrix.map f g.edges }

    let fromEdges (states: 'v list) (edgeMatrix: Matrix<'e>) : Graph<'v, 'e> =
        { vertexMap = states |> List.indexed |> List.map (fun (i, v) -> (i, v)) |> Map.ofList
          edges = edgeMatrix }

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
        (g: Graph<'v, 'e>)
        : Graph<'v, 'e> =
        let n = vertexCount g
        let diag = Matrix.diagonal n selectedVertices true false
        let filtered = LinearAlgebra.mxm diag g.edges maskOp combineOp zero
        { g with edges = filtered }

    /// Generic filter: keep only incoming edges to specified vertices.
    /// result = edges × diagonal(selectedVertices) using maskOp and combineOp.
    let filterIncomingGeneric
        (zero: 'e)
        (maskOp: 'e -> bool -> 'e)
        (combineOp: 'e -> 'e -> 'e)
        (selectedVertices: Set<int>)
        (g: Graph<'v, 'e>)
        : Graph<'v, 'e> =
        let n = vertexCount g
        let diag = Matrix.diagonal n selectedVertices true false
        let filtered = LinearAlgebra.mxm g.edges diag maskOp combineOp zero
        { g with edges = filtered }

    /// Filter to keep only outgoing edges from specified vertices.
    /// Multiplies diagonal(selectedVertices) by edges in the Boolean semiring.
    let filterOutgoing (selectedVertices: Set<int>) (g: Graph<'v, bool>) : Graph<'v, bool> =
        filterOutgoingGeneric false (&&) (||) selectedVertices g

    /// Filter to keep only incoming edges to specified vertices.
    /// Multiplies edges by diagonal(selectedVertices) in the Boolean semiring.
    let filterIncoming (selectedVertices: Set<int>) (g: Graph<'v, bool>) : Graph<'v, bool> =
        filterIncomingGeneric false (fun e k -> e && k) (||) selectedVertices g
