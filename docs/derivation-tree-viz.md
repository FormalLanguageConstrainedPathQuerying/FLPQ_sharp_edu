# Derivation Tree Dot Renderer

Module: `DerivationTreeDot` in `FLPQ.Printers`.

## Overview

Renders derivation trees as Graphviz DOT graphs. Supports plain tree rendering and combined stack+tree overlay rendering for LL/LR parser step visualization.

## Types

Uses `DerivationTree<'t,'nt>` from `DerivationTree.fs` and `LRStackFrame<'t,'nt>` from `VisualizationTypes.fs`. No new types.

## Functions

- `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string` — renders tree to DOT with leaf nodes as boxes, internal nodes as ovals, top-to-bottom layout.
- `toDotWithStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> DerivationTree<'t,'nt> list -> string` — renders a derivation tree with an overlay stack chain. `stackTrees` are tree nodes from stack frames in top-to-bottom order. Matching tree nodes are connected via dashed edges and constrained to the same rank via `rank=same`.
- `toDotWithLRStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LRStackFrame<'t,'nt> list -> string` — renders a derivation tree with an LR stack chain including all stack frames. `LRSymbol` frames render as tree nodes; `LRState(n)` frames render as labeled "sN" nodes with gray fill. All frames are connected via dashed edges and constrained to the same rank.

## Design decisions

- Symbol visualizer callback for flexible label rendering
- Unique node ID generated via mutable counter
- LR state frames visually distinguished (gray fill) from symbol frames
- Shared `toDotWithStack` used by LL visualizer; `toDotWithLRStack` used by LR visualizer
