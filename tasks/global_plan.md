# Global Plan: Tasks 98--99

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 98 | Improve LR table rendering. Fix hline count and column separators. | Bug fix | Pending |
| 99 | Improve generation for LR family. CLI variants, summary names, example data. | Enhancement | Pending |

## Dependencies

```
Task 98 ── independent (only changes LRTableTeX.fs)
Task 99 ── independent (changes CLI types, LRRunner, Summary, data/)
```

Both tasks are independent of each other.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 98 | `src/FLPQ.Printers/LRTableTeX.fs` | None |
| 99 | `src/FLPQ.Cli/AlgorithmTypes.fs`, `src/FLPQ.Cli/Program.fs`, `src/FLPQ.Cli/LRRunner.fs`, `src/FLPQ.Cli/Summary.fs`, `data/` | None (98 touches printers, 99 touches CLI and data) |

## Execution Order

1. **Task 98** — Fix LR table TeX rendering:
   - Fix column spec: remove the trailing ` | ` from `actionCols` that creates three bars between ACTION and GOTO sections (should be `||` only)
   - Fix row formatting: header row only gets `\hline\hline`, data rows use `\hline N & ... \\ \hline` (single hline for each)

2. **Task 99** — Improve LR family CLI and examples:
   - Expand `Algorithm` DU from `LR` to `LR0 | SLR1 | CLR1`
   - Update `LRRunner.runLR` to accept variant and dispatch to correct table builder
   - Update `Program.fs` to handle all three LR variants
   - Update `Summary.fs` to produce precise names: "LR(0)", "SLR(1)", "CLR(1)"
   - Add example grammars and inputs in `data/` for LR algorithms (e.g., arithmetic expression grammar from task 11, which is SLR(1)-compatible)

## Shared Infrastructure

None. These tasks touch entirely separate areas of the project.

## Architecture Alignment

- **Task 98**: Aligns with the book's tabular format (Chapter 7, `05_BottomUp.tex`) which shows `\hline\hline` after the header row and single `\hline` for data rows.
- **Task 99**: Makes CLI interface precise (LR(0) vs SLR(1) vs CLR(1)), mirroring the book's categorization of LR parser types.
