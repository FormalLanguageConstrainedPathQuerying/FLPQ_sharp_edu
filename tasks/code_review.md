# Code Review Report

## Scope

Reviewed all `.fs` source files (31 in `src/`, 61 in `tests/`).  
Report generated: 2026-07-05. Prior reports: 2026-07-01, 2026-06-30, 2026-06-29.  
**Status: analysis only — no fixes applied by this task.**

---

## Status Changes Since Last Report (2026-07-01)

The following issues from the prior report have been resolved (confirmed by code inspection):

| Issue | Task | Resolution |
|-------|------|------------|
| Matrix.data public mutable field | 122 | Now private; `get`/`set` exposed via `Matrix` module |
| Dfa.alphabet constructs temp NFA | — | Delegates cleanly to shared `Nfa.collectAlphabet` |
| LRAutomaton buildLR0/buildLR1 duplication | 119/120 | Both delegate to generic `buildLR` |
| Valiant init block duplication | 111 | `initValiant` shared by standard and modified variants |
| RsmBlock state type always int | 131 | `RsmDfa<'t,'nt>` type alias defined at `RSM.fs:14` |
| CYK uses mutable HashSet | — | Now uses immutable F# `Set<Nonterminal<'nt>>` |
| Submatrix field naming `A`/`B` | 121 | Renamed to `row`/`col` |
| nonterminalsOf/terminalsOf duplicated | 119/123 | Public in `Grammar.fs`; duplicates removed |
| Rhs.toList/toSymbols ambiguities | 124 | Renamed to `toListWithEpsilon`/`toNonEpsilonList` |
| Unused definitions | 123 | All removed |
| readIfExists duplicate definition | 123 | Consolidated in `SummaryTeX.fs` |
| collectSteps duplicate implementation | 123 | Single public function in `SummaryTeX.fs` |
| termPrinter duplicate lambda | 123 | Extracted to `TeXRenderer.termPrinterFromSymbolVisualizer` |
| escapeLabel not reused in AutomatonDot | 123 | Now public, used by `AutomatonDot.fs` |
| SummaryKind double-mapping | 123 | `SummaryKind` DU with `toString` member in `SummaryTeX.fs` |
| LLTableTeX duplicates nonterminalsOf/terminalsOf | 119 | Uses public versions from `Grammar.fs` |
| No test for LL(k>1) | 132 | `LLHigherKTests` module with k=2 and k=3 fact tests |
| Valiant empty-input not tested | 132 | Empty-input tests for standard and modified Valiant |
| Cross-algorithm RPQ uses single-label DFAs only | 115 | `RegexPropertyTests` module uses random regex DFAs |
| Property tests for toCnf, FirstFollow, NFA→DFA | 128 | NFA→DFA: proper FsCheck property tests exist; toCnf/FirstFollow: see new issues N1/N2 below |

---

## 1. Architectural Issues

### 1.1 [PREVIOUSLY 1.1] Matrix.data — RESOLVED

`Matrix<'a>.data` is now a private field. All external access goes through `Matrix.get`/`Matrix.set`. Code throughout the project uses these accessors consistently.

### 1.2 [PREVIOUSLY 1.2] Dfa.alphabet — RESOLVED

`Dfa.alphabet` (`Automaton.fs:290`) delegates to `Nfa.collectAlphabet a.transitions`, which scans all matrix cells and extracts `ATerm` labels (skipping `AEpsilon`). Since DFA has no epsilon edges, the epsilon branch is dead code but harmless. No temporary NFA record is constructed.

### 1.3 [PREVIOUSLY 1.3] LRAutomaton.buildLR0/buildLR1 — RESOLVED

A generic `buildLR` function (`LRParser.fs:181–197`) accepts closure and goto functions as higher-order parameters. Both `buildLR0` (lines 200–217) and `buildLR1` (lines 221–241) are thin wrappers (~18–21 lines each) that delegate to `buildLR`. No duplication.

### 1.4 [PREVIOUSLY 1.4] Valiant init block — RESOLVED

`initValiant` (`Valiant.fs:325`) is called by both `parseWithTrace` (line 379) and `parseModifiedWithTrace` (line 418). The modified variant destructures fields from the opaque init record for its algorithm-specific use, but the initialization itself is fully shared.

### 1.5 [PREVIOUSLY 1.5] RsmBlock state type — RESOLVED

`RsmDfa<'t,'nt>` type alias is defined at `RSM.fs:14` as `DFA<RsmSymbol<'t,'nt>, int>` and used as the type of `RsmBlock.dfa`.

### 1.6 [NEW] FSharpPlus listed as dependency for GraphAnalysis but not used

`FLPQ.GraphAnalysis.fsproj` includes `<PackageReference Include="FSharpPlus" Version="1.9.1" />` but neither `Graph.fs` nor `MsBfs.fs` uses any FSharpPlus types (no `NonEmptyList`, `NonEmptySet`, etc.).

**Suggested fix**: Remove the unused package reference.

---

## 2. Code Quality and Duplication

### 2.1 [NEW] GraphTests.fs has duplicate test blocks

`GraphTests.fs` lines 63–72 (`filterOutgoing empty set`) and lines 74–83 (`filterIncoming empty set`) are structurally identical, differing only in the function called (`Graph.filterOutgoing` vs `Graph.filterIncoming`). The outer verification loop (`for i in 0..2 do for j in 0..2 do Assert.False(...)`) is copy-pasted.

**Suggested fix**: Extract the verification loop into a shared helper `checkAllEdgesFalse`.

### 2.2 [NEW] Graph.edges compared via Assert.Equal may not work reliably

`GraphTests.fs:55` uses `Assert.Equal(g.edges, filtered.edges)` where `.edges` is `Matrix<bool>`. While `Matrix<'a>` is a record and F# record equality is structural, the `data` field inside `Matrix` is `'a[,]`. .NET array equality is reference equality by default — two distinct arrays with identical content would fail comparison. This test happens to pass because `filterOutgoing` with all vertices returns the source matrix, but the comparison is fragile.

**Suggested fix**: Compare element-by-element or define `Equals`/`GetHashCode` on `Matrix<'a>`.

### 2.3 Submatrix field naming inconsistency

`Submatrix` record (`Valiant.fs:7`): `{ row: int; col: int; Size: int }`. Fields `row` and `col` use camelCase but `Size` uses PascalCase — inconsistent within a single record type. The project conventions specify camelCase for values/fields.

**Suggested fix**: Rename `Size` → `size`.

### 2.4 [NEW] BooleanDecomposition.decompose and decomposeNonEmptySet structurally similar

`BooleanDecomposition.fs`: `decompose` (lines 13–30) and `decomposeNonEmptySet` (lines 35–56) share the same structure: collect distinct elements via comprehension, then map each element to a boolean matrix. The only difference is how cells are accessed (`Set.contains` vs pattern-matching `NonEmptySet.contains`).

**Suggested fix**: Extract a common helper parameterized by a cell-test function.

---

## 3. Naming and Style

### 3.1 Module names shadowing types

Module names `Nfa`/`Dfa` (lowercase 'a') chosen to avoid conflicting with type names `NFA`/`DFA`. `Nfa.alphabet` and `Dfa.alphabet` must be carefully qualified to avoid type/module ambiguity. Works fine across files but requires care within `Automaton.fs` itself.

### 3.2 Trace type locations inconsistent

| Trace type | Defined in |
|-----------|-----------|
| `LLParsingStep` | `FLPQ.Languages/LLParser.fs` |
| `LRParsingStep` | `FLPQ.Languages/LRParser.fs` |
| `LRStackFrame` | `FLPQ.Languages/LRParser.fs` |
| `CykTraceStep` | `FLPQ.Languages/Cyk.fs` |
| `ValiantTraceStep` | `FLPQ.Languages/Valiant.fs` |
| `ModifiedValiantTraceStep` | `FLPQ.Languages/Valiant.fs` |

No common convention for where trace-step types are defined. All trace types are colocated with their respective algorithm files, which is a reasonable convention. `VisualizationStep` (the bridge type) lives in `FLPQ.Printers/VisualizationTypes.fs`. The printers project must reference `FLPQ.Languages` to import these trace types.

### 3.3 File naming

`VisualizationTypes.fs` contains only the `VisualizationStep` struct. The name is accurate, though the file is in `FLPQ.Printers` while the LL/LR parser step types it bridges to are in `FLPQ.Languages`.

### 3.4 RsmSymbol uses RequireQualifiedAccess

`RsmSymbol` uses `[<RequireQualifiedAccess>]` with `RTerm`/`RNonterm` prefixes, while `Symbol<'t,'nt>` uses `T`/`N`/`Epsilon` unqualified. Inconsistent access patterns within the same namespace. This is deliberate — `Symbol` is used pervasively in pattern matching throughout the codebase, while `RsmSymbol` is a narrower concern — but the inconsistency is worth noting.

---

## 4. Test Coverage Gaps

### 4.1 Missing TeX/DOT runtime checks

`TexCompilationTests.fs`, `ExternalToolsTests.fs`, and all visualization tests assume `lualatex`/`dot` are on `PATH`. No runtime guard or skip if the external tool is absent. Tests use `[<Trait("Category", "TeX")>]` for filtering, but tests still fail if run without the tool in any configuration.

### 4.2 [NEW] Property test quality issues — mislabeled as property-based

**4.2.1 GrammarTests.toCnf**: Labelled `[<Property(MaxTest=100)>]` but the test body ignores FsCheck-generated inputs and iterates over 7 hardcoded grammars against 13 hardcoded test strings. The `MaxTest=100` parameter is misleading — it re-runs the same 91 (7×13) checks 100 times. This provides no additional coverage beyond a single run.

**4.2.2 FirstFollowTests.firstK**: Labelled `[<Property(MaxTest=200)>]` but ignores generated inputs and iterates over 5 hardcoded grammars. Additionally, the test checks that `computedFirst` is a superset of the brute-force derivation result, not exact equality — this could pass vacuously if the computed set is too large.

**Suggested fix**: Either convert to `[<Fact>]` (iterating over grammars once) since they don't use FsCheck generation, or add true random-string generation with proper FsCheck generators.

### 4.3 LR(0) table "reduce on everything" produces conflicts silently

`buildLR0Table` adds reduce actions for every terminal (including epsilon) in every state with a completed item. This generates many ShiftReduce/ReduceReduce conflicts. The conflicts are collected in `LRTable.conflicts` but the parser still attempts to use the table — with conflicts populated, the behavior is undefined (first-inserted action wins via `Map.tryFind`). Task 130 added conflict behavior fact tests, but there is no test that verifies parsing with a conflicting table produces a predictable, documented error or fallback behavior.

### 4.4 [NEW] LL(k>1) has no property-based equivalence tests

`LLHigherKTests` module has `[<Fact>]` tests for k=2 and k=3 grammars with hardcoded strings. No property-based test comparing LL(k>1) against CYK/Valiant using FsCheck-generated inputs.

**Suggested fix**: Add `[<Property>]` tests using FsCheck string generators for the k=2 and k=3 grammars, checking equivalence with CYK and Valiant.

### 4.5 [NEW] No property tests for grammars 9 and 10

Grammar 9 (ambiguous: `{a,b,c,x,y}` alphabet) and grammar 10 have only `[<Fact>]` acceptance tests in `LLParserTests.fs`. No equivalence tests against CYK or Valiant. Additionally, no FsCheck string generator exists for the `{a,b,c,x,y}` alphabet — adding one would enable property-based equivalence tests.

**Suggested fix**: Add a `Grammar9StringGenerators` module in `Generators.fs` and use it for property-based equivalence tests.

### 4.6 MsBfs property test naming

`MsBfsTests.fs` uses `[<Properties(Arbitrary = [| typeof<RandomGraphGenerators> |])>]`. `RandomGraphGenerators` is defined in `FLPQ.TestUtilities/Generators.fs` as a proper FsCheck `Arbitrary` type, consistent with the rest of the project.

### 4.7 [NEW] No explicit Nfa.epsilonClosure direct unit test

`Nfa.epsilonClosure` is only exercised indirectly through `Nfa.accept` and `Automaton.toDfa`. No standalone test verifies closure correctness for epsilon cycles, multi-step epsilon chains, or self-loops.

**Suggested fix**: Add direct unit tests for epsilon closure edge cases.

### 4.8 [NEW] RPQ tests use only {"a","b"} alphabet

All RPQ generators (`RPQGenerators`, `StressRpqGenerators`, `RegexAndGraphGenerators`) hardcode labels as `[ "a"; "b" ]`. No tests with larger alphabets, numeric labels, or special characters. The graph reader and RPQ algorithms are generic over terminal type, so this is a test-only limitation.

**Suggested fix**: Generate alphabets of varying sizes and character sets in RPQ generators.

---

## 5. Visualization and Printers

### 5.1 LRTableTeX.allActionsFor reconstructs conflicts from conflict list

`allActionsFor` (`LRTableTeX.fs:15–30`) enumerates conflicts from `table.conflicts` list to augment actions from `table.action`. When a conflict exists in the action map, only the first-inserted action is stored (by `Map.tryFind`). The visualization may show actions (e.g., both shift and reduce) that the parser never takes, because the parser uses only the first action inserted into the map. This creates a mismatch between the visualized table and the actual parser behavior. The approach is reasonable for visualization purposes (it shows what conflicts were detected), but the discrepancy should be documented.

---

## 6. Suggestions Prioritized

| Priority | Issue | Effort |
|----------|-------|--------|
| **High** | Fix `GrammarTests.toCnf` property test: either use true FsCheck-generated random strings or convert to `[<Fact>]` | Medium |
| **High** | Fix `FirstFollowTests.firstK` property test: same issue; also check exact equality, not superset | Medium |
| **Medium** | Fix Submatrix `Size` → `size` naming inconsistency | Small |
| **Medium** | Define LR parse behavior when conflicts exist (fail vs best-effort) | Small |
| **Medium** | Add LL(k>1) property-based equivalence tests against CYK/Valiant | Medium |
| **Medium** | Add property tests for grammars 9 and 10 equivalence against CYK/Valiant | Medium |
| **Medium** | Add FsCheck string generator for `{a,b,c,x,y}` alphabet | Small |
| **Low** | Remove unused `FSharpPlus` dependency from `FLPQ.GraphAnalysis.fsproj` | Small |
| **Low** | Fix `GraphTests.fs` `Assert.Equal(g.edges, ...)` — compare element-by-element or define `Equals` | Small |
| **Low** | Deduplicate `GraphTests.fs` empty-set filter test body | Small |
| **Low** | Deduplicate `BooleanDecomposition.decompose` / `decomposeNonEmptySet` | Medium |
| **Low** | Add direct unit test for `Nfa.epsilonClosure` | Small |
| **Low** | Extend RPQ test alphabet beyond `{"a","b"}` | Medium |
| **Low** | Add runtime guards for `lualatex`/`dot` absence in visualization tests | Small |
| **Low** | Standardize trace-type locations across parsers | Medium |

---

## 7. Issues Resolved (Tasks 40–45, 64–66, 111–132)

These issues from prior reports are now resolved:

| Task | Issue | Resolution |
|------|-------|------------|
| 40 | CYK/Valiant hardcoded to `string` | Made `Cyk` and `Valiant` generic over `'t, 'nt`; accept pre-tokenized input |
| 41 | `buildLR0Table`/`buildSLR1Table`/`buildCLR1Table` duplication | Extracted shared `populateShiftGoto` helper |
| 42 | Valiant bypasses `BooleanDecomposition` | Valiant now uses `decompose` for initial matrix and `recompose` for result extraction |
| 43 | `checkDotCompiles`/`checkDotCompilesWithInfo` duplication | `checkDotCompiles` delegates to `checkDotCompilesWithInfo` |
| 44 | `LR0Item`/`LR1Item` PascalCase fields | Renamed to camelCase: `lhs`, `rhs`, `dot`, `lookahead` |
| 45 | `LRParserTests.fs` submodule duplication | Extracted parameterized test helpers (`testAcceptReject`, `testLeaves`, etc.) |
| 64 | RPQ in separate modules | Moved RPQ to own project, MS-BFS to GraphAnalysis, unified `evaluate` interface |
| 65 | Various refactoring | Parsing input types, LR code duplication, FsCheck generators, CLI code dedup |
| 66 | Printers separation | Moved printers to separate `FLPQ.Printers` project |
| 111 | Valiant Seq.head crash, deduplicate init | `initValiant` shared, guard for empty decomposition |
| 112 | `string` with printer-function parameters in SymbolTeX | SymbolTeX uses `nonterminalPrinter`/`terminalPrinter` functions |
| 113 | CYK core deduplication | Parameterized `cykCore` helper with callbacks |
| 114 | Shared `Generators.fs` module | Created `FLPQ.TestUtilities` with all FsCheck generators |
| 115 | RPQ cross-algorithm equivalence tests | `RegexPropertyTests` module with random regex DFAs |
| 116 | Tokenizer unit tests | Dedicated `TokenizerTests.fs` with property tests |
| 117 | CLI tests project | Created `FLPQ.Cli.Tests`, moved `CliSummaryTests`, added runner tests |
| 118 | Large-input stress tests | Stress tests across all algorithm families |
| 119 | Deduplicate automaton infrastructure | `collectAlphabet`, `toDfa`, shared LR BFS |
| 120 | Reuse LR automaton in CLI runners | LR runners use `LRAutomaton` types |
| 121 | Naming and style fixes | `lr0AutomatonToTikz`, `isCompleted`, etc. |
| 122 | Matrix.data private | Get/set accessor functions, semantic color labels |
| 123 | Deduplicate miscellaneous helpers | `readIfExists`, `collectSteps`, `termPrinter`, `escapeLabel`, `SummaryKind` |
| 124 | Resolve Rhs.toList/toSymbols ambiguity | `toListWithEpsilon` / `toNonEpsilonList` |
| 125 | Move VisualizationStep and trace-type locations | `VisualizationStep` → `FLPQ.Printers/VisualizationTypes.fs` |
| 126 | XML documentation comments | Added to undocumented public APIs |
| 127 | Refactor SummaryTeX.fs | From mutable to functional pipelines |
| 128 | Property-based equivalence tests | toCnf, FirstFollow, NFA→DFA |
| 129 | Fill golden test gaps | LL table, Matrix, Automaton, Derivation tree, Valiant trace |
| 130 | LR conflict behavior tests | Added conflict detection and resolution tests |
| 131 | BinaryPair named struct, RsmDfa alias, RsmSymbol consistency | Completed |
| 132 | LL(k>1) tests, modified Valiant empty-input, move NFA/DFA tests | Completed |

Also resolved incidentally:
- `CYK` no longer uses mutable `HashSet` — now uses immutable F# `Set`.
- `SubmatrixBlock` (Matrix.fs): used by `Valiant.stepToTeX` (ValiantTeX.fs).
- `LinearAlgebra.kron`: used by `KroneckerRPQ.evaluate`.
- `writeDotFile`/`writeTexFile` duplication: merged into single `writeOutputFile` in `Program.fs`.
- Valiant tests cover grammar6 expression grammar.
- `Submatrix` fields renamed from `A`/`B` → `row`/`col`.
- `buildLR0`/`buildLR1` both delegate to generic `buildLR`.
- `Dfa.alphabet` delegates to `Nfa.collectAlphabet` without constructing a temporary NFA record.
