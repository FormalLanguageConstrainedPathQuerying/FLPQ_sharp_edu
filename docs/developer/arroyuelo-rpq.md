# Arroyuelo's RPQ Algorithm

**Tags:** algorithm, rpq, regular, matrix-multiplication, boolean-decomposition, fixed-point
**Kind:** algorithm
**Module:** ArroyueloRPQ
**Source:** `src/FLPQ.RPQ/ArroyueloRPQ.fs`
**Depends on:** Matrix, Graph, Automaton, BooleanDecomposition, MsBfs, EbnfParser
**Used by:** FLPQ.Cli
**Book reference:** Chapter 11, Section 03_Arroyuelo.tex

> **Abstract:** Implements Arroyuelo's matrix-based Regular Path Querying algorithm. Translates a regular expression AST into a Boolean matrix expression and evaluates it in post-order: M(ε) = I, M(a) = M_a, M(E1|E2) = M(E1) ∨ M(E2), M(E1/E2) = M(E1) × M(E2), M(E*) = I ∨ M(E)^+. Returns a |sources| × |V| boolean reachability matrix.

## Contents

- [Algorithm](#algorithm)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

Translates a regular expression AST into a Boolean matrix expression and evaluates it in post-order:

- M(ε) = I (identity matrix)
- M(a) = M_a (graph adjacency matrix for label a)
- M(E1 | E2) = M(E1) ∨ M(E2) (element-wise OR)
- M(E1 / E2) = M(E1) × M(E2) (Boolean matrix product)
- M(E*) = I ∨ M(E)^+ (identity + transitive closure)

Uses dense Boolean matrices. The key contribution is the mapping from regular expression to matrix operations.

**Time complexity:** O(|E| · n³) where |E| is the regex size and n is the graph vertex count (dominated by matrix multiplication and transitive closure).

## Function Signatures

### `evaluate: NFA<'t, int> -> Regexp<'t, 'nt> -> Matrix<bool>`
Evaluate a regular expression AST on the given graph. The graph is provided as an NFA where states are vertices and transitions are labeled edges. Per-label boolean adjacency matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`. Returns a |sources| × |V| boolean reachability matrix where sources are taken from the NFA's start states.

### `transitiveClosure: Matrix<bool> -> Matrix<bool>` (private)
Compute transitive closure of a square Boolean matrix using repeated squaring.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Reuses `Regexp` AST from `EbnfParser` | No need for a separate regex type |
| Transitive closure via repeated squaring | O(n) iterations, O(n³) per iteration — standard approach |
| Uses `MsBfs.boolAdd` and `MsBfs.boolMul` | Reuses Boolean semiring operations |
| Per-label matrices via `BooleanDecomposition` | Consistent with Belyanin's approach |
| Sources from NFA start states | Restricts the full |V|×|V| result to source rows |

## Book Reference

Chapter 11, `03_Arroyuelo.tex`.

## See Also

- [Belyanin RPQ](belyanin-rpq.md) — BFS-based single-source RPQ
- [Kronecker RPQ](kronecker-rpq.md) — Kronecker product with MS-BFS
- [MS-BFS module](msbfs.md) — Boolean semiring operations
- [BooleanDecomposition module](boolean-decomposition.md) — per-label matrix decomposition
- [EBNF Parser](ebnf-parser.md) — Regexp AST type
