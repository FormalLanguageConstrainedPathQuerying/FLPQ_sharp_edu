# Task 253: Improve CYK and Valiant (+ modified) and its visualization

## Reuse Analysis

- **Existing `SppfParsingEntry` and `SppfParsingTable` types** (`ParsingTable.fs`) — already the canonical SPPF cell types, reused as-is
- **Existing `ParsingTableTeX.ntCellToTeX`** (`ParsingTableTeX.fs:28`) — renders `Set<Nonterminal>` → reused for `-no-sppf-table` flag rendering
- **Existing `ParsingTableTeX.sppfEntryCellToTeX`** (`ParsingTableTeX.fs:35`) — renders `Set<SppfParsingEntry>` → reused as-is
- **Existing SPPF computation paths** in `Cyk.fs` and `Valiant.fs` become the canonical internal paths
- **Existing CLI arg parsing** (`AlgorithmTypes.fs`) — extend with new flag
- **Existing golden test infrastructure** (`GoldenHelpers.fs`) — reuse for new golden tests
- **`TestHelpers.checkCykValiantEquivalence`** — currently tests both SPPF and non-SPPF table equality. After unification, the separate SPPF-vs-non-SPPF check becomes redundant (they use same data). Consolidate.

---

### S1: Unify CYK — remove non-SPPF computation path, reimplement public API as wrappers

**Code:** `src/FLPQ.Languages/Cyk.fs`
**Tests:** `tests/FLPQ.Languages.Tests/CykTests.fs` (no changes needed — public API preserved)
**Docs:** `docs/developer/FLPQ.Languages.md` (update CYK module docs)

**Spec:**
- Remove private non-SPPF functions: `findTerminalRules`, `findBinaryProductions`, `computeCell`, `cykCore`, `cykTable`, `tableTrace`, `isAccepted`
- Keep private SPPF functions: `findTerminalRulesWithProdIdx`, `findBinaryProductionsWithProdIdx`, `computeCellSppf`, `cykSppfCore`, `isSppfAccepted`
- Add private helper `sppfTableToNtTable : SppfParsingTable<'nt> -> ParsingTable<'nt>` — extracts only nonterminals from SPPF cells
- Add private helper `sppfTraceStepToNtTraceStep : CykSppfTraceStep<'nt> -> CykTraceStep<'nt>` — converts trace step
- Reimplement `parse`: call `parseWithSppfTable`, return `snd`
- Reimplement `parseWithTable`: call `parseWithSppfTable`, convert table via `sppfTableToNtTable`, return `(ntTable, accepted)`
- Reimplement `parseWithTrace`: call `parseWithSppfTrace`, convert each step via `sppfTraceStepToNtTraceStep`
- `parseWithSppfInfo`, `parseWithSppfTable`, `parseWithSppfTrace` unchanged
- `isSppfAccepted` renamed to `isAccepted` (the old `isAccepted` is removed)
- Equivalence: all existing CYK tests must pass. Tables produced by `parseWithTable` must be identical to pre-refactoring tables

---

### S2: Unify Valiant — remove non-SPPF computation path, reimplement public API as wrappers

**Code:** `src/FLPQ.Languages/Valiant.fs`
**Tests:** `tests/FLPQ.Languages.Tests/ValiantTests.fs` (no changes needed)
**Docs:** `docs/developer/FLPQ.Languages.md` (update Valiant module docs)

**Spec:**
- Remove non-SPPF private functions: `InitData`, `mxmSet`, `writeSliceUnion`, `copyFullTable`, `diffCells`, `extractSlice`, `doMultiplications`, `complete`, `compute`, `completeLayerModified`, `completeVLayerModified`, `terminalRulesFromGrammar`, `binaryRulesFromGrammar`, `initValiant`
- Remove non-SPPF private helpers: `setMult`, `snapshot` (these are only used by non-SPPF path)
- Keep all SPPF private functions: `InitDataSppf`, `mxmSetSppf`, `writeSliceUnionSppf`, `copyFullTableSppf`, `diffCellsSppf`, `extractSliceSppf`, `doMultiplicationsSppf`, `completeSppf`, `computeSppf`, `completeLayerModifiedSppf`, `completeVLayerModifiedSppf`, `terminalRulesFromGrammarSppf`, `binaryRulesFromGrammarSppf`, `initValiantSppf`
- Rename SPPF internal types/functions to drop "Sppf" suffix:
  - `InitDataSppf` → `InitData`
  - `mxmSetSppf` → `mxmSet`
  - `writeSliceUnionSppf` → `writeSliceUnion`
  - `copyFullTableSppf` → `copyFullTable`
  - `diffCellsSppf` → `diffCells`
  - `extractSliceSppf` → `extractSlice`
  - `doMultiplicationsSppf` → `doMultiplications`
  - `completeSppf` → `complete`
  - `computeSppf` → `compute`
  - `completeLayerModifiedSppf` → `completeLayerModified`
  - `completeVLayerModifiedSppf` → `completeVLayerModified`
  - `initValiantSppf` → `initValiant`
- The `adj` prefix functions (`terminalRulesFromGrammarSppf`, `binaryRulesFromGrammarSppf`) are only used by SPPF path now, rename to drop suffix too
- Add private helper `sppfTableToNtTable : SppfParsingTable<'nt> -> ParsingTable<'nt>`
- Add private helper `sppfTraceStepToNtTraceStep : ValiantSppfTraceStep<'nt> -> ValiantTraceStep<'nt>`
- Add private helper `sppfModifiedTraceStepToNtTraceStep : ModifiedValiantSppfTraceStep<'nt> -> ModifiedValiantTraceStep<'nt>`
- Reimplement `parse`: call `parseWithSppfTable`, return `snd`
- Reimplement `parseWithTable`: call `parseWithSppfTable`, convert table via `sppfTableToNtTable`, return `(ntTable, accepted)`
- Reimplement `parseWithTrace`: call `parseWithSppfTrace`, convert each step
- Same for modified variants
- Equivalence: all existing Valiant tests must pass. Tables produced must be identical to pre-refactoring tables

---

### S3: Add `-no-sppf-table` CLI flag

**Code:** `src/FLPQ.Cli/AlgorithmTypes.fs`, `src/FLPQ.Cli/Program.fs`, `src/FLPQ.Cli/CykRunner.fs`, `src/FLPQ.Cli/ValiantRunner.fs`, `src/FLPQ.Printers/ParsingTableTeX.fs`, `src/FLPQ.Printers/CykTeX.fs`, `src/FLPQ.Printers/ValiantTeX.fs`
**Tests:** `tests/FLPQ.Cli.Tests/CykRunnerTests.fs`, `tests/FLPQ.Cli.Tests/ValiantRunnerTests.fs`
**Docs:** `docs/user/cli.md` (update CLI flags)

**Spec:**
- Add `[<AltCommandLine("--no-sppf-table")>] NoSppfTable` to `AlgorithmTypes.Arguments` DU
- Add usage string: "Render table cells as sets of nonterminals (without SPPF split points and production indices)"
- In `Program.fs`: parse flag, pass to runners
- In `ParsingTableTeX.fs`: add `sppfEntryAsNtCellToTeX : ('nt -> string) -> Set<SppfParsingEntry<'nt>> -> string` — renders SPPF entries as just nonterminal names (no split point / production index)
- In `CykTeX.fs`: add `sppfTableToTeXStyledAsNt` and `sppfTableToTeXAsNt` functions using `sppfEntryAsNtCellToTeX`
- In `ValiantTeX.fs`: add `sppfStepToTeXAsNt` and `sppfModifiedStepToTeXAsNt` functions using `sppfEntryAsNtCellToTeX`
- In `CykRunner.fs`: accept `noSppfTable: bool` parameter. When true, render steps using new AsNt functions instead of SPPF triples. SPPF visualization (`sppf.tikz.tex` / `sppf.dot`) still generated.
- In `ValiantRunner.fs`: same pattern for both `runValiant` and `runValiantModified`
- In `Program.fs`: extract `noSppfTable` flag, pass to `CykRunner.runCyk`, `ValiantRunner.runValiant`, `ValiantRunner.runValiantModified`

---

### S4: Add and migrate tests

**Code:** `tests/FLPQ.Cli.Tests/CykRunnerTests.fs`, `tests/FLPQ.Cli.Tests/ValiantRunnerTests.fs`, `tests/FLPQ.Printers.Tests/CykSummaryGoldenTests.fs`, `tests/FLPQ.Printers.Tests/ValiantTraceGoldenTests.fs`
**Tests:** New test cases added; existing tests verified to pass
**Docs:** None

**Spec:**
- S4a: Update `CykSummaryGoldenTests.fs` — change `Cyk.parseWithTrace` → `Cyk.parseWithSppfTrace` and `CykTeX.tableToTeX`/`tableToTeXStyled` → `CykTeX.sppfTableToTeX`/`sppfTableToTeXStyled`. Regenerate golden reference files.
- S4b: Update `ValiantTraceGoldenTests.fs` — change `Valiant.parseWithTrace` → `Valiant.parseWithSppfTrace` and `ValiantTeX.stepToTeX` → `ValiantTeX.sppfStepToTeX`. Similarly for modified. Regenerate golden reference files.
- S4c: Update `TexCompilationTests.fs` — change non-SPPF trace function calls to SPPF variants, update rendering calls accordingly.
- S4d: In `TestHelpers.checkCykValiantEquivalence` — remove separate SPPF-vs-non-SPPF acceptance check (lines 357-365) since they now always match. Keep the SPPF table cross-check and tree validation.
- S4e: In `CykRunnerTests.fs`: add test verifying that `-no-sppf-table` flag produces nonterminal-only cells in table.tex (parse generated TeX, verify format).
- S4f: In `ValiantRunnerTests.fs`: add similar tests for Valiant and modified Valiant.
- S4g: Add CLI integration test in `CykRunnerTests.fs`/`ValiantRunnerTests.fs`: verify that when `-no-sppf-table` is NOT passed, cells contain triple format `(Nt, k, prodIdx)`. Verify SPPF output is still generated in both modes.
- S4h: Verify all existing tests pass (CYK, Valiant, runners, printers, compilation).

---

### S5: Code review and quality gates

**Code:** All modified files
**Tests:** All tests
**Docs:** None

**Spec:**
- Run linter on modified projects
- Run format check
- Run full build
- Run all tests
- Run hard gate
