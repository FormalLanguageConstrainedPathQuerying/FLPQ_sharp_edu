# Detailed Plan: Tasks 29 & 30 — Epsilon transitions & NonEmptySet

## Goals

1. Add explicit epsilon transitions field to Automaton
2. Change transition matrix cell type from `Set<'t>` to `Option<NonEmptySet<'t>>`
3. Apply NonEmptyList/NonEmptySet where semantically required

## Changes

### 1. Automaton type

```fsharp
type Automaton<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Option<NonEmptySet<'t>>>
      epsTransitions: Set<int * int>
      startStates: Set<int>
      finalStates: Set<int> }
```

### 2. Automaton module functions

- `alphabet`: iterate cells, collect NonEmptySet elements
- `move`: check `Option<NonEmptySet<'t>>` cells for symbol
- `fromTransitions`: create NonEmptySet for transition symbols, set epsTransitions
- `toDfa`: handle new types, propagate epsTransitions
- `isDeterministic`: check NonEmptySet count > 1 for non-determinism

### 3. AutomatonVisualizer

- Draw epsilon transitions as dotted edges with epsilon label

### Files

| File | Action |
|------|--------|
| `src/FLPQ.Languages/Automaton.fs` | Modify type, all functions |
| `src/FLPQ.Languages/AutomatonVisualizer.fs` | Handle Option<NonEmptySet>, add epsilon edges |
| `tests/FLPQ.Languages.Tests/AutomatonTests.fs` | Update tests |
| `tests/FLPQ.Languages.Tests/AutomatonVisualizationTests.fs` | Update assertions |
