# Detailed Plan: Task 141 — Refactor Automaton.intersection output type

## Goal
Change `Nfa.intersect` return type from `NFA<'t, int * int>` to `NFA<'t, 's * 'v>` — the state labels in the result must be pairs of original state labels from the input automata.

## Current state
`Nfa.intersect (a: NFA<'t, 's>) (b: NFA<'t, 'v>) : NFA<'t, int * int>`:

- Internally works with integer indices (0..nA-1, 0..nB-1) encoded as flat `p = iA * nB + iB`.
- Result graph labels are `(p/nB, p%nB)` — integer pairs that lose original `'s`/`'v` type info.

## Changes

### 1. `Automaton.fs` — `Nfa.intersect`

- Change signature: `NFA<'t, 's> -> NFA<'t, 'v> -> NFA<'t, 's * 'v>`.
- Extract vertex labels arrays from input automata.
- Build result graph labels as `(labelsA.[iA], labelsB.[iB])` instead of `(iA, iB)`.

### 2. Tests
No changes needed — tests use `Nfa.accept` and `Nfa.stateCount` which work with vertex indices regardless of label type.

## Verification
- `dotnet build FLPQ.slnx -c Debug`
- `dotnet test` — all tests must pass
- `dotnet fantomas . --check`
