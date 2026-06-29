# Detailed Plan: Task 38 — More TeX compilation tests

## Changes

### 1. Fix LL test
Replace string assertions (`\begin{pNiceMatrix}`) with actual `checkTexCompiles` calls for both stack and input.

### 2. Add CYK all-steps TeX test
Iterate all CYK trace steps, call `checkTexCompiles` on `tableToTeX` for each step.

### 3. Add Valiant TeX test
Call `Valiant.parseWithTrace`, iterate steps, call `checkTexCompiles` on each step's TeX.

### Files

| File | Action |
|------|--------|
| `tests/FLPQ.Languages.Tests/TexCompilationTests.fs` | Fix LL test, add CYK and Valiant tests |
