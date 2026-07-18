# LinearAlgebra Module

**Tags:** data-structure, linear-algebra, matrix-multiplication, kronecker-product, semiring
**Kind:** data-structure
**Module:** LinearAlgebra
**Source:** `src/FLPQ.LinearAlgebra/LinearAlgebra.fs`
**Depends on:** Matrix
**Used by:** Graph, Automaton, KroneckerRPQ, Nfa
**Book reference:** Chapter 1, Section 07_MatricesAndVectors.tex

> **Abstract:** Provides generic linear algebra operations over the `Matrix<'a>` type: matrix-matrix multiplication (`mxm`) and Kronecker product (`kron`). All operations are parameterized by semiring operations (`opMult`, `opAdd`, `zero`), enabling use over arbitrary semirings (numeric, Boolean, tropical, set-based, etc.). No boxing or interfaces — pure function parameters.

## Contents

- [Data Structure](#data-structure)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Property-Based Test Invariants](#property-based-test-invariants)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

The LinearAlgebra module operates purely on `Matrix<'a>` values — it defines no new types. Its role is to provide generic operations that other modules use by supplying appropriate semiring parameters:
- **Boolean semiring**: `mxm (&&) (||) false` — used by MS-BFS, Graph filtering
- **Set-based semiring**: `mxm setMult Set.union Set.empty` — used by Valiant
- **Numeric semiring**: `mxm (*) (+) 0` — used in property tests

## Module Functions

### `mxm` — Generic Matrix-Matrix Multiplication
```fsharp
val mxm:
    a: Matrix<'a> -> b: Matrix<'b> ->
    opMult: ('a -> 'b -> 'c) -> opAdd: ('c -> 'c -> 'c) -> zero: 'c ->
    Matrix<'c>
```
Classical triple-nested loop multiplication. For each cell `(i,j)`, computes the sum over `k` of `opMult(a[i,k], b[k,j])`, starting from `zero` and accumulating with `opAdd`.

**Preconditions:** `a.cols = b.rows`. **Time complexity:** O(a.rows · b.cols · a.cols).

### `kron` — Kronecker Product
```fsharp
val kron:
    a: Matrix<'a> -> b: Matrix<'b> ->
    opMult: ('a -> 'b -> 'c) -> zero: 'c ->
    Matrix<'c>
```
Computes the Kronecker (tensor) product A ⊗ B. Each element `a[i,j]` of A is replaced by the block `opMult(a[i,j], B)`.

**Postcondition:** Result has dimensions `(a.rows * b.rows) × (a.cols * b.cols)`. **Time complexity:** O(a.rows · a.cols · b.rows · b.cols).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic semiring operations as function parameters | Enables use over numeric, Boolean, tropical, and other semirings without boxing or interfaces |
| `mxm` uses mutable accumulator in innermost loop | Clear, direct implementation matching textbook descriptions; avoids functional overhead |
| `kron` computes indices via integer division/modulo | Direct mapping to Kronecker product definition; no intermediate block allocation |
| `zero` parameter in `kron` | Consistency with `mxm` signature |

## Property-Based Test Invariants

1. **Kronecker product with 1×1 matrix is equivalent to `map`**: `kron (init 1 1 v) B (*) 0 = map (fun x -> v * x) B`.

2. **Identity matrix property for `mxm`**: `mxm I A (*) (+) 0 = A` and `mxm A I (*) (+) 0 = A`.

3. **Transpose of product**: `transpose (mxm a b (*) (+) 0) = mxm (transpose b) (transpose a) (*) (+) 0`.

## Book Reference

Generic matrix multiplication over semirings is a fundamental building block for algorithms that compute transitive closures and solve formal language constrained reachability problems. The Kronecker product is used in constructing larger automata from smaller components.

Chapter 1, Section 07_MatricesAndVectors.tex.

## See Also

- [Matrix module](matrix.md) — Matrix type, creation, TeX printing
- [BooleanDecomposition module](boolean-decomposition.md) — decompose/recompose
- [Graph module](graph.md) — uses mxm for Boolean matrix operations
- [MS-BFS module](msbfs.md) — Boolean semiring mxm for BFS propagation
