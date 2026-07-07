# Automaton Module

Generic finite automaton types and operations: nondeterministic (NFA) and deterministic (DFA). Both wrap the generic `Graph` type from `FLPQ.GraphAnalysis`. Epsilon transitions are stored as `AEpsilon`-labeled edges in the transition matrix, not as a separate field. Used as the basis for LR automata construction, RSM blocks, RPQ query automata, and automaton intersection.

## Type Definitions

### `AutomatonLabel<'t>`

```fsharp
type AutomatonLabel<'t> =
    | ATerm of 't
    | AEpsilon
```

Distinguishes terminal symbols from epsilon in the transition matrix. `ATerm t` is a regular transition on symbol `t`; `AEpsilon` is an epsilon transition (consumed without reading input). This DU replaces the separate `epsTransitions: Set<int * int>` field in NFA.

### `Config`

```fsharp
[<Struct>]
type Config =
    { state: int
      position: int }
```

A configuration in automaton acceptance: a state index and the current input position. Used by `Nfa.accept` in the working-set algorithm.

### `NFA<'t, 's>`

```fsharp
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startStates: Set<int>
      finalStates: Set<int> }
    member this.states = this.graph |> Graph.vertices |> List.map snd
    member this.transitions = this.graph.edges
```

A nondeterministic finite automaton. Epsilon transitions are stored in the matrix as cells containing `Some nes` where `nes` includes `AEpsilon`.
- `graph`: the underlying graph; vertices are state labels, edges are transition label sets.
- `states`: computed member property — returns state labels as an ordered list.
- `transitions`: computed member property — returns the edge matrix `Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>`.
- `startStates`: set of initial state indices (multiple allowed).
- `finalStates`: set of accepting state indices.

### `DFA<'t, 's>`

```fsharp
type DFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startState: int
      finalStates: Set<int> }
    member this.states = this.graph |> Graph.vertices |> List.map snd
    member this.transitions = this.graph.edges
```

A deterministic finite automaton with exactly one start state and no epsilon transitions.

## Nfa Module — Function Signatures

### `Nfa.buildMatrix`

```fsharp
val buildMatrix: int -> (int * AutomatonLabel<'t> * int) list -> Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>
```

Builds a transition matrix from a list of `(fromIdx, label, toIdx)` transitions. Labels are `AutomatonLabel<'t>` values.

### `Nfa.fromTransitions`

```fsharp
val fromTransitions: 's list -> (int * 't * int) list -> Set<int * int> -> Set<int> -> Set<int> -> NFA<'t, 's>
```

Constructs an NFA from a list of regular transitions (raw `'t` symbols), epsilon transitions as `(fromIdx, toIdx)` pairs, start states, and final states. Internally wraps regular symbols as `ATerm` and epsilon pairs as `AEpsilon`, then builds a single transition matrix containing both.

### `Nfa.stateCount`

```fsharp
val stateCount: NFA<'t, 's> -> int
```

Returns the number of states.

### `Nfa.alphabet`

```fsharp
val alphabet: NFA<'t, 's> -> Set<'t>
```

Collects all terminal symbols appearing in the automaton. `AEpsilon` labels are excluded.

### `Nfa.move`

```fsharp
val move: NFA<'t, 's> -> int -> 't -> Set<int>
```

Returns the set of state indices reachable from a given state by a specific terminal symbol. Only `ATerm`-labeled edges are considered; `AEpsilon` edges are ignored (use `epsilonClosure` for those).

### `Nfa.epsilonClosure`

```fsharp
val epsilonClosure: NFA<'t, 's> -> int -> Set<int>
```

Returns the epsilon-closure of a state: all states reachable via zero or more `AEpsilon`-labeled edges in the transition matrix. Uses a fixed-point iteration over the matrix.

### `Nfa.moveSet`

```fsharp
val moveSet: NFA<'t, 's> -> Set<int> -> 't -> Set<int>
```

Returns states reachable from any state in the set by a given terminal symbol. Union of `move` over all states.

### `Nfa.toDfa`

```fsharp
val toDfa: NFA<'t, 's> -> DFA<'t, Set<int>>
```

Converts NFA to DFA via subset construction. Each DFA state is a `Set<int>` representing a set of NFA states.

### `Nfa.accept`

```fsharp
val accept: NFA<'t, 's> -> Terminal<'t> list -> bool
```

Classical NFA acceptance with a working set of configurations. Handles epsilon transitions via epsilon closure expansion. Uses a `visited` set of configurations to prevent infinite loops from epsilon cycles. Returns `true` if there exists a path from a start state to a final state consuming the entire input.

Algorithm:
1. Initialize working set with epsilon closures of all start states at position 0.
2. While working set is not empty: remove a configuration `(state, pos)`. If `state` is final and `pos` equals input length → accept. Otherwise, if `pos < input length`, follow all transitions on `input[pos]`, take epsilon closures of targets, add new `(targetState, pos+1)` configurations not yet visited.

### `Nfa.intersectEdgeSets`

```fsharp
val intersectEdgeSets:
    Option<NonEmptySet<AutomatonLabel<'t>>> ->
    Option<NonEmptySet<AutomatonLabel<'t>>> ->
    Option<NonEmptySet<AutomatonLabel<'t>>>
```

Element-wise set intersection of two optional non-empty sets of automaton labels. Used as the multiplication operation for Kronecker products of automaton transition matrices (both in `Nfa.intersect` and `KroneckerRPQ.evaluate`). Returns `None` if either operand is `None` or the intersection is empty.

### `Nfa.intersect`

```fsharp
val intersect: NFA<'t, 's> -> NFA<'t, 'v> -> NFA<'t, 's * 'v>
```

Intersect two NFAs without epsilon transitions using linear algebra. Returns an NFA whose language is `L(a) ∩ L(b)` with product state labels `('s * 'v)` — each state label is a pair of the original state labels from the input automata.

Algorithm:
1. Single Kronecker product of transition matrices with `intersectEdgeSets` → `productTransitions`.
2. Boolean mask for MS-BFS: `k = Matrix.map Option.isSome productTransitions`.
3. Forward MS-BFS from start pairs `(sA, sB)` on `k`.
4. Backward MS-BFS from final pairs `(fA, fB)` on `k^T`.
5. Column-wise OR via `Matrix.reduceByColumn` to find product states reachable from start AND can-reach-final.
6. `Graph.keepVertices` to retain only useful states and edges between them.

Precondition: both NFAs must be epsilon-free (no `AEpsilon` in transition matrices). The result has no epsilon transitions.

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

### `Dfa.alphabet`

```fsharp
val alphabet: DFA<'t, 's> -> Set<'t>
```

Collects all terminal symbols appearing in the DFA. `AEpsilon` labels are excluded.

### `Dfa.move`

```fsharp
val move: DFA<'t, 's> -> int -> 't -> int option
```

Returns the target state index reachable from a given state by a specific terminal symbol, or `None`.

### `Dfa.isDeterministic`

```fsharp
val isDeterministic: DFA<'t, 's> -> bool
```

Checks whether the DFA is deterministic: for each state and each alphabet symbol, there is at most one target state.

### `Dfa.accept`

```fsharp
val accept: DFA<'t, 's> -> Terminal<'t> list -> bool
```

DFA acceptance — sequential state transitions. Follows input symbols one by one; accepts iff the final state is accepting.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Automaton wraps `Graph<'s, ...>` | Separates graph structure (vertices + edges) from automaton semantics (start/final state annotations), following the book's hierarchy. |
| `.states` and `.transitions` as member properties | Backward compatibility: existing code that accesses `nfa.states` or `dfa.transitions` works unchanged. |
| `AutomatonLabel<'t>` DU for epsilon in matrix | Eliminates the separate `epsTransitions: Set<int * int>` field. Epsilon transitions are a first-class edge label, simplifying the data model. Callers of `fromTransitions` still pass epsilon transitions as a convenience set. |
| `epsilonClosure` scans matrix | After epsilon moves into the matrix, closure requires scanning rows for `AEpsilon` edges rather than iterating a Set. O(n²) per fixed-point iteration but only called in `accept` and `toDfa`. |
| `move`/`alphabet`/`isDeterministic` match `ATerm` | These operations are only meaningful for terminal symbols; `AEpsilon` is explicitly skipped. |
| Single Kronecker for intersection (vs per-label) | `LinearAlgebra.kron` with `intersectEdgeSets` avoids `BooleanDecomposition`, per-label loops, and OR-summation. |
| `intersectEdgeSets` as shared helper | Both `Nfa.intersect` and `KroneckerRPQ.evaluate` use the same set-intersection operation on Kronecker products. |
| `Matrix.reduceByColumn` for column-wise OR | Replaces manual double-nested loops collapsing MS-BFS result rows into a single boolean array. |
| `Graph.keepVertices` for vertex removal | Instead of manual transition collection + filtered graph, a single call drops useless states and reindexes. |
| Transition matrix over `Option<NonEmptySet<AutomatonLabel<'t>>>` | Uses `NonEmptySet` to prevent empty transition labels; supports multiple symbols between same pair of states. |
| NFA has epsilon transitions | Explicit epsilon transitions enable natural encoding of regular expression operations (union, star). |
| DFA has single start state, no epsilon | Deterministic semantics: exactly one transition per (state, symbol) pair. |
| Generic over `'t` and `'s` | Reusable for LR(0) (with `Set<LR0Item>` states), LR(1) (with `Set<LR1Item>` states), RSM blocks, and RPQ query automata. |

## Book Relationship

The automaton module provides the generic finite automaton infrastructure used by:
- LR automata construction (`LRAutomaton.buildLR0`, `buildLR1`) with `Symbol<'t,'nt>` labels.
- RSM builder (`RsmBuilder`) to construct deterministic blocks from EBNF rules via Brzozowski derivatives.
- RPQ algorithms (`BelyaninRPQ`, `KroneckerRPQ`, `ArroyueloRPQ`) with string-labeled query automata.
- Automaton intersection (`Nfa.intersect`) — classical product construction implemented via Kronecker product + MS-BFS.
