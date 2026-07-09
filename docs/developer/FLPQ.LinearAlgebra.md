# FLPQ.LinearAlgebra

Core library providing generic matrix types and linear algebra operations. No dependencies on other FLPQ projects.

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.LinearAlgebra/`
- **Dependencies**: FSharpPlus

## Modules

| Module | Source | Documentation |
|--------|--------|---------------|
| `Matrix` | `Matrix.fs` | [Matrix module design and logic](matrix.md) |
| `LinearAlgebra` | `LinearAlgebra.fs` | [LinearAlgebra module design and logic](linear-algebra.md) |
| `BooleanDecomposition` | `BooleanDecomposition.fs` | [BooleanDecomposition module design and logic](boolean-decomposition.md) |

## Role

Provides the foundation for all other projects:
- **`Matrix<'a>`** — generic matrix type wrapping `'a[,]` with explicit dimensions
- **`mxm`** — general matrix-matrix multiplication parameterized by semiring operations
- **`kron`** — Kronecker product
- **`BooleanDecomposition.decompose/recompose`** — boolean decomposition of set-valued matrices, used by Valiant and RPQ algorithms

## Book References

- Chapter 1: Matrix and vector definitions, Kronecker product
- Chapter 3: Boolean semiring, boolean decomposition
- Chapter 7: Valiant algorithm (uses boolean decomposition)
