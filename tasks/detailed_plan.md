# Detailed Plan: Tasks 007-008 — Boolean Decomposition and Valiant Algorithm

## Overview

Task 7: Boolean decomposition of matrices over sets — decompose a `Matrix<Set<'a>>` into a family of `Matrix<bool>`, one per distinct element.

Task 8: Valiant's algorithm for CFG parsing — uses Boolean matrix multiplication to achieve subcubic complexity via recursive submatrix decomposition.

## Task 7: Boolean Decomposition

### Book reference
Definition `\label{def:BoolDecomposition}` in `01_BasicDefinitions.tex`:
Given matrix M indexed by (i,j) with set-valued cells, for each element `l`, build a Boolean matrix `M_l` where `M_l[i,j]=1` iff `l ∈ M[i,j]`.

### Functions

```fsharp
val decomposeBy: ('a -> 'a -> bool) -> Matrix<Set<'a>> -> Map<'a, Matrix<bool>>
```
- Collects all elements across all cells
- For each distinct element, creates a `Matrix<bool>` with same dimensions
- Equality via caller-provided comparer (generic)

```fsharp
val recomposeBy: Map<'a, Matrix<bool>> -> Matrix<Set<'a>>
```
- Inverse of decompose: for each (i,j), cell contains set of elements whose Boolean matrix has `true` at (i,j)
- Requires all matrices have same dimensions

### Design decisions
- `Map` requires comparison constraint on key type; use `Map` with a key comparison function or just `list` of pairs
- Simpler: use a list of pairs `(element, Matrix<bool>)` since we don't need fast lookup
- Actually, use `Map` with string keys (already comparable) for the concrete case
- For the generic case, provide the element equality via a function parameter

## Task 8: Valiant Algorithm

### Book reference
Full chapter in `02_Valiant.tex` (Section `\label{sec:Valiant}`).

### Core concepts

**Tables**: T (parsing table), P (auxiliary pair table), both (n+1)×(n+1) triangular.

**Submatrix**: defined by vertex (a,b) and size s. Cells: `a-s < i ≤ a` and `b ≤ j < b+s`. Square of size s×s.

**Boolean decomposition representation**:
- T as `Map<Nonterminal<string>, Matrix<bool>>` (one Boolean matrix per nonterminal)
- P as `Map<Nonterminal<string>*Nonterminal<string>, Matrix<bool>>` (one per pair)

**Key operations**:
- `bottomSubmatrix`, `leftSubmatrix`, `rightSubmatrix`, `topSubmatrix` — split into 4
- `rightGrounded`, `leftGrounded` — shift to diagonal
- `performMultiplications` — Boolean mxm of submatrices, store in P
- `complete(m)` — recursive fill of submatrix m
- `compute(i,j)` — recursive top-level

### Submatrix helpers

```fsharp
type Submatrix = { A: int; B: int; Size: int }

val cells: Submatrix -> (int*int) list
val extractBoolMatrix: Matrix<bool> -> Submatrix -> Matrix<bool>
val bottomSubmatrix: Submatrix -> Submatrix
val leftSubmatrix: Submatrix -> Submatrix
...
```

### Boolean matrix slices

Given the full (n+1)×(n+1) Boolean matrix for a nonterminal N, extract the s×s slice for a submatrix:
- `(extracted)[x,y] = full[a-size+x+1, b+y]` for x,y ∈ [0, s-1]

### Multiplication step

For each pair (B,C) ∈ N×N:
1. Extract B's Boolean s×s matrix from m1's region of T
2. Extract C's Boolean s×s matrix from m2's region of T
3. Compute product via `LinearAlgebra.mxm` with Boolean semiring (AND, OR, false)
4. Store result into P at m's region for pair (B,C) — OR into existing values

### Complete procedure

```
complete(m):
  if size(m)==1 and (a-size+1)+1 == b:  // diagonal cell
    fill T[a-s+1, b] from terminal rules
  elif size(m)==1:
    fill T[a-s+1, b] from P[a-s+1, b] pairs: if (B,C)∈P and A→BC∈R, add A to T
  else:
    B = bottomSubmatrix(m), L = leftSubmatrix(m), R = rightSubmatrix(m), T = topSubmatrix(m)
    complete(B)
    performMultiplications([(L, leftGrounded(L), B)])
    complete(L)
    performMultiplications([(R, B, rightGrounded(R))])
    complete(R)
    performMultiplications([(T, leftGrounded(T), R)])
    performMultiplications([(T, L, rightGrounded(T))])
    complete(T)
```

### Top-level

```
valiantParse(grammar, input):
  n = length(input)
  Pad n+1 to next power of 2 if needed
  Initialize T (n+1)×(n+1) with false (Boolean decomposition: one per nonterminal)
  Initialize P (n+1)×(n+1) with false (Boolean decomposition: one per pair of productions)
  call compute(n+1, n+1) — or just complete(mainSubmatrix)
  return T[0,n] contains start symbol
```

### Property test

```fsharp
[<Property>]
let ``Valiant and CYK return identical results`` (grammar, input) =
    Cyk.parse grammar input = Valiant.parse grammar input
```

## Files

| File | Action |
|------|--------|
| `src/FLPQ.Core/BooleanDecomposition.fs` | Create — decomposing/recomposing set-valued matrices |
| `src/FLPQ.Core/Valiant.fs` | Create — Valiant parsing algorithm |
| `src/FLPQ.Core/FLPQ.Core.fsproj` | Modify — add both files |
| `tests/FLPQ.Core.Tests/ValiantTests.fs` | Create — tests |
| `tests/FLPQ.Core.Tests/FLPQ.Core.Tests.fsproj` | Modify |
| `docs/boolean-decomposition.md` | Create |
| `docs/valiant.md` | Create |
| `docs/main.md`, `docs/architecture.md` | Modify |
| `tasks/tasks.md` | Mark tasks 7,8 done |
