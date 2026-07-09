# Arroyuelo's RPQ Module Design

## Overview

The `ArroyueloRPQ` module in `FLPQ.RPQ` implements Arroyuelo's matrix-based RPQ algorithm. Based on Chapter 11, `03_Arroyuelo.tex`.
Accepts the graph as an NFA and a regular expression AST. Returns a |sources| × |V| boolean reachability matrix.

## Algorithm

Translates a regular expression AST into a Boolean matrix expression and evaluates it in post-order:

- M(ε) = I (identity matrix)
- M(a) = M_a (graph adjacency matrix for label a)
- M(E1 | E2) = M(E1) ∨ M(E2) (element-wise OR)
- M(E1 / E2) = M(E1) × M(E2) (Boolean matrix product)
- M(E*) = I ∨ M(E)^+ (identity + transitive closure)

Uses dense Boolean matrices. The key contribution is the mapping from regular expression to matrix operations.

## Function Signatures

### `evaluate: NFA<'t, int> -> Regexp<'t, 'nt> -> Matrix<bool>`
Evaluate a regular expression AST on the given graph. The graph is provided as an NFA where states are vertices and transitions are labeled edges. Per-label boolean adjacency matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`. Returns a |sources| × |V| boolean reachability matrix where sources are taken from the NFA's start states.

### `transitiveClosure: Matrix<bool> -> Matrix<bool>` (private)
Compute transitive closure of a square Boolean matrix using repeated squaring.

## Design Decisions

- Reuses the `Regexp` AST type from `EbnfParser.fs`.
- Transitive closure uses O(n) iterations of repeated squaring (n = matrix size).
- Uses `MsBfs.boolAdd` and `MsBfs.boolMul` for Boolean semiring operations.
- Uses `BooleanDecomposition.decomposeNonEmptySet` to derive per-label matrices from the NFA.
- Sources are taken from the NFA's start states; the full |V|×|V| result is restricted to source rows.

## Relationship to the Book

- Chapter 11, `03_Arroyuelo.tex`.
