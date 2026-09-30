# BelyaninStepVisualizer Module

**Tags:** visualization, rpq, belyanin, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** BelyaninStepVisualizer
**Source:** `src/FLPQ.Printers/BelyaninStepVisualizer.fs`
**Depends on:** BelyaninRPQ (FLPQ.RPQ), PathSemiring, PathSemiringTeX, RpqGraphViz, AutomatonDot, AutomatonTikz
**Used by:** FLPQ.Cli (Belyanin runner, task 281 S4)
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Renders each step of Belyanin's path-semiring evaluation (one main-loop
> iteration of the BFS-like traversal, plus an initialization step) into a three-line
> layout: the start-of-step state (frontier F and visited V matrices, the query DFA with
> the frontier states highlighted, the input graph with green sources, lightblue frontier
> vertices, and lightred frontier path edges), one row per active label with the two
> explicit propagation products `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` and
> `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` plus the used transitions / followed edges
> highlighted red bold, and — for non-init steps — the end-of-step state (New F, V). Both
> DOT and TikZ are produced per step; the runner writes only one format (TikZ by default,
> DOT with `--use-dot`).

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
type BelyaninLabelVisual =
    { SelectFormula: string; ExtendFormula: string; AutomatonDot: string; AutomatonTikz: string; GraphDot: string; GraphTikz: string }

type BelyaninBoundaryVisual =
    { Matrices: string; AutomatonDot: string; AutomatonTikz: string; GraphDot: string; GraphTikz: string }

type BelyaninVisualizationStep =
    { Start: BelyaninBoundaryVisual; Labels: BelyaninLabelVisual list; End: BelyaninBoundaryVisual option }
```

One record per trace step (`BelyaninRPQ.BelyaninTraceStep`), one `BelyaninLabelVisual`
per active label in trace order. A boundary is the F/V matrices (F/V at the start, New
F/V at the end) plus the automaton/graph figures with that boundary's frontier
highlighted. The init step has no end state (`End = None`) and no label rows — the
absence is unrepresentable by accident, so the writer and the tests match on `End`
instead of checking for empty strings.

## Module Functions

```fsharp
val renderSteps: ('t -> string) -> DFA<'t, int> -> NFA<'t, int> -> BelyaninTraceStep<'t> list -> BelyaninVisualizationStep list
```

`renderSteps terminalPrinter dfa graph steps` renders every trace step. The terminal
printer labels the graph's edges, the query DFA transitions, and the per-label formulas.
Every highlight set is derived from the trace only (no cross-step data):

- `frontierStates m` — automaton states q with a non-empty M[q, \*] cell.
- `RpqGraphViz.frontierVertices m` — graph vertices at which m's paths end.
- `usedAutoEdges m n` — DFA transitions q → q′ with `n.[q, q']` and a frontier path at q.
- `followedEdges g select` — graph edges u → v with `g.[u, v]` and a selected path at u.

## Step Artifacts

The runner (`Helpers.writeBelyaninStepsVisualization`) writes one directory per trace
step; the init step has only the start trio:

| File | Content |
| --- | --- |
| `matrices_start.tex` | `$F$` + matrix(M), then `$V$` + matrix(P \\ M) — visited before the step |
| `automaton_start.{tikz.tex\|dot}` | DFA, frontier states of M highlighted (no edge highlights) |
| `graph_start.{tikz.tex\|dot}` | green sources, lightblue frontier vertices of M, lightred path edges of M |
| `label_i_select.tex` | `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` — every matrix written out |
| `label_i_extend.tex` | `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` — every matrix written out |
| `label_i_automaton.{tikz.tex\|dot}` | DFA, the label's used a-transitions red bold (no state highlights) |
| `label_i_graph.{tikz.tex\|dot}` | green sources, red followed a-edges, lightblue from-endpoints, lightyellow targets, lightred path edges of M |
| `matrices_end.tex` | `$New F$` + matrix(NewM), then `$V$` + matrix(P) — non-init steps only |
| `automaton_end.{tikz.tex\|dot}` | DFA, frontier states of NewM highlighted — non-init steps only |
| `graph_end.{tikz.tex\|dot}` | green sources, lightblue frontier vertices of NewM, lightred path edges of NewM — non-init steps only |

Label index `i` = position in the trace's `Labels` list (Map iteration order over the
boolean decomposition). The per-label graph figure's highlight sets: followed edges =
`followedEdges G^a F^a`, from-endpoints = their sources, targets =
`RpqGraphViz.frontierVertices Extend`. A label whose Select is non-empty but whose
Extend is entirely empty (the book's I_simple drop, e.g. example step 4 label b) still
renders both rows: followed edges red, targets empty.

### Colors

The single source of truth for the Belyanin step figure colors (TikZ / DOT):

| Tier | Meaning | TikZ | DOT |
| --- | --- | --- | --- |
| Start vertex / initial state | graph sources, DFA start state | `fill=green!30` | `fillcolor=green` |
| Frontier states / frontier (initial) vertices | non-empty M[q, \*] / paths' endpoints | `fill=lightblue!20` | `fillcolor=lightblue` |
| Target vertices | newly reached: Extend[?, v] ≠ ∅ | `fill=yellow!20` | `fillcolor=lightyellow` |
| Current transition edges | used a-transitions / followed a-edges | `red, thick` | `color=red, penwidth=2.0` |
| Frontier path edges | edges of the frontier paths (M or NewM) | `red!40` | `color="#FF9999"` |
| Empty matrix cell | — | `$\cdot$` | — |

Vertex fill precedence in the graph figures: start > frontier > target (a source that is
also a frontier vertex renders green). In the automaton figures the existing renderer
precedence applies (a highlighted state renders lightblue whether or not it is also the
start state — see [AutomatonTikz](automaton-viz.md)).

## Step Templates

Six templates replace the old two (`data/`):

- `Belyanin_step_(tikz_)template.tex` — the start-of-step line: three minipages
  (0.34/0.33/0.33 of `\textwidth`) with `__MATRICES_START__`,
  `__AUTOMATON_START_TIKZ__` / `__AUTOMATON_START_PDF__`, and
  `__GRAPH_START_TIKZ__` / `__GRAPH_START_PDF__`, then the `__LABEL_ROWS__` and
  `__ROW_END__` slots. Init step: both slots empty.
- `Belyanin_row_end_(tikz_)template.tex` — the end-of-step line: same three minipages
  with `__MATRICES_END__`, `__AUTOMATON_END_TIKZ__` / `__AUTOMATON_END_PDF__`, and
  `__GRAPH_END_TIKZ__` / `__GRAPH_END_PDF__`.
- `Belyanin_label_row_(tikz_)template.tex` — one per-label block: two lines of
  `[math | figure]` minipages (0.56/0.44) with `__SELECT_FORMULA__`,
  `__LABEL_AUTOMATON_TIKZ__` / `__LABEL_AUTOMATON_PDF__`, `__EXTEND_FORMULA__`, and
  `__LABEL_GRAPH_TIKZ__` / `__LABEL_GRAPH_PDF__`.

The matrix minipages keep the `\begingroup \setlength{\textwidth}{\linewidth}` group so
the adjustbox-wrapped matrices shrink to the column. At summary time the section builder
(`SummaryTeX.belyaninStepSection`) fills the templates file-driven from the step dir,
wraps the TikZ figures in `SummaryTeX.wrapTikzAdjustboxColumn` (DOT mode includes
`dot_pdfs/{stepName}_*.pdf`), concatenates the label rows in numeric label-index order,
and wraps the whole filled template in `SummaryTeX.wrapStepAdjustbox` (at most
`\textwidth` and `0.9\textheight`) so every step fits one page — see
[SummaryTeX module](summary-tex.md).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Per-label intermediate result named F^a (no prime) | User guidance: the first per-label line is `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]`, the second `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]`; all multiplication signs are `\otimes` (the old partial `\times G^a` fragment is gone) |
| V at the start of a step = P \\ M (cell-wise set difference) | The trace stores P *after* `P ← P ⊕ F`, so removing the current frontier recovers exactly the visited-before state; no trace change needed. V at the end of the step = the stored P |
| Used transitions / followed edges highlighted per label | The book's per-label term `(N^a)^T ⊗ M ⊗ G^a` acts on exactly these edges; highlighting them (not whole paths) shows where the label propagates |
| Green sources in every step graph and the root Input Graph | Matches the automaton's initial-state fill, so the two figures of a line read as one system (user guidance; shared with Arroyuelo RPQ's root graph) |
| Per-label rows only for non-empty Select | All-empty products add noise; the book's I_simple drop is visible as a non-empty Select with an empty Extend (example: step 4, label b) |
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
