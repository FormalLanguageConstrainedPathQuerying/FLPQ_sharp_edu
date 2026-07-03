# Visualization Types

## Overview

Shared types for LL and LR parser step visualization, plus a shared TeX rendering module.

## Types

### `VisualizationStep` (struct)

Pre-rendered visualization output for a single parser step. Used by `LLStepVisualizer` and `LRStepVisualizer`.

- `treeAndStack: string` — Combined Graphviz DOT graph: the derivation tree with an overlay stack chain. Stack nodes are connected via dashed edges (top-to-bottom) and constrained to the same rank via `rank=same`.
- `input: string` — TeX (one-row pNiceMatrix) representation of the input with the current position underlined

### `StepInput<'t, 'nt>` (struct)

Input state for LL/LR parser step visualization.

- `tokens: Symbol<'t, 'nt> list` — all input tokens
- `position: int` — current position in the input

### `LLStackFrame<'t, 'nt>`

Frame on the unified LL parser stack. Two variants:

- `LLTree of DerivationTree<'t, 'nt>` — a tree node carrying the current frontier symbol (terminal, epsilon, or nonterminal leaf).
- `LLMarker of Nonterminal<'nt> * int` — marks the boundary of a nonterminal expansion with the expected number of children. When the marker reaches the top of the stack, `n` trees are popped from `completed` and combined into `Node(nt, children)`.

The unified stack represents the tree frontier with markers for nonterminal boundaries. During nonterminal expansion, RHS symbols are pushed as `LLTree(Leaf sym)` followed by `LLMarker(nt, rhsLength)`. When a marker reaches the top, the completed children are combined into a properly nested `Node`.

### `LLParsingStep<'t, 'nt>` (struct)

Data for a single LL parser visualization step. Collected during `LLParser.parseWithSteps`.

- `stack: LLStackFrame<'t, 'nt> list` — unified LL parser stack (tree frames + markers)
- `completed: DerivationTree<'t, 'nt> list` — roots of fully-built subtrees that have been removed from the stack
- `input: StepInput<'t>` — input state

### `LRParsingStep<'t, 'nt>` (struct)

Data for a single LR parser visualization step. Collected during `LRParser.parseWithSteps`.

- `tree: DerivationTree<'t, 'nt>` — partial derivation tree built so far
- `stack: LRStackFrame<'t, 'nt> list` — unified LR parser stack (interleaved states and symbols with trees)
- `input: StepInput<'t, 'nt>` — input state

### `LRStackFrame<'t, 'nt>` (struct)

Frame on the unified LR parser stack. Replaces the previous dual-stack (separate state and tree stacks). Tree nodes are symbols: roots of partial trees are placed in stack and used as symbols.

- `LRState of state: int` — an LR automaton state number
- `LRSymbol of tree: DerivationTree<'t,'nt>` — a derivation tree node; its root symbol serves as the grammar symbol

The unified stack alternates between states and tree nodes: `[LRState(n), LRSymbol(tree_k), ..., LRState(1), LRSymbol(tree_1), LRState(0)]` (cons-based, head = top). On shift: push `LRSymbol(Leaf(token))` then `LRState(nextState)`. On reduce: pop `2·|β|` frames, extract child trees from `LRSymbol` frames in RHS order, create `Node(lhs, children)`, push `LRSymbol(Node(...))` then `LRState(gotoState)`.

## Modules

### `TeXRenderer`

Shared TeX rendering helper for parser visualization.

- `inputRow: (Symbol<'t,'nt> -> string) -> Symbol<'t,'nt> list -> int -> string` — renders input tokens as a one-row `pNiceMatrix` with the token at the given position underlined via `\underbar`. Empty input renders as `\varepsilon`.

### `DerivationTreeDot`

DOT rendering for derivation trees and combined stack+tree visualization.

- `toDot: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> string` — renders a single derivation tree as a Graphviz DOT graph.
- `toDotWithLLStack: (Symbol<'t,'nt> -> string) -> LLStackFrame<'t,'nt> list -> DerivationTree<'t,'nt> list -> string` — renders a combined DOT graph showing both completed subtrees and the current LL stack. Completed subtrees are rendered as full trees inside a `cluster_completed` subgraph. Stack `LLTree` frames and `LLMarker` frames (rendered as gray boxes) are connected via dashed edges and constrained to the same rank via `rank=same`.
- `toDotWithLRStack: (Symbol<'t,'nt> -> string) -> LRStackFrame<'t,'nt> list -> string` — renders a derivation tree with an overlay LR stack chain including all stack frames. `LRSymbol` frames render as derivation tree nodes. `LRState` frames render as labeled "sN" nodes with gray fill. All frames are connected via dashed edges and constrained to the same rank.

### `LLStepVisualizer`

- `renderStep: (Symbol<'t,'nt> -> string) -> LLParsingStep<'t,'nt> -> VisualizationStep` — renders a single LL parsing step (raw F# data) to a `VisualizationStep` (DOT + TeX strings) using `DerivationTreeDot.toDotWithLLStack` and `TeXRenderer.inputRow`.
- `renderSteps: (Symbol<'t,'nt> -> string) -> LLParsingStep<'t,'nt> list -> VisualizationStep list` — renders a list of LL parsing steps.

### `LRStepVisualizer`

- `renderStep: (Symbol<'t,'nt> -> string) -> LRParsingStep<'t,'nt> -> VisualizationStep` — renders a single LR parsing step (raw F# data) to a `VisualizationStep` using `DerivationTreeDot.toDotWithLRStack` (includes state frames) and `TeXRenderer.inputRow`.
- `renderSteps: (Symbol<'t,'nt> -> string) -> LRParsingStep<'t,'nt> list -> VisualizationStep list` — renders a list of LR parsing steps.

## CYK Trace Step

Defined in `Cyk.fs`:

### `CykTraceStep<'nt>` (struct)

- `table: ParsingTable<'nt>` — snapshot of the working table at this step, where `ParsingTable<'nt> = Matrix<Set<Nonterminal<'nt>>>`
- `highlights: Matrix.Highlight list` — cells modified at this step (for yellow highlighting)

## Valiant Trace Step

Defined in `Valiant.fs`:

### `ValiantTraceStep<'nt>` (struct)

- `table: ParsingTable<'nt>` — recomposed matrix snapshot at this step, shared type with CYK

## Design Decisions

- **Separation of data collection and rendering**: `parseWithSteps` in LLParser/LRParser collects raw F# data (`LLParsingStep`/`LRParsingStep`). Rendering to TeX/DOT strings happens in `LLStepVisualizer`/`LRStepVisualizer` using shared `TeXRenderer` functions. CYK and Valiant trace functions similarly return structured data (`CykTraceStep`/`ValiantTraceStep`); TeX conversion happens at call sites.
- **Single combined DOT for stack+tree**: The LL and LR visualizers produce a single combined DOT graph (`treeAndStack`) instead of separate `tree.dot` and `stack.tex` files. The combined DOT shows the derivation tree with an overlay stack chain (dashed edges, `rank=same` constraint on stack nodes). Input visualization remains as TeX.
- **LL uses `toDotWithLLStack`**, **LR uses `toDotWithLRStack`**: LL visualizer renders both completed subtrees (inside a cluster) and the stack frontier (tree frames with dashed chain, markers as gray boxes). LR visualizer uses `DerivationTreeDot.toDotWithLRStack` which renders all `LRStackFrame` frames including `LRState` frames (labeled "sN" nodes with gray fill).
- **`TeXRenderer` is shared**: `inputRow` is identical for both parsers.
- **Struct types** for stack allocation efficiency on steps data.
- **Visualizers remain the public API** for consumers who want pre-rendered strings (CLI, tests). Consumers who need raw data can use `parseWithSteps`/`parseWithTrace` directly.
- **Unified LL stack with markers**: The LL parser uses a unified stack (`LLStackFrame`) with two variants: `LLTree` for frontier symbols and `LLMarker` for nonterminal boundaries. Markers enable proper hierarchical tree construction and track when subtrees are complete. Completed subtrees are captured in the step data and rendered alongside the stack.
- **Unified LR stack**: The LR parser uses a single unified stack (`LRStackFrame`) instead of two separate stacks (state + tree). The visualizer extracts state numbers from this unified stack for rendering.
