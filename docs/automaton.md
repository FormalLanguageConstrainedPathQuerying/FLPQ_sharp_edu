# Automaton Module

Generic finite automaton type and operations. Used as the basis for LR(0) and LR(1) automata construction.

## Type Definition

### `Automaton<'t, 's>`

```fsharp
type Automaton<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Set<'t>>
      startStates: Set<int>
      finalStates: Set<int> }
```

A finite automaton parameterized by transition label type `'t` and state data type `'s`.

- `states`: ordered list of state data (states are identified by their index).
- `transitions`: square matrix (`n × n`) where cell `[i, j]` contains the set of symbols labeling the transition from state `i` to state `j`.
- `startStates`: indices of initial states.
- `finalStates`: indices of accepting states.

## Function Signatures

### `Automaton.stateCount`

```fsharp
val stateCount: Automaton<'t, 's> -> int
```

Returns the number of states in the automaton.

### `Automaton.alphabet`

```fsharp
val alphabet: Automaton<'t, 's> -> Set<'t>
```

Collects all transition symbols appearing anywhere in the automaton.

### `Automaton.move`

```fsharp
val move: Automaton<'t, 's> -> int -> 't -> Set<int>
```

Returns the set of state indices reachable from a given state by a specific symbol.

### `Automaton.moveSet`

```fsharp
val moveSet: Automaton<'t, 's> -> Set<int> -> 't -> Set<int>
```

Returns the set of state indices reachable from any state in a set by a specific symbol.
Equivalent to the union of `move` over all states in the set.

### `Automaton.fromTransitions`

```fsharp
val fromTransitions: 's list -> (int * 't * int) list -> Set<int> -> Set<int> -> Automaton<'t, 's>
```

Constructs an automaton from a list of transitions `(fromIdx, symbol, toIdx)`, state data, start states, and final states.
Builds the transition matrix internally.

### `Automaton.toDfa`

```fsharp
val toDfa: Automaton<'t, 's> -> Automaton<'t, Set<int>>
```

Converts a nondeterministic automaton to a deterministic one via subset construction.
Each state of the resulting DFA is a `Set<int>` representing a set of original state indices.

### `Automaton.isDeterministic`

```fsharp
val isDeterministic: Automaton<'t, 's> -> bool
```

Checks whether the automaton is deterministic: exactly one start state and at most one target state per symbol per source state.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| States identified by index | Transitions use integer indices; state data (`'s`) carries semantic information (e.g., set of LR items). |
| Transition matrix over `Set<'t>` | Supports nondeterminism naturally; each cell holds all symbols for that transition. |
| Generic over `'t` and `'s` | Reusable for LR(0) (with `Set<LR0Item>` states) and LR(1) (with `Set<LR1Item>` states). |
| `fromTransitions` builder | Simplifies construction; clients don't need to build the transition matrix manually. |

## Book Relationship

The automaton module provides the generic finite automaton infrastructure. It is used by the LR automata construction (`LRAutomaton.buildLR0`, `buildLR1`) to represent the resulting DFA.
