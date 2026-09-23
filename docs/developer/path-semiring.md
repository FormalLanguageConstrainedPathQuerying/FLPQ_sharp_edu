# PathSemiring Module

**Tags:** regular, rpq, matrix, semiring, path-index
**Kind:** data-structure
**Module:** PathSemiring
**Source:** `src/FLPQ.RPQ/PathSemiring.fs`
**Depends on:** Matrix (FLPQ.LinearAlgebra)
**Used by:** ArroyueloRPQ (trace), BelyaninRPQ (trace)
**Book reference:** Chapter 11, Section sec:RPQ_BFS, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Path semiring over sets of vertex sequences — the book's path semiring carrier 2^{V\*} applied to matrix-based RPQ evaluation. A matrix cell holds the set of paths (vertex sequences, endpoints included) witnessing reachability between its row and column vertices. Only simple paths are stored, which keeps cells finite on cyclic graphs; concatenation drops non-simple results.

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

A matrix over the path semiring has cells of type `Set<int list>`: each cell `[i, j]` stores the set of paths from vertex `i` to vertex `j`. A path is a vertex sequence with endpoints included (`[i; ...; j]`).

Semiring operations:

- **Zero** — the empty set of paths.
- **Addition** — set union of cells.
- **Multiplication** — path concatenation: `p1` and `p2` combine when `last p1 = head p2`; the shared vertex is written once (`p1 @ List.tail p2`), and the result is kept only when it is simple.

Well-formedness invariant: every path in cell `[i, j]` starts at `i` and ends at `j`. All operations preserve the invariant; `mulCells` is total (empty paths contribute nothing).

## Type Definitions

No dedicated path type: a path is a plain `int list` (vertex sequence). Matrix cells are `Set<int list>`; matrices are `Matrix<Set<int list>>` from FLPQ.LinearAlgebra.

## Module Functions

```fsharp
val zero: Set<int list>
val isZero: Set<int list> -> bool
val isSimple: int list -> bool
val extend: int list -> int -> int list option
val mulCells: Set<int list> -> Set<int list> -> Set<int list>
val addMatrices: Matrix<Set<int list>> -> Matrix<Set<int list>> -> Matrix<Set<int list>>
val mxm: Matrix<Set<int list>> -> Matrix<Set<int list>> -> Matrix<Set<int list>>
val boolSelectRows: Matrix<bool> -> Matrix<Set<int list>> -> Matrix<Set<int list>>
val boolExtendCols: Matrix<Set<int list>> -> Matrix<bool> -> Matrix<Set<int list>>
val identity: int -> Matrix<Set<int list>>
val transitiveClosure: Matrix<Set<int list>> -> Matrix<Set<int list>>
val isEmpty: Matrix<Set<int list>> -> bool
val allPaths: Matrix<Set<int list>> -> Set<int list>
val booleanProjection: Matrix<Set<int list>> -> Matrix<bool>
```

- `zero` / `isZero` — the additive identity and its test.
- `isSimple` — no vertex occurs twice in the path.
- `extend p v` — append `v` to `p`, returning `None` when `v` already occurs (would break simplicity).
- `mulCells a b` — cell product: concatenate every pair with matching endpoints, keep only simple results.
- `addMatrices` — cell-wise union.
- `mxm` — matrix product over the path semiring, built on `LinearAlgebra.mxm`.
- `boolSelectRows n m` — `(N^T) ⊗ M`: result `[q', v]` is the union of `M[q, v]` over all `q` with `N[q, q'] = true`. The automaton-side step of Belyanin's propagation (book: `(N^a)^T ⊗ M`).
- `boolExtendCols m g` — `M ⊗ G`: result `[q, v']` is the union, over all `v` with `G[v, v'] = true`, of each path in `M[q, v]` extended by `v'` (non-simple extensions dropped). The graph-side step of Belyanin's propagation (book: `⊗ G^a`).
- `identity n` — the trivial path `[i]` on the diagonal, empty cells elsewhere.
- `transitiveClosure m` — repeated squaring of `I + M` (the same loop shape as the Boolean transitive closure in ArroyueloRPQ). After the loop, cell `[i, j]` holds every simple path from `i` to `j` that is a concatenation of paths from `M` (padded with trivial paths to a power of two).
- `isEmpty m` — every cell is empty.
- `allPaths m` — union of all cells: every path stored anywhere in the matrix.
- `booleanProjection m` — a cell is true when it holds at least one path.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Only simple paths are stored | Cells must be finite on cyclic graphs; matches the book's `I_simple` semantics (02_BFS.tex). Consequence: on cyclic graphs the boolean projection may be a strict subset of full reachability (labelled walks that revisit vertices are excluded); on acyclic graphs every walk is simple, so the projection equals the Boolean result |
| Concatenation drops non-simple results | Keeps the invariant that cells hold only simple paths; exactly the book's example where candidate `(v1, v2, v1)` is dropped |
| `transitiveClosure` reuses the Boolean squaring loop | One implementation shape for both semirings: after `k` squarings the matrix contains all (simple) paths expressible with at most `2^k` segments; the loop runs until `2^k >= n`, which covers every simple path |
| `boolSelectRows` / `boolExtendCols` live here, not in BelyaninRPQ | They are semiring-level operations (boolean × path matrix) shared by the Belyanin trace (task 281); keeping them in the semiring module avoids a second copy of the propagation algebra |
| No path type alias — cells are plain `Set<int list>` | The module composes with existing matrix code (e.g. ArroyueloRPQ's trace steps) without conversions; a PascalCase alias would risk colliding with `System.IO.Path` in consumers that open both |

## Book Reference

Chapter 11, `02_BFS.tex`: Section sec:RPQ_BFS (path semiring carrier 2^{V\*}, `I_simple` semantics), Algorithm algo:RPQ_BFS_semiring (propagation step `(N^a)^T ⊗ M ⊗ G^a`), figure in subsec:RPQ_BFS_example (matrix cells holding vertex sequences).

## See Also

- [Arroyuelo RPQ module](arroyuelo-rpq.md) — matrix-based regex evaluation over this semiring
- [Belyanin RPQ module](belyanin-rpq.md) — BFS propagation using `boolSelectRows` / `boolExtendCols`
- [FLPQ.RPQ hub](FLPQ.RPQ.md)
