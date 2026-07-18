# Valiant Algorithm

**Tags:** algorithm, parsing, Valiant, matrix-multiplication, dynamic programming, cfg
**Kind:** algorithm
**Module:** Valiant
**Source:** `src/FLPQ.Languages/Valiant.fs`
**Depends on:** Matrix, Grammar
**Used by:** FLPQ.Cli
**Book reference:** Section sec:Valiant

> **Abstract:** Implements Valiant's parsing algorithm for context-free grammars in Chomsky Normal Form. Uses set-based matrix operations and recursive submatrix decomposition to achieve subcubic complexity. Each cell is a `Set<Nonterminal<'nt>>` — same representation as CYK. Matrix multiplication uses a set-based semiring: addition is set union, multiplication computes nonterminals derivable via binary rules.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Submatrix Operations](#submatrix-operations)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

### Standard Valiant

```
compute(i, j):                  // build table for cells i ≤ i' < j
  if j-i ≥ 4: recurse on halves
  build submatrix with vertex at center, call complete()

complete(m):                    // fill submatrix m
  if size(m)=1:
    if diagonal: fill from terminal rules (pre-filled in init)
    else: already in table from multiplications
  else:
    B=bottomSubmatrix, L=leftSubmatrix, R=rightSubmatrix, T=topSubmatrix
    complete(B)
    doMultiplications({(L, leftGrounded(L), B)})
    complete(L)
    doMultiplications({(R, B, rightGrounded(R))})
    complete(R)
    doMultiplications({(T, leftGrounded(T), R)})
    doMultiplications({(T, L, rightGrounded(T))})
    complete(T)
```

### Modified Valiant

```
main():
  initialize diagonal T[l-1,l] from terminal rules
  for layer = 1..ceil(log n):
    M = constructLayer(layer)
    completeLayerModified(M)

completeLayerModified(M):
  if size(m)=1:
    for each m in M: fill T[i,j] from terminal rules (diagonal) or from multiplications (off-diagonal)
  else:
    completeLayerModified(bottom quarters of M)
    completeVLayerModified(M)

completeVLayerModified(M):
  leftSubLayer  = {leftSubmatrix(m)  | m ∈ M}
  rightSubLayer = {rightSubmatrix(m) | m ∈ M}
  topSubLayer   = {topSubmatrix(m)   | m ∈ M}
  first tasks:  L(m_l) × rightNeighbor(m_l) for m_l ∈ leftSubLayer
                leftNeighbor(m_r) × R(m_r) for m_r ∈ rightSubLayer
  multiplications via mxmSet
  completeLayerModified(leftSubLayer ∪ rightSubLayer)
  second tasks: L(m_t) × rightNeighbor(m_t) for m_t ∈ topSubLayer
  third tasks:  leftNeighbor(m_t) × R(m_t) for m_t ∈ topSubLayer
  multiplications
  completeLayerModified(topSubLayer)
```

## Type Definitions

### `Submatrix`
```fsharp
type Submatrix = { row: int; col: int; Size: int }
```
Defines a square region in the parsing table. The cells are `(i,j)` where `A - Size < i ≤ A` and `B ≤ j < B + Size`. The pair `(A,B)` is the vertex (bottom-right corner relative to the diagonal).

### `ValiantTraceStep<'nt>`
```fsharp
[<Struct>]
type ValiantTraceStep<'nt when 'nt: comparison> =
    { table: ParsingTable<'nt>
      target: Submatrix
      multiplied: (Submatrix * Submatrix) list
      changedCells: (int * int) list }
```
Record representing a single multiplication step from `doMultiplications`. Contains the current table snapshot, the target submatrix receiving the result, the list of (left, right) operand submatrices, and the coordinates of cells that changed.

### `ModifiedValiantTraceStep<'nt>`
```fsharp
[<Struct>]
type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
    | LayerForward of table: ParsingTable<'nt> * layerSize: int * submatrices: Submatrix list
    | LayerBackward of table: ParsingTable<'nt> * layerSize: int * submatrices: Submatrix list * changedCells: (int * int) list
```
DU representing trace steps for the modified Valiant algorithm. `LayerForward` records before processing a V-layer. `LayerBackward` records after processing with changed cell coordinates.

## Submatrix Operations

### Quarter splitting
- `bottomSubmatrix(m)` — lower-left quarter, closest to the diagonal (higher row indices)
- `leftSubmatrix(m)` — upper-left quarter (lower row indices, same columns)
- `rightSubmatrix(m)` — lower-right quarter (higher rows, right columns)
- `topSubmatrix(m)` — upper-right quarter (lower rows, right columns)

### Grounding
- `rightGrounded(m)` — shifts submatrix so vertex lies on the diagonal `i+1=j`
- `leftGrounded(m)` — shifts submatrix so vertex lies on the diagonal

### Set-based matrix operations
- `setMult(binaryRules)(a, b)` — set semiring multiplication: `{N3 | N3 → N1 N2, N1 ∈ a, N2 ∈ b}`
- `mxmSet(binaryRules)(a, b)` — matrix multiplication using `setMult` as ⊗ and set union as ⊕
- `writeSliceUnion(target, m, slice)` — union a slice into the target submatrix
- `extractSlice(matrix, m)` — extract a submatrix slice

### Modified Valiant additions
| Function | Signature | Description |
|----------|-----------|-------------|
| `rightNeighbor` | `Submatrix -> Submatrix` | Shift submatrix down by its size |
| `leftNeighbor` | `Submatrix -> Submatrix` | Shift submatrix left by its size |
| `constructLayer` | `int -> int -> Submatrix list` | Build V-layer i: disjoint submatrices of size 2^i |

## Function Signatures

### `parse`
```fsharp
val parse: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> bool
```
Determines whether the token sequence belongs to the language of grammar `g` using Valiant's algorithm.

### `parseWithTable`
```fsharp
val parseWithTable: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ParsingTable<'nt> * bool
```
Runs Valiant's algorithm without tracing and returns both the final parsing table and the acceptance status.

### `parseWithTrace`
```fsharp
val parseWithTrace: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ValiantTraceStep<'nt> list
```
Runs standard Valiant with step-by-step tracing of `doMultiplications` calls only. Each trace step records a single multiplication task.

### `parseModified`
```fsharp
val parseModified: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> bool
```
Check acceptance using the modified Valiant algorithm.

### `parseModifiedWithTable`
```fsharp
val parseModifiedWithTable: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ParsingTable<'nt> * bool
```
Run the modified Valiant algorithm and return both the parsing table and acceptance status.

### `parseModifiedWithTrace`
```fsharp
val parseModifiedWithTrace: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ModifiedValiantTraceStep<'nt> list
```
Run the modified Valiant algorithm with step-by-step tracing. Each step captures the table state before/after processing one V-layer.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Set-based matrices (no Boolean decomposition) | Simpler: each cell holds `Set<Nonterminal<'nt>>` directly. No `decompose`/`recompose` conversion |
| Multiplication-only trace steps | Trace records only `doMultiplications` results (target + operand submatrices + changed cells), omitting decomposition transitions and size-1 terminal steps |
| Terminal rules pre-filled in `initValiant` | Ensures all diagonal cells have data before layer processing starts |
| `bottomSubmatrix` uses higher row indices | "Closest to diagonal" means row index closer to column index |
| Padding to next power of 2 | Valiant's precondition: n+1 = 2^k for some k |
| Recursive `complete` with `and compute` | F# `let rec ... and ...` for mutually recursive functions |
| V-shaped layers of disjoint submatrices | Enables batched parallel multiplications as described in the book |

## Book Reference

Section `\label{sec:Valiant}`: Valiant's algorithm reduces context-free parsing to matrix multiplication, replacing the CYK triple loop with fast submatrix multiplication.

## See Also

- [CYK algorithm](cyk.md) — classic O(n³) CYK, shares `ParsingTable<'nt>` type
- [Grammar module](grammar.md) — CNF transformation
- [Matrix module](matrix.md) — underlying matrix type and set-based operations
