# Kronecker-based CFPQ Algorithm

**Tags:** cfpq, context-free, kronecker-product, bfs, rsm, fixed-point, product-construction
**Kind:** algorithm
**Module:** KroneckerCfpq
**Source:** `src/FLPQ.CFPQ/KroneckerCfpq.fs`
**Depends on:** Matrix, LinearAlgebra, Automaton, RSM, MsBfs, CfpqInput, AzimovCfpq
**Book reference:** Chapter 12, TensorProduct.tex section2 and lst:algo1; Chapter 3, 05_BFS.tex

> **Abstract:** Straightforward Kronecker-product-based Context-Free Path Querying over an RSM and a labeled graph. Each fixed-point iteration takes the Kronecker product of the RSM transition matrix and the current edge-label matrix (terminal edges plus nonterminal edges derived so far), accumulates it into the previously accumulated product, runs MS-BFS from every (block start state × vertex) pair, and extracts the newly derived nonterminal edges as the graph for the next iteration. The book's transitive closure is replaced by MS-BFS since only reachability from block start states is needed. Returns per-nonterminal n×n Boolean reachability matrices, the productive steps, and the iteration count.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

Input: an RSM `⟨N, Σ, B, B_S, Q, Q_S⟩` (flat transition matrix `M1 = rsm.Transitions`,
q = |Q| states) and a labeled graph `G = (V, {E_a})` with n = |V| vertices.

1. **Initialize the edge-label matrix** `m2` (n×n over
   `Option<NonEmptySet<AutomatonLabel<RsmSymbol>>>`): lift every terminal edge of `G` to
   the label `ATerm(RTerm(a))`. Nonterminal edges are added by the loop.
2. **Initialize** the accumulated product `prod = 0^(qn × qn)` and the MS-BFS sources
   `S = {(start(A), v) | A ∈ N, v ∈ V}`. Product-state index: `idx(s, v) = s·n + v`; the
   visited row of `(A, i)` is `aIdx·n + i` where `aIdx` is A's position in the
   nonterminal order.
3. **Loop** until an iteration adds no new edges:
   1. `k = kron(M1, m2, Nfa.intersectEdgeSets)` → Boolean mask (the product-graph adjacency).
   2. `prod ← prod ∨ k` (`MsBfs.boolAdd`) — accumulate into the previously accumulated product.
   3. `visited = MsBfs.msBfs(S, prod)`.
   4. For every nonterminal A: `T_A[i,j] = ∃ f ∈ finals(A): visited[aIdx·n + i, idx(f, j)]`;
      for each cell where `T_A[i,j]` is true but `m2[i,j]` lacks the label
      `ATerm(RNonterm A)`, record the changed cell and add the label to `m2`.
   5. If no cells changed, stop; otherwise record a step (changed cells sorted by
      (nonterminal, row, column) plus the per-nonterminal matrix snapshot).
4. **Return** `{ Matrices = T_A per nonterminal; Steps; Iterations }` — `Iterations`
   counts the final no-change application.

A product path from `(start(A), i)` to `(f, j)` with f final in A corresponds exactly to a
derivation `A ⇒* w` where `w` labels a graph path `i ⇝ j` over the current `m2` edges
(terminals plus nonterminals derived so far). Since `m2` only grows, this is a monotone
fixed-point iteration over edge sets; it terminates when an iteration adds nothing.

**Time complexity:** per iteration O(q²·n²) label-set intersections for the Kronecker
product plus MS-BFS on the qn×qn mask from |N|·n sources (O(|N|·n·(qn)²) per BFS round in
the dense implementation, up to qn rounds). The number of iterations is bounded by the
number of distinct nonterminal edges that can be added — at most |N|·n² — plus one final
no-change iteration.

## Type Definitions

### `CfpqKronStep<'nt, 't>`

A productive fixed-point iteration. The first three fields mirror `AzimovCfpq.CfpqStep`
(the changed cells and the per-nonterminal matrix state after the iteration,
`Matrix<bool option>` with `Some true` for reachable cells); the rest carry the
per-iteration data for step-by-step visualization (see
[KroneckerStepViz](kronecker-step-viz.md)):

```fsharp
{ Iteration: int
  ChangedCells: AzimovCfpq.CfpqCell<'nt> list
  Matrices: Map<Nonterminal<'nt>, Matrix<bool option>>
  M1: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t,'nt>>>>>   // RSM transitions (constant)
  M2: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t,'nt>>>>>   // edge-label matrix this iteration
  K: Matrix<bool>                                                       // Kronecker result mask (qn x qn)
  InitialFront: Matrix<bool>                                            // MS-BFS seed front (k x qn)
  FinalFront: Matrix<bool>                                              // MS-BFS visited (k x qn)
  FinalStateCols: Set<int> }                                            // (final f, vertex v) columns
```

### `CfpqKronResult<'nt, 't>`

```fsharp
{ Matrices: Map<Nonterminal<'nt>, Matrix<bool>>   // final T_A per nonterminal
  Steps: CfpqKronStep<'nt, 't> list               // productive iterations only
  Iterations: int }                               // incl. the final no-change one
```

## Function Signatures

### `evaluate: RSM<'t, 'nt> -> CfpqGraph<'t> -> CfpqKronResult<'nt, 't>`

Runs the Kronecker CFPQ fixed point described above.

- **Precondition:** the RSM has no `AEpsilon` transitions (true for all `RsmBuilder`
  output). Epsilon reachability falls out of the MS-BFS source initialization: a source
  pair `(start(A), v)` is visited at itself, so `T_A[v,v]` is true whenever block A's start
  state is final.
- **Degenerate inputs:** n = 0 or q = 0 need no guards — empty sources / 0×0 product yield
  one no-change iteration, empty steps, and empty (or absent) matrices, consistent with
  `AzimovCfpq` behavior.

### `answer: CfpqKronResult<'nt, 't> -> RSM<'t, 'nt> -> Matrix<bool>`

The reachability answer: the start block's matrix (`Map.find rsm.StartBlock result.Matrices`).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| MS-BFS instead of the book's transitive closure | Only reachability *from* block start states is needed to extract `T_A`; MS-BFS from the `(start(A), v)` sources computes exactly that, cheaper than a full closure. |
| No epsilon pre-seeding of `m2` | Zero-length product paths make ε fall out of the MS-BFS source initialization. The result is identical to the book's version; only the iteration count can differ on ε-grammars. |
| Accumulation step `prod ← prod ∨ k` | Monotone no-op in this straightforward version (`m2` only grows, so `k` only grows and `prod = k` after the first iteration). Kept for the loop shape described in the task and as the hook for a future incremental variant à la LAGraph PR #10. |
| Reuse `AzimovCfpq.CfpqStep` / `CfpqCell` | Same step/cell shape as the Azimov algorithm, so per-step TeX rendering (`CfpqMatrixTeX.stepToTeX`) and test comparison helpers work for both algorithms without duplication. |
| Per-nonterminal extraction from visited rows | `T_A[i,j] = ∃ f ∈ finals(A): visited[(start(A), i), (f, j)]` — one MS-BFS run yields every nonterminal's matrix at once; the delta against `m2` gives the productive cells. |
| The step is its own record, not an Azimov alias | Kronecker captures per-iteration data (Kronecker operands/result, MS-BFS fronts) that Azimov does not produce; a separate `CfpqKronStep<'nt,'t>` keeps the Azimov `CfpqStep`/`CfpqCell` types untouched and self-contains each step for rendering. |
| `FinalFront`/`InitialFront` from `MsBfs.msBfsWithTrace` | The MS-BFS loop is factored into `msBfsWithTrace` so the initial (seed) front and the final visited front are both available for the step visualization; `msBfs` projects out the trace. |
| `FinalStateCols` computed once | The accept-reading columns `(final f, vertex v)` are independent of the nonterminal and of the iteration, so they are built once outside the loop and stored (by value) on each step. |

## Book Reference

- Chapter 12, `TensorProduct.tex`, section `section2` ("Тензорное произведение") and
  listing `lst:algo1` — the tensor-product CFPQ fixed point and its worked example
  (S → aSb | ab over a 4-vertex graph; reproduced by the golden test in
  `tests/FLPQ.CFPQ.Tests/KroneckerCfpqTests.fs`).
- Chapter 3, `05_BFS.tex` — MS-BFS (`algo:MS-BFS_linal`) used in place of the transitive
  closure.

## See Also

- [Azimov CFPQ](azimov-cfpq.md) — the grammar-based matrix fixed point this algorithm is compared against
- [Kronecker RPQ](kronecker-rpq.md) — the regular (DFA) special case of the same product construction
- [RSM module](rsm.md) — the recursive state machine input type
- [MS-BFS module](msbfs.md) — multiple-source BFS and `boolAdd`
- [LinearAlgebra module](linear-algebra.md) — the Kronecker product
- [CfpqInput module](cfpq-input.md) — the labeled-graph input type
