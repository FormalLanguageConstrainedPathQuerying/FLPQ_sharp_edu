# Detailed Plan: Tasks 17, 18, 19 — LR Parser

## Task 17: LR Automata as Deterministic Finite Automata

Refactor `buildLR0Automaton` and `buildLR1Automaton` to return `Automaton<'t, 's>` types
instead of raw tuples. The automaton type is already defined in `Automaton.fs`:

```fsharp
type Automaton<'t, 's when 't: comparison> =
    { states: 's list
      transitions: Matrix<Set<'t>>
      startStates: Set<int>
      finalStates: Set<int> }
```

### Changes to LRParser.fs

1. `buildLR0Automaton` returns `Automaton<Symbol<string,string>, Set<LR0Item>>`
   - Keep BFS logic identical
   - Use `Automaton.fromTransitions` to construct result
   - `startStates = set [0]`
   - `finalStates`: states containing item `S' -> S·` (augmented rule with dot at end)

2. `buildLR1Automaton` returns `Automaton<Symbol<string,string>, Set<LR1Item>>`
   - Same approach as LR(0)
   - `startStates = set [0]`
   - `finalStates`: states containing item `S' -> S·, $` (augmented rule with dot at end)

3. Make both functions part of a public module `LRAutomaton` with:
   - `closureLR0`, `gotoLR0`, `buildLR0`
   - `closureLR1`, `gotoLR1`, `buildLR1`

### Design Decision
- Split the module into `LRAutomaton` (automata construction) and `LRParser` (table building + parsing)
- This follows the principle that the automaton is a separate concern from the parser

## Task 18: LR Parsing Tables

Build CLR(1), SLR(1), and LR(0) parsing tables.

### Changes to LRParser.fs

1. **Conflicts as data**, not exceptions: introduce `LRConflict` type:
   ```fsharp
   type LRConflict =
       | ShiftReduce of state: int * symbol: string * shiftTo: int * reduceRule: int
       | ReduceReduce of state: int * symbol: string * rule1: int * rule2: int
   ```

2. `LRTable` gets a `conflicts` field:
   ```fsharp
   type LRTable =
       { action: Map<int * string, LRAction>
         goto: Map<int * Nonterminal<string>, int>
         conflicts: LRConflict list }
   ```
   Some grammars are genuinely not LR(k) for any k; tables are still computed but conflicts are recorded.

3. For LR(0): reduce on ALL terminals (including "") for completed items.
4. For SLR(1): reduce only on follow set terminals for completed items.
5. For CLR(1): reduce on the lookahead from LR(1) items.

Simplify: keep internal LR table builders. Public API:
- `buildLR0Table(g) -> LRTable`
- `buildSLR1Table(g) -> LRTable`
- `buildCLR1Table(g) -> LRTable`

### Conflict resolution 
When a conflict occurs, prefer shift over reduce (standard LR approach). This allows parsing even of ambiguous grammars.

## Task 19: LR Parser Interpreter

Fix existing parser bugs and add tests.

### Bug fixes in LRParser.fs
1. Remove duplicate expression at line 367
2. Remove dead code (steps counter with outer while loop that never repeats) at lines 390-399

### Tests to add in LRParserTests.fs

#### Fact tests
1. SLR(1) grammar3: accept/reject lists, leaves match input
2. CLR(1) grammar3: accept/reject lists, leaves match input
3. SLR(1) grammar7: accept/reject lists, leaves match input
4. CLR(1) grammar7: accept/reject lists, leaves match input
5. SLR(1) grammar8: accept/reject lists, leaves match input
6. CLR(1) grammar8: accept/reject lists, leaves match input
7. grammar6 (ambiguous): verify conflicts are detected, parser still works for known good inputs
8. grammar1: SLR(1) and CLR(1) for accept/reject and leaves
9. grammar2: SLR(1) and CLR(1) for accept/reject and leaves

#### Property-based tests
1. grammar2 vs grammar1: SLR(1)/CLR(1) parsers agree on acceptance (for AbStringGenerator)
2. Leaves concatenation equals input (for any parser that succeeds)
3. grammar7 vs grammar8: SLR(1)/CLR(1) parsers agree on acceptance

### Grammar LR-compatibility summary
- grammar1 (S->aSbS|eps): Ambiguous, has conflicts in all LR types
- grammar2 (S->aSb|eps|SS): Ambiguous, similar to grammar1
- grammar3 (S->aS|a): NOT LR(0), IS SLR(1) and CLR(1)
- grammar6 (ambiguous arithmetic): Ambiguous, has conflicts
- grammar7 (E->E+T|T, ...): SLR(1) and CLR(1) should work
- grammar8 (E->T+E|T, ...): SLR(1) and CLR(1) should work

## Files
| File | Action |
|------|--------|
| `src/FLPQ.Core/LRParser.fs` | Refactor: split into LRAutomaton + LRParser modules, fix bugs |
| `tests/FLPQ.Core.Tests/LRParserTests.fs` | Rewrite with comprehensive tests |
| `docs/lr-parser.md` | Create documentation |
| `tasks/tasks.md` | Mark tasks 17, 18, 19 as done |
