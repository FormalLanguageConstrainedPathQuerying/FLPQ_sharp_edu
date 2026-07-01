# Visualization Types

## Overview

Shared types for LL and LR parser step visualization, plus a shared TeX rendering module.

## Types

### `VisualizationStep` (struct)

Pre-rendered visualization output for a single parser step. Used by `LLVisualizer` and `LRVisualizer`.

- `tree: string` — Graphviz DOT representation of the derivation tree at this step
- `stack: string` — TeX (one-row pNiceMatrix) representation of the parser stack
- `input: string` — TeX (one-row pNiceMatrix) representation of the input with current position underlined

### `StepInput<'t, 'nt>` (struct)

Input state for LL/LR parser step visualization.

- `tokens: Symbol<'t, 'nt> list` — all input tokens
- `position: int` — current position in the input

### `LLStackFrame<'t, 'nt>` (struct)

Frame on the unified LL parser stack. Replaces the previous dual-stack (separate symbol and tree stacks). Tree nodes are symbols: current leaves of the partial tree are placed in stack and used as symbols.

- `LLFrame of Symbol<'t,'nt> * DerivationTree<'t,'nt>` — a grammar symbol with its associated tree node

The unified stack represents the tree frontier: `[LLFrame(sym_k, tree_k), ..., LLFrame(sym_1, tree_1)]` (cons-based, head = top). Terminal match pops the frame and adds its tree to completed subtrees. Nonterminal expansion pops the frame and pushes RHS symbols as new `LLFrame` entries. The visualizer extracts symbols from each frame for stack display.

### `LLParsingStep<'t, 'nt>` (struct)

Data for a single LL parser visualization step. Collected during `LLParser.parseWithSteps`.

- `tree: DerivationTree<'t, 'nt>` — partial derivation tree built so far
- `stack: LLStackFrame<'t, 'nt> list` — unified LL parser stack (symbols with tree nodes)
- `input: StepInput<'t, 'nt>` — input state

### `LRParsingStep<'t, 'nt>` (struct)

Data for a single LR parser visualization step. Collected during `LRParser.parseWithSteps`.

- `tree: DerivationTree<'t, 'nt>` — partial derivation tree built so far
- `stack: LRStackFrame<'t, 'nt> list` — unified LR parser stack (interleaved states and symbols with trees)
- `input: StepInput<'t, 'nt>` — input state

### `LRStackFrame<'t, 'nt>` (struct)

Frame on the unified LR parser stack. Replaces the previous dual-stack (separate state and tree stacks).

- `LRState of int` — an LR automaton state number
- `LRSymbol of Symbol<'t,'nt> * DerivationTree<'t,'nt>` — a grammar symbol with its associated parse tree

The unified stack alternates between states and symbols: `[LRState(n), LRSymbol(X_k, t_k), ..., LRState(1), LRSymbol(X_1, t_1), LRState(0)]` (cons-based, head = top). Shift pushes `LRSymbol` then `LRState`. Reduce pops 2·|β| frames, extracts child trees in RHS order.

## Modules

### `TeXRenderer`

Shared TeX rendering helpers for parser visualization.

- `oneRowMatrix: ('a -> string) -> 'a list -> string` — renders a list of items as a one-row `pNiceMatrix` with `margin=2pt`. Empty list renders as `\varepsilon`. Used for both LL symbol stack and LR state stack.
- `inputRow: (Symbol<'t,'nt> -> string) -> Symbol<'t,'nt> list -> int -> string` — renders input tokens as a one-row `pNiceMatrix` with the token at the given position underlined via `\underbar`. Empty input renders as `\varepsilon`.

### `LLVisualizer`

- `visualizeSteps: (Symbol<'t,'nt> -> string) -> Grammar<'t,'nt> -> Map<...> -> int -> Symbol<'t,'nt> list -> VisualizationStep list` — runs `LLParser.parseWithSteps` and renders the collected data to `VisualizationStep` using `DerivationTreeVisualizer.toDot` and `TeXRenderer`.

### `LRVisualizer`

- `visualizeSteps: (Symbol<'t,'nt> -> string) -> Grammar<'t,'nt> -> LRTable<'t,'nt> -> Symbol<'t,'nt> list -> VisualizationStep list` — runs `LRParser.parseWithSteps` and renders the collected data to `VisualizationStep`.

## CYK Trace Step

Defined in `Cyk.fs`:

### `CykTraceStep<'t, 'nt>` (struct)

- `table: Matrix<CykCell<'t,'nt>>` — snapshot of the working table at this step
- `highlights: Matrix.Highlight list` — cells modified at this step (for yellow highlighting)

## Valiant Trace Step

Defined in `Valiant.fs`:

### `ValiantTraceStep<'nt>` (struct)

- `table: Matrix<Set<Nonterminal<'nt>>>` — recomposed matrix snapshot at this step

## Design Decisions

- **Separation of data collection and rendering**: `parseWithSteps` in LLParser/LRParser collects raw F# data (`LLParsingStep`/`LRParsingStep`). Rendering to TeX/DOT strings happens in `LLVisualizer`/`LRVisualizer` using shared `TeXRenderer` functions. CYK and Valiant trace functions similarly return structured data (`CykTraceStep`/`ValiantTraceStep`); TeX conversion happens at call sites.
- **`TeXRenderer` is shared**: `oneRowMatrix` handles both LL symbol stacks and LR state stacks (parametrized by item printer). `inputRow` is identical for both parsers.
- **Struct types** for stack allocation efficiency on steps data.
- **Visualizers remain the public API** for consumers who want pre-rendered strings (CLI, tests). Consumers who need raw data can use `parseWithSteps`/`parseWithTrace` directly.
- **Unified LL stack**: The LL parser uses a single unified stack (`LLStackFrame`) instead of two separate stacks (symbol + tree). Each frame carries both a symbol for parsing and a tree node representing the symbol in the derivation tree. The stack is the tree frontier.
- **Unified LR stack**: The LR parser uses a single unified stack (`LRStackFrame`) instead of two separate stacks (state + tree). The visualizer extracts state numbers from this unified stack for rendering.
