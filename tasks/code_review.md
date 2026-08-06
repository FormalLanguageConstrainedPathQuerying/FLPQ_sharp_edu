# Code Review Report

## Scope

Reviewed all 62 `.fs` source files (`src/`) and 49 `.fs` test files (`tests/`).
Report generated: 2026-08-06. Fresh full-repo review — all prior reports verified and consolidated.
**Status: 56 OPEN issues. FSharpLint: 0 warnings on all source files.**

Each issue was verified against the current codebase. Issues from prior reports that are now fixed (R1, R3–R6, N7, R7, 4.4/N10, 4.6, 4.8, 4.9, N6, 5.2) or obsolete (N9/GllCykEquivalence) have been removed.

---

## 1. Duplication

| # | Description | Source | Severity | File:Line |
|---|-------------|--------|----------|-----------|
| D1 | `addToIndex` — structurally identical local wrapper function in `Gll.fs:111–118` and `Rnglr.fs:104–111`. Both delegate to `PathIndex.addWithTracking`. | §12 | Medium | `Gll.fs:111`, `Rnglr.fs:104` |
| D2 | Twin Grammar test modules — `GllTests.fs` and `RnglrTests.fs` share identical module structure (GllSharedAcceptance, GllTreeYield, GllPropertyTreeYield, etc.). Most logic is shared via `ParsingTestCases` and `TestHelpers`, but near-identical boilerplate remains. | §12 | Medium | `GllTests.fs`, `RnglrTests.fs` |
| D3 | `regexToDfa` (`RPQTests.fs:237`) and `buildBlockDfa` (`EbnfParser.fs:319`) — both call `Regexp.buildDfaFromRegex` with slight parameter wrappings. Conceptual duplication. | §12 | Medium | `RPQTests.fs:237`, `EbnfParser.fs:319` |
| D4 | `escapeLabel` — identical `s.Replace("\"", "\\\"")` defined in 3 files: `DerivationTreeDot.fs:8`, `SppfDot.fs:9`, `BasicSppfDot.fs:10`. Canonical source is `DerivationTreeDot.fs`. | §12 | Medium | `SppfDot.fs:9`, `BasicSppfDot.fs:10` |
| D5 | `countScc` / `countNonTrivialScc` — near-identical Tarjan's SCC implementations in `BasicSppf.fs:254–336`. ~80 lines duplicated; differ only in SCC-counting condition. | §12 | Medium | `BasicSppf.fs:254,294` |
| D6 | Valiant algorithm duplicated for two type variants in `Valiant.fs`. `complete`/`completeSppf`, `compute`/`computeSppf`, etc. share identical recursive structures (~350 near-identical lines). | §12 | Medium | `Valiant.fs` |
| D7 | `ValiantRunner.runValiant` / `ValiantRunner.runValiantModified` share ~80% identical body (grammar parsing, tokenization, I/O, SPPF construction, step iteration). | §12 | Low | `ValiantRunner.fs:9,53` |

## 2. Architecture

| # | Description | Source | Severity | File:Line |
|---|-------------|--------|----------|-----------|
| A1 | Missing explicit `ProjectReference` to `FLPQ.GraphAnalysis` in `FLPQ.Printers.fsproj` and `FLPQ.Cli.fsproj`. Both projects use `open FLPQ.GraphAnalysis` / `FLPQ.GraphAnalysis.Graph.*` but rely on transitive propagation through `FLPQ.Languages`. | §8 | Medium | `.fsproj` files |
| A2 | `GraphReader.fs` in `FLPQ.RPQ` — parses graph files into `NFA<string, int>` (type from `FLPQ.Languages`). Could belong in `FLPQ.GraphAnalysis` or `FLPQ.Languages`. | §8,§9 | Low | `GraphReader.fs` |
| A3 | `System.IO` + file-read convenience functions in core algorithm modules: `Grammar.fs:4,118`, `EbnfParser.fs:4,301,391`, `GraphReader.fs:3,85`. Thin wrappers alongside pure algorithm code — acceptable but worth noting. | §8 | Low | Multiple files |
| A4 | `LRTableTeX.allActionsFor` (`LRTableTeX.fs:15–31`) reconstructs conflicts by scanning both `table.Action` map and `table.Conflicts` list independently, then concatenating. Visualization may show actions the parser never takes. | §21 | Low | `LRTableTeX.fs:15` |
| A5 | `Valiant.fs` at 912 lines — oversized. SPPF variant functions could be split into a separate file or unified with a generic representation. | §9 | Low | `Valiant.fs` |

## 3. Naming and Types

| # | Description | Source | Severity | File:Line |
|---|-------------|--------|----------|-----------|
| T1 | `VisualizationStep` record (`VisualizationTypes.fs:7`) uses hardcoded `string` fields: `TreeAndStack: string`, `Input: string`. Serialization bridge type — acceptable but breaks genericity chain. | §6,§22 | Low | `VisualizationTypes.fs:7` |
| T2 | `DerivationTree.Node` (`DerivationTree.fs:7`) uses `list` instead of `NonEmptyList` for children. Type-level invariant: a non-leaf node must have at least one child. | §7,§11 | Low | `DerivationTree.fs:7` |

## 4. Genericity (Hardcoded to `string`)

| # | Description | Source | Severity | File:Line |
|---|-------------|--------|----------|-----------|
| G1 | `RsmToGrammar.convert` — signature `RSM<string, string> -> Grammar<string, string>`. Uses `sprintf` for nonterminal names. | §6 | Low | `RsmToGrammar.fs:23` |
| G2 | `EbnfParser.parseEbnf` returns `(Nonterminal<string> * Regexp<string, string>) list`. `RsmBuilder.buildRSM` takes `Map<Nonterminal<string>, Regexp<string, string>>`. Inherently text-bound. | §6 | Low | `EbnfParser.fs:298,377` |
| G3 | `Grammar.parseGrammar` returns `Grammar<string, string>`. Uses `System.Char.IsUpper`. Acceptable for text parser. | §6 | Low | `Grammar.fs:103` |
| G4 | `RsmToGrammar.ntName` returns `string`, not generic `'nt`. Input is `Nonterminal<string>`. | §6 | Low | `RsmToGrammar.fs:11` |
| G5 | `RsmBuilder` module (in `EbnfParser.fs:317–392`) hardcoded to `string`. No generic variants. | §6 | Low | `EbnfParser.fs:317` |

## 5. Test Coverage Gaps

| # | Description | Source | Severity | File:Line |
|---|-------------|--------|----------|-----------|
| T1 | RPQ generators hardcode alphabet as `["a"; "b"]` in multiple generator types (`Generators.fs:127,191,268,348`). No tests with larger alphabets, numeric labels, or special characters. | §16 | Low | `Generators.fs` |
| T2 | `GllPropertyTreeYield` module (`GllTests.fs:88–122`) has no `[<Properties(Arbitrary=...)>]`. Uses FsCheck's default `Arbitrary<string>` which generates random unicode — most strings are trivially rejected. Compare with `RnglrPropertyTreeYield` which uses `GenToArbitrary.AbString`. | §15,§16 | Low | `GllTests.fs:88` |
| T3 | `GraphTests.fs:253` — `[<Property>]` test `fromEdges produces correct dimensions` takes `()` and uses only hardcoded data. Should be `[<Fact>]`. | §15 | Low | `GraphTests.fs:253` |
| T4 | `PathIndex.fs` (351 lines) — no dedicated unit test file. Only tested indirectly through `PathIndexTeXTests.fs` (golden) and `GllTests.fs`/`RnglrTests.fs` (acceptance). Missing direct tests for `GridIndex`, `RangeKey`, `PathIndex` operations. | §18 | Medium | `PathIndex.fs` |
| T5 | `RnglrTableTeX.fs` (276 lines) — no dedicated unit test file. Only tested indirectly through `RnglrStepVisualizationTests.fs` golden tests. | §18 | Medium | `RnglrTableTeX.fs` |

## 6. Documentation Gaps

| # | Description | Source | Severity | File:Line |
|---|-------------|--------|----------|-----------|
| D1 | Missing XML doc comments on all 13 public functions in `FLPQ.Cli/Helpers.fs`. | §5 | High | `Helpers.fs` |
| D2 | Missing XML doc comments on all 6 CLI runner entry-point functions (`runCyk`, `runValiant`, `runGll`, `runLL`, `runLR`, `runRnglr`). | §5 | Medium | `*Runner.fs` |
| D3 | Missing XML doc comments on `FLPQ.Cli/Summary.fs` (`algorithmToKind`, `algorithmLower`, `buildSummary`) and `Program.fs` (`runCli`). | §5 | Low | `Summary.fs`, `Program.fs` |
| D4 | Missing XML doc comments on various `FLPQ.Printers/` rendering functions (AutomatonDot, CykTeX, step visualizers, SppfDot, GssDot, MatrixTeX, LLTableTeX, LRTableTeX, SummaryTeX). | §5 | Low | Multiple files |
| D5 | Missing book reference comments in 8 algorithm files: `Cyk.fs`, `Valiant.fs`, `LLParser.fs`, `LRParser.fs`, `Rnglr.fs`, `FirstFollow.fs`, `Automaton.fs`, and CNF/Grammar transformation functions in `Grammar.fs`. Every implementation must be traceable to a specific algorithm or example in the book. | §20 | Medium | Multiple files |

## 7. Language Registry Violations

> **Constraint source:** §13 — language registry must be single source of truth for all grammars, RSMs, accept/reject strings, generators. Use `AnnotatedGrammar.Text` for grammar text, `.Grammar` for parsed CFGs, `.Rsm` for pre-built RSMs. If a needed grammar is missing, add it to the registry first.

The [tests-writer skill](.opencode/skills/tests-writer/SKILL.md) and [language-registry guide](docs/developer/guides/language-registry.md) mandate that `FLPQ.TestUtilities.LanguageRegistry` is the single source of truth for **all** language-dependent information.

### 7.1 Hardcoded `Grammar.parseGrammar` — Non-Printer Test Files

These files call `Grammar.parseGrammar` with hardcoded strings instead of sourcing grammar text from the registry.

| # | File | ~Locations | Duplicates Registry? | Severity |
|---|------|-----------|---------------------|----------|
| RV1 | `GrammarTests.fs` — tests verify `Grammar.parseGrammar` itself. Uses `"S -> a S b S \| eps"` (Dyck1), `"S -> a S \| a"` (APlus), `"S -> a"` (SingleA), `"S -> eps"` (EpsilonOnly), etc. Should use `LanguageRegistry.*.Grammars[n].Text`. Edge-case grammars (unit chains, empty input) must be registered first. | ~36 | Partial | Medium |
| RV2 | `FirstFollowTests.fs` — tests of First/Follow computation. Uses `"S -> a S b S \| eps"` (Dyck1), `"S -> a S \| a"` (APlus), `"S -> a B \| B -> b"` (ANB-like), `"E -> E + T \| T \| T -> x"` (ArithExpr subset), etc. Should use registry grammars. | ~13 | Partial | Medium |
| RV3 | `StressTests.fs:10` — `balancedGrammar` is Dyck1 grammar1. Should use `LanguageRegistry.Dyck1.Grammars[0].Grammar`. Line 123 dynamically generates grammar text — acceptable. | 1 | Yes | Low |
| RV4 | `PathIndexTeXTests.fs` — 4 hardcoded Dyck1 `Grammar.parseGrammar` calls, then bypasses registry RSM via `TestHelpers.grammarToRsm`. | ~4 | Yes | Medium |

### 7.2 Hardcoded `Grammar.parseGrammar` — Printer/Golden Test Files

Every printer/golden test hardcodes grammar strings that duplicate LanguageRegistry entries.

| # | File | ~Locations | Registry Equivalent | Severity |
|---|------|-----------|--------------------|----------|
| RV5 | `TexCompilationTests.fs` | ~14 | Dyck1, APlus | Medium |
| RV6 | `AutomatonVisualizationTests.fs` | 4 | Dyck1, APlus | Medium |
| RV7 | `LLVisualizerTests.fs` | 7 | Dyck1, APlus, ANBN, SingleAB | Medium |
| RV8 | `LRVisualizerTests.fs` | 4 | Dyck1, APlus, ArithExpr | Medium |
| RV9 | `LRStepsGoldenTests.fs` | 2 | APlus, ArithExpr | Medium |
| RV10 | `LRTableTeXGoldenTests.fs` | 2 | Dyck1, ArithExpr | Medium |
| RV11 | `GrammarTeXGoldenTests.fs` | 3 | Dyck1, ArithExpr, TwoTrackDyck | Medium |
| RV12 | `LLStepsGoldenTests.fs` | 2 | Dyck1, APlus | Medium |
| RV13 | `LLTableTeXGoldenTests.fs` | 1 | Dyck1 | Low |
| RV14 | `ValiantTraceGoldenTests.fs` | 2 | Dyck1 | Low |
| RV15 | `CykSummaryGoldenTests.fs` | 1 | Dyck1 | Low |
| RV16 | `DerivationTreeVisualizationTests.fs` | 1 | APlus | Low |

**Total: ~89 hardcoded `Grammar.parseGrammar` calls across 16 files.** Each duplicates grammar text already in LanguageRegistry.

### 7.3 Hardcoded `RsmBuilder.buildRSMFromText` — Parser/Infrastructure Test Files

These files test EBNF/RSM-related functions but hardcode EBNF grammar text instead of sourcing from the registry.

| # | File | ~Locations | Duplicates Registry? | Severity |
|---|------|-----------|---------------------|----------|
| RV17 | `EbnfParserTests.fs` | ~20 | Partial | Medium |
| RV18 | `RSMTests.fs` | ~17 | Partial (SingleA, SingleAB) | Medium |
| RV19 | `RsmToGrammarTests.fs` | ~10 | Partial | Medium |

### 7.4 Hardcoded `RsmBuilder.buildRSMFromText` — Printer/Golden Test Files

| # | File | ~Locations | Severity |
|---|------|-----------|----------|
| RV20 | `TexCompilationTests.fs` | ~7 | Medium |
| RV21 | `RnglrTests.fs` | 1 | Low |
| RV22 | `RnglrStepVisualizationTests.fs` | 1 | Low |
| RV23 | `GssDotVisualizationTests.fs` | 1 | Low |

**Total: ~46 hardcoded `RsmBuilder.buildRSMFromText` calls across 7 files.**

### 7.5 `TestHelpers.grammarToRsm` Bypasses Pre-built `g.Rsm` + Hardcoded Utilities

| # | Description | ~Locations | Severity |
|---|-------------|-----------|----------|
| RV24 | `TestHelpers.grammarToRsm` reconstructs RSM from text when `AnnotatedGrammar.Rsm` already exists. Called in `GllTests.fs` (~11), `RnglrTests.fs` (~6), `CrossParserEquivalenceTests.fs` (~6), `ParsingTestCases.fs:177`, `PathIndexTeXTests.fs` (~4). Should use `g.Rsm` directly. | ~28 | Medium |
| RV25 | `TestHelpers.fs` itself hardcodes `grammarToEbnfText` (line 87), `grammarToRsm` (line 107), `buildRegexRsm` (line 129). These utility functions bypass the registry. Fix: remove `grammarToRsm` (replaced by `g.Rsm`), move `grammarToEbnfText` to registry (or delete — callers should use `AnnotatedGrammar.Text`). | 3 | Medium |
| RV26 | `grammarToEbnfText` duplicated in `LanguageRegistry.fs:~50` and `TestHelpers.fs:87`. | 2 | Low |

### 7.6 `data/*.bnf` Files Duplicate Registry Grammar Definitions

| # | Description | Severity |
|---|-------------|----------|
| RV27 | 7 `.bnf` files in `data/` duplicate LanguageRegistry entries: `example_grammar.bnf` (Dyck1 grammar1), `example_grammar_amb.bnf` (Dyck1 grammar2), `example_grammar_a_a_a.bnf` (APlus grammar5), `example_grammar_an_bn.bnf` (ANBN), `example_grammar_chain.bnf`, `example_grammar_simple.bnf`, `example_lr_grammar.bnf` (ArithExpr grammar7). Used by `FLPQ.Cli.Tests`. Fix: CLI tests should generate temp files from the registry or read `.Text`. | Low |
