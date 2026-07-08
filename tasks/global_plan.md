# Global Plan: Tasks 142--144

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 142 | Add GLL regex-DFA equivalence property tests | Tests | None |
| 143 | Add GLL + RNGLR acceptance & derivation tree tests for 4 grammars | Tests | None (independent of 142) |
| 144 | Add property tests for cross-algorithm equivalence (GLL == RNGLR == CYK) on grammars from 143 | Tests | 143 (needs grammars defined there) |

## Dependencies Graph

```
Task 142 → (independent)
Task 143 → Task 144
```

- Task 142 adds a new `GllRegexEquivalence` module in `GllTests.fs`. Independent of 143/144.
- Task 143 adds new grammars and Fact tests in both `GllTests.fs` and `RnglrTests.fs`.
- Task 144 adds Property tests referencing grammars from 143. Must follow 143.

## Execution Order

1. **Task 142** — GLL regex equivalence tests
2. **Task 143** — GLL + RNGLR acceptance & tree tests
3. **Task 144** — Cross-algorithm equivalence property tests

## Conflict Analysis

- **Task 142 vs 143/144**: 142 only adds code to `GllTests.fs`. 143 adds to both `GllTests.fs` and `RnglrTests.fs`. 144 adds Property tests referencing 143's grammars. No file conflicts if done in order.
- **Task 143 vs 144**: 144 depends on 143's grammars. Sequential execution prevents conflicts.

## Shared Infrastructure

- `TestGrammars.fs` — existing shared grammar definitions. New grammars from task 143 could be added here or defined inline in test files.
- `Generators.fs` — existing `AStringGenerators` for "a"-only strings. May need new generators for combined "a"/"b" strings.
- `GllTests.fs` — existing GLL tests. Task 142 adds GllRegexEquivalence module. Task 143 adds grammar-specific acceptance/tree tests. Task 144 adds property equivalence tests.
- `RnglrTests.fs` — existing RNGLR tests. Task 143 adds grammar-specific acceptance/tree tests. Task 144 adds property equivalence tests.

## Task 142: GLL Regex Equivalence Tests

Add `GllRegexEquivalence` module to `GllTests.fs` mirroring RNGLR's `RnglrRegexEquivalence`:
- `S -> a* ≡ DFA for a*` ([<Property>])
- `S -> a* a* ≡ DFA for a* a*` ([<Property>])
- `S -> (a | b)* ≡ DFA for (a | b)*` ([<Property>])
- `S -> (a | b)* (a | c)* ≡ DFA for (a | b)* (a | c)*` ([<Property>])
- For random strings, gllAccepts rsm str = dfaAccepts dfa str.

## Task 143: GLL + RNGLR Acceptance & Tree Tests

Grammars:
1. `S -> N a*; N -> (a a) | a` — Accept: a, aa, aaa, aaaa. Reject: empty, b, ab, aab, aaab, abaa
2. `S -> a* N; N -> a | (a a)` — Accept: a, aa, aaa, aaaa. Reject: empty, b, ab, aab, aaab, abaa
3. `S -> N*; N -> a | (a a)` — Accept: empty, a, aa, aaa, aaaa. Reject: b, ab, aab, aaab, abaa
4. `S -> a | S S | S S S` — Accept: a, aa, aaa, aaaa. Reject: empty, b, ab, aab, aaab, abaa

For each grammar: add [<Fact>] acceptance tests (GLL) and derivation tree leaf tests (GLL + RNGLR).

## Task 144: Cross-Algorithm Equivalence Property Tests

For all 4 grammars from task 143, add [<Property>] tests:
- `acceptGLL str == acceptGLR str == acceptCYK str`
- Reuse FsCheck string generators (filtered to alphabet `a`/`b` where needed).
