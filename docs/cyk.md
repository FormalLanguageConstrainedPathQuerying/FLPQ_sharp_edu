# CYK Module

## Module Purpose

Implements the Cocke-Younger-Kasami (CYK) parsing algorithm for context-free grammars in Chomsky Normal Form. Provides table visualization via TeX output. Uses the common `ParsingTable<'nt>` type (matrix of nonterminal sets) shared with the Valiant algorithm.

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
Determines whether the token sequence belongs to the language of grammar `g`.

**Algorithm**:
1. Converts `g` to CNF via `Grammar.toCnf`
2. For an empty input, checks if the CNF start symbol has an epsilon production
3. For non-empty input of length n:
   - Creates an n×n table of `Set<Nonterminal<'nt>>`, all cells empty
   - Fills diagonal (span 1): `table[i,i] = {A | A → input[i] is a rule}`
   - For span lengths 2..n, for each start position i, fills `table[i, i+len-1]` by trying all split points k and checking for binary productions `A → B C` where B is in `table[i,k]` and C is in `table[k+1, i+len-1]`
4. Returns `true` iff the start symbol is in `table[0, n-1]`

**Time complexity**: `O(n³ · |P|)` where n is the input length and |P| is the number of productions.

### `parseWithTable`
```fsharp
val parseWithTable: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> ParsingTable<'nt> * bool
```
Runs CYK and returns both the final parsing table (n × n matrix where cell `[i,j]` contains the set of nonterminals deriving the substring) and the acceptance status.

### `parseWithTrace`
```fsharp
val parseWithTrace: freshNonterminal:(int -> 'nt) -> g:Grammar<'t, 'nt> -> terminals:Terminal<'t> list -> CykTraceStep<'nt> list
```
Runs CYK and returns the sequence of working table states, one per diagonal. Useful for visualizing the algorithm step by step. The first element is the table after filling the diagonal, subsequent elements show the state after each span length.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Grammar auto-converted to CNF inside `parse` | Caller doesn't need to manually convert; simplifies API |
| Cells use `Set<Nonterminal<'nt>>` | Common type shared with Valiant; immutable F# Set for purity |
| Empty cells use empty Set | Simpler than `Option`; empty set naturally represents "no nonterminals" |
| Terminals passed as `Terminal<'t> list` | Consistent with Valiant; no Symbol conversion needed |
| `parseWithTrace` returns one matrix per diagonal | Enables step-by-step visualization of the algorithm's progress |

## Test Grammars (from Task 6 Specification)

(unchanged)

## Relationship to the Book

CYK is a fundamental dynamic programming algorithm for context-free language parsing. The algorithm uses a triangular matrix (upper triangle of an n×n matrix) where each cell (i,j) stores the set of nonterminals that derive the substring w[i..j]. The `ParsingTable<'nt>` type (equivalent to `Matrix<Set<Nonterminal<'nt>>>`) directly corresponds to this structure.
