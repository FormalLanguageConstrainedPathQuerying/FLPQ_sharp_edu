# Derivation Tree Visualizer

## Overview

Visualizes derivation trees as Graphviz DOT graphs.

## Types

Uses `DerivationTree<'t,'nt>` from `DerivationTree.fs`. No new types.

## Functions

- `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string` — renders tree to DOT with leaf nodes as boxes, internal nodes as ovals, top-to-bottom layout

## Design decisions

- Symbol visualizer callback for flexible label rendering
- Unique node ID generated via mutable counter
