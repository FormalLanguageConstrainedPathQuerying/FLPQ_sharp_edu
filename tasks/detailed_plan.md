# Detailed Plan: Task 252 — Improve Valiant Table Rendering

**Status: COMPLETE** (commit `3eb45a7`)

## Task Description
Improve Valiant (and modified Valiant) table rendering:
1. Use `\rectanglecolor` instead of `\Block` to highlight submatrices
2. Wrap matrix with `\begin{adjustbox}{max width=\textwidth}...\end{adjustbox}`
3. Use `$` instead of `\[` and `\]` inside adjustbox

---

### S1: Modify MatrixTeX.toTeXStyled — add useRectangleColor and useAdjustbox params

**Code:** `src/FLPQ.Printers/MatrixTeX.fs`
**Tests:** None — existing golden/compilation tests verify rendering output
**Docs:** None — internal refactoring, no API changes

**Spec:**
- Add `useRectangleColor: bool` parameter (default `false`) to `toTeXStyled`
- Add `useAdjustbox: bool` parameter (default `false`) to `toTeXStyled`
- When `useRectangleColor=true`:
  - Instead of embedding `\Block[...]{rowCount-colCount}{content}` in the cell at (StartRow, StartCol), generate `\CodeBefore` section before the matrix body with `\rectanglecolor{FILL_COLOR!20}{rowStart-rowEnd}{colStart-colEnd}` commands
  - The `\rectanglecolor` indices are 1-based LaTeX coordinates including headers:
    - rowStart = block.StartRow + dataRowOffset + 1
    - rowEnd = rowStart + block.RowCount - 1
    - colStart = block.StartCol + dataColOffset + 1
    - colEnd = colStart + block.ColCount - 1
  - Remove `\Block` cell wrapping when `useRectangleColor=true` — cells render as plain (with optional `\cellcolor` for highlights)
  - Need `\Body` marker after `\CodeBefore` section in nicematrix
  - The color for `\rectanglecolor` uses fill-only (no draw), like `red!10` for CurrentStepSubmatrix and `{color}!20` for Submatrix indices
- When `useAdjustbox=true`:
  - Prepend `\begin{adjustbox}{max width=\textwidth}$`
  - Append `$\end{adjustbox}`
  - This replaces the outer `\[...\]` math wrapping
- Parameters default to `false` — no change for existing callers (CYK, PathIndex, etc.)
- `\setcounter{MaxMatrixCols}` must appear BEFORE `\begin{adjustbox}`, not inside

---

### S2: Update ValiantTeX.fs — pass useRectangleColor=true, useAdjustbox=true

**Code:** `src/FLPQ.Printers/ValiantTeX.fs`
**Tests:** None at this point — golden tests updated in S5
**Docs:** None

**Spec:**
- In `stepToTeX` (line 69): pass `useRectangleColor=true, useAdjustbox=true`
- In `modifiedStepToTeX` (line 110 and 153): pass `useRectangleColor=true, useAdjustbox=true`
- In `sppfStepToTeX` (line 224): pass `useRectangleColor=true, useAdjustbox=true`
- In `sppfModifiedStepToTeX` (line 268 and 319): pass `useRectangleColor=true, useAdjustbox=true`

---

### S3: Update SummaryTeX.fs — change tableStepSection wrapping

**Code:** `src/FLPQ.Printers/SummaryTeX.fs`
**Tests:** Golden/compilation tests updated in S5
**Docs:** None

**Spec:**
- `tableStepSection` currently wraps table content with `wrapMath` which produces `\begin{center}\[...\]\end{center}`
- After the change, the matrix content already has `\begin{adjustbox}{max width=\textwidth}$...$\end{adjustbox}` wrapping
- Change to use `wrapCenter` instead (just `\begin{center}...\end{center}` without `\[...\]`)
- This aligns with the task example which shows `\begin{center}\begin{adjustbox}...\end{adjustbox}\end{center}`

---

### S4: Update LaTeX templates — add adjustbox package

**Code:** `data/tex_summary_template.tex`, `data/tex_template.tex`
**Tests:** None at this point — verified when golden tests regenerate in S5
**Docs:** None

**Spec:**
- Add `\usepackage{adjustbox}` to `tex_summary_template.tex` (used for summary compilation)
- Add `\usepackage{adjustbox}` to `tex_template.tex` (used for golden test compilation)
- Place after `\usepackage{nicematrix}` line

---

### S5: Regenerate golden reference files

**Code:** None
**Tests:** `tests/FLPQ.Printers.Tests/ValiantTraceGoldenTests.fs` — tests must pass with regenerated goldens
**Docs:** None

**Spec:**
- Run golden tests with `CREATE_GOLDEN_FILES=1` to regenerate:
  - `valiant_grammar1_abab.tex`
  - `valiant_modified_grammar1_ab.tex`
- Copy generated files to `tests/FLPQ.Printers.Tests/GoldenData/`
- Verify tests pass

---

### S6: Run all tests and fix any issues

**Code:** Any affected files
**Tests:** Full test suite
**Docs:** None

**Spec:**
- Build entire solution
- Run all tests
- Run fsharplint on changed projects
- Fix any failures

