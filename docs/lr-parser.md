# LR Parser Module Design and Logic

## Overview

Implements LR(0), SLR(1), and CLR(1) parsing table construction and an LR parser
interpreter with derivation tree building. Also provides LR(0) and LR(1) automaton
construction as cases of the generic deterministic finite automaton type from `Automaton.fs`.

## Files

| File | Purpose |
|------|---------|
| `src/FLPQ.Core/LRParser.fs` | LR item types, automata construction, table building, parser |
| `docs/lr-parser.md` | This documentation |

## Relation to the Book

Corresponds to the LR parsing chapters of the book.

## Type Definitions

### LR(0) Item

```fsharp
type LR0Item =
    { Lhs: Nonterminal<string>
      Rhs: Symbol<string, string> list
      Dot: int }
```

Represents a production rule with a dot marking how much of the RHS has been consumed.
E.g., `A → α·β` means `α` has been parsed and `β` is yet to be parsed.

### LR(1) Item

```fsharp
type LR1Item =
    { Lhs: Nonterminal<string>
      Rhs: Symbol<string, string> list
      Dot: int
      Lookahead: Symbol<string, string> }
```

Extends LR(0) items with a lookahead symbol for more precise reduce decisions.
E.g., `A → α·β, a` means reduce `A → αβ` only when the next input token is `a`.
End-of-input lookahead is represented as `Epsilon` (matching the Symbol type).

### LR Action

```fsharp
type LRAction =
    | Shift of int      // Push state and consume input
    | Reduce of int     // Pop stack by rule, push goto state
    | Accept            // Accept input
```

### LR Conflict

```fsharp
type LRConflict =
    | ShiftReduce of state: int * symbol: string * shiftTo: int * reduceRule: int
    | ReduceReduce of state: int * symbol: string * rule1: int * rule2: int
```

Records conflicts detected during table construction. Stored as data (not exceptions)
so callers can inspect them.

### LR Table

```fsharp
type LRTable =
    { action: Map<int * Symbol<string, string>, LRAction>
      goto: Map<int * Nonterminal<string>, int>
      conflicts: LRConflict list }
```

`action` maps `(stateIndex, terminalString) → LRAction`.
`goto` maps `(stateIndex, nonterminal) → stateIndex`.

## Design Decisions

### Automaton as Generic DFA (Task 17)

LR automata are returned as `Automaton<Symbol<string, string>, Set<LR0Item>>` and
`Automaton<Symbol<string, string>, Set<LR1Item>>`, leveraging the generic finite
automaton type from `Automaton.fs`. This means:
- State labels are sets of items
- Transition labels are grammar symbols
- Start state is always index 0 (the initial closure state)
- Final states are those containing the augmented rule's completed item `S' → S·`

### Conflict Detection (Task 18)

Conflicts are detected during table construction and recorded in the `LRTable.conflicts`
field. The resolution strategy follows standard LR practice:
- **Shift-reduce**: prefer shift (added first from transitions)
- **Reduce-reduce**: keep the first rule encountered

This is done by adding shift actions first (from the automaton transitions), then
attempting to add reduce actions only for unoccupied table slots.

### Parser (Task 19)

The parser is a stack-based shift-reduce interpreter:
- State stack tracks current parser state indices
- Tree stack tracks derivation tree fragments
- On shift: push state + leaf node, advance input
- On reduce: pop  children, build nonterminal node, use goto to push new state
- On accept: return the single remaining tree node

A step limit of 10000 prevents infinite loops from epsilon-reduction cycles.

### Augmentation

All table construction and parsing uses an augmented grammar internally:
`S' → S` where `S'` is a fresh start symbol. This ensures the parser knows when to
accept (the item `S' → S·` triggers Accept).

## Module `LRAutomaton`

### Functions

#### `buildLR0`
```fsharp
val buildLR0: Grammar<string, string> -> Automaton<Symbol<string, string>, Set<LR0Item>>
```
Builds the canonical LR(0) automaton. Each state is computed via the `closureLR0`
function, then BFS explores all reachable states through `gotoLR0` transitions.

#### `buildLR1`
```fsharp
val buildLR1: Grammar<string, string> -> Automaton<Symbol<string, string>, Set<LR1Item>>
```
Builds the canonical LR(1) automaton. Same BFS structure as LR(0), but `closureLR1`
propagates lookahead symbols using `firstK` computations.

#### `closureLR0` (private)
```fsharp
val closureLR0: Rule<string, string> list -> Set<LR0Item> -> Set<LR0Item>
```
Adds all item derivations reachable without consuming input. For each item with dot
before a nonterminal `B`, adds `B → ·γ` for all rules of `B`.

#### `gotoLR0` (private)
```fsharp
val gotoLR0: Rule<string, string> list -> Set<LR0Item> -> Symbol<string, string> -> Set<LR0Item>
```
Advances the dot past a symbol and computes closure. Filters items with dot before the
symbol, advances the dot, then applies closure.

#### `closureLR1` (private)
Like `closureLR0` but also propagates lookahead terminals. When adding
`B → ·γ, l`, the lookahead `l` is computed from `first_k(β l)` where `β` is the
string following `B` in the source item.

#### `gotoLR1` (private)
Like `gotoLR0` but preserves lookahead terminals from the source items.

## Module `LRParser`

### Table Building Functions

#### `buildLR0Table`
```fsharp
val buildLR0Table: Grammar<string, string> -> LRTable
```
No lookahead information: completed items reduce on ALL grammar terminals plus
end-of-input marker `Epsilon`. Most non-trivial grammars will have conflicts.
Epsilon items (`rhs = [Epsilon]`) are treated as immediately completed (dot at 0 counts as completed).

#### `buildSLR1Table`
```fsharp
val buildSLR1Table: Grammar<string, string> -> LRTable
```
Uses `followK` sets to restrict reduce actions. Completed items reduce only on
terminals in the LHS nonterminal's follow set.

#### `buildCLR1Table`
```fsharp
val buildCLR1Table: Grammar<string, string> -> LRTable
```
Uses lookahead from LR(1) items for precise reduce decisions. The most powerful
LR construction — resolves all conflicts that SLR(1) cannot.

### Parser Function

#### `parse`
```fsharp
val parse: Grammar<string, string> -> LRTable -> string -> Option<DerivationTree<string, string>>
```
Parses an input string using an LR parsing table. Returns `Some(tree)` on success,
`None` on failure or if the step limit is exceeded. Each character is treated as
a separate terminal token.

Preconditions: none (handles all inputs gracefully).
Postconditions: if `Some(tree)`, then `leaves(tree)` concatenated equals the input string
(modulo epsilon nodes, which produce no leaves).

#### `leaves`
```fsharp
val leaves: DerivationTree<string, string> -> string list
```
Collects all leaf terminal strings from a derivation tree in left-to-right order.
Delegates to `LLParser.leaves`.

## Testing

Tests are in `tests/FLPQ.Core.Tests/LRParserTests.fs`:

| Test Category | What is Verified |
|---------------|-----------------|
| Grammar1 | SLR(1)/CLR(1) acceptance, rejection, tree leaves |
| Grammar2 | Tables buildable, conflicts detected (ambiguous grammar) |
| Grammar3 | SLR(1)/CLR(1) acceptance, rejection, tree leaves, no conflicts |
| Grammar6 | All table types have conflicts (ambiguous arithmetic) |
| Grammar7 | SLR(1)/CLR(1) acceptance, rejection, tree leaves, no conflicts |
| Grammar8 | SLR(1)/CLR(1) acceptance, rejection, tree leaves, no conflicts |
| Automaton | Expected structure: states > 1, deterministic, one start/final state |
| Cross-parser | SLR(1) and CLR(1) agree on acceptance for each grammar |
| Cross-grammar | grammar7 and grammar8 CLR(1) agree on same-language strings |
| Property tests | Leaves match input, SLR(1)/CLR(1) agree, for random strings |

## Limitations

- Current implementation uses single-character tokens. Multi-character tokenization
  is outside the scope of this reference implementation.
- Step limit of 10000 prevents infinite loops but also limits maximum parse depth.
- Grammar2 (`S → aSb | eps | SS`) is ambiguous and not LR(k) for any k; tables
  contain conflicts and the parser may fail to parse even valid strings.
- Grammar6 (ambiguous arithmetic) has conflicts in all table types.
