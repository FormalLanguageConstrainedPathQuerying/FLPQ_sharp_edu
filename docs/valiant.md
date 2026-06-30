# Valiant Module

## Module Purpose

Implements Valiant's parsing algorithm for context-free grammars in Chomsky Normal Form. Uses Boolean matrix multiplication and recursive submatrix decomposition to achieve subcubic complexity `O(n^ω)` where `ω < 2.38`.

## Type Definitions

### `Submatrix`
```fsharp
type Submatrix = { A: int; B: int; Size: int }
```
Defines a square region in the parsing table. The cells are `(i,j)` where `A - Size < i ≤ A` and `B ≤ j < B + Size`. The pair `(A,B)` is the vertex (bottom-right corner relative to the diagonal).

## Function Signatures

### `parse`
```fsharp
val parse: g:Grammar<string, string> -> input:string -> bool
```
Determines whether `input` belongs to the language of grammar `g` using Valiant's algorithm.

### `parseWithTable`
```fsharp
val parseWithTable: g:Grammar<string, string> -> input:string -> Matrix<Set<Nonterminal<string>>> * bool
```
Runs Valiant's algorithm and returns both the final parsing table (n × n matrix, same format as CYK's `parseWithTable`) and the acceptance status. The algorithm internally uses Boolean decomposition for efficient submatrix multiplication, then converts the result to a set-valued matrix.

## Submatrix Operations

### Quarter splitting
- `bottomSubmatrix(m)` — lower-left quarter, closest to the diagonal (higher row indices)
- `leftSubmatrix(m)` — upper-left quarter (lower row indices, same columns)
- `rightSubmatrix(m)` — lower-right quarter (higher rows, right columns)
- `topSubmatrix(m)` — upper-right quarter (lower rows, right columns)

### Grounding
- `rightGrounded(m)` — shifts submatrix so vertex lies on the diagonal `i+1=j`
- `leftGrounded(m)` — shifts submatrix so vertex lies on the diagonal

### Multiplication
- `performMultiplications` — for each nonterminal pair `(B,C)`, extracts Boolean slices from submatrices of T, multiplies them using `LinearAlgebra.mxm` with Boolean semiring `(∧, ∨, false)`, and stores the result into P.

## Algorithm Structure

```
compute(i, j):       — build table for cells i ≤ i' < j
  if j-i ≥ 4: recurse on halves
  build submatrix with vertex at center, call complete()

complete(m):         — fill submatrix m
  if size(m)=1:
    if diagonal: fill from terminal rules
    else: fill from P via binary rules
  else:
    B=bottomSubmatrix, L=leftSubmatrix, R=rightSubmatrix, T=topSubmatrix
    complete(B)
    performMultiplications({(L, leftGrounded(L), B)})
    complete(L)
    performMultiplications({(R, B, rightGrounded(R))})
    complete(R)
    performMultiplications({(T, leftGrounded(T), R)})
    performMultiplications({(T, L, rightGrounded(T))})
    complete(T)
```

## Book Reference

Section `\label{sec:Valiant}`: Valiant's algorithm reduces context-free parsing to Boolean matrix multiplication, replacing the CYK triple loop with fast submatrix multiplication.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Boolean decomposition as internal representation | Enables O(n^ω) Boolean matrix multiplication for the core operation |
| Public API returns `Matrix<Set<Nonterminal>>` | Consistent with CYK's output; hides Boolean decomposition implementation detail |
| bottomSubmatrix uses higher row indices | "Closest to diagonal" means row index closer to column index |
| Padding to next power of 2 | Valiant's precondition: n+1 = 2^k for some k |
| Recursive `complete` with `and compute` | F# `let rec ... and ...` for mutually recursive functions |

## Modified Valiant Algorithm

### Module Purpose

Implements the modified Valiant's algorithm from the book (subsection "Модифицированный алгоритм"). Instead of the original recursive bisection `compute/complete` strategy, the modified version structures the parsing table into V-shaped layers of disjoint submatrices of equal size, enabling batched parallel execution of matrix multiplications.

### Type Definitions

#### `ModifiedValiantTraceStep<'nt>`
```fsharp
[<Struct>]
type ModifiedValiantTraceStep<'nt when 'nt: comparison> =
    { table: Matrix<Set<Nonterminal<'nt>>>
      layerSize: int
      submatrices: Submatrix list }
```
A trace step for the modified algorithm. Each step corresponds to one V-layer. `layerSize` is the size of submatrices in this layer (a power of 2). `submatrices` lists all disjoint submatrices in the layer.

### Function Signatures

#### `parseModified`
```fsharp
val parseModified: g:Grammar<string, string> -> input:string -> bool
```
Check acceptance using the modified Valiant algorithm.

#### `parseModifiedWithTable`
```fsharp
val parseModifiedWithTable: g:Grammar<string, string> -> input:string -> Matrix<Set<Nonterminal<string>>> * bool
```
Run the modified Valiant algorithm and return both the parsing table and acceptance status.

#### `parseModifiedWithTrace`
```fsharp
val parseModifiedWithTrace: g:Grammar<string, string> -> input:string -> ModifiedValiantTraceStep<'nt> list
```
Run the modified Valiant algorithm with step-by-step tracing. Each step captures the table state after processing one V-layer.

#### `stepToTeX`
```fsharp
val stepToTeX: cellPrinter:(Set<Nonterminal<'nt>> -> string) -> step:ModifiedValiantTraceStep<'nt> -> string
```
Render a trace step to TeX (pNiceMatrix) with submatrices highlighted in different colors. Submatrix coordinates from the padded matrix are clipped to the n×n recomposed matrix bounds.

### New Submatrix Operations

| Function | Signature | Description |
|----------|-----------|-------------|
| `rightNeighbor` | `Submatrix -> Submatrix` | Shift submatrix down by its size: `sshift(m, m.Size, 0)`. Maps left quarter to bottom quarter of the same parent. |
| `leftNeighbor` | `Submatrix -> Submatrix` | Shift submatrix left by its size: `sshift(m, 0, -m.Size)`. Maps right quarter to bottom quarter of the same parent. |
| `constructLayer` | `int -> int -> Submatrix list` | Build V-layer `i`: disjoint submatrices of size `2^i`. Base submatrix at `(2^i - 1, 2^i)`, shifted by `(k·2^i, k·2^i)` for `k ≥ 0`. |

### Algorithm Structure

```
main():
  initialize diagonal T[l-1,l] from terminal rules
  for layer = 1 .. ceil(log n):
    M = constructLayer(layer)
    completeLayer(M)

completeLayer(M):          — process set M of submatrices of equal size
  if size(m) = 1:
    for each m in M: fill T[i,j] where i+1 ≠ j from P via binary rules
  else:
    completeLayer(bottom quarters of M)
    completeVLayer(M)

completeVLayer(M):         — parallel processing of V-layer M
  leftSubLayer  = {leftSubmatrix(m)  | m ∈ M}
  rightSubLayer = {rightSubmatrix(m) | m ∈ M}
  topSubLayer   = {topSubmatrix(m)   | m ∈ M}
  first tasks:  L(m_l) × rightNeighbor(m_l) for m_l ∈ leftSubLayer
                leftNeighbor(m_r) × R(m_r) for m_r ∈ rightSubLayer
  performMultiplications(first tasks)
  completeLayer(leftSubLayer ∪ rightSubLayer)
  second tasks: L(m_t) × rightNeighbor(m_t) for m_t ∈ topSubLayer
  third tasks:  leftNeighbor(m_t) × R(m_t) for m_t ∈ topSubLayer
  performMultiplications(second tasks)
  performMultiplications(third tasks)
  completeLayer(topSubLayer)
```

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| V-shaped layers of disjoint submatrices | Enables batched parallel multiplications as described in the book |
| `completeLayerModified` and `completeVLayerModified` are mutually recursive | Matches the book's recursive decomposition; F# requires `and` for mutual recursion |
| Trace stores recomposed n×n matrix with padded-matrix submatrix coordinates | Submatrices are clipped to visible bounds for TeX rendering |
| `stepToTeX` clips submatrices to n×n | Submatrix coordinates from the padded matrix may extend beyond the visible table |
| Reuse of `performMultiplications` with task lists | The existing function already supports batched multiplications via list of triples |
| Same Boolean decomposition as standard Valiant | Consistent representation; no redundant code |

## Book Reference

Section `\label{sec:Valiant}` subsection "Модифицированный алгоритм": The modified algorithm structures the table into V-shaped layers of disjoint submatrices, enabling parallel matrix multiplication and natural adaptation to substring search.
