# RNGLR for RSM

**Tags:** algorithm, parsing, rnglr, cfpq, lr, rsm, path-index, gss, sppf, product-construction, shift-reduce
**Kind:** algorithm
**Module:** Rnglr
**Source:** `src/FLPQ.Languages/Rnglr.fs`
**Depends on:** Matrix, Graph, RSM, PathIndex, Automaton, RnglrTypes, RnglrLR
**Used by:** FLPQ.Cli, TestHelpers
**Book reference:** Section sec:CFPQ_RNGLR (Chapter 6)

> **Abstract:** Implements Right-Nulled Generalized LR (RNGLR) parsing for Recursive State Machines — the LR-based counterpart to GLL. RNGLR builds a **path index** during execution using a layered shift-then-reduce approach with per-vertex fixpoint. Reductions are processed by traversing the GSS backwards through inverted RSM block DFAs (product construction). The SPPF is built separately from the index. The LR(0) automaton is adapted for RSM items.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [GSS Module Functions](#gss-module-functions)
- [Core Function Signatures](#core-function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

The core algorithm is layered: **shift** all terminals at a vertex, then a **fixpoint** loop of reductions until no more new GSS edges or path index entries are produced.

### buildPathIndex

1. **Initialization**: Extend RSM with fresh start `S'`, build LR(0) parsing table, pre-allocate path index matrix (size K_rsm × K_rsm), pre-allocate GSS, build inverted RSM block data (reverse transition maps).

2. **Main loop** — vertex-by-vertex, layered shift-then-reduce with fixpoint:
   - **Shift phase**: For each terminal edge `v --t--> vNext` in the input graph, if current LR state has a Shift action on `t`, create GSS edge and consume stored states for product BFS continuation. Enqueue target for reduction.
   - **Reduction phase** (fixpoint at each vertex): For each pending `(lrState, vertex)` pair, find items at final states of blocks → reduce via `findPredecessors` (product BFS through inverted RSM block) → apply Goto → create GSS edge. Cascades recursively.

3. **Product BFS**: Following GSS edges backwards through inverted RSM transitions, adding PTerminal, PNonterminal, and PIntermediate entries to the path index. Reaches block start states to find predecessors.

4. **Acceptance**: Check path index cell `(startGlobalState, 0) → (finalGlobalState, vertexCount - 1)`.

### processReduction

For each predecessor found by product BFS:
1. Look up LR Goto table for `(lrStatePre, reduceNt)` → gotoTarget.
2. Create GSS edge `(gotoTarget, vEnd) --N(reduceNt)--> gssIdxPre`.
3. PEpsilonNonterminal only when `vPre = vEnd` and `finalRsmState = globalStart` (true epsilon).
4. Cascade recursively: new GSS vertex may trigger further reductions.

## Type Definitions

### RnglrItem (struct)
```fsharp
[<Struct>]
type RnglrItem<'nt> = { BlockNonterminal: Nonterminal<'nt>; RsmState: int }
```
An LR item over an RSM: a position in a specific RSM block's DFA. Unlike grammar-based LR items (production + dot position), RSM items track which block nonterminal and which state within that block.

### RnglrDescriptor (struct)
```fsharp
[<Struct>]
type RnglrDescriptor = { LrState: int; Vertex: int }
```
A descriptor in the RNGLR worklist: a parsing position (LR automaton state, input graph vertex). Unlike GLL descriptors, the GSS vertex is derivable as `lrState * vertexCount + vertex`, and range tracking is handled by the product BFS (storedStates mechanism). Serves as the worklist item in per-vertex pending queues and deduplication sets.

### RnglrTable
```fsharp
type RnglrTable<'t, 'nt> =
    { Action: Map<int * Symbol<'t, 'nt>, LRAction<Nonterminal<'nt>>>
      Goto: Map<int * Nonterminal<'nt>, int>
      Automaton: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>> }
```
LR(0) parsing table built from the extended RSM's automaton.

### RnglrGssVertex (struct)
```fsharp
[<Struct>]
type RnglrGssVertex = { LrState: int; InputVertex: int }
```
A vertex in the RNGLR Graph-Structured Stack: (LR automaton state, input graph vertex). Pre-allocated: |Q_lr| × |V| vertices.

### RnglrGssEdge (struct)
```fsharp
[<Struct>]
type RnglrGssEdge<'t, 'nt> = { EdgeSymbol: Symbol<'t, 'nt> }
```
GSS edge labeled with the grammar symbol recognized at this step. Multiple edges between same pair possible → `NonEmptySet`.

### RnglrGSS
```fsharp
type RnglrGSS<'t, 'nt> =
    { GssGraph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      StoredStates: Set<Nonterminal<'nt> * int * int * int> array }
```
The RNGLR Graph-Structured Stack. `StoredStates[i]` caches intermediate automaton intersection states: `Set<Nonterminal * invState * rangeEndState * rangeEndVertex>`. Each tuple records an in-progress backwards traversal through a block's inverted DFA.

## GSS Module Functions

| Function | Description |
|----------|-------------|
| `linearIndex vertexCount lrState inputVertex` | Maps `(lrState, inputVertex)` to a linear GSS index: `lrState * vertexCount + inputVertex` |
| `init lrStateCount vertexCount` | Pre-allocates the GSS with all |Q_lr| * |V| vertices |
| `addEdge gss fromIdx toIdx label` | Adds edge, returns and clears StoredStates[fromIdx] |
| `getStoredStates gss gssIdx` | Reads StoredStates[gssIdx] without clearing |
| `setStoredStates gss gssIdx states` | Writes storedStates for a GSS vertex |
| `outgoingEdges gss gssIdx` | Enumerates all (targetIdx, symbol) pairs departing from gssIdx |

## Core Function Signatures

### Rnglr.buildPathIndex
```fsharp
val buildPathIndex:
    freshStart: Nonterminal<'nt> -> rsm: RSM<'t, 'nt> -> inputGraph: Graph<int, Option<'t>>
    -> PathIndex<'t, 'nt>
```
Core RNGLR algorithm — builds the path index through layered shift-then-reduce with per-vertex fixpoint.

### Rnglr.isAccepted
```fsharp
val isAccepted: pathIndex: PathIndex<'t, 'nt> -> extRsm: RSM<'t, 'nt> -> vertexCount: int -> bool
```
Checks whether the input graph is accepted. Inspects the path index cell `(startGlobalState, 0) → (finalGlobalState, vertexCount - 1)`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| S' reduces via Goto, not freshStart exclusion | `productBfs` traverses inverted S' block to add PNonterminal(S) at root range |
| `productBfs` adds PNonterminal for RNonterm transitions | Every inverse RNonterm step corresponds to a recognized nonterminal |
| PEpsilonNonterminal only when vPre=vEnd && finalRsmState=globalStart | True epsilon derivations only: start=final state in block and no input consumed |
| PNonterminal only from productBfs, not processReduction | Separates structural traversal from path index recording |
| StoredStates in mutable array outside Graph type | Same reason as GLL: value-copy structs prevent in-place mutation via vertexMap |
| Vertices pre-allocated as |Q_lr| * |V| | All possible GSS vertices exist from initialization |
| Deduplication of cascades via processedGotos | Prevents reprocessing same (reduceNt, predecessor) pair at same GSS vertex |
| Layered shift-then-reduce with per-vertex fixpoint | Ensures completeness at each input position before advancing |

## Book Reference

- Section sec:CFPQ_RNGLR — RNGLR for CFPQ over RSMs
- Chapter 6, `03_RecursiveAutomata.tex` — RSM definition
- Section sec:CFPQ_GLL — GLL parsing (counterpart algorithm, shared path index type)
- `RnglrLR.fs` — LR(0) table construction for RSM items

## See Also

- [GLL for RSM](gll.md) — LL-based counterpart sharing SPPF and PathIndex
- [SPPF module](sppf.md) — SPPF construction from path index
- [PathIndex module](path-index.md) — path index types and operations
- [RSM module](rsm.md) — Recursive State Machine model
- [LR parser](lr-parser.md) — standard LR infrastructure (LRAction, automaton construction)
