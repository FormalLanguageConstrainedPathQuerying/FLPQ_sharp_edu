# Arroyuelo Step Common

**Tags:** visualization, rpq, arroyuelo, dot, tikz, step-visualization
**Kind:** visualization
**Module:** ArroyueloStepCommon
**Source:** `src/FLPQ.Printers/ArroyueloStepCommon.fs`
**Depends on:** RegexpTreeDot, RegexpTreeTikz, RegexpTeX, Regexp (FLPQ.Languages)
**Used by:** ArroyueloSimplePathStepVisualizer, ArroyueloReachabilityStepVisualizer

> **Abstract:** Shared record type, regexp-tree model, and tree rendering for the two Arroyuelo RPQ step visualizers (simple-path and reachability). Both return the same `ArroyueloVisualizationStep` list so the CLI writer and the `-s` summary reuse one pipeline. `renderSteps` builds the vertical tree once from the full post-order trace, drops leaf (`Base`) steps, and assembles each emitted step from caller-supplied matrix and graph renderers.

## Contents

- [Data Structure](#data-structure)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Data Structure

```fsharp
[<Struct>]
type ArroyueloVisualizationStep =
    { TreeDot: string; TreeTikz: string; Matrices: string; GraphDot: string; GraphTikz: string }

type ArroyueloTreeNode<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Index: int; Expr: Regexp<'t, 'nt>; Children: int list }
```

`ArroyueloTreeNode` is the tree view shared by the path trace (`ArroyueloTraceStep`) and the
Boolean trace (`ArroyueloBooleanTraceStep`): post-order index, subexpression, and child indices.

## Module Functions

### `renderTree: ('t -> string) -> ('nt -> string) -> ArroyueloTreeNode<'t, 'nt> list -> int -> string * string`

Render the vertical regexp tree with the current node highlighted, in `(dot, tikz)` form. Every
node (including leaves) is drawn.

### `renderSteps: ('t -> string) -> ('nt -> string) -> ArroyueloTreeNode<'t, 'nt> list -> ('step -> bool) -> ('step -> int) -> ('step -> string) -> ('step -> string * string) -> 'step list -> ArroyueloVisualizationStep list`

`renderSteps terminal nonterm nodes isBase nodeIndexOf buildMatrices buildGraph steps` filters
out `isBase` steps, then renders the tree (via `renderTree`) and the caller's matrix/graph for
each remaining step. The tree is built once from `nodes`; the per-step renderers are supplied by
the concrete visualizer (path semiring vs. Boolean).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Common record and tree pipeline | Simple-path and reachability steps differ only in matrix and graph rendering; sharing the tree and record keeps one pipeline for the CLI writer and summary (task 291) |
| Generic over the step type | The path and Boolean traces are different types; `'step` plus accessor functions avoids duplicating the tree logic |
| Leaves dropped at render time, not in the trace | The trace must retain leaves for `Children`/`NodeIndex` so the tree can be built; only emission is filtered |
| `ArroyueloTreeNode` instead of the concrete trace type | Removes the dependency of the common tree code on the specific algorithm trace |

## See Also

- [Arroyuelo simple-path step visualization](arroyuelo-step-viz.md)
- [Arroyuelo reachability step visualization](arroyuelo-reachability-step-viz.md)
- [Regexp tree DOT](regexp-tree-dot.md) / [Regexp tree TikZ](regexp-tree-tikz.md)
