# InputGraphTikz

**Tags:** visualization, tikz, graph, input
**Kind:** visualization
**Module:** InputGraphTikz
**Source:** `src/FLPQ.Printers/InputGraphTikz.fs`
**Depends on:** AutomatonTikz (header, footer, label escaping)
**Used by:** GllStepVisualizer, RnglrStepVisualizer

> **Abstract:** TikZ rendering for the GLL/RNGLR input graph. Converts a `Graph<int, Option<'t>>` (normally a linear path with one labeled edge per input position) to a `tikzpicture` using layered graphdrawing, with optional highlighting of the current input position. Non-loop edge labels are registered in the flpqdot measurement pre-pass and placed by the router (the stock quote is dropped).

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The input graph represents a sequence of terminal tokens as a directed path: vertices 0..n where edge i→i+1 carries the token at position i. Step visualizers render it next to the automaton and GSS figures so the current position is visible. This module is the TikZ counterpart of [InputGraphDot](InputGraphDot.md); the merged summary and step output select one of the two via the `--use-dot` flag.

## Supported Formats

- **TikZ** — a single `\graph [dot layout, ...]` opened by `AutomatonTikz.tikzHeader`, with one node per vertex and one edge per labeled pair. Every non-loop edge with a non-empty label is registered in the flpqdot measurement pre-pass and rendered without a stock quote (the router places the label); the input graph has one edge per pair, so the one-edge-per-pair rule ([FlpqdotTikz](flpqdot-tikz.md)) never excludes an edge.
- **Highlighting** — when `currentVertex` is specified, the vertex is filled with `lightgreen!20`.
- **Reciprocal edges** — because the renderer is generic over `Graph<int, Option<'t>>`, an edge whose reverse is also present is separated natively by the flpqdot channel router (via the `dot layout` key), so the pair does not draw as two overlapping lines; the renderer emits no `bend` option.

## Function Signatures

### `toTikz`

```fsharp
val toTikz:
    terminalPrinter: ('t -> string) ->
    inputGraph: Graph<int, Option<'t>> ->
    currentVertex: int option ->
    string
```

Renders the input graph as a TikZ `tikzpicture` string.

- `terminalPrinter` — function converting a terminal token to its string representation for edge labels.
- `inputGraph` — the input graph (normally a path over vertices 0..n; edges carry `Some token` or `None`).
- `currentVertex` — if `Some v`, vertex `v` is highlighted with `lightgreen!20`.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Reuses `AutomatonTikz` header/footer and `escapeLatex` | Keeps the `dot layout`, node style, and label escaping identical to the automaton renderers |
| `fill=lightgreen!20` for the current position | Matches the DOT renderer's current-position color and distinguishes it from the GSS/automaton fills |
| Reciprocal pairs separated by the layout engine | The generic input type could carry both directions of a pair; the flpqdot channel router (via `dot layout`) separates them into distinct channels natively, keeping the edge appearance consistent with the automaton, RSM, and GSS renderers (task 289) |
| Labels registered in the measurement pre-pass | The router (Lua) cannot see edge labels; registering each non-loop label via `\flpqdotmeasureedgelabel` (and dropping the stock quote) lets the router place it at a collision-free position (task 30, S1b) |

## See Also

- [InputGraphDot](InputGraphDot.md) — the DOT counterpart
- [GLL step visualization](gll-step-visualizer.md) — main consumer
- [Automaton visualization](automaton-viz.md) — shared dot layout and graph options
