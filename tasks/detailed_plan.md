# Detailed Plan: Task 133 — Simplify Valiant (and modified Valiant) algorithms

## Status: COMPLETED

## Summary
Replaced boolean decomposition-based Valiant implementation with a simpler set-based approach (direct set matrices, like CYK). Added forward/backward step visualization for both standard and modified Valiant. Added CLI support for modified Valiant.

## Changes Made

### 1. Valiant.fs — core algorithm rewrite
- Removed `BooleanDecomposition` dependency entirely
- Replaced `tByNt: Dictionary<Nonterminal, Matrix<bool>>` and `pByPair: Dictionary<BinaryPair, Matrix<bool>>` with a single `table: Matrix<Set<Nonterminal<'nt>>>`
- New functions: `setMult`, `mxmSet`, `writeSliceUnion`, `diffCells`, `snapshot`, `doMultiplications`
- `initValiant` now pre-fills diagonal cells with terminal rules (fix for modified Valiant)
- `complete`/`compute`: same recursive structure but on set-based table
- `completeLayerModified`/`completeVLayerModified`: same structure, set-based

### 2. Trace step types — forward/backward semantics
- `ValiantTraceStep<'nt>`: DU with `Forward(table * Submatrix)` and `Backward(table * Submatrix * (Submatrix*Submatrix) list * (int*int) list)`
- `ModifiedValiantTraceStep<'nt>`: DU with `LayerForward(table * int * Submatrix list)` and `LayerBackward(table * int * Submatrix list * (int*int) list)`

### 3. ValiantTeX.fs — new visualization
- `stepToTeX`: renders Forward steps (submatrix outlined in red + yellow highlights) and Backward steps (target in red, multiplied submatrices in colored blocks, changed cells in yellow)
- `modifiedStepToTeX`: renders LayerForward (colored blocks for layer submatrices) and LayerBackward (colored blocks + yellow highlights for changed cells)
- Removed `boolDecompToTeX`

### 4. ValiantRunner.fs
- Removed boolean decomposition output (no more `bool_decomp_*.tex` files)
- Added `runValiantModified` function

### 5. CLI changes
- `AlgorithmTypes.fs`: added `ValiantModified` case
- `Program.fs`: routed `ValiantModified` to `ValiantRunner.runValiantModified`
- `Summary.fs`: added `ValiantModified` to `TablePerStep` mapping
- `SummaryTeX.fs`: removed `bool_decomp_*.tex` handling from `tableStepSection`

### 6. Test updates
- `ValiantTests.fs`: updated tests to pattern-match on DU trace step types
- `ValiantRunnerTests.fs`: removed boolean decomposition check test
- `TexCompilationTests.fs`: updated Valiant test to extract table from DU
- `ValiantTraceGoldenTests.fs`: regenerated golden data files

### Verification
- All 356 FLPQ.Languages.Tests pass
- All 56 FLPQ.Cli.Tests pass
- All 67 FLPQ.Printers.Tests pass
- All Valiant-related golden tests pass
- Both `Valiant` and `ValiantModified` work via CLI
