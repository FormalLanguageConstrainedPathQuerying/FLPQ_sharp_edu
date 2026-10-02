# RPQ Graph DOT

**Tags:** visualization, rpq, graph, dot
**Kind:** visualization
**Module:** RpqGraphDot
**Source:** `src/FLPQ.Printers/RpqGraphDot.fs`
**Depends on:** DerivationTreeDot (`escapeLabel`)
**Used by:** RpqGraphViz

> **Abstract:** Graphviz DOT rendering of the RPQ input graph (a labeled NFA). Dedicated to RPQ — unlike the general GSS renderer it carries no stack state and does not depend on the GSS modules. Vertex fill precedence is target (lightyellow) > current/frontier (lightblue) > start (green); current edges render red bold and path edges light red. Produced alongside the TikZ variant so the runner can pick a format.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

`RpqGraphDot` renders one `digraph` per call from vertex and edge sets. The input graph is
always fully active; the caller supplies the highlight sets. The output is byte-identical to
the RPQ parameterization of the former general GSS renderer, so existing Belyanin/Arroyuelo
goldens do not change.

## Supported Formats

| Format | Output |
| --- | --- |
| DOT | `digraph GSS { rankdir=LR; compound=true; ... }` — ellipse vertices `v_i`, edge labels from the caller, fills via `style=filled, fillcolor=...` |

## Function Signatures

### `toDot: (int -> string) -> (int * int -> string) -> Set<int> -> Set<int * int> -> Set<int> -> Set<int> -> Set<int> -> Set<int * int> -> Set<int * int> -> string`

`toDot vertexLabelPrinter edgeLabelPrinter activeVertices activeEdges highlightedVertices startVertices frontierVertices highlightedEdges pathEdges`.
Labels are escaped with `DerivationTreeDot.escapeLabel`. Highlighted edges render
`color=red, penwidth=2.0`, path edges `color="#FF9999"` (quoted so `#` does not start a DOT
comment), and an edge in both sets renders highlighted.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Dedicated RPQ module instead of reusing `GssDot` | The RPQ input graph is an NFA, not a graph-structured stack; GSS is a GLL/RNGLR artifact and must not be a dependency of RPQ (task 291) |
| Byte-identical to the previous RPQ output | Keeps the Belyanin step goldens unchanged while removing the GSS dependency |
| Vertex fill precedence target > current > start | Matches the RPQ step semantics (a reached target dominates a frontier position, which dominates a source) and the TikZ variant |

## See Also

- [RPQ graph visualization](rpq-graph-viz.md) — the orchestrator that calls this renderer
- [RPQ graph TikZ](rpq-graph-tikz.md) — the TikZ counterpart
