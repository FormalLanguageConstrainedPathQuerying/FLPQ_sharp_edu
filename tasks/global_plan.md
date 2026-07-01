# Global Plan: Tasks 67--72

## Task Summary

| ID | Description | Type | Status |
|----|-------------|------|--------|
| 67 | Merge modified Valiant trace + table (table extracted from last trace step) | Refactoring | Pending |
| 68 | Unify `buildLR0`/`buildLR1` into single parametrized function | Refactoring | Pending |
| 69 | Input for all parsing algorithms must be `Terminal list`, not `Symbol list` | Refactoring | Pending |
| 70 | Make `nonterminalsOf`/`terminalsOf` public in `Grammar.fs`, deduplicate | Refactoring | Pending |
| 71 | Use `MyGen`/`MyArb` instead of `System.Random.Shared` in property test generators | Refactoring | Pending |
| 72 | Add LL(2) parsing tests with specific grammar | Test | Pending |

## Dependencies

```
Task 70 (public nonterminalsOf/terminalsOf) ── independent, small
Task 69 (Terminal list input) ── independent, affects many files
Task 67 (merge Valiant trace+table) ── independent
Task 68 (unify buildLR0/buildLR1) ── independent
Task 71 (MyGen/MyArb cleanup) ── independent, may be already done
Task 72 (LL(2) tests) ── independent
```

All tasks are independent. Execution order can be any, but tasks 69 touches most files so should be done early to avoid merge conflicts.

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 67 | `src/FLPQ.Languages/Valiant.fs`, `tests/FLPQ.Languages.Tests/ValiantTests.fs` | Task 69 (Valiant signature change) |
| 68 | `src/FLPQ.Languages/LRParser.fs` | Task 69 (LRParser signature change) |
| 69 | Many files across src/ and tests/ | Tasks 67, 68, 72 |
| 70 | `src/FLPQ.Languages/Grammar.fs`, `src/FLPQ.Printers/LLTableTeX.fs`, `tests/FLPQ.Languages.Tests/GrammarTests.fs` | None |
| 71 | `tests/FLPQ.GraphAnalysis.Tests/RandomGraphGenerators.fs` (may be already done) | None |
| 72 | `tests/FLPQ.Languages.Tests/LLParserTests.fs`, `tests/FLPQ.Languages.Tests/TestGrammars.fs` | None |

## Execution Order

Recommended order (minimizing rework):
1. **Task 70** — Small, independent, makes nonterminalsOf/terminalsOf public for other tasks
2. **Task 71** — Investigate, likely already done, mark accordingly
3. **Task 69** — Changes input types across all parsing algorithms (major refactoring)
4. **Task 67** — Modified Valiant refactoring (after task 69 so signatures align)
5. **Task 68** — LR buildLR0/buildLR1 unification (after task 69)
6. **Task 72** — LL(2) tests (after task 69 for correct tokenizer usage)

## Shared Infrastructure

All tasks operate on existing modules. Task 69 introduces a consistent input type across all parsing algorithms.

## Detailed Changes Per Task

### Task 67 — Merge modified Valiant
- Remove code duplication between `parseModifiedWithTable` and `parseModifiedWithTrace`
- Have single internal computation that collects trace
- `parseModifiedWithTable` extracts last step's table → derive acceptance from it
- `parseModified` calls `parseModifiedWithTable` and returns `snd`
- Also apply same pattern to standard Valiant (`parseWithTable`/`parseWithTrace`)

### Task 68 — Unify buildLR0/buildLR1
- Extract `getSymbols` as shared generic helper
- Create `buildLR` that takes item-specific parameters
- `buildLR0` and `buildLR1` become thin wrappers calling `buildLR`

### Task 69 — Terminal list input
- Change all parsing function signatures: `Symbol<'t,'nt> list` → `Terminal<'t> list`
- Add `Terminal.toSymbols: Terminal<'t> list -> Symbol<'t,'nt> list` helper
- Update Tokenizer to expose `tokenizeTerminals` as the main entry
- Update all tests to use `Tokenizer.tokenizeTerminals`

### Task 70 — Make nonterminalsOf/terminalsOf public
- Change `private` → public in `Grammar.fs`
- Replace duplicates in `LLTableTeX.fs` with calls to `Grammar.nonterminalsOf`/`Grammar.terminalsOf`
- Update `GrammarTests.fs` private `nonterminalsOfCnf` if applicable

### Task 71 — MyGen/MyArb cleanup
- Verify no `System.Random.Shared` usage remains in .fs files
- If already clean, just verify and mark done
- If found, replace with FsCheck `MyGen`/`MyArb` patterns

### Task 72 — LL(2) tests
- Add grammar definition to `TestGrammars.fs`
- Add accept/reject string lists
- Add tests in `LLParserTests.fs`:
  - LL(2) table has no conflicts
  - Accepts all valid strings
  - Rejects all invalid strings
  - Leaves match input string
  - Also add property-based test with cross-check against CYK/Valiant
