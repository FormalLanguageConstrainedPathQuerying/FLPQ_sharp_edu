# Global Plan: Tasks 128--132

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 128 | Add property-based equivalence tests: toCnf, FirstFollow, NFA→DFA, AutomatonDot, RsmBuilder, fix BooleanDecompositionTests | Tests | 131 (types must be stable) |
| 129 | Fill golden test gaps: LL table TeX, Matrix TeX, Automaton dot/Tikz, Derivation tree dot, Valiant trace TeX + TeX/DOT runtime compilation checks | Tests | All others (end-to-end) |
| 130 | Add LR conflict behavior tests: verify conflict detection, conflict reporting in visualization | Tests | 131 (types must be stable) |
| 131 | Refactoring: BinaryPair named struct, RsmDfa type alias, RsmSymbol consistency | Refactor | None |
| 132 | LL(k>1) tests, modified Valiant empty-input test, move 4 NFA/DFA tests, verify CliSummaryTests.fs | Tests | 131 (types must be stable) |

## Dependencies Graph

```
Task 131 → Task 128, Task 130, Task 132 → Task 129
```

- Task 131 (refactoring) must go first to avoid test rewriting after type changes.
- Tasks 128, 130, 132 can proceed in any order after 131 (they touch different test files).
- Task 129 (golden tests) goes last — golden tests are end-to-end and catch any regressions from prior changes.

## Execution Order

1. **Task 131** — Refactoring (BinaryPair, RsmDfa, RsmSymbol)
2. **Task 128** — Property-based equivalence tests
3. **Task 130** — LR conflict behavior tests  
4. **Task 132** — LL(k>1) tests, test moves, empty-input test
5. **Task 129** — Golden test gaps

## Conflict Analysis

- **Task 131 vs 128**: 131 changes types used in tests (BinaryPair in Valiant, RsmSymbol). 128 adds property tests that reference these types. 131 first avoids rewriting.
- **Task 131 vs 132**: 132 adds LL(k>1) tests, moves DFA/NFA tests. 131 changes RsmSymbol which is in RSM modules. Minimal overlap since 132 is mostly tests.
- **Task 128 vs 130**: Different test files entirely (Property tests vs LR conflict tests).
- **Task 128 vs 132**: Different test concerns (property equivalence vs LL(k>1)/test moves).
- **Task 129 vs all**: Golden tests for LL table TeX, Matrix TeX, Automaton dot/Tikz, Derivation tree dot, Valiant trace TeX. Goes last to avoid golden file churn.

## Shared Infrastructure

- `Generators.fs` in `tests/FLPQ.TestUtilities/` — already shared; new property tests (task 128) should add generators there if needed.
- `ExternalToolsTests.fs` pattern — task 129 runtime compilation checks should follow existing patterns.
- `GoldenHelpers.fs` — already shared for golden test file I/O; task 129 reuses it.

## Task 131: Refactoring Details

### Changes
1. Define `BinaryPair<'nt> = { left: Nonterminal<'nt>; right: Nonterminal<'nt> }` in `Grammar.fs` (or `Valiant.fs`)
2. Replace all `Nonterminal<'nt> * Nonterminal<'nt>` tuples in `Valiant.fs` with `BinaryPair<'nt>`
3. Define `type RsmDfa<'t,'nt when 't: comparison and 'nt: comparison> = DFA<RsmSymbol<'t,'nt>, int>` in `RSM.fs`
4. Use `RsmDfa` in `RsmBlock` and `RsmBuilder`
5. Make `RsmSymbol` and `Symbol` consistent: either add `[<RequireQualifiedAccess>]` to `Symbol` or remove from `RsmSymbol`
   - Remove `[<RequireQualifiedAccess>]` from `RsmSymbol` to match `Symbol` (both have unique case names: RTerm/RNonterm vs T/N/Epsilon, no collision risk)
   - Or add `[<RequireQualifiedAccess>]` to `Symbol` — more impactful change, would require updating all `Symbol` pattern matches
   - **Decision**: Remove `[<RequireQualifiedAccess>]` from `RsmSymbol` (less disruptive)

### Equivalence
- All existing tests must pass after refactoring

## Task 128: Property-Based Equivalence Tests

### Changes
1. `toCnf` language preservation: generate random grammars, convert to CNF, check same strings accepted up to length N
2. `FirstFollow` correctness: generate random grammars, compute FIRST/FOLLOW, verify against brute-force derivation
3. NFA→DFA language preservation: generate random NFAs, check random strings against both
4. `AutomatonDot` output parseable: generate random NFAs/DFAs, render to dot, verify dot is syntactically valid
5. `RsmBuilder` output computable: generate random RSM text, build RSM, verify structure
6. Fix `BooleanDecompositionTests` property test: add assertion that non-empty input must produce non-empty decomposition

### Files to touch
- `tests/FLPQ.Languages.Tests/GrammarTests.fs` — toCnf preservation
- `tests/FLPQ.Languages.Tests/FirstFollowTests.fs` — FirstFollow correctness
- `tests/FLPQ.Languages.Tests/AutomatonTests.fs` — NFA→DFA preservation
- `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — AutomatonDot parseability
- `tests/FLPQ.Languages.Tests/RSMTests.fs` — RsmBuilder computability
- `tests/FLPQ.LinearAlgebra.Tests/BooleanDecompositionTests.fs` — fix property test

## Task 129: Golden Test Gaps

### Changes
1. LL table TeX golden tests — test `LLTableTeX.tableToTeX` output against reference files
2. Matrix TeX golden tests — test `MatrixTeX.toTeX` output against reference files
3. Automaton dot/Tikz golden tests — test `AutomatonDot`/`AutomatonTikz` output against reference files
4. Derivation tree dot golden tests — test `DerivationTreeDot.toDot` output against reference files
5. Valiant/modified Valiant trace TeX golden tests — test `ValiantTeX` output against reference files
6. TeX/DOT runtime compilation checks for modules lacking them — integrate with `ExternalToolsTests` pattern

### Files to touch
- New: `tests/FLPQ.Printers.Tests/LLTableTeXGoldenTests.fs`
- New: `tests/FLPQ.Printers.Tests/MatrixTeXGoldenTests.fs` (or extend `MatrixTeXTests.fs`)
- New/Extend: `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — add golden tests
- New/Extend: `tests/FLPQ.Printers.Tests/DerivationTreeVisualizationTests.fs` — add golden tests
- New/Extend: `tests/FLPQ.Printers.Tests/` Valiant trace golden tests
- `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — add compilation checks

## Task 130: LR Conflict Behavior Tests

### Changes
1. Test that ambiguous grammar produces conflicts in LR(0) table
2. Test that `LRTable.conflicts` list contains expected conflict types
3. Test that non-LR grammar produces "reduce on everything" conflicts
4. Test that conflict reporting in visualization matches actual table conflicts

### Files to touch
- `tests/FLPQ.Languages.Tests/LRParserTests.fs`

## Task 132: LL(k>1), Empty-Input, Test Moves

### Changes
1. Add LL(k>1) parsing tests — generate test grammars requiring k>1 lookahead, verify correct parsing
2. Add modified Valiant empty-input test
3. Move 4 NFA/DFA backward-compatibility member tests from `GraphAnalysis.Tests/GraphTests.fs` to `FLPQ.Languages.Tests/AutomatonTests.fs`
4. Remove `FLPQ.Languages` reference from `GraphAnalysis.Tests`
5. Verify `CliSummaryTests.fs` is already in `FLPQ.Cli.Tests` (no residual reference)

### Files to touch
- `tests/FLPQ.Languages.Tests/LLParserTests.fs`
- `tests/FLPQ.Languages.Tests/ValiantTests.fs`
- `tests/FLPQ.Languages.Tests/AutomatonTests.fs` — add 4 moved tests
- `tests/FLPQ.GraphAnalysis.Tests/GraphTests.fs` — remove 4 tests
- `tests/FLPQ.GraphAnalysis.Tests/FLPQ.GraphAnalysis.Tests.fsproj` — remove FLPQ.Languages reference
