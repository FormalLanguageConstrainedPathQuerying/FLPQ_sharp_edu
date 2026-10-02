# RPQ Graph TikZ

**Tags:** visualization, rpq, graph, tikz
**Kind:** visualization
**Module:** RpqGraphTikz
**Source:** `src/FLPQ.Printers/RpqGraphTikz.fs`
**Depends on:** AutomatonTikz
**Used by:** RpqGraphViz

> **Abstract:** TikZ graphdrawing rendering of the RPQ input graph (a labeled NFA) with layered layout. Dedicated to RPQ — it does not depend on the GSS modules. Vertex fill precedence is target (yellow!20) > current/frontier (lightblue!20) > start (green!30); current edges render red bold and path edges light red; reciprocal edge pairs are bent apart. Labels are taken as already-rendered TeX.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

`RpqGraphTikz` emits a `tikzpicture` with a single layered `\graph`. The input graph is always
fully active; the caller supplies the highlight sets. The output is byte-identical to the RPQ
parameterization of the former general GSS renderer, so existing Belyanin/Arroyuelo goldens do
not change.

## Supported Formats

| Format | Output |
| --- | --- |
| TikZ | `\begin{tikzpicture} \graph [layered layout, nodes={draw, circle}, grow'=right, ...] { ... }; \end{tikzpicture}` |

## Function Signatures

### `toTikz: (int -> string) -> (int * int -> string) -> Set<int> -> Set<int * int> -> Set<int> -> Set<int> -> Set<int> -> Set<int * int> -> Set<int * int> -> string`

`toTikz vertexLabelPrinter edgeLabelPrinter activeVertices activeEdges highlightedVertices startVertices frontierVertices highlightedEdges pathEdges`.
Labels are taken as already-rendered TeX (the RPQ caller passes `$v_i$` vertices and escaped
edge labels), so no second escaping is applied. Highlighted edges render `red, thick`, path
edges `red!40`. A reciprocal pair (`u -> v` and `v -> u`) gets `bend left=15` on both edges via
`AutomatonTikz.reciprocalBendAttr`; self-loops are never bent.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Dedicated RPQ module instead of reusing `GssTikz` | The RPQ input graph is an NFA, not a graph-structured stack; GSS is a GLL/RNGLR artifact and must not be a dependency of RPQ (task 291) |
| Byte-identical to the previous RPQ output | Keeps the Belyanin step goldens unchanged while removing the GSS dependency |
| Reciprocal pairs bent with `bend left=15` | TikZ would otherwise draw both directions as fully overlapping straight lines (task 282, S2) |
| Labels accepted as already-rendered TeX | The RPQ caller escapes edge labels once and formats vertex labels as `$v_i$`; a second escape would corrupt them |

## See Also

- [RPQ graph visualization](rpq-graph-viz.md) — the orchestrator that calls this renderer
- [RPQ graph DOT](rpq-graph-dot.md) — the DOT counterpart
- [Automaton TikZ](automaton-viz.md) — shared TikZ primitives (header/footer, bend helper)
