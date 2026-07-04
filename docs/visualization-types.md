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

### `LLParsingStep<'t, 'nt>` (struct)

Data for a single LL parser visualization step. Collected during `LLParser.parseWithSteps`.

- `tree: DerivationTree<'t, 'nt>` — immutable snapshot of the full partial derivation tree at this step
- `stack: LLStackLeaf<'t, 'nt> list` — immutable snapshots of stack frontier nodes with their paths in the tree
- `input: StepInput<'t>` — input state

### `LLStackLeaf<'t, 'nt>` (struct)

A stack leaf node in an LL parsing step with its path from the tree root.

- `tree: DerivationTree<'t, 'nt>` — immutable snapshot of the leaf node
- `path: int list` — child indices from root to this leaf (e.g., `[0; 1]` means root → child[0] → child[1])

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
- `toDotWithLLStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LLStackLeaf<'t,'nt> list -> string` — renders the full partial derivation tree with a stack chain overlay. The tree is rendered first (solid edges). Stack leaves are located by their path in the tree and connected via dashed edges with a same-rank constraint, showing the current parsing frontier.
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
- **Single combined DOT for stack+tree**: The LL and LR visualizers produce a single combined DOT graph (`treeAndStack`). The combined DOT shows the derivation tree with an overlay stack chain (dashed edges, `rank=same` constraint on stack nodes). Input visualization remains as TeX.
- **LL uses the full tree as base**: `toDotWithLLStack` renders the full immutable tree snapshot with stack leaves identified by path and
connected via dashed edges. The tree is rendered in a single pass, with a path-to-node-id map used to locate stack frontier nodes for the dashed chain overlay.
- **`TeXRenderer` is shared**: `inputRow` is identical for both parsers.
- **Struct types** for stack allocation efficiency on steps data.
- **Visualizers remain the public API** for consumers who want pre-rendered strings (CLI, tests). Consumers who need raw data can use `parseWithSteps`/`parseWithTrace` directly.
- **Unified LL stack with mutable tree nodes**: The LL parser uses mutable tree nodes on the stack. When a nonterminal is expanded, the existing node gets its children set in-place, and the children become the new stack. Step snapshots capture the immutable state at each moment. No separate marker frames or completed list needed.
- **Unified LR stack**: The LR parser uses a single unified stack (`LRStackFrame`) instead of two separate stacks (state + tree). The visualizer extracts state numbers from this unified stack for rendering.
