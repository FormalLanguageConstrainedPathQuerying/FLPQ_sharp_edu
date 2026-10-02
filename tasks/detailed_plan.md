# Detailed Plan: Task 291 — Improve regexp rendering and Arroyuelo RPQ rendering

## Task description (verbatim)

```
291. Improve regexp rendering and Arroyuelo RPQ rendering. Regexp rendering (all places): use dot for concatenation. Arroyuelo RPQ rendering: 1. Use vertical layout for tree. 2. Visualize only steps for intermediate nodes, not leafs. 3. Add default CLI option for reachability only, not path, similarly to BelyaninRPQ semantics. 4. Check that adjust boxes used for tikz and matrices (look at Belyanin RPQ). 5. Improve step layout. Two columns. First is for tree. Second consists of two lines. First one is matrices formula, second is graph figure. 6. On each step for boolean reachability semantics do not highlight paths or edges, but add edges with respect to result matrix with label of form of respective part of regular expression (handled node).
**[USER GUIDANCE]**: (A) Regexp: TeX concatenation renders as `\cdot` in RegexpTeX only; `/` remains the EBNF input syntax; plain-text `Regexp.toString` is unchanged. (B) Arroyuelo gains a `--semantics` option (`reachability` default, `simplePath`), mirroring Belyanin task 288: a Boolean reachability trace plus visualizer, while simplePath preserves the existing path-semiring trace/visualizer (renamed to reflect simple paths). (C) RPQ must not depend on GSS: introduce dedicated RPQ renderers (`RpqGraphDot`/`RpqGraphTikz`, `RegexpTreeDot`/`RegexpTreeTikz`) reusing `AutomatonTikz` primitives; `RpqGraph*` output must stay byte-identical so Belyanin goldens do not change; do not modify `GssDot`/`GssTikz`, which should end up referenced only by the GLL/RNGLR visualizers. (D) Reachability step graph keeps the original input edges and adds one red-bold edge per true cell (u,v) of the handled node's Boolean result matrix M(E), labeled with that node's subexpression; when a derived edge collides with an input edge, the two labels merge into one edge. No path/edge highlighting for reachability. (E) Tree is vertical (root at top, `grow'=down`; DOT `rankdir=TB`). (F) Only intermediate AST nodes (Alt, Seq, Star) produce step directories; the tree figure still shows all nodes including leaves; a single-leaf query produces no step directories (the result is still written). (G) Step layout: left column = regexp tree; right column = two lines, first the matrix formula, second the graph figure; per-component top-aligned adjustboxes (`valign=T`, column width), no whole-step adjustbox, following Belyanin.
```

## Overview

Five workstreams:

1. **Regexp TeX** — concatenation renders as `\cdot` instead of `/` (`RegexpTeX` only).
2. **Decouple RPQ from GSS** — the RPQ input graph (via `RpqGraphViz`) and the regexp AST tree are currently rendered through the GSS stack renderers. Introduce dedicated `RpqGraphDot`/`RpqGraphTikz` (byte-identical output) and `RegexpTreeDot`/`RegexpTreeTikz` (vertical tree). `GssDot`/`GssTikz` are not modified.
3. **Arroyuelo simple-path step visualizer** — split shared code into `ArroyueloStepCommon`, rename the visualizer, render a vertical tree, emit only intermediate (Alt/Seq/Star) steps.
4. **Arroyuelo Boolean reachability** — new Boolean trace in `ArroyueloRPQ`, new reachability visualizer; per-step graph keeps input edges and adds one red-bold edge per true M(E) cell labeled by the handled node's subexpression (labels merged on collision), no path highlights.
5. **CLI + layout + summary** — `--semantics` for Arroyuelo, two-column/two-row step template, per-component top-aligned adjustboxes, summary legend.

## Reuse analysis

- Reuse `BelyaninStepCommon` patterns (`renderVisualizationSteps`, `wrapAdjustbox`, `matrixTileWithBody`) as the template for `ArroyueloStepCommon` (same project, same style).
- Reuse `AutomatonTikz` primitives in the new renderers: `escapeLatex`, `reciprocalBendAttr`, `tikzHeaderWithOptions`, `tikzFooter`, `layeredGraphOptions`, `defaultGrowDirection`; add `treeGrowDirection`/`treeRankDirection`.
- Reuse `DerivationTreeDot.escapeLabel` for DOT labels (as `GssDot` does).
- Reuse `PathSemiringTeX.matrixWithVertexLabels` (simplePath) and `PathSemiringTeX.boolMatrixToTeXWith` (reachability).
- Reuse `RegexpTeX.toTeX` (TikZ derived labels) and `Regexp.toString` (DOT derived labels).
- Reuse `RpqGraphViz` path helpers (`pathHighlights`, `frontierVertices`, `edgeEndpoints`) unchanged.
- Reuse the existing `RpqSemantics` DU and `-semantics` CLI flag; only the `Program`/usage dispatch changes.
- Reuse the Belyanin reachability runner/visualizer as the structural model for the Arroyuelo ones.

---

### S1: Regexp concatenation renders as `\cdot`

**Code:** `src/FLPQ.Printers/RegexpTeX.fs` — in `toTeX`, change the `RSeq` separator from `" / "` to `" \cdot "`; update the module/`toTeX` doc comment.

**Tests:** `tests/FLPQ.Printers.Tests/RegexpTexTests.fs` — update the expected strings of every case containing a sequence: `a \cdot b`, `(a \cdot b) \cdot c`, `(a \cdot b) \mid c`, `(a \cdot b)^*`, `\text{walk} \cdot (O \mid R)^*`. Keep the lualatex compile test (update its regexp if it asserts text). Regenerate goldens that embed `RegexpTeX.toTeX` (`arroyuelo_step_*_tree.tikz`, `arroyuelo_step_*_matrices.tex`, and any runner `regexp.tex` assertions).

**Docs:** `docs/developer/rpq-regexp-viz.md` — table row for `RSeq`, the abstract, and the "Sequence renders as `/`" design-decision row.

**Spec:**

- Only the TeX renderer changes; `EbnfParser.toString` (plain text) and the EBNF input syntax (`/`) are untouched.
- Parenthesization logic is unchanged.
- Golden indices/contents that contain the sequence label must be regenerated (they also change again in S3; regenerate here to keep S1 self-contained).

---

### S2: Dedicated RPQ graph renderers (decouple from GSS)

**Code:**

- New `src/FLPQ.Printers/RpqGraphDot.fs` — `toDot (vertexLabelPrinter: int -> string) (edgeLabelPrinter: int*int -> string) (activeVertices) (activeEdges) (highlightedVertices) (startVertices) (frontierVertices) (highlightedEdges) (pathEdges) : string`. Emits the RPQ input graph in Graphviz DOT, byte-identical to the current `GssDot.toDotFromSets` output for the RPQ parameterization (`rankdir=LR`, `compound=true`, ellipse vertices, fill priority `highlighted > frontier > start`, `color=red, penwidth=2.0` for current edges, `color="#FF9999"` for path edges).
- New `src/FLPQ.Printers/RpqGraphTikz.fs` — `toTikz (...)` with the same parameter set; byte-identical to `GssTikz.toTikzFromSets` for RPQ (`layered layout, nodes={draw, circle}, grow'=right`, fill priority `highlighted > frontier > start` with fills `yellow!20 > lightblue!20 > green!30`, edge styles `red, thick` / `red!40`, reciprocal `bend left=15` via `AutomatonTikz.reciprocalBendAttr`).
- `src/FLPQ.Printers/RpqGraphViz.fs` — replace the `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` calls in `renderGraph` with `RpqGraphDot.toDot` / `RpqGraphTikz.toTikz`. Public API and all other functions unchanged.
- `src/FLPQ.Printers/FLPQ.Printers.fsproj` — add `RpqGraphDot.fs`/`RpqGraphTikz.fs` after `AutomatonTikz.fs` and before `RpqGraphViz.fs`.

**Tests:** `tests/FLPQ.Printers.Tests/RpqGraphVizTests.fs` — add unit tests for the new modules (vertex fill tiers, edge styles, reciprocal bend, empty-label edges). The existing Belyanin step-visualization golden tests (`BelyaninStepVisualizationTests`, `BelyaninReachabilityStepVisualizationTests`) act as the byte-identity regression: they must pass unchanged.

**Docs:** update `docs/developer/rpq-graph-viz.md` (renderers are now RPQ-dedicated and independent of GSS); new `docs/developer/rpq-graph-dot.md` and `docs/developer/rpq-graph-tikz.md` (new modules); update `docs/developer/FLPQ.Printers.md` and `docs/project/architecture.md`.

**Spec:**

- The new renderers reuse `AutomatonTikz` and `DerivationTreeDot` primitives only; they must not reference `GssDot`/`GssTikz`.
- Output must be byte-identical to today's `RpqGraphViz` output (empty-label edge handling, self-loop `loop above`, reciprocal bends, escape rules included).
- Keep all highlight parameters so Belyanin/Arroyuelo callers are untouched.

---

### S3: Dedicated vertical regexp tree renderer + Arroyuelo simple-path restructure

**Code:**

- New `src/FLPQ.Printers/RegexpTreeDot.fs` — `toDot (label: int -> string) (nodes: Set<int>) (edges: Set<int*int>) (current: int) : string`: `digraph { rankdir=TB; ... }`, rectangle nodes, label via `DerivationTreeDot.escapeLabel`, current node `fillcolor=lightblue`.
- New `src/FLPQ.Printers/RegexpTreeTikz.fs` — `toTikz (label: int -> string) (nodes) (edges) (current) : string`: `layered layout, nodes={draw, rectangle}, grow'=down`, labels already TeX (`$...$`, no escaping), current node `fill=lightblue!20`.
- `src/FLPQ.Printers/AutomatonTikz.fs` — add `treeGrowDirection = "grow'=down"` (and `treeRankDirection = "TB"` if kept there).
- New `src/FLPQ.Printers/ArroyueloStepCommon.fs` — the `ArroyueloVisualizationStep` record, `treeNodeSet`/`treeEdgeSet`/`treeVertexLabel`/`treeVertexLabelTikz`, tree rendering via `RegexpTree*`, and a shared `renderSteps` parameterized by `buildMatrices`/`buildGraph` (mirrors `BelyaninStepCommon.renderVisualizationSteps`). `renderSteps` builds the tree from the **full** trace, filters out `Base` steps, and emits one record per remaining step.
- Rename `src/FLPQ.Printers/ArroyueloStepVisualizer.fs` → `ArroyueloSimplePathStepVisualizer.fs`; module `ArroyueloSimplePathStepVisualizer`; its `renderSteps` supplies path matrices (`PathSemiringTeX.matrixWithVertexLabels`) and the path-highlight graph (`RpqGraphViz.renderGraph` with `pathHighlights`).
- `src/FLPQ.Printers/FLPQ.Printers.fsproj` — order: `RegexpTreeDot/Tikz` before `ArroyueloStepCommon`, then `ArroyueloSimplePathStepVisualizer`.
- `src/FLPQ.Cli/Helpers.fs` — record type now `ArroyueloStepCommon.ArroyueloVisualizationStep`.

**Tests:** rewrite `tests/FLPQ.Printers.Tests/ArroyueloStepVisualizationTests.fs` for the new step set (intermediate only) and vertical tree; regenerate `arroyuelo_step_*` goldens. Add a structural test that the RPQ printer sources contain no reference to `Gss` (locate `src/` from the test base directory). Keep the template-fill and lualatex tests with the existing placeholders.

**Docs:** update `docs/developer/arroyuelo-step-viz.md` (vertical tree, intermediate-only steps, shared `ArroyueloStepCommon`); update `docs/developer/rpq-regexp-viz.md` (tree no longer uses GSS); new `docs/developer/regexp-tree-dot.md`, `docs/developer/regexp-tree-tikz.md`, `docs/developer/arroyuelo-step-common.md`; update `docs/developer/FLPQ.Printers.md` and `docs/project/architecture.md`.

**Spec:**

- The tree figure shows every AST node (leaves included); the current node is highlighted; layout vertical.
- `renderSteps` receives the full trace and outputs only non-`Base` steps; `NodeIndex`/`Children` remain the full-trace post-order indices.
- A single-leaf query yields an empty visualization-step list (runner still writes root artifacts and `result.tex`).

---

### S4: Arroyuelo Boolean reachability trace + visualizer

**Code:**

- `src/FLPQ.RPQ/ArroyueloRPQ.fs`:
  - `[<Struct>] type ArroyueloBooleanTraceStep<'t,'nt> = { NodeIndex: int; Expr: Regexp<'t,'nt>; Operation: ArroyueloOperation; Operands: Matrix<bool> list; Result: Matrix<bool>; Children: int list }`.
  - Refactor the recursive Boolean evaluation so `evalExpression` and the new tracing entry point share one core (one source of truth for ε/I, term adjacency, ∨, ×, TC).
  - `evaluateBooleanWithTrace : NFA<'t,int> -> Regexp<'t,'nt> -> ArroyueloBooleanTraceStep<'t,'nt> list * Matrix<bool>` — full `|V|×|V|` Boolean matrix, post-order steps.
  - `evaluate` keeps its public signature (`|sources|×|V|`) and delegates to the shared core.
- New `src/FLPQ.Printers/ArroyueloReachabilityStepVisualizer.fs` — module `ArroyueloReachabilityStepVisualizer`; uses `ArroyueloStepCommon` for the tree; Boolean matrix equation via `PathSemiringTeX.boolMatrixToTeXWith BelyaninStepCommon.vertexLabel BelyaninStepCommon.vertexLabel` (row/col both `v_i`); graph built with `RpqGraphViz.renderGraphWithDerived`.
- `src/FLPQ.Printers/RpqGraphViz.fs` — add `renderGraphWithDerived (terminalPrinter) (graph) (derivedTikzLabel: int*int -> string option) (derivedDotLabel: int*int -> string option) (startVertices) : string * string`. Merges original and derived edge sets: original label + `", "` + derived label; edges present in the derived set render red bold; TikZ uses `skipEscaping=true` with pre-escaped original labels and `$<TeX>$` derived labels; DOT uses the plain derived label.
- `src/FLPQ.Printers/FLPQ.Printers.fsproj` — add `ArroyueloReachabilityStepVisualizer.fs` after `ArroyueloSimplePathStepVisualizer.fs`.

**Tests:** new `tests/FLPQ.Printers.Tests/ArroyueloReachabilityStepVisualizationTests.fs` (Boolean matrices, derived-edge count/labels/merge, no path edges, compile); add a `[<Property>]` equivalence test that the reachability trace's final matrix projected to the sources equals `ArroyueloRPQ.evaluate`; add `ArroyueloBoolean` trace tests in `tests/FLPQ.RPQ.Tests`.

**Docs:** new `docs/developer/arroyuelo-reachability-step-viz.md`; update `docs/developer/arroyuelo-rpq.md` (Boolean trace type/functions); update `docs/developer/rpq-graph-viz.md` (derived-edge rendering); update `docs/developer/FLPQ.RPQ.md`, `docs/developer/FLPQ.Printers.md`, `docs/project/architecture.md`.

**Spec:**

- Derived edge label: the handled node's subexpression (`RegexpTeX.toTeX` for TikZ, `Regexp.toString` for DOT).
- One derived edge per true cell `(u,v)` of `step.Result`; self-loops included.
- Collision with an input edge merges labels into one edge (derived styling wins, red bold).
- No path-edge (`red!40`) or result-vertex (`yellow!20`) highlights; start vertices stay green.
- Boolean semantics is genuine reachability (TC), not a projection of the simple-path trace.

---

### S5: CLI semantics, step templates, and summary integration

**Code:**

- `src/FLPQ.Cli/AlgorithmTypes.fs` — extend the `Semantics` usage text to cover Arroyuelo.
- `src/FLPQ.Cli/Program.fs` — for `ArroyueloRPQ`, read `Semantics` with default `Reachability` and dispatch to `ArroyueloReachabilityRunner` / `ArroyueloSimplePathRunner`.
- Rename `src/FLPQ.Cli/ArroyueloRunner.fs` → `ArroyueloSimplePathRunner.fs` (module `ArroyueloSimplePathRunner`); new `src/FLPQ.Cli/ArroyueloReachabilityRunner.fs` (Boolean `result.tex` + per-source reachable lines, modeled on `BelyaninReachabilityRunner`); update `FLPQ.Cli.fsproj`.
- `data/Arroyuelo_step_template.tex` and `data/Arroyuelo_step_tikz_template.tex` — two columns: left minipage = tree figure; right minipage = matrices line (`\begingroup\setlength{\textwidth}{\linewidth}`) then graph figure; per-figure top alignment.
- `src/FLPQ.Printers/SummaryTeX.fs` — `arroyueloStepSection`: wrap tree/graph with `wrapTikzAdjustboxColumnTop`, drop `wrapStepAdjustbox`; add a reachability-aware `arroyueloColorLegend` (or one shared legend covering both semantics).

**Tests:** rename/extend `tests/FLPQ.Cli.Tests/ArroyueloRunnerTests.fs` (simplePath) and add `ArroyueloReachabilityRunnerTests.fs`; add CLI dispatch tests for the default and `simplePath`; update `SummaryTests`/`CliSummaryTests` and the lualatex step-template compile test for the new layout.

**Docs:** update `docs/user/cli.md` (Arroyuelo `--semantics`, output file list/layout), `docs/developer/summary-tex.md` (Arroyuelo step section/legend), `docs/developer/FLPQ.Cli.md` (runners).

**Spec:**

- Default `-a ArroyueloRPQ` behavior becomes `reachability`; `--semantics simplePath` restores the path rendering.
- Both semantics share the same step directory/file names so the summary pipeline is reused.
- The layouts' `valign=T` and per-component adjustboxes follow the Belyanin pattern; the whole-step `0.9\textheight` box is removed so tall steps can break pages.

---

### S6: Cross-cutting documentation and navigation

**Code:** none (documentation-only subtask).

**Tests:** none (documentation-only).

**Docs:**

- `docs/developer/FLPQ.Printers.md` and `docs/developer/FLPQ.RPQ.md` and `docs/developer/FLPQ.Cli.md` — module/runner lists.
- `docs/project/architecture.md` — add all new files.
- `docs/main.md` — add the new doc pages under the appropriate section.
- `docs/developer/rpq-graph-viz.md`, `docs/developer/rpq-regexp-viz.md` — cross-reference the new renderers.
- Verify every `.md` touched in S1–S6 is mdformat-clean.

**Spec:**

- No duplicate ownership: each new module has exactly one doc; hubs and `main.md` link to it.
- No dangling references to the old `ArroyueloStepVisualizer` module or GSS-based RPQ rendering.

---

## Verification

After S1–S6 are committed: run code review to zero findings, then the hard gate
(`nohup python3 tools/hard_gate.py ...`) until `STATUS: PASS`, merge to `dev`, and mark
291 `[done]` in `tasks/tasks.md`.
