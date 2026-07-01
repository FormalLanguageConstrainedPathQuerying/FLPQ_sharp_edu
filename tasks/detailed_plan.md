# Detailed Plan: Task 68 — Unify buildLR0/buildLR1

## Goal

Merge duplicated structure between buildLR0 and buildLR1 into a single parametrized function.

## Shared structure

Both functions:
1. Get augmentedRule = aug.rules.[0]  
2. Create start item from augmented rule
3. Compute startItems via closure
4. Define `getSymbols` with identical pattern (iterate items, get symbol at dot position)
5. Define `gotoFn`
6. Create `isAcceptState` via Set.contains acceptItem
7. Call buildAutomaton

## Approach

Extract `getSymbols` into a parameterized helper (takes `dotOf` and `rhsOf` accessors).
Create `buildLR` that handles steps 1-7 given item constructors and closure/goto functions.
`buildLR0` and `buildLR1` become 8-line wrappers.
