# Detailed Plan: Task 286 — Improve Belyanin RPQ step layout

## Task description (verbatim)

```
286. improve belyanin RPQ stepe layout. For now all substeps (+ result) of each step rendered in single line. Split it into several lines: start of step (V, F, graph, automata), graph step, atm step (each on single line) for each active symbol, final (new V, new F, automata, graph)
**[USER GUIDANCE]**: Each substep is one line. Start and final lines are left-to-right F, V, graph, automaton. Each active symbol gets two lines: select formula (N^a)^T \otimes F = [N^a^T] \otimes [F] = [F^a] + automaton figure, then extend formula F^a \otimes G^a = [F^a] \otimes [G^a] = [Extend] + graph figure. The matrix-multiplication formula is rendered as one horizontal line (matrices side by side), not a vertical stack. Remove the whole-step 0.9\textheight adjustbox so a step can span pages; wrap each component instead. Applies to Belyanin RPQ only (Arroyuelo stays as is).
```

## Scope and constraints

Rendering-only task. Touched modules: `BelyaninStepVisualizer`, the matrices
renderer `PathSemiringTeX` (new non-adjustbox "body" variants), the six
templates in `data/`, `Helpers.writeBelyaninStepsVisualization`, and
`SummaryTeX.belyaninStepSection`. The algorithm
(`BelyaninRPQ.evaluateWithTrace`) and its trace are **untouched**; every
artifact is still derived from the existing trace.

Book reference: Chapter 11, `02_BFS.tex`, algorithm `algo:RPQ_BFS_semiring`.
This task changes presentation only, not the math.

## Target layout (per step)

```
[ F ] [ V ] [ graph ] [ automaton ]                          <- start line (4 blocks)
[ (N^a)^T \otimes F = [N^a^T] \otimes [F] = [F^a] ] [ automaton ]   <- atm step line
[ F^a \otimes G^a = [F^a] \otimes [G^a] = [Extend] ] [ graph ]      <- graph step line
... one (atm, graph) pair of lines per active symbol, trace order ...
[ New F ] [ V ] [ graph ] [ automaton ]                      <- final line (4 blocks)
```

- Start/final order: **F, V, graph, automaton** left-to-right (same for both).
- Each active symbol: two lines, select/automaton first, then extend/graph.
- The multiplication formula is **one horizontal math line**, wrapped once in
  `\begin{adjustbox}{max width=\textwidth}` and shrunk to the formula column.
- The whole step is **no longer** wrapped in `wrapStepAdjustbox`, so a tall step
  can break across pages. Each component keeps its own width-limited adjustbox.
  This applies to **Belyanin only** — Arroyuelo keeps `wrapStepAdjustbox`.

## Reuse

- `MatrixTeX.toTeXStyled` already exposes a `useAdjustbox` flag; the new raw
  matrix bodies reuse it with `useAdjustbox = false`.
- `SummaryTeX.wrapTikzAdjustboxColumn` wraps the per-label/start/end figures.
- `Helpers.writeOutputFile` and the existing `writeBoundary` shape are reused;
  only the boundary file names change.
- Template-fill pattern (`SummaryTeX.fill`, `readIfExists`) is reused; only the
  placeholder names and six template files change.
- No new modules; the boundary record is split, not replaced.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Boundary matrices split into `Frontier` + `Visited` artifacts | The start/final line places F and V in separate side-by-side minipages; a single combined string cannot be split by a template |
| Artifact names `frontier_{start,end}.tex` / `visited_{start,end}.tex` | Mirrors the F/V notation; `matrices_*` no longer describes the content |
| One-line formulas use raw matrix bodies wrapped once in an adjustbox | Avoids nested adjustbox and the current paragraph-per-matrix vertical stack; a single shrink keeps the expression on one visual line |
| Remove `wrapStepAdjustbox` for Belyanin only | User guidance: a step may span pages; Arroyuelo must stay one-page |
| `\hfill%` separators and widths summing to \<= 0.98 | The outer shrink box is gone, so lines must fit `\textwidth` without Overfull boxes |

## Subtasks

### S1: Split the step-boundary artifacts and rebuild the step layout

**Status:** done (dc66a96)

This is one atomic pipeline change: the record field, the writer, the summary
builder, and the templates must change together to keep the CLI output
coherent.

**Code:**

- `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — replace
  `BelyaninBoundaryVisual.Matrices: string` with `Frontier: string` and
  `Visited: string`; replace `renderMatricesStart`/`renderMatricesEnd` with four
  single-block renderers (`renderFrontierStart`, `renderVisitedStart`,
  `renderFrontierEnd`, `renderVisitedEnd`); wire `renderSteps`.
- `src/FLPQ.Cli/Helpers.fs` — `writeBelyaninStepsVisualization` writes
  `frontier_start.tex` + `visited_start.tex` and, for non-init steps,
  `frontier_end.tex` + `visited_end.tex`; update the doc comment.
- `src/FLPQ.Printers/SummaryTeX.fs` — `belyaninStepSection` fills the new
  placeholders from the split artifacts and returns
  `[ header; filledTemplate; "" ]` for Belyanin (no `wrapStepAdjustbox`).
- `data/Belyanin_step_template.tex`, `data/Belyanin_step_tikz_template.tex`,
  `data/Belyanin_row_end_template.tex`,
  `data/Belyanin_row_end_tikz_template.tex`,
  `data/Belyanin_label_row_template.tex`,
  `data/Belyanin_label_row_tikz_template.tex` — four-block start/end lines and
  `\hfill%` label rows.

**Tests:**

- `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` — update
  `writeStepDir`, the F/V block tests, and the q_i-header/V-content tests to the
  split fields; regenerate the boundary goldens and remove the stale
  `matrices_*` goldens.
- `tests/FLPQ.Printers.Tests/SummaryTexSectionTests.fs` — new placeholders;
  assert Belyanin no longer emits `max totalheight=0.9\textheight`.
- `tests/FLPQ.Cli.Tests/BelyaninRunnerTests.fs` — expected artifact names.
- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` — update the "keeps each step on one
  page" comment.

**Docs:** `docs/developer/belyanin-step-viz.md`,
`docs/developer/FLPQ.Cli.md`, `docs/user/cli.md`,
`docs/developer/summary-tex.md` — updated in S4.

**Spec:**

- `renderFrontierStart step = @"$\text{F}$" + "\n" + matrix step.M`.
- `renderVisitedStart step = @"$\text{V}$" + "\n" + matrix (P \ M)`.
- `renderFrontierEnd step = @"$\text{New } F =$" + "\n" + matrix step.NewM`.
- `renderVisitedEnd step = @"$\text{V}$" + "\n" + matrix step.P`.
- Start placeholders: `__FRONTIER_START__`, `__VISITED_START__`,
  `__GRAPH_START_TIKZ__`/`__GRAPH_START_PDF__`,
  `__AUTOMATON_START_TIKZ__`/`__AUTOMATON_START_PDF__`.
- End placeholders: `__FRONTIER_END__`, `__VISITED_END__`,
  `__GRAPH_END_TIKZ__`/`__GRAPH_END_PDF__`,
  `__AUTOMATON_END_TIKZ__`/`__AUTOMATON_END_PDF__`.
- Start/end lines: four `[t]` minipages (F, V, graph, automaton), separated by
  `\hfill%`, widths summing to \<= 0.98\\textwidth; F/V minipages keep the
  `\begingroup \setlength{\textwidth}{\linewidth}` group.
- Label rows: keep the existing placeholders; switch `~` separators to
  `\hfill%` and set formula/figure widths to fit.
- Existing adjustbox-wrapped matrix functions and all Arroyuelo output stay
  byte-identical.

### S2: Render the select/extend formulas as one horizontal line

**Status:** done (47f248d)

**Code:**

- `src/FLPQ.Printers/PathSemiringTeX.fs` — add compact (no-adjustbox, no `$`)
  matrix-body functions reusing `MatrixTeX.toTeXStyled` with
  `useAdjustbox = false`.
- `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — rewrite
  `selectFormula`/`extendFormula` to compose one math expression and wrap it once
  in `\begin{adjustbox}{max width=\textwidth}`.

**Tests:** `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` —
update the explicit-matrix assertions and add a one-line structure test;
regenerate the select/extend goldens.

**Docs:** `docs/developer/path-semiring-tex.md`,
`docs/developer/belyanin-step-viz.md` — updated in S4.

**Spec:**

- Body helper emits `\begin{pNiceMatrix}...\end{pNiceMatrix}` with no `$` and no
  adjustbox.
- `selectFormula` produces
  `\begin{adjustbox}{max width=\textwidth}` + `$` +
  `(N^{a})^T \otimes F =` + body(transpose N) + `\otimes` + body(F) + `=` +
  body(Select) + `$` + `\end{adjustbox}`, all on one visual line.
- `extendFormula` produces the analogous
  `F^{a} \otimes G^{a} = body(Select) \otimes body(G) = body(Extend)`.
- Existing adjustbox-wrapped matrix functions and all Arroyuelo output stay
  byte-identical.

### S3: Regenerate the example visualization and tune the layout

**Status:** done — regenerated `viz_output/BRPQ` (gitignored); merged PDF compiles with
zero Overfull boxes and spans 8 pages (was 3), start/final lines are F, V, graph,
automaton, each label gives two one-line products, no width tuning needed.

**Code:** adjust minipage widths in the S1 templates only if the rendered layout
is illegible.
**Tests:** manual/visual verification only (`viz_output/BRPQ`, gitignored).
**Docs:** none beyond S4.

**Spec:**

- Run the Belyanin RPQ summary on `data/example_regexp.txt` +
  `data/example_graph.txt` and compile the merged PDF; confirm the start/final
  lines show F, V, graph, automaton; each symbol shows the two lines; the
  formula is one horizontal line; a step can span pages.

### S4: Update documentation and format

**Status:** done — `belyanin-step-viz.md`, `summary-tex.md`, `path-semiring-tex.md`,
`FLPQ.Cli.md`, and `user/cli.md` updated alongside S1/S2, plus the Arroyuelo-only note on
`wrapStepAdjustbox` and the page-spanning rationale; all changed `.md` files mdformat-clean.

**Code:** no source changes.
**Tests:** none.
**Docs:** `docs/developer/belyanin-step-viz.md`,
`docs/developer/summary-tex.md`, `docs/developer/path-semiring-tex.md`,
`docs/developer/FLPQ.Cli.md`, `docs/user/cli.md`; run `mdformat` on every
changed `.md`.

**Spec:**

- Replace "three-line" wording and `matrices_start`/`matrices_end` references.
- Document the one-line formula, the split artifacts, the page-spanning change,
  and that Arroyuelo is unchanged.

## Design Notes

### User guidance (authoritative)

Recorded verbatim in the task-log entry and the "Task description (verbatim)"
section above. Summary: one substep per line; start/final F, V, graph,
automaton; two lines per active symbol (select+automaton, extend+graph);
formula on one horizontal line; remove the whole-step height cap for Belyanin;
Arroyuelo untouched.
