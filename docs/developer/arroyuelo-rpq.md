# Arroyuelo's RPQ Algorithm

**Tags:** algorithm, rpq, regular, matrix-multiplication, boolean-decomposition, fixed-point
**Kind:** algorithm
**Module:** ArroyueloRPQ
**Source:** `src/FLPQ.RPQ/ArroyueloRPQ.fs`
**Depends on:** Matrix, Graph, Automaton, BooleanDecomposition, MsBfs, EbnfParser, PathSemiring
**Used by:** FLPQ.Cli
**Book reference:** Chapter 11, Section 03_Arroyuelo.tex

> **Abstract:** Implements Arroyuelo's matrix-based Regular Path Querying algorithm. Translates a regular expression AST into a Boolean matrix expression and evaluates it in post-order: M(ε) = I, M(a) = M_a, M(E1|E2) = M(E1) ∨ M(E2), M(E1/E2) = M(E1) × M(E2), M(E\*) = I ∨ M(E)^+. Returns a |sources| × |V| boolean reachability matrix. Also provides `evaluateWithTrace`, the same evaluation over the path semiring that records one step per AST node for visualization.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
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
- M(E\*) = I ∨ M(E)^+ (identity + transitive closure)

Uses dense Boolean matrices. The key contribution is the mapping from regular expression to matrix operations.

**Time complexity:** O(|E| · n³) where |E| is the regex size and n is the graph vertex count (dominated by matrix multiplication and transitive closure).

The same post-order evaluation runs over the path semiring (`evaluateWithTrace`): M(ε) = I with trivial paths, M(a) = per-label adjacency as single-edge paths, M(E1|E2) = cell-wise union, M(E1/E2) = path matrix product, M(E\*) = I ⊕ TC(M). Cells hold sets of simple vertex paths instead of booleans.

## Type Definitions

### `ArroyueloOperation`

```fsharp
type ArroyueloOperation = Base | Alt | Seq | Star
```

The matrix operation performed at a regexp tree node: `Base` for leaves (REps, REmpty, RTerm, RNonterm), `Alt`/`Seq` for the binary nodes, `Star` for Kleene star.

### `ArroyueloTraceStep<'t, 'nt>`

```fsharp
[<Struct>]
type ArroyueloTraceStep<'t, 'nt> =
    { NodeIndex: int
      Expr: Regexp<'t, 'nt>
      Operation: ArroyueloOperation
      Operands: Matrix<Set<int list>> list
      Result: Matrix<Set<int list>>
      Children: int list }
```

One step of the path-semiring evaluation: one post-order visit of a regexp tree node. `NodeIndex` is the post-order position (the last step is the root); `Children` holds the post-order indices of the node's children (empty for leaves); `Operands` are the child results (`[]` for Base, `[left; right]` for Alt/Seq, `[child]` for Star).

## Function Signatures

### `evaluate: NFA<'t, int> -> Regexp<'t, 'nt> -> Matrix<bool>`

Evaluate a regular expression AST on the given graph. The graph is provided as an NFA where states are vertices and transitions are labeled edges. Per-label boolean adjacency matrices are derived via `BooleanDecomposition.decomposeNonEmptySet`. Returns a |sources| × |V| boolean reachability matrix where sources are taken from the NFA's start states.

### `evaluateWithTrace: NFA<'t, int> -> Regexp<'t, 'nt> -> ArroyueloTraceStep<'t, 'nt> list * Matrix<Set<int list>>`

Evaluate a regexp on the given graph over the path semiring, recording one trace step per AST node in post-order (children before parent; the last step is the root). Returns the steps and the full |V| × |V| result matrix. Deterministic: post-order traversal, no map iteration order leaks into step content.

### `transitiveClosure: Matrix<bool> -> Matrix<bool>` (private)

Compute transitive closure of a square Boolean matrix using repeated squaring.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Reuses `Regexp` AST from `EbnfParser` | No need for a separate regex type |
| Transitive closure via repeated squaring | O(n) iterations, O(n³) per iteration — standard approach |
| Uses `MsBfs.boolAdd` and `MsBfs.boolMul` | Reuses Boolean semiring operations |
| Per-label matrices via `BooleanDecomposition` | Consistent with Belyanin's approach |
| Sources from NFA start states | Restricts the full \|V\| × \|V\| matrix to the queried sources |
| Path trace stores only simple paths | Cells stay finite on cyclic graphs (book `I_simple` semantics). Consequence: on cyclic graphs the boolean projection of the trace may be a strict subset of `evaluate`'s reachability (walks that revisit vertices are excluded); on acyclic graphs every walk is simple, so the two coincide — verified by a property test |

## Book Reference

Chapter 11, `03_Arroyuelo.tex`.

## See Also

- [Belyanin RPQ](belyanin-rpq.md) — BFS-based single-source RPQ
- [PathSemiring module](path-semiring.md) — the semiring used by `evaluateWithTrace`
- [Kronecker RPQ](kronecker-rpq.md) — Kronecker product with MS-BFS
- [MS-BFS module](msbfs.md) — Boolean semiring operations
- [BooleanDecomposition module](boolean-decomposition.md) — per-label matrix decomposition
- [EBNF Parser](ebnf-parser.md) — Regexp AST type
