# InputGraphTikz

**Tags:** visualization, tikz, graph, input
**Kind:** visualization
**Module:** InputGraphTikz
**Source:** `src/FLPQ.Printers/InputGraphTikz.fs`
**Depends on:** AutomatonTikz (header, footer, label escaping, reciprocal-bend helper)
**Used by:** GllStepVisualizer, RnglrStepVisualizer

> **Abstract:** TikZ rendering for the GLL/RNGLR input graph. Converts a `Graph<int, Option<'t>>` (normally a linear path with one labeled edge per input position) to a `tikzpicture` using layered graphdrawing, with optional highlighting of the current input position.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The input graph represents a sequence of terminal tokens as a directed path: vertices 0..n where edge i→i+1 carries the token at position i. Step visualizers render it next to the automaton and GSS figures so the current position is visible. This module is the TikZ counterpart of [InputGraphDot](InputGraphDot.md); the merged summary and step output select one of the two via the `--use-dot` flag.

## Supported Formats

- **TikZ** — a single `\graph [layered layout, ...]` opened by `AutomatonTikz.tikzHeader`, with one node per vertex and one quoted edge label per edge.
- **Highlighting** — when `currentVertex` is specified, the vertex is filled with `lightgreen!20`.
- **Reciprocal edges** — because the renderer is generic over `Graph<int, Option<'t>>`, an edge whose reverse is also present is bent (`bend left=15`) so the pair does not draw as two overlapping straight lines; self-loops are never bent (shared rule `AutomatonTikz.reciprocalBendAttr`).

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
| Reuses `AutomatonTikz` header/footer and `escapeLatex` | Keeps the layered layout, node style, and label escaping identical to the automaton renderers |
| `fill=lightgreen!20` for the current position | Matches the DOT renderer's current-position color and distinguishes it from the GSS/automaton fills |
| Reciprocal pairs bent via the shared helper | The generic input type could carry both directions of a pair; the shared `reciprocalBendAttr` rule keeps the edge appearance consistent with the automaton, RSM, and GSS renderers (task 289) |

## See Also

- [InputGraphDot](InputGraphDot.md) — the DOT counterpart
- [GLL step visualization](gll-step-visualizer.md) — main consumer
- [Automaton visualization](automaton-viz.md) — shared layered layout and reciprocal-bend helper
