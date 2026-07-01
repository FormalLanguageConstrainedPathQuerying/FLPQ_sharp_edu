# Graph Module

Namespace: `FLPQ.GraphAnalysis`. Project: `FLPQ.GraphAnalysis`.

## Type Definition

### `Graph<'v, 'e>`

```fsharp
type Graph<'v, 'e> =
    { vertexMap: Map<int, 'v>
      edges: Matrix<'e> }
```

Generic graph structure where:
- Vertices are identified by integer indices and stored in a map (`vertexMap`). Each vertex has an associated value of type `'v` (the vertex label).
- Edges are stored in a square matrix of type `Matrix<'e>` where element `[i, j]` represents the edge from vertex `i` to vertex `j`.

**Design rationale**: The graph is the fundamental structure in the book. Vertices are indexed by integers for efficient matrix operations. The map associates indices to labels, supporting arbitrary label types. Edges are stored as a matrix to enable linear-algebraic operations (Boolean matrix multiplication, Kronecker product, etc.).

**Relationship to the book**: Chapter 1, `07_MatricesAndVectors.tex` — the adjacency matrix representation of graphs. Chapter 3, `05_BFS.tex` — using Boolean matrix operations for graph algorithms.

## Function Signatures

### Construction

- `fromEdges: 'v list -> Matrix<'e> -> Graph<'v, 'e>` — creates a graph from a list of vertex labels and an edge matrix. Vertices are indexed 0..n-1 in order.

### Accessors

- `vertexCount: Graph<'v, 'e> -> int` — returns the number of vertices.
- `vertices: Graph<'v, 'e> -> (int * 'v) list` — returns all vertices as (index, label) pairs, sorted by index.
- `tryGetVertex: int -> Graph<'v, 'e> -> 'v option` — returns the vertex label at the given index, or `None` if out of range.
- `getVertex: int -> Graph<'v, 'e> -> 'v` — returns the vertex label at the given index. Fails if out of range.
- `edge: Graph<'v, 'e> -> int -> int -> 'e` — returns the edge value between two vertices.

### Transformations

- `mapVertices: ('v -> 'w) -> Graph<'v, 'e> -> Graph<'w, 'e>` — transforms vertex labels.
- `mapEdges: ('e -> 'f) -> Graph<'v, 'e> -> Graph<'v, 'f>` — transforms edge values.

### Vertex Removal

- `keepVertices: Set<int> -> Graph<'v, 'e> -> Graph<'v, 'e>` — keeps only the specified vertices and edges between them. Vertex indices are remapped to 0..|keep|-1 preserving ascending order. Useful states are retained with their original labels; edges are extracted from the original matrix at the corresponding positions.

### Generic Graph Filtering

- `filterOutgoingGeneric: zero:'e -> maskOp:(bool -> 'e -> 'e) -> combineOp:('e -> 'e -> 'e) -> Set<int> -> Graph<'v, 'e> -> Graph<'v, 'e>` — keeps only outgoing edges from selected vertices. Multiplies `diagonal(selected)` by `edges` using `maskOp` (keep flag × edge) and `combineOp` (merge multiple edges between same pair). Generalized for arbitrary edge types, not just `bool`.

- `filterIncomingGeneric: zero:'e -> maskOp:('e -> bool -> 'e) -> combineOp:('e -> 'e -> 'e) -> Set<int> -> Graph<'v, 'e> -> Graph<'v, 'e>` — keeps only incoming edges to selected vertices. Multiplies `edges` by `diagonal(selected)`.

### Boolean Graph Filtering

- `filterOutgoing: Set<int> -> Graph<'v, bool> -> Graph<'v, bool>` — keeps only outgoing edges from selected vertices. Delegates to `filterOutgoingGeneric` with `zero=false`, `maskOp=(&&)`, `combineOp=(||)`.
- `filterIncoming: Set<int> -> Graph<'v, bool> -> Graph<'v, bool>` — keeps only incoming edges to selected vertices. Delegates to `filterIncomingGeneric`.

**Relationship to the book**: Chapter 3, `05_BFS.tex` — filtering is done via multiplication by diagonal matrices that serve as vertex selectors. For selecting vertices i, j, k, multiply the adjacency matrix by a diagonal matrix with ones at (i,i), (j,j), (k,k) and zeros elsewhere.

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic filter functions parameterized by `zero`/`maskOp`/`combineOp` | Enables filtering on graphs with arbitrary edge types (e.g., `Option<NonEmptySet<AutomatonLabel<'t>>>`) without requiring Boolean decomposition. Boolean versions delegate to generic ones. |
| `keepVertices` instead of filter-combinations | Removing vertices automatically removes all incident edges — no separate edge-filtering step needed. More direct than `filterOutgoing |> filterIncoming`. |
| `keepVertices` preserves ascending order | Remapped indices are deterministic and predictable. Callers building maps from old to new indices can rely on this order. |

## Integration with Automaton Types

NFA and DFA types wrap Graph for their state/transition storage:

```fsharp
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startStates: Set<int>
      finalStates: Set<int> }
    member this.states = this.graph |> Graph.vertices |> List.map snd
    member this.transitions = this.graph.edges
```

This separation follows the book's hierarchy: a graph is a generic structure, and an automaton is a graph with additional information about start and final states.
