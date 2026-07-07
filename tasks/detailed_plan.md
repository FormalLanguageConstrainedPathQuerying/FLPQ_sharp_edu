# Detailed Plan: Task 138 - Implement RNGLR for RSM

## Goal
Implement RNGLR (Right-Nulled Generalized LR) for RSM. RNGLR is the LR-based counterpart to GLL (task 137), sharing SPPF, PathIndex, and tree extraction infrastructure.

## Architecture

### New files in `src/FLPQ.Languages/`:
- `RnglrTypes.fs` — RNGLR types: LR item over RSM, RnglrGSS, RnglrTable
- `RnglrLR.fs` — LR(0) table construction over RSM blocks  
- `Rnglr.fs` — Core algorithm: layered shift-then-reduce, reduction via intersection

### New files in `tests/`:
- `tests/FLPQ.Languages.Tests/RnglrTests.fs` — all RNGLR tests

### Compilation order in .fsproj:
```
RnglrTypes.fs → RnglrLR.fs → Rnglr.fs
```
Placed after Gll.fs.

## Sub-tasks

### 1. RnglrTypes.fs — Type definitions

- `RnglrItem<'nt>`: struct { blockNonterminal, rsmState } — LR item over RSM
- `RnglrAction`: Shift of int | Reduce of Nonterminal<'nt> | Accept — LR actions
- `RnglrTable<'t,'nt>`: { action: Map<(int * Symbol<'t,'nt>), RnglrAction<'nt>>; goto: Map<(int * Nonterminal<'nt>), int>; automaton: DFA<Symbol<'t,'nt>, Set<RnglrItem<'nt>>> }
- `RnglrGssVertex`: struct { lrState: int; inputVertex: int } — GSS vertex
- `RnglrGssEdge`: struct { symbol: Symbol<'t,'nt> } — GSS edge label
- `RnglrGSS<'t,'nt>`: { graph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge>>>; storedReductions: Map<Nonterminal<'nt>, Set<int * int>> array }
- Module `RnglrGSS`: init, addEdge, getStoredReductions, setStoredReductions, clearStoredReductions

### 2. RnglrLR.fs — LR(0) table construction

Function `buildLR0Table : RSM<'t,'nt> -> RnglrTable<'t,'nt>`:
- Closure: for item (N,q), if DFA has RNonterm(M) transition from q, add (M, start_M)
- Goto: advance items by following DFA transitions matching the symbol
- Build DFA via Automaton.buildAutomaton
- Actions: shift on terminal, reduce when rsmState is final, accept for start item at end

### 3. Rnglr.fs — Core algorithm

Function `buildPathIndex : RSM<'t,'nt> -> Graph<int, Option<'t>> -> PathIndex<'t,'nt>`:
- Pre-process: build LR table, flatten RSM states (reuse collectRsmData pattern), pre-allocate GSS
- Layered processing for string input (vertices 0..n):
  - Initialize GSS at (lrState_0, 0), process initial reductions
  - For v = 0 to n-1: shift phase, then reduce phase
  - For each reduction: invert RSM block, intersect with backward-reachable GSS, find predecessors, create edges

Helper `invertRsmBlock : RsmBlock<'t,'nt> -> NFA<RsmSymbol<'t,'nt>, int>`:
- Reverse all DFA transitions, swap start/final states

Helper `buildGssNfa : RnglrGSS<'t,'nt> -> int -> int -> NFA<RsmSymbol<'t,'nt>, int>`:
- Build backward-reachable NFA from GSS vertex via outgoing edges, mapping Symbol→RsmSymbol

### 4. Tests

- Equivalence with CYK ([<Property>])
- Equivalence with GLL ([<Property>])  
- Acceptance fact tests
- Reduction cascade test

## Key design decisions

1. **LR(0) not LR(1)**: Simpler, sufficient for RNGLR concept. LR(1) can be added later by extending the closure with lookaheads computed from RSM+FIRST/FOLLOW.

2. **String input only**: Initial implementation handles linear string graphs. Extension to general graphs is possible via topological layers.

3. **Reuse GLL infrastructure**: SPPF, PathIndex, buildSppfFromIndex, extractDerivationTree, stringToGraph — all reused as-is. Only the path index POPULATION differs.

4. **storedReductions as array**: Same pattern as GLL's storedPops — mutable array indexed by GSS vertex, avoids struct mutation issues with Graph's immutable Map.

## Blocked issues (awaiting user guidance)

### 1. LR goto entries missing for nested nonterminal calls

**Which tests fail**: `S -> a S | b accepts a a b`, `S -> a S b S | eps accepts a b a b`, `S -> a S b S | eps accepts a a b b`, left-recursive `S -> a S | a accepts a a a`

**Algorithmic gap**: The LR(0) closure over RSM items `(blockNonterminal, rsmState)` correctly adds items for nonterminal transitions, but the goto table lacks entries for `(lrState, N)` where N is the nonterminal being reduced. When a reduction by N completes and finds predecessor `(lrStatePre, vPre)`, the lookup `goto(lrStatePre, N)` returns None because the LR automaton does not have transitions on Symbol.N(N) from state `lrStatePre`. This is because the augmented start state does not include a wrapper item like `S' → S` that would provide a goto entry.

**What was tried**: 
- Attempted direct acceptance for the start nonterminal with no predecessors (works for empty input but not for non-empty chains).
- Attempted treating the situation where `findPredecessors` returns the initial state as an accept case (works for S → a b but not for S → a S | b due to missing goto entries for intermediate reductions).

**Help needed**: Should the LR table construction include an augmented start item? Or should reductions at the top level be handled differently (accept when predecessor is initial state and no goto exists)?

### 2. Right-nullable chain cascading incomplete

**Which tests fail**: `S -> A B, A -> a A | eps, B -> b B | eps accepts a b`, `S -> A B, A -> a A | eps, B -> b B | eps accepts a a b`, `S -> A B, A -> a A | eps, B -> b B | eps accepts empty`

**Algorithmic gap**: The reduce phase processes reductions once per GSS vertex but does not iterate to fixpoint at a layer. When A reduces via epsilon at layer 0, the resulting GSS edge triggers a reduction by S (since S → A B requires A). But this cascading reduction is not re-processed. The `tryEnqueue` adds `(gotoTarget, v)` to the queue, but it is dequeued in the main loop which interleaves with shift processing rather than completing all reductions at the current layer first.

**What was tried**: The current code enqueues `gotoTarget` for reduction at the same layer, but the while loop may process shifts at other layers before coming back to complete reductions at this layer. This means the cascade is not guaranteed to complete before the layer is considered done.

**Help needed**: Should the reduce phase iterate to fixpoint at each layer (like the task spec describes: "reductions may cascade")? Should there be a separate inner loop for reductions vs. the outer shift loop?
