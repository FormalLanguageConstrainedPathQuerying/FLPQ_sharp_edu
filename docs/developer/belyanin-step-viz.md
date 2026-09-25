# BelyaninStepVisualizer Module

**Tags:** visualization, rpq, belyanin, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** BelyaninStepVisualizer
**Source:** `src/FLPQ.Printers/BelyaninStepVisualizer.fs`
**Depends on:** BelyaninRPQ (FLPQ.RPQ), PathSemiring, PathSemiringTeX, RpqGraphViz, AutomatonDot, AutomatonTikz
**Used by:** FLPQ.Cli (Belyanin runner, task 281 S4)
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Renders each step of Belyanin's path-semiring evaluation (one main-loop iteration of the BFS-like traversal, plus an initialization step) into three artifacts: the query DFA with the current frontier states highlighted, the frontier/accumulated matrices with the per-label propagation products, and the input graph with the step's frontier paths in light red, the edges traversed on the step in red bold, and their endpoints highlighted. Both DOT and TikZ are produced per step; the runner writes only one format (TikZ by default, DOT with `--use-dot`).

## Contents

- [Data Structure](#data-structure)
- [Module Functions](#module-functions)
- [Step Artifacts](#step-artifacts)
- [Step Templates](#step-templates)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

```fsharp
type BelyaninVisualizationStep =
    { AutomatonDot: string; AutomatonTikz: string; Matrices: string; GraphDot: string; GraphTikz: string }
```

One record per trace step (`BelyaninRPQ.BelyaninTraceStep`).

## Module Functions

```fsharp
val renderSteps: ('t -> string) -> DFA<'t, int> -> NFA<'t, int> -> BelyaninTraceStep<'t> list -> BelyaninVisualizationStep list
```

`renderSteps terminalPrinter dfa graph steps` renders every trace step. The terminal printer
labels the graph's edges, the query DFA transitions, and the per-label propagation blocks.

## Step Artifacts

- **Automaton** — the query DFA with the current frontier states highlighted (lightblue): q with a non-empty M[q, \*] cell in the step. Rendered with `AutomatonDot.dfaToDotWithHighlights` / `AutomatonTikz.dfaToTikzWithHighlights` (state labels `q_i`, TikZ `$q_i$`, circle shape).
- **Matrices** — a vertical stack of matrices: the initialization step shows F (frontier) and V (visited); an iteration shows F (frontier after the mask), V (accumulated), then per label the explicit propagation chain `(N^a)^T ⊗ F = <N^a^T> ⊗ <F> = <Select>` and `× G^a = <G^a> = <Extend>` — every matrix written out, with N^a^T and G^a as boolean matrices (`\bullet`/`\cdot` cells) — (only labels with a non-empty Select — see [Belyanin RPQ](belyanin-rpq.md) design decisions), and finally `New F`. Path matrices use q_i state row labels and v_i vertex column labels (`PathSemiringTeX.matrixWithStateVertexLabels`); the boolean blocks use q/q headers (N^a^T) and v/v headers (G^a).
- **Graph** — all graph vertices/edges active, edge labels = terminal names; three highlight tiers: path edges (light red) = all edges of the paths in the step's frontier M (`RpqGraphViz.pathEdges`), current edges (red, bold) = the last edge of every path in the step's NewM (`RpqGraphViz.pathLastEdges`) — the edges traversed on this step, and highlighted vertices (yellow) = the endpoints of the current edges (`RpqGraphViz.edgeEndpoints`). The initialization step has no highlights. Step k's highlight sets depend only on step k's M and NewM — no cross-step leakage.

## Step Templates

`data/Belyanin_step_template.tex` (DOT: `__STEP_AUTOMATON_PDF__`, `__STEP_GRAPH_PDF__`) and
`data/Belyanin_step_tikz_template.tex` (TikZ: `__STEP_AUTOMATON_TIKZ__`, `__STEP_GRAPH_TIKZ__`),
both with a `__MATRICES__` slot. Two-column minipage layout modeled on
`data/RNGLR_step_template.tex`: left = automaton figure + matrices, right = graph. The
matrices sit in a group where `\textwidth` is shortened to the minipage's `\linewidth`, so
the adjustbox-wrapped matrices shrink to the column instead of the page width. The DOT
template's figures use `\includegraphics[width=\linewidth]` so they fit their minipage
column (a minipage does not change `\textwidth`). At summary time the section builder
wraps the TikZ figures in `SummaryTeX.wrapTikzAdjustboxColumn` and the whole filled
template in `SummaryTeX.wrapStepAdjustbox` (at most `\textwidth` and `0.9\textheight`) so
every step fits one page — see [SummaryTeX module](summary-tex.md).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Automaton highlights = frontier states (non-empty M[q, \*]) | The step's partial result in the automaton is exactly which states the current frontier paths sit in; reuses the S5 highlight variants of the DFA renderers |
| Current edges = `pathLastEdges NewM`, path edges = `pathEdges M` | The edge traversed on a step is the last edge of the extended (NewM) paths; highlighting all of M's edges instead lags by one step (task 284, S5). Vertex highlights are restricted to the current edges' endpoints so the figure shows where the step acts, not the whole accumulated path |
| Per-label blocks only for non-empty Select | All-empty products add noise; the book's I_simple drop is visible as a non-empty Select with an empty Extend (example: iteration 4, label b) |
| Both DOT and TikZ produced per step, runner picks one | Matches the existing runner convention (TikZ default, DOT via `--use-dot`) without rendering twice |

## Book Reference

Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring` — each main-loop iteration
propagates the frontier through `(N^a)^T ⊗ M ⊗ G^a` per label; the book example figure
`figures/02_BFS/algorithm_step.tex` shows paths in matrix cells.

## See Also

- [Belyanin RPQ module](belyanin-rpq.md) — `evaluateWithTrace` produces the trace steps
- [RPQ graph visualization](rpq-graph-viz.md) — shared input-graph rendering with path highlights
- [PathSemiringTeX module](path-semiring-tex.md) — matrix rendering
- [FLPQ.Printers hub](FLPQ.Printers.md)
