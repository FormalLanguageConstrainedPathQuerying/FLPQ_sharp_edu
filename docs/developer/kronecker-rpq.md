# Kronecker-based RPQ Algorithm

**Tags:** algorithm, rpq, regular, kronecker-product, bfs, automaton, matrix-multiplication, product-construction
**Kind:** algorithm
**Module:** KroneckerRPQ
**Source:** `src/FLPQ.RPQ/KroneckerRPQ.fs`
**Depends on:** Matrix, LinearAlgebra, Graph, Automaton, MsBfs
**Used by:** FLPQ.Cli
**Book reference:** Chapter 12, Section 03_TensorProduct.tex; Chapter 3, Section 05_BFS.tex

> **Abstract:** Implements the Kronecker product-based Regular Path Querying algorithm with MS-BFS filtering. Computes a single Kronecker product of DFA and graph transition matrices using `Nfa.intersectEdgeSets` (set intersection) as the element-wise operation, then runs MS-BFS on the boolean mask for reachability filtering. Eliminates per-label decomposition and per-label Kronecker calls. Returns a |sources| × |V| boolean reachability matrix.

## Contents

- [Algorithm](#algorithm)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

1. Compute a single Kronecker product `P = DFA_transitions ⊗ graph_transitions` using `Nfa.intersectEdgeSets` (set intersection) as the element-wise operation. Each cell `P[i,j]` contains the set of labels for which both the DFA and the graph have a transition at the corresponding pair of positions.
2. Boolean mask: `K = Matrix.map Option.isSome P` — true wherever there is a common label.
3. Run MS-BFS on `K` from start pairs `S = {(q_s, v_s) | q_s ∈ Q_S, v_s ∈ startVertices}`.
4. For each source `i` and vertex `v`, `v` is reachable if `∃ q_f ∈ finalStates: forwardVisited[i][(q_f, v)] = 1`.

The result is a |startVertices|×|V| boolean reachability matrix.

**Time complexity:** O(|Q|²·|V|²) for the Kronecker product, plus MS-BFS O(k·|Q|·|V|) where k is the number of sources.

## Function Signatures

### `evaluate: DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`

Run Kronecker-based RPQ.
- Input: DFA query and graph as NFA.
- Output: |sources| × |V| boolean reachability matrix.
- Sources are taken from the NFA's start states.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Single Kronecker with `Nfa.intersectEdgeSets` | Eliminates per-label `BooleanDecomposition`, per-label boolean `nMat` construction, per-label `kron` calls, and the OR-summation loop. One `LinearAlgebra.kron` call replaces all of that. |
| `intersectEdgeSets` shared with `Nfa.intersect` | Same set-intersection operation powers both automaton intersection and Kronecker RPQ. Defined once in `Nfa` module. |
| MS-BFS on boolean mask | The product matrix `P` carries label sets; `K = map Option.isSome` strips to boolean for MS-BFS. Equivalent to the old per-label OR-summation but computed in one pass via Kronecker. |
| No `BooleanDecomposition` usage | The module no longer imports or uses `BooleanDecomposition.decomposeNonEmptySet` — the Kronecker product directly works with `Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>`. |

## Book Reference

- Chapter 12, `03_TensorProduct.tex`: tensor product approach for intersection.
- Chapter 3, `05_BFS.tex`: MS-BFS used for filtering reachable (state, vertex) pairs.

## See Also

- [Belyanin RPQ](belyanin-rpq.md) — BFS-based single-source RPQ
- [Arroyuelo RPQ](arroyuelo-rpq.md) — matrix-based regex evaluation
- [Automaton module](automaton.md) — NFA/DFA types and `intersectEdgeSets`
- [MS-BFS module](msbfs.md) — multiple-source BFS
- [LinearAlgebra module](linear-algebra.md) — Kronecker product
