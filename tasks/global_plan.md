
# Global Plan: Tasks 119--121

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 119 | Deduplicate automaton infrastructure: shared `alphabet`, reuse `buildAutomaton` for `toDfa`, deduplicate `buildLR0`/`buildLR1` BFS | Refactor | None |
| 120 | Reuse LR automaton in CLI runners — return automaton from table builders, use in `LRRunner` | Refactor | 119 (modified table builders + automaton types) |
| 121 | Fix naming and style: rename misspelled functions, fix `LRSymbol` collision, params consistency, rename `Submatrix.A/B`, remove unused `aug`, remove magic 10000 | Refactor | 119, 120 (touches same files after refactoring) |

## Dependencies Graph

```
Task 119 (Deduplicate automaton infrastructure)
    └── Task 120 (Reuse LR automaton in CLI — depends on refactored table builders)
    └── Task 121 (Fix naming/style — independent but best done after structural refactoring)
```

Tasks 120 and 121 could theoretically be independent, but doing them after 119 avoids merge conflicts on `LRParser.fs` and `Automaton.fs`.

## Execution Order

1. **Task 119** — Deduplicate automaton infrastructure
2. **Task 120** — Reuse LR automaton in CLI runners
3. **Task 121** — Fix naming and style issues

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 119 | `src/FLPQ.Languages/Automaton.fs`, `src/FLPQ.Languages/LRParser.fs` | 120 (same files), 121 (naming in same files) |
| 120 | `src/FLPQ.Languages/LRParser.fs`, `src/FLPQ.Cli/LRRunner.fs` | 121 (naming in LRParser.fs) |
| 121 | `src/FLPQ.Printers/LRAutomatonTikz.fs`, `src/FLPQ.Languages/LRParser.fs`, `src/FLPQ.Languages/VisualizationTypes.fs`, `src/FLPQ.Languages/Valiant.fs`, `src/FLPQ.Printers/ValiantTeX.fs`, `src/FLPQ.Printers/GrammarTeX.fs`, `src/FLPQ.Printers/MatrixTeX.fs`, `src/FLPQ.Printers/LLTableTeX.fs`, `src/FLPQ.GraphAnalysis/Graph.fs`, `src/FLPQ.LinearAlgebra/BooleanDecomposition.fs`, `src/FLPQ.Cli/LRRunner.fs`, `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs`, `tests/FLPQ.Languages.Tests/ValiantTests.fs`, docs | Depends on 119+120 refactored state |

## Shared Infrastructure

### Task 119: Deduplicate automaton infrastructure

**1. Shared `alphabet` function:**
- Current: `Nfa.alphabet` (lines 74-87) and `Dfa.alphabet` (lines 323-336) — identical code
- Extract into a private helper in `Automaton.fs` taking `Matrix<Option<NonEmptySet<AutomatonSymbol<'t>>>>` 
- Both `Nfa.alphabet` and `Dfa.alphabet` become one-liners calling the helper

**2. Replace `Dfa.alphabet` temporary-NFA:**
- `Dfa.alphabet` currently duplicates the NFA iteration pattern
- After step 1, this is resolved

**3. Replace `Nfa.toDfa` with `buildAutomaton`:**
- Both implement identical worklist-based state-space exploration:
  - Worklist queue with deduplication
  - For each state, for each symbol, compute transition target, assign new index
  - Final states determined by predicate
- Create adapter functions: `getSymbols` = `alphabet nfa`, `goto` = `moveSet`, `isAcceptState` = checks intersection with final states
- Note: `buildAutomaton` uses `Dfa.fromTransitions` while `toDfa` manually builds via `Graph.fromEdges` and `buildMatrix` — need to check equivalence
- The `Set<int>` state type in DFA (from `toDfa`) vs generic `Set<'item>` in `buildAutomaton` — need adapter

**4. Deduplicate `buildLR0`/`buildLR1`:**
- Current: `buildLR0` (lines 219-238) and `buildLR1` (lines 240-262) have ~60 duplicated lines
- Both delegate to `buildLR` helper (already exists!) which delegates to `buildAutomaton`
- Wait — the agent reports they already share `buildLR` (lines 202-217). The ~60 lines is the helper `buildLR` itself.
- Actually looking again: `buildLR0` and `buildLR1` are thin wrappers (20 lines each) over `buildLR`. The near-identical portion is the `mkStartItem`, `mkCompleteItem`, `dotOf`, `rhsOf` lambda construction pattern.
- Already well-factored! Task says "extracting a common BFS framework parameterized by closure function and item construction" — this is already done via `buildLR` + `buildAutomaton`.
- Need to verify this matches what the task intends; may just need minimal cleanup.

### Task 120: Reuse LR automaton in CLI

- Modify `LRTable<'t, 'nt>` to include an optional `automaton` field (or add to return type)
- Or: return a tuple `(LRTable * DFA<...>)` from table builders
- `buildLR0Table` and `buildSLR1Table` already call `LRAutomaton.buildLR0 aug` internally
- `buildCLR1Table` already calls `LRAutomaton.buildLR1 aug` internally
- In `LRRunner`: currently builds automaton a second time for rendering — instead reuse from table builder
- Need to decide: return type — tuple vs extended LRTable

Decision: Add a field to `LRTable` with the automaton. Problem: `LRTable` is generic only over `'t, 'nt`, but the automaton DFA has different state types for LR0 vs LR1 (`Set<LR0Item>` vs `Set<LR1Item>`). Options:
a) Make `LRTable` generic over `'state` as well: `LRTable<'t, 'nt, 'state>`
b) Use a discriminated union for the automaton: `type LRAutomaton = LR0 of DFA<Symbol<'t,'nt>, Set<LR0Item<'t,'nt>>> | LR1 of DFA<Symbol<'t,'nt>, Set<LR1Item<'t,'nt>>>`
c) Return tuple from builders, don't modify LRTable

Option (c) is simplest and least invasive. Let's check what the task says: "Modify table-construction functions to return the built automaton as part of LRTable (or a separate return value)". OK, separate return value (tuple) is fine.

### Task 121: Fix naming and style

**1. Rename `lr0AutomatontoTikz` / `lr1AutomatontoTikz`:**
- Change to `lr0AutomatonToTikz` / `lr1AutomatonToTikz`
- Update: `LRAutomatonTikz.fs`, `LRRunner.fs`, `AutomatonVisualizationTests.fs`, `automaton-viz.md`
- Also rename module-level reference in docs

**2. Fix `LRSymbol` collision:**
- `LRSymbol` is both a DU case of `LRStackFrame` and a module name
- Rename module to `LRSymbolHelpers`
- Update: `VisualizationTypes.fs` (definition), any usage of `LRSymbol.symbol`/`LRSymbol.tree`
- Find all usages of `module LRSymbol` qualified access

**3. Make `LRAutomatonTikz` consistent with `AutomatonTikz.dfaToTikz`:**
- `dfaToTikz` takes: `labelPrinter`, `stateVisualizer`, `shape`, `dfa`
- `lr0AutomatonToTikz` currently takes: `terminalPrinter`, `nonterminalPrinter`, `aug`, `dfa`
- Task says: accept `labelPrinter`/`stateVisualizer`/`shape` parameters
- Remove `aug` (unused)

**4. Rename `isCompleted` → `isCompletedLR0`, `isCompleted1` → `isCompletedLR1`:**
- In `LRParser.fs` lines 268, 270
- Update all call sites in `buildLR0Table`, `buildSLR1Table`, `buildCLR1Table`

**5. Replace single-letter params:**
- `g` → `grammar` in `GrammarTeX.fs` (3 functions)
- `m` → `matrix` in `BooleanDecomposition.fs` (2 functions)
- `m` → `matrix` in `MatrixTeX.fs` (2 functions)
- `g` → `graph` in `Graph.fs` (12 functions)
- `g` → `grammar` in `LLTableTeX.fs` (1 function)

**6. Rename `Submatrix.A`/`B` → `row`/`col`:**
- `Valiant.fs` type definition and all usage
- `ValiantTeX.fs` all usage
- `ValiantTests.fs` test usage
- `docs/valiant.md`

**7. Remove unused `aug` param:**
- From `lr0AutomatonToTikz` and `lr1AutomatonToTikz` signatures
- Update all call sites to remove the arg

**8. Remove magic number 10000:**
- In `LRParser.parseWithSteps` line 485
- Replace with named constant or just remove (infinite loop should be handled differently, or keep a well-named constant)

**Equivalence checks:**
- Task 119: NFA→DFA must produce identical DFA; LR0/LR1 automata unchanged
- Task 120: LR parsing results identical
- Task 121: Pure renames, all tests must pass