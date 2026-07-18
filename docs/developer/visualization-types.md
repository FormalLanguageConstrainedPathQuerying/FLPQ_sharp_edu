# Visualization Types

**Tags:** visualization, parsing, tex, dot, ll, lr, derivation-tree, stack
**Kind:** visualization
**Module:** VisualizationTypes
**Source:** `src/FLPQ.Languages/VisualizationTypes.fs`
**Depends on:** DerivationTree, Grammar
**Used by:** LLStepVisualizer, LRStepVisualizer, FLPQ.Cli

> **Abstract:** Shared types for LL and LR parser step visualization, plus a shared TeX rendering module. Defines `VisualizationStep` (pre-rendered DOT + TeX output for a single parsing step), `LLParsingStep`/`LRParsingStep` (raw F# data collected during parsing), `LLStackLeaf`/`LRStackFrame` (stack frame representations), and `TeXRenderer` (input row rendering). Follows the data-then-print pattern: parsers collect data, visualizers render it.

## Contents

- [Overview](#overview)
- [Data Types](#data-types)
- [Renderer Modules](#renderer-modules)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The visualization pipeline separates data collection from rendering:
1. LL/LR parsers collect raw F# data (`LLParsingStep`/`LRParsingStep`) during `parseWithSteps`.
2. `LLStepVisualizer`/`LRStepVisualizer` convert data to `VisualizationStep` (DOT + TeX strings).
3. CLI/test code writes the rendered strings to files.

CYK and Valiant similarly produce structured trace data (`CykTraceStep`/`ValiantTraceStep`); TeX conversion happens at call sites.

## Data Types

### `VisualizationStep` (struct)
Pre-rendered visualization output: `treeAndStack` (combined DOT graph) and `input` (TeX input row).

### `StepInput<'t, 'nt>` (struct)
Input state: `tokens` (all input symbols) and `position` (current index).

### `LLParsingStep<'t, 'nt>` (struct)
LL step data: immutable `tree` snapshot, `stack` leaf list with paths, and `input` state.

### `LLStackLeaf<'t, 'nt>` (struct)
Stack leaf with its path from root (`tree` + `path: int list`).

### `LRParsingStep<'t, 'nt>` (struct)
LR step data: partial `tree`, unified `stack` (LRStackFrame list), and `input` state.

### `LRStackFrame<'t, 'nt>` (struct)
Unified stack frame: `LRState of state: int` (automaton state) or `LRSymbol of tree: DerivationTree<'t,'nt>` (tree node). Stack alternates state/tree: `[LRState(n), LRSymbol(tree_k), ..., LRState(0)]`.

## Renderer Modules

### `TeXRenderer`
- `inputRow: (Symbol<'t,'nt> -> string) -> Symbol<'t,'nt> list -> int -> string` — renders input as one-row pNiceMatrix with current token underlined.

### `DerivationTreeDot`
- `toDot` — single tree to DOT
- `toDotWithLLStack` — full tree + LL stack chain overlay (dashed edges, same-rank)
- `toDotWithLRStack` — LR stack chain overlay with LR state frames (gray fill)

### `LLStepVisualizer`
- `renderStep` / `renderSteps` — convert `LLParsingStep` to `VisualizationStep`

### `LRStepVisualizer`
- `renderStep` / `renderSteps` — convert `LRParsingStep` to `VisualizationStep`

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Separation of data collection and rendering | Parsers collect raw data; visualizers render it. No formatting in algorithm code |
| Single combined DOT for stack+tree | LL/LR produce one DOT graph with tree + overlay stack chain |
| LL full tree as base | Tree rendered once; stack leaves located by path for dashed chain overlay |
| `TeXRenderer` is shared | `inputRow` identical for both parsers |
| Struct types | Stack allocation efficiency for step data |
| Unified LR stack | Single `LRStackFrame` replaces dual state+tree stacks |

## See Also

- [LL parser](ll-parser.md) — produces `LLParsingStep` data
- [LR parser](lr-parser.md) — produces `LRParsingStep` data
- [DerivationTree module](derivation-tree.md) — tree types
- [Derivation tree visualization](derivation-tree-viz.md) — DOT rendering
