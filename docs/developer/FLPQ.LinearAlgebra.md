# FLPQ.LinearAlgebra

**Tags:** linear-algebra, matrix, boolean-decomposition, kronecker-product, sparse-matrix
**Kind:** hub
**Source:** `src/FLPQ.LinearAlgebra/`
**Depends on:** FSharpPlus
**Used by:** FLPQ.GraphAnalysis, FLPQ.Languages, FLPQ.RPQ, FLPQ.Printers
**Book reference:** Chapters 1, 3, 7

> **Abstract:** Core library providing generic matrix types and linear algebra operations: `Matrix<'a>` (generic 2D matrix), `mxm` (parameterized matrix multiplication), `kron` (Kronecker product), and `BooleanDecomposition` (decompose/recompose set-valued matrices into Boolean vectors). No dependencies on other FLPQ projects. Foundation for all other FLPQ modules.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Role](#role)
- [Book References](#book-references)
- [See Also](#see-also)

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

## See Also

- [Matrix module](matrix.md) — matrix type, TeX printing, styled printing
- [LinearAlgebra module](linear-algebra.md) — semiring operations, Kronecker product
- [BooleanDecomposition module](boolean-decomposition.md) — decompose/recompose set matrices
