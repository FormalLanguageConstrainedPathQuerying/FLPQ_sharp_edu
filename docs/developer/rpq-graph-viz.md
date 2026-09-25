# RPQ Graph Visualization

**Tags:** visualization, rpq, graph, dot, tikz, path-semiring
**Kind:** visualization
**Module:** RpqGraphViz
**Source:** `src/FLPQ.Printers/RpqGraphViz.fs`
**Depends on:** GssDot, GssTikz, AutomatonTikz, PathSemiring
**Used by:** ArroyueloStepVisualizer, BelyaninStepVisualizer, ArroyueloRunner, BelyaninRunner

> **Abstract:** Shared rendering of the RPQ input graph (a labeled NFA) with path highlights. Given a path semiring matrix, extracts the vertices and edges used by its stored paths and renders the graph in DOT and TikZ with those elements highlighted. Used by both RPQ step visualizers and runners so the graph figure has one source of truth.

## Contents

- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Function Signatures

### `pathHighlights: Matrix<Set<int list>> -> Set<int> * Set<int * int>`

Vertices and edges used by the paths stored in a path semiring matrix. A path
`[v0; v1; ...]` contributes all its vertices and all consecutive vertex pairs as edges.

### `renderGraph: ('t -> string) -> NFA<'t, int> -> Set<int> -> Set<int * int> -> string * string`

Render the graph with the given highlighted vertices/edges (pass empty sets for the plain
input graph). Returns `(dot, tikz)`. Vertex labels are `v_i` (TikZ: `$v_i$`), edge labels
are the comma-joined terminal labels of the pair (epsilon-only edges label as "ε").

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Extracted from ArroyueloStepVisualizer | Both RPQ step visualizers and both runners render the same graph figure — one shared module instead of duplicated logic (task 281, S2) |
| Reuses `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` | The input graph is an NFA with states = vertices; all vertices/edges active, step paths as highlights, no stored-pop/current-vertex state |
| TikZ rendering passes `bendReciprocalEdges = true` | An input graph may carry both directions of a pair (e.g. `v2 -> v3` and `v3 -> v2`); TikZ would draw the two straight edges fully on top of each other, while `bend left=15` on both turns the pair into two symmetric arcs. DOT needs no equivalent — Graphviz separates reciprocal pairs automatically (task 282, S2) |

## See Also

- [Arroyuelo step visualization](arroyuelo-step-viz.md) — regexp tree + matrix equations + this graph figure
- [Path semiring](path-semiring.md) — the path matrices consumed by `pathHighlights`
- [GSS DOT](gss-dot.md) / [GSS TikZ](gss-tikz.md) — the underlying set-based graph renderers
