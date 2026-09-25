# Belyanin's LARPQ Algorithm

**Tags:** algorithm, rpq, regular, bfs, automaton, matrix-multiplication, boolean-decomposition, path-semiring
**Kind:** algorithm
**Module:** BelyaninRPQ
**Source:** `src/FLPQ.RPQ/BelyaninRPQ.fs`
**Depends on:** Matrix, Graph, Automaton, BooleanDecomposition, MsBfs, PathSemiring
**Used by:** FLPQ.Cli
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Implements Belyanin's LARPQ algorithm — a BFS-based Regular Path Querying algorithm. Accepts a DFA query and a labeled graph (as NFA). Operates on two |Q|×|V| matrices (frontier M and accumulated results P), propagating through simultaneous automaton backward + graph forward transitions. The Boolean variant returns a |sources| × |V| reachability matrix; the path-semiring trace variant (`evaluateWithTrace`) stores sets of simple vertex paths in the cells and records one step per main-loop iteration for visualization.

## Contents

- [Algorithm](#algorithm)
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

## Function Signatures

### `evaluate: DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`

Run Belyanin's RPQ algorithm.

- Input: DFA (query automaton) and graph as NFA.
- Output: |sources| × |V| boolean matrix where each row indicates reachable vertices from the corresponding source vertex.
- Sources are taken from the NFA's start states. Per-label graph matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`.

### `evaluateWithTrace: DFA<'t, int> -> NFA<'t, int> -> BelyaninTraceStep<'t> list * Matrix<Set<int list>>`

Run the algorithm over the [path semiring](path-semiring.md) and record one trace step per
main-loop iteration (plus an initialization step). Returns the steps and the final
accumulated matrix P.

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
| Trace: combined multi-source run | All sources share one traversal — M[q, v] holds paths from any source; a path's head identifies its source. Paths from different sources never interfere (distinct vertex sequences, DFA determinism), so the combined run is the union of the per-source runs and still terminates in at most \|V\|-1 iterations |
| Trace: path-level mask | M \\ P drops exact path duplicates, not whole (q,v) pairs — pair-level masking would drop unprocessed simple paths sharing a cell. Each simple path is produced exactly once, so the mask is defensive; kept for book fidelity (I^P_reach) |
| Trace: cells store simple paths only | Every cell is finite on cyclic graphs and the loop terminates in at most \|V\|-1 iterations. Consequence: on cyclic graphs the boolean projection may be a strict subset of `evaluate`'s reachability (walks revisiting a vertex are excluded by I_simple); on acyclic graphs the two coincide — tested as a property |
| Trace: result derived from P | Per-source reachability comes from path heads at final states in the final P (`reachableFromPaths`) — the trace is the single source of truth; the runner does not call the Boolean `evaluate` separately |

## Book Reference

Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring`; the path semiring carrier
2^{V\*} with the index operator I_simple (book example figure
`figures/02_BFS/algorithm_step.tex` shows paths in matrix cells).

## See Also

- [Arroyuelo RPQ](arroyuelo-rpq.md) — matrix-based regex evaluation
- [Path semiring](path-semiring.md) — the custom semiring over sets of vertex sequences used by `evaluateWithTrace`
- [Kronecker RPQ](kronecker-rpq.md) — Kronecker product with MS-BFS
- [MS-BFS module](msbfs.md) — Boolean semiring operations
- [BooleanDecomposition module](boolean-decomposition.md) — per-label matrix decomposition
