
# Global Plan: Tasks 125--127

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 125 | Move `VisualizationStep` to Printers, standardize trace-type locations | Refactor | None |
| 126 | Add XML doc comments to undocumented public APIs | Docs | 125 (VisualizationTypes changes) |
| 127 | Refactor `SummaryTeX.fs` from mutable to functional | Refactor | 125 (types motion), 126 (doc comments) |

## Dependencies Graph

```
Task 125 → Task 126, Task 127
Task 126 and 127 are independent of each other after 125.
```

## Execution Order

1. **Task 125** — Move types (structural, potentially breaking)
2. **Task 126** — Add XML doc comments
3. **Task 127** — Refactor SummaryTeX.fs

## Task 125: Move Visualization Types

### Changes
1. Move `LLStackLeaf`, `LLParsingStep` from `VisualizationTypes.fs` → `LLParser.fs`
2. Move `LRStackFrame`, `LRParsingStep` from `VisualizationTypes.fs` → `LRParser.fs`
3. Move `VisualizationStep` from `VisualizationTypes.fs` → new `FLPQ.Printers/VisualizationTypes.fs`
4. Keep `StepInput` in `FLPQ.Languages/VisualizationTypes.fs`
5. Delete `LRSymbolHelpers` module (unused)
6. Update all references:
   - `LLStepVisualizer.fs` — open FLPQ.Printers for VisualizationStep
   - `LRStepVisualizer.fs` — open FLPQ.Printers for VisualizationStep
   - `Helpers.fs` — already opens FLPQ.Printers, still works
   - `GoldenHelpers.fs` — add open FLPQ.Printers
   - `LRParser.fs` — add LRStackFrame, LRParsingStep definitions
   - `LLParser.fs` — add LLStackLeaf, LLParsingStep definitions
   - `DerivationTreeDot.fs` — already opens FLPQ.Languages, still works
7. Update `FLPQ.Printers.fsproj` — add `VisualizationTypes.fs` before LLStepVisualizer
8. Update `FLPQ.Languages.fsproj` — remove `VisualizationTypes.fs` from compile list (actually keep it since StepInput stays)

### Equivalence
- All existing tests must pass
- Compilation must succeed

## Task 126: XML Doc Comments

### Files to document
- `Matrix.fs` — rows, cols, get, set, create, init, ofArray2D, fold, map, map2, transpose + HighlightLabel, SubmatrixBlockLabel, Highlight, SubmatrixBlock types
- `Graph.fs` — vertexCount, vertices, tryGetVertex, getVertex, edge, mapVertices, mapEdges, fromEdges
- `Automaton.fs` Nfa module — collectAlphabet, buildMatrix, stateCount, alphabet, move, epsilonClosure, moveSet
- `Automaton.fs` Dfa module — stateCount, alphabet, move, isDeterministic
- `SummaryTeX.fs` — SummaryKind, wrapMath, wrapCenter, wrapTikzCenter, includePdf, section, readIfExists, collectSteps, headerSection, tableStepSection, stackStepSection, buildContent
- `RSM.fs` — blocks, blockOf, startBlock, nonterminals, terminals, startStates, stateCount

### Equivalence
- Compilation with GenerateDocumentationFile=true must succeed
- All existing tests must pass

## Task 127: Refactor SummaryTeX.fs

### Changes
Replace `let mutable lines = [] ; lines <- lines @ [...]` with functional patterns:

- `headerSection`: use `List.collect` / sequence expressions to build lines from option matches
- `tableStepSection`: use sequence expressions
- `stackStepSection`: use sequence expressions
- `buildContent`: use `List.collect` for step iteration

### Equivalence
- Golden tests for summary TeX must produce identical output
