# Flpqdot TikZ

**Tags:** visualization, tikz, graph, flpqdot, label
**Kind:** visualization
**Module:** FlpqdotTikz
**Source:** `src/FLPQ.Printers/FlpqdotTikz.fs`
**Depends on:** (none — pure string emission)
**Used by:** RpqGraphTikz, AutomatonTikz, GssTikz, RsmTikz, InputGraphTikz (the `dot layout` renderers with edge labels)

> **Abstract:** Shared emission helpers for the flpqdot `dot layout` renderers: the
> `\flpqdotmeasureedgelabel` measurement pre-pass (one command per labeled edge, emitted
> before the `\graph`) and the edge line that applies the label-adoption rule — a non-loop
> edge with a non-empty label that is its pair's only edge registers its label in the
> pre-pass and drops the stock quote (the router's edge-callback wrapper places the label at
> a collision-free position); a loop edge or an edge sharing its pair with a parallel edge
> keeps its stock quote (the router skips loops and cannot tell parallel edges apart).

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The flpqdot channel router is label-aware only when the renderer tells it what each edge's
label is: the router (Lua) cannot see edge labels — they live on the TeX side. `FlpqdotTikz`
is the single owner of that hand-off: it builds the measurement pre-pass (one
`\flpqdotmeasureedgelabel{<key>}{<label>}` command per labeled edge, emitted between
`\begin{tikzpicture}` and `\graph` by the shared header) and the edge line that pairs with
it (a registered non-loop edge drops its stock quote, so the router's `EdgeLabel.sty`
wrapper places the label; a loop edge keeps its quote, since the router skips loops).

### The one-edge-per-pair rule

An edge may be registered only when its `tail->head` key is produced by **exactly one
edge**. The router's label channel is keyed by `tail->head`, and the edge callback
(`\flpqdotplacelabel` in `EdgeLabel.sty`) receives only the two node names — it cannot tell
parallel edges with the same key apart, so registering such a key would place the same
label on every edge of the pair. (Reciprocal pairs are unaffected: `u->v` and `v->u` are
distinct keys.) Each renderer therefore decides per key which of its edges are unique:

| Renderer | Parallel edges arise from | Unique (registered) |
| --- | --- | --- |
| `RpqGraphTikz`, `GssTikz`, `InputGraphTikz` | never (one edge per pair) | every non-loop edge with a non-empty label |
| `AutomatonTikz` | a transition cell holding both terminal and epsilon labels (a solid + a dotted edge) | non-loop cells with terminal XOR epsilon labels |
| `RsmTikz` | a transition cell holding several symbols (one edge per symbol) | non-loop single-symbol cells |

All other edges (loops, non-unique pairs) keep their stock quote and render exactly as
before adoption.

## Function Signatures

### `edgeKey: string -> string -> string`

The syntactic edge key the router uses to look up a measured label: `tail->head` as written
in the graph body.

### `measureCommand: string -> string -> string`

One measurement pre-pass command: `\flpqdotmeasureedgelabel{<key>}{<label>}`.

### `prePass: (string * string) list -> string`

The measurement pre-pass for a set of labeled edges: one command per (key, label),
newline-joined at the graph-body indentation. `""` when there are no labeled edges.

### `emitEdge: StringBuilder -> string -> string -> string -> string -> unit`

One edge line for a `dot layout` graph body, applying the label-adoption rule. `style`
holds the edge's style options (e.g. `red, thick`); a non-loop edge with a non-empty label
drops the stock quote (its label is registered in the pre-pass and placed by the router's
edge-callback wrapper), a loop edge keeps it (the router skips loops).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Single owner of the pre-pass and the adoption rule | The `tail->head` key convention and the quote-drop rule must match the router's lookups (`LabelMeasure.channelSizeEither`, the `EdgeLabel.sty` callback) exactly; one module keeps them in sync |
| Syntactic `tail->head` key | The layered layout may reverse a back-edge during cycle removal; the router's lookups tolerate the reversal, so the key need not match the model's direction |
| Loop edges keep the stock quote | The router skips loops (no channel, no placement); the stock quote is the only label path for them |
| One-edge-per-pair rule | The edge callback sees only node names, so a key with parallel edges would get the same label on all of them; non-unique keys keep the stock quote (see the rule above) |
| Style options survive the quote drop | A highlighted/path edge keeps `red, thick` / `red!40` in the option list; only the quoted label moves to the router |

## See Also

- [flpqdot graph-drawing library](tikz-gd-library.md) — the router that consumes the pre-pass
- [RPQ graph TikZ](rpq-graph-tikz.md) — the first adopting renderer
- [Automaton TikZ](automaton-viz.md) — the shared header that emits the pre-pass
