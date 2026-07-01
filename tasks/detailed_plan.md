# Detailed Plan: Task 66 — Create separate project for printers

## Goal

Create a new `src/FLPQ.Printers` project that centralizes all TeX and Dot printing/visualization logic.
Algorithms continue to collect data as F# data structures; printers convert that data to output formats.

## Decisions

### What stays in original projects

1. **Matrix.fs** (LinearAlgebra): Keep `Highlight` and `SubmatrixBlock` types (they are data, not rendering). Move `toTeX`/`toTeXStyled` to Printers.
2. **VisualizationTypes.fs** (Languages): Keep struct types (`VisualizationStep`, `StepInput`, `LLParsingStep`, `LRStackFrame`, `LRParsingStep`). Move `TeXRenderer` module to Printers.
3. **Cyk.fs** (Languages): Keep algorithm + trace types. Move `cellToTeX`, `tableToTeX`, `tableToTeXStyled` to Printers.
4. **Valiant.fs** (Languages): Keep algorithm + trace types. Move `stepToTeX` to Printers.
5. **LLParser.fs** (Languages): Keep parsing + table building. Move `tableToTeX` + helpers to Printers.
6. **LRParser.fs** (Languages): Keep parsing + table building. Move `tableToTeX` + helpers to Printers.

### What moves to Printers (entire files)

7. `DerivationTreeVisualizer.fs` → `DerivationTreeDot.fs`
8. `AutomatonVisualizer.fs` → `AutomatonDot.fs`
9. `LLVisualizer.fs` → `LLStepVisualizer.fs`
10. `LRVisualizer.fs` → `LRStepVisualizer.fs`

### Dependency chain

```
FLPQ.Printers depends on:
  - FLPQ.LinearAlgebra (for Matrix, Highlight, SubmatrixBlock)
  - FLPQ.Languages (for Grammar, CykTraceStep, ValiantTraceStep, LLParsingStep, etc.)
```

New total dependency: FLPQ.Cli → FLPQ.Printers → FLPQ.Languages → FLPQ.LinearAlgebra

### Namespace

All printer modules use namespace `FLPQ.Printers`.

## Files to Create

### src/FLPQ.Printers/
| File | Contents | Source |
|------|----------|--------|
| `MatrixTeX.fs` | `toTeX`, `toTeXStyled` functions | From `Matrix.fs` |
| `TeXRenderer.fs` | `oneRowMatrix`, `inputRow` | From `VisualizationTypes.fs` |
| `DerivationTreeDot.fs` | `DerivationTreeVisualizer.toDot` | From `DerivationTreeVisualizer.fs` |
| `AutomatonDot.fs` | `AutomatonVisualizer.nfaToDot`, `dfaToDot` | From `AutomatonVisualizer.fs` |
| `CykTeX.fs` | `cellToTeX`, `tableToTeX`, `tableToTeXStyled` | From `Cyk.fs` |
| `ValiantTeX.fs` | `stepToTeX` | From `Valiant.fs` |
| `LLTableTeX.fs` | `tableToTeX` + helpers | From `LLParser.fs` |
| `LRTableTeX.fs` | `tableToTeX` + helpers | From `LRParser.fs` |
| `LLStepVisualizer.fs` | `LLVisualizer.visualizeSteps` | From `LLVisualizer.fs` |
| `LRStepVisualizer.fs` | `LRVisualizer.visualizeSteps` | From `LRVisualizer.fs` |

### tests/FLPQ.Printers.Tests/
| File | Contents | Source |
|------|----------|--------|
| `MatrixTeXTests.fs` | toTeX tests (4 tests) | From `MatrixTests.fs` |
| `AutomatonVisualizationTests.fs` | Dot compilation tests | From `AutomatonVisualizationTests.fs` |
| `DerivationTreeVisualizationTests.fs` | Dot compilation tests | From `DerivationTreeVisualizationTests.fs` |
| `LLVisualizerTests.fs` | Step viz tests | From `LLVisualizerTests.fs` |
| `LRVisualizerTests.fs` | Step viz tests | From `LRVisualizerTests.fs` |
| `TexCompilationTests.fs` | TeX compilation tests | From `TexCompilationTests.fs` |
| `TestUtils.fs` | Shared helpers (DotInfo, checkDotCompiles, checkTexCompiles) | Move from Languages.Tests |

## Files to Modify

### Source
- `Matrix.fs` — remove toTeX, toTeXStyled
- `VisualizationTypes.fs` — remove TeXRenderer module
- `Cyk.fs` — remove cellToTeX, tableToTeX, tableToTeXStyled
- `Valiant.fs` — remove stepToTeX
- `LLParser.fs` — remove tableToTeX, renderSet, renderRule, nonterminalsOf, terminalsOf
- `LRParser.fs` — remove tableToTeX, actionStr, actionCell, gotoCell, stateCount, allActionsFor
- `FLPQ.Languages.fsproj` — remove 4 visualizer files
- `FLPQ.Cli/Program.fs` — add `open FLPQ.Printers`, update references
- `FLPQ.Cli/FLPQ.Cli.fsproj` — add reference to FLPQ.Printers
- `FLPQ.slnx` — add src/FLPQ.Printers and tests/FLPQ.Printers.Tests

### Tests
- `FLPQ.LinearAlgebra.Tests/MatrixTests.fs` — remove toTeX tests (4 tests)
- `FLPQ.Languages.Tests/CykTests.fs` — update to not call Cyk.tableToTeX
- `FLPQ.Languages.Tests/FLPQ.Languages.Tests.fsproj` — remove 4 viz test files + TestUtils
- `FLPQ.LinearAlgebra.Tests/FLPQ.LinearAlgebra.Tests.fsproj` — add reference to FLPQ.Printers (for toTeX tests that stay... actually no, toTeX tests move to Printers.Tests)

### Docs
- `docs/main.md` — add FLPQ.Printers entry
- Create `docs/FLPQ.Printers.md` — overview of printer project

## Execution Order

1. Create feature branch `feature/066-printers-project`
2. Create `src/FLPQ.Printers/` project, add to solution
3. Create printer source files (all 10 files)
4. Update original source files (remove moved code)
5. Update Languages.fsproj
6. Update Cli (Program.fs + fsproj)
7. Create `tests/FLPQ.Printers.Tests/` project
8. Move/update test files
9. Build, fix errors, run tests
10. Format code
11. Update documentation
12. Merge to dev
