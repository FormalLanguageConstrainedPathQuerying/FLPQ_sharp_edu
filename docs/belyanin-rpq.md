# Belyanin's LARPQ Module Design

## Overview

The `BelyaninRPQ` module in `FLPQ.Languages` implements Belyanin's BFS-based single-source RPQ algorithm. Based on Chapter 11, `02_BFS.tex`, algorithm `algo:RPQ_BFS_semiring`.

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

### `evaluate: DFA<'t, int> -> Map<'t, Matrix<bool>> -> int -> bool[]`
Run Belyanin's RPQ algorithm.
- Input: DFA (query automaton), per-label graph adjacency matrices, start vertex index.
- Output: boolean vector of length |V| indicating reachable vertices from v_s.

## Design Decisions

- Uses DFA's single start state (deterministic automaton).
- Per-label matrices are built on-the-fly from the DFA's transition matrix.
- Uses `MsBfs.boolAdd`, `MsBfs.boolMul`, and `MsBfs.maskFilter` for Boolean semiring operations.
- The index-based unary operator I^P_reach is implemented via `maskFilter`: filtering out already accumulated (q,v) pairs from P.

## Relationship to the Book

- Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring`.
