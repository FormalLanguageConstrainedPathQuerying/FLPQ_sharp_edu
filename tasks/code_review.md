# Code Review Report (Task 39)

## Scope

Reviewed all `.fs` source files (17 in `src/`, 1 in `src/FLPQ.Cli/`) and all `.fs` test files (15 in `tests/`).  
Report generated: 2026-06-29.  
**Status: analysis only — no fixes applied.**

---

## 1. Architectural Issues

### 1.1 Inconsistent genericity across parsing algorithms

`Grammar`, `LLParser`, `LRParser`, `FirstFollow`, `Automaton` are all generic over `'t` (terminal type) and `'nt` (nonterminal type). But `Cyk`, `Valiant`, and `Tokenizer` are hardcoded to `string`-based symbols (`Terminal<string>`, `Nonterminal<string>`, `Symbol<string,string>`).

**Impact**: Cannot reuse CYK/Valiant with custom terminal/nonterminal types (e.g., tokenized AST nodes, typed nonterminals). The `Tokenizer` is also string-specific, so all parsing paths go through `string` anyway — but the asymmetry is architecturally inconsistent.

**Suggested fix**: Make `Cyk` and `Valiant` modules generic over `'t,'nt`, or at least define type aliases for their hardcoded signatures.

### 1.2 Matrix.data is a public mutable field

`Matrix<'a>.data` is exposed as a public `'a[,]` field. Code throughout the project mutates it directly: `matrix.data.[i,j] <- value` in `Automaton.fs`, `Valiant.fs`, `Cyk.fs`.

**Impact**: Breaks encapsulation. No guarantees about matrix invariants. Test code can (and does) bypass `Matrix` module functions entirely.

**Suggested fix**: Make `data` private, expose `get`/`set` functions in `Matrix` module.

### 1.3 Dfa.alphabet/stateCount delegates to Nfa by constructing a temporary NFA

`Dfa.alphabet` and `Dfa.stateCount` create a fake `NFA` record on the fly just to reuse `Nfa.alphabet`/`Nfa.stateCount`. This is inefficient and conceptually misleading (a DFA is not an NFA).

**Suggested fix**: Duplicate the 5-line iteration logic directly in `Dfa.alphabet`, or extract a shared private helper.

### 1.4 LRParser table builders have heavy code duplication

`buildLR0Table`, `buildSLR1Table`, `buildCLR1Table` share ~80% identical logic for shift/goto processing from the automaton transitions. Only the reduce-action placement differs (all terminals vs follow set vs item lookahead).

**Impact**: Bug fixes must be applied in 3 places. Any change to transition iteration requires 3 edits.

**Suggested fix**: Extract the common shift/goto population into a private helper parameterized by a reduce-dispatch function.

---

## 2. Code Quality and Duplication

### 2.1 Unused definitions

| Location | Definition | Notes |
|----------|-----------|-------|
| `Matrix.fs` | `SubmatrixBlock` | Defined but never used in any source or test file. Added in task 33 but no consumer. |
| `LinearAlgebra.fs` | `LinearAlgebra.kron` | Kronecker product is defined and tested but never called from any algorithm in `src/`. |
| `GrammarTests.fs` | `nonterminalsOfCnf` | Private helper defined but never invoked. |
| `BooleanDecomposition.fs` | Entire module | `decompose`/`recompose` are defined and tested, but no algorithm currently uses them. The Valiant implementation uses raw `Matrix<bool>` dictionaries, not the BooleanDecomposition abstraction. |

### 2.2 Duplication in TestUtils.fs

`checkDotCompiles` and `checkDotCompilesWithInfo` duplicate ~15 lines of process-invocation logic. The latter is a superset of the former.

### 2.3 Duplication in LRParserTests.fs

Sub-modules `Grammar1`, `Grammar3`, `Grammar7`, `Grammar8` have nearly identical structure: SLR accept/reject, SLR leaves, CLR accept/reject, CLR leaves. Each test case differs only in the grammar, table builder call, and expected strings.

**Impact**: ~200 lines of near-duplicate test code could be ~50 lines of parameterized tests.

### 2.4 Rhs.toList vs Rhs.toSymbols ambiguity

Both functions convert `Rhs<'t,'nt>` to `Symbol<'t,'nt> list`. The only difference: `toList` returns `[Epsilon]` for epsilon-rhs, `toSymbols` returns `[]`. The choice between them in calling code is not always obvious and could lead to subtle bugs (empty list vs epsilon list).

**Suggested fix**: Rename `toSymbols` to `toNonEpsilonList` or document the distinction more prominently.

### 2.5 CYK uses mutable HashSet

`Cyk.CykCell = Option<HashSet<Symbol<string,string>>>` uses `System.Collections.Generic.HashSet`. While consistent with the "no nontrivial optimizations" guideline, the rest of the codebase uses F# `Set` for purity. The CYK cell accumulation logic (`accumulated.Add(N nt) |> ignore`) is a rare use of imperative .NET collections in otherwise functional code.

---

## 3. Naming and Style

### 3.1 Module names shadowing types

`Automaton.fs` used to have `type Automaton<'t,'s>`. After task 31, the type was split into `NFA`/`DFA`. The old `Automaton` module was removed, but the file is still named `Automaton.fs`. The module names `Nfa`/`Dfa` (lowercase 'a') were chosen to avoid conflicting with the type names `NFA`/`DFA`.

**Impact**: `Nfa.alphabet` and `Dfa.alphabet` must be carefully qualified to avoid type/module ambiguity. In separate files it works fine, but within `Automaton.fs` itself, the Nfa module's internal function `alphabet` must be called without qualification.

### 3.2 camelCase vs PascalCase in record fields

`VisualizationStep` uses camelCase fields (`tree`, `stack`, `input`) which is correct per F# conventions. But `LR0Item`/`LR1Item` use PascalCase (`Lhs`, `Rhs`, `Dot`, `Lookahead`) — inconsistent with the project style guideline ("Use PascalCase for types and modules, camelCase for functions and values"). Record fields are values and should be camelCase.

### 3.3 File naming

`VisualizationTypes.fs` is a valid name but the file only contains one type (`VisualizationStep`). The plural "Types" suggests multiple types.

---

## 4. Test Coverage Gaps

### 4.1 Missing TeX/DOT runtime checks in tests

`TexCompilationTests.fs` and all `*VisualizationTests.fs` assume `pdflatex`/`dot` are on `PATH`. No runtime guard or skip marker if the external tool is absent. This is partially mitigated by `[<Trait("Category", ...)>]` which allows filtering, but tests still fail if run without the tool even in a filtered run.

### 4.2 No property tests for crucial domains

| Module | Missing Property Tests |
|--------|----------------------|
| `Grammar.toCnf` | No property that CNF preserves language or that repeated `toCnf` is idempotent |
| `FirstFollow` | All `[<Fact>]` — no FsCheck properties for first/follow set correctness |
| `NFA/DFA` | No property that `toDfa` preserves language (or acceptance) |
| `AutomatonVisualizer` | No property tests — all hardcoded automata |
| `LLParser` | No property that table built from a grammar is "well-formed" (no conflicts for LL(k)-capable grammars) |

### 4.3 LR(0) table "reduce on everything" produces conflicts silently

`buildLR0Table` adds reduce actions for every terminal (including epsilon) in every state with a completed item. This generates many ShiftReduce/ReduceReduce conflicts. The conflicts are collected in `LRTable.conflicts` but the parser still attempts to use the table — with `conflicts` populated, the behavior is undefined (first-inserted action wins via `Map.tryFind`). No test verifies that parsing with a conflicting table fails predictably.

### 4.4 Expression grammars not tested for Valiant

`ValiantTests.fs` covers grammars 1-5 but not 6-8 (the arithmetic expression grammars). CYK and LL/LR do cover them.

### 4.5 No test for higher-k LL parsing

All LL tests use `k=1`. The `buildTable` function accepts arbitrary `k` but is never tested with `k>1`.

---

## 5. Tooling and CI Observations

### 5.1 CI workflow runs `dotnet test` without `--no-build`

The CI jobs `build-and-test` and `graphviz-tests` both do `dotnet build` followed by `dotnet test --no-build` in the main job, but the `graphviz-tests` job does `dotnet build` then `dotnet test --no-build`. Wait — checking the actual ci.yml:

```
- name: Build
  run: dotnet build -c ${{ matrix.config }} --no-restore
- name: Test (excluding Graphviz)
  run: dotnet test -c ${{ matrix.config }} --no-build --filter "Category!=Graphviz&Category!=TeX"
```

This is correct — build then test with `--no-build`. The `graphviz-tests` job also has its own build step. Both are correct.

### 5.2 No Release-mode test coverage for Graphviz/TeX tests

The `graphviz-tests` job has `strategy: matrix: config: [Debug, Release]`. Good.

---

## 6. Suggestions Prioritized

| Priority | Issue | Effort |
|----------|-------|--------|
| **High** | Make CYK/Valiant generic over `'t,'nt` (or explicitly document why they aren't) | Medium |
| **High** | Deduplicate `buildLR0Table`/`buildSLR1Table`/`buildCLR1Table` | Medium |
| **Medium** | Make `Matrix.data` private, add accessor functions | Medium |
| **Medium** | Remove `SubmatrixBlock` or add a consumer that uses it | Small |
| **Medium** | Deduplicate `LRParserTests.fs` grammar sub-modules | Medium |
| **Medium** | No-conflict handling: define LR parse behavior when conflicts exist | Small |
| **Low** | Rename `Rhs.toSymbols` to `Rhs.toNonEpsilonList` | Small |
| **Low** | Fix `LR0Item`/`LR1Item` field casing (PascalCase → camelCase) | Small |
| **Low** | Add property tests for `toCnf`, `FirstFollow`, automaton conversion | Large |
| **Low** | Add LL(k>1) tests | Medium |
| **Low** | Add Valiant tests for expression grammars | Small |
| **Low** | Remove `nonterminalsOfCnf` (unused) | Small |
