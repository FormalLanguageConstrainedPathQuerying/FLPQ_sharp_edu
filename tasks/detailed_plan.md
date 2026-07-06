# Detailed Plan: Task 134 — Improve Valiant visualization

## Status: COMPLETED

## Summary
Simplified Valiant trace to only record `doMultiplications` steps. Removed `Forward` trace steps (decomposition transitions and size-1 terminal submatrix processing). Changed `ValiantTraceStep` from a DU (`Forward | Backward`) to a record type since only one case remains. Each `doMultiplications` call now always emits a trace step (removed the `changedCells` guard).

Fixed a coordinate system bug: trace steps previously captured an `n × n` snapshot (columns shifted by −1 via `snapshot`) but the TeX rendering also applied −1 column offsets to compensate. This double-compensation caused blocks to render at wrong positions for certain steps (5, 6, 7, 19, 20, 21, 25, 26, 27). Now uses `copyFullTable` (full `tableSize × tableSize`) for trace steps with correct coordinates, and removed all −1 column offset workarounds from the TeX rendering.

Added fill colors to all submatrix blocks (`fill=red!10` for target, `fill=<color>!20` for multiplied submatrices) to make three submatrices clearly visible, not just bordered.

## Changes Made (original)

### 1. Valiant.fs — trace type simplification and algorithm changes
- Changed `ValiantTraceStep<'nt>` from DU to record:
  ```fsharp
  [<Struct>]
  type ValiantTraceStep<'nt when 'nt: comparison> =
      { table: ParsingTable<'nt>
        target: Submatrix
        multiplied: (Submatrix * Submatrix) list
        changedCells: (int * int) list }
  ```
- Removed `Forward` emissions from `complete` (size-1 branch and post-recursion branch)
- Changed `doMultiplications` to always emit a trace step (removed `if not (List.isEmpty changed) then` guard)
- Changed `parseWithTable` to compute directly (no longer depends on `parseWithTrace`), enabling correct results even when no multiplications occur (short inputs)
- `parseModifiedWithTable` unchanged (modified Valiant always produces trace steps via fallback)

### 2. ValiantTeX.fs — simplified rendering
- Removed `Forward` match case from `stepToTeX` — now only handles the record type
- All three submatrices (target, m1, m2) are visualized: target in red (`CurrentStepSubmatrix`), m1/m2 in colored blocks (`Submatrix idx`)

### 3. Test updates
- `TexCompilationTests.fs`: changed input from "a a" to "a a a a" to trigger `doMultiplications`
- `ValiantTraceGoldenTests.fs`: changed test input from "a b" to "a b a b" to trigger `doMultiplications`, renamed golden file to `valiant_grammar1_abab.tex`
- Removed old golden file `valiant_grammar1_ab.tex`

### 4. Documentation
- Updated `docs/valiant.md`: `ValiantTraceStep` type, `parseWithTable`/`parseWithTrace` descriptions, design decisions

### Verification
- All 586 tests pass across all 6 test projects

### 5. Coordinate system fix (finalization)
- **Root cause**: trace steps used `snapshot table init.n` which created an `n × n` matrix with columns shifted by −1 (`rj + 1`). Meanwhile, `ValiantTeX.stepToTeX` applied its own `−1` column offset to compensate. This double‑compensation caused block coordinates to be wrong when submatrix corners fell near table boundaries, making blocks disappear in certain steps (5, 6, 7, 19, 20, 21, 25, 26, 27).
- **Fix**: added `copyFullTable` function that copies the full `tableSize × tableSize` matrix without coordinate transformation. Trace steps now store `copyFullTable table init.tableSize` instead of `snapshot table init.n`.
- Removed all `−1` column offset workarounds from `ValiantTeX.stepToTeX`: `cj = j` (was `j − 1`), `startCol = step.target.col` (was `col − 1`), `endCol = step.target.col + step.target.Size − 1` (was `Size − 2`), `sc1 = max 0 m1.col` (was `col − 1`), `sc2 = max 0 m2.col` (was `col − 1`).
- Changed multiplied block labels from `idx` (which caused block deduplication when multiple blocks shared positions) to `idx * 2 + 1` and `idx * 2 + 2` for unique color indices.

### 6. Fill color enhancement
- Added `fill=red!10` to `CurrentStepSubmatrix` blocks and `fill=<color>!20` to other submatrix blocks in `MatrixTeX.toTeXStyled`.
- Makes all three submatrices clearly visible as filled colored rectangles, not just thin‑bordered cells.
- Added `blockFillColor` helper.

### Final Verification
- All 589 tests pass across all 6 test projects.

---

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
