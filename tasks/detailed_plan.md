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
