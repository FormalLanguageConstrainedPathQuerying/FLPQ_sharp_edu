# FLPQ.GraphAnalysis

Graph analysis library providing MS-BFS and Boolean/Mask semiring operations. Depends on `FLPQ.LinearAlgebra`.

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.GraphAnalysis/`
- **Dependencies**: `FLPQ.LinearAlgebra`

## Modules

| Module | Source | Documentation |
|--------|--------|---------------|
| `MsBfs` | `MsBfs.fs` | [MS-BFS and matrix operations module design and logic](msbfs.md) |

## Role

Provides graph traversal utilities used by RPQ algorithms:
- **MS-BFS** — multiple-source BFS expressed as linear-algebraic operations: front propagation via Boolean matrix multiplication, filtered by inverted mask semiring
- **Boolean semiring** operations (`⊕_B`, `⊗_B`) — element-wise OR and Boolean matrix product
- **Mask semiring** operation (`⊕_M`) — inverted mask filtering

## Book References

- Chapter 3: MS-BFS algorithm, Boolean semiring, mask semiring
- Chapter 11: Kronecker-based RPQ (uses MS-BFS)
