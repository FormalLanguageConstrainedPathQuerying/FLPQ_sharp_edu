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
