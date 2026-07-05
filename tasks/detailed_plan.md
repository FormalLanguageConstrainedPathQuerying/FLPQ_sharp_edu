# Detailed Plan: Task 109 — Golden Tests for LL and LR Steps Visualization

## Problem

No golden tests exist for LL and LR step-by-step visualization output. Changes to the visualization code could silently break the generated DOT output. Golden tests are needed to catch regressions.

## Test Inputs

LL tests (using `LLParser.parseWithSteps` with k=1, then `LLStepVisualizer.renderSteps`):

| Test | Grammar | Input | Golden File |
|------|---------|-------|-------------|
| LL grammar1 "a b" | `S -> a S b S \| eps` | `"a b"` | `ll_grammar1_ab.dot` |
| LL grammar1 "a a b a b b" | `S -> a S b S \| eps` | `"a a b a b b"` | `ll_grammar1_aababb.dot` |

LR tests (using `LRAutomaton.augmentGrammar`, `LRParser.buildSLR1Table`, `LRParser.parseWithSteps`, then `LRStepVisualizer.renderSteps`):

| Test | Grammar | Input | Golden File |
|------|---------|-------|-------------|
| LR grammar3 "a a" | `S -> a S \| a` | `"a a"` | `lr_grammar3_aa.dot` |
| LR grammar7 "x + x" | Expression grammar | `"x + x"` | `lr_grammar7_xplusx.dot` |

## Golden File Format

Each golden file contains the concatenated DOT content for all steps, with step headers:

```
--- Step 0 ---
digraph StackTree {
...
}

--- Step 1 ---
digraph StackTree {
...
}
```

## Files to Create/Modify

1. **New**: `tests/FLPQ.Printers.Tests/LLStepsGoldenTests.fs`
2. **New**: `tests/FLPQ.Printers.Tests/LRStepsGoldenTests.fs`
3. **New**: `tests/FLPQ.Printers.Tests/GoldenData/ll_grammar1_ab.dot`
4. **New**: `tests/FLPQ.Printers.Tests/GoldenData/ll_grammar1_aababb.dot`
5. **New**: `tests/FLPQ.Printers.Tests/GoldenData/lr_grammar3_aa.dot`
6. **New**: `tests/FLPQ.Printers.Tests/GoldenData/lr_grammar7_xplusx.dot`
7. **Modify**: `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — add compilation entries and golden data `.dot` glob

## Implementation Steps

1. Create golden dot reference files first (by running the generation code, saving output)
2. Create test files with golden tests
3. Update `.fsproj`
4. Format, build, test
