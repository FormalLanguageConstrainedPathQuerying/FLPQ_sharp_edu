# Graph Module

**Tags:** data-structure, graph, matrix, adjacency-matrix
**Kind:** data-structure
**Module:** Graph
**Source:** `src/FLPQ.GraphAnalysis/Graph.fs`
**Depends on:** Matrix, LinearAlgebra
**Used by:** Automaton, GLL, RNGLR, all RPQ, SPPF, PathIndex
**Book reference:** Chapter 1, Section 07_MatricesAndVectors.tex; Chapter 3, Section 05_BFS.tex

> **Abstract:** Provides the generic `Graph<'v,'e>` type: vertices identified by integer indices stored in a map, edges in a square adjacency matrix. Used as the foundation for finite automata (NFA/DFA wrap Graph), SPPF, GSS, and all graph-based algorithms. Supports vertex/edge transformations, vertex removal with deterministic remapping, and generic Boolean matrix-based filtering.

## Contents

- [Data Structure](#data-structure)
- [Type Definition](#type-definition)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Integration with Automaton Types](#integration-with-automaton-types)
- [See Also](#see-also)

## Data Structure

A `Graph<'v,'e>` is a labeled directed graph where:
- **Vertices** are integer-indexed (0..n-1) with labels stored in a `Map<int, 'v>`. Integer indices enable efficient matrix operations.
- **Edges** are stored in a square `Matrix<'e>` where cell `[i,j]` is the edge from vertex i to vertex j.

This representation follows the book's adjacency matrix model — enabling linear-algebraic operations such as Boolean matrix multiplication for BFS, Kronecker products for automaton intersection, and diagonal-matrix filtering for vertex selection.

## Type Definition

```fsharp
type Graph<'v, 'e> =
    { vertexMap: Map<int, 'v>
      edges: Matrix<'e> }
```

## Module Functions

### Construction
- `fromEdges: 'v list -> Matrix<'e> -> Graph<'v, 'e>` — creates a graph from a list of vertex labels and an edge matrix.

### Accessors
- `vertexCount: Graph<'v, 'e> -> int`
- `vertices: Graph<'v, 'e> -> (int * 'v) list` — all (index, label) pairs, sorted by index
- `tryGetVertex: int -> Graph<'v, 'e> -> 'v option`
- `getVertex: int -> Graph<'v, 'e> -> 'v`
- `edge: Graph<'v, 'e> -> int -> int -> 'e`

### Transformations
- `mapVertices: ('v -> 'w) -> Graph<'v, 'e> -> Graph<'w, 'e>`
- `mapEdges: ('e -> 'f) -> Graph<'v, 'e> -> Graph<'v, 'f>`

### Vertex Removal
- `keepVertices: Set<int> -> Graph<'v, 'e> -> Graph<'v, 'e>` — keeps only specified vertices and edges between them. Indices remapped to 0..|keep|-1 preserving ascending order.

### Generic Graph Filtering
- `filterOutgoingGeneric: zero:'e -> maskOp:(bool -> 'e -> 'e) -> combineOp:('e -> 'e -> 'e) -> Set<int> -> Graph<'v, 'e> -> Graph<'v, 'e>` — keeps outgoing edges from selected vertices via diagonal matrix multiplication.
- `filterIncomingGeneric: zero:'e -> maskOp:('e -> bool -> 'e) -> combineOp:('e -> 'e -> 'e) -> Set<int> -> Graph<'v, 'e> -> Graph<'v, 'e>` — keeps incoming edges to selected vertices.

### Boolean Graph Filtering
- `filterOutgoing: Set<int> -> Graph<'v, bool> -> Graph<'v, bool>`
- `filterIncoming: Set<int> -> Graph<'v, bool> -> Graph<'v, bool>`

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic filter functions parameterized by `zero`/`maskOp`/`combineOp` | Enables filtering on graphs with arbitrary edge types without Boolean decomposition |
| `keepVertices` instead of filter-combinations | Removing vertices automatically removes all incident edges — no separate edge-filtering step needed |
| `keepVertices` preserves ascending order | Remapped indices are deterministic and predictable |

## Integration with Automaton Types

NFA and DFA types wrap Graph for their state/transition storage:

```fsharp
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startStates: Set<int>
      finalStates: Set<int> }
```

This separation follows the book's hierarchy: a graph is a generic structure, and an automaton is a graph with additional start/final state annotations.

## See Also

- [Automaton module](automaton.md) — NFA/DFA wrapping Graph
- [MS-BFS module](msbfs.md) — BFS using Boolean graph adjacency
- [Matrix module](matrix.md) — underlying edge storage
- [SPPF module](sppf.md) — SPPF as a Graph
