# Regexp Tree DOT

**Tags:** visualization, rpq, regexp, dot
**Kind:** visualization
**Module:** RegexpTreeDot
**Source:** `src/FLPQ.Printers/RegexpTreeDot.fs`
**Depends on:** DerivationTreeDot (`escapeLabel`)
**Used by:** ArroyueloStepCommon

> **Abstract:** Graphviz DOT rendering of the RPQ regexp AST tree for the Arroyuelo step visualizers. The tree is vertical (`rankdir=TB`), nodes are rectangles, and the current (handled) node is filled light blue. Dedicated to RPQ — it does not depend on the GSS renderers.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

`RegexpTreeDot` renders one `digraph` from the AST node set, the parent -> child edge set, and
the current node index. Labels are supplied by the caller (the subexpression TeX) and escaped
for DOT.

## Supported Formats

| Format | Output |
| --- | --- |
| DOT | `digraph RegexpTree { rankdir=TB; ... }` — rectangle nodes `v_i`, current node `style=filled, fillcolor=lightblue` |

## Function Signatures

### `toDot: (int -> string) -> Set<int> -> Set<int * int> -> int -> string`

`toDot label nodes edges current` renders the tree. `nodes` are the post-order node indices,
`edges` the parent -> child pairs, `current` the highlighted node.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Dedicated RPQ module instead of reusing `GssDot` | The regexp tree is an AST, not a graph-structured stack; GSS is a GLL/RNGLR artifact and must not be a dependency of RPQ (task 291) |
| Vertical layout (`rankdir=TB`) | Matches the requested tree orientation and the TikZ counterpart |
| Rectangle nodes | A tree node is a subexpression, not a state; rectangles distinguish it from automaton/graph circles |

## See Also

- [Regexp tree TikZ](regexp-tree-tikz.md) — the TikZ counterpart
- [Arroyuelo step common](arroyuelo-step-common.md) — the caller
- [RPQ regexp visualization](rpq-regexp-viz.md) — the regexp formula rendering
