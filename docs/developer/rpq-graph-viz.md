# RPQ Graph Visualization

**Tags:** visualization, rpq, graph, dot, tikz, path-semiring
**Kind:** visualization
**Module:** RpqGraphViz
**Source:** `src/FLPQ.Printers/RpqGraphViz.fs`
**Depends on:** GssDot, GssTikz, AutomatonTikz, PathSemiring
**Used by:** ArroyueloStepVisualizer, BelyaninSimplePathStepVisualizer, BelyaninReachabilityStepVisualizer, ArroyueloRunner, BelyaninSimplePathRunner, BelyaninReachabilityRunner

> **Abstract:** Shared rendering of the RPQ input graph (a labeled NFA) with path highlights. Given a path semiring matrix, extracts the vertices and edges used by its stored paths and renders the graph in DOT and TikZ with two edge highlight tiers: path edges (light red) and current-step edges (red, bold), plus vertex fills for start (green), frontier/current (light blue), and highlighted/target (yellow) vertices with target > current > start precedence. Used by both RPQ step visualizers and runners so the graph figure has one source of truth.

## Contents

- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Function Signatures

### `pathVertices: Matrix<Set<int list>> -> Set<int>`

Vertices used by the paths stored in a path semiring matrix. A path `[v0; v1; ...]`
contributes all its vertices.

### `pathEdges: Matrix<Set<int list>> -> Set<int * int>`

Edges used by the paths stored in a path semiring matrix. A path `[v0; v1; ...]`
contributes all consecutive vertex pairs as edges.

### `pathLastEdges: Matrix<Set<int list>> -> Set<int * int>`

The last edge of every stored path with at least two vertices — the edge traversed when
the path was extended to its final vertex. Trivial (single-vertex) paths contribute
nothing. This is the "current step" edge set for a frontier matrix.

### `edgeEndpoints: Set<int * int> -> Set<int>`

Both endpoints of every edge in the set.

### `frontierVertices: Matrix<Set<int list>> -> Set<int>`

The vertices at which the stored paths end — the frontier's positions in the graph
(the graph-side counterpart of the automaton's frontier states). A path contributes its
last vertex; trivial single-vertex paths mark their vertex as a frontier position.

### `pathHighlights: Matrix<Set<int list>> -> Set<int> * Set<int * int>`

`(pathVertices, pathEdges)` — vertices and edges used by the stored paths.

### `renderGraph: ('t -> string) -> NFA<'t, int> -> Set<int> -> Set<int> -> Set<int> -> Set<int * int> -> Set<int * int> -> string * string`

Render the graph with the given highlights (pass empty sets for the plain input
graph): highlightedVertices get the yellow target fill, startVertices the green source
fill, frontierVertices the light blue current fill, currentEdges render red and bold,
pathEdges light red (an edge in both sets renders as current). Vertex fill precedence is
target > current > start, so a target that is also current or start renders yellow and a
source that is also current renders lightblue (the tier order mirrors
`GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`). Returns `(dot, tikz)`.
Vertex labels are `v_i` (TikZ: `$v_i$`), edge labels are the comma-joined terminal
labels of the pair (epsilon-only edges label as "ε").

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Extracted from ArroyueloStepVisualizer | Both RPQ step visualizers and both runners render the same graph figure — one shared module instead of duplicated logic (task 281, S2) |
| Reuses `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` | The input graph is an NFA with states = vertices; all vertices/edges active, step paths as highlights, no stored-pop/current-vertex state |
| TikZ rendering passes `bendReciprocalEdges = true` | An input graph may carry both directions of a pair (e.g. `v2 -> v3` and `v3 -> v2`); TikZ would draw the two straight edges fully on top of each other, while `bend left=15` on both turns the pair into two symmetric arcs. DOT needs no equivalent — Graphviz separates reciprocal pairs automatically (task 282, S2) |
| Two edge tiers: path (light red) and current (red, bold) | A step figure must distinguish the accumulated frontier paths from the single edge traversed on that step; `pathLastEdges` of the next frontier is exactly the traversed edges (task 284, S5) |

## See Also

- [Arroyuelo step visualization](arroyuelo-step-viz.md) — regexp tree + matrix equations + this graph figure
- [Path semiring](path-semiring.md) — the path matrices consumed by `pathHighlights`
- [GSS DOT](gss-dot.md) / [GSS TikZ](gss-tikz.md) — the underlying set-based graph renderers
