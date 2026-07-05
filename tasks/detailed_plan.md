
# Detailed Plan: Task 124 — Rename Rhs Functions

## Goal
Rename `Rhs.toList` → `toListWithEpsilon` and `Rhs.toSymbols` → `toNonEpsilonList` to clearly signal epsilon behavior.

## Steps

### 1. Rename in Grammar.fs (definition + internal call sites)
- `Grammar.fs:35`: `let toList` → `let toListWithEpsilon`
- `Grammar.fs:39`: `let toSymbols` → `let toNonEpsilonList`
- `Grammar.fs:111`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`
- `Grammar.fs:122`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`
- `Grammar.fs:137`: `Rhs.toList` → `Rhs.toListWithEpsilon`
- `Grammar.fs:232`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`
- `Grammar.fs:326`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`

### 2. Rename in other source files
- `FirstFollow.fs:75,97,142`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`
- `LLParser.fs:20,97`: `Rhs.toList` → `Rhs.toListWithEpsilon`
- `Valiant.fs:310,322`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`
- `LRParser.fs`: All `Rhs.toSymbols` → `Rhs.toNonEpsilonList` (12 occurrences)
- `LLTableTeX.fs:34`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`
- `LRTableTeX.fs:69`: `Rhs.toSymbols` → `Rhs.toNonEpsilonList`

### 3. Rename in test files
- `GrammarTests.fs:62,110`: `Rhs.toList` → `Rhs.toListWithEpsilon`

### 4. Build, format, test
