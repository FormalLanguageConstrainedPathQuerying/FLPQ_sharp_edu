# Detailed Plan: Task 003 — Linear Algebra over Generic Matrices

## Overview
Add `LinearAlgebra` module with generic matrix-matrix multiplication (`mxm`) and Kronecker product (`kron`).

## 1. Module: `LinearAlgebra.fs`

### 1.1 `mxm` — Generic Matrix-Matrix Multiplication

```fsharp
val mxm:
    a: Matrix<'a> ->
    b: Matrix<'b> ->
    opMult: ('a -> 'b -> 'c) ->
    opAdd: ('c -> 'c -> 'c) ->
    zero: 'c ->
    Matrix<'c>
```

Classical triple-nested loop algorithm:
- Precondition: `a.cols = b.rows`. Throw `ArgumentException` if not.
- Result dimensions: `a.rows × b.cols`.
- For each `(i, j)`: initialize accumulator as `zero`, then for `k` in `0 .. a.cols-1`: accumulator = opAdd(accumulator, opMult(a[i,k], b[k,j])).

### 1.2 `kron` — Kronecker Product

```fsharp
val kron:
    a: Matrix<'a> ->
    b: Matrix<'b> ->
    opMult: ('a -> 'b -> 'c) ->
    zero: 'c ->
    Matrix<'c>
```

- Result dimensions: `(a.rows * b.rows) × (a.cols * b.cols)`.
- Element at `(i * b.rows + r, j * b.cols + s)` = `opMult(a[i,j], b[r,s])`.
- `zero` parameter included per task spec (may not be used in basic implementation but provided for consistency).

## 2. Tests: `LinearAlgebraTests.fs`

### 2.1 Property Tests

1. **Kronecker product with 1×1 matrix is equivalent to map**:
   For 1×1 matrix A and any B: `kron A B opMult zero = map (opMult A[0,0]) B`.

2. **Identity matrix property for mxm**:
   For square matrix A, there exists identity matrix I such that:
   `mxm A I (*) (+) 0 = A` and `mxm I A (*) (+) 0 = A`.

3. **Transpose of product equals product of transposes in reverse order**:
   `transpose (mxm a b opMult opAdd zero) = mxm (transpose b) (transpose a) opMult opAdd zero`.

### 2.2 Unit Tests

- `mxm` throws when `a.cols ≠ b.rows`.
- `mxm` produces correct result dimensions.
- `kron` produces correct result dimensions.
- `kron` with 1×1 matrix produces correct values.
- `mxm` with identity matrix returns original.
- `mxm` computes known product correctly (e.g., 2×3 times 3×2).

## 3. Documentation: `docs/linear-algebra.md`

- Type signatures with pre/post-conditions.
- Algorithm descriptions (triple-nested loop, block-based Kronecker).
- Relationship to matrix module.

## 4. Files to Create/Modify

| File | Action |
|------|--------|
| `src/FLPQ.Core/LinearAlgebra.fs` | Create — mxm and kron implementations |
| `src/FLPQ.Core/FLPQ.Core.fsproj` | Modify — add `LinearAlgebra.fs` to compile list |
| `src/FLPQ.Core/Library.fs` | Modify — keep as namespace placeholder |
| `tests/FLPQ.Core.Tests/LinearAlgebraTests.fs` | Create — property and unit tests |
| `tests/FLPQ.Core.Tests/FLPQ.Core.Tests.fsproj` | Modify — add `LinearAlgebraTests.fs` |
| `docs/linear-algebra.md` | Create — module documentation |
| `docs/main.md` | Modify — add link |
| `docs/architecture.md` | Modify — update module table |
| `tasks/tasks.md` | Modify — mark task 3 as done |

## 5. Implementation Order
1. Write detailed plan (this file)
2. Create `LinearAlgebra.fs` with `mxm` and `kron`
3. Add `LinearAlgebra.fs` to `FLPQ.Core.fsproj`
4. Create `LinearAlgebraTests.fs` with property and unit tests
5. Add `LinearAlgebraTests.fs` to `FLPQ.Core.Tests.fsproj`
6. Build and run tests
7. Format code
8. Create `docs/linear-algebra.md`
9. Update `docs/main.md` and `docs/architecture.md`
10. Update `tasks/tasks.md`
11. Merge to dev
