# Code Review Report

## Scope

Reviewed all 62 `.fs` source files (`src/`) and 49 `.fs` test files (`tests/`).
Report generated: 2026-08-06. Fresh full-repo review — all prior reports verified and consolidated.
**Status: 29 OPEN issues. FSharpLint: 0 warnings on all source files.**

Each issue was verified against the current codebase. Issues from prior reports that are now fixed (R1, R3–R6, N7, R7, 4.4/N10, 4.6, 4.8, 4.9, N6, 5.2) or obsolete (N9/GllCykEquivalence) have been removed.

---

## 1. Duplication

| # | Description | Severity | File:Line |
|---|-------------|----------|-----------|
| D1 | `addToIndex` — structurally identical local wrapper function in `Gll.fs:111–118` and `Rnglr.fs:104–111`. Both delegate to `PathIndex.addWithTracking`. | Medium | `Gll.fs:111`, `Rnglr.fs:104` |
| D2 | Twin Grammar test modules — `GllTests.fs` and `RnglrTests.fs` share identical module structure (GllSharedAcceptance, GllTreeYield, GllPropertyTreeYield, etc.). Most logic is shared via `ParsingTestCases` and `TestHelpers`, but near-identical boilerplate remains. | Medium | `GllTests.fs`, `RnglrTests.fs` |
| D3 | `regexToDfa` (`RPQTests.fs:237`) and `buildBlockDfa` (`EbnfParser.fs:319`) — both call `Regexp.buildDfaFromRegex` with slight parameter wrappings. Conceptual duplication. | Medium | `RPQTests.fs:237`, `EbnfParser.fs:319` |
| D4 | `escapeLabel` — identical `s.Replace("\"", "\\\"")` defined in 3 files: `DerivationTreeDot.fs:8`, `SppfDot.fs:9`, `BasicSppfDot.fs:10`. Canonical source is `DerivationTreeDot.fs`. | Medium | `SppfDot.fs:9`, `BasicSppfDot.fs:10` |
| D5 | `countScc` / `countNonTrivialScc` — near-identical Tarjan's SCC implementations in `BasicSppf.fs:254–336`. ~80 lines duplicated; differ only in SCC-counting condition. | Medium | `BasicSppf.fs:254,294` |
| D6 | Valiant algorithm duplicated for two type variants in `Valiant.fs`. `complete`/`completeSppf`, `compute`/`computeSppf`, etc. share identical recursive structures (~350 near-identical lines). | Medium | `Valiant.fs` |
| D7 | `ValiantRunner.runValiant` / `ValiantRunner.runValiantModified` share ~80% identical body (grammar parsing, tokenization, I/O, SPPF construction, step iteration). | Low | `ValiantRunner.fs:9,53` |

## 2. Architecture

| # | Description | Severity | File:Line |
|---|-------------|----------|-----------|
| A1 | Missing explicit `ProjectReference` to `FLPQ.GraphAnalysis` in `FLPQ.Printers.fsproj` and `FLPQ.Cli.fsproj`. Both projects use `open FLPQ.GraphAnalysis` / `FLPQ.GraphAnalysis.Graph.*` but rely on transitive propagation through `FLPQ.Languages`. | Medium | `.fsproj` files |
| A2 | `GraphReader.fs` in `FLPQ.RPQ` — parses graph files into `NFA<string, int>` (type from `FLPQ.Languages`). Could belong in `FLPQ.GraphAnalysis` or `FLPQ.Languages`. | Low | `GraphReader.fs` |
| A3 | `System.IO` + file-read convenience functions in core algorithm modules: `Grammar.fs:4,118`, `EbnfParser.fs:4,301,391`, `GraphReader.fs:3,85`. Thin wrappers alongside pure algorithm code — acceptable but worth noting. | Low | Multiple files |
| A4 | `LRTableTeX.allActionsFor` (`LRTableTeX.fs:15–31`) reconstructs conflicts by scanning both `table.Action` map and `table.Conflicts` list independently, then concatenating. Visualization may show actions the parser never takes. | Low | `LRTableTeX.fs:15` |
| A5 | `Valiant.fs` at 912 lines — oversized. SPPF variant functions could be split into a separate file or unified with a generic representation. | Low | `Valiant.fs` |

## 3. Naming and Types

| # | Description | Severity | File:Line |
|---|-------------|----------|-----------|
| T1 | `VisualizationStep` record (`VisualizationTypes.fs:7`) uses hardcoded `string` fields: `TreeAndStack: string`, `Input: string`. Serialization bridge type — acceptable but breaks genericity chain. | Low | `VisualizationTypes.fs:7` |
| T2 | `DerivationTree.Node` (`DerivationTree.fs:7`) uses `list` instead of `NonEmptyList` for children. Type-level invariant: a non-leaf node must have at least one child. | Low | `DerivationTree.fs:7` |

## 4. Genericity (Hardcoded to `string`)

| # | Description | Severity | File:Line |
|---|-------------|----------|-----------|
| G1 | `RsmToGrammar.convert` — signature `RSM<string, string> -> Grammar<string, string>`. Uses `sprintf` for nonterminal names. | Low | `RsmToGrammar.fs:23` |
| G2 | `EbnfParser.parseEbnf` returns `(Nonterminal<string> * Regexp<string, string>) list`. `RsmBuilder.buildRSM` takes `Map<Nonterminal<string>, Regexp<string, string>>`. Inherently text-bound. | Low | `EbnfParser.fs:298,377` |
| G3 | `Grammar.parseGrammar` returns `Grammar<string, string>`. Uses `System.Char.IsUpper`. Acceptable for text parser. | Low | `Grammar.fs:103` |
| G4 | `RsmToGrammar.ntName` returns `string`, not generic `'nt`. Input is `Nonterminal<string>`. | Low | `RsmToGrammar.fs:11` |
| G5 | `RsmBuilder` module (in `EbnfParser.fs:317–392`) hardcoded to `string`. No generic variants. | Low | `EbnfParser.fs:317` |

## 5. Test Coverage Gaps

| # | Description | Severity | File:Line |
|---|-------------|----------|-----------|
| T1 | RPQ generators hardcode alphabet as `["a"; "b"]` in multiple generator types (`Generators.fs:127,191,268,348`). No tests with larger alphabets, numeric labels, or special characters. | Low | `Generators.fs` |
| T2 | `GllPropertyTreeYield` module (`GllTests.fs:88–122`) has no `[<Properties(Arbitrary=...)>]`. Uses FsCheck's default `Arbitrary<string>` which generates random unicode — most strings are trivially rejected. Compare with `RnglrPropertyTreeYield` which uses `GenToArbitrary.AbString`. | Low | `GllTests.fs:88` |
| T3 | `GraphTests.fs:253` — `[<Property>]` test `fromEdges produces correct dimensions` takes `()` and uses only hardcoded data. Should be `[<Fact>]`. | Low | `GraphTests.fs:253` |
| T4 | `PathIndex.fs` (351 lines) — no dedicated unit test file. Only tested indirectly through `PathIndexTeXTests.fs` (golden) and `GllTests.fs`/`RnglrTests.fs` (acceptance). Missing direct tests for `GridIndex`, `RangeKey`, `PathIndex` operations. | Medium | `PathIndex.fs` |
| T5 | `RnglrTableTeX.fs` (276 lines) — no dedicated unit test file. Only tested indirectly through `RnglrStepVisualizationTests.fs` golden tests. | Medium | `RnglrTableTeX.fs` |

## 6. Documentation Gaps

| # | Description | Severity | File:Line |
|---|-------------|----------|-----------|
| D1 | Missing XML doc comments on all 13 public functions in `FLPQ.Cli/Helpers.fs`. | High | `Helpers.fs` |
| D2 | Missing XML doc comments on all 6 CLI runner entry-point functions (`runCyk`, `runValiant`, `runGll`, `runLL`, `runLR`, `runRnglr`). | Medium | `*Runner.fs` |
| D3 | Missing XML doc comments on `FLPQ.Cli/Summary.fs` (`algorithmToKind`, `algorithmLower`, `buildSummary`) and `Program.fs` (`runCli`). | Low | `Summary.fs`, `Program.fs` |
| D4 | Missing XML doc comments on various `FLPQ.Printers/` rendering functions (AutomatonDot, CykTeX, step visualizers, SppfDot, GssDot, MatrixTeX, LLTableTeX, LRTableTeX, SummaryTeX). | Low | Multiple files |
| D5 | Missing book reference comments in 8 algorithm files: `Cyk.fs`, `Valiant.fs`, `LLParser.fs`, `LRParser.fs`, `Rnglr.fs`, `FirstFollow.fs`, `Automaton.fs`, and CNF/Grammar transformation functions in `Grammar.fs`. Every implementation must be traceable to a specific algorithm or example in the book. | Medium | Multiple files |
