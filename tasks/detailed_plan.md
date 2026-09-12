# Detailed Plan: Task 271 — Improve GLL and GLR steps rendering

## Task description (verbatim)

271. Improve GLL and GLR steps rendering. Wrap GSS and RSM (rsm for GLL only) in each step with `\begin{adjustbox}{max width=\textwidth} ... \end{adjustbox}`. Use existing wrapper function.

## Scope decisions

- "GLR" = RNGLR, the project's GLR algorithm (`SummaryKind.RNGLR`).
- "Existing wrapper function" = `SummaryTeX.wrapTikzAdjustbox`
  (`src/FLPQ.Printers/SummaryTeX.fs:55`) — it produces exactly
  `\begin{center}\begin{adjustbox}{max width=\textwidth} ... \end{adjustbox}\end{center}`.
- Applies to **TikZ mode** (default). In DOT mode the per-step GSS/RSM figures are
  dot-compiled PDFs included via `\includegraphics[width=\textwidth]`; no existing
  wrapper function covers PDF inclusions, so DOT mode is unchanged.
- The wrap happens at template-fill time in `gllStepSection` / `rnglrStepSection`.
  The now-redundant `\begin{center}...\end{center}` around the GSS/RSM placeholders
  in the two TikZ step templates is removed — the wrapper provides centering.
- Out of scope: the other figures in the step layout (input graph, path index,
  descriptors table) and the minipage column widths — separate concerns.

## Reuse analysis

- **Reused:** `SummaryTeX.wrapTikzAdjustbox` (no new wrapper function);
  `CliSummaryTests.runWithSummaryEBNF` and the existing GLL/RNGLR fact patterns;
  the existing tikz-mode merged-summary compilation facts in `TexCompilationTests.fs`
  (`GLL/RNGLR merged summary TeX with tikz compiles with lualatex`) — they exercise
  the new wrapping end-to-end without modification.
- **New:** two end-to-end assertion facts in `CliSummaryTests.fs`; edits to the two
  TikZ step templates; `summary-tex.md` updates.

### S1: Wrap per-step GSS/RSM figures with adjustbox [done — 90e3cbb]

**Code:**

- `src/FLPQ.Printers/SummaryTeX.fs`:
  - `gllStepSection` (TikZ branch): substitute `wrapTikzAdjustbox gssTikz` for
    `__STEP_GSS_TIKZ__` and `wrapTikzAdjustbox rsmTikz` for `__STEP_RSM_TIKZ__`.
  - `rnglrStepSection` (TikZ branch): substitute `wrapTikzAdjustbox gssTikz` for
    `__STEP_GSS_TIKZ__`.
- `data/GLL_step_tikz_template.tex`: remove `\begin{center}...\end{center}` around
  `__STEP_GSS_TIKZ__` and `__STEP_RSM_TIKZ__` (the wrapper provides centering).
- `data/RNGLR_step_tikz_template.tex`: same for `__STEP_GSS_TIKZ__`.

**Tests:**

- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` — two new facts:
  - `GLL summary wraps each step GSS and RSM figure in adjustbox` — run the GLL
    summary (TikZ mode, grammar `S -> a S b | eps`, input `a a b b`); extract every
    step section (`\subsection*{Initialization}` / `\subsection*{Step N}`) and assert
    each contains exactly two `\begin{adjustbox}{max width=\textwidth}` (GSS + RSM);
    assert the global count equals `1 + 2 * stepCount` (the extra one is the head
    Extended RSM figure from `headerSection`).
  - `RNGLR summary wraps each step GSS figure in adjustbox` — same for RNGLR:
    exactly one per step; global count `1 + stepCount`.
- Existing facts that must keep passing (no modification): the tikz-mode merged
  summary compilation facts in `TexCompilationTests.fs` (they compile the new
  wrapping) and the DOT-mode compilation facts (unchanged code path).

**Docs:**

- `docs/developer/summary-tex.md`: update the `wrapTikzAdjustbox` design-decision row
  (now also used by `gllStepSection` / `rnglrStepSection` for per-step GSS/RSM
  figures; DOT mode keeps `\includegraphics[width=\textwidth]`) and the per-step
  overview bullet.

**Spec:**

- The wrapper is applied unconditionally at substitution time: a missing figure file
  yields an empty adjustbox, which compiles fine and matches today's empty-center
  behavior.
- The DOT branches of both functions are unchanged.
- Template placeholders keep their names (`__STEP_GSS_TIKZ__`, `__STEP_RSM_TIKZ__`);
  only the surrounding centering moves into the wrapper output.
