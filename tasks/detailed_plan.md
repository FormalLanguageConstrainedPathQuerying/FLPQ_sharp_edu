# Detailed Plan: Task 006 — CYK Algorithm

## Overview
Implement the Cocke-Younger-Kasami (CYK) parsing algorithm for context-free grammars in CNF. Use `Matrix<Option<HashSet<Symbol<string, string>>>>` as the working table.

## 1. Implementation in `Cyk.fs`

### Types

The working table cell type:
```fsharp
type CykCell = Option<HashSet<Symbol<string, string>>>
```

### Functions

```fsharp
val parse: Grammar<string, string> -> string (*terminals*) -> bool
```
- Converts grammar to CNF via `Grammar.toCnf`
- Tokenizes input string into terminals (character by character, or space-separated words)
- Runs CYK algorithm
- Returns true if start symbol ∈ table[0, n-1]

```fsharp
val parseWithTrace: Grammar<string, string> -> string (*terminals*) -> Matrix<CykCell> list
```
- Returns the sequence of table states for visualization
- Each step corresponds to filling a diagonal (length l)

```fsharp
val tableToTeX: (Symbol<string, string> -> string) -> Matrix<CykCell> -> string
```
- Converts a CYK table to TeX
- None cells printed as `\cdot`
- Uses Matrix.toTeX internally

### CYK Algorithm

Input: CNF grammar G, string w of length n
Output: true if w ∈ L(G)

1. Initialize n×n table with None in all cells
2. Fill diagonal (len=1): table[i,i] = {A | A → w[i] ∈ rules}
3. For len = 2 to n:
   For i = 0 to n-len:
      j = i + len - 1
      table[i,j] = ∅
      For k = i to j-1:
         For each rule A → B C:
            if N B ∈ table[i,k] and N C ∈ table[k+1,j]:
               add N A to table[i,j]
4. Return start ∈ table[0, n-1]

## 2. Tokenization

For matching the test examples, the input string should be tokenized into characters (since terminals are single characters like 'a', 'b').

## 3. Tests

Each test from task 6:
1. Grammar: S -> a S b S | eps — test acceptable and unacceptable strings
2. Grammar: S -> a S b | eps | S S — property test: same language as grammar 1
3. Grammar: S -> a S | a — test acceptable and unacceptable strings
4. Grammar: S -> S a | a — property test: same language as grammar 3
5. Grammar: S -> S S | S S S | a — property test: same language as grammar 3

## 4. Files

| File | Action |
|------|--------|
| `src/FLPQ.Core/Cyk.fs` | Create — CYK algorithm and table printing |
| `src/FLPQ.Core/FLPQ.Core.fsproj` | Modify |
| `tests/FLPQ.Core.Tests/CykTests.fs` | Create |
| `tests/FLPQ.Core.Tests/FLPQ.Core.Tests.fsproj` | Modify |
| `docs/cyk.md` | Create |
| `docs/main.md`, `docs/architecture.md` | Modify |
| `tasks/tasks.md` | Mark task 6 done |
