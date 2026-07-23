# InputGraphDot

**Tags:** visualization, dot, graph, input
**Kind:** visualization
**Module:** InputGraphDot
**Source:** `src/FLPQ.Printers/InputGraphDot.fs`

> **Abstract:** DOT rendering for the GLL input graph. Converts a linear path graph (vertices 0..n with labeled edges representing terminals) to a Graphviz DOT digraph with optional highlighting of the current input position.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The input graph represents a linear sequence of terminal tokens as a directed path: vertices 0..n where edge i→i+1 carries token at position i. This module renders that graph to DOT format so it can be embedded in visualizations (summary PDF, step-by-step output).

## Supported Formats

- **DOT** — Graphviz digraph with `rankdir=LR`, circular vertex shapes, and labeled edges.
- **Highlighting** — When `currentVertex` is specified, the corresponding vertex is filled with green!30 to indicate the current input position.

## Function Signatures

### `toDot`

```fsharp
val toDot:
    terminalPrinter: ('t -> string) ->
    inputGraph: Graph<int, Option<'t>> ->
    currentVertex: int option ->
    string
```

Renders the input graph as a DOT digraph string.

- `terminalPrinter` — function to convert a terminal token to its string representation for edge labels.
- `inputGraph` — the input path graph (vertices 0..n, edges carry `Some token` or `None`).
- `currentVertex` — if `Some v`, vertex `v` is highlighted with green fill.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| `fillcolor=green!30` for current position | Distinguishes from current GSS node (lightblue) and new GSS vertices (lightyellow) |
| `shape=circle` for all vertices | Uniform appearance matching automaton visualization style |
| `rankdir=LR` | Left-to-right layout matches the natural reading order of input |

## See Also

- [GllStepVisualizer](gll-step-visualizer.md) — GLL step visualization that consumes this module
- [GSS Dot](gss-dot.md) — GSS DOT rendering following a similar pattern
- [RsmDot](rsm-dot.md) — RSM DOT rendering
