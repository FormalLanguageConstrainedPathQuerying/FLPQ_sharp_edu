# Detailed Plan: Task 119 — Deduplicate Automaton Infrastructure

## Goal
Deduplicate shared automaton infrastructure across `Automaton.fs` and `LRParser.fs`.

## Steps

### 1. Extract shared `alphabet` function ✅
- Created `Nfa.collectAlphabet` (public) that extracts all `ATerm` labels from a transition matrix
- `Nfa.alphabet` → calls `collectAlphabet a.transitions`
- `Dfa.alphabet` → calls `Nfa.collectAlphabet a.transitions`
- **Status**: Done. Both return identical results.

### 2. Generic `buildAutomaton` + update `toDfa` ✅
- Moved `buildAutomaton` from private in `LRAutomaton` module to a new `Automaton` module in `Automaton.fs`
- Made it generic over symbol type `'sym` (previously hardcoded to `Symbol<'t,'nt>`)
- `Automaton.toDfa` calls `Automaton.buildAutomaton` with adapter functions
- Removed `toDfa` from `Nfa` module (moved to `Automaton` module)
- Updated all call sites: `AutomatonTests.fs`, `StressTests.fs` — `Nfa.toDfa` → `Automaton.toDfa`
- Updated `LRAutomaton.buildLR` to call `Automaton.buildAutomaton` (from `LRParser.fs`)
- **Status**: Done. All tests pass.

### 3. Verify buildLR0/buildLR1 deduplication ✅
- `buildLR` helper already exists as the "common BFS framework parameterized by closure function and item construction"
- `buildLR0` and `buildLR1` are thin wrappers (~20 lines each) over `buildLR`
- **Status**: Already done. Noted in `fixes_for_book.md`.

## Files Modified
- `src/FLPQ.Languages/Automaton.fs` — added `collectAlphabet`, `Automaton.buildAutomaton`, `Automaton.toDfa`
- `src/FLPQ.Languages/LRParser.fs` — removed local `buildAutomaton`, updated `buildLR` to call `Automaton.buildAutomaton`
- `tests/FLPQ.Languages.Tests/AutomatonTests.fs` — `Nfa.toDfa` → `Automaton.toDfa`
- `tests/FLPQ.Languages.Tests/StressTests.fs` — `Nfa.toDfa` → `Automaton.toDfa`
- `tasks/fixes_for_book.md` — noted `buildLR0`/`buildLR1` already deduplicated

## Equivalence Verification
- All 505+ tests pass
- NFA→DFA conversion produces identical DFA (structurally same algorithm with adapter functions)
- LR(0)/LR(1) automata unchanged (same `buildLR` → `buildAutomaton` pipeline)
