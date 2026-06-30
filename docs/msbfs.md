# MS-BFS Module Design

## Overview

The `MsBfs` module in `FLPQ.LinearAlgebra` implements the multiple-source BFS algorithm and supporting Boolean semiring operations. Based on Chapter 3, `05_BFS.tex`.

## Type Definitions

No new types. All operations use the existing `Matrix<bool>` type.

## Function Signatures

### `boolAdd: Matrix<bool> -> Matrix<bool> -> Matrix<bool>`
Boolean semiring addition (⊕_B): element-wise OR (`map2 (||)`).

### `boolMul: Matrix<bool> -> Matrix<bool> -> Matrix<bool>`
Boolean semiring multiplication (⊗_B): matrix-matrix product with AND as multiplication and OR as addition (`mxm (&&) (||) false`).

### `maskFilter: Matrix<bool> -> Matrix<bool> -> Matrix<bool>`
Mask operation (⊕_M): element-wise `nf && not v`. Keeps values from the first operand only where the second is 0. Used to filter BFS front: keep only vertices NOT yet visited.

Truth table: 0⊕0=0, 1⊕1=0, 0⊕1=0, 1⊕0=1.

### `msBfs: int[] -> Matrix<bool> -> Matrix<bool>`
Multiple-source BFS. Performs independent BFS traversals from k starting vertices simultaneously. Returns a k×|V| boolean matrix where row i is the BFS front for source K[i].

Algorithm (`algo:MS-BFS_linal`):
1. Initialize front: for each source i, set `front[i, K[i]] = 1`
2. While front ≠ 0:
   - `visited ← visited ⊕_B front` (accumulate)
   - `new_front ← front ⊗_B M` (propagate via Boolean matrix product)
   - `front ← new_front ⊕_M visited` (filter out already visited)
3. Return visited

## Design Decisions

- All operations expressed through existing generic matrix operations (`map2`, `mxm`) — no ad-hoc loops.
- MS-BFS is in the LinearAlgebra project because it's a pure matrix operation that doesn't depend on language types.
- The `anyTrue` helper is a private function for checking the termination condition.
- Boolean semiring operations are exposed as standalone functions for reuse by RPQ algorithms.

## Relationship to the Book

- Chapter 3, `05_BFS.tex`: MS-BFS algorithm (`algo:MS-BFS_linal`)
- Boolean semiring B = ⟨{0,1}, ∨, ∧⟩
- Mask structure M = ⟨{0,1}, ⊕⟩
