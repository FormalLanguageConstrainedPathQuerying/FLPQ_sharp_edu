# Derivation Tree DOT Renderer

**Tags:** visualization, derivation-tree, dot, graphviz, tree, ll, lr
**Kind:** visualization
**Module:** DerivationTreeDot
**Source:** `src/FLPQ.Printers/`
**Depends on:** DerivationTree, VisualizationTypes
**Used by:** LLStepVisualizer, LRStepVisualizer

> **Abstract:** Renders derivation trees as Graphviz DOT graphs. Supports plain tree rendering (`toDot`), combined tree + LL stack overlay (`toDotWithLLStack`), and combined tree + LR stack overlay (`toDotWithLRStack`). Stack leaves are located by path and connected via dashed edges with same-rank constraints. LR state frames are visually distinguished with gray fill.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The module provides three rendering modes:
1. **Plain tree**: single derivation tree, leaf nodes as boxes, internal nodes as ovals, top-to-bottom layout.
2. **Tree + LL stack**: tree rendered once with path-to-node-id map; stack leaves located by path, connected via dashed edges, constrained to same rank.
3. **Tree + LR stack**: combined stack including LR state frames (gray fill, labeled "sN"), connected by dashed edges, same-rank constraint.

## Function Signatures

- `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string` — renders plain tree to DOT
- `toDotWithLLStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LLStackLeaf<'t,'nt> list -> string` — tree + LL stack chain overlay
- `toDotWithLRStack: (Symbol<'t,'nt> -> string) -> LRStackFrame<'t,'nt> list -> string` — tree + LR stack chain overlay with state frames

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Symbol visualizer callback | Flexible label rendering for any symbol type |
| Unique node IDs via mutable counter | Ensures no ID collisions in the generated DOT |
| Full tree as base for LL overlay | Tree rendered once; stack leaves located by path |
| LR state frames visually distinguished | Gray fill separates automaton states from tree symbols |

## See Also

- [Visualization Types](visualization-types.md) — LLStackLeaf, LRStackFrame types
- [DerivationTree module](derivation-tree.md) — tree types
- [Automaton visualization](automaton-viz.md) — automaton DOT/Tikz rendering
