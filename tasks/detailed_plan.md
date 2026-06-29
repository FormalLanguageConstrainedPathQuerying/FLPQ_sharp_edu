# Detailed Plan: Task 48 — Refactoring of all parsing algorithms visualization

## Goal

Parsing algorithms must collect and return data for visualization as F# data structures. After collection, data is converted to TeX/DOT using shared standalone functions. LL and LR TeX conversion is shared.

## Current State Issues

1. `VisualizationStep` holds pre-rendered strings (`tree: string`, `stack: string`, `input: string`) — not composable data
2. LL and LR `parseWithSteps` duplicate TeX rendering logic (stack one-row matrix, input with position marker)
3. Valiant `parseWithTrace` pre-renders TeX during execution, returns `(Matrix * string) list`
4. CYK `parseWithTrace` returns tuple `(Matrix * Highlight list)` — no named type

## New Data Types

### In VisualizationTypes.fs (after existing types)

```fsharp
/// Input state for LL/LR parser step visualization.
[<Struct>]
type StepInput<'t, 'nt> =
    { tokens: Symbol<'t,'nt> list
      position: int }

/// Data for a single LL parser visualization step.
[<Struct>]
type LLParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t,'nt>
      stack: Symbol<'t,'nt> list
      input: StepInput<'t,'nt> }

/// Data for a single LR parser visualization step.
[<Struct>]
type LRParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t,'nt>
      stateStack: int list
      input: StepInput<'t,'nt> }

/// Shared TeX rendering helpers for parser visualization.
module TeXRenderer =
    /// Render a list of items as a one-row pNiceMatrix.
    val oneRowMatrix: ('a -> string) -> 'a list -> string

    /// Render input tokens with the current position underlined.
    val inputRow: (Symbol<'t,'nt> -> string) -> Symbol<'t,'nt> list -> int -> string
```

### In Cyk.fs

```fsharp
/// Data for a single CYK algorithm trace step.
[<Struct>]
type CykTraceStep<'t, 'nt> =
    { table: Matrix<CykCell<'t,'nt>>
      highlights: Matrix.Highlight list }
```

### In Valiant.fs

```fsharp
/// Data for a single Valiant algorithm trace step.
[<Struct>]
type ValiantTraceStep<'nt> =
    { table: Matrix<Set<Nonterminal<'nt>>> }
```

## Changes per File

### 1. VisualizationTypes.fs
- Add `StepInput<'t,'nt>` struct
- Add `LLParsingStep<'t,'nt>` struct
- Add `LRParsingStep<'t,'nt>` struct
- Add `TeXRenderer` module with `oneRowMatrix` and `inputRow`
- Keep existing `VisualizationStep` struct (Visualizers still return it)

### 2. Cyk.fs
- Add `CykTraceStep<'t,'nt>` struct
- Change `parseWithTrace` return type from `(Matrix<CykCell> * Highlight list) list` to `CykTraceStep list`
- Keep `tableToTeX` and `tableToTeXStyled` (pure rendering, already good)

### 3. Valiant.fs
- Add `ValiantTraceStep<'nt>` struct
- Change `parseWithTrace` to return `ValiantTraceStep list` instead of `(Matrix * string) list`
- Remove inline TeX rendering from `completeTrace`

### 4. LLParser.fs
- Remove `symbolVisualizer` parameter from `parseWithSteps`
- Remove inline TeX rendering (stack, input)
- Remove DOT rendering (tree) — just collect `DerivationTree`
- Return `LLParsingStep<'t,'nt> list` instead of `VisualizationStep list`
- Update `parse` (just discards steps as before)

### 5. LRParser.fs
- Remove `symbolVisualizer` parameter from `parseWithSteps`
- Remove inline TeX rendering (stack, input)
- Remove DOT rendering (tree) — just collect `DerivationTree`
- Return `LRParsingStep<'t,'nt> list` instead of `VisualizationStep list`
- Update `parse` (just discards steps as before)

### 6. LLVisualizer.fs
- Call `LLParser.parseWithSteps` (no symbolVisualizer)
- Render steps using `TeXRenderer` and `DerivationTreeVisualizer.toDot`
- Return `VisualizationStep list`

### 7. LRVisualizer.fs
- Call `LRParser.parseWithSteps` (no symbolVisualizer)
- Render steps using `TeXRenderer` and `DerivationTreeVisualizer.toDot`
- Return `VisualizationStep list`

### 8. Program.fs (CLI)
- CYK: Use `CykTraceStep` fields; call `Cyk.tableToTeX`/`tableToTeXStyled` as before
- Valiant: Use `ValiantTraceStep.table`; call `Matrix.toTeX` directly
- LL: Call `LLVisualizer.visualizeSteps` which now renders internally
- LR: Call `LRVisualizer.visualizeSteps` which now renders internally

### 9. Tests
- **LLVisualizerTests.fs**: Already uses Visualizers — should work unchanged (Visualizers still return VisualizationStep)
- **LRVisualizerTests.fs**: Same
- **CykTests.fs**: Update `parseWithTrace` destructuring from `(table, highlights)` to `step.table`, `step.highlights`
- **TexCompilationTests.fs**: Update same way
- **ValiantTests.fs**: Update `parseWithTrace` destructuring from `(_, tex)` to `step.table`

### 10. docs/visualization-types.md
- Update with new types and TeXRenderer module

## Dependencies

None. This is a standalone refactoring.
