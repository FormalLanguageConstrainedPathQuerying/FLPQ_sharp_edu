# BelyaninSimplePathStepVisualizer Module

**Tags:** visualization, rpq, belyanin, dot, tikz, matrix, step-visualization, simple-path
**Kind:** visualization
**Module:** BelyaninSimplePathStepVisualizer
**Source:** `src/FLPQ.Printers/BelyaninSimplePathStepVisualizer.fs`
**Depends on:** BelyaninRPQ (FLPQ.RPQ), BelyaninStepCommon, PathSemiring, PathSemiringTeX, RpqGraphViz, AutomatonDot, AutomatonTikz
**Used by:** FLPQ.Cli (`BelyaninSimplePathRunner`, task 288)
**Book reference:** Chapter 11, Section 02_BFS.tex, Algorithm algo:RPQ_BFS_semiring

> **Abstract:** Renders each step of Belyanin's path-semiring evaluation — the simplePath
> semantics (`BelyaninRPQ.evaluateSimplePathWithTrace`), one main-loop iteration of the
> BFS-like traversal plus an initialization step. Each step is one line per substep:
> the start-of-step line (frontier F, visited V, the input graph with green sources,
> lightblue frontier vertices, and lightred frontier path edges, and the query DFA with the
> frontier states highlighted — left to right), two lines per active label with the explicit
> propagation products `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` and
> `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` (each product on one horizontal line) plus the used
> transitions / followed edges highlighted red bold, and — for non-init steps — the
> end-of-step line (F, V, graph, DFA). Both DOT and TikZ are produced per step; the
> runner writes only one format (TikZ by default, DOT with `--use-dot`).

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
    { Frontier: string; Visited: string; AutomatonDot: string; AutomatonTikz: string; GraphDot: string; GraphTikz: string }

type BelyaninVisualizationStep =
    { Start: BelyaninBoundaryVisual; Labels: BelyaninLabelVisual list; End: BelyaninBoundaryVisual option }
```

The three record types are defined in [BelyaninStepCommon](belyanin-step-common.md) and
shared with the reachability visualizer (the CLI writer and the `-s` summary consume the
same record). One record per trace step (`BelyaninRPQ.BelyaninTraceStep`), one
`BelyaninLabelVisual` per active label in trace order. A boundary
is the frontier F and visited V matrices as **separate** artifacts (F/V at the start and at
the end; the step template places them side by side) plus the automaton/graph figures with
that boundary's frontier highlighted. The init step has no end state (`End = None`) and no
label rows — the absence is unrepresentable by accident, so the writer and the tests match
on `End` instead of checking for empty strings.

## Module Functions

```fsharp
val renderSteps: ('t -> string) -> DFA<'t, int> -> NFA<'t, int> -> BelyaninTraceStep<'t> list -> BelyaninVisualizationStep list
```

`renderSteps terminalPrinter dfa graph steps` renders every trace step. The terminal
printer labels the graph's edges, the query DFA transitions, and the per-label formulas.
Every highlight set is derived from the trace only (no cross-step data):

- `BelyaninStepCommon.frontierStates PathSemiring.isZero m` — automaton states q with a
  non-empty M[q, \*] cell.
- `RpqGraphViz.frontierVertices m` — graph vertices at which m's paths end.
- `BelyaninStepCommon.usedAutoEdges PathSemiring.isZero m n` — DFA transitions q → q′ with
  `n.[q, q']` and a frontier path at q.
- `BelyaninStepCommon.followedEdges PathSemiring.isZero g select` — graph edges u → v with
  `g.[u, v]` and a selected path at u.
- `frontierStates F^a` — per-label DFA target states: q with a non-empty `F^a[q, \*]` cell
  (the destinations of the used a-transitions), highlighted lightyellow.

## Step Artifacts

The runner (`Helpers.writeBelyaninStepsVisualization`) writes one directory per trace
step; the init step has only the start artifacts:

| File | Content |
| --- | --- |
| `frontier_start.tex` | `$F = $` matrix(M) — the current frontier, `valign=T` |
| `visited_start.tex` | `$V = $` matrix(P \\ M) — visited before the step, `valign=T` |
| `automaton_start.{tikz.tex\|dot}` | DFA, frontier states of M highlighted (no edge highlights) |
| `graph_start.{tikz.tex\|dot}` | green sources, lightblue frontier vertices of M, lightred path edges of M |
| `label_i_select.tex` | `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` — every matrix written out, one line, `valign=T` |
| `label_i_extend.tex` | `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` — every matrix written out, one line, `valign=T` |
| `label_i_automaton.{tikz.tex\|dot}` | DFA, the label's used a-transitions red bold and its target states lightyellow |
| `label_i_graph.{tikz.tex\|dot}` | green sources, red followed a-edges, lightblue current vertices (non-empty `Select` columns), lightyellow targets, lightred path edges of M |
| `frontier_end.tex` | `$F = $` matrix(NewM) — non-init steps only, `valign=T` |
| `visited_end.tex` | `$V = $` matrix(P) — non-init steps only, `valign=T` |
| `automaton_end.{tikz.tex\|dot}` | DFA, frontier states of NewM highlighted — non-init steps only |
| `graph_end.{tikz.tex\|dot}` | green sources, lightblue frontier vertices of NewM, lightred path edges of NewM — non-init steps only |

Every boundary tile renders the title and the matrix in one adjustbox/math as
`$F = [F]$` / `$V = [V]$`; every step adjustbox carries `valign=T` (including the
start frontier F).

Label index `i` = position in the trace's `Labels` list (Map iteration order over the
boolean decomposition). The per-label graph figure's highlight sets: followed edges =
`followedEdges G^a F^a`, current vertices = `RpqGraphViz.frontierVertices Select` (the
selected `F^a` columns — a superset of the followed-edge sources, so a selected vertex
with no outgoing a-edge is still shown lightblue), targets =
`RpqGraphViz.frontierVertices Extend`. A label whose Select is non-empty but whose
Extend is entirely empty (the book's I_simple drop, e.g. example step 4 label b) still
renders both rows: followed edges red, targets empty.

### Colors

The single source of truth for the Belyanin step figure colors (TikZ / DOT):

| Tier | Meaning | TikZ | DOT |
| --- | --- | --- | --- |
| Start vertex / initial state | graph sources, DFA start state | `fill=green!30` | `fillcolor=green` |
| Frontier states / frontier (initial) vertices | non-empty M[q, \*] / paths' endpoints | `fill=lightblue!20` | `fillcolor=lightblue` |
| Target vertices / DFA targets | newly reached: Extend[?, v] ≠ ∅ (graph) / F^a[q, \*] ≠ ∅ (DFA) | `fill=yellow!20` | `fillcolor=lightyellow` |
| Current transition edges | used a-transitions / followed a-edges | `red, thick` | `color=red, penwidth=2.0` |
| Frontier path edges | edges of the frontier paths (M or NewM) | `red!40` | `color="#FF9999"` |
| Empty matrix cell | — | `$\cdot$` | — |

Vertex fill precedence (graph and automaton, user guidance): target over current (frontier)
over start. A source that is also a frontier vertex renders lightblue, and a target that is
also current or start renders yellow; in the automaton the final-state fill sits below
current (target over current over final over start) and the double ring is always kept —
see [AutomatonTikz](automaton-viz.md) and [GssTikz](gss-tikz.md).

## Step Templates

Six templates replace the old two (`data/`):

- `Belyanin_step_(tikz_)template.tex` — the start-of-step line: four minipages in
  order F, V, graph, automaton (0.24 × `\textwidth`, `[t]`-aligned, `\hfill%`-separated)
  with `__FRONTIER_START__`, `__VISITED_START__`,
  `__GRAPH_START_TIKZ__` / `__GRAPH_START_PDF__`, and
  `__AUTOMATON_START_TIKZ__` / `__AUTOMATON_START_PDF__`, then the `__LABEL_ROWS__` and
  `__ROW_END__` slots. Init step: both slots empty.
- `Belyanin_row_end_(tikz_)template.tex` — the end-of-step line: the same four minipages
  with `__FRONTIER_END__`, `__VISITED_END__`,
  `__GRAPH_END_TIKZ__` / `__GRAPH_END_PDF__`, and
  `__AUTOMATON_END_TIKZ__` / `__AUTOMATON_END_PDF__`.
- `Belyanin_label_row_(tikz_)template.tex` — one per-label block: two lines of
  `[formula | figure]` minipages (0.6/0.38, `\hfill%`-separated) with
  `__SELECT_FORMULA__`, `__LABEL_AUTOMATON_TIKZ__` / `__LABEL_AUTOMATON_PDF__`,
  `__EXTEND_FORMULA__`, and `__LABEL_GRAPH_TIKZ__` / `__LABEL_GRAPH_PDF__`.

Every minipage line starts with `\noindent` (the lines are separate paragraphs, and the
paragraph indent would otherwise overfull the line). The matrix minipages keep the
`\begingroup \setlength{\textwidth}{\linewidth}` group so the adjustbox-wrapped matrices
shrink to the column. At summary time the section builder
(`SummaryTeX.belyaninStepSection`) fills the templates file-driven from the step dir,
wraps the TikZ figures in the top-aligned `SummaryTeX.wrapTikzAdjustboxColumnTop`
(`max width=\linewidth, valign=T`; DOT mode includes `dot_pdfs/{stepName}_*.pdf`), and
concatenates the label rows in numeric label-index order. The filled template is
**not** wrapped whole: each component keeps its own width-limited adjustbox, so a tall
step can break across pages (Arroyuelo RPQ follows the same rule since task 291) — see
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
| F and V as separate boundary artifacts | The start/end line places F and V in separate side-by-side minipages, so the boundary record exposes `Frontier` and `Visited` instead of one combined string the template cannot split |
| One substep per line; start/end use F, V, graph, automaton (`\noindent`, `\hfill%`) | Splitting the packed row into one line per substep makes every component full-column-width and readable; the `\noindent` removes the paragraph indent that would overfull the line, and the `\hfill%` separators keep the widths within `\textwidth` |
| Products rendered as one horizontal line | `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` and `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` are single math expressions (matrices side by side) wrapped once in an adjustbox, instead of a vertical matrix stack — user guidance (`PathSemiringTeX` body variants supply the un-wrapped matrices) |
| No whole-step adjustbox | User guidance: a step may span pages; each component is wrapped in its own width-limited adjustbox instead. Arroyuelo RPQ follows the same rule since task 291 |
| Boundary tiles render `$F = [F]$` / `$V = [V]$`, end frontier titled `F` | User guidance: the title and matrix are one adjustbox/math, and the step position conveys start vs end, so "New F" is dropped |
| `valign=T` on every step adjustbox | User guidance: top-aligns the tiles so each boundary/label line reads evenly, including the start frontier F. Arroyuelo RPQ also uses top-aligned figures since task 291 |
| Per-label DFA targets = `frontierStates F^a` | Parallels the graph's `frontierVertices Extend` (the states selected on the step, i.e. the destinations of the used a-transitions); lightyellow with the highest precedence (target > current > start) |

## Book Reference

Chapter 11, `02_BFS.tex`: algorithm `algo:RPQ_BFS_semiring` — each main-loop iteration
propagates the frontier through `(N^a)^T ⊗ M ⊗ G^a` per label; the book example figure
`figures/02_BFS/algorithm_step.tex` shows paths in matrix cells.

## See Also

- [Belyanin RPQ module](belyanin-rpq.md) — `evaluateSimplePathWithTrace` produces the trace steps
- [Belyanin step common](belyanin-step-common.md) — shared record types and cell-agnostic helpers
- [Belyanin reachability step visualization](belyanin-reachability-step-viz.md) — the Boolean variant (no path edges)
- [RPQ graph visualization](rpq-graph-viz.md) — shared input-graph rendering with path highlights
- [PathSemiringTeX module](path-semiring-tex.md) — matrix rendering
- [FLPQ.Printers hub](FLPQ.Printers.md)
