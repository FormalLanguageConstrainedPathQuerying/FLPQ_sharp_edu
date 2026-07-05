
# Global Plan: Tasks 109--110

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|-------------|
| 109 | Golden tests for LL and LR steps visualization | Test | None |
| 110 | CLI: check output directory is empty, clean if not | Feature | None |

## Dependencies Graph

```
Task 109 (LL/LR steps golden tests) ── independent, reuses GoldenHelpers
Task 110 (CLI clean output dir)      ── independent, modifies Helpers.fs and Program.fs
```

Both tasks are fully independent and can be done in either order.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 109 | `tests/FLPQ.Printers.Tests/LLStepsGoldenTests.fs` (new), `tests/FLPQ.Printers.Tests/LRStepsGoldenTests.fs` (new), `tests/FLPQ.Printers.Tests/GoldenData/*.dot` (new), `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` | None |
| 110 | `src/FLPQ.Cli/Helpers.fs`, `src/FLPQ.Cli/Program.fs` | None |

## Execution Order

1. **Task 109** — LL/LR steps golden tests (independent)
2. **Task 110** — CLI clean output dir (independent)

## Shared Infrastructure

- **verifyGolden helper**: Already exists in `GoldenHelpers.fs`. Task 109 reuses it directly.
- **Step visualization**: LL and LR step visualization already exists via `LLStepVisualizer.renderSteps` and `LRStepVisualizer.renderSteps`. Task 109 generates the DOT content from these and compares with golden references.

## Architecture Alignment

- **Task 109**: Golden tests follow the existing pattern from `CykSummaryGoldenTests.fs`. Each test generates combined DOT content for all steps, concatenated with step headers, and compares against golden `.dot` reference files. Test inputs:
  - LL: grammar1 (`S -> a S b S | eps`) with inputs `"a b"` and `"a a b a b b"`
  - LR: grammar3 (`S -> a S | a`) with input `"a a"` and grammar7 (expression grammar) with input `"x + x"`
- **Task 110**: Add `cleanOutputDir` function in `Helpers.fs` that checks if directory exists and is non-empty, and if so deletes and recreates it. Call it from `Program.fs` before dispatching to runners.

## Detailed Task Plans

### Task 109 (LL/LR steps golden tests)

Files to create/modify:
- `tests/FLPQ.Printers.Tests/LLStepsGoldenTests.fs` (new) — golden tests for LL step dot files
- `tests/FLPQ.Printers.Tests/LRStepsGoldenTests.fs` (new) — golden tests for LR step dot files
- `tests/FLPQ.Printers.Tests/GoldenData/ll_grammar1_ab.dot` (new)
- `tests/FLPQ.Printers.Tests/GoldenData/ll_grammar1_aababb.dot` (new)
- `tests/FLPQ.Printers.Tests/GoldenData/lr_grammar3_aa.dot` (new)
- `tests/FLPQ.Printers.Tests/GoldenData/lr_grammar7_xplusx.dot` (new)
- `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — add new files to compilation order and golden data content includes

The golden tests will:
1. Parse a grammar
2. Run parser with steps (LL or LR)
3. Render steps via `LLStepVisualizer.renderSteps` / `LRStepVisualizer.renderSteps`
4. Concatenate all step DOT contents with `--- Step N ---` headers
5. Compare with golden reference file

### Task 110 (CLI clean output dir)

Files to modify:
- `src/FLPQ.Cli/Helpers.fs` — add `cleanOutputDir` function
- `src/FLPQ.Cli/Program.fs` — call `cleanOutputDir` before dispatching to algorithm runners