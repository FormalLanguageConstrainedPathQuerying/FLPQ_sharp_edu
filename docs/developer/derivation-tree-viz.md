# Derivation Tree Dot Renderer

Module: `DerivationTreeDot` in `FLPQ.Printers`.

## Overview

Renders derivation trees as Graphviz DOT graphs. Supports plain tree rendering and combined stack+tree overlay rendering for LL/LR parser step visualization.

## Types

Uses `DerivationTree<'t,'nt>` from `DerivationTree.fs` and `LRStackFrame<'t,'nt>` from `VisualizationTypes.fs`. No new types.

## Functions

- `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string` — renders tree to DOT with leaf nodes as boxes, internal nodes as ovals, top-to-bottom layout.
- `toDotWithLLStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LLStackLeaf<'t,'nt> list -> string` — renders the full partial derivation tree with a stack chain overlay. The tree is rendered in a single pass with a path-to-node-id map. Stack leaves are located by their path and connected via dashed edges with a same-rank constraint.
- `toDotWithLRStack: (Symbol<'t,'nt> -> string) -> LRStackFrame<'t,'nt> list -> string` — renders a derivation tree with an LR stack chain including all stack frames. `LRSymbol` frames render as tree nodes; `LRState(n)` frames render as labeled "sN" nodes with gray fill. All frames are connected via dashed edges and constrained to the same rank.

## Design decisions

- Symbol visualizer callback for flexible label rendering
- Unique node ID generated via mutable counter
- `toDotWithLLStack` uses full tree as base: renders tree once, then overlays stack chain by locating frontier leaves via path
- LR state frames visually distinguished (gray fill) from symbol frames

Tests carry the `Graphviz` category (require `dot`). See [test categories](guides/test-categories.md).
