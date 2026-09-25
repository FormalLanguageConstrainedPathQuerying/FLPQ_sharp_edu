# GSS DOT Rendering

**Tags:** visualization, gll, rnglr, dot, graph-structured-stack
**Kind:** visualization
**Module:** GssDot
**Source:** `src/FLPQ.Printers/GssDot.fs`
**Depends on:** DerivationTreeDot (label escaping)
**Used by:** GllStepVisualizer, RnglrStepVisualizer, ArroyueloStepVisualizer, RpqGraphViz

> **Abstract:** Graphviz DOT rendering of the graph-structured stack (GSS). Renders active vertices as ellipses with fill colors for the highlighted, stored-pop, and current states, and edges with two highlight tiers: highlighted edges are red with penwidth=2.0, path edges are light red (#FF9999). `toDotFromSets` renders directly from vertex/edge sets (step visualization); `toDot` renders a full GSS struct.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The GSS is the working data structure of the GLL and RNGLR parsers: a graph whose
vertices are (LR state, input vertex) pairs and whose edges carry matched symbol
sets. Step visualizers render only the active part of the stack with the step's new
elements highlighted; the RPQ visualizers reuse the same set-based rendering for the
input graph.

## Supported Formats

- Graphviz DOT (`digraph GSS`, `rankdir=LR`), compiled to PDF by the runner via
  `ExternalTools`.

## Function Signatures

### `toDot: (int -> string) -> (int * int -> string) -> Set<int> -> Set<int * int> -> int option -> GSS -> string`

Renders a full GSS struct. Only active vertices (those with outgoing edges) are
rendered. Parameters: vertex label printer, edge label printer, highlightedVertices,
highlightedEdges, currentVertex, gss.

### `toDotFromSets: (int -> string) -> (int * int -> string) -> Set<int> -> Set<int * int> -> Set<int> -> Set<int * int> -> Set<int * int> -> Set<int> -> int option -> (int -> int) option -> string`

Renders directly from vertex/edge sets — used for step visualization where only the
active elements are known. Parameters in order: vertex label printer, edge label
printer, activeVertices, activeEdges, highlightedVertices, highlightedEdges,
pathEdges, storedPopVertices, currentVertex, positionOf.

- Vertex fills: current = lightblue (overrides all), stored-pop = orange,
  highlighted = lightyellow, else plain.
- Edge tiers: highlighted = `color=red, penwidth=2.0`; path = `color="#FF9999"`
  (an edge in both sets renders as highlighted); else plain.
- `positionOf`: when `Some`, one `{rank=same; ...}` subgraph per position groups the
  vertices of that input position into a layer.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Two edge highlight tiers (highlighted / path) | Step figures need to distinguish the elements added on the current step (red, bold) from the accumulated structure they extend (light red); one tier cannot express both |
| `toDotFromSets` alongside `toDot` | Step visualizers know only the active vertex/edge sets, not the full GSS struct; sharing the rendering keeps DOT and TikZ output consistent |
| Label escaping via `DerivationTreeDot.escapeLabel` | One source of truth for DOT label escaping across tree and stack renderers |
| Hex colors quoted (`color="#FF9999"`) | An unquoted `#` starts a DOT comment and breaks the edge line at graphviz compile time |

## See Also

- [GSS TikZ](gss-tikz.md) — the TikZ counterpart with layered layout
- [GLL step visualization](gll-step-visualizer.md) / [RNGLR](rnglr.md) — main consumers
- [RPQ graph visualization](rpq-graph-viz.md) — reuses `toDotFromSets` for input graphs
