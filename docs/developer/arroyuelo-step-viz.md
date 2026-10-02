# Arroyuelo Simple-Path Step Visualization

**Tags:** visualization, rpq, arroyuelo, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** ArroyueloSimplePathStepVisualizer
**Source:** `src/FLPQ.Printers/ArroyueloSimplePathStepVisualizer.fs`
**Depends on:** ArroyueloStepCommon, ArroyueloRPQ (FLPQ.RPQ), RpqGraphViz, PathSemiringTeX, RegexpTeX, StepTeX
**Used by:** ArroyueloSimplePathRunner, FLPQ.Cli
**Book reference:** Chapter 11, Section sec:RPQ_Arroyuelo

> **Abstract:** Renders the simple-path evaluation of Arroyuelo's RPQ algorithm. Each emitted step is one intermediate regexp-tree node (Alt/Seq/Star); leaves are not emitted as steps but appear in the vertical tree figure. Per step it produces the regexp tree with the current node highlighted, the path-semiring matrix equation for the step's operation, and the input graph with the vertices/edges of the step's result paths highlighted. Both DOT and TikZ are produced; the runner writes one format (TikZ by default, DOT with `--use-dot`). The shared record and tree rendering live in [ArroyueloStepCommon](arroyuelo-step-common.md).

## Contents

- [Step Artifacts](#step-artifacts)
- [Tree](#tree)
- [Matrices](#matrices)
- [Graph](#graph)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Step Artifacts

One record per emitted step (`ArroyueloStepCommon.ArroyueloVisualizationStep`), written by
`FLPQ.Cli` as `step_N/tree.{dot,tikz.tex}`, `step_N/matrices.tex`, `step_N/graph.{dot,tikz.tex}`.

## Tree

The regexp AST rendered by `RegexpTreeDot` / `RegexpTreeTikz`. The tree is **vertical** (root at
top; TikZ `grow'=down`, DOT `rankdir=TB`) and rectangle-shaped. Every AST node is drawn,
including leaves; the current step's node is filled light blue. A tree node's label is its
subexpression via `RegexpTeX.toTeX`.

## Matrices

One horizontal formula line of path semiring matrix bodies
(`PathSemiringTeX.matrixToTeXBody`) per operation, wrapped in a single top-aligned adjustbox
(`StepTeX.wrapFormula`): Alt shows `M(left) ⊕ M(right) = M(node)`; Seq shows
`M(left) × M(right) = M(node)`; Star shows `TC(M(child)) = M(node)`.

## Graph

All graph vertices/edges active, edge labels = terminal names; highlighted vertices/edges
(yellow/red) are the union over all paths in the step's Result matrix, derived via
`RpqGraphViz.pathHighlights`. Step k's highlight set depends only on step k's Result.

## Module Functions

```fsharp
val renderSteps: ('t -> string) -> ('nt -> string) -> NFA<'t, int> -> ArroyueloTraceStep<'t, 'nt> list -> ArroyueloVisualizationStep list
```

`renderSteps terminalPrinter nontermPrinter graph steps` takes the **full** post-order trace,
builds the tree once, filters out `Base` (leaf) steps, and emits one record per remaining
step. The terminal and nonterminal printers label the graph's edges and the tree
subexpressions.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Only intermediate nodes (Alt/Seq/Star) are emitted | Leaf steps carry no matrix operation worth visualizing; the tree still shows the leaves for context (task 291) |
| Tree is vertical | Matches the book's tree orientation and reads better beside the tall matrix stack (task 291) |
| Dedicated `RegexpTreeDot` / `RegexpTreeTikz` | The regexp tree is an AST, not a GSS; RPQ must not depend on the GSS renderers (task 291) |
| Tree includes every AST node | The highlighted current node must be locatable in the full expression |
| Graph highlights derived from the step's Result only | Each step figure must show what that step computed; `PathSemiring.allPaths` gives exactly the stored simple paths |
| Step graphs carry no green sources / frontier tier | The green source fill applies to the shared root Input Graph and Belyanin's step figures; Arroyuelo step graphs keep the simpler look |

## Book Reference

Chapter 11, `03_Arroyuelo.tex`: Section sec:RPQ_Arroyuelo — matrix evaluation of the regexp
tree; each node's matrix equation is one step.

## See Also

- [Arroyuelo step common](arroyuelo-step-common.md) — shared record, tree model, and rendering
- [Arroyuelo RPQ module](arroyuelo-rpq.md) — `evaluateWithTrace` produces the trace steps
- [Arroyuelo reachability step visualization](arroyuelo-reachability-step-viz.md) — the Boolean semantics
- [PathSemiringTeX module](path-semiring-tex.md) — matrix rendering
- [RPQ graph visualization](rpq-graph-viz.md) — the graph figure
- [FLPQ.Printers hub](FLPQ.Printers.md)
