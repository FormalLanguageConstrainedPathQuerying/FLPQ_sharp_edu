# FLPQ.GraphAnalysis

Graph analysis library providing generic graph type, MS-BFS, and Boolean/Mask semiring operations. Depends on `FLPQ.LinearAlgebra`.

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
