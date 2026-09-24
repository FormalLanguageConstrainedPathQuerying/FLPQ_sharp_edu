# BelyaninStepVisualizer Module

**Tags:** visualization, rpq, belyanin, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** BelyaninStepVisualizer
**Source:** `src/FLPQ.Printers/BelyaninStepVisualizer.fs`
**Depends on:** BelyaninRPQ (FLPQ.RPQ), PathSemiring, PathSemiringTeX, RpqGraphViz, AutomatonDot, AutomatonTikz
**Used by:** FLPQ.Cli (Belyanin runner, task 281 S4)
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Renders each step of Belyanin's path-semiring evaluation (one main-loop iteration of the BFS-like traversal, plus an initialization step) into three artifacts: the query DFA with the current frontier states highlighted, the frontier/accumulated matrices with the per-label propagation products, and the input graph with the current frontier paths highlighted. Both DOT and TikZ are produced per step; the runner writes only one format (TikZ by default, DOT with `--use-dot`).

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
val renderSteps: (string -> string) -> DFA<string, int> -> NFA<string, int> -> BelyaninTraceStep<string> list -> BelyaninVisualizationStep list
```

`renderSteps terminalPrinter dfa graph steps` renders every trace step. The terminal printer labels the graph's edges.

## Step Artifacts

- **Automaton** — the query DFA with the current frontier states highlighted (lightblue): q with a non-empty M[q, \*] cell in the step. Rendered with `AutomatonDot.dfaToDotWithHighlights` / `AutomatonTikz.dfaToTikzWithHighlights` (state labels `q_i`, TikZ `$q_i$`, circle shape).
- **Matrices** — a vertical stack of path semiring matrices (`PathSemiringTeX.matrixWithVertexLabels`): the initialization step shows M and P; an iteration shows M (frontier after the mask), P (accumulated), then per label the two propagation products `(N^a)^T ⊗ M = <Select>` and `× G^a = <Extend>` (only labels with a non-empty Select — see [Belyanin RPQ](belyanin-rpq.md) design decisions), and finally `New M`.
- **Graph** — all graph vertices/edges active, edge labels = terminal names; highlighted vertices/edges (yellow/red) = the union over all paths in the step's frontier M, derived via `RpqGraphViz.pathHighlights`. Step k's highlight set depends only on step k's M — no cross-step leakage.

## Step Templates

`data/Belyanin_step_template.tex` (DOT: `__STEP_AUTOMATON_PDF__`, `__STEP_GRAPH_PDF__`) and
`data/Belyanin_step_tikz_template.tex` (TikZ: `__STEP_AUTOMATON_TIKZ__`, `__STEP_GRAPH_TIKZ__`),
both with a `__MATRICES__` slot. Two-column minipage layout modeled on
`data/RNGLR_step_template.tex`: left = automaton figure + matrices, right = graph. The
matrices sit in a group where `\textwidth` is shortened to the minipage's `\linewidth`, so
the adjustbox-wrapped matrices shrink to the column instead of the page width.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Automaton highlights = frontier states (non-empty M[q, \*]) | The step's partial result in the automaton is exactly which states the current frontier paths sit in; reuses the S5 highlight variants of the DFA renderers |
| Graph highlights derived from the step's M only | Each step figure must show what that step processes; union over `RpqGraphViz.pathHighlights` gives exactly the vertices/edges of the stored simple frontier paths |
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
