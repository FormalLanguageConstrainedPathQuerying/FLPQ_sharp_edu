# FLPQ.GraphAnalysis

**Tags:** graph, ms-bfs, bfs, boolean, matrix-multiplication, semiring
**Kind:** hub
**Source:** `src/FLPQ.GraphAnalysis/`
**Depends on:** FLPQ.LinearAlgebra
**Used by:** FLPQ.Languages, FLPQ.RPQ
**Book reference:** Chapters 3, 11

> **Abstract:** Graph analysis library providing the generic `Graph<'s,'e>` type (labeled graph with vertices in a map and edges in a matrix), MS-BFS (multiple-source breadth-first search expressed as linear-algebraic operations), and Boolean/mask semiring operations. Depends on `FLPQ.LinearAlgebra` for matrix types and operations.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Role](#role)
- [Book References](#book-references)
- [See Also](#see-also)

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.GraphAnalysis/`
- **Dependencies**: `FLPQ.LinearAlgebra`

## Modules

| Module | Source | Documentation |
|--------|--------|---------------|
| `Graph` | `Graph.fs` | [Graph module design and logic](graph.md) |
| `MsBfs` | `MsBfs.fs` | [MS-BFS and matrix operations module design and logic](msbfs.md) |

## Role

Provides graph infrastructure used by languages and RPQ algorithms:
- **Graph** — generic graph type with vertices in a map and edges in a matrix. NFA/DFA types wrap this graph. Provides edge filtering via Boolean matrix multiplication with diagonal matrices.
- **MS-BFS** — multiple-source BFS expressed as linear-algebraic operations: front propagation via Boolean matrix multiplication, filtered by inverted mask semiring
- **Boolean semiring** operations (`⊕_B`, `⊗_B`) — element-wise OR and Boolean matrix product
- **Mask semiring** operation (`⊕_M`) — inverted mask filtering

## Book References

- Chapter 3: MS-BFS algorithm, Boolean semiring, mask semiring
- Chapter 11: Kronecker-based RPQ (uses MS-BFS)

## See Also

- [Graph module](graph.md) — graph type, vertex/edge operations, filtering
- [MS-BFS module](msbfs.md) — multiple-source BFS algorithm
