# Global Plan: Tasks 185–186

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|--------------|
| 185 | Add path index invariant for accepted strings | Invariant + Fixes | None |
| 186 | Cross-test grammars/inputs between GLL and RNGLR tests | Tests + Fixes | 185 (correct algorithms needed before cross-testing) |

## Dependencies Graph

```
Task 185 → (add invariant, fix any violations)
Task 186 → (depends on 185 — cross-testing requires correct algorithms)
```

## Execution Order

1. **Task 185** — Add path index invariant for accepted strings, fix any algorithm violations
2. **Task 186** — Collect all grammar+input pairs, extend both GLL and RNGLR tests to union

## Rationale

- Task 185 adds a new invariant that may expose bugs in GLL or RNGLR. These bugs must be fixed before cross-testing in Task 186, which would otherwise add failing tests without distinguishing broken algorithms from missing test coverage.
- Task 186 extends both test suites to cover the union of grammars/inputs. Any failures discovered during this extension likely reflect pre-existing algorithm bugs — fixing them after Task 185's invariant additions ensures both invariants and acceptance/tree correctness are maintained.

## Conflict Analysis

- Both tasks modify `PathIndex.fs` (adding invariant checker), `GllTests.fs`, `RnglrTests.fs`, and `TestGrammars.fs`.
- Task 185 (invariant addition) changes `PathIndex.fs` only for the invariant function — narrow change.
- Task 186 (test expansion) changes `GllTests.fs`, `RnglrTests.fs`, and possibly `TestGrammars.fs` — new test cases using existing infrastructure.
- The primary overlap is `PathIndex.fs`: Task 185 adds a function; Task 186 only reads it via TestHelpers. No conflict if executed sequentially.

## Shared Infrastructure

- `PathIndex.fs` — invariant function added in Task 185, consumed by both GLL and RNGLR test infrastructure
- `TestGrammars.fs` — shared grammar definitions used by both GLL and RNGLR tests
- `TestHelpers.fs` — shared test pipeline (`accepts`, `checkReject`) that calls invariant validators
