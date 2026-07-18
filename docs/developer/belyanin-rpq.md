# Belyanin's LARPQ Algorithm

**Tags:** algorithm, rpq, regular, bfs, automaton, matrix-multiplication, boolean-decomposition
**Kind:** algorithm
**Module:** BelyaninRPQ
**Source:** `src/FLPQ.RPQ/BelyaninRPQ.fs`
**Depends on:** Matrix, Graph, Automaton, BooleanDecomposition, MsBfs
**Used by:** FLPQ.Cli
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Implements Belyanin's LARPQ algorithm — a BFS-based single-source Regular Path Querying algorithm. Accepts a DFA query and a labeled graph (as NFA). Operates on two |Q|×|V| matrices (front and accumulated results), propagating through simultaneous automaton backward + graph forward transitions. Returns a |sources| × |V| boolean reachability matrix.

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
   - P ← P ⊕_B M (accumulate in Boolean semiring)
   - M ← Σ_a (N^a)^T ⊗_B M ⊗_B G^a (propagate: automaton backward + graph forward)
4. Result: F ⊗_B P where F selects final states — returns a boolean vector of reachable vertices.

**Time complexity:** O(|Q|·|V|·d·|Σ|) where d is the BFS diameter.

## Function Signatures

### `evaluate: DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`
Run Belyanin's RPQ algorithm.
- Input: DFA (query automaton) and graph as NFA.
- Output: |sources| × |V| boolean matrix where each row indicates reachable vertices from the corresponding source vertex.
- Sources are taken from the NFA's start states. Per-label graph matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Sources from NFA start states | Runs the single-source algorithm per source, stacks results |
| Per-label matrices via `BooleanDecomposition` | Maps automaton and graph transitions to boolean matrices by label |
| Uses `MsBfs.boolAdd`, `MsBfs.boolMul`, `MsBfs.maskFilter` | Reuses Boolean semiring operations from the MS-BFS module |
| Index-based unary operator via `maskFilter` | Filtering out already accumulated (q,v) pairs from P |

## Book Reference

Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring`.

## See Also

- [Arroyuelo RPQ](arroyuelo-rpq.md) — matrix-based regex evaluation
- [Kronecker RPQ](kronecker-rpq.md) — Kronecker product with MS-BFS
- [MS-BFS module](msbfs.md) — Boolean semiring operations
- [BooleanDecomposition module](boolean-decomposition.md) — per-label matrix decomposition
