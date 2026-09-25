# GSS TikZ Rendering

**Tags:** visualization, gll, rnglr, tikz, graph-structured-stack
**Kind:** visualization
**Module:** GssTikz
**Source:** `src/FLPQ.Printers/GssTikz.fs`
**Depends on:** AutomatonTikz (header, layered layout options, label escaping)
**Used by:** GllStepVisualizer, RnglrStepVisualizer, ArroyueloStepVisualizer, RpqGraphViz

> **Abstract:** TikZ rendering of the graph-structured stack (GSS) from vertex/edge sets using graphdrawing's layered layout. Vertices get fill colors for the highlighted, stored-pop, and current states; edges have two highlight tiers — highlighted edges are red and bold (`thick`), path edges are light red (`red!40`). Reciprocal edge pairs can be bent into symmetric arcs so they do not draw on top of each other.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The GSS is the working data structure of the GLL and RNGLR parsers. Step visualizers
render the active part of the stack with the step's new elements highlighted; the RPQ
visualizers reuse the same set-based rendering for the input graph.

## Supported Formats

- TikZ `tikzpicture` with `\graph [layered layout, ...]`, compiled by lualatex via
  `ExternalTools`.

## Function Signatures

### `toTikzFromSets: (int -> string) -> (int * int -> string) -> Set<int> -> Set<int * int> -> Set<int> -> Set<int * int> -> Set<int * int> -> Set<int> -> int option -> string -> bool -> (int -> int) option -> bool -> string`

Renders directly from vertex/edge sets. Parameters in order: vertex label printer,
edge label printer, activeVertices, activeEdges, highlightedVertices, highlightedEdges,
pathEdges, storedPopVertices, currentVertex, shape, skipEscaping, positionOf,
bendReciprocalEdges.

- Vertex fills: current = lightblue!20 (overrides all), stored-pop = orange!30,
  highlighted = yellow!20, else plain.
- Edge tiers: highlighted = `red, thick`; path = `red!40` (an edge in both sets
  renders as highlighted); else plain.
- `positionOf`: when `Some`, one `{ [same layer] ... }` collection per position and the
  grow direction switches to `grow=left` (pgf's same-layer chaining reverses the
  orientation under the default grow direction).
- `bendReciprocalEdges`: when true, every edge whose reverse is also drawn gets
  `bend left=15`, turning a reciprocal pair into two symmetric arcs.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Two edge highlight tiers (highlighted / path) | Step figures need to distinguish the elements added on the current step (red, bold — the book draws the current walk `thick`) from the accumulated structure they extend (light red); one tier cannot express both |
| Reuses `AutomatonTikz` primitives | Header, layered layout options, and label escaping stay consistent with the automaton renderers |
| Reciprocal pairs bent by default in RPQ graphs | The input graph may carry both directions of a pair; TikZ would draw the two straight edges fully on top of each other (DOT needs no equivalent — Graphviz separates them) |

## See Also

- [GSS DOT](gss-dot.md) — the DOT counterpart
- [GLL step visualization](gll-step-visualizer.md) / [RNGLR](rnglr.md) — main consumers
- [RPQ graph visualization](rpq-graph-viz.md) — reuses `toTikzFromSets` for input graphs
