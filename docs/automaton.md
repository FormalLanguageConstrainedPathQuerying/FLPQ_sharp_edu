# Automaton Module

Generic finite automaton types and operations: nondeterministic (NFA) and deterministic (DFA). Used as the basis for LR automata construction and RSM blocks.

## Type Definitions

### `NFA<'t, 's>`

```fsharp
type NFA<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Option<NonEmptySet<'t>>>
      epsTransitions: Set<int * int>
      startStates: Set<int>
      finalStates: Set<int> }
```

A nondeterministic finite automaton with epsilon transitions.
- `states`: ordered list of state data (states are identified by their index).
- `transitions`: square matrix where cell `[i, j]` contains `Some set` of symbols labeling transitions from state `i` to `j`, or `None`.
- `epsTransitions`: set of epsilon transitions `(fromIdx, toIdx)`, consumed without reading input.
- `startStates`: set of initial state indices (multiple allowed).
- `finalStates`: set of accepting state indices.

### `DFA<'t, 's>`

```fsharp
type DFA<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Option<NonEmptySet<'t>>>
      startState: int
      finalStates: Set<int> }
```

A deterministic finite automaton with exactly one start state and no epsilon transitions.
- `states`: ordered list of state data.
- `transitions`: square matrix where cell `[i, j]` contains `Some set` of symbols, or `None`.
- `startState`: single start state index.
- `finalStates`: set of accepting state indices.

## Nfa Module — Function Signatures

### `Nfa.buildMatrix`

```fsharp
val buildMatrix: 's list -> (int * 't * int) list -> Matrix<Option<NonEmptySet<'t>>>
```

Builds a transition matrix from a list of `(fromIdx, symbol, toIdx)` transitions.

### `Nfa.fromTransitions`

```fsharp
val fromTransitions: 's list -> (int * 't * int) list -> Set<int * int> -> Set<int> -> Set<int> -> NFA<'t, 's>
```

Constructs an NFA from transition list, epsilon transitions, start states, and final states.

### `Nfa.stateCount`

```fsharp
val stateCount: NFA<'t, 's> -> int
```

Returns the number of states.

### `Nfa.alphabet`

```fsharp
val alphabet: NFA<'t, 's> -> Set<'t>
```

Collects all transition symbols appearing anywhere in the automaton.

### `Nfa.move`

```fsharp
val move: NFA<'t, 's> -> int -> 't -> Set<int>
```

Returns the set of state indices reachable from a given state by a specific symbol.

### `Nfa.epsilonClosure`

```fsharp
val epsilonClosure: NFA<'t, 's> -> int -> Set<int>
```

Returns the epsilon-closure of a state: all states reachable via zero or more epsilon transitions.

### `Nfa.moveSet`

```fsharp
val moveSet: NFA<'t, 's> -> Set<int> -> 't -> Set<int>
```

Returns states reachable from any state in the set by a given symbol. Union of `move` over all states.

### `Nfa.toDfa`

```fsharp
val toDfa: NFA<'t, 's> -> DFA<'t, Set<int>>
```

Converts NFA to DFA via subset construction. Each DFA state is a `Set<int>` representing a set of NFA states.

## Dfa Module — Function Signatures

### `Dfa.fromTransitions`

```fsharp
val fromTransitions: 's list -> (int * 't * int) list -> int -> Set<int> -> DFA<'t, 's>
```

Constructs a DFA from transition list, start state, and final states.

### `Dfa.stateCount`

```fsharp
val stateCount: DFA<'t, 's> -> int
```

Returns the number of states.

### `Dfa.alphabet`

```fsharp
val alphabet: DFA<'t, 's> -> Set<'t>
```

Collects all transition symbols appearing anywhere in the DFA.

### `Dfa.move`

```fsharp
val move: DFA<'t, 's> -> int -> 't -> int option
```

Returns the target state index reachable from a given state by a specific symbol, or `None`.

### `Dfa.isDeterministic`

```fsharp
val isDeterministic: DFA<'t, 's> -> bool
```

Checks whether the DFA is deterministic: for each state and each alphabet symbol, there is at most one target state (i.e., no two columns in the same row contain the same symbol).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| States identified by index | Transitions use integer indices; state data (`'s`) carries semantic information (e.g., set of LR items). |
| Transition matrix over `Option<NonEmptySet<'t>>` | Uses `NonEmptySet` to prevent empty transition labels; supports multiple symbols between same pair of states. |
| NFA has epsilon transitions | Explicit epsilon transitions enable natural encoding of regular expression operations (union, star). |
| DFA has single start state, no epsilon | Deterministic semantics: exactly one transition per (state, symbol) pair. |
| `Dfa.isDeterministic` validates construction | Since `Dfa.fromTransitions` does not enforce determinism at type level, this function is used to verify correctness of manually constructed DFAs. |
| Generic over `'t` and `'s` | Reusable for LR(0) (with `Set<LR0Item>` states), LR(1) (with `Set<LR1Item>` states), and RSM blocks. |

## Book Relationship

The automaton module provides the generic finite automaton infrastructure. It is used by:
- LR automata construction (`LRAutomaton.buildLR0`, `buildLR1`) to represent the resulting DFA.
- RSM builder (`RsmBuilder`) to construct deterministic blocks from EBNF rules via Brzozowski derivatives.
