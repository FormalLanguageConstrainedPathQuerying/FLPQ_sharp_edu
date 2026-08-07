# Global Plan: Valiant SPPF Fixes and Complete Invariants (Tasks 248, 249, 250)

## Tasks

| ID | Title | Summary |
|----|-------|---------|
| 248 | Fix Valiant SPPF SplitPoint | Fix `mxmSetSppf` to use absolute SplitPoint = `m1.Col + k` instead of local `k`. Verifies SPPF tables byte-identical CYK vs Valiant. |
| 249 | Fix BasicSppf Production node reuse | Replace `getOrCreate(Production(...))` with direct allocation. Each Production node is context-dependent (parent cell determines children). |
| 250 | Complete invariant checks | Add `allCompatibleGrammars`, restore full invariant checks (tree leaves, byte-identical tables, child count = RHS length, structural SPPF equivalence). |

## Dependencies

- **248 is independent** — pure algorithmic fix in Valiant.fs
- **249 is independent** — pure fix in BasicSppf.fs  
- **250 depends on both 248 and 249** — invariant checks that require correctness from prior fixes

## Execution Order

1. **Task 248** — Fix the fundamental data bug (SplitPoint convention mismatch)
2. **Task 249** — Fix the SPPF construction bug (Production node sharing)
3. **Task 250** — Complete invariant checks that verify correctness of both fixes

## Overlapping Files

| File | 248 | 249 | 250 |
|------|-----|-----|-----|
| `Valiant.fs` | +leftColOffset param to mxmSetSppf, update doMultiplicationsSppf | — | — |
| `BasicSppf.fs` | — | direct alloc for Production nodes | — |
| `LanguageRegistry.fs` | — | — | +allCompatibleGrammars |
| `TestHelpers.fs` | — | — | extend checkCykValiantEquivalence |
| `CrossParserEquivalenceTests.fs` | — | — | update checkLanguages to use allCompatibleGrammars |

## Reuse Analysis

### Task 248
- `Matrix.mxmi` — existing, already provides `(i, k, j)` indices
- `Submatrix` type — existing, provides `.Col` offset
- `doMultiplicationsSppf` — existing, has access to `m1.Col`

### Task 249
- `fromParsingTable` — existing, local `getOrCreate` function
- `BasicSppfNodeInfo.Production` — existing DU case

### Task 250
- `LanguageRegistry.allLanguages` — existing
- `AnnotatedGrammar.Properties` — existing, has `IsRsmDerived`, `DoesNotCoverFullLanguage`
- `TestHelpers.isCykValiantCompatible` — existing filter
- `TestHelpers.checkCykValiantEquivalence` — existing, needs extension
- `BasicSppf.validateProductionChildren` — existing
- `Scc.countNonTrivialScc` — existing

## Risk Assessment

- **Task 248**: High correctness risk — changes multiplication logic. Mitigated by existing table comparison tests.
- **Task 249**: Medium risk — changes SPPF structure. Mitigated by tree yield tests.
- **Task 250**: Low risk — mostly adds verification checks; fixes may be needed if invariants fail.
