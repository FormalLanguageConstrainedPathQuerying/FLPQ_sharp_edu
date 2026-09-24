# Detailed Plan: Task 282 — Improve RPQ visualization

## Task description (verbatim)

```
282. Improve RPQ visualization. 1. Unify CLI argumanes. No separated key for graph. it is just an input. Also unify grammar and regexp keys: In all cases it is a query (like in RPQ or CFPQ). 2. Imprve hraph visualization in steps. First, it friquently does not fit page. Does ajustbox used appropriately? Does template configured correctly? Second, It contains edges that draw fully overlapped (eg two edges between v2 and v3 in example input). Can such cases be automatically handled to draw curve edges without overlapping?
      **[USER GUIDANCE]**: Do not touch GLL and RNGLR for now.
```

## Scope and constraints

- RPQ algorithms only: ArroyueloRPQ and BelyaninRPQ (runners, step visualizers,
  `RpqGraphViz`, step templates, summary sections). **GLL and RNGLR visualization
  output must stay byte-identical** (user guidance). Mechanical signature updates at
  GLL/RNGLR call sites are allowed only when the passed value preserves behavior
  exactly (S2 passes `false` for the new bending flag).
- The CLI unification (item 1) necessarily touches the shared CLI layer
  (`AlgorithmTypes`, `Program`) and all CLI tests — that is what "unify" means;
  parsing-algorithm behavior is unchanged, only the flag names change.

## Empirical findings (verified before planning)

1. **Vertical overflow (confirmed):** compiling the current Belyanin merged summary
   (`data/example_regexp.txt` + `data/example_graph.txt`) yields
   `Overfull \vbox (61.7pt)` and `(69.9pt)` on the step pages — the left-column
   matrix stack (up to 8 `pNiceMatrix` blocks) exceeds `\textheight`.
2. **Horizontal fit bug (confirmed by inspection):** `SummaryTeX.wrapTikzAdjustbox`
   limits figures to `max width=\textwidth`, but RPQ step figures sit inside
   minipage columns of `0.46\textwidth` — a wide graph figure is allowed to be up to
   the full page width and overflows its column. The templates'
   `\includegraphics[width=\textwidth]` (DOT mode) has the same bug. The existing
   doc claim "inside a step minipage `\textwidth` equals the column width"
   (`docs/developer/summary-tex.md`) is wrong — a minipage does not change
   `\textwidth` (which is why the templates carry the
   `\setlength{\textwidth}{\linewidth}` hack for matrices).
3. **Reciprocal edges in DOT are fine:** `dot` automatically draws a reciprocal pair
   (`v2 -> v3`, `v3 -> v2`) as two non-overlapping curves (verified via `-Tsvg` edge
   paths). No DOT change needed.
4. **Reciprocal edges in TikZ overlap:** `GssTikz.toTikzFromSets` emits both
   directions as straight lines on top of each other. Giving both edges of a pair
   `bend left=15` produces two symmetric arcs (standard TikZ idiom).
5. **`\subsection*` cannot run inside an adjustbox** (verified: LaTeX error
   "Something's wrong--perhaps a missing \\item"), so the step heading must stay
   outside the shrinking box. Measured in the fixed summary geometry (12pt article,
   landscape A4, 1cm margins; `\textheight` ≈ 540pt): the subsection heading consumes
   ≈ 46pt (8.6%). Limiting step content to `0.9\textheight` leaves ≈ 54pt of headroom
   and removes the overflow (verified: no Overfull in a test document).
6. **Pre-existing gate blocker found while running S2 tests:**
   `AutomatonVisualizationTests` referenced goldens `dfa_aplus.Dot` / `nfa_aplus.Dot`
   (uppercase extension) but the committed files are lowercase `.dot` — on case-sensitive
   Linux `File.Exists` fails, so two golden tests always failed. Fixed by lowercasing
   the two test strings (separate `fix(tests)` commit, not part of S2's scope).
7. **Pre-existing flake found while running S3 tests:** `GllRunnerTests` failed once in
   the full CLI suite with `ArgumentOutOfRangeException` inside `StringBuilder.ToString()`
   (the captured `StringWriter`). Root cause: `withCapturedOutput` redirects the
   process-global `Console.Out`, but xUnit ran other collections (e.g. `CliSummaryTests`)
   in parallel, and their console writes raced the capture. The four capturing modules
   already share one xUnit collection; that only serializes them against each other.
   Fixed by disabling xUnit parallelization for the whole CLI test assembly
   (`xunit.runner.json` with `parallelizeTestCollections: false`) — process-global
   console state is fundamentally incompatible with cross-collection parallelism
   (separate `fix(282-S3)` commit, not part of S3's scope).

## Reuse analysis (reusing skill)

- Reused as-is: `SummaryTeX.wrapCenter`, `section`, `readIfExists`;
  `GssDot.toDotFromSets` (unchanged); `ExternalTools.compileTexStringWithTemplateLog`
  (returns lualatex stdout — used for Overfull assertions);
  `ExternalTools.compileTexFile` (leaves `<name>.log` in the output dir — used by the
  end-to-end no-Overfull test); `GoldenHelpers.verifyGolden` + `CREATE_GOLDEN_FILES=1`
  workflow; Argu `Arguments` pattern.
- Generalized: none (generalizing `wrapTikzAdjustbox` to `\linewidth` would change
  GLL/RNGLR step output — forbidden by user guidance).
- New: `SummaryTeX.wrapTikzAdjustboxColumn`, `SummaryTeX.wrapStepAdjustbox`,
  `GssTikz.toTikzFromSets` trailing `bendReciprocalEdges: bool` parameter,
  `AlgorithmTypes.Query` case.
- Note (pre-existing, out of scope): `docs/developer/gss-dot.md` / `gss-tikz.md` links
  in `rpq-graph-viz.md` point to non-existent files.

## Subtasks

### S1: Unify CLI arguments — one `-q` query and `-i` input for all algorithms [done — cf23db5]

**Code:**

- `src/FLPQ.Cli/AlgorithmTypes.fs`: replace the `Grammar` (`-g`) and `Regexp` (`-r`)
  cases with a single `Query` case (`<AltCommandLine("-q")>`); delete the `GraphFile`
  (`--graph`) case. Usage strings: `Query` = "Path to the query file: grammar (.bnf)
  for parsing algorithms, regexp (EBNF; the first rule's RHS is the query) for RPQ
  algorithms"; `Input` = "Path to the input file: input string for parsing
  algorithms, graph (start vertices line + 'from label to' edges) for RPQ
  algorithms".
- `src/FLPQ.Cli/Program.fs`: `runParsingAlgorithm` reads `AlgorithmTypes.Query`
  instead of `Grammar`; the Arroyuelo/Belyanin dispatch reads `Query` + `Input` and
  passes them to the runners.
- `src/FLPQ.Cli/ArroyueloRunner.fs`, `src/FLPQ.Cli/BelyaninRunner.fs`: rename
  parameters `regexpFile`/`graphFile` → `queryFile`/`inputFile` (semantics unchanged:
  the query is parsed as a regexp, the input as a graph).

**Tests:**

- `tests/FLPQ.Cli.Tests/ProgramDispatchTests.fs`: `-g` → `-q` everywhere; RPQ tests
  switch `-r <regexp> --graph <graph>` → `-q <regexp> -i <graph>`; rename the
  "without regexp file" / "without graph file" facts to "without query file" /
  "without input file" (same non-zero-exit assertions).
- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs`: `runWithSummary` and
  `runWithSummaryEBNFUseDot` use `-q`; `runRpqWithSummary` uses `-q`/`-i`.
- `tests/FLPQ.Cli.Tests/ErrorPathTests.fs`: `-g` → `-q`.

**Docs:**

- `docs/user/cli.md`: flag table — drop the `-g`, `-r`, `--graph` rows, add the `-q`
  row, update the `-a` and `-i` descriptions; update the example-usage commands.
- `docs/developer/FLPQ.Cli.md`: update the Arguments description (single query/input
  pair for all algorithms).

**Spec:**

- Old flags (`-g`, `-r`, `--graph`) are removed, not aliased — Argu rejects unknown
  arguments, so stale invocations fail with a usage message.
- Parsing algorithms keep identical behavior; only the flag spelling changes.
- RPQ runners keep parsing the query via `RpqInput.parseRegexpFile` and the input
  via `GraphReader.parseGraphFile`.

### S2: Bend reciprocal edges in the RPQ graph TikZ rendering [done — 2e81c3c]

**Code:**

- `src/FLPQ.Printers/GssTikz.fs`: add a trailing `bendReciprocalEdges: bool`
  parameter to `toTikzFromSets`. When true, an edge `(u, v)` with `u <> v` and
  `(v, u) ∈ activeEdges` gets `, bend left=15` in its TikZ edge attributes — in all
  three rendering branches (highlighted red: `["l", red, bend left=15]`; plain
  labeled: `["l", bend left=15]`; unlabeled: `[bend left=15]`). Self-loops are never
  bent. When false, output is byte-identical to today.
- `src/FLPQ.Printers/RpqGraphViz.fs`: pass `true` (the input graph is the consumer).
- `src/FLPQ.Printers/GllStepVisualizer.fs` (2 call sites),
  `src/FLPQ.Printers/RnglrStepVisualizer.fs`,
  `src/FLPQ.Printers/ArroyueloStepVisualizer.fs` (tree figure): pass `false` —
  mechanical signature update, GLL/RNGLR/tree output unchanged.

**Tests:**

- `tests/FLPQ.Printers.Tests/GssDotTests.fs` (`GssTikzTests` module): add `false` to
  the two existing call sites; new facts — (a) reciprocal pair with empty labels and
  `true` → both edges render `[bend left=15]`; (b) same graph with `false` → no
  "bend" anywhere; (c) one-way edge with `true` → no bend.
- `tests/FLPQ.Printers.Tests/RpqGraphVizTests.fs`: new facts on the example graph
  (has the 2↔3 reciprocal pair) — (a) plain render: tikz contains
  `v2 ->["c", bend left=15] v3` and `v3 ->["b", bend left=15] v2`, while the
  one-way edge `v0 ->["a"] v1` has no bend; (b) highlight `(2,3)`: the red edge keeps
  its label and gains the bend (`["c", red, bend left=15]`).
- Golden regeneration: delete `tests/FLPQ.Printers.Tests/GoldenData/belyanin_step_{0..5}_graph.tikz`,
  run the Belyanin golden tests with `CREATE_GOLDEN_FILES=1`, copy the regenerated
  files back, re-run green. (Arroyuelo test graph has no reciprocal pair — its
  goldens stay unchanged; verify.)

**Docs:**

- `docs/developer/rpq-graph-viz.md`: design-decision row — reciprocal edge pairs are
  bent in TikZ (`bend left=15` on both directions) because straight lines would fully
  overlap; DOT needs no change (Graphviz separates reciprocal edges automatically).
- `docs/developer/rnglr.md` GSS design table: one-line note that the new trailing
  `bendReciprocalEdges` parameter defaults to off for GLL/RNGLR (behavior unchanged).

**Spec:**

- Bend angle is a fixed constant `15` (degrees), matching Graphviz's gentle
  reciprocal-edge curvature.
- The decision is per edge: `(u, v)` bends iff its reverse is also drawn.
- DOT output of `RpqGraphViz.renderGraph` is unchanged.

### S3: Fit RPQ steps on one page — column-width figure wraps + whole-step adjustbox [done — 4f63b9f]

**Code:**

- `src/FLPQ.Printers/SummaryTeX.fs`:
  - New `wrapTikzAdjustboxColumn (tikz: string) : string` — centered adjustbox with
    `max width=\linewidth` (shrink-only). Inside the RPQ step templates' minipage
    columns `\linewidth` is the column width, so over-wide figures shrink to fit
    their column.
  - New `wrapStepAdjustbox (tex: string) : string` — adjustbox with
    `max width=\textwidth, max totalheight=0.9\textheight` wrapping the entire filled
    step template; the 10% headroom accommodates the step's `\subsection*` heading,
    which must stay outside the box (sectioning commands cannot run inside an
    adjustbox — verified). Shrink-only: short steps are never scaled.
  - `arroyueloStepSection`: TikZ mode wraps the tree and graph figures with
    `wrapTikzAdjustboxColumn` (replacing `wrapTikzAdjustbox false`); both modes wrap
    the filled template with `wrapStepAdjustbox`.
  - `belyaninStepSection`: same for the automaton and graph figures.
- `data/Arroyuelo_step_template.tex`, `data/Belyanin_step_template.tex`:
  `\includegraphics[width=\textwidth,keepaspectratio]` →
  `width=\linewidth` (DOT-mode figures must fit their minipage column).

**Tests:**

- `tests/FLPQ.Printers.Tests/SummaryTexSectionTests.fs`: update the four RPQ step
  section facts — dot-mode exact-line assertions now expect the outer adjustbox
  wrapper; TikZ-mode facts assert the figure wrap is
  `\begin{adjustbox}{max width=\linewidth}`. New fact: each filled RPQ step contains
  exactly one `max totalheight=0.9\textheight` adjustbox (both modes).
- `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` and
  `ArroyueloStepVisualizationTests.fs`: update the private `fillStepTemplate` to
  mirror the real pipeline — figures via `SummaryTeX.wrapTikzAdjustboxColumn`, whole
  template via `SummaryTeX.wrapStepAdjustbox`. Keep the existing compile facts; new
  fact per algorithm: every filled step compiles with lualatex and the output
  contains no "Overfull" (via
  `ExternalTools.compileTexStringWithTemplateLog` with `tex_summary_template.tex`).
- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs`: new end-to-end facts — for both RPQ
  algorithms in TikZ and DOT mode, the merged summary compiles and its lualatex log
  (left by `ExternalTools.compileTexFile` in the output dir) contains no "Overfull".

**Docs:**

- `docs/developer/summary-tex.md`: document `wrapTikzAdjustboxColumn` and
  `wrapStepAdjustbox`; add design-decision rows for the RPQ step fit (column-width
  figure wrap; whole-step shrink with 0.9 headroom and why the heading stays
  outside); correct the false claim that "`\textwidth` equals the column width inside
  a step minipage".
- `docs/developer/arroyuelo-step-viz.md`, `docs/developer/belyanin-step-viz.md`:
  note the DOT-template `\linewidth` includegraphics and the whole-step adjustbox
  wrap applied by the summary section builder.

**Spec:**

- The per-step artifact files (written by the runners) are unchanged — wrapping is
  applied only at summary time, so step-directory output stays byte-identical except
  for S2's graph TikZ bending.
- `wrapTikzAdjustbox` (the `\textwidth` variant) stays as-is for GLL/RNGLR/SPPF/LL/LR
  and the RPQ header — untouched per user guidance.
- Verification: run both RPQ algorithms with `-s` (TikZ and DOT), compile the merged
  TeX twice, grep the log for "Overfull" — must be absent.

## Execution order

S1 → S2 → S3. Each subtask is an independent, compilable, testable increment with its
own commit (`feat(282-SN)` / `docs` as appropriate).
