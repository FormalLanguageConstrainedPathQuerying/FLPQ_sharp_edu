# Detailed Plan: Task 290 — Improve Belyanin RPQ rendering

## Task description (verbatim)

```
290. Improve Belyanin RPQ algorthm rendering. 1. In steps, in case when no transition edges do not miss highlighting of current vertex in the graph. 2. Add 'valign=T' option for first adjustbox of first line in step too (box with F=[F] V=[V]). It leads to better layout. 3. Unify colors priority in graph and automata. For example, in current version if garph vertex is start and current it colored as start (in green), but in the same case automata vertex colored as current (blue). I propose: current dominates start (as in automata), target dominates current.
**[USER GUIDANCE]**: (1) The graph's lightblue current vertices in a per-label figure must be the selected vertices (the non-empty columns of F^a), which always exist for an active label, not only the sources of followed edges; followed sources are a subset. (2) The start frontier F tile also gets `valign=T` (the only step box still lacking it). (3) Unified fill priority: target > current > start in both graph and automaton; in the automaton the final-state fill sits between current and start, i.e. target > current > final > start (the double ring is always kept).
```

## Overview

Three rendering improvements for the Belyanin RPQ step visualizations (simple-path and
reachability). They touch the two Belyanin step visualizers, the shared Belyanin step helpers, the
shared GSS graph renderers, and the DFA renderers.

Root causes found during planning:

1. **Current vertex missed without transition edges** — `BelyaninSimplePathStepVisualizer.renderLabel`
   and `BelyaninReachabilityStepVisualizer.renderLabel` derive the graph's lightblue "current"
   vertices as the sources of the followed (red) edges. When a label's `Select` is non-empty but
   `G^a` has no outgoing edge from a selected vertex, `followed` is empty and the current vertex is
   not highlighted (concrete case: example step 4 label 2, `c`: `Select` has a path at `v3` but
   `G^c` has no edge from `v3`, so `v3` is currently plain). The current vertices must be the
   selected vertices — the non-empty columns of `F^a` (`ls.Select`).
2. **Missing `valign=T`** — `BelyaninStepCommon.matrixTileWithBody false` is used only by
   `renderFrontierStart` (both visualizers); every other tile passes `true`. The start frontier F
   tile must also be top-aligned, after which the boolean parameter is dead.
3. **Color priority divergence** — the shared graph renderers
   (`GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`) order fills
   `start > current > frontier > storedPop > highlighted`, so an RPQ start vertex that is also a
   frontier/current vertex stays green, while the automaton renders the same overlap blue. The
   desired unified order is **target > current > start** in both, with the automaton final fill at
   **target > current > final > start**. RPQ passes target as `highlightedVertices`, current as
   `frontierVertices`, start as `startVertices`; GLL/RNGLR/Arroyuelo pass `startVertices = ∅` and
   `frontierVertices = ∅`, so a reorder to
   `currentVertex > storedPop > highlighted > frontier > start` is byte-preserving for them and
   yields exactly `target > current > start` for RPQ.

## Reuse analysis

- Q1/Q3: reuse `RpqGraphViz.frontierVertices` (path string) and the reachability private
  `frontierVertices` (bool) to obtain the non-empty `F^a` columns — no new helper.
- Q1/Q3: `BelyaninStepCommon.colNonEmpty` / `rowNonEmpty` remain the single source of truth for the
  cell-emptiness notion; no change.
- Q4: generalize `BelyaninStepCommon.matrixTileWithBody` by dropping the now-always-true
  `valignTop` parameter.
- Q3: the precedence is data-driven through the existing `startVertices` / `frontierVertices` /
  `highlightedVertices` / `storedPopVertices` / `currentVertex` parameters; only the branch order
  changes. No new parameter.
- Q3: `AutomatonTikz.nodeOptions` and `AutomatonDot.stateDeclarations` keep their existing fill
  attributes (`green!30`, `red!30`, `lightblue!20`, `yellow!20`; `green`, `lightyellow`), only the
  order changes.

## Subtasks

### S1: Graph current vertices from the selected `F^a` columns

**Code:** `src/FLPQ.Printers/BelyaninSimplePathStepVisualizer.fs` (`renderLabel`),
`src/FLPQ.Printers/BelyaninReachabilityStepVisualizer.fs` (`renderLabel`).
**Tests:** `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` (the `fromEndpoints`
computation in `per-label graph DOT highlights followed edges red bold and tiers the endpoint vertices` and the corresponding TikZ fact),
`tests/FLPQ.Printers.Tests/BelyaninReachabilityStepVisualizationTests.fs` (same). Regenerate the
affected `belyanin_step_*_label_*_graph.tikz` and `belyanin_reach_step_*_label_*_graph.tikz`
goldens.
**Docs:** `docs/developer/belyanin-step-viz.md`, `docs/developer/belyanin-reachability-step-viz.md`.

**Spec:**

- In `BelyaninSimplePathStepVisualizer.renderLabel`, replace
  `let fromEndpoints = followed |> Set.fold ...` with
  `let currentVertices = RpqGraphViz.frontierVertices ls.Select`.
  Pass `currentVertices` in the `frontierVertices` slot of `RpqGraphViz.renderGraph`.
- In `BelyaninReachabilityStepVisualizer.renderLabel`, replace `fromEndpoints` with
  `let currentVertices = frontierVertices ls.Select` (the existing private boolean helper).
  Keep `targets = frontierVertices ls.Extend`.
- Rename the local to `currentVertices` in both (clearer; the followed edges stay `followed`).
- Rationale: `followedEdges` already requires `colNonEmpty select u`, so followed sources are a
  subset of the selected vertices; using the selected vertices never removes a highlight and adds
  the missing current vertex when no edge is followed.

### S2: Top-align the start frontier F tile

**Code:** `src/FLPQ.Printers/BelyaninStepCommon.fs` (`matrixTileWithBody`),
`src/FLPQ.Printers/BelyaninSimplePathStepVisualizer.fs` (`renderFrontierStart` and the three other
tiles), `src/FLPQ.Printers/BelyaninReachabilityStepVisualizer.fs` (same).
**Tests:** `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` (`frontier start has exactly the F block and no valign`), `tests/FLPQ.Printers.Tests/BelyaninReachabilityStepVisualizationTests.fs`
(`visited start block is P minus M and frontier start has no valign`). Regenerate all
`belyanin_step_*_frontier_start.tex` and `belyanin_reach_step_*_frontier_start.tex` goldens.
**Docs:** `docs/developer/belyanin-step-viz.md` (Step Artifacts table and the `valign=T` design
decision), `docs/developer/belyanin-reachability-step-viz.md` (Step Artifacts table).

**Spec:**

- Change both `renderFrontierStart` calls to `matrixTileWithBody true ...` and simplify
  `matrixTileWithBody` to a two-argument function that always emits
  `max width=\textwidth, valign=T`; update the four call sites per visualizer and the doc comment.
- Update the two tests to assert `Assert.Contains("valign=T", frontier)` instead of
  `DoesNotContain`, and rename the facts (`... and frontier start is top-aligned`).
- Update the docs: the start frontier F tile is now top-aligned like every other step box.

### S3: Unified target > current > start color priority

**Code:** `src/FLPQ.Printers/GssDot.fs` (`renderVertex`), `src/FLPQ.Printers/GssTikz.fs` (vertex
fill block), `src/FLPQ.Printers/AutomatonDot.fs` (`stateDeclarations`),
`src/FLPQ.Printers/AutomatonTikz.fs` (`nodeOptions`).
**Tests:** `tests/FLPQ.Printers.Tests/GssDotTests.fs` (the DOT and TikZ precedence facts),
`tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs`
(`dfaToDotWithHighlights precedence: ...`, `dfaToTikzWithHighlights precedence: ...`),
`tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` (the `lightblue`/`lightyellow` tier
computations), `tests/FLPQ.Printers.Tests/BelyaninReachabilityStepVisualizationTests.fs` (same).
Regenerate the affected `*_graph_*` and `*_automaton*.tikz` goldens. Verify GLL/RNGLR/Arroyuelo
tests and goldens are unchanged.
**Docs:** `docs/developer/gss-dot.md`, `docs/developer/gss-tikz.md`,
`docs/developer/automaton-viz.md`, `docs/developer/belyanin-step-viz.md` (Colors + design
decision), `docs/developer/belyanin-reachability-step-viz.md` (Colors),
`docs/developer/rpq-graph-viz.md`.

**Spec:**

- `GssDot` / `GssTikz` vertex fill precedence (highest first):
  `currentVertex (lightblue) > storedPop (orange) > highlighted (yellow/lightyellow) > frontierVertices (lightblue) > startVertices (green)`.
  RPQ (no currentVertex/storedPop) therefore renders `target > current > start`; GLL/RNGLR
  (`start = ∅`, `frontier = ∅`) keep their effective `current > storedPop > highlighted`.
- `AutomatonDot.stateDeclarations` first-match order:
  `if target then lightyellow elif highlighted then lightblue elif start then green` with
  `peripheries=2` appended whenever the state is final.
- `AutomatonTikz.nodeOptions` last-wins fill order: `start` fill, `final` fill, `highlighted`
  (current) fill, `target` fill — i.e. `target > current > final > start`. Keep the
  `label=above:Start`, `double`, and `double distance=1.5pt` attributes unchanged.

### S4: Regenerate Belyanin goldens and run the Printers test project

**Code:** none (golden data only).
**Tests:** `tests/FLPQ.Printers.Tests/GoldenData/` (`belyanin_*` simple-path and reachability
Belyanin goldens).
**Docs:** none.

**Spec:**

- Delete the affected committed goldens and their copies under
  `tests/FLPQ.Printers.Tests/bin/Debug/net10.0/GoldenData/`, run the test project to regenerate the
  output goldens, copy the regenerated files back into `tests/FLPQ.Printers.Tests/GoldenData/`, then
  re-run the test project and confirm zero failures.
- Confirm `GssDotTests`, `GssDotVisualizationTests`, `AutomatonVisualizationTests`, the Belyanin
  visualization tests, `RpqGraphVizTests`, and `SummaryTexSectionTests` all pass, and that no
  GLL/RNGLR/Arroyuelo golden changed.

### S5: Documentation pass and full-repo code review

**Code:** none expected; fix any finding from the `code-review` skill.
**Tests:** re-run the affected test projects after any fix.
**Docs:** finish the documentation updates listed in S1–S3 and keep `docs/developer/FLPQ.Printers.md`
and related hubs consistent if they enumerate the changed behavior.

**Spec:**

- Load the `code-review` skill and iterate the full-repo review (architecture, duplication,
  signature consistency, naming, test gaps) to zero findings.
- Ensure every `.md` touched is `mdformat`-clean.

## Verification

- Per subtask: `dotnet fantomas .`, `python3 tools/quality_check.py`, affected `dotnet test`, and
  `mdformat` on every touched `.md`.
- Final: `python3 tools/hard_gate.py` (async) until `STATUS: PASS`, then merge to `dev` and mark
  task 290 `[done]`.

## Status

- S1 — done (49c782a): current graph vertices = selected `F^a` columns in both visualizers.
- S2 — done (e152bc9): start frontier F tile top-aligned; `matrixTileWithBody` param dropped.
- S3 — done (d8cfd71): unified fill priority target > current > start (graph and automaton),
  automaton target > current > final > start.
- S4 — done: full `FLPQ.Printers.Tests` project green (433 passed, 0 skipped).
- S5 — done (961b2ef): code review fix — final-target precedence coverage and Belyanin color
  doc blockquote; report recorded in `tasks/code_review.md`.
- Hard gate and merge pending.
