# Detailed Plan: Task 272 — SPPF TikZ: resizebox → adjustbox (max width + max totalheight)

## Task description (verbatim)

272. For all tikz-based SPPF visualization in all algorithms (CYK, Valiant, GLL, RNGLR) replace resizebox with ajustbox. Use ajustbox with following parameters `\begin{adjustbox}{max width=\textwidth, max totalheight=\textheight} ... \end{ajustbox}`. Generalize existing ajustbox wrapper function to use it with and without max totalheight limit.

## Scope decisions

- "tikz-based SPPF visualization" = the trailing `SPPF (Shared Packed Parse Forest)`
  section of the merged summary, built by `SummaryTeX.sppfSection`
  (`src/FLPQ.Printers/SummaryTeX.fs:384`). In TikZ mode it currently wraps
  `sppf.tikz.tex` with `wrapTikzCenter` (resizebox `\resizebox{0.98\textwidth}{!}`).
  This is the single SPPF placement for all four algorithms — CYK, Valiant, GLL and
  RNGLR all reach it via `buildContent` → `sppfSection`. Changing it once covers all
  four.
- "existing ajustbox wrapper function" = `SummaryTeX.wrapTikzAdjustbox`
  (`src/FLPQ.Printers/SummaryTeX.fs:55`). It is generalized to take a leading
  `limitHeight: bool`: when true the options are
  `max width=\textwidth, max totalheight=\textheight`; when false they stay
  `max width=\textwidth` (today's behavior).
- The SPPF section uses the generalized wrapper with `limitHeight = true`.
- Every existing `wrapTikzAdjustbox` call site passes `false`, so its output is
  byte-identical to today: ext-RSM head (`headerSection`), LL/LR stack step
  (`stackStepSection`), GLL per-step GSS+RSM (`gllStepSection`), RNGLR per-step GSS
  (`rnglrStepSection`).
- DOT mode is unchanged: `sppfSection` still falls back to the dot-compiled
  `dot_pdfs/sppf.pdf` via `\includegraphics`.
- The standalone `sppf.tikz.tex` artifact is a bare `tikzpicture` (no wrapper) and is
  not touched — only the merged-summary wrapping changes.
- Out of scope: the other resizebox figures (LR automaton, GLL input string) keep
  `wrapTikzCenter`; the task scopes the migration to SPPF only.

## Reuse analysis

- **Reused:** `SummaryTeX.wrapTikzAdjustbox` (generalized in place, not duplicated);
  `CliSummaryTests.runWithSummary` / `runWithSummaryEBNF` / `mergedTexPath` helpers and
  the existing fact patterns; the TikZ-mode merged-summary compilation facts in
  `TexCompilationTests.fs` (`GLL/RNGLR merged summary TeX with tikz compiles with lualatex`) — they compile the new SPPF wrapping end-to-end without modification.
- **New:** the `limitHeight` flag on `wrapTikzAdjustbox`; one shared assertion helper
  plus four facts (one per algorithm) for the SPPF max-totalheight adjustbox;
  `summary-tex.md` updates.

### S1: Generalize wrapTikzAdjustbox with a max-totalheight flag [done — 17ffd33]

**Code:**

- `src/FLPQ.Printers/SummaryTeX.fs`:
  - Change `wrapTikzAdjustbox (tikz: string)` to
    `wrapTikzAdjustbox (limitHeight: bool) (tikz: string)`. When `limitHeight` is true
    the adjustbox options are `max width=\textwidth, max totalheight=\textheight`;
    otherwise `max width=\textwidth`. Update the doc comment to describe the flag.
  - Update every existing call site to pass `false`:
    - ext-RSM head (`headerSection`, ~line 168): point-free `wrapTikzAdjustbox` becomes
      `(fun t -> wrapTikzAdjustbox false t)`.
    - LL/LR stack step (`stackStepSection`, ~line 243): `wrapTikzAdjustbox false tikz`.
    - GLL per-step GSS+RSM (`gllStepSection`, ~lines 313–314):
      `wrapTikzAdjustbox false gssTikz` / `wrapTikzAdjustbox false rsmTikz`.
    - RNGLR per-step GSS (`rnglrStepSection`, ~line 370): `wrapTikzAdjustbox false gssTikz`.

**Tests:**

- No new tests (behavior is unchanged for every existing call site). The existing
  adjustbox facts must keep passing unmodified: `LL summary default mode embeds step stack-trees as inline TikZ`, `SLR(1) summary step stack-trees use adjustbox scaling`,
  `GLL summary wraps each step GSS and RSM figure in adjustbox`, `RNGLR summary wraps each step GSS figure in adjustbox`.

**Docs:**

- `docs/developer/summary-tex.md`: update the LaTeX-helper signature to
  `val wrapTikzAdjustbox: bool -> string -> string` and note that the flag adds
  `max totalheight=\textheight` when true.

**Spec:**

- The flag is a leading (curried) parameter, so the one point-free call site
  (`maybe "ext_rsm.tikz.tex" ... wrapTikzAdjustbox`) must be wrapped in a lambda.
- With `limitHeight = false` the emitted TeX is byte-identical to today's output — this
  subtask is a pure refactor with no behavior change.
- `wrapTikzCenter` is untouched here (still used by the LR automaton and GLL input
  string).

### S2: Switch the SPPF section from resizebox to adjustbox with max totalheight [done — 9324c94]

**Code:**

- `src/FLPQ.Printers/SummaryTeX.fs`:
  - `sppfSection` (TikZ branch, ~line 390): replace `wrapTikzCenter tikz` with
    `wrapTikzAdjustbox true tikz`.
  - Update the `sppfSection` doc comment to state that the TikZ figure is wrapped in an
    adjustbox limited to `\textwidth` and `\textheight`.

**Tests:**

- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs`:
  - New shared helper `assertSppfUsesMaxTotalHeightAdjustbox (outDir) (algorithm)` that
    reads the merged TeX and asserts it contains
    `\begin{adjustbox}{max width=\textwidth, max totalheight=\textheight}`. Only the
    SPPF section uses this variant, so the assertion is precise.
  - Four new facts (TikZ mode), one per algorithm: CYK (`runWithSummary "CYK" false`),
    Valiant (`runWithSummary "Valiant" false`), GLL and RNGLR
    (`runWithSummaryEBNF ... "S -> a S b | eps" "a a b b"`).
- Existing facts that must keep passing: the TikZ-mode merged-summary compilation facts
  in `TexCompilationTests.fs` (they compile the new SPPF wrapping), and the GLL/RNGLR
  per-step adjustbox facts — their needle `\begin{adjustbox}{max width=\textwidth}` does
  not match the SPPF variant (the closing brace differs from a comma), so counts are
  unaffected.

**Docs:**

- `docs/developer/summary-tex.md`: update the Design Decisions table — the SPPF section
  now uses `wrapTikzAdjustbox true` (adjustbox, max width + max totalheight) instead of
  `wrapTikzCenter` (resizebox); remove SPPF from the `wrapTikzCenter` row's scope and
  from the "Remaining resizebox inclusions" note.

**Spec:**

- The DOT branch of `sppfSection` is unchanged (`\includegraphics` for
  `dot_pdfs/sppf.pdf`).
- `max totalheight=\textheight` keeps tall SPPF figures within the page height,
  complementing the existing max-width shrink; adjustbox is shrink-only, so small
  figures are never upscaled and node font metrics are preserved.
