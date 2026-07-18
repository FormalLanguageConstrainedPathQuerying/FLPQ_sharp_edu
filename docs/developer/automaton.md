# Automaton Module

**Tags:** data-structure, automaton, nfa, dfa, epsilon-closure, subset-construction, product-construction, graph
**Kind:** data-structure
**Module:** Automaton
**Source:** `src/FLPQ.Languages/Automaton.fs`
**Depends on:** Graph, Matrix
**Used by:** LLParser, LRParser, EbnfParser, RSM, GLL, RNGLR, BelyaninRPQ, ArroyueloRPQ, KroneckerRPQ
**Book reference:** Chapter 5 (finite automata), Chapter 6 (RSM)

> **Abstract:** Generic finite automaton types and operations: nondeterministic (NFA) and deterministic (DFA). Both wrap the generic `Graph` type. Epsilon transitions are stored as `AEpsilon`-labeled edges in the transition matrix. Provides NFA-to-DFA subset construction, epsilon closure, intersection via Kronecker product + MS-BFS, and acceptance algorithms. Used as the basis for LR automata, RSM blocks, RPQ query automata, and automaton intersection.

## Contents

- [Data Structure](#data-structure)
- [Type Definitions](#type-definitions)
- [Nfa Module Functions](#nfa-module-functions)
- [Dfa Module Functions](#dfa-module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Data Structure

Automata wrap `Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>` — vertices are state labels, edges are transition label sets. The separation follows the book's hierarchy: a graph is a generic structure, and automata are graphs with additional semantic annotations (start/final state sets). This enables reuse of graph operations (vertex counting, filtering, keepVertices) without reimplementation.

### `AutomatonLabel<'t>`
```fsharp
type AutomatonLabel<'t> =
    | ATerm of 't
    | AEpsilon
```
Distinguishes terminal symbols from epsilon in the transition matrix. Eliminates the separate `epsTransitions: Set<int * int>` field.

### `Config` (struct)
```fsharp
[<Struct>]
type Config =
    { state: int
      position: int }
```
A configuration in automaton acceptance: a state index and current input position.

### `NFA<'t, 's>`
```fsharp
type NFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startStates: Set<int>
      finalStates: Set<int> }
```
Nondeterministic finite automaton with multiple start states. Epsilon transitions stored in the matrix as cells containing `Some nes` where `nes` includes `AEpsilon`.

### `DFA<'t, 's>`
```fsharp
type DFA<'t, 's when 't: comparison> =
    { graph: Graph<'s, Option<NonEmptySet<AutomatonLabel<'t>>>>
      startState: int
      finalStates: Set<int> }
```
Deterministic finite automaton with exactly one start state and no epsilon transitions.

## Nfa Module Functions

### Construction and Conversion
```fsharp
val buildMatrix: int -> (int * AutomatonLabel<'t> * int) list -> Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>
val fromTransitions: 's list -> (int * 't * int) list -> Set<int * int> -> Set<int> -> Set<int> -> NFA<'t, 's>
val toDfa: NFA<'t, 's> -> DFA<'t, Set<int>>
```

### Operations
```fsharp
val stateCount: NFA<'t, 's> -> int
val alphabet: NFA<'t, 's> -> Set<'t>
val move: NFA<'t, 's> -> int -> 't -> Set<int>
val epsilonClosure: NFA<'t, 's> -> int -> Set<int>
val moveSet: NFA<'t, 's> -> Set<int> -> 't -> Set<int>
val accept: NFA<'t, 's> -> Terminal<'t> list -> bool
```

### Intersection
```fsharp
val intersectEdgeSets:
    Option<NonEmptySet<AutomatonLabel<'t>>> -> Option<NonEmptySet<AutomatonLabel<'t>>>
    -> Option<NonEmptySet<AutomatonLabel<'t>>>
val intersect: NFA<'t, 's> -> NFA<'t, 'v> -> NFA<'t, 's * 'v>
```

## Dfa Module Functions

```fsharp
val fromTransitions: 's list -> (int * 't * int) list -> int -> Set<int> -> DFA<'t, 's>
val stateCount: DFA<'t, 's> -> int
val alphabet: DFA<'t, 's> -> Set<'t>
val move: DFA<'t, 's> -> int -> 't -> int option
val isDeterministic: DFA<'t, 's> -> bool
val accept: DFA<'t, 's> -> Terminal<'t> list -> bool
```

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Automaton wraps `Graph<'s, ...>` | Separates graph structure from automaton semantics, following the book's hierarchy |
| `AutomatonLabel<'t>` DU for epsilon in matrix | Eliminates separate `epsTransitions` field; epsilon transitions are first-class edge labels |
| `epsilonClosure` scans matrix | After epsilon moves into matrix, closure requires scanning rows for `AEpsilon` edges |
| `move`/`alphabet` skip `AEpsilon` | These operations are only meaningful for terminal symbols |
| Single Kronecker for intersection | `LinearAlgebra.kron` with `intersectEdgeSets` avoids per-label loops and OR-summation |
| `intersectEdgeSets` as shared helper | Both `Nfa.intersect` and `KroneckerRPQ.evaluate` use same set-intersection operation |
| Transition matrix over `Option<NonEmptySet<AutomatonLabel<'t>>>` | Prevents empty transition labels; supports multiple symbols between same pair of states |
| Generic over `'t` and `'s` | Reusable for LR items, RSM blocks, RPQ query automata, and custom state types |

## Book Reference

The automaton module provides the generic finite automaton infrastructure used by:
- LR automata construction with `Symbol<'t,'nt>` labels
- RSM builder constructing deterministic blocks from EBNF rules via Brzozowski derivatives
- RPQ algorithms with string-labeled query automata
- Automaton intersection — classical product construction via Kronecker product + MS-BFS

## See Also

- [Graph module](graph.md) — underlying graph type
- [RSM module](rsm.md) — RSM blocks as DFAs
- [LR parser](lr-parser.md) — uses Automaton for LR state machines
- [Kronecker RPQ](kronecker-rpq.md) — uses `intersectEdgeSets` and intersection
