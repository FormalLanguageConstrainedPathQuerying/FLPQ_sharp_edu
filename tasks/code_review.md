# Code Review Report

## Scope

Reviewed all `.fs` source files (31 in `src/`, 61 in `tests/`).
Report generated: 2026-07-08. Prior reports: 2026-07-05, 2026-07-01, 2026-06-30, 2026-06-29.
**Status: analysis only — no fixes applied by this task.**

---

## 1. Architectural Issues

### 1.1 [OPEN] FSharpPlus listed as dependency for GraphAnalysis but not used

`FLPQ.GraphAnalysis.fsproj` includes `<PackageReference Include="FSharpPlus" Version="1.9.1" />` but neither `Graph.fs` nor `MsBfs.fs` uses any FSharpPlus types (no `NonEmptyList`, `NonEmptySet`, etc.).

**Suggested fix**: Remove the unused package reference.

### 1.2 [NEW] collectGraphEdges duplicated between Gll.fs and Rnglr.fs

Identical function `collectGraphEdges` (`Graph<int, Option<'t>> -> ResizeArray<'t * int>[]`) is defined in both:
- `Gll.fs:49–58` (inside `module GLL`)
- `Rnglr.fs:35–44` (inside `module Rnglr`)

Both iterate over the edge matrix and collect `(terminal, targetVertex)` pairs per source vertex. Same signature, same logic.

**Suggested fix**: Extract to a shared module (e.g., `GraphAnalysis.Graph` or a new `FLPQ.Languages.GraphUtils` module).

### 1.3 [NEW] Test helpers duplicated between RnglrTests.fs and GllTests.fs

Two substantial helper functions are copy-pasted:
- `grammarToEbnfText` (`RnglrTests.fs:11–29`, `GllTests.fs:13–31`) — converts a BNF grammar to EBNF text for RSM builder
- `grammarToRsm` (`RnglrTests.fs:31–33`, `GllTests.fs:35–37`) — wraps `grammarToEbnfText` + `RsmBuilder.buildRSMFromText`

Additionally, supporting functions `stringToTerminals`/`stringToChars`, `inputToGraph`/`terminalsToGraph`, `rnglrAccepts`/`gllAccepts`, `cykAccepts`, and `nonEpsilon` are duplicated or near-duplicated.

**Suggested fix**: Move to `FLPQ.TestUtilities` or a shared `TestHelpers.fs` in `FLPQ.Languages.Tests`.

### 1.4 [NEW] GraphReader in FLPQ.RPQ — questionable placement

`GraphReader.fs` parses graph files into `NFA<string, int>`. It lives in `FLPQ.RPQ` but its output type (`NFA`) is from `FLPQ.Languages`. The graph-reading concern could arguably belong in `FLPQ.GraphAnalysis` or `FLPQ.Languages`.

---

## 2. Code Quality and Duplication

### 2.1 [OPEN] GraphTests.fs has duplicate test blocks

`GraphTests.fs` lines 63–72 (`filterOutgoing empty set`) and lines 74–83 (`filterIncoming empty set`) are structurally identical, differing only in the function called. The verification loop is copy-pasted.

**Suggested fix**: Extract `checkAllEdgesFalse`.

### 2.2 [OPEN] Graph.edges compared via Assert.Equal may not work reliably

`GraphTests.fs:55` uses `Assert.Equal(g.Edges, filtered.Edges)` where `.Edges` is `Matrix<bool>`. The `Data` field inside `Matrix` is `'a[,]` — .NET array equality is reference equality. This test happens to pass because `filterOutgoing` with all vertices returns the source matrix, but the comparison is fragile.

**Suggested fix**: Compare element-by-element or define `Equals`/`GetHashCode` on `Matrix<'a>`.

### 2.3 [OPEN] Submatrix field naming inconsistency

`Submatrix` record (`Valiant.fs:7`): `{ Row: int; Col: int; Size: int }`. Fields `Row` and `Col` use PascalCase but the project convention specifies camelCase for record fields.

**Suggested fix**: Rename to `{ row: int; col: int; size: int }`.

### 2.4 [OPEN] BooleanDecomposition.decompose and decomposeNonEmptySet structurally similar

`BooleanDecomposition.fs`: `decompose` (lines 12–27) and `decomposeNonEmptySet` (lines 32–52) share the same structure: collect distinct elements via comprehension, then map each element to a boolean matrix. Only cell-access logic differs.

**Suggested fix**: Extract a common helper parameterized by a cell-test function.

### 2.5 [NEW] TokenStringGenerators in TokenizerTests instead of shared Generators

`TokenizerTests.fs` defines `TokenStringGenerators` locally (a FsCheck `Arbitrary<string>` type) instead of placing it in the shared `FLPQ.TestUtilities/Generators.fs`. This violates the project rule: "FsCheck generators for shared project types must live in a common `Generators.fs` module."

**Suggested fix**: Move to `Generators.fs` if the generator is useful elsewhere, or rename to indicate it's test-local.

---

## 3. Naming and Style

### 3.1 Module names shadowing types

Module names `Nfa`/`Dfa` (lowercase 'a') chosen to avoid conflicting with type names `NFA`/`DFA`. Requires careful qualification within `Automaton.fs`.

### 3.2 Trace type locations

All trace types are colocated with their respective algorithm files (`LLParser.fs`, `LRParser.fs`, `Cyk.fs`, `Valiant.fs`). `VisualizationStep` (bridge type) lives in `FLPQ.Printers/VisualizationTypes.fs`. Consistent convention.

### 3.3 RsmSymbol uses RequireQualifiedAccess

`RsmSymbol` uses `[<RequireQualifiedAccess>]` with `RTerm`/`RNonterm` prefixes, while `Symbol<'t,'nt>` uses unqualified `T`/`N`/`Epsilon`. Deliberate — `Symbol` is used pervasively in pattern matching.

### 3.4 [NEW] LR0Item/LR1Item field casing inconsistent with RnglrItem

`RnglrItem<'nt>` (`RnglrTypes.fs:10–12`) uses camelCase fields: `{ blockNonterminal; rsmState }`. However, `LR0Item`/`LR1Item` (`LRParser.fs:8–19`) use PascalCase: `{ Lhs; Rhs; Dot }`.

**Suggested fix**: Rename `LR0Item`/`LR1Item` fields to camelCase (`lhs`, `rhs`, `dot`, `lookahead`). Note: task 44 claims this was done, but current code still uses PascalCase.

---

## 4. Test Coverage Gaps

### 4.1 [CRITICAL] Stubbed tests in RnglrTests.fs (3 empty-body tests)

`RnglrTests.fs` lines 175–188 contain three `[<Fact>]` tests with empty bodies `()`:
- Line 176: ``S -> a S b | eps accepts a a b b`` — comment: "grammar2 with S -> S S creates unbounded DFA states"
- Line 180: ``S -> a S b | eps rejects a a b`` — same comment
- Line 185: ``S -> a S b | eps | S S accepts a b a b`` — similar comment

These tests pass silently (empty body = no assertion = always passes). They violate the project rule: "No stubbed tests: no Assert(true) and similar, no commented checks, no empty test body, no tests without checks."

**Suggested fix**: Either implement the tests or use `[<Fact(Skip="grammar2 with S -> S S creates unbounded DFA states")>]` to make the skip visible in test output. Per project rules: "A Skip is visible in test output; an empty body silently produces a false positive."

### 4.2 [OPEN] Property tests mislabeled as property-based

**4.2.1 GrammarTests.toCnf**: `[<Property(MaxTest = 100)>]` at line 383 iterates over 7 hardcoded grammars against 13 hardcoded test strings (91 checks × 100 iterations = 9100 identical comparisons). Ignores FsCheck-generated inputs entirely.

**4.2.2 FirstFollowTests.firstK**: `[<Property(MaxTest = 200)>]` iterates over 5 hardcoded grammars. Checks superset (not exact equality), so could pass vacuously if computed set is too large.

**Suggested fix**: Convert to `[<Fact>]` since they don't use FsCheck generation, or add true random-string generation with proper FsCheck generators.

### 4.3 [OPEN] LR(0) table "reduce on everything" produces conflicts silently

`buildLR0Table` adds reduce actions for every terminal in every state with a completed item. Conflicts are collected but the parser uses first-inserted action via `Map.tryFind`. No test verifies parsing behavior with a conflicting table.

### 4.4 [OPEN] LL(k>1) has no property-based equivalence tests

`LLHigherKTests` module has `[<Fact>]` tests for k=2 and k=3 grammars with hardcoded strings. No property-based test comparing LL(k>1) against CYK/Valiant using FsCheck-generated inputs.

### 4.5 [OPEN] No property tests for grammars 9 and 10

Grammar 9 (ambiguous: `{a,b,c,x,y}` alphabet) and grammar 10 have only `[<Fact>]` acceptance tests. No equivalence tests against CYK or Valiant. No FsCheck string generator exists for the `{a,b,c,x,y}` alphabet.

### 4.6 [OPEN] No explicit Nfa.epsilonClosure direct unit test

`Nfa.epsilonClosure` is only exercised indirectly through `Nfa.accept` and `Automaton.toDfa`. No standalone test verifies closure correctness for epsilon cycles, multi-step epsilon chains, or self-loops.

### 4.7 [OPEN] RPQ tests use only {"a","b"} alphabet

All RPQ generators hardcode labels as `[ "a"; "b" ]`. No tests with larger alphabets, numeric labels, or special characters.

### 4.8 [NEW] Graph operations have no property-based tests

`GraphTests.fs` has only `[<Fact>]` tests. No property-based tests for `filterOutgoing`, `filterIncoming`, `keepVertices`, `mapVertices`, `mapEdges`, or `fromEdges`.

**Suggested fix**: Add property tests verifying invariants (e.g., `keepVertices` preserves edge connectivity, `mapVertices` preserves structure).

### 4.9 [NEW] BooleanDecomposition.recompose has no property test for dimension consistency

`BooleanDecompositionTests.fs` has a `decompose then recompose is identity` property test, but no test verifying that all matrices in the decomposition have the same dimensions as the original matrix.

---

## 5. Visualization and Printers

### 5.1 LRTableTeX.allActionsFor reconstructs conflicts from conflict list

`allActionsFor` (`LRTableTeX.fs:15–30`) enumerates conflicts from `table.Conflicts` to augment actions from `table.Action`. When a conflict exists, only the first-inserted action is stored in the map. The visualization may show actions that the parser never takes.

### 5.2 [NEW] GoldenHelpers.verifyGolden creates golden files on first run

`GoldenHelpers.fs:13–28`: `verifyGolden` writes the generated output to the golden file if it doesn't exist, then compares. First test run _creates_ the golden file and passes.

**Risk**: If the first run produces incorrect output (e.g., from a bug), the golden file captures the buggy output and all subsequent tests pass silently.

---

## 6. Genericity and Type Safety

### 6.1 [NEW] RsmToGrammar.convert hardcoded to string

`RsmToGrammar.convert` (`RsmToGrammar.fs:23`) has signature `RSM<string, string> -> Grammar<string, string>`. The `ntName` helper uses `sprintf` to construct nonterminal names. Cannot work with generic `'t`, `'nt` types.

**Impact**: RSM-to-grammar conversion cannot be used in a generic parsing pipeline.

### 6.2 [NEW] EbnfParser and RsmBuilder hardcoded to string

`EbnfParser.parseEbnf` returns `(Nonterminal<string> * Regexp<string, string>) list`. `RsmBuilder.buildRSM` takes `Map<Nonterminal<string>, Regexp<string, string>>`. Inherently string-bound because they parse text input — acceptable for text parsers, but means the EBNF→RSM pipeline cannot be composed with generic grammars.

### 6.3 [NEW] Grammar.parseGrammar hardcoded to string

`Grammar.parseGrammar` (`Grammar.fs:97`) returns `Grammar<string, string>`. The `classifyToken` helper uses `System.Char.IsUpper`. Inherently string-bound — acceptable for a text parser.

---

## 7. Suggestions Prioritized

| Priority | Issue | Section | Effort |
|----------|-------|---------|--------|
| **Critical** | Fix 3 stubbed tests in RnglrTests.fs (empty body) | 4.1 | Small |
| **High** | Fix `GrammarTests.toCnf` property test: convert to `[<Fact>]` or use true FsCheck | 4.2 | Medium |
| **High** | Fix `FirstFollowTests.firstK` property test: same; check exact equality | 4.2 | Medium |
| **High** | Deduplicate `collectGraphEdges` between Gll.fs and Rnglr.fs | 1.2 | Small |
| **High** | Deduplicate test helpers between RnglrTests.fs and GllTests.fs | 1.3 | Medium |
| **Medium** | Fix Submatrix `Size` → `size` naming inconsistency | 2.3 | Small |
| **Medium** | Fix LR0Item/LR1Item field casing (PascalCase → camelCase) | 3.4 | Small |
| **Medium** | Define LR parse behavior when conflicts exist | 4.3 | Small |
| **Medium** | Add LL(k>1) property-based equivalence tests against CYK/Valiant | 4.4 | Medium |
| **Medium** | Add property tests for grammars 9 and 10 equivalence | 4.5 | Medium |
| **Medium** | Add FsCheck string generator for `{a,b,c,x,y}` alphabet | 4.5 | Small |
| **Medium** | Add property-based tests for Graph operations | 4.8 | Medium |
| **Low** | Remove unused `FSharpPlus` from `FLPQ.GraphAnalysis.fsproj` | 1.1 | Small |
| **Low** | Fix `GraphTests.fs` `Assert.Equal(g.Edges, ...)` — element-by-element | 2.2 | Small |
| **Low** | Deduplicate `GraphTests.fs` empty-set filter test body | 2.1 | Small |
| **Low** | Deduplicate `BooleanDecomposition.decompose` / `decomposeNonEmptySet` | 2.4 | Medium |
| **Low** | Move `TokenStringGenerators` to shared `Generators.fs` | 2.5 | Small |
| **Low** | Add direct unit test for `Nfa.epsilonClosure` | 4.6 | Small |
| **Low** | Extend RPQ test alphabet beyond `{"a","b"}` | 4.7 | Medium |
| **Low** | Add dimension-consistency property test for BooleanDecomposition | 4.9 | Small |
| **Low** | Document GoldenHelpers.verifyGolden risk (captures buggy output) | 5.2 | Small |

---

## 8. Issues Resolved

Brief summary of issues confirmed resolved from prior reports:

| Task | Issue | Status |
|------|-------|--------|
| 40 | CYK/Valiant hardcoded to `string` | RESOLVED — generic over `'t, 'nt` |
| 41 | LR table builder duplication | RESOLVED — shared `populateShiftGoto` |
| 42 | Valiant bypasses BooleanDecomposition | RESOLVED — uses `decompose`/`recompose` |
| 43 | checkDotCompiles duplication | RESOLVED — delegates to `checkDotCompilesWithInfo` |
| 44 | LR0Item/LR1Item field casing | **NOT RESOLVED** — still PascalCase (see §3.4) |
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
| 128 | Property-based equivalence tests | PARTIAL — exist but mislabeled (see §4.2) |
| 129 | Fill golden test gaps | RESOLVED — LL, Matrix, Automaton, etc. |
| 130 | LR conflict behavior tests | RESOLVED — conflict detection/resolution |
| 131 | BinaryPair struct, RsmDfa alias | RESOLVED |
| 132 | LL(k>1) tests, modified Valiant empty-input | PARTIAL — Fact tests only, no properties (see §4.4) |

Additional resolved items: CYK uses immutable `Set` (no mutable `HashSet`), `Submatrix` fields renamed from `A`/`B` to `Row`/`Col`, `buildLR0`/`buildLR1` delegate to generic `buildLR`, `Dfa.alphabet` delegates to `Nfa.collectAlphabet`.
