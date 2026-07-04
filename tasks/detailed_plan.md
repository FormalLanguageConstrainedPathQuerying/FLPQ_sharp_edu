# Detailed Plan: Task 102 — CYK Merged Summary Golden Tests

## Problem

No golden tests exist for CYK merged summary TeX generation. We need to generate reference TeX files and tests that verify future changes don't accidentally break the output.

The existing golden tests (`GrammarTeXGoldenTests.fs`, `LRTableTeXGoldenTests.fs`) each have a duplicated `verifyGolden` helper. We should extract this before creating new tests.

## Goal

1. Extract shared `verifyGolden` helper from existing golden test files into `GoldenHelpers.fs`
2. Create CYK golden tests that generate merged summary TeX for multiple examples and compare with reference files
3. Generate initial golden reference files

## Design Decisions

### Decision 1: Extracting shared verifyGolden

Move `verifyGolden` into a new `GoldenHelpers.fs` module that provides:
```fsharp
let verifyGolden (goldenFileName: string) (actualContent: string) = ...
```

Both `GrammarTeXGoldenTests.fs` and `LRTableTeXGoldenTests.fs` will call `GoldenHelpers.verifyGolden`.

### Decision 2: How to generate merged TeX for golden tests

The merged summary pipeline involves:
1. Parsing grammar + converting to CNF
2. Tokenizing input
3. Running CYK (parseWithTrace)
4. Writing grammar_original.tex, grammar_cnf.tex, input.tex, and step table.tex files
5. Calling SummaryTeX.buildContent + template wrapping

We create a helper function `generateCykSummaryTex(grammarStr, inputTokens) -> string` that does all of this in a temp directory and returns the final merged TeX.

### Decision 3: Test data

Use two examples:
- grammar1 (`S -> a S b S | eps`) with input `aababb` → produces several CYK steps with non-trivial table
- grammar7 (expression grammar) with input `x + x` → produces larger table with more nonterminals in CNF

## Implementation Checklist

### 1. `tests/FLPQ.Printers.Tests/GoldenHelpers.fs` — Shared helper
- [ ] Create module with `verifyGolden` function
- [ ] Use `goldenDataDir` path from existing pattern

### 2. Refactor existing golden tests
- [ ] `GrammarTeXGoldenTests.fs` — use `GoldenHelpers.verifyGolden` instead of local copy
- [ ] `LRTableTeXGoldenTests.fs` — use `GoldenHelpers.verifyGolden` instead of local copy

### 3. `tests/FLPQ.Printers.Tests/CykSummaryGoldenTests.fs` — New golden tests
- [ ] Helper to generate CYK summary TeX from grammar string and input tokens
- [ ] Test: grammar1, input `aababb`
- [ ] Test: grammar7, input `x + x`
- [ ] Each test generates merged TeX and calls `verifyGolden`

### 4. `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — Project file
- [ ] Add `GoldenHelpers.fs` before golden test files in compilation order
- [ ] Add `CykSummaryGoldenTests.fs` in compilation order

### 5. Generate golden reference files
- [ ] Run tests (they will fail and create golden files)
- [ ] Copy golden files to `GoldenData/`

### 6. Final Checks
- [ ] Format: `dotnet fantomas . --check`
- [ ] Build: `dotnet build FLPQ.slnx -c Release`
- [ ] Tests: `dotnet test`
