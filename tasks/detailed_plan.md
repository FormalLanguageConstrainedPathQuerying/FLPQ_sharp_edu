# Task 257: Fix modified Valiant visualization (power-of-two grid, layered trace)

## Reuse Analysis

- Reuse `copyFullTable` (`Valiant.fs:211`) for full padded trace tables (classical Valiant already uses it at `Valiant.fs:270`).
- Reuse the "new entries appeared" predicate already used by `diffCells` (`Valiant.fs:214-227`) for the new full-table diff helper `diffWholeTable`.
- Reuse `constructLayer` for layers `1..maxLayer`; init blocks are the diagonal terminal cells (`i, i+1` for `i < n`).
- Reuse `Matrix.CurrentStepSubmatrix` label (→ `red!10`) for the single light-red layer blocks; reuse `Matrix.CurrentCell` (→ yellow) for changed cells. No changes to `Matrix.fs` or `MatrixTeX.fs`.
- Reuse golden-test infrastructure (`GoldenHelpers.verifyGolden`, `CREATE_GOLDEN_FILES=1`).

---

### S1: Restructure modified Valiant trace + fix rendering + regenerate golden

**Code:** `src/FLPQ.Languages/Valiant.fs` (`parseModifiedWithSppfTrace`, new `diffWholeTable` helper), `src/FLPQ.Printers/ValiantTeX.fs` (`sppfModifiedStepToTeX`, `sppfModifiedStepToTeXAsNt`)
**Tests:** regenerate `tests/FLPQ.Printers.Tests/GoldenData/valiant_modified_grammar1_ab.tex` (existing `ValiantTraceGoldenTests.fs` golden)
**Docs:** `docs/developer/valiant.md` (trace step structure + color scheme)

**Spec:**
- `Valiant.fs`:
  - Add `let private diffWholeTable (before: SppfParsingTable<'nt>) (after: SppfParsingTable<'nt>) (tableSize: int) : (int * int) list` returning global `(i,j)` where `Set.difference after.[i,j] before.[i,j]` is non-empty.
  - Rewrite `parseModifiedWithSppfTrace` (lines 565-597):
    - After `initValiant`, emit an init step: `LayerForwardSppf(copyFullTable table tableSize, 1, [for i in 0..n-1 -> {Row=i; Col=i+1; Size=1}])`.
    - Loop `layer in 1..maxLayer`; for each non-empty `constructLayer layer tableSize`: snapshot `before = copyFullTable table tableSize`, run `completeLayerModified init table layerSubmatrices`, compute `changed = diffWholeTable before table tableSize`, emit `LayerBackwardSppf(copyFullTable table tableSize, 1 <<< layer, layerSubmatrices, changed)`.
    - Replace all `snapshot table n` in the trace path with `copyFullTable table tableSize`.
    - Do NOT change `parseModifiedWithSppfInfo`/`parseModifiedWithSppfTable` (they keep `snapshot table n`).
- `ValiantTeX.fs` (`sppfModifiedStepToTeX` 81-178, `sppfModifiedStepToTeXAsNt` 253-350):
  - Remove column shift: `startCol = m.Col - 1` → `m.Col`; `endCol = m.Col + m.Size - 2` → `m.Col + m.Size - 1`.
  - Layer blocks: `Label = Matrix.CurrentStepSubmatrix` (single light-red `red!10`) instead of `Matrix.Submatrix idx`; drop the `mapi` index.
  - LayerBackward highlight: `cj = j - 1` → `cj = j`.
- Regenerate golden `valiant_modified_grammar1_ab.tex` (now: init step + layer-1 step on a `4×4` grid, `red!10` blocks + yellow changed cells).

---

### S2: Add regression property tests for modified Valiant trace structure

**Code:** (none — tests only)
**Tests:** `tests/FLPQ.Languages.Tests/ValiantTests.fs` — new module `ModifiedValiantTraceStructureTests`
**Docs:** none (behavior documented in S1)

**Spec:**
- Property test (`[<Property>]`, reuse `GenToArbitrary.AbString` / Dyck1 grammar): for a non-empty input, every modified Valiant SPPF trace step table is power-of-two aligned — `Matrix.rows = Matrix.cols = nextPowerOfTwo(n+1)` — and equals the classical Valiant trace table dimension for the same input.
- Property test: the trace step sequence has exactly one step per layer — first step is `LayerForwardSppf` with `layerSize = 1` and 1×1 diagonal blocks (`Row = Col - 1`, `Size = 1`); subsequent steps are `LayerBackwardSppf` with `layerSize = 2, 4, …` and no duplicate layer size.
- Property test: for a non-trivial input (length ≥ 2), at least one `LayerBackwardSppf` step has non-empty `ChangedCells`.
