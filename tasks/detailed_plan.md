# Detailed Plan: Task 21 — Make LL and LR parsers generic

## Goal

LL and LR parsers currently hardcode `Grammar<string, string>`. Make them work with `Grammar<'t, 'nt>` for arbitrary display and token types.

## Changes

### 1. LLParser — make generic

**Current:**
```fsharp
val buildTable: Grammar<string, string> -> int -> Map<Nonterminal<string> * Symbol<string, string> list, int>
val parse: Grammar<string, string> -> Map<...> -> int -> string -> Option<DerivationTree<string, string>>
```

**New:**
```fsharp
val buildTable: Grammar<'t, 'nt> -> int -> Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>
val parse: Grammar<'t, 'nt> -> Map<...> -> int -> tokens: Symbol<'t, 'nt> list -> Option<DerivationTree<'t, 'nt>>
```

Tokenization moves to caller. The `parse` function takes pre-tokenized input.

### 2. LRParser — make types and functions generic

**Types become generic:**
```fsharp
type LR0Item<'t, 'nt> = { Lhs: Nonterminal<'nt>; Rhs: Symbol<'t,'nt> list; Dot: int }
type LR1Item<'t, 'nt> = { Lhs: Nonterminal<'nt>; Rhs: Symbol<'t,'nt> list; Dot: int; Lookahead: Symbol<'t,'nt> }
type LRConflict<'t, 'nt> = ShiftReduce of ... | ReduceReduce of ...
type LRTable<'t, 'nt> = { action: Map<int * Symbol<'t,'nt>, LRAction>; goto: Map<int * Nonterminal<'nt>, int>; conflicts: LRConflict list }
```

**augmentGrammar** becomes:
```fsharp
val augmentGrammar: fresh: Nonterminal<'nt> -> g: Grammar<'t,'nt> -> Grammar<'t,'nt>
```
Takes the fresh nonterminal as a parameter instead of deriving it from string concatenation.

**Table builders:**
```fsharp
val buildLR0Table: Grammar<'t,'nt> -> LRTable<'t,'nt>
val buildSLR1Table: Grammar<'t,'nt> -> LRTable<'t,'nt>
val buildCLR1Table: Grammar<'t,'nt> -> LRTable<'t,'nt>
```

**parse:**
```fsharp
val parse: Grammar<'t,'nt> -> LRTable<'t,'nt> -> tokens: Symbol<'t,'nt> list -> Option<DerivationTree<'t,'nt>>
```

### 3. Test updates

All test files call parsers with `Grammar.parseGrammar` which produces `Grammar<string, string>`. Calls need to:
- Tokenize before calling `parse`: `Tokenizer.tokenize input`
- For LR: provide fresh nonterminal for augment: `Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")`
- For LR types: use `SLR1Table`, etc. (type inference should handle this)

Since `'t='string` and `'nt='string` in all existing tests, type inference makes this transparent — existing test code should mostly work without changes.

### 4. `leaves` on LRParser

Current: `let leaves: DerivationTree<string, string> -> string list = DerivationTree.leaves`
Since DerivationTree.leaves is already generic, this just becomes the identity delegate. Can remove and have callers use `DerivationTree.leaves` directly.

## Files

| File | Action |
|------|--------|
| `src/FLPQ.Languages/LLParser.fs` | Generic 't, 'nt |
| `src/FLPQ.Languages/LRParser.fs` | Generic 't, 'nt types and functions |
| `tests/FLPQ.Languages.Tests/LLParserTests.fs` | Tokenize before parse, update tree construction |
| `tests/FLPQ.Languages.Tests/LRParserTests.fs` | Tokenize, fresh nonterminal for augment |
| `tests/FLPQ.Languages.Tests/CykTests.fs` | No change (CYK is not LL/LR) |
| `tests/FLPQ.Languages.Tests/ValiantTests.fs` | No change |
