# Detailed Plan: Task 20 — Refactoring

## Overview

Seven sub-tasks covering documentation, code organization, test coverage, and project structure.

## Task 20.3: Remove `states.Length > 500` check

**File:** `src/FLPQ.Core/LRParser.fs`
- Remove lines 164-165 in `buildLR0` (the `if states.Length > 500 then failwithf ...`)
- Remove lines 234-235 in `buildLR1` (same check)

## Task 20.2: Derivation Tree Module

**New file:** `src/FLPQ.Core/DerivationTree.fs`

Move from `LLParser.fs`:
- `DerivationTree<'t, 'nt>` type (DU: Leaf, Epsilon, Node)
- `leaves` function (generalize from `DerivationTree<string,string>` to `DerivationTree<'t,'nt>` returning `'t list`)
- Add module `DerivationTree` with these items

**Update:** `LLParser.fs` — remove type and leaves, add `open FLPQ.Core`
**Update:** `LRParser.fs` — change `LLParser.leaves` reference to `DerivationTree.leaves`
**Update:** `FLPQ.Core.fsproj` — add `DerivationTree.fs` before `LLParser.fs`

## Task 20.5: Common Tokenizer

**New file:** `src/FLPQ.Core/Tokenizer.fs`

Module `Tokenizer`:
```fsharp
/// Tokenize an input string into a list of grammar symbols.
/// Multi-character terminals are space-separated in the input.
let tokenize (terminalToSymbol: string -> Symbol<'t, 'nt>) (input: string) : Symbol<'t, 'nt> list

/// Tokenize into raw strings (for lookahead in LL/LR parsers).
let tokenizeStrings (input: string) : string list

/// Tokenize into Terminal values (for LR parser).
let tokenizeTerminals (input: string) : Terminal<string> list
```

**Update:** All parsers (Cyk, LL, LR) use `Tokenizer.tokenize` functions instead of their private versions.

## Task 20.4.5: Generic firstK/followK

**File:** `src/FLPQ.Core/FirstFollow.fs`

Current signatures work only on `Grammar<string, string>`. Make generic:

```fsharp
val firstK: Grammar<'t, 'nt> -> ('t -> string) -> int -> Map<Nonterminal<'nt>, Set<string>>
val followK: Grammar<'t, 'nt> -> ('t -> string) -> int -> Map<Nonterminal<'nt>, Set<string>>
val firstKOfString: Map<Nonterminal<'nt>, Set<string>> -> int -> Symbol<'t, 'nt> list -> Set<string>
```

The `terminalToString: 't -> string` function converts terminal values to their string representation for lookahead computation.

Internal helpers become generic:
- `computeFirstK: Rule<'t,'nt> list -> Nonterminal<'nt> list -> int -> Map<Nonterminal<'nt>, Set<string>>`
- `firstOfSymbols` parameterized by `terminalToString`

**Update callers:**
- `LLParser.fs`: `FirstFollow.firstK g id k`, `FirstFollow.followK g id k`
- `LRParser.fs`: `FirstFollow.firstK aug id 1`, etc.
- `FirstFollowTests.fs`: update all test calls

## Task 20.6: Remove Stubs

- Delete `src/FLPQ.Core/Library.fs`
- Delete `tests/FLPQ.Core.Tests/Tests.fs`
- Update both `.fsproj` files to remove compile entries

## Task 20.4.6: Boolean Decomposition Property Tests

**File:** `tests/FLPQ.Core.Tests/BooleanDecompositionTests.fs`

Add `[<Property>]` test:
```fsharp
[<Property>]
let ``compose of decompose and recompose is identity`` (m: Matrix<Set<int>>) =
    let decomp = BooleanDecomposition.decompose m
    let restored = BooleanDecomposition.recompose decomp
    // cells match
```

Need to add a generator for `Matrix<Set<int>>`.

## Task 20.4.1-4, 20.4.7: Cross-Parser Property Tests

### 20.4.1: LL and LR agree (for compatible grammars)
- Grammar1 (both LL(1) and SLR(1)/CLR(1) work): property test that LL and LR results agree

### 20.4.2: LL and Valiant agree (for compatible grammars)
- Grammar1: LL(1) parser and Valiant agree

### 20.4.3: LR and CYK agree (for compatible grammars)
- Grammar1/3: LR and CYK agree

### 20.4.4: CYK and Valiant reject tables identical
- When both reject, tables must match (already partially tested, strengthen)

### 20.4.7: Use grammar6/7/8 for all parsers
- Add property tests using grammar6/7/8 where appropriate (they define the same language as grammar6)
- For grammar6/7/8 cross-parser agreement tests

## Task 20.1: LL Parser Documentation

**New file:** `docs/ll-parser.md`

Cover:
- `DerivationTree` type definition
- `LLParser.buildTable` signature and behavior (LL(k) table construction)
- `LLParser.parse` signature and behavior (table-driven recursive descent)
- Lookahead computation from first_k and follow_k
- Conflict detection
- Tokenization assumptions
- Book relationship

## Task 20.7: Split Projects

### New projects

1. **`src/FLPQ.LinearAlgebra/FLPQ.LinearAlgebra.fsproj`**
   - Matrix.fs, LinearAlgebra.fs, BooleanDecomposition.fs
   
2. **`src/FLPQ.Languages/FLPQ.Languages.fsproj`**
   - DerivationTree.fs, Tokenizer.fs, Grammar.fs, Cyk.fs, Valiant.fs,
   - FirstFollow.fs, Automaton.fs, LLParser.fs, LRParser.fs
   - Depends on FLPQ.LinearAlgebra

3. **`tests/FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj`**
   - MatrixTests.fs, LinearAlgebraTests.fs, BooleanDecompositionTests.fs

4. **`tests/FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj`**
   - TestGrammars.fs, GrammarTests.fs, CykTests.fs, ValiantTests.fs,
   - FirstFollowTests.fs, AutomatonTests.fs, LLParserTests.fs, LRParserTests.fs

### Namespaces
- `FLPQ.LinearAlgebra` for linear algebra modules
- `FLPQ.Languages` for languages modules

### Updates
- All source files get correct namespace
- All test files get correct opens
- Solution file updated with 4 projects
- CI config updated (no changes needed, dotnet build/test work with solution)

## Implementation Order

1. Create detailed_plan.md (this file) — DONE
2. 20.3: Remove states check
3. 20.2: Create DerivationTree.fs
4. 20.5: Create Tokenizer.fs
5. 20.4.5: Make firstK/followK generic
6. 20.6: Remove stubs
7. 20.4.6: Boolean decomposition property tests
8. 20.4.1-4, 20.4.7: Cross-parser property tests
9. 20.1: LL documentation
10. 20.7: Split projects
11. Update docs (architecture.md, main.md, knowledge_base.md)
12. Format, build, test, merge

## Files

| File | Action |
|------|--------|
| `src/FLPQ.Core/LRParser.fs` | Remove states.Length check |
| `src/FLPQ.Core/DerivationTree.fs` | NEW — DerivationTree type + leaves |
| `src/FLPQ.Core/Tokenizer.fs` | NEW — Common tokenizer |
| `src/FLPQ.Core/FirstFollow.fs` | Make generic |
| `src/FLPQ.Core/LLParser.fs` | Remove DerivationTree type/leaves, use DerivationTree/Tokenizer modules |
| `src/FLPQ.Core/Cyk.fs` | Use Tokenizer |
| `src/FLPQ.Core/FLPQ.Core.fsproj` | Update compile order (add new files, remove stubs) |
| `src/FLPQ.Core/Library.fs` | DELETE |
| `tests/FLPQ.Core.Tests/Tests.fs` | DELETE |
| `tests/FLPQ.Core.Tests/BooleanDecompositionTests.fs` | Add property tests |
| `tests/FLPQ.Core.Tests/LLParserTests.fs` | Add cross-parser property tests |
| `tests/FLPQ.Core.Tests/LRParserTests.fs` | Add cross-parser property tests |
| `tests/FLPQ.Core.Tests/ValiantTests.fs` | Add cross-parser property tests |
| `tests/FLPQ.Core.Tests/CykTests.fs` | Add cross-parser property tests |
| `tests/FLPQ.Core.Tests/FirstFollowTests.fs` | Update for generic API |
| `docs/ll-parser.md` | NEW — LL parser documentation |
| `docs/architecture.md` | Update with new modules and project structure |
| `docs/main.md` | Add ll-parser.md link |
