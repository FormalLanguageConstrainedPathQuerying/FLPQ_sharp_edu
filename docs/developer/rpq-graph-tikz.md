# RPQ Graph TikZ

**Tags:** visualization, rpq, graph, tikz
**Kind:** visualization
**Module:** RpqGraphTikz
**Source:** `src/FLPQ.Printers/RpqGraphTikz.fs`
**Depends on:** AutomatonTikz, FlpqdotTikz
**Used by:** RpqGraphViz

> **Abstract:** TikZ graphdrawing rendering of the RPQ input graph (a labeled NFA) with dot layout. Dedicated to RPQ — it does not depend on the GSS modules. Vertex fill precedence is target (yellow!20) > current/frontier (lightblue!20) > start (green!30); current edges render red bold and path edges light red; reciprocal edge pairs are separated natively by the flpqdot router. Non-loop edge labels are registered in the flpqdot measurement pre-pass and placed by the router (the stock quote is dropped); loop edges keep their stock quote. Labels are taken as already-rendered TeX.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

`RpqGraphTikz` emits a `tikzpicture` with a single layered `\graph`. The input graph is always
fully active; the caller supplies the highlight sets. Every non-loop edge with a non-empty
label is registered in the flpqdot measurement pre-pass (emitted before the `\graph` via the
shared header) and its stock quote is dropped, so the router places the label at a
collision-free position; loop edges keep their stock quote (the router skips loops).

## Supported Formats

| Format | Output |
| --- | --- |
| TikZ | `\begin{tikzpicture} \graph [dot layout, nodes={draw, circle}, grow'=right, ...] { ... }; \end{tikzpicture}` |

## Function Signatures

### `toTikz: (int -> string) -> (int * int -> string) -> Set<int> -> Set<int * int> -> Set<int> -> Set<int> -> Set<int> -> Set<int * int> -> Set<int * int> -> string`

`toTikz vertexLabelPrinter edgeLabelPrinter activeVertices activeEdges highlightedVertices startVertices frontierVertices highlightedEdges pathEdges`.
Labels are taken as already-rendered TeX (the RPQ caller passes `$v_i$` vertices and escaped
edge labels), so no second escaping is applied. Highlighted edges render `red, thick`, path
edges `red!40`. A reciprocal pair (`u -> v` and `v -> u`) is separated natively by the flpqdot
channel router (via the `dot layout` key); the renderer emits no `bend` option. Non-loop
edges with a non-empty label are registered in the measurement pre-pass and rendered without
a stock quote (the router places the label); loop edges keep their stock quote.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Dedicated RPQ module instead of reusing `GssTikz` | The RPQ input graph is an NFA, not a graph-structured stack; GSS is a GLL/RNGLR artifact and must not be a dependency of RPQ (task 291) |
| Reciprocal pairs separated by the flpqdot router | The `dot layout` channel router separates reciprocal pairs into distinct channels (opposite sides) natively; the renderer emits no `bend` option (task 282, S2) |
| Labels registered in the measurement pre-pass | The router (Lua) cannot see edge labels; registering each non-loop label via `\flpqdotmeasureedgelabel` (and dropping the stock quote) lets the router place it at a collision-free position — the fix for the Kronecker-CFPQ step-5 label overlap (task 30, S1a) |
| Labels accepted as already-rendered TeX | The RPQ caller escapes edge labels once and formats vertex labels as `$v_i$`; a second escape would corrupt them |

## See Also

- [RPQ graph visualization](rpq-graph-viz.md) — the orchestrator that calls this renderer
- [RPQ graph DOT](rpq-graph-dot.md) — the DOT counterpart
- [Flpqdot TikZ](flpqdot-tikz.md) — the shared pre-pass and edge-emission helpers
- [Automaton TikZ](automaton-viz.md) — shared TikZ primitives (header/footer)
