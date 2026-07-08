# Detailed Plan: Task 142 — GLL Regex Equivalence Tests

## Goal
Add `GllRegexEquivalence` module to `GllTests.fs` with 4 property tests comparing GLL with DFA for regex patterns.

## Subtasks
1. [pending] Add `gllAcceptsRsm` helper that checks GLL acceptance given a pre-built RSM (similar to existing `gllAccepts` but takes RSM directly)
2. [pending] Add `buildRegexRsm`, `dfaFromRegexRsm`, `dfaAcceptsRegex` helpers (mirror RNGLR's helpers)
3. [pending] Add 4 `[<Property(MaxTest=50)>]` tests:
   - `S -> a*` ≡ DFA for `a*`
   - `S -> a* a*` ≡ DFA for `a* a*`
   - `S -> (a | b)*` ≡ DFA for `(a | b)*`
   - `S -> (a | b)* (a | c)*` ≡ DFA for `(a | b)* (a | c)*`
4. [pending] Run `dotnet test` — all tests must pass
5. [pending] Format with `dotnet fantomas .`
6. [pending] Commit and checkout to dev
