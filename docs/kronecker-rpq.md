# Kronecker-based RPQ Module Design

## Overview

The `KroneckerRPQ` module in `FLPQ.RPQ` implements the Kronecker product-based RPQ algorithm with MS-BFS filtering. Based on Chapter 12, `03_TensorProduct.tex`, adapted to RPQ.
Accepts a DFA (query) and the graph as an NFA. Returns a |sources| × |V| boolean reachability matrix.

## Algorithm

1. For each label a, compute Kronecker product K_a = N^a ⊗ G^a (where ⊗ uses AND as element-wise operation).
2. Build K = Σ_a K_a (element-wise OR over all labels).
3. Run MS-BFS on K from start pairs S = {(q_s, v_s) | q_s ∈ Q_S, v_s ∈ startVertices}.
4. For each source i and vertex v, v is reachable if ∃q_f ∈ finalStates: forwardVisited[i][(q_f, v)] = 1.

The result is a |startVertices|×|V| boolean reachability matrix.

## Function Signatures

### `evaluate: DFA<'t, int> -> Map<'t, Matrix<bool>> -> int[] -> Matrix<bool>`
Run Kronecker-based RPQ.
- Input: DFA query, per-label graph adjacency matrices, source vertex indices.
- Output: |sources|×|V| boolean reachability matrix.

## Design Decisions

- Uses `LinearAlgebra.kron` with AND as element-wise operation for Kronecker product.
- MS-BFS operates on the combined Kronecker product matrix K.
- Result is derived from forward search filtered by final states.
- The Kronecker product approach embeds the automaton and graph into a single state space, enabling parallel BFS from multiple start pairs.

## Relationship to the Book

- Chapter 12, `03_TensorProduct.tex`: tensor product approach for intersection.
- Chapter 3, `05_BFS.tex`: MS-BFS used for filtering reachable (state, vertex) pairs.
