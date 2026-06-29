# Global Plan: Tasks 38—39

## Task Summary

| ID | Description | Type |
|----|-------------|------|
| 38 | Add more TeX compilation tests (Valiant, CYK all steps). Fix LL TeX test. | Testing |
| 39 | Code review: analyze architecture, code quality, duplicates, naming. Report only. | Analysis |

## Dependencies

Both tasks are independent. Execution order: 38 → 39.

- **38** touches `TexCompilationTests.fs` and may fix `LLVisualizerTests.fs`.
- **39** is read-only: reads the entire codebase and tests, writes `tasks/code_review.md`. No code changes.

## Execution Order

1. **Task 38** — Add Valiant TeX test, CYK all-steps TeX test, fix LL TeX test
2. **Task 39** — Code review, generate `tasks/code_review.md`

## Task 38 Details

### Current state
- `TexCompilationTests.fs` has 3 tests: CYK (first step only), LL (pNiceMatrix presence only, no pdflatex), LR (all steps pdflatex)
- LL test does NOT call `checkTexCompiles` — only checks for `\begin{pNiceMatrix}` in strings. Needs to be fixed.

### Changes
| File | Action |
|------|--------|
| `tests/FLPQ.Languages.Tests/TexCompilationTests.fs` | Add Valiant trace TeX test. Add CYK all-steps TeX test. Fix LL test to actually call `checkTexCompiles`. |

## Task 39 Details

### Scope
- Read all source files in `src/`
- Read all test files in `tests/`
- Analyze for: architecture issues, code duplication, naming inconsistencies, poor style, missing docs
- Write findings to `tasks/code_review.md`
- **Do NOT fix anything**
