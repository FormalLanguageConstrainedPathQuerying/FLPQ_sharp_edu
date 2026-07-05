
# Global Plan: Tasks 114--118

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|-------------|
| 114 | Create shared `Generators.fs` module for FsCheck generators. Consolidate from 10 files. Create shared test-utility project `FLPQ.TestUtilities`. | Refactor + New project | None |
| 115 | Extend RPQ cross-algorithm equivalence property tests to cover complex regex patterns (RStar, RAlt, RSeq, epsilon, multi-symbol). Use FsCheck generators. | Test | 114 |
| 116 | Add dedicated unit tests for `Tokenizer.fs`. Cover: empty string, whitespace-only, multi-character terminals, edge cases. Property-based tests with FsCheck-generated strings. | Test | 114 |
| 117 | Create `FLPQ.Cli.Tests` project and add to `FLPQ.slnx`. Move `CliSummaryTests.fs` from `FLPQ.Printers.Tests`. Add unit tests for CLI modules. Remove reverse dependency. | New project + Test | 114 |
| 118 | Add large-input stress tests across all algorithm families (CYK length 50+, Valiant 20+ NTs, NFA→DFA 30+ states, RPQ 50+ vertices, LR 100+ states). FsCheck generators with higher bounds. | Test | 114 |

## Dependencies Graph

```
Task 114 (Shared Generators.fs + FLPQ.TestUtilities) ── FOUNDATION for all other tasks
    ├── Task 115 (RPQ regex property tests)
    ├── Task 116 (Tokenizer tests)
    ├── Task 117 (CLI tests project)
    └── Task 118 (Stress tests)
```

Task 114 must be done first. Tasks 115-118 can be done in any order after 114 (they are independent of each other).

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 114 | New: `tests/FLPQ.TestUtilities/` (project, Generators.fs). Modify: all 6 test project `.fsproj` files (add ProjectReference), all 10 source files that define `MyGen`/`MyArb`. Modify: `FLPQ.slnx`. | All tasks (they depend on 114) |
| 115 | `tests/FLPQ.RPQ.Tests/RPQTests.fs` | None (RPQ-specific) |
| 116 | `tests/FLPQ.Languages.Tests/TokenizerTests.fs` (new) | None (new file) |
| 117 | New: `tests/FLPQ.Cli.Tests/`. Modify: `FLPQ.slnx`, `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj`, `tests/FLPQ.Printers.Tests/CliSummaryTests.fs` (removed). | None (removes deps from Printers.Tests, adds new project) |
| 118 | Multiple existing test files — each gets new stress test module or file. `tests/FLPQ.TestUtilities/Generators.fs` (extended with high-bound generators). | 114 (extends Generators) |

## Execution Order

1. **Task 114** — Shared Generators.fs + FLPQ.TestUtilities (foundation)
2. **Tasks 115, 116, 117, 118** — can be done in parallel after 114

Recommended: 114 → 115 → 116 → 117 → 118 (linear) to minimize context switching.

## Shared Infrastructure

### Task 114: FLPQ.TestUtilities project

New test-utility project `tests/FLPQ.TestUtilities/` referenced by all existing test projects.

**Generators.fs** will contain:
- `MatrixGenerators` (from `MatrixTests.fs`): `Matrix<int>`, same-dim pair
- `LinearAlgebraGenerators` (from `LinearAlgebraTests.fs`): square `Matrix<int>`, compatible dims pair
- `SetMatrixGenerators` (from `BooleanDecompositionTests.fs`): `Matrix<Set<int>>`
- `RandomGraphGenerators` (from `RandomGraphGenerators.fs`): `Matrix<bool> * int[]`
- `RPQGenerators` (from `RPQTests.fs`): `RPQTestData`
- `StringGenerators` (from `TestGrammars.fs`): `AbString`, `AString`, `ExprString`
- `IntersectionGenerators` (from `AutomatonTests.fs`): `NfaArb`, `StringArb`
- Remove `MyGen`/`MyArb` duplication from 10 files — consolidate to single `module MyGen = FsCheck.FSharp.Gen` / `module MyArb = FsCheck.FSharp.Arb` in `Generators.fs`
- Remove dead unused imports from `EbnfParserTests.fs` and `RsmToGrammarTests.fs`

**FLPQ.TestUtilities.fsproj**: references FsCheck.Xunit, no source deps needed in most cases. May reference FLPQ.Languages/FLPQ.LinearAlgebra/FLPQ.GraphAnalysis if generator types need those.

### Task 115: RPQ regex property tests

- Generate random regex patterns using FsCheck: `Regexp<string, string>` 
- Convert regex to DFA (ArroyueloRPQ internally does this or we use `Regexp.toDfa` equivalent)
- Generate random NFA graphs (reuse `RPQGenerators`)
- Property test: Belyanin(DFA, NFA) ≡ Arroyuelo(regex, NFA)
- Property test: Belyanin(DFA, NFA) ≡ Kronecker(DFA, NFA)  
- Property test: Arroyuelo(regex, NFA) ≡ Kronecker(DFA, NFA)
- Need a generator for random regex patterns over alphabet `{a, b}`

### Task 116: Tokenizer tests

New file: `tests/FLPQ.Languages.Tests/TokenizerTests.fs`

Property-based test categories:
- `tokenizeStrings`: empty string → `[]`, whitespace-only → `[]`, single/multi-char terminals, leading/trailing spaces
- `tokenizeGen` with identity classifier → returns symbols
- `tokenize`: roundtrip property (tokenize then reconstruct)
- `tokenizeTerminals`: terminal extraction
- Edge cases: null, very long strings, special characters in terminals
- Use FsCheck-generated strings

### Task 117: FLPQ.Cli.Tests

New project: `tests/FLPQ.Cli.Tests/`

Files:
- `CliSummaryTests.fs` (moved from Printers.Tests)
- `CykRunnerTests.fs`: tests for CykRunner logic
- `ValiantRunnerTests.fs`: tests for ValiantRunner
- `LLRunnerTests.fs`: tests for LLRunner
- `LRRunnerTests.fs`: tests for LRRunner
- `HelpersTests.fs`: tests for Helpers (readIfExists, collectSteps, naturalSortKey, cleanOutputDir)
- `AlgorithmTypesTests.fs`: tests for Algorithm parsing
- `ErrorPathTests.fs`: invalid grammar file, missing input file, bad algorithm name, bad lookahead, empty output directory

Remove `CliSummaryTests.fs` from Printers.Tests.
Remove `FLPQ.Cli` project reference from Printers.Tests .fsproj.

### Task 118: Stress tests

Large-input test categories across test projects:

1. **CYK**: Input length 50-200, verify acceptance matches small-input reference. Verify termination < 30s.
2. **Valiant**: Grammars with 20-50 nonterminals, input length 30-100. Verify termination and correctness.
3. **NFA→DFA**: NFAs with 30-100 states, verify DFA preserves language (random strings).
4. **RPQ**: Graphs with 50-200 vertices, regex/DFA queries. Verify all three algorithms match.
5. **LR**: Grammars producing 100-500 automaton states. Verify parsing correctness.
6. **Matrix operations**: 200×200 matrices with mxm, kron. Verify termination.

Generator bounds: use 50-200 instead of current 5-15. Each stress test verifies termination within reasonable time (30s) and correctness.

## Detailed Task Plans

See `detailed_plan.md` for the current task's detailed plan.
