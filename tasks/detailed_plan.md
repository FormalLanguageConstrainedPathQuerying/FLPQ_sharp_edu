# Detailed Plan: Task 222 — Improve LR table inclusion to RNGLR summary

### S1: Refactor RnglrTableTeX — extract tabular-only content function

**Code:** `src/FLPQ.Printers/RnglrTableTeX.fs`
**Tests:** Build check; existing golden tests (will need regeneration)
**Docs:** None

**Spec:**
- Create private `buildTabular` function that produces ONLY the `\begin{tabular}{...}...\end{tabular}` block (no center, no wrapper)
- Refactor `tableToTeX` to call `buildTabular` and wrap in `\begin{center}...\end{center}` for backward compatibility
- Create public `tableToTeXTabularOnly` that returns raw tabular without any wrapping
- Similarly refactor `tableToTeXWithHighlights`: extract shared tabular building, keep wrapper

### S2: Add summary wrapper function and update SummaryTeX header

**Code:** `src/FLPQ.Printers/SummaryTeX.fs`
**Tests:** Build check
**Docs:** None

**Spec:**
- Add `wrapTabularResized` function: wraps raw tabular in `\begin{center}\resizebox{0.3\textwidth}{!}{%% TABULAR }\end{center}` — NO math mode, NO inner center
- In `headerSection` for RNGLR: read `rnglr_table.tex`, strip its outer center (or use new tabular-only output), apply `wrapTabularResized`
- Since `tableToTeX` currently outputs center+tabular, the summary wrapper must handle this. Best approach: use `tableToTeXTabularOnly` from runner for summary, or strip center from existing file

### S3: Update RnglrRunner to write tabular-only for summary

**Code:** `src/FLPQ.Cli/RnglrRunner.fs`
**Tests:** Runner tests
**Docs:** None

**Spec:**
- Write `rnglr_table.tex` using the tabular-only function (no center wrapper)
- SummaryTeX will wrap it with `wrapTabularResized`

### S4: Update RNGLR step template — remove math mode from LR table

**Code:** `data/RNGLR_step_template.tex`
**Tests:** Build check
**Docs:** None

**Spec:**
- Replace `$ __LR_TABLE__ $` with raw `__LR_TABLE__` (no math mode)
- Keep resizebox and center: `\begin{center}\resizebox{\textwidth}{!}{%% __LR_TABLE__ }\end{center}`

### S5: Update per-step lr_table.tex generation

**Code:** `src/FLPQ.Printers/RnglrStepVisualizer.fs`
**Tests:** Golden tests (regenerate)
**Docs:** None

**Spec:**
- Per-step `lr_table.tex` should use tabular-only output (no center, no math mode) since the template provides its own wrapping
- Update `renderStep` to use `tableToTeXTabularOnly` or `tableToTeXWithHighlights` without center wrapper

### S6: Regenerate golden data and verify tests

**Code:** Golden files in `tests/FLPQ.Printers.Tests/GoldenData/`
**Tests:** All RNGLR-related tests pass
**Docs:** None

**Spec:**
- Regenerate `rnglr_lr_table_step0.tex` golden file
- Verify all existing tests pass
