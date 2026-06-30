# Arroyuelo's RPQ Module Design

## Overview

The `ArroyueloRPQ` module in `FLPQ.Languages` implements Arroyuelo's matrix-based RPQ algorithm. Based on Chapter 11, `03_Arroyuelo.tex`.

## Algorithm

Translates a regular expression AST into a Boolean matrix expression and evaluates it in post-order:

- M(ε) = I (identity matrix)
- M(a) = M_a (graph adjacency matrix for label a)
- M(E1 | E2) = M(E1) ∨ M(E2) (element-wise OR)
- M(E1 / E2) = M(E1) × M(E2) (Boolean matrix product)
- M(E*) = I ∨ M(E)^+ (identity + transitive closure)

Uses dense Boolean matrices. The key contribution is the mapping from regular expression to matrix operations.

## Function Signatures

### `evaluate: Map<'t, Matrix<bool>> -> int -> Regexp<'t, 'nt> -> Matrix<bool>`
Evaluate a regular expression AST to a full |V|×|V| Boolean matrix.

### `evaluateWithSources: Map<'t, Matrix<bool>> -> int -> Regexp<'t, 'nt> -> int[] option -> Matrix<bool>`
Evaluate a regexp and restrict to source rows. Returns |startVertices|×|V| matrix.

### `transitiveClosure: Matrix<bool> -> Matrix<bool>` (private)
Compute transitive closure of a square Boolean matrix using repeated squaring.

## Design Decisions

- Reuses the `Regexp` AST type from `EbnfParser.fs`.
- Transitive closure uses O(n) iterations of repeated squaring (n = matrix size).
- Uses `MsBfs.boolAdd` and `MsBfs.boolMul` for Boolean semiring operations.
- Source restriction: when sources are specified, extracts corresponding rows from the full matrix.

## Relationship to the Book

- Chapter 11, `03_Arroyuelo.tex`.
