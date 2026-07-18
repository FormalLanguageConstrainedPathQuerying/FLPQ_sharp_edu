# MS-BFS Algorithm

**Tags:** algorithm, graph, bfs, matrix-multiplication, boolean, semiring
**Kind:** algorithm
**Module:** MsBfs
**Source:** `src/FLPQ.GraphAnalysis/MsBfs.fs`
**Depends on:** Matrix
**Used by:** BelyaninRPQ, ArroyueloRPQ, KroneckerRPQ, Automaton, Nfa
**Book reference:** Chapter 3, Section 05_BFS.tex, Algorithm algo:MS-BFS_linal

> **Abstract:** Implements the Multiple-Source Breadth-First Search (MS-BFS) algorithm expressed as linear-algebraic operations. Performs independent BFS traversals from k starting vertices simultaneously using Boolean matrix multiplication for front propagation and mask-filtering for visited-set management. Returns a k×|V| boolean matrix where row i is the visited set for source K[i]. Also provides Boolean semiring operations (⊕_B, ⊗_B) and mask filtering (⊕_M).

## Contents

- [Algorithm](#algorithm)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

Algorithm `algo:MS-BFS_linal`:

1. Initialize front: for each source i, set `front[i, K[i]] = 1`
2. While front ≠ 0:
   - `visited ← visited ⊕_B front` (accumulate visited vertices)
   - `new_front ← front ⊗_B M` (propagate via Boolean matrix product — find neighbors)
   - `front ← new_front ⊕_M visited` (filter out already visited vertices)
3. Return visited

**Time complexity:** O(k · d · |V|) where k is the number of sources, d is the BFS diameter.
**Space complexity:** O(k · |V|)

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

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| All operations via existing generic matrix operations (`map2`, `mxm`) | No ad-hoc loops; consistent functional style |
| Boolean semiring operations as standalone functions | Reused by Belyanin, Arroyuelo, and Kronecker RPQ algorithms |
| MS-BFS in `FLPQ.GraphAnalysis` | Grouped with graph-related operations alongside the `Graph` module |
| Private `anyTrue` helper | Clean termination condition check in the BFS loop |

## Book Reference

Chapter 3, `05_BFS.tex`: MS-BFS algorithm (`algo:MS-BFS_linal`). Boolean semiring B = ⟨{0,1}, ∨, ∧⟩. Mask structure M = ⟨{0,1}, ⊕⟩.

## See Also

- [Belyanin RPQ](belyanin-rpq.md) — uses MS-BFS Boolean semiring operations
- [Arroyuelo RPQ](arroyuelo-rpq.md) — uses MS-BFS Boolean semiring operations
- [Kronecker RPQ](kronecker-rpq.md) — uses MS-BFS for reachability filtering
- [Graph module](graph.md) — adjacency matrix and vertex operations
