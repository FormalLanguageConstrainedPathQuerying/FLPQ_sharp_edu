# ArroyueloStepVisualizer Module

**Tags:** visualization, rpq, arroyuelo, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** ArroyueloStepVisualizer
**Source:** `src/FLPQ.Printers/ArroyueloStepVisualizer.fs`
**Depends on:** ArroyueloRPQ (FLPQ.RPQ), PathSemiring, GssDot, GssTikz, RegexpTeX, PathSemiringTeX, AutomatonTikz
**Used by:** FLPQ.Cli (Arroyuelo runner, task 280 S7)
**Book reference:** Chapter 11, Section sec:RPQ_Arroyuelo

> **Abstract:** Renders each step of Arroyuelo's path-semiring evaluation (one post-order visit of a regexp tree node) into three artifacts: the regexp tree with the current node highlighted, the matrix equation for the step's operation, and the graph with the vertices/edges of the step's result paths highlighted. Both DOT and TikZ are produced per step; the runner writes only one format (TikZ by default, DOT with `--use-dot`).

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
type ArroyueloVisualizationStep =
    { TreeDot: string; TreeTikz: string; Matrices: string; GraphDot: string; GraphTikz: string }
```

One record per trace step (`ArroyueloRPQ.ArroyueloTraceStep`).

## Module Functions

```fsharp
val renderSteps: (string -> string) -> NFA<string, int> -> ArroyueloTraceStep<string, string> list -> ArroyueloVisualizationStep list
```

`renderSteps terminalPrinter graph steps` renders every trace step. The terminal printer labels the graph's edges.

## Step Artifacts

- **Tree** — nodes = AST node indices (the trace steps are in post-order, so the list index is the NodeIndex), edges = parent→child, node label = the subexpression via `RegexpTeX.toTeX`, current node = the step's NodeIndex (lightblue). Rendered with `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` (shape `rectangle`, `skipEscaping = true` — see Design Decisions).
- **Matrices** — a vertical stack of path semiring matrices (`PathSemiringTeX.matrixToTeX`) per operation: Base shows the node's formula above the result matrix; Alt shows `M(left) ⊕ M(right) = M(node)`; Seq shows `M(left) × M(right) = M(node)`; Star shows `TC(M(child)) = M(node)`.
- **Graph** — all graph vertices/edges active, edge labels = terminal names; highlighted vertices/edges (yellow/red) = the union over all paths in the step's Result matrix, derived via `PathSemiring.allPaths` (consecutive vertex pairs of each path are edges). Step k's highlight set depends only on step k's Result — no cross-step leakage.

## Step Templates

`data/Arroyuelo_step_template.tex` (DOT: `__STEP_TREE_PDF__`, `__STEP_GRAPH_PDF__`) and `data/Arroyuelo_step_tikz_template.tex` (TikZ: `__STEP_TREE_TIKZ__`, `__STEP_GRAPH_TIKZ__`), both with a `__MATRICES__` slot. Two-column minipage layout modeled on `data/RNGLR_step_template.tex`: left = tree figure + matrices, right = graph. The matrices sit in a group where `\textwidth` is shortened to the minipage's `\linewidth`, so the adjustbox-wrapped matrices shrink to the column instead of the page width. The DOT template's figures use `\includegraphics[width=\linewidth]` so they fit their minipage column (a minipage does not change `\textwidth`). At summary time the section builder wraps the TikZ figures in `SummaryTeX.wrapTikzAdjustboxColumn` and the whole filled template in `SummaryTeX.wrapStepAdjustbox` (at most `\textwidth` and `0.9\textheight`) so every step fits one page — see [SummaryTeX module](summary-tex.md).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Tree and graph reuse the GSS renderers (`GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`) | One implementation of "layered graph with highlighted/current vertex" for all step figures; no dedicated regexp-tree module (see [RPQ regexp visualization](rpq-regexp-viz.md)) |
| `GssTikz.toTikzFromSets` now skips escaping vertex labels when `skipEscaping = true` | Tree node labels are already-TeX math-mode formulas (`RegexpTeX.toTeX`) that must not be escaped; the existing callers pass plain-text vertex labels with no LaTeX specials, so their output is byte-identical (golden tests unchanged) |
| TikZ tree node labels wrap the formula in `$...$` | `as={...}` node content is text mode; math-mode formulas need an explicit math wrapper (same convention as `LRAutomatonTikz.stateContentToTikzAs`) |
| Graph highlights derived from the step's Result only | Each step figure must show what that step computed; union over `PathSemiring.allPaths` gives exactly the vertices/edges of the stored simple paths |
| Tree passes `bendReciprocalEdges = false`, graph inherits bending from [RpqGraphViz](rpq-graph-viz.md) | Tree edges are parent→child (one direction only, no reciprocal pairs); the input graph may carry both directions of a pair and needs the TikZ bend (task 282, S2) |
| Both DOT and TikZ produced per step, runner picks one | Matches the existing runner convention (TikZ default, DOT via `--use-dot`) without rendering twice |

## Book Reference

Chapter 11, `03_Arroyuelo.tex`: Section sec:RPQ_Arroyuelo — matrix evaluation of the regexp tree; each node's matrix equation is one step.

## See Also

- [Arroyuelo RPQ module](arroyuelo-rpq.md) — `evaluateWithTrace` produces the trace steps
- [PathSemiringTeX module](path-semiring-tex.md) — matrix rendering
- [RPQ regexp visualization](rpq-regexp-viz.md) — RegexpTeX and the tree-rendering reuse decision
- [FLPQ.Printers hub](FLPQ.Printers.md)
