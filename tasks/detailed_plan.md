# Detailed Plan: Task 287 — Improve Belyanin RPQ step rendering

## Task description (verbatim)

```
287. Improve Belyanin RPQ stepe rendering bit more. 1. Use 'valign=T' for ajust boxes in steps. Except first box of each step. 2. for F and V use '$F=[F]$'and '$V=[V]$' notation. 3. not 'New F', just 'F'. For example for both look at step 0 and step 1 from @viz_output/BRPQ/results/belyaninrpq/belyaninrpq_merged.tex 4. Highlight target state in automata similarly to target vertex in grah.
**[USER GUIDANCE]**: valign=T applies to every Belyanin step adjustbox except the start frontier F matrix (the first box); Arroyuelo RPQ is unchanged. Boundary tiles render the title and matrix in one adjustbox/math as '$F = <matrix>$' / '$V = <matrix>$', and the end frontier is titled 'F' (no 'New'). The per-label DFA figure highlights in lightyellow the target states {q : F^a[q,*] != empty} (destinations of the used a-transitions), with start > frontier > target precedence; boundary DFAs are unchanged.
```

## Scope and constraints

Rendering-only task. Touched modules: `BelyaninStepVisualizer`, `SummaryTeX`
(Belyanin figure wrap + legend), `AutomatonTikz` / `AutomatonDot` (new target
tier), templates untouched, docs. `BelyaninRPQ.evaluateWithTrace` and the trace
are untouched.

Arroyuelo RPQ output stays byte-identical: the shared
`wrapTikzAdjustboxColumn` is kept, and the DFA renderer change only adds an
extra parameter that Arroyuelo never uses.

## Target rendering (reference: hand-edited `viz_output/BRPQ/...merged.tex`, steps 0/1)

```
[F = M]      [V = P\M]   [graph]   [automaton]     start line (F box: no valign; rest valign)
[(N^a)^T ⊗ F = [N] ⊗ [F] = [F^a]]   [automaton]    select line (target states yellow)
[F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]]   [graph]   extend line
...
[F = NewM]   [V = P]     [graph]   [automaton]     end line (all valign; frontier titled F)
```

## Reuse

- `PathSemiringTeX.matrixWithStateVertexLabelsBody` (task 286) supplies the raw
  pNiceMatrix for the one-adjustbox boundary tiles.
- `RpqGraphViz.frontierVertices` / the existing `frontierStates` helper compute
  the target sets.
- `SummaryTeX.fill` / `readIfExists` template-fill pattern unchanged.
- `AutomatonTikz.nodeOptions` / `AutomatonDot.stateDeclarations` gain one tier
  rather than a parallel renderer.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| `valign=T` on every step adjustbox except the start F matrix | User guidance; top-aligns the tiles so the boundary and label lines read evenly. The start F is the anchor and stays unrounded |
| Belyanin-only valign | User guidance: Arroyuelo RPQ rendering must not change; a new `wrapTikzAdjustboxColumnTop` is used only by `belyaninStepSection` |
| Boundary tiles combine title + matrix in one adjustbox/math (`$F = ...$`) | User guidance/example; the title is no longer a separate paragraph above the matrix |
| End frontier titled `F` (no `New`) | User guidance/example: the step position conveys start vs end |
| Target tier parameter on the DFA renderers | Reuses one renderer; boundary DFAs pass an empty set; precedence start > frontier > target mirrors the graph vertex tiers |
| Target states = `frontierStates F^a` | Equivalent to the destinations of the used a-transitions and parallel to the graph's `frontierVertices Extend` |

## Subtasks

### S1: F/V = matrix notation, drop "New F", matrix/formula valign

**Status:** done (7963dca)

**Code:** `src/FLPQ.Printers/BelyaninStepVisualizer.fs`

- Add private `matrixTile (valignTop: bool) (title: string) (m)` emitting
  `\begin{adjustbox}{max width=\textwidth[, valign=T]}` containing
  `$\text{<title>} = <matrixWithStateVertexLabelsBody m>$`.
- `renderFrontierStart` → `matrixTile false "\text{F}" step.M`;
  `renderVisitedStart` → `matrixTile true "\text{V}" (P \ M)`;
  `renderFrontierEnd` → `matrixTile true "\text{F}" step.NewM`;
  `renderVisitedEnd` → `matrixTile true "\text{V}" step.P`.
- `wrapFormula` options → `max width=\textwidth, valign=T`.

**Tests:** `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` — rewrite
the F/V block facts (`\text{F} =` / `\text{V} =`, no `\text{New }`, valign
presence per tile); formula structure fact asserts `valign=T`; regenerate the
`frontier_*` / `visited_*` and `label_*_select/extend` goldens.

**Docs:** `docs/developer/belyanin-step-viz.md`.

### S2: Belyanin figure-wrapper valign

**Status:** done (5961cbf)

**Code:** `src/FLPQ.Printers/SummaryTeX.fs` — add
`wrapTikzAdjustboxColumnTop` (`max width=\linewidth, valign=T`); use it for the
four boundary figures and both label figures in `belyaninStepSection`.

**Tests:** `tests/FLPQ.Printers.Tests/SummaryTexSectionTests.fs` — update the
Belyanin adjustbox option-count patterns; template-fill and no-Overfull facts
stay green.

**Docs:** `docs/developer/summary-tex.md`.

### S3: Target states in the per-label DFA

**Status:** done (d38546b)

**Code:**

- `src/FLPQ.Printers/AutomatonTikz.fs`, `src/FLPQ.Printers/AutomatonDot.fs` —
  add `targetStates: Set<int>` to `dfaToTikzWithHighlights` /
  `dfaToDotWithHighlights`; target fill `fill=yellow!20` / `fillcolor=lightyellow`
  with lowest precedence (TikZ: add before start; DOT: highlighted → lightblue,
  start → green, else target → lightyellow); `dfaToTikz`/`dfaToDot` pass an
  extra `Set.empty`.
- `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — `dfaFigures` takes
  `targetStates`; `automatonWithFrontier` passes `Set.empty`; `renderLabel`
  passes `frontierStates ls.Select`.
- `src/FLPQ.Printers/SummaryTeX.fs` — `belyaninColorLegend` yellow row →
  "Target vertices and automaton states (newly reached on the step)".

**Tests:** `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — update
call sites; add target-tier facts (TikZ `yellow!20`, DOT `lightyellow`,
precedence). `BelyaninStepVisualizationTests` — new fact that the per-label DFA
highlights exactly `frontierStates Select` in `lightyellow`; regenerate the
per-label automaton goldens.

**Docs:** `docs/developer/automaton-viz.md`,
`docs/developer/belyanin-step-viz.md`.

### S4: Example verification and documentation sweep

**Status:** done — `viz_output/BRPQ` regenerated: merged PDF compiles with zero Overfull
boxes, 7 pages, all 6 steps; step 0/1 match the reference (no-valign start F, `F =` /
`V =` tiles, `valign=T` elsewhere, DFA target states lightyellow). Docs swept.

**Code:** none (width tuning only if needed).
**Tests:** regenerate `viz_output/BRPQ` and compile; zero Overfull; step 0/1
match the reference. Update all affected docs and `mdformat`.

**Docs:** `belyanin-step-viz.md`, `summary-tex.md`, `path-semiring-tex.md`,
`FLPQ.Cli.md`, `user/cli.md`, `automaton-viz.md`.

## Design Notes

### User guidance (authoritative)

Recorded verbatim in the task-log entry and the "Task description (verbatim)"
section above. valign=T on every Belyanin step adjustbox except the start F;
Belyanin only; `$F = ...$` / `$V = ...$`; end frontier titled `F`; per-label DFA
target states lightyellow with start > frontier > target precedence.
