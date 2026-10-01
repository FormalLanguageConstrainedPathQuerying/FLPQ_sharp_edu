# Belyanin's LARPQ Algorithm

**Tags:** algorithm, rpq, regular, bfs, automaton, matrix-multiplication, boolean-decomposition, path-semiring
**Kind:** algorithm
**Module:** BelyaninRPQ
**Source:** `src/FLPQ.RPQ/BelyaninRPQ.fs`
**Depends on:** Matrix, Graph, Automaton, BooleanDecomposition, MsBfs, PathSemiring
**Used by:** FLPQ.Cli (BelyaninSimplePathRunner, BelyaninReachabilityRunner), BelyaninSimplePathStepVisualizer, BelyaninReachabilityStepVisualizer
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Implements Belyanin's LARPQ algorithm — a BFS-based Regular Path Querying algorithm. Accepts a DFA query and a labeled graph (as NFA). Operates on two |Q|×|V| matrices (frontier M and accumulated results P), propagating through simultaneous automaton backward + graph forward transitions. Two semantics share the module: **reachability** (Boolean) computes the classical RPQ answer and is complete on cyclic graphs — `evaluate` (all sources) and `evaluateReachabilityWithTrace` (single-source trace); **simplePath** (path semiring) stores sets of simple vertex paths for path-rendering visualization — `evaluateSimplePathWithTrace`.

## Contents

- [Algorithm](#algorithm)
- [Semantics](#semantics)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

Operates on two |Q|×|V| matrices: front M and accumulated results P.

1. Build per-label matrices N^a (automaton transitions) and G^a (graph adjacency).
2. Initialize M: for each start state q_s ∈ Q_S, set M[q_s, v_s] = 1.
3. While M ≠ 0:
   - M ← I^P_reach(M) (mask: drop already found (q,v) pairs)
   - P ← P ⊕\_B M (accumulate in Boolean semiring)
   - M ← Σ_a (N^a)^T ⊗\_B M ⊗\_B G^a (propagate: automaton backward + graph forward)
4. Result: F ⊗\_B P where F selects final states — returns a boolean vector of reachable vertices.

**Time complexity:** O(|Q|·|V|·d·|Σ|) where d is the BFS diameter.

## Semantics

Two classical RPQ semantics share this module; the CLI selects one with `--semantics`
(`-semantics`):

- **reachability** (default) — Boolean BFS reachability, the classical RPQ answer. A vertex
  is reachable when some accepting walk reaches it, so the algorithm is complete on cyclic
  graphs. Functions: `evaluate` (all sources, the authoritative answer) and
  `evaluateReachabilityWithTrace` (single-source Boolean trace). Consumers:
  `BelyaninReachabilityRunner` and [BelyaninReachabilityStepVisualizer](belyanin-reachability-step-viz.md).
- **simplePath** — the path-semiring semantics: a vertex is reachable only when an accepting
  *simple* path reaches it. Functions: `evaluateSimplePathWithTrace`. Consumers:
  `BelyaninSimplePathRunner` and [BelyaninSimplePathStepVisualizer](belyanin-step-viz.md).

The simple-path trace cannot witness a walk that revisits a vertex (`I_simple`), so on
cyclic graphs it may report fewer reachable vertices than the Boolean reachability
algorithm. This is the defining difference between the two semantics, not a defect of the
trace; the earlier default (deriving the answer from the simple-path trace) was wrong for
the classical reachability semantics and is fixed by the `reachability` default.

## Function Signatures

### `evaluate: DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`

Run Belyanin's RPQ algorithm.

- Input: DFA (query automaton) and graph as NFA.
- Output: |sources| × |V| boolean matrix where each row indicates reachable vertices from the corresponding source vertex.
- Sources are taken from the NFA's start states. Per-label graph matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`.

### `evaluateReachabilityWithTrace: DFA<'t, int> -> NFA<'t, int> -> BelyaninReachabilityTraceStep<'t> list * Matrix<bool>`

Run the Boolean (reachability) algorithm over a single source — the first start vertex,
sorted — recording one trace step per main-loop iteration plus an initialization step.
Returns the steps and the final accumulated Boolean matrix P. Cells are `bool` (reachable /
not reachable) and no path information is collected, so the traversal is complete on
cyclic graphs. The trace is single-source because Boolean cells do not identify their
origin; the result for all sources is produced by `evaluate`.

- `BelyaninReachabilityTraceStep { Index; IsInit; M; P; Labels; NewM }` — Boolean
  `|Q|×|V|` matrices; the initialization step has `NewM = M`, empty `P`, and no labels.
- `BelyaninReachabilityLabelStep { Label; N; G; Select; Extend }` — Boolean matrices;
  Select = (N^a)^T ⊗ M via `MsBfs.boolMul` on the transposed N^a, Extend = Select ⊗ G^a.

### `evaluateSimplePathWithTrace: DFA<'t, int> -> NFA<'t, int> -> BelyaninTraceStep<'t> list * Matrix<Set<int list>>`

Run the algorithm over the [path semiring](path-semiring.md) — the **simplePath**
semantics: a vertex is reported reachable only when an accepting *simple* path reaches it.
Records one trace step per main-loop iteration (plus an initialization step). Returns the
steps and the final accumulated matrix P. All sources share one traversal; a path's head
identifies its source.

- `BelyaninTraceStep { Index; IsInit; M; P; Labels; NewM }` — M is the frontier after the
  mask, P the accumulated result, Labels the per-label products for labels with a
  non-empty Select, NewM the next frontier.
- `BelyaninLabelStep { Label; N; G; Select; Extend }` — N is the DFA transition matrix
  N^a and G is the graph edge matrix G^a (stored so the trace is self-contained for
  rendering the explicit per-label products); Select = (N^a)^T ⊗ M (automaton backward
  step via `PathSemiring.boolSelectRows`), Extend = Select × G^a (graph forward step via
  `PathSemiring.boolExtendCols`, which drops non-simple extensions — the book's I_simple).

### `reachableFromPaths: DFA<'t, int> -> Matrix<Set<int list>> -> int array -> (int * int list) list`

Per-source reachable vertices from the accumulated matrix P: v is reachable from source s
when some final state holds a path from s to v. Sources and vertex lists are sorted.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Sources from NFA start states | Runs the single-source algorithm per source, stacks results |
| Per-label matrices via `BooleanDecomposition` | Maps automaton and graph transitions to boolean matrices by label |
| Uses `MsBfs.boolAdd`, `MsBfs.boolMul`, `MsBfs.maskFilter` | Reuses Boolean semiring operations from the MS-BFS module |
| Index-based unary operator via `maskFilter` | Filtering out already accumulated (q,v) pairs from P |
| simplePath trace: combined multi-source run | All sources share one traversal — M[q, v] holds paths from any source; a path's head identifies its source. Paths from different sources never interfere (distinct vertex sequences, DFA determinism), so the combined run is the union of the per-source runs and still terminates in at most \|V\|-1 iterations |
| simplePath trace: path-level mask | M \\ P drops exact path duplicates, not whole (q,v) pairs — pair-level masking would drop unprocessed simple paths sharing a cell. Each simple path is produced exactly once, so the mask is defensive; kept for book fidelity (I^P_reach) |
| simplePath trace: cells store simple paths only | Every cell is finite on cyclic graphs and the loop terminates in at most \|V\|-1 iterations. Consequence: on cyclic graphs the Boolean projection may be a strict subset of `evaluate`'s reachability (walks revisiting a vertex are excluded by I_simple); on acyclic graphs the two coincide — tested as a property |
| Reachability trace: Boolean cells, single source | Cells are `bool`, so the traversal is complete on cyclic graphs; without path heads a cell cannot identify its source, so the trace covers only the first start vertex. `evaluate` remains the all-sources answer |
| reachability answer uses `evaluate` | The reachability runner derives its answer from the Boolean `evaluate` (all sources), never from the simple-path trace — the trace only drives the step visualization |

## Book Reference

Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring`; the path semiring carrier
2^{V\*} with the index operator I_simple (book example figure
`figures/02_BFS/algorithm_step.tex` shows paths in matrix cells).

## See Also

- [Arroyuelo RPQ](arroyuelo-rpq.md) — matrix-based regex evaluation
- [Belyanin simple-path step visualization](belyanin-step-viz.md) — renders the simplePath trace (path matrices, path edges)
- [Belyanin reachability step visualization](belyanin-reachability-step-viz.md) — renders the reachability trace (Boolean matrices, no path edges)
- [Path semiring](path-semiring.md) — the custom semiring over sets of vertex sequences used by `evaluateSimplePathWithTrace`
- [Kronecker RPQ](kronecker-rpq.md) — Kronecker product with MS-BFS
- [MS-BFS module](msbfs.md) — Boolean semiring operations
- [BooleanDecomposition module](boolean-decomposition.md) — per-label matrix decomposition
