# LinearAlgebra Module

## Module Purpose

Provides generic linear algebra operations over the `Matrix<'a>` type: matrix-matrix multiplication (`mxm`) and Kronecker product (`kron`). All operations are parameterized by the semiring operations (`opMult`, `opAdd`, `zero`), enabling use over arbitrary semirings (numeric, Boolean, tropical, etc.).

## Function Signatures

### `mxm` — Generic Matrix-Matrix Multiplication

```fsharp
val mxm:
    a: Matrix<'a> ->
    b: Matrix<'b> ->
    opMult: ('a -> 'b -> 'c) ->
    opAdd: ('c -> 'c -> 'c) ->
    zero: 'c ->
    Matrix<'c>
```

Classical triple-nested loop multiplication. For each cell `(i, j)` of the result, computes the sum over `k` of `opMult(a[i,k], b[k,j])`, starting from `zero` and accumulating with `opAdd`.

**Preconditions**:
- `a.cols = b.rows`. If not, throws `ArgumentException`.

**Postcondition**:
- Result has dimensions `a.rows × b.cols`.
- The operation is associative when `opMult` distributes over `opAdd` and `opAdd` is associative and commutative.

**Time complexity**: `O(a.rows * b.cols * a.cols)`.

### `kron` — Kronecker Product

```fsharp
val kron:
    a: Matrix<'a> ->
    b: Matrix<'b> ->
    opMult: ('a -> 'b -> 'c) ->
    zero: 'c ->
    Matrix<'c>
```

Computes the Kronecker (tensor) product `A ⊗ B`. Each element `a[i,j]` of A is replaced by the block `opMult(a[i,j], B)`.

Formally: element at global position `(i, j)` in the result equals `opMult(a[i / b.rows, j / b.cols], b[i % b.rows, j % b.cols])`.

**Postcondition**:
- Result has dimensions `(a.rows * b.rows) × (a.cols * b.cols)`.

**Time complexity**: `O(a.rows * a.cols * b.rows * b.cols)`.

### Note on `zero` Parameter

The `zero` parameter in `kron` is included for signature consistency with `mxm`. It is not used in the basic implementation but is available for potential extensions.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Generic semiring operations as function parameters | Enables use over numeric, Boolean, tropical, and other semirings without boxing or interfaces |
| `mxm` uses mutable accumulator in innermost loop | Clear, direct implementation matching textbook descriptions; avoids functional overhead |
| `kron` computes indices via integer division/modulo | Direct mapping to Kronecker product definition; no intermediate block allocation |
| `zero` parameter in `kron` | Consitency with `mxm` signature; task specification requirement |

## Property-Based Test Invariants

1. **Kronecker product with 1×1 matrix is equivalent to `map`**: For any scalar value `v` and matrix B, `kron (init 1 1 v) B (*) 0 = map (fun x -> v * x) B`.

2. **Identity matrix property for `mxm`**: For any square matrix A, `mxm I A (*) (+) 0 = A` and `mxm A I (*) (+) 0 = A`, where I is the identity matrix of matching size.

3. **Transpose of product equals product of transposes in reverse order**: `transpose (mxm a b (*) (+) 0) = mxm (transpose b) (transpose a) (*) (+) 0`, where a.cols = b.rows.

## Relationship to the Book

Generic matrix multiplication over semirings is a fundamental building block for algorithms that compute transitive closures and solve formal language constrained reachability problems. The Kronecker product is used in constructing larger automata from smaller components.
