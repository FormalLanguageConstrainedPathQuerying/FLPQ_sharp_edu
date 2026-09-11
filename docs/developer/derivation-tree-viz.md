# Derivation Tree Visualization

**Tags:** visualization, derivation-tree, dot, tikz, graphviz, graphdrawing, tree, ll, lr
**Kind:** visualization
**Module:** DerivationTreeDot, DerivationTreeTikz
**Source:** `src/FLPQ.Printers/`
**Depends on:** DerivationTree, VisualizationTypes
**Used by:** LLStepVisualizer, LRStepVisualizer

> **Abstract:** Renders derivation trees in two output formats: Graphviz DOT (`DerivationTreeDot`) and Tikz with layered layout (`DerivationTreeTikz`). Supports plain tree rendering, combined tree + LL stack overlay, and combined tree + LR stack overlay. Stack frontier nodes are constrained to a single layer (DOT `{rank=same}`, Tikz `{ [same layer] ... }`) and connected via dashed chain edges that do not constrain the layout. LR state frames are visually distinguished with gray fill.

## Contents

- [Supported Formats](#supported-formats)
- [DerivationTreeDot Module](#derivationtreedot-module)
- [DerivationTreeTikz Module](#derivationtreetikz-module)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Supported Formats

| Format | Module | Features |
| --- | --- | --- |
| **DOT** | `DerivationTreeDot` | Plain tree, LL stack overlay, LR stack overlay via Graphviz |
| **Tikz** | `DerivationTreeTikz` | Same three modes with graphdrawing layered layout and native same-layer constraint |

## DerivationTreeDot Module

### Functions

- `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string` — renders plain tree to DOT
- `toDotWithLLStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LLStackLeaf<'t,'nt> list -> string` — tree + LL stack chain overlay
- `toDotWithLRStack: (Symbol<'t,'nt> -> string) -> LRStackFrame<'t,'nt> list -> string` — tree + LR stack chain overlay with state frames

### Visual Style

- Leaves: `shape=box`; nonterminals: default ellipse; top-to-bottom layout (`rankdir=TB`)
- Stack frontier: dashed edges with `constraint=false` (drawn, not layout-constraining) plus a `{rank=same; ...}` group
- LR state frames: `fillcolor=lightgray`, labeled `sN`

## DerivationTreeTikz Module

### Functions

- `toTikzWithLLStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LLStackLeaf<'t,'nt> list -> string` — tree + LL stack chain overlay
- `toTikzWithLRStack: (Symbol<'t,'nt> -> string) -> LRStackFrame<'t,'nt> list -> string` — tree + LR stack chain overlay with state frames

### Visual Style

- Layout: `layered layout, level sep=2cm, sibling sep=1.5cm` (default orientation is root-on-top; no `grow` option needed)
- Nodes: global `nodes={draw, shape=ellipse}`; leaves and LR state frames override with `rectangle`; LR state frames add `fill=lightgray`
- Labels: symbol visualizer output wrapped in math mode (`as={$S$}`, `as={$\varepsilon$}`); LR state labels are plain text (`as={s3}`)
- Stack frontier: dashed chain edges plus a `{ [same layer] n1, n2, ... };` collection

### Same-Layer Mechanism

The graphdrawing `same layer` collection is the native equivalent of DOT's `{rank=same; ...}`.
Nodes in the cluster are contracted before ranking; edges between two members of the same
cluster become self-loops that the layered algorithm removes before ranking and restores for
drawing — so the dashed chain is drawn but does not constrain the layout, exactly like
`constraint=false` in DOT. The frontier lands on a single row at depth = max path length to any
member (verified against `dot -Tplain` coordinate baselines).

### Chain Edge Direction

- **LL**: chain edges are emitted in the same order as the DOT renderer (stack-list order).
  The tree edges dominate ordering; the frontier renders left-to-right in natural tree order.
- **LR**: chain edges are emitted in **reversed** stack order (bottom frame → top frame).
  With DOT's direction, graphdrawing's crossing minimization scrambles the horizontal frame
  order; with the reversed direction the frames appear left-to-right in stack order, matching
  the DOT layout. The dashed arrows therefore point toward the stack top.

### Template

Uses `data/tex_tikz_template.tex`. The `ellipse` shape requires the `shapes.geometric` Tikz
library, which both `tex_tikz_template.tex` and `tex_summary_template.tex` load.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Symbol visualizer callback | Flexible label rendering for any symbol type |
| Unique node IDs via mutable counter | Ensures no ID collisions; DOT and Tikz renderers use the same pre-order numbering so goldens correspond 1:1 |
| Full tree as base for LL overlay | Tree rendered once; stack leaves located by path |
| LR state frames visually distinguished | Gray fill separates automaton states from tree symbols |
| Tikz `same layer` instead of manual positioning | Native same-rank constraint keeps the frontier on one row without fragile coordinate math |
| Reversed LR chain direction in Tikz | Required for correct left-to-right frame ordering under graphdrawing crossing minimization (see Chain Edge Direction) |
| Math-mode labels in Tikz | `\varepsilon` and other TeX symbol content only renders in math mode |

## See Also

- [Visualization Types](visualization-types.md) — LLStackLeaf, LRStackFrame types
- [DerivationTree module](derivation-tree.md) — tree types
- [Automaton visualization](automaton-viz.md) — automaton DOT/Tikz rendering
