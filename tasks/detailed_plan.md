# Detailed Plan: Task 143 — GLL + RNGLR Acceptance & Derivation Tree Tests

## Goal
Add acceptance and derivation tree tests for 4 grammars from task 143 in GllTests.fs and RnglrTests.fs.

## Grammars
1. `S -> N a*; N -> (a a) | a`
2. `S -> a* N; N -> a | (a a)`
3. `S -> N*; N -> a | (a a)`
4. `S -> a | S S | S S S` (standard CFG, not EBNF)

## Subtasks
1. [pending] Add `gllTreeRsm` helper for tree extraction from RSM (GLL)
2. [pending] Add `rnglrTreeRsm` helper for tree extraction from RSM (RNGLR)
3. [pending] Add `rnglrAcceptsRsm` helper for acceptance from RSM (RNGLR)
4. [pending] Add `GllGrammarAcceptanceAndTree` module to GllTests.fs with acceptance + tree tests
5. [pending] Add `RnglrGrammarAcceptanceAndTree` module to RnglrTests.fs with acceptance + tree tests
6. [pending] Run `dotnet test` — all tests must pass
7. [pending] Format, commit, merge to dev
