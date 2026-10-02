# Arroyuelo Reachability Step Visualization

**Tags:** visualization, rpq, arroyuelo, boolean, dot, tikz, matrix, step-visualization
**Kind:** visualization
**Module:** ArroyueloReachabilityStepVisualizer
**Source:** `src/FLPQ.Printers/ArroyueloReachabilityStepVisualizer.fs`
**Depends on:** ArroyueloStepCommon, ArroyueloRPQ (FLPQ.RPQ), RpqGraphViz, PathSemiringTeX, RegexpTeX, Regexp, StepTeX
**Used by:** ArroyueloReachabilityRunner (FLPQ.Cli)
**Book reference:** Chapter 11, Section sec:RPQ_Arroyuelo

> **Abstract:** Renders the Boolean reachability evaluation of Arroyuelo's RPQ algorithm. Each emitted step is one intermediate regexp-tree node (Alt/Seq/Star); leaves are not emitted as steps but appear in the vertical tree figure. Per step it produces the regexp tree with the current node highlighted, the Boolean matrix equation for the node's operation, and the input graph with derived edges: for every true cell (u, v) of the handled node's Boolean result matrix M(E), an edge u -> v labeled with the node's subexpression is added (red bold). No paths or input edges are highlighted; when a derived edge collides with an input edge, the labels merge.

## Contents

- [Step Artifacts](#step-artifacts)
- [Matrices](#matrices)
- [Derived-Edge Graph](#derived-edge-graph)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Step Artifacts

One record per emitted step (`ArroyueloStepCommon.ArroyueloVisualizationStep`), written by
`FLPQ.Cli` as `step_N/tree.{dot,tikz.tex}`, `step_N/matrices.tex`, `step_N/graph.{dot,tikz.tex}`.

## Matrices

The regexp tree and the one-line matrix formula share the layout of the simple-path variant
([ArroyueloStepCommon](arroyuelo-step-common.md)). Matrices are Boolean (`\bullet`/`\cdot`,
rows and columns both `v_i`): Alt shows `M(left) ∨ M(right) = M(node)`; Seq shows
`M(left) × M(right) = M(node)`; Star shows `TC(M(child)) = M(node)`.

## Derived-Edge Graph

The graph keeps the original input edges (plain) and adds one red-bold edge per true cell
`(u, v)` of the handled node's Boolean result matrix M(E), labeled with the node's
subexpression (`RegexpTeX.toTeX` in TikZ, `Regexp.toString` in DOT). A derived edge that
collides with an input edge merges the two labels into one edge (e.g. `a, b | c`).
No path (`red!40`) or result-vertex (`yellow!20`) highlights are drawn; start vertices stay
green. The rendering is done by `RpqGraphViz.renderGraphWithDerived`.

## Module Functions

```fsharp
val renderSteps: ('t -> string) -> ('nt -> string) -> NFA<'t, int> -> ArroyueloBooleanTraceStep<'t, 'nt> list -> ArroyueloVisualizationStep list
```

`renderSteps terminalPrinter nontermPrinter graph steps` takes the full post-order Boolean
trace, builds the tree once, filters out `Base` (leaf) steps, and emits one record per
remaining step.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Boolean reachability, not a path projection | The Boolean semantics is complete on cyclic graphs; the simple-path projection of `evaluateWithTrace` misses non-simple accepting walks. The algorithm uses `ArroyueloRPQ.evaluateBooleanWithTrace` (task 291) |
| No path/edge highlighting | Boolean cells identify a relation, not a witness path; there are no paths to draw (task 291) |
| Added M(E) edges labeled with the handled node | Visualizes the matrix as the relation the node computes, tying the figure back to the regular expression (task 291) |
| Derived-edge labels merge with input-edge labels on the same pair | Both are meaningful (input symbol and computed relation); merging keeps a single edge per pair without parallel-edge support |
| Same emitted-step set as the simple-path variant | Leaves carry no operation; the tree still shows them for context |

## Book Reference

Chapter 11, `03_Arroyuelo.tex`: Section sec:RPQ_Arroyuelo — Boolean matrix evaluation of the
regexp tree.

## See Also

- [Arroyuelo step common](arroyuelo-step-common.md) — shared record, tree model, and rendering
- [Arroyuelo RPQ module](arroyuelo-rpq.md) — `evaluateBooleanWithTrace`
- [Arroyuelo simple-path step visualization](arroyuelo-step-viz.md) — the path-semiring semantics
- [RPQ graph visualization](rpq-graph-viz.md) — `renderGraphWithDerived`
- [FLPQ.Printers hub](FLPQ.Printers.md)
