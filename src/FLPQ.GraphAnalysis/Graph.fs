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

    /// Filter to keep only outgoing edges from specified vertices.
    /// Multiplies diagonal(selectedVertices) by edges in the Boolean semiring:
    /// result[i,j] = OR_k (diag[i,k] AND edges[k,j]).
    /// This preserves edges where the source vertex is in selectedVertices.
    let filterOutgoing (selectedVertices: Set<int>) (g: Graph<'v, bool>) : Graph<'v, bool> =
        let n = vertexCount g
        let diag = Matrix.diagonal n selectedVertices true false
        let filtered = LinearAlgebra.mxm diag g.edges (&&) (||) false
        { g with edges = filtered }

    /// Filter to keep only incoming edges to specified vertices.
    /// Multiplies edges by diagonal(selectedVertices) in the Boolean semiring:
    /// result[i,j] = OR_k (edges[i,k] AND diag[k,j]).
    /// This preserves edges where the target vertex is in selectedVertices.
    let filterIncoming (selectedVertices: Set<int>) (g: Graph<'v, bool>) : Graph<'v, bool> =
        let n = vertexCount g
        let diag = Matrix.diagonal n selectedVertices true false
        let filtered = LinearAlgebra.mxm g.edges diag (&&) (||) false
        { g with edges = filtered }
