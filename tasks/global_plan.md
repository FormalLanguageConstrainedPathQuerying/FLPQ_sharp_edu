# Global Plan: Tasks 74--76

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 74 | LL: Single unified stack for symbols and tree | Refactoring | Pending |
| 75 | LR: Single unified stack for symbols and tree | Refactoring | Pending |
| 76 | Common DOT-based stack+tree visualization for LL and LR | Feature | Pending |

## Dependencies

```
Task 74 (LL unified stack) ── independent
Task 75 (LR unified stack) ── independent
Task 76 (visualization) ── depends on both 74 and 75
```

Tasks 74 and 75 modify different source files and can proceed independently.
Task 76 depends on the final types from both 74 and 75.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 74 | `VisualizationTypes.fs`, `LLParser.fs`, `LLStepVisualizer.fs`, `LLVisualizerTests.fs`, tests, docs | Task 76 (visualizer changes) |
| 75 | `LRParser.fs`, `LRStepVisualizer.fs`, `LRVisualizerTests.fs`, tests, docs | Task 76 (visualizer changes) |
| 76 | `VisualizationTypes.fs`, `LLStepVisualizer.fs`, `LRStepVisualizer.fs`, `DerivationTreeDot.fs`, common types, tests | Both 74 and 75 |

## Execution Order

1. **Task 74** — LL unified stack (adds LLStackFrame type, rewrites LL parser)
2. **Task 75** — LR unified stack (verifies/refines LR unified stack, LR already partially unified from task 49)
3. **Task 76** — Common visualization:
   - Update `VisualizationStep` to support DOT-based stack+tree rendering (replacing TeX one-row stack)
   - Input stays as TeX
   - Stack+tree combined into single DOT graph
   - Stack forms linear chain (top-to-bottom)
   - Tree nodes on stack are subtrees with children
   - `rank=same` for stack nodes
   - Create shared functions for LL and LR

## Shared Infrastructure

- `LLStackFrame` type (in `VisualizationTypes.fs`) — unified frame for LL, analogous to `LRSymbol` in LR
- Common DOT generation function for stack+tree visualization (in `DerivationTreeDot.fs` or new file)
- Common `VisualizationStep` structure updated

## Architecture Alignment

- **Separation of data collection and rendering**: `parseWithSteps` collects raw F# data. Converting to DOT/TeX happens in separate modules.
- **LLStackFrame aligns with LRStackFrame**: both carry `Symbol * DerivationTree`.
- **LL no longer has dual stacks**: single `LLStackFrame list` replaces `Symbol list` + `DerivationTree list`.
- **LR already unified (task 49)**: verify consistency, no structural changes needed.
