# Detailed Plan: Task 009 — Refactoring

## Overview
Refactor test structure, normalize API types, and add comprehensive property-based tests.

## Changes

### 1. Common test data module
Created `tests/FLPQ.Core.Tests/TestGrammars.fs` with:
- Pre-parsed grammars 1–5 (used by CykTests, ValiantTests)
- `AbStringGenerators` and `AStringGenerators` (FsCheck generators for restricted alphabets)

### 2. Separate boolean decomposition tests
Moved 6 unit tests from `ValiantTests.fs` into `tests/FLPQ.Core.Tests/BooleanDecompositionTests.fs`.

### 3. Normalized parseWithTable return type
Both `Cyk.parseWithTable` and `Valiant.parseWithTable` now return `Matrix<Set<Nonterminal<string>>> * bool` — an n×n matrix where cell[i,j] (0≤i≤j<n) contains the set of nonterminals deriving that substring. Empty cells are empty sets.

CYK: converts from internal `Matrix<Option<HashSet<Symbol>>>` representation.
Valiant: converts from internal Boolean decomposition `Map<Nonterminal, Matrix<bool>>`.

### 4. Table-level property tests
Added 3 FsCheck property tests in ValiantTests: for grammar 1, 2, and 3 with random strings, verify both acceptance status AND full table contents match between CYK and Valiant.

### 5. Documentation updates
- Created `docs/valiant.md` with algorithm description
- Updated `docs/cyk.md` with `parseWithTable` signature
- Updated `docs/architecture.md` with new test files

## Files Changed
| File | Action |
|------|--------|
| `tests/.../TestGrammars.fs` | Created |
| `tests/.../BooleanDecompositionTests.fs` | Created |
| `docs/valiant.md` | Created |
| `tests/.../ValiantTests.fs` | Rewritten |
| `tests/.../CykTests.fs` | Rewritten |
| `src/FLPQ.Core/Cyk.fs` | parseWithTable → Matrix<Set<NT>> |
| `src/FLPQ.Core/Valiant.fs` | parseWithTable → Matrix<Set<NT>> |
| `tests/.../FLPQ.Core.Tests.fsproj` | Added new files |
| `docs/cyk.md` | Added parseWithTable |
| `docs/architecture.md` | Updated structure |
| `tasks/tasks.md` | Added task 9, marked done |
