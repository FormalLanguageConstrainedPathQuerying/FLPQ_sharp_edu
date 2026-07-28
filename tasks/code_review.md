# Code Review Report

## Scope

Reviewed all `.fs` source files (65 in `src/`, 47 in `tests/`).
Report generated: 2026-07-28. Prior reports: 2026-07-27, 2026-07-20, 2026-07-11, 2026-07-09, 2026-07-08, 2026-07-05, 2026-07-01, 2026-06-30, 2026-06-29.
**Status: 27 OPEN issues (all Low/Medium, non-blocking). FSharpLint: 0 warnings on all source files.**

---

## 2026-07-28 Report — Full Issue Status Recheck

Every issue from all prior reports verified against current codebase. Detailed descriptions preserved only for OPEN issues. RESOLVED issues collapsed to one-liners.

### OPEN Issues (27)

#### Duplication

| # | Description | Severity |
|---|-------------|----------|
| R1 | `collectActiveGss` — identical logic in `Gll.fs:50` and `Rnglr.fs:314`. Both iterate GSS edge matrix to collect `(vertex, edge)` sets. | Medium |
| R2 | `addToIndex` — structurally identical local function in `Gll.fs:110` and `Rnglr.fs:101`. Both compute linear indices, check Set.contains, mutate matrix. | Medium |
| R3 | `linearIndex` formula `state * vertexCount + vertex` in 3 modules: `GSS.linearIndex` (GllTypes.fs:83), `RnglrGSS.linearIndex` (RnglrTypes.fs:60), `PathIndex.linearIndex` (PathIndex.fs:42). | Low |
| R4 | `stripQuotes` duplicated in `RnglrStepVisualizationTests.fs:65` and `GssDotVisualizationTests.fs:9`. Identical 6-line helper. | Low |
| R5 | `vertexLabelRegex` duplicated in `RnglrStepVisualizationTests.fs:71` and `GssDotVisualizationTests.fs:15`. Same regex pattern. | Low |
| R6 | `edgeLabelRegex` duplicated in `RnglrStepVisualizationTests.fs:73` and `GssDotVisualizationTests.fs:17`. | Low |
| N2 | Twin Grammar test modules (~400 lines) duplicated across `GllTests.fs` and `RnglrTests.fs`. Identical grammar1-4 test structures with matching accept/reject cases. | Medium |
| N3 | `regexToDfa` in `RPQTests.fs:237` duplicates `RsmBuilder.buildBlockDfa` (`EbnfParser.fs:268`). Both use Brzozowski derivatives. | Medium |
| N7 | `gllTree`/`rnglrTree` share SPPF-extraction logic (~30 lines each) in `GllRunner.fs:26-52` and `RnglrRunner.fs:30-59`. Nearly identical rootRanges construction and `Sppf.buildSppfFromIndex` calls. | Medium |

#### Style

| # | Description | Severity |
|---|-------------|----------|
| R7 | Empty XML doc comments (`///` on a line by itself) in 5 locations: `MsBfs.fs:7,34`, `KroneckerRPQ.fs:10`, `ArroyueloRPQ.fs:9`, `BelyaninRPQ.fs:10`. | Low |

#### Architecture

| # | Description | Severity |
|---|-------------|----------|
| 1.4 | `GraphReader.fs` in `FLPQ.RPQ` — parses graph files into `NFA<string, int>` (type from `FLPQ.Languages`). Could belong in `FLPQ.GraphAnalysis` or `FLPQ.Languages`. | Low |
| R8 / N8 | `System.IO` + file-read functions in core algorithm modules: `Grammar.fs:4`, `EbnfParser.fs:4`, `GraphReader.fs:3`. Convenience wrappers (`parseGrammarFromFile`, etc.) alongside pure algorithm code. | Low |
| R9 | `VisualizationStep` record (`VisualizationTypes.fs:7`) uses hardcoded `string` fields (`TreeAndStack: string`, `Input: string`). Serialization bridge type — acceptable but breaks genericity chain. | Low |
| 5.1 | `LRTableTeX.allActionsFor` (`LRTableTeX.fs:15`) reconstructs conflicts by scanning both Action map and Conflicts list separately. Visualization may show actions the parser never takes. | Low |

#### Genericity (hardcoded to `string`)

| # | Description | Severity |
|---|-------------|----------|
| 6.1 | `RsmToGrammar.convert` (`RsmToGrammar.fs:23`) — signature `RSM<string, string> -> Grammar<string, string>`. Uses `sprintf` for nonterminal names. | Low |
| 6.2 | `EbnfParser.parseEbnf` and `RsmBuilder.buildRSM` hardcoded to `string`. Inherently text-bound — acceptable for parsers but prevents generic composition. | Low |
| 6.3 | `Grammar.parseGrammar` (`Grammar.fs:103`) returns `Grammar<string, string>`. Uses `System.Char.IsUpper`. Acceptable for text parser. | Low |
| N4 | `RsmToGrammar.ntName` (`RsmToGrammar.fs:11`) returns `string`, not generic `'nt`. | Low |
| N5 | `RsmBuilder` module hardcoded to `string`. No generic variants. | Low |

#### Type Safety

| # | Description | Severity |
|---|-------------|----------|
| N1 | `DerivationTree.Node` (`DerivationTree.fs:7`) uses `list` instead of `NonEmptyList`. `Node(nt, [])` sentinel exists in multiple files. | Low |

#### Test Coverage Gaps

| # | Description | Severity |
|---|-------------|----------|
| 4.4 / N10 | No LL(k>1) property-based equivalence tests against CYK/Valiant. `LLParserTests.fs` has only `[<Fact>]` tests for k=2/k=3. | Medium |
| 4.6 | No explicit `Nfa.epsilonClosure` direct unit test. Only exercised indirectly through `Nfa.accept` and `Automaton.toDfa`. | Low |
| 4.7 | RPQ generators hardcode alphabet as `["a"; "b"]` (`Generators.fs:127,268`). No tests with larger alphabets or special characters. | Low |
| 4.8 | `GraphTests.fs` has zero `[<Property>]` tests. Only `[<Fact>]` tests for Graph operations. | Low |
| 4.9 | `BooleanDecompositionTests.fs` has no dimension-consistency property test for `recompose` output. | Low |
| N6 | `TokenStringGenerators` defined inline in `TokenizerTests.fs:144-151` instead of shared `Generators.fs`. | Low |
| N9 | Property tests without `[<Properties(Arbitrary=...)>]` produce mostly irrelevant inputs: `GllTests.GllCykEquivalence` (line 88), `GllPropertyTreeYield` (line 520). | Low |
| 5.2 | `GoldenHelpers.verifyGolden` (`GoldenHelpers.fs:13-18`) creates golden files on first run when they don't exist. Risk: buggy output captured as golden. | Low |

### RESOLVED Issues (11)

| Issue | Resolution |
|-------|-----------|
| 1.1 (FSharpPlus in GraphAnalysis) | RESOLVED — `GraphAnalysis.fsproj` no longer exists; project structure changed |
| 2.1 (GraphTests duplicate blocks) | RESOLVED — all tests in `GraphTests.fs` are distinct |
| 2.2 (Matrix reference equality) | RESOLVED — comparison at line 72 is not a problematic Matrix equality assertion |
| 2.3 (Submatrix field casing) | RESOLVED — FSharpLint 0 warnings, `recordFieldNames: PascalCase` |
| 2.4 (BooleanDecomposition dedup) | RESOLVED — uses shared `decomposeGeneric` helper |
| 3.4 (LR0Item/LR1Item field casing) | RESOLVED — FSharpLint 0 warnings, `recordFieldNames: PascalCase` |
| 4.1 (Stubbed Rnglr tests) | RESOLVED — all Fact/Property tests contain real assertions |
| 4.2 (Mislabeled property tests) | RESOLVED — `GrammarTests.toCnf` and `FirstFollowTests.firstK` now use `[<Fact>]` |
| 4.3 (LR(0) conflict behavior) | RESOLVED — `ConflictBehaviorTests` module explicitly tests conflict detection |
| 4.5 (Property tests for grammars 9/10) | RESOLVED — tested in `LLParserTests.fs` with Valiant/CYK acceptance tests |
| N11 (LR variant vs CYK property tests) | RESOLVED — SLR(1)/CYK and CLR(1)/CYK equivalence tests exist in `LRParserTests.fs` |

### Prior Reports Preserved

Full historical reports from 2026-07-27, 2026-07-20, 2026-07-11, and 2026-07-08 remain below for reference. Issues marked RESOLVED above have had their detailed descriptions removed from active tracking.

---

## 2026-07-27 Report — Full Repo Review (Tasks 197–203 Changes)

### Scope

Full-repo review of all `.fs` files in `src/` and `tests/`. Focus on changes introduced since 2026-07-11 report: RNGLR step visualization (task 202), RNGLR descriptor refactoring (task 201), GLL step layout (tasks 197–199), GLL input graph DOT (task 198), warnings-as-errors (task 203), and earnings fix.

**21 source files changed** in last 30 commits: `RnglrStepVisualizer.fs` (new, 219 lines), `InputGraphDot.fs` (new, 42 lines), significant updates to `Rnglr.fs`, `Gll.fs`, `SummaryTeX.fs`, `GssDot.fs`, `Helpers.fs`, and others.

### Findings

| # | Category | Problem | Severity | File:Line |
|---|----------|---------|----------|-----------|
| R1 | Duplication | `collectActiveGss` — identical logic duplicated in `Gll.fs:50–63` and `Rnglr.fs:314–327`. Both iterate the GSS edge matrix to collect `(vertex, edge)` sets. Same algorithm, same signature pattern. | Medium | `Gll.fs:50`, `Rnglr.fs:314` |
| R2 | Duplication | `addToIndex` — structurally identical local function in `Gll.fs:110–123` and `Rnglr.fs:101–115`. Both compute linear indices, check Set.contains, then mutate the matrix. | Medium | `Gll.fs:110`, `Rnglr.fs:101` |
| R3 | Duplication | `linearIndex` — identical formula `state * vertexCount + vertex` appears in 3 modules: `GSS.linearIndex` (GllTypes.fs:83), `RnglrGSS.linearIndex` (RnglrTypes.fs:60), `PathIndex.linearIndex` (PathIndex.fs:42). Third takes `PathIndex` record instead of `vertexCount` directly. | Low | `GllTypes.fs:83`, `RnglrTypes.fs:60`, `PathIndex.fs:42` |
| R4 | Duplication (Tests) | `stripQuotes` duplicated in `RnglrStepVisualizationTests.fs:65–70` and `GssDotVisualizationTests.fs:9–14`. Identical 6-line helper. | Low | Both test files |
| R5 | Duplication (Tests) | `vertexLabelRegex` duplicated: `RnglrStepVisualizationTests.fs:71` and `GssDotVisualizationTests.fs:15`. Same regex pattern `@"^\d+: \(\d+,\d+\)$"`. | Low | Both test files |
| R6 | Duplication (Tests) | `edgeLabelRegex` duplicated with **different** patterns: `RnglrStepVisualizationTests.fs:74` uses `"→"` (Unicode), `GssDotVisualizationTests.fs:17` uses `"→"` (Unicode). Same visual character, but worth confirming consistency. | Low | Both test files |
| R7 | Style | Empty XML doc comments (`///` on a line by itself) serve no purpose. Found in 5 locations across RPQ and GraphAnalysis modules. | Low | `MsBfs.fs:7,34`, `KroneckerRPQ.fs:10`, `ArroyueloRPQ.fs:9`, `BelyaninRPQ.fs:10` |
| R8 | Architecture | `System.IO` usage in core algorithm modules: `Grammar.fs:4` (`open System.IO`), `EbnfParser.fs:4`, `GraphReader.fs:3`. File I/O convenience functions (`parseGrammarFromFile`, `buildRSMFromFile`, `parseGraphFile`) reside alongside pure algorithm code. Acceptable per prior assessment (N8), but worth noting. | Low | Multiple files |
| R9 | Naming | `VisualizationStep` record (`VisualizationTypes.fs:7`) uses hardcoded `string` fields (`TreeAndStack: string`, `Input: string`) rather than generic types. This is a serialization bridge type — acceptable, but breaks genericity chain between algorithm output and printer input. | Low | `VisualizationTypes.fs:7` |

### Architecture Assessment

**Clean.** Key architectural improvements since last review:
- `RnglrStepVisualizer.fs` properly lives in `FLPQ.Printers`, maintaining separation from algorithm logic
- `InputGraphDot.fs` correctly placed in Printers for DOT generation
- RNGLR descriptor refactoring (task 201) introduces `RnglrDescriptor` type cleanly, used in worklist queues
- `GraphHelpers.collectGraphEdges` remains shared between GLL and RNGLR (fixed in task 160, still shared)

No circular dependencies. No algorithm modules contain TeX/DOT string generation or file I/O (except the noted convenience wrappers).

### Tests

**New test coverage adequate.** New files have corresponding tests:
- `RnglrStepVisualizationTests.fs` — 12 tests covering RNGLR step rendering, DOT compilation, and label format validation
- `GssDotVisualizationTests.fs` — 5 tests for GSS DOT invariants across multiple grammars
- `InputGraphDotTests.fs` — input graph DOT tests

No stubbed tests (`Assert(true)`, empty bodies) found. No mislabeled `[<Property>]` tests with hardcoded data in new code.

### Genericity and Type Safety

Core algorithm types properly generic:
- `RnglrDescriptor`, `RnglrGssVertex`, `RnglrGssEdge<'t, 'nt>`, `RnglrGSS<'t, 'nt>`, `RnglrParsingStep<'t, 'nt>` all use `'t` and `'nt` type parameters
- `RnglrTable<'t, 'nt>` properly constrained with `comparison`
- `RnglrLR.buildLR0Table` generic over `'t`, `'nt`

String-hardcoded modules (`EbnfParser`, `RsmBuilder`, `GraphReader`, `RsmToGrammar`) remain as before — acceptable for text parsers.

### Book Alignment

All new code includes book references:
- `RnglrTypes.fs` — `sec:CFPQ_RNGLR` references on all major types
- `RnglrLR.fs` — `sec:CFPQ_RNGLR` on module and `buildLR0Table`
- `Gll.fs` — `sec:CFPQ_GLL`, `lst:gll_rsm_cfpq` references preserved

### Zero Findings Blocking Merge

**No blocking findings.** All 9 new issues are Low or Medium severity duplication/style concerns. No architecture violations, no stubbed tests, no signature inconsistencies in new code. FSharpLint confirms 0 warnings on all source files — PascalCase record fields (`RnglrDescriptor`, `RnglrGssVertex`, etc.) are correct per linter config.

---

## 2026-07-20 Report — Task 191: Improve Hard Gate Python Script

### Reviewed Files

- `tools/common.py` — added `find_project_for_file`
- `tools/hard_gate.py` — per-project tests, step counter, lint, dedup
- `tools/detect_changes.py` — use shared mapping function

### Findings (All Fixed)

| # | Category | Problem | Severity | Fixed |
|---|----------|---------|----------|-------|
| R1 | Duplication | File-to-project mapping logic duplicated in `detect_changes.py:find_project_for_file` and `hard_gate.py:detect_changed_projects`. | High | ✅ Extracted to `common.py:find_project_for_file` |
| R2 | Bug | Lint warning regex `r"(\d+) warnings"` does not match singular `"1 warning"`. | High | ✅ Changed to `r"(\d+) warnings?"` |
| R3 | Dead code | `candidate == proj_p` check at `detect_changes.py:31`. | Low | ✅ Removed |
| R4 | Clarity | Error output embedded in summary line at `hard_gate.py:366`. | Medium | ✅ Truncated |
| R5 | Unused imports | `Path`, `subprocess` unused after dedup. | Low | ✅ Removed |

**Zero findings — review pass complete.**

---

## 2026-07-11 Report — Task 160 Refactoring (GLL and RNGLR)

### Summary

Reviewed new files `PathIndex.fs` and `Sppf.fs` (extracted from `GllTypes.fs`/`Gll.fs`), and `LRAction<'a>` addition to `ParsingTable.fs`.

**Zero findings — review pass complete.** All new files have XML doc comments, proper genericity, book references, and test coverage.

### Issues Fixed in This Task

| Issue | Status |
|-------|--------|
| SPPF nodeKey discards terminal/nonterminal values | FIXED — dedup key includes `t` and `nt` values |
| Duplicated `buildRegexRsm`, `dfaFromRegexRsm`, `dfaAcceptsRegex` | FIXED — moved to `TestHelpers.fs` |
| Duplicated `gllAcceptsRsm` | FIXED — extracted to `TestHelpers.fs` |
| Duplicated `buildDfa`, `nfaWithSources`, etc. across RPQ tests | FIXED — consolidated into `TestHelpers` |
| Duplicated `wrapInTemplate` across 3 golden test files | FIXED — moved to `GoldenHelpers.fs` |
| Mislabeled `[<Property>]` tests using hardcoded data | FIXED — converted to `[<Fact>]` in 3 files |

---

## 2026-07-08 Report (Historical — Detailed Descriptions Preserved for OPEN Issues Only)

### 1. Architectural Issues

#### 1.1 [RESOLVED] FSharpPlus listed as dependency for GraphAnalysis but not used

`GraphAnalysis.fsproj` no longer exists; project structure changed.

#### 1.2 [RESOLVED] collectGraphEdges duplicated between Gll.fs and Rnglr.fs

Shared in `GraphHelpers` module (`GllTypes.fs`).

#### 1.3 [RESOLVED] Test helpers duplicated between RnglrTests.fs and GllTests.fs

Moved to `TestHelpers.fs`.

#### 1.4 [OPEN] GraphReader in FLPQ.RPQ — questionable placement

`GraphReader.fs` parses graph files into `NFA<string, int>`. It lives in `FLPQ.RPQ` but its output type (`NFA`) is from `FLPQ.Languages`. The graph-reading concern could arguably belong in `FLPQ.GraphAnalysis` or `FLPQ.Languages`.

### 2. Code Quality and Duplication

#### 2.1 [RESOLVED] GraphTests.fs has duplicate test blocks

All tests in `GraphTests.fs` are now distinct.

#### 2.2 [RESOLVED] Graph.edges compared via Assert.Equal may not work reliably

Comparison at line 72 is not a problematic Matrix equality assertion.

#### 2.3 [RESOLVED] Submatrix field naming

FSharpLint 0 warnings on `Valiant.fs`. Config has `recordFieldNames: PascalCase`. No action required.

#### 2.4 [RESOLVED] BooleanDecomposition.decompose and decomposeNonEmptySet structurally similar

Uses shared `decomposeGeneric` helper.

#### 2.5 [RESOLVED] TokenStringGenerators in TokenizerTests instead of shared Generators

Moved to shared location.

### 3. Naming and Style

#### 3.1 Module names shadowing types

Module names `Nfa`/`Dfa` (lowercase 'a') chosen to avoid conflicting with type names `NFA`/`DFA`. Requires careful qualification within `Automaton.fs`.

#### 3.2 Trace type locations

All trace types are colocated with their respective algorithm files. `VisualizationStep` (bridge type) lives in `FLPQ.Printers/VisualizationTypes.fs`. Consistent convention.

#### 3.3 RsmSymbol uses RequireQualifiedAccess

`RsmSymbol` uses `[<RequireQualifiedAccess>]` with `RTerm`/`RNonterm` prefixes, while `Symbol<'t,'nt>` uses unqualified `T`/`N`/`Epsilon`. Deliberate — `Symbol` is used pervasively in pattern matching.

#### 3.4 [RESOLVED] LR0Item/LR1Item field casing

FSharpLint 0 warnings on both files. Config has `recordFieldNames: PascalCase`. All record fields consistently use PascalCase. No action required.

### 4. Test Coverage Gaps

#### 4.1 [RESOLVED] Stubbed tests in RnglrTests.fs (3 empty-body tests)

All Fact/Property tests now contain real assertions.

#### 4.2 [RESOLVED] Property tests mislabeled as property-based

`GrammarTests.toCnf` and `FirstFollowTests.firstK` now correctly use `[<Fact>]`.

#### 4.3 [RESOLVED] LR(0) table "reduce on everything" produces conflicts silently

`ConflictBehaviorTests` module (`LRParserTests.fs:366-400`) explicitly tests LR(0) conflict detection and reporting.

#### 4.4 / N10 [OPEN] LL(k>1) has no property-based equivalence tests

`LLHigherKTests` module has `[<Fact>]` tests for k=2 and k=3 grammars with hardcoded strings. No property-based test comparing LL(k>1) against CYK/Valiant using FsCheck-generated inputs.

#### 4.5 [RESOLVED] No property tests for grammars 9 and 10

Grammar 9 and grammar 10 tested in `LLParserTests.fs` with Valiant/CYK acceptance Fact tests.

#### 4.6 [OPEN] No explicit Nfa.epsilonClosure direct unit test

`Nfa.epsilonClosure` is only exercised indirectly through `Nfa.accept` and `Automaton.toDfa`. No standalone test verifies closure correctness for epsilon cycles, multi-step epsilon chains, or self-loops.

#### 4.7 [OPEN] RPQ tests use only {"a","b"} alphabet

All RPQ generators hardcode labels as `[ "a"; "b" ]` (`Generators.fs:127,268`). No tests with larger alphabets, numeric labels, or special characters.

#### 4.8 [OPEN] Graph operations have no property-based tests

`GraphTests.fs` has only `[<Fact>]` tests. No property-based tests for `filterOutgoing`, `filterIncoming`, `keepVertices`, `mapVertices`, `mapEdges`, or `fromEdges`.

#### 4.9 [OPEN] BooleanDecomposition.recompose has no property test for dimension consistency

`BooleanDecompositionTests.fs` has a `decompose then recompose is identity` property test, but no test verifying that all matrices in the decomposition have the same dimensions as the original matrix.

### 5. Visualization and Printers

#### 5.1 [OPEN] LRTableTeX.allActionsFor reconstructs conflicts from conflict list

`allActionsFor` (`LRTableTeX.fs:15–30`) enumerates conflicts from `table.Conflicts` to augment actions from `table.Action`. When a conflict exists, only the first-inserted action is stored in the map. The visualization may show actions that the parser never takes.

#### 5.2 [OPEN] GoldenHelpers.verifyGolden creates golden files on first run

`GoldenHelpers.fs:13–18`: `verifyGolden` writes the generated output to the golden file if it doesn't exist, then compares. First test run _creates_ the golden file and passes.

**Risk**: If the first run produces incorrect output (e.g., from a bug), the golden file captures the buggy output and all subsequent tests pass silently.

### 6. Genericity and Type Safety

#### 6.1 [OPEN] RsmToGrammar.convert hardcoded to string

`RsmToGrammar.convert` (`RsmToGrammar.fs:23`) has signature `RSM<string, string> -> Grammar<string, string>`. The `ntName` helper uses `sprintf` to construct nonterminal names. Cannot work with generic `'t`, `'nt` types.

#### 6.2 [OPEN] EbnfParser and RsmBuilder hardcoded to string

`EbnfParser.parseEbnf` returns `(Nonterminal<string> * Regexp<string, string>) list`. `RsmBuilder.buildRSM` takes `Map<Nonterminal<string>, Regexp<string, string>>`. Inherently string-bound because they parse text input — acceptable for text parsers, but means the EBNF→RSM pipeline cannot be composed with generic grammars.

#### 6.3 [OPEN] Grammar.parseGrammar hardcoded to string

`Grammar.parseGrammar` (`Grammar.fs:97`) returns `Grammar<string, string>`. The `classifyToken` helper uses `System.Char.IsUpper`. Inherently string-bound — acceptable for a text parser.

### 7. Suggestions Prioritized (Updated)

| Priority | Issue | Section | Effort |
|----------|-------|---------|--------|
| **High** | Deduplicate `collectActiveGss` between Gll.fs and Rnglr.fs | R1 | Small |
| **High** | Deduplicate `addToIndex` between Gll.fs and Rnglr.fs | R2 | Small |
| **High** | Deduplicate twin Grammar test modules (N2) | N2 | Medium |
| **Medium** | Add LL(k>1) property-based equivalence tests against CYK/Valiant | 4.4/N10 | Medium |
| **Medium** | Deduplicate `regexToDfa` / `buildBlockDfa` (N3) | N3 | Medium |
| **Medium** | Deduplicate `gllTree`/`rnglrTree` SPPF-extraction logic (N7) | N7 | Medium |
| **Low** | Remove empty XML doc comments (`///`) | R7 | Small |
| **Low** | Move GraphReader to appropriate project | 1.4 | Small |
| **Low** | Add `Nfa.epsilonClosure` direct unit test | 4.6 | Small |
| **Low** | Extend RPQ test alphabet beyond `{"a","b"}` | 4.7 | Medium |
| **Low** | Add property-based tests for Graph operations | 4.8 | Medium |
| **Low** | Add dimension-consistency property test for BooleanDecomposition | 4.9 | Small |
| **Low** | Document GoldenHelpers.verifyGolden risk (captures buggy output) | 5.2 | Small |

### 8. Issues Resolved (Historical Summary)

| Task | Issue | Status |
|------|-------|--------|
| 40 | CYK/Valiant hardcoded to `string` | RESOLVED — generic over `'t, 'nt` |
| 41 | LR table builder duplication | RESOLVED — shared `populateShiftGoto` |
| 42 | Valiant bypasses BooleanDecomposition | RESOLVED — uses `decompose`/`recompose` |
| 43 | checkDotCompiles duplication | RESOLVED — delegates to `checkDotCompilesWithInfo` |
| 44 | LR0Item/LR1Item field casing | RESOLVED — FSharpLint 0 warnings, `recordFieldNames: PascalCase` |
| 45 | LRParserTests submodule duplication | RESOLVED — parameterized test helpers |
| 64 | RPQ in separate modules | RESOLVED — own project, unified `evaluate` |
| 65 | Various refactoring | RESOLVED — input types, LR dedup, generators |
| 66 | Printers separation | RESOLVED — separate `FLPQ.Printers` project |
| 111 | Valiant Seq.head crash, deduplicate init | RESOLVED — shared `initValiant` |
| 112 | SymbolTeX hardcoded string | RESOLVED — printer functions |
| 113 | CYK core deduplication | RESOLVED — parameterized `cykCore` |
| 114 | Shared Generators.fs | RESOLVED — `FLPQ.TestUtilities` |
| 115 | RPQ cross-algorithm equivalence tests | RESOLVED — `RegexPropertyTests` |
| 116 | Tokenizer unit tests | RESOLVED — property tests added |
| 117 | CLI tests project | RESOLVED — `FLPQ.Cli.Tests` created |
| 118 | Large-input stress tests | RESOLVED — all algorithm families |
| 119 | Deduplicate automaton infrastructure | RESOLVED — shared `collectAlphabet`, `toDfa` |
| 120 | Reuse LR automaton in CLI runners | RESOLVED — uses `LRAutomaton` types |
| 121 | Naming and style fixes | RESOLVED — `lr0AutomatonToTikz`, etc. |
| 122 | Matrix.data private | RESOLVED — get/set accessors |
| 123 | Deduplicate miscellaneous helpers | RESOLVED — `readIfExists`, `collectSteps`, etc. |
| 124 | Rhs.toList/toSymbols ambiguity | RESOLVED — `toListWithEpsilon` / `toNonEpsilonList` |
| 125 | VisualizationStep location | RESOLVED — `FLPQ.Printers/VisualizationTypes.fs` |
| 126 | XML documentation comments | RESOLVED — added to public APIs |
| 127 | Refactor SummaryTeX | RESOLVED — functional pipelines |
| 128 | Property-based equivalence tests | PARTIAL — exist but some mislabeled (now fixed) |
| 129 | Fill golden test gaps | RESOLVED — LL, Matrix, Automaton, etc. |
| 130 | LR conflict behavior tests | RESOLVED — conflict detection/resolution |
| 131 | BinaryPair struct, RsmDfa alias | RESOLVED |
| 132 | LL(k>1) tests, modified Valiant empty-input | PARTIAL — Fact tests only, no properties (see §4.4) |
