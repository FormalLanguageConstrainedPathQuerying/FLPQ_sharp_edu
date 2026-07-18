# CYK Algorithm

**Tags:** algorithm, parsing, CYK, Chomsky Normal Form, dynamic programming, matrix
**Kind:** algorithm
**Module:** Cyk
**Source:** `src/FLPQ.Languages/Cyk.fs`
**Depends on:** Matrix, Grammar
**Used by:** Valiant
**Book reference:** Chapter 7, Section sec:CYK

> **Abstract:** Implements the Cocke-Younger-Kasami (CYK) parsing algorithm for context-free grammars in Chomsky Normal Form. Uses a set-based triangular parsing table where cell (i,j) stores the set of nonterminals deriving substring w[i..j]. Provides table visualization via TeX output. Shares the `ParsingTable<'nt>` type with the Valiant algorithm.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

**Input:** CNF grammar G, input string w of length n
**Output:** `true` iff w ∈ L(G)

```
1. Convert G to CNF via Grammar.toCnf
2. If n = 0: accept iff start symbol has epsilon production
3. Create n×n table T of Set<Nonterminal<'nt>>, all cells empty
4. For i = 0..n-1:                    // diagonal: span 1
     T[i,i] ← {A | A → w[i] ∈ rules}
5. For len = 2..n:                    // increasing span lengths
     For i = 0..n-len:
       j ← i + len - 1
       For k = i..j-1:                 // split point
         For each A → B C ∈ rules:
           If B ∈ T[i,k] ∧ C ∈ T[k+1,j]:
             T[i,j] ← T[i,j] ∪ {A}
6. Accept iff start ∈ T[0,n-1]
```

**Time complexity:** O(n³ · |P|) where n is input length and |P| is the number of productions.
**Space complexity:** O(n²)

## Type Definitions

### `CykTraceStep<'nt>`
```fsharp
type CykTraceStep<'nt> =
    { table: ParsingTable<'nt>
      highlights: Matrix.Highlight list }
```
A snapshot of the CYK working table at one step, with cells modified at this step highlighted in yellow.

## Function Signatures

### `parse`
```fsharp
val parse: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> bool
```
Determines whether the token sequence belongs to the language of grammar `g`. Auto-converts the grammar to CNF internally.

### `parseWithTable`
```fsharp
val parseWithTable: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ParsingTable<'nt> * bool
```
Runs CYK and returns both the final parsing table (n × n matrix where cell `[i,j]` contains the set of nonterminals deriving the substring) and the acceptance status.

### `parseWithTrace`
```fsharp
val parseWithTrace: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> CykTraceStep<'nt> list
```
Runs CYK and returns the sequence of working table states, one per diagonal. The first element is the table after filling the diagonal, subsequent elements show the state after each span length. Useful for step-by-step visualization.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Grammar auto-converted to CNF inside `parse` | Caller doesn't need to manually convert; simplifies API |
| Cells use `Set<Nonterminal<'nt>>` | Common type shared with Valiant; immutable F# Set for purity |
| Empty cells use empty Set | Simpler than `Option`; empty set naturally represents "no nonterminals" |
| Terminals passed as `Terminal<'t> list` | Consistent with Valiant; no Symbol conversion needed |
| `parseWithTrace` returns one matrix per diagonal | Enables step-by-step visualization of the algorithm's progress |

## Book Reference

CYK is a fundamental dynamic programming algorithm for context-free language parsing. The algorithm uses a triangular matrix (upper triangle of an n×n matrix) where each cell (i,j) stores the set of nonterminals that derive the substring w[i..j]. The `ParsingTable<'nt>` type (equivalent to `Matrix<Set<Nonterminal<'nt>>>`) directly corresponds to this structure.

Chapter 7, Section sec:CYK.

## See Also

- [Valiant algorithm](valiant.md) — reduces parsing to matrix multiplication, shares `ParsingTable<'nt>` type
- [Grammar module](grammar.md) — CNF transformation
- [Matrix module](matrix.md) — underlying matrix type and TeX rendering
