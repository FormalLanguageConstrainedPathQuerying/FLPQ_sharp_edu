# Belyanin's LARPQ Module Design

## Overview

The `BelyaninRPQ` module in `FLPQ.RPQ` implements Belyanin's LARPQ algorithm. Based on Chapter 11, `02_BFS.tex`, algorithm `algo:RPQ_BFS_semiring`.
Accepts a DFA (query) and the graph as an NFA. Returns a |sources| × |V| boolean reachability matrix.

## Algorithm

Operates on two |Q|×|V| matrices: front M and accumulated results P.

1. Build per-label matrices N^a (automaton transitions) and G^a (graph adjacency).
2. Initialize M: for each start state q_s ∈ Q_S, set M[q_s, v_s] = 1.
3. While M ≠ 0:
   - M ← I^P_reach(M) (mask: drop already found (q,v) pairs)
   - P ← P ⊕_B M (accumulate in Boolean semiring)
   - M ← Σ_a (N^a)^T ⊗_B M ⊗_B G^a (propagate: automaton backward + graph forward)
4. Result: F ⊗_B P where F selects final states — returns a boolean vector of reachable vertices.

## Function Signatures

### `evaluate: DFA<'t, int> -> NFA<'t, int> -> Matrix<bool>`
Run Belyanin's RPQ algorithm.
- Input: DFA (query automaton) and graph as NFA.
- Output: |sources| × |V| boolean matrix where each row indicates reachable vertices from the corresponding source vertex.
- Sources are taken from the NFA's start states. Per-label graph matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`.

## Design Decisions

- Sources are taken from the NFA's start states; runs the single-source algorithm per source and stacks results.
- Per-label graph matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`.
- Uses `MsBfs.boolAdd`, `MsBfs.boolMul`, and `MsBfs.maskFilter` for Boolean semiring operations.
- The index-based unary operator I^P_reach is implemented via `maskFilter`: filtering out already accumulated (q,v) pairs from P.

## Relationship to the Book

- Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring`.
