# BelyaninReachabilityStepVisualizer Module

**Tags:** visualization, rpq, belyanin, reachability, boolean, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** BelyaninReachabilityStepVisualizer
**Source:** `src/FLPQ.Printers/BelyaninReachabilityStepVisualizer.fs`
**Depends on:** BelyaninRPQ (FLPQ.RPQ), BelyaninStepCommon, PathSemiringTeX, RpqGraphViz, AutomatonDot, AutomatonTikz
**Used by:** FLPQ.Cli (`BelyaninReachabilityRunner`, task 288)
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Renders each step of Belyanin's Boolean RPQ evaluation — the reachability
> semantics (`BelyaninRPQ.evaluateReachabilityWithTrace`) — as one line per substep: the
> start-of-step line (frontier F, visited V, the input graph with green sources and
> lightblue current vertices, and the query DFA with the frontier states highlighted), one
> pair of lines per active label with the explicit Boolean products
> `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` and `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` plus the
> used transitions / followed edges red bold and the target states / vertices lightyellow,
> and — for non-init steps — the end-of-step line (F, V, graph, DFA). No path information
> is collected, so no path edges are drawn; both DOT and TikZ are produced per step.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Step Artifacts](#step-artifacts)
- [Colors](#colors)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

This is the reachability (Boolean) counterpart of
[BelyaninSimplePathStepVisualizer](belyanin-step-viz.md). It consumes
`BelyaninRPQ.evaluateReachabilityWithTrace`, whose cells are `bool` (reachable / not
reachable) rather than sets of simple paths. The step layout, artifact file names, record
type ([BelyaninStepCommon](belyanin-step-common.md)), DOT/TikZ figure helpers, and the `-s`
summary templates are identical to the simple-path variant, so the CLI step writer and the
summary section builder are reused unchanged.

Differences from the path-semiring visualizer:

- all matrices are Boolean (`\bullet` / `\cdot`), rendered with
  `PathSemiringTeX.boolMatrixToTeXBody` for `q_i`/`v_j` labels;
- `V` at the start of a step is `P ∧ ¬M` (Boolean intersection with the complement);
- no path edges are ever drawn (no `RpqGraphViz.pathEdges`), because no path data exists;
- the trace covers a single source (the Boolean cells do not identify their origin); the
  runner still reports every source via `BelyaninRPQ.evaluate`.

The used current→target transitions (automaton) and followed current→target edges (graph)
are still highlighted red bold with their symbols, derived from `N^a`, `G^a`, and the
current frontier.

## Function Signatures

```fsharp
val renderSteps:
    ('t -> string) -> DFA<'t, int> -> NFA<'t, int> ->
    BelyaninReachabilityTraceStep<'t> list -> BelyaninVisualizationStep list
```

`renderSteps terminalPrinter dfa graph steps` renders every Boolean reachability trace
step. Highlights (all via the cell-generic helpers in `BelyaninStepCommon`, with
`isZero = not`):

- current automaton states = `frontierStates step.M` (lightblue);
- current graph vertices = `frontierVertices step.M` (columns of `M` with a true cell);
- per-label used transitions = `usedAutoEdges step.M ls.N` (red bold);
- per-label target automaton states = `frontierStates ls.Select` (lightyellow);
- per-label followed edges = `followedEdges ls.G ls.Select` (red bold);
- per-label target graph vertices = `frontierVertices ls.Extend` (lightyellow).

## Step Artifacts

File names match the simple-path visualizer exactly, so `Helpers.writeBelyaninStepsVisualization`
and `SummaryTeX.belyaninStepSection` are reused. The init step has only the start artifacts.

| File | Content |
| --- | --- |
| `frontier_start.tex` | `$F = $` Boolean matrix(M) — no `valign=T` (first box) |
| `visited_start.tex` | `$V = $` Boolean matrix(P ∧ ¬M), `valign=T` |
| `automaton_start.{tikz.tex\|dot}` | DFA, frontier states of M highlighted (no edge highlights) |
| `graph_start.{tikz.tex\|dot}` | green sources, lightblue current vertices of M (no edges) |
| `label_i_select.tex` | `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]`, Boolean, one line |
| `label_i_extend.tex` | `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]`, Boolean, one line |
| `label_i_automaton.{tikz.tex\|dot}` | DFA, current frontier states lightblue, target states lightyellow, used a-transitions red bold |
| `label_i_graph.{tikz.tex\|dot}` | green sources, lightblue from-endpoints, lightyellow targets, followed a-edges red bold — no path edges |
| `frontier_end.tex` | `$F = $` Boolean matrix(NewM) — non-init only |
| `visited_end.tex` | `$V = $` Boolean matrix(P) — non-init only |
| `automaton_end.{tikz.tex\|dot}` | DFA, frontier states of NewM — non-init only |
| `graph_end.{tikz.tex\|dot}` | green sources, lightblue current vertices of NewM — non-init only |

## Colors

The same tier colors as the simple-path visualizer apply (see
[Belyanin step visualization](belyanin-step-viz.md#colors)): green start/source, lightblue
current frontier, lightyellow target, red bold current transition edges. The **Frontier
path edges** tier (`red!40` / `#FF9999`) never appears here because the reachability trace
stores no paths.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Reuse the step record and artifact names | The `-s` summary templates and the CLI writer are file/record driven; identical names make the Boolean variant a drop-in |
| Boolean cells rendered with `boolMatrixToTeXBody` | The reachability trace stores `bool`, so path-tuple rendering does not apply |
| Current and target states/vertices highlighted, plus used/followed edges red bold | User guidance: no paths are drawn, but the step's current→target propagation (with edge symbols) is still visible |
| No `pathEdges` anywhere | The reachability trace collects no path information; the light-red path tier is unreachable by construction |
| Single-source trace | Boolean cells do not identify the origin source; the first start vertex drives the trace, while the answer for all sources comes from `BelyaninRPQ.evaluate` |

## See Also

- [Belyanin step common](belyanin-step-common.md) — shared record types and helpers
- [Belyanin simple-path step visualization](belyanin-step-viz.md) — the path semiring variant
- [Belyanin RPQ module](belyanin-rpq.md) — `evaluateReachabilityWithTrace` / `evaluate`
- [RPQ graph visualization](rpq-graph-viz.md) — shared input-graph rendering
