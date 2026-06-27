# CYK Module

## Module Purpose

Implements the Cocke-Younger-Kasami (CYK) parsing algorithm for context-free grammars in Chomsky Normal Form. Provides table visualization via TeX output.

## Type Definitions

### `CykCell`
```fsharp
type CykCell = Option<HashSet<Symbol<string, string>>>
```
A cell in the CYK working table. `None` represents an empty cell; `Some set` represents the set of nonterminals that derive the corresponding substring.

## Function Signatures

### `parse`
```fsharp
val parse: g:Grammar<string, string> -> input:string -> bool
```
Determines whether `input` belongs to the language of grammar `g`.

**Algorithm**:
1. Converts `g` to CNF via `Grammar.toCnf`
2. Tokenizes `input` character by character into terminal symbols
3. For an empty string, checks if the CNF start symbol has an epsilon production
4. For non-empty input of length n:
   - Creates an n×n table, all cells `None`
   - Fills diagonal (span 1): `table[i,i] = {A | A → input[i] is a rule}`
   - For span lengths 2..n, for each start position i, fills `table[i, i+len-1]` by trying all split points k and checking for binary productions `A → B C` where B is in `table[i,k]` and C is in `table[k+1, i+len-1]`
5. Returns `true` iff the start symbol is in `table[0, n-1]`

**Time complexity**: `O(n³ · |P|)` where n is the input length and |P| is the number of productions.

### `parseWithTrace`
```fsharp
val parseWithTrace: g:Grammar<string, string> -> input:string -> Matrix<CykCell> list
```
Runs CYK and returns the sequence of working table states, one per diagonal. Useful for visualizing the algorithm step by step. The first element is the table after filling the diagonal, subsequent elements show the state after each span length.

### `tableToTeX`
```fsharp
val tableToTeX: symbolPrinter:(Symbol<string, string> -> string) -> table:Matrix<CykCell> -> string
```
Converts a CYK working table to a LaTeX string using the `pNiceMatrix` environment.

- Empty cells (`None`) are printed as `\cdot`
- Non-empty cells are printed as `{sym1, sym2, ...}` using the provided `symbolPrinter`
- Row and column numbers are shown (1-based)

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Grammar auto-converted to CNF inside `parse` | Caller doesn't need to manually convert; simplifies API |
| Tokenization character by character | Matches the test examples where terminals are single characters |
| `CykCell` uses `HashSet` | Efficient membership tests and duplicate elimination; `None` for empty cells avoids allocation |
| `tableToTeX` prints `None` as `\cdot` | Standard mathematical notation for empty matrix entries; specified in task requirements |
| `parseWithTrace` returns one matrix per diagonal | Enables step-by-step visualization of the algorithm's progress |

## Test Grammars (from Task 6 Specification)

| # | Grammar | Language | Test Type |
|---|---------|----------|-----------|
| 1 | `S → a S b S \| ε` | Dyck language over {a,b} | Acceptance/rejection of specific strings |
| 2 | `S → a S b \| ε \| S S` | Same as grammar 1 | Property test: equivalence with grammar 1 |
| 3 | `S → a S \| a` | `a⁺` (one or more a's) | Acceptance/rejection |
| 4 | `S → S a \| a` | Same as grammar 3 | Property test: equivalence with grammar 3 |
| 5 | `S → S S \| S S S \| a` | Same as grammar 3 | Property test: equivalence with grammar 3 |

## Relationship to the Book

CYK is a fundamental dynamic programming algorithm for context-free language parsing. The algorithm uses a triangular matrix (upper triangle of an n×n matrix) where each cell (i,j) stores the set of nonterminals that derive the substring w[i..j]. The `Matrix<Option<HashSet<Symbol>>>` representation directly corresponds to this structure.
