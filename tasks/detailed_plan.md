# Detailed Plan: Task 52 — Modified Valiant Algorithm

## Overview
Implement the modified Valiant algorithm from Book Chapter 7, `02_Valiant.tex`, subsection "Модифицированный алгоритм". The algorithm structures the parsing table into V-shaped layers of disjoint submatrices, enabling batched/parallel matrix multiplications.

## Design Decisions

### 1. Layer Construction
- `constructLayer(i)`: builds a set of disjoint submatrices of size `2^i`
- Base submatrix: `submatrixByBottomCellAndSize((2^i-1, 2^i), 2^i)`
- Shift by `(k·2^i, k·2^i)` for k ≥ 0, keep those fitting within T matrix
- Use existing `Submatrix` type

### 2. Neighbor Functions
- `rightNeighbor(m)`: shift down by size — `sshift(m, m.Size, 0)`
- `leftNeighbor(m)`: shift left by size — `sshift(m, 0, -m.Size)`
- These map between quarters of the same parent submatrix:
  - rightNeighbor(left_quarter) = bottom_quarter
  - leftNeighbor(right_quarter) = bottom_quarter (via col shift)
- Verified with property-based tests against standard Valiant

### 3. Core Procedures
- `completeLayer(M)`: processes a set M of submatrices of equal size
  - If size=1: fill T[i,j] for bottom cells where i+1 ≠ j (diagonal cells handled in init)
  - Otherwise: recursively `completeLayer` on bottom quarters, then `completeVLayer(M)`
- `completeVLayer(M)`: the core parallel processing
  - Build leftSubLayer, rightSubLayer, topSubLayer from quarters of each m in M
  - 3 batches of multiplications with `performMultiplications` (already supports task lists)
  - Interleaved with recursive `completeLayer` calls on sub-layers

### 4. Integration with Existing Code
- Reuse: `Submatrix` type, all helper functions (bottomSubmatrix, leftSubmatrix, rightSubmatrix, topSubmatrix, sshift, rightGrounded, leftGrounded, extractSlice, writeSlice, nextPowerOfTwo)
- Reuse: `performMultiplications` (already takes list of triples)
- Reuse: `terminalRulesFromGrammar`, `binaryRulesFromGrammar`, `BooleanDecomposition.decompose/recompose`
- New: `parseModified`, `parseModifiedWithTable`, `parseModifiedWithTrace`

### 5. Visualization
- Modified trace step type: `ModifiedValiantTraceStep` with layer submatrices and colors
- Each layer shown independently with submatrices highlighted in different colors
- Visualize both boolean decomposition and recomposed matrices
- Use `\cdot` for false/empty, `1` for true
- Border and fill submatrices

### 6. Tests
- Property-based: modified Valiant == standard Valiant (same acceptance, same table)
- TeX compilation tests for visualization steps
- Layer correctness: each layer covers disjoint submatrices, union covers upper triangle

## Files to Modify
1. `src/FLPQ.Languages/Valiant.fs` — add modified algorithm
2. `tests/FLPQ.Languages.Tests/ValiantTests.fs` — add tests
3. `tests/FLPQ.Languages.Tests/TexCompilationTests.fs` — add TeX tests
4. `docs/valiant.md` — update documentation

## Implementation Steps
1. Implement `constructLayer`, `rightNeighbor`, `leftNeighbor`
2. Implement `completeLayer` and `completeVLayer`
3. Implement `parseModifiedWithTable`
4. Implement `parseModified` and `parseModifiedWithTrace`
5. Add property-based tests
6. Add visualization trace type
7. Add TeX compilation tests
8. Update documentation
