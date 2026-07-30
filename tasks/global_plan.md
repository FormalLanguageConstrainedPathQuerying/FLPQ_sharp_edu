# Global Plan: RNGLR Refactoring (Tasks 216, 217, 218)

## Tasks

| ID | Title | Summary |
|----|-------|---------|
| 216 | RNGLR GSS visualization — edge symbols | Show grammar symbols on GSS edges in DOT instead of coordinate pairs |
| 217 | RNGLR Descriptor refactoring — explicit GssIdx + continuous GSS numbering | Add GssIdx to descriptor; lazy on-demand GSS vertices with sequential IDs |
| 218 | RNGLR steps — LR table with highlighted actions | Replace LR automaton DOT with highlighted LR table TeX in steps |

## Dependencies

- **217 depends on 216** — 216 establishes edge-symbol-aware visualization that 217 inherits and adapts for new vertex numbering.
- **218 is independent** of 216/217 — touches different fields of `RnglrParsingStep` and different parts of `RnglrStepVisualizer.fs`.

## Execution Order

1. **Task 216** — smallest, establishes GSS edge symbol infrastructure
2. **Task 218** — adds action highlighting to steps; minimal overlap with 216
3. **Task 217** — largest restructuring; builds on 216's visualization + 218's step fields

Reasoning: 216 is straightforward (add one field, update label printer). 218 is medium (3 fields, new table rendering). 217 is heavy (redesign GSS type, rewrite algorithm access patterns). Doing the lighter tasks first reduces conflict surface when 217 restructures.

## Overlapping Files

| File | 216 | 217 | 218 |
|------|-----|-----|-----|
| `RnglrTypes.fs` (RnglrParsingStep) | +ActiveGssEdgeSymbols | +GssIdx in descriptor | +ActiveShiftTerminals, +ActiveReduceNt, +LevelReductions |
| `Rnglr.fs` | populate edge symbols | use getOrCreateVertex/desc.GssIdx | capture shift/reduce actions |
| `RnglrStepVisualizer.fs` | edge label printer | descriptor 3 fields, vertex labels | LR table instead of automaton |
| `RnglrRunner.fs` | — | pass vertex info | — |
| `Helpers.fs` | — | — | lr_automaton.dot → lr_table.tex |
| `RnglrTableTeX.fs` | — | — | +tableToTeXWithHighlights |
| `GoldenHelpers.fs` | +rnglrEdgeLabelRegex | update regex | — |
| `RnglrStepVisualizationTests.fs` | edge label test | update for new formats | golden + table compile test |
| `data/RNGLR_step_template.tex` | — | — | replace STEP_LR_AUTOMATON_PDF |
| `SummaryTeX.fs` / `Summary.fs` | — | — | remove LR automaton PDF code |
| `GllTypes.fs` (GraphHelpers) | — | +collectActiveGssForDict | — |
| `docs/developer/rnglr.md` | — | update types/GSS docs | — |
| Golden data (6 files) | regenerate (edge symbols) | regenerate (numbering+descriptor) | regenerate (lr_table) |

## Reuse Analysis

- `RnglrGSS.outgoingEdges` — used in 216 to collect edge symbols from GSS
- `RnglrTableTeX.tableToTeX` — existing function; 218 adds variant with highlights
- `GraphHelpers.collectActiveGss` — existing for Matrix-based edges; 217 needs Dictionary variant
- `GssDot.toDotFromSets` — existing shared function; used by both GLL and RNGLR visualizers
- `PathIndexTeX.toTeXWithHighlights` — existing; unchanged in all three tasks
- `GridIndex.linearIndex` — 217 removes RNGLR dependency on this; PathIndex still uses it
- Golden test infrastructure (`GoldenHelpers.verifyGolden`, `ExternalTools.compileDotStringToInfo`) — reused in all three tasks
