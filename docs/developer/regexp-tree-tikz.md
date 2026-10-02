# Regexp Tree TikZ

**Tags:** visualization, rpq, regexp, tikz
**Kind:** visualization
**Module:** RegexpTreeTikz
**Source:** `src/FLPQ.Printers/RegexpTreeTikz.fs`
**Depends on:** AutomatonTikz
**Used by:** ArroyueloStepCommon

> **Abstract:** TikZ graphdrawing rendering of the RPQ regexp AST tree for the Arroyuelo step visualizers. The tree is vertical (root at top, `grow'=down`), nodes are rectangles, and the current (handled) node is filled light blue. Dedicated to RPQ — it does not depend on the GSS renderers.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

`RegexpTreeTikz` emits a `tikzpicture` with a single layered `\graph` growing down. Labels are
taken as already-rendered TeX (the caller wraps the subexpression in `$...$`), so no escaping is
applied.

## Supported Formats

| Format | Output |
| --- | --- |
| TikZ | `\begin{tikzpicture} \graph [layered layout, nodes={draw, rectangle}, grow'=down, ...] { ... }; \end{tikzpicture}` |

## Function Signatures

### `toTikz: (int -> string) -> Set<int> -> Set<int * int> -> int -> string`

`toTikz label nodes edges current` renders the tree. `nodes` are the post-order node indices,
`edges` the parent -> child pairs, `current` the highlighted node (fill `lightblue!20`).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Dedicated RPQ module instead of reusing `GssTikz` | The regexp tree is an AST, not a graph-structured stack; GSS is a GLL/RNGLR artifact and must not be a dependency of RPQ (task 291) |
| Vertical layout (`grow'=down`) | Reads as a conventional tree (root above the leaves) beside the tall matrix stack (task 291) |
| Rectangle nodes | A tree node is a subexpression, not a state |
| Labels accepted as already-rendered TeX | The caller wraps the subexpression in `$...$`; escaping would corrupt the math |

## See Also

- [Regexp tree DOT](regexp-tree-dot.md) — the DOT counterpart
- [Arroyuelo step common](arroyuelo-step-common.md) — the caller
- [Automaton TikZ](automaton-viz.md) — shared TikZ primitives (header/footer, layered layout options)
