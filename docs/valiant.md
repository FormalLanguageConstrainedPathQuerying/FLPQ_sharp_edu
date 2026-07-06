# Valiant Module

## Module Purpose

Implements Valiant's parsing algorithm for context-free grammars in Chomsky Normal Form. Uses set-based matrix operations and recursive submatrix decomposition to achieve subcubic complexity. Each cell is a `Set<Nonterminal<'nt>>` — same representation as CYK. Matrix multiplication uses a set-based semiring: addition is set union, multiplication computes nonterminals derivable via binary rules.

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
    | Forward of table: ParsingTable<'nt> * submatrix: Submatrix
    | Backward of table: ParsingTable<'nt> * target: Submatrix * multiplied: (Submatrix * Submatrix) list * changedCells: (int * int) list
```
DU representing trace steps for the standard Valiant algorithm. `Forward` records a decomposition step (submatrix entered). `Backward` records a multiplication step with target, source submatrices, and changed cell coordinates.

### `ModifiedValiantTraceStep<'nt>`
```fsharp
[<Struct>]
type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
    | LayerForward of table: ParsingTable<'nt> * layerSize: int * submatrices: Submatrix list
    | LayerBackward of table: ParsingTable<'nt> * layerSize: int * submatrices: Submatrix list * changedCells: (int * int) list
```
DU representing trace steps for the modified Valiant algorithm. `LayerForward` records before processing a V-layer. `LayerBackward` records after processing with changed cell coordinates.

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
Runs Valiant's algorithm and returns both the final parsing table (n × n matrix of `Set<Nonterminal<'nt>>`, same format as CYK's `parseWithTable`) and the acceptance status.

### `parseWithTrace`
```fsharp
val parseWithTrace: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ValiantTraceStep<'nt> list
```
Runs standard Valiant with step-by-step tracing, returning a list of `Forward`/`Backward` trace steps.

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

## Algorithm Structure (Standard Valiant)

```
compute(i, j):       — build table for cells i ≤ i' < j
  if j-i ≥ 4: recurse on halves
  build submatrix with vertex at center, call complete()

complete(m):         — fill submatrix m
  if size(m)=1:
    if diagonal: fill from terminal rules (already pre-filled in init)
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

## Algorithm Structure (Modified Valiant)

```
main():
  initialize diagonal T[l-1,l] from terminal rules
  for layer = 1 .. ceil(log n):
    M = constructLayer(layer)
    completeLayerModified(M)

completeLayerModified(M):
  if size(m) = 1:
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

## New Submatrix Operations (Modified Valiant)

| Function | Signature | Description |
|----------|-----------|-------------|
| `rightNeighbor` | `Submatrix -> Submatrix` | Shift submatrix down by its size |
| `leftNeighbor` | `Submatrix -> Submatrix` | Shift submatrix left by its size |
| `constructLayer` | `int -> int -> Submatrix list` | Build V-layer `i`: disjoint submatrices of size `2^i` |

## Book Reference

Section `\label{sec:Valiant}`: Valiant's algorithm reduces context-free parsing to matrix multiplication, replacing the CYK triple loop with fast submatrix multiplication.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Set-based matrices (no Boolean decomposition) | Simpler: each cell holds `Set<Nonterminal<'nt>>` directly. No `decompose`/`recompose` conversion |
| Forward/backward trace steps (DU) | Clearly separates decomposition from multiplication results |
| Terminal rules pre-filled in `initValiant` | Ensures all diagonal cells have data before layer processing starts |
| `bottomSubmatrix` uses higher row indices | "Closest to diagonal" means row index closer to column index |
| Padding to next power of 2 | Valiant's precondition: n+1 = 2^k for some k |
| Recursive `complete` with `and compute` | F# `let rec ... and ...` for mutually recursive functions |
| V-shaped layers of disjoint submatrices | Enables batched parallel multiplications as described in the book |
