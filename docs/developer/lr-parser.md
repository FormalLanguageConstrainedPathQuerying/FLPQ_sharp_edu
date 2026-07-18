# LR Parser Module

**Tags:** algorithm, parsing, lr, shift-reduce, automaton, subset-construction, derivation-tree, grammar
**Kind:** algorithm
**Module:** LRParser
**Source:** `src/FLPQ.Languages/LRParser.fs`
**Depends on:** Grammar, Automaton, DerivationTree, FirstFollow
**Used by:** FLPQ.Cli
**Book reference:** LR parsing chapters

> **Abstract:** Implements LR(0), SLR(1), and CLR(1) parsing table construction and an LR parser interpreter with derivation tree building. Also provides LR(0) and LR(1) automaton construction as cases of the generic deterministic finite automaton type from `Automaton.fs`. Uses unified stack (states + tree nodes), augmented grammar for accept detection, and conflict recording.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [LRAutomaton Functions](#lrautomaton-functions)
- [LRParser Functions](#lrparser-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

### LR Automaton Construction

LR automata are built via BFS state-space exploration from the initial closure state:
1. Start with closure of `S' → ·S` (augmented grammar's first item).
2. For each discovered state and each grammar symbol X, compute `goto(state, X)`:
   - Advance dot past X in all items where dot precedes X.
   - Apply closure to the result.
3. Add transitions and new states; repeat until fixpoint.

### Table Construction

**LR(0):** Completed items reduce on ALL grammar terminals + end-of-input `Epsilon`. Most non-trivial grammars have conflicts.

**SLR(1):** Reduces restricted to terminals in LHS nonterminal's follow set.

**CLR(1):** Uses lookahead from LR(1) items for precise reduce decisions. Most powerful — resolves all conflicts SLR(1) cannot.

### Parser (Shift-Reduce Interpreter)

Uses a unified stack of `LRStackFrame` (state + tree node):
1. **Shift**: push `LRSymbol(Leaf(token))` then `LRState(nextState)`, advance input.
2. **Reduce**: pop `2·|β|` frames (state + tree pairs), build `Node(lhs, children)`, push `LRSymbol(Node(...))` then `LRState(gotoState)`.
3. **Accept**: triggered by `S' → S·` item.

A step limit of 10000 prevents infinite loops from epsilon-reduction cycles.

## Type Definitions

### LR(0) Item
```fsharp
type LR0Item =
    { Lhs: Nonterminal<string>
      Rhs: Symbol<string, string> list
      Dot: int }
```
Represents a production rule with a dot marking how much of the RHS has been consumed. E.g., `A → α·β`.

### LR(1) Item
```fsharp
type LR1Item =
    { Lhs: Nonterminal<string>
      Rhs: Symbol<string, string> list
      Dot: int
      Lookahead: Symbol<string, string> }
```
Extends LR(0) items with a lookahead symbol for more precise reduce decisions. End-of-input lookahead is `Epsilon`.

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
Records conflicts detected during table construction. Stored as data (not exceptions) so callers can inspect them.

### LR Table
```fsharp
type LRTable =
    { action: Map<int * Symbol<string, string>, LRAction>
      goto: Map<int * Nonterminal<string>, int>
      conflicts: LRConflict list }
```
`action` maps `(stateIndex, terminalString) → LRAction`. `goto` maps `(stateIndex, nonterminal) → stateIndex`.

## LRAutomaton Functions

### `buildLR0`
```fsharp
val buildLR0: Grammar<string, string> -> Automaton<Symbol<string, string>, Set<LR0Item>>
```
Builds the canonical LR(0) automaton via closure + BFS goto exploration.

### `buildLR1`
```fsharp
val buildLR1: Grammar<string, string> -> Automaton<Symbol<string, string>, Set<LR1Item>>
```
Builds the canonical LR(1) automaton. Same BFS structure as LR(0), but `closureLR1` propagates lookahead via firstK computations.

## LRParser Functions

### `buildLR0Table`
```fsharp
val buildLR0Table: Grammar<string, string> -> LRTable
```
LR(0) table: completed items reduce on ALL grammar terminals plus end-of-input `Epsilon`.

### `buildSLR1Table`
```fsharp
val buildSLR1Table: Grammar<string, string> -> LRTable
```
SLR(1) table: uses `followK` sets to restrict reduce actions.

### `buildCLR1Table`
```fsharp
val buildCLR1Table: Grammar<string, string> -> LRTable
```
CLR(1) table: uses lookahead from LR(1) items for precise reduce decisions.

### `parse`
```fsharp
val parse: Grammar<string, string> -> LRTable -> string -> Option<DerivationTree<string, string>>
```
Parses an input string using an LR parsing table. Returns `Some(tree)` on success, `None` on failure or if the step limit is exceeded. Each character is treated as a separate terminal token.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Automaton as Generic DFA | Leverages `Automaton<Symbol, Set<items>>` — states are item sets, transitions are grammar symbols |
| Conflict detection as data, not exceptions | `LRTable.conflicts` list records all conflicts; callers can inspect them |
| Conflict resolution: prefer shift, then first reduce | Standard LR practice: shifts added first from transitions, then reduces for unoccupied slots |
| Unified stack (states + tree nodes) | Single `LRStackFrame` type holds both LR states and tree symbols — avoids parallel stack bookkeeping |
| Augmented grammar internally | `S' → S` ensures accept detection; fresh start symbol guards against name collisions |
| Step limit of 10000 | Prevents infinite loops from epsilon-reduction cycles |
| Single-character tokens | Reference implementation simplicity; multi-character tokenization is outside scope |

## Book Reference

Corresponds to the LR parsing chapters of the book. LR(0)/SLR(1)/CLR(1) table construction follows the standard textbook algorithms. LR automata leverage the generic finite automaton infrastructure from `Automaton.fs`.

## See Also

- [LL parser](ll-parser.md) — top-down counterpart
- [Automaton module](automaton.md) — generic DFA type
- [DerivationTree module](derivation-tree.md) — derivation tree types
- [First/Follow module](first-follow.md) — followK for SLR(1)
- [Grammar module](grammar.md) — grammar types, ExtendedGrammar
