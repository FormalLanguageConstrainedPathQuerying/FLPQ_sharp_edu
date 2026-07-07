# RNGLR for RSM

Implements Right-Nulled Generalized LR (RNGLR) parsing for Recursive State Machines (RSM).
Book reference: sec:CFPQ_RNGLR. RNGLR is the LR-based counterpart to GLL (task 137).

## Overview

RNGLR builds a **path index** during layered shift-then-reduce execution, sharing SPPF, PathIndex, and tree extraction infrastructure with GLL. Input strings are treated as linear graphs (vertices 0..n, edges i→i+1).

## Architecture

### Files

- `src/FLPQ.Languages/RnglrTypes.fs` — RNGLR types: LR item over RSM, RnglrGSS, LR table
- `src/FLPQ.Languages/RnglrLR.fs` — LR(0) table construction over RSM blocks
- `src/FLPQ.Languages/Rnglr.fs` — Core algorithm: layered processing, reduction via product BFS
- `tests/FLPQ.Languages.Tests/RnglrTests.fs` — Tests
- `docs/rnglr.md` — This documentation

### Reused from GLL

- `SppfNodeInfo`, `SppfEdgeLabel`, `SPPF` types (from `GllTypes.fs`)
- `PathIndex`, `RangeKey`, `RangeDescriptor`, `PathIndexEntry` types
- `buildSppfFromIndex`, `extractDerivationTree` (from `Gll.fs`)
- `stringToGraph` for input conversion

## Types

### RnglrTypes.fs

**LR item over RSM**:
```fsharp
[<Struct>] RnglrItem<'nt> = { blockNonterminal: Nonterminal<'nt>; rsmState: int }
```
An LR item is a position (local state) in an RSM block's DFA.

**LR actions and table**:
```fsharp
RnglrAction<'nt> = Shift of int | Reduce of Nonterminal<'nt> | Accept
RnglrTable<'t,'nt> = { action: Map<(int * Symbol), RnglrAction>; goto: Map<(int * Nonterminal), int>; automaton: DFA<Symbol, Set<RnglrItem>> }
```

**RNGLR GSS**:
```fsharp
RnglrGssVertex = { lrState: int; inputVertex: int }
RnglrGssEdge<'t,'nt> = { symbol: Symbol<'t,'nt> }
RnglrGSS<'t,'nt> = { graph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge>>>; storedReductions: Map<Nonterminal, Set<int*int*int>>[] }
```
Bottom-up GSS with symbol-labeled edges. Pre-allocated with |Q_lr|×|V| vertices. `storedReductions[i]` caches reduction results at vertex i for replay when new edges arrive.

## Algorithm

### LR table construction (`RnglrLR.buildLR0Table`)

1. **Items**: Each LR item is `(blockNonterminal, rsmState)` — a position in an RSM block DFA.
2. **Closure**: For item (N,q), if DFA has transition q --RNonterm(M)--> _, add (M, startState_M).
3. **Goto**: Advance each item by following DFA transitions matching the symbol. Take closure of advanced set.
4. **Table**: Shift on terminal transitions. Reduce when rsmState is final. Accept for start nonterminal at final position.

### Core algorithm (`Rnglr.buildPathIndex`)

1. **Build LR(0) table** from RSM.
2. **Pre-allocate GSS** with all |Q_lr|×|V| vertices.
3. **Epsilon handling**: At layer 0, if initial LR state has reduce items, record PNonterminal directly.
4. **Layered processing** (for each vertex v):
   - **SHIFT**: For each GSS node (lrState, v), follow LR shift actions on matching graph edges. Add PTerminal entries. Handle stored reductions from newly created edges.
   - **REDUCE**: For each GSS node with reduce actions, perform **product BFS**:
     - Start at (gssIdx, eachOriginalFinalState)
     - Traverse: from (g, q), follow GSS outgoing edges labeled X and inverted RSM transitions from q on X
     - When reaching original start state of inverted RSM: found predecessor (lrStatePre, vPre)
     - For each predecessor: goto(lrStatePre, N) → gotoTarget. Add GSS edge and PNonterminal/PIntermediate entries.

### Reduction via product BFS

Instead of building full NFAs and intersecting, RNGLR uses a direct BFS on the product space (GSS vertex × inverted RSM state):
1. Start at the triggering GSS vertex and the original final states of the inverted RSM block.
2. At each step, match GSS outgoing edges with inverted RSM transitions on the same symbol.
3. When reaching the original start state in the inverted RSM (top of the reduction), the GSS vertex is a predecessor.
4. Cache results via `storedReductions` for replay when new edges arrive.

## Design Decisions

1. **LR(0) not LR(1)**: Simpler table construction, sufficient for demonstrating the RNGLR concept. LR(1) extension would add lookahead computation from RSM FIRST/FOLLOW sets.

2. **LR state-indexed path index**: PathIndex uses LR automaton state count (not RSM state count) for matrix sizing, since all entries reference LR states.

3. **Product BFS instead of NFA intersection**: Direct BFS on the product space avoids the complexity of building/dismantling NFAs and preserves the original GSS vertex indices needed for edge creation.

4. **String input only**: Current implementation handles linear graph (string) inputs. Extension to general graphs requires topological layer ordering.

## Limitations

- LR(0) only — lookaheads would improve precision on complex grammars.
- Goto entries for nonterminals within RSM blocks may be missing for deeply nested calls.
- Right-nullable chains with multiple epsilon nonterminals may not fully cascade (the reduce phase processes once per trigger, not iteratively until fixpoint).
- Grammar2 (S → S S) causes unbounded DFA states in RSM builder.
