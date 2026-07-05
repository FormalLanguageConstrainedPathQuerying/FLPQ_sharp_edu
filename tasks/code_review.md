# Code Review Report

## Scope

Reviewed all `.fs` source files (31 in `src/`, 24 in `tests/`).  
Report generated: 2026-07-01. Prior reports: 2026-06-29, 2026-06-30.  
**Status: analysis only — no fixes applied by this task.**

---

## 1. Architectural Issues

### 1.1 Matrix.data is a public mutable field

`Matrix<'a>.data` is exposed as a public `'a[,]` field. Code throughout the project mutates it directly: `matrix.data.[i,j] <- value` in `Automaton.fs`, `Valiant.fs`, `Cyk.fs`, `BelyaninRPQ.fs`, `KroneckerRPQ.fs`, `MsBfs.fs`.

**Impact**: Breaks encapsulation. No guarantees about matrix invariants. Test code can (and does) bypass `Matrix` module functions entirely.

**Suggested fix**: Make `data` private, expose `get`/`set` functions in `Matrix` module.

### 1.2 Dfa.alphabet delegates to Nfa by constructing a temporary NFA

`Dfa.alphabet` (`Automaton.fs:172–177`) creates a fake `NFA` record on the fly just to reuse `Nfa.alphabet`. This is inefficient and conceptually misleading (a DFA is not an NFA).

**Suggested fix**: Duplicate the 5-line iteration logic directly in `Dfa.alphabet`, or extract a shared private helper.

### 1.3 LRAutomaton.buildLR0/buildLR1 near-identical BFS construction

Lines 160–194 and 224–257 in `LRParser.fs` are structurally identical except for `closureLR0` vs `closureLR1` and the final-state check. ~60 lines of duplicated state-exploration logic.

**Impact**: Bug fixes must be applied in 2 places. Any change to the BFS loop requires 2 edits.

**Suggested fix**: Extract the common BFS framework into a private helper parameterized by the closure function and accept-item construction.

### 1.4 Valiant init block duplicated between standard and modified variants

The Valiant initialization (building `tByNt` dictionary, `pByPair` dictionary, terminal rule setup via `BooleanDecomposition.decompose`) is duplicated ~4x:

| Function | Lines |
|----------|-------|
| `parseWithTable` | Relies on `initValiant` helper |
| `parseWithTrace` | Relies on `initValiant` helper |
| `parseModifiedWithTable` | 422–456 (inline) |
| `parseModifiedWithTrace` | 504–534 (inline) |

`initValiant` (`Valiant.fs:317–359`) exists but is only used by the standard variants. The modified variants copy the same ~35 lines inline.

**Suggested fix**: Either make `initValiant` suitable for both or create a shared initialization that returns a reusable data structure usable by both algorithms.

### 1.5 RsmBlock state type parameter is always `int`

`RsmBlock<'t,'nt>` contains `dfa: DFA<RsmSymbol<'t,'nt>, int>` — state names are always `[0..n-1]`. The `'s` parameter of `DFA` is unused flexibility. This propagates to `RsmBuilder.buildBlockDfa` which constructs `Dfa.fromTransitions [0..n-1]`. The EBNF-to-RSM pipeline never uses named states.

**Suggested fix**: Either make the state type configurable (parameterize `buildBlockDfa` and `RsmBlock`) or define a type alias `RsmDfa<'t,'nt> = DFA<RsmSymbol<'t,'nt>, int>`.

---

## 2. Code Quality and Duplication

### 2.1 nonterminalsOf/terminalsOf duplicated across modules [FIXED in task 119/123]

`Grammar.nonterminalsOf` and `Grammar.terminalsOf` are now public (no `private` modifier). `LLTableTeX.fs` uses the public versions directly. No duplicates remain.

### 2.2 Rhs.toList vs Rhs.toSymbols vs Rhs.length — three epsilon ambiguities [FIXED in task 124]

Fixed: `Rhs.toSymbols` → `Rhs.toNonEpsilonList` (returns `[]` for epsilon), `Rhs.toList` → `Rhs.toListWithEpsilon` (returns `[Epsilon]` for epsilon).

### 2.3 CYK uses mutable HashSet

`Cyk.CykCell = Option<HashSet<Symbol<'t,'nt>>>` uses `System.Collections.Generic.HashSet` with imperative accumulation (`accumulated.Add(N nt) |> ignore`). The rest of the codebase uses F# `Set` for purity.

**Justification (from project guidelines)**: "no nontrivial optimizations" — but F# `Set` would be equally functional and more consistent. The current choice introduces mutable state into otherwise functional code.

### 2.4 Valiant Submatrix field naming is cryptic

`Submatrix` type (`Valiant.fs:7`): fields `A`, `B`, `Size`. These represent row coordinate, column coordinate, and submatrix dimension, but a reader unfamiliar with the algorithm cannot infer this from names alone.

**Suggested fix**: Use `row`, `col`, `size` or `RowEnd`, `ColStart`, `Size`.

### 2.5 Unused definitions [FIXED in task 123]

| Location | Definition | Status |
|----------|-----------|--------|
| `Automaton.fs:91` | `buildDfaMatrix` | Already removed (never existed in current codebase) |
| `GrammarTests.fs:162` | `nonterminalsOfCnf` | Removed |
| `Generators.fs:233` | `StringArb` | Removed (never used) |

### 2.11 readIfExists duplicate definition [FIXED in task 123]

`readIfExists` was defined both in `FLPQ.Cli/Helpers.fs` (public) and `FLPQ.Printers/SummaryTeX.fs` (private). Removed from Helpers, made public in SummaryTeX. Helpers tests now use `SummaryTeX.readIfExists`.

### 2.12 collectSteps duplicate implementation [FIXED in task 123]

Step directory enumeration was implemented inline in `SummaryTeX.buildContent` and as a function in `FLPQ.Cli/Helpers.fs`. Extracted to a single public `collectSteps` in `SummaryTeX`, used by both.

### 2.14 termPrinter duplicate lambda [FIXED in task 123]

Identical `termPrinter` lambda (`(Terminal t) -> symbolVisualizer (T(Terminal t))`) existed in both `LLStepVisualizer` and `LRStepVisualizer`. Extracted to `TeXRenderer.termPrinterFromSymbolVisualizer`.

### 2.15 escapeLabel not reused in AutomatonDot [FIXED in task 123]

`DerivationTreeDot.escapeLabel` was private and not used in `AutomatonDot`, which had two inline `.Replace` calls. Made `escapeLabel` public; `AutomatonDot` now uses it.

### 2.17 SummaryKind double-mapping [FIXED in task 123]

The old `SummaryKind` DU (`TablePerStep | StackPerStep`) was insufficient, requiring a parallel `string` mapping ("table"/"ll"/"lr"). Replaced with `SummaryTeX.SummaryKind` (`TablePerStep | LL | LR`) with `toString` member. Moved from `FLPQ.Cli.Summary` to `FLPQ.Printers.SummaryTeX` to avoid circular dependency.

## 3. Naming and Style

### 3.1 Module names shadowing types

Module names `Nfa`/`Dfa` (lowercase 'a') chosen to avoid conflicting with type names `NFA`/`DFA`. `Nfa.alphabet` and `Dfa.alphabet` must be carefully qualified to avoid type/module ambiguity. Works fine across files but requires care within `Automaton.fs` itself.

### 3.2 Trace type locations inconsistent

| Trace type | Defined in |
|-----------|-----------|
| `LLParsingStep` | `VisualizationTypes.fs` |
| `LRParsingStep` | `VisualizationTypes.fs` |
| `LRStackFrame` | `VisualizationTypes.fs` |
| `CykTraceStep` | `Cyk.fs` |
| `ValiantTraceStep` | `Valiant.fs` |
| `ModifiedValiantTraceStep` | `Valiant.fs` |

No common convention for where trace-step types are defined. `CykTraceStep`/`Submatrix` live alongside algorithm logic, while LL/LR types are centralized.

### 3.3 File naming

`VisualizationTypes.fs` contains `LLParsingStep`, `LRParsingStep`, `LRStackFrame`, `StepInput`, `VisualizationStep`. The name "Types" implies only types, but the file also contains mutable state used by printer modules. Actually, checked — no mutable state here, but the name still doesn't signal that these are parser-intermediate structures.

### 3.4 RsmSymbol uses RequireQualifiedAccess

`RsmSymbol` uses `[<RequireQualifiedAccess>]` and prefixes `RTerm`/`RNonterm`, while `Symbol<'t,'nt>` uses `T`/`N`/`Epsilon` without qualification. Inconsistent use of discriminated union access patterns within the same namespace.

---

## 4. Test Coverage Gaps

### 4.1 Missing TeX/DOT runtime checks in tests

`TexCompilationTests.fs` and all `*VisualizationTests.fs` assume `pdflatex`/`dot` are on `PATH`. No runtime guard or skip if the external tool is absent. Tests use `[<Trait("Category", "TeX")>]` for filtering, but tests still fail if run without the tool in any configuration.

### 4.2 No property tests for crucial domains

| Module | Missing Property Tests |
|--------|----------------------|
| `Grammar.toCnf` | No property that CNF preserves language or that repeated `toCnf` is idempotent |
| `FirstFollow` | All `[<Fact>]` — no FsCheck properties for first/follow set correctness |
| `NFA/DFA` | No property that `toDfa` preserves language (or acceptance) |
| `AutomatonDot` | No property tests — all hardcoded automata |
| `RsmBuilder` | No property tests — only fact tests against hardcoded EBNF strings |

### 4.3 LR(0) table "reduce on everything" produces conflicts silently

`buildLR0Table` adds reduce actions for every terminal (including epsilon) in every state with a completed item. This generates many ShiftReduce/ReduceReduce conflicts. The conflicts are collected in `LRTable.conflicts` but the parser still attempts to use the table — with conflicts populated, the behavior is undefined (first-inserted action wins via `Map.tryFind`). No test verifies that parsing with a conflicting table fails predictably.

### 4.4 No test for higher-k LL parsing (k > 1)

All LL tests use `k=1`. The `buildTable` function accepts arbitrary `k` but is never tested with `k>1`.

### 4.5 Valiant `parseModifiedWithTable` not tested with empty input

Only non-empty inputs are tested for the modified Valiant variant. The standard `parseWithTable` handles empty input, but no test verifies the modified variant's empty-input path (line 458-463 in Valiant.fs).

### 4.6 MsBfs property test uses FsCheck correctly but naming is inconsistent

`MsBfsTests.fs` uses `[<Properties(Arbitrary = [| typeof<RandomGraphGenerators.RandomGraphGenerators> |])>]` which is defined in `RandomGraphGenerators.fs`. The generator uses `System.Random.Shared` internally — not a bug but different from other test files that use `MyGen`/`MyArb` aliases. No correctness issue but inconsistent style.

### 4.7 Cross-algorithm RPQ property tests use only single-label DFAs

`RPQTests.fs` property tests (section "Cross-algorithm property-based tests") compare Belyanin/Arroyuelo/Kronecker results using only a single `(0, "a", 1)` DFA. The property that all three algorithms agree is only tested for the most trivial query automaton. A more robust test would generate random DFAs.

---

## 5. VIsualization and Printers

### 5.1 LRTableTeX.allActionsFor reconstructs conflicts from conflict list

`allActionsFor` (`LRTableTeX.fs:15–30`) enumerates conflicts from `table.conflicts` list to augment actions from `table.action`. However, when a conflict exists in the action map, only the first-inserted action is stored (by `Map.tryFind`). The visualization may show actions (e.g., both shift and reduce) that the parser never takes, because the parser will only use the first action inserted into the map. This creates a mismatch between the visualized table and the actual parser behavior.

### 5.2 LLTableTeX duplicates nonterminalsOf/terminalsOf [FIXED in task 119]

As noted in 2.1, `LLTableTeX.fs` previously reimplemented `nonterminalsOf`/`terminalsOf`. Now uses the public versions from `Grammar.fs`.

---

## 6. Suggestions Prioritized

| Priority | Issue | Effort |
|----------|-------|--------|
| **High** | Deduplicate Valiant init block: make `initValiant` reusable by modified variant | Medium |
| **High** | Make `nonterminalsOf`/`terminalsOf` public in `Grammar.fs`; remove duplicates from `LLTableTeX.fs` | Small |
| **Medium** | Make `Matrix.data` private, add accessor functions | Medium |
| **Medium** | Deduplicate `LRAutomaton.buildLR0`/`buildLR1` BFS construction | Medium |
| **Medium** | Define LR parse behavior when conflicts exist (fail vs best-effort) | Small |
| **Medium** | Rename `Submatrix` fields `A`/`B` → `row`/`col` | Small |
| **Low** | Fix `Dfa.alphabet` to not construct temporary NFA | Small |
| **Low** | Remove unused `buildDfaMatrix` (`Automaton.fs:91`) and `nonterminalsOfCnf` (`GrammarTests.fs:162`) | Small — DONE task 123 |
| **Low** | Rename `Rhs.toSymbols` → `Rhs.toNonEpsilonList`; document distinction | Small — DONE task 124 |
| **Low** | Standardize trace-type locations across parsers | Medium |
| **Low** | Add property tests for `toCnf`, `FirstFollow`, automaton conversion | Large |
| **Low** | Add LL(k>1) tests | Medium |
| **Low** | Test empty input for `parseModifiedWithTable` | Small |
| **Low** | Generate random DFAs for cross-algorithm RPQ property tests | Medium |

---

## 7. Issues Resolved (Tasks 40–45, 64–66)

These issues from the prior report are now resolved:

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

Also resolved incidentally:
- `SubmatrixBlock` (Matrix.fs:61): now used by `Valiant.stepToTeX` (ValiantTeX.fs).
- `LinearAlgebra.kron`: now used by `KroneckerRPQ.evaluate`.
- `writeDotFile`/`writeTexFile` duplication: merged into single `writeOutputFile` in `Program.fs`.
- Valiant tests now cover grammar6 expression grammar.
