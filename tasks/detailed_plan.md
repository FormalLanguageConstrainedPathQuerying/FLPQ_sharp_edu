# Detailed Plan: Tasks 57-63 — RPQ Algorithms and MS-BFS

## Overview

Implement multiple-source BFS (linear-algebra based), supporting matrix operations, three RPQ algorithms (Belyanin, Arroyuelo, Kronecker-based), graph reading, and property-based tests.

## Task 57: MS-BFS

### Algorithm
- Input: sources `K` (int array), graph adjacency matrix `M` (|V|×|V| boolean)
- Output: |K|×|V| boolean reachability matrix
- Algorithm from Chapter 3, `05_BFS.tex`

### Implementation in `src/FLPQ.LinearAlgebra/MsBfs.fs`

```fsharp
module MsBfs =
    let msBfs (sources: int[]) (adjacencyMatrix: Matrix<bool>) : Matrix<bool>
```

## Task 58: Matrix Operations

Operations go in the same `MsBfs.fs` module:

1. Boolean semiring: `map2 (||)` for ⊕_B, `mxm (&&) (||) false` for ⊗_B
2. Mask semiring: `map2 (fun nf v -> nf && not v)` for ⊕_M
3. Bool decomposition: already exists in BooleanDecomposition.fs
4. Index-based unary operator: not needed for MS-BFS/RPQ directly (used in book for advanced path enumeration); skip for now, add stubs if needed
5. Kronecker product: already exists
6. MS-BFS: implemented as part of task 57

## Task 59: Belyanin's LARPQ

### Implementation in `src/FLPQ.Languages/BelyaninRPQ.fs`

Algorithm:
1. Build per-label matrices N^a (automaton) and G^a (graph)
2. Initialize front M as |Q|×|V| boolean matrix
3. Set M[q_s, v_s] = 1 for start states and start vertices
4. Iterate: M ← I(M), P ← P ⊕_B M, M ← Σ_a (N^a)^T ⊗_B M ⊗_B G^a
5. Return F ⊗_B P (final state filtering)

## Task 60: Arroyuelo's RPQ

### Implementation in `src/FLPQ.Languages/ArroyueloRPQ.fs`

Evaluates regex AST to boolean matrix:
- Identity, terminals, reverse terminals, alternation (OR), concatenation (product), Kleene star/plus (closure)

## Task 61: Kronecker-based RPQ

### Implementation in `src/FLPQ.Languages/KroneckerRPQ.fs`

Algorithm:
1. For each label a: K_a = N^a ⊗ G^a (Kronecker product with AND)
2. K = Σ_a K_a (element-wise OR)
3. Run MS-BFS from start pairs (q_s, v_s)
4. Run reverse MS-BFS from final states
5. Intersect results
6. Project onto vertices

## Task 62: Graph Reading

### Implementation in `src/FLPQ.Languages/GraphReader.fs`

Parse graph files:
- Optional first line: start vertices (0-based indices)
- Following lines: `fromVertex label toVertex`
- Build per-label boolean adjacency matrices

## Task 63: Property-based Tests

Generate random (graph, regex, sources) and verify all three algorithms produce identical results.

## Implementation Order

1. Create `MsBfs.fs` (tasks 57+58) + tests
2. Create `GraphReader.fs` (task 62) + tests
3. Create `BelyaninRPQ.fs` (task 59) + tests
4. Create `ArroyueloRPQ.fs` (task 60) + tests
5. Create `KroneckerRPQ.fs` (task 61) + tests
6. Create property-based tests (task 63)
7. Update documentation
8. Format, build, test
