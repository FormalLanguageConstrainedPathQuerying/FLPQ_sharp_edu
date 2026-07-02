# Global Plan: Tasks 88--91

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 88 | Fix nonterminals rendering in TeX. Generate N_i names in CNF, remove regex from renderers. | Refactoring | Pending |
| 89 | Fix input rendering for LL and LR. Render Terminal content without type wrappers. | Bug fix | Pending |
| 90 | Improve LL and LR stacks. Stack frame contains only derivation tree node, not Symbol+Node pair. | Refactoring | Pending |
| 91 | Improve LL and LR steps visualization. Render combined stack-tree structure directly without splitting. | Refactoring | Pending |

## Dependencies

```
Task 88 ── independent (CNF name generation + printers cleanup)
Task 89 ── independent (printer change: use symbolPrinter instead of string)
Task 90 ── independent (changes LLStackFrame type, updates parser)
Task 91 ── depends on 90 (uses simplified stack type for direct rendering)
```

Tasks 88, 89, and 90 are independent of each other. Task 91 depends on Task 90.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 88 | `src/FLPQ.Languages/Grammar.fs`, `src/FLPQ.Printers/CykTeX.fs`, `src/FLPQ.Printers/GrammarTeX.fs`, `src/FLPQ.Printers/ValiantTeX.fs` | None (no overlap with other tasks) |
| 89 | `src/FLPQ.Printers/LLStepVisualizer.fs`, `src/FLPQ.Printers/LRStepVisualizer.fs`, `src/FLPQ.Printers/DerivationTreeDot.fs`, `src/FLPQ.Cli/Program.fs`, test files | None (no overlap with other tasks) |
| 90 | `src/FLPQ.Languages/VisualizationTypes.fs`, `src/FLPQ.Languages/LLParser.fs`, `src/FLPQ.Printers/LLStepVisualizer.fs` | 91 (same files) |
| 91 | `src/FLPQ.Languages/VisualizationTypes.fs`, `src/FLPQ.Languages/LLParser.fs`, `src/FLPQ.Languages/LRParser.fs`, `src/FLPQ.Printers/DerivationTreeDot.fs`, `src/FLPQ.Printers/LLStepVisualizer.fs`, `src/FLPQ.Printers/LRStepVisualizer.fs` | 90 (same files) |

## Execution Order

1. **Task 88** — Fix CNF nonterminal naming:
   - Change `freshStringNonterminal` to generate `N_i` instead of `N_CNF_{i}`
   - Remove regex-based `shortNtName` from `ValiantTeX.fs`, `GrammarTeX.fs`, `CykTeX.fs`
   - Replace with identity or simple `string n` since names are already correctly formatted

2. **Task 89** — Fix input rendering for LL and LR:
   - Change `LLStepVisualizer.renderSteps` to accept and use a symbol visualizer (already does)
   - Change `LRStepVisualizer.renderSteps` to accept and use a symbol visualizer (already does)
   - Fix `Program.fs:160,183` to pass `symbolPrinter` instead of `string`
   - Fix test files to pass a proper unwrapping function instead of `string`

3. **Task 90** — Improve LL stack frame type:
   - Change `LLStackFrame` from `LLFrame of Symbol * DerivationTree` to `LLFrame of DerivationTree`
   - Update `LLParser.fs` to extract symbol from tree node instead of storing it separately
   - Update `LLStepVisualizer.fs` to work with the new type

4. **Task 91** — Improve visualization:
   - Remove separate `tree` field from `LLParsingStep` and `LRParsingStep` — the stack already contains the combined structure
   - `recordStep` no longer builds a synthetic `currentTree`
   - Render the combined stack-tree directly from stack frames (for LR: all frames including states are in the stack; for LL: tree nodes are leaves)
   - For LL: render derivation trees rooted at non-leaf stack nodes; leaves in the stack are identified by position
   - For LR: render partial trees rooted at LRSymbol nodes; LRState frames shown as labeled boxes
   - All stack items placed at same rank

## Shared Infrastructure

- Task 88 shares `freshStringNonterminal` in Grammar.fs with CYK/Valiant callers
- Tasks 90+91 share `VisualizationTypes.fs` changes

## Architecture Alignment

- **Task 88**: CNF should generate proper display names, not rely on regex in printers. This aligns with the principle that data types should carry correct information.
- **Task 89**: Consistent use of `symbolPrinter` across CLI — no `string` (F# identity) in visualization path.
- **Task 90**: Remove redundant Symbol from stack frame — LR already has this design (just tree node). LL should match.
- **Task 91**: Render combined structure directly — eliminates the fragile tree/stack matching by node identity in `DerivationTreeDot.fs`.
