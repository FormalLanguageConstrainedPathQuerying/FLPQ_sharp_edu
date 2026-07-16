# RNGLR for RSM

Implements Right-Nulled Generalized LR (RNGLR) parsing for Recursive State Machines (RSM) as described in the book section sec:CFPQ_RNGLR. The LR-based counterpart to GLL.

## Overview

RNGLR builds a **path index** (matrix) during execution. The **SPPF** (Shared Packed Parse Forest) is built separately from the index. Unlike standard LR parsing where GSS pops happen on reductions, RNGLR processes reductions by traversing the GSS backwards: it follows GSS edges in reverse through inverted RSM block DFAs to discover predecessors (product construction), then reduces via the LR goto table.

The core algorithm is layered: **shift** all terminals at a vertex, then a **fixpoint** loop of reductions until no more new GSS edges or path index entries are produced.

## Relation to the Book

Book section sec:CFPQ_RNGLR. Also relies on the LR(0) automaton construction adapted for RSM items (see `RnglrLR.fs`) and the RSM model (see `rsm.md`).

## Files

| File | Purpose |
|------|---------|
| `src/FLPQ.Languages/RnglrTypes.fs` | Type definitions: RnglrItem, RnglrTable, GSS vertex/edge, RnglrGSS with operations |
| `src/FLPQ.Languages/Rnglr.fs` | Core algorithm: buildPathIndex, isAccepted, processing logic |
| `src/FLPQ.Languages/RnglrLR.fs` | LR(0) table construction from RSM blocks |

## Type Definitions

### RnglrItem (struct)

```fsharp
[<Struct>]
type RnglrItem<'nt> = { BlockNonterminal: Nonterminal<'nt>; RsmState: int }
```

An LR item over an RSM: a position in a specific RSM block's DFA. Unlike grammar-based LR items (which track a production + dot position), RSM items track which block nonterminal and which state within that block. The item `{ BlockNonterminal = N, RsmState = q }` means the parser is inside block `N` at state `q`.

### RnglrTable

```fsharp
type RnglrTable<'t, 'nt> =
    { Action: Map<int * Symbol<'t, 'nt>, LRAction<Nonterminal<'nt>>>
      Goto: Map<int * Nonterminal<'nt>, int>
      Automaton: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>> }
```

The RNGLR parsing table built from the LR(0) automaton of the extended RSM. `Action` maps `(automatonState, symbol)` to Shift/Reduce/Accept actions. `Goto` maps `(automatonState, nonterminal)` to the target automaton state. `Automaton` is the deterministic LR(0) automaton with states labeled by sets of `RnglrItem`.

### RnglrGssVertex (struct)

```fsharp
[<Struct>]
type RnglrGssVertex = { LrState: int; InputVertex: int }
```

A vertex in the RNGLR Graph-Structured Stack. Represents a parser position: `(LR automaton state, input graph vertex)`. Each vertex is pre-allocated as one of `|Q_lr| × |V|` possible pairs.

### RnglrGssEdge (struct)

```fsharp
[<Struct>]
type RnglrGssEdge<'t, 'nt> = { EdgeSymbol: Symbol<'t, 'nt> }
```

An edge in the GSS, labeled with the grammar symbol that was recognized at this step. Multiple edges between the same pair of vertices are possible, so the adjacency matrix stores `Option<NonEmptySet<RnglrGssEdge>>`.

### RnglrGSS

```fsharp
type RnglrGSS<'t, 'nt> =
    { GssGraph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      StoredStates: Set<Nonterminal<'nt> * int * int * int> array }
```

The RNGLR Graph-Structured Stack — a labeled directed graph encoding all parsing paths. Vertices are `(lrState, inputVertex)` pairs, pre-allocated as `|Q_lr| * |V|`.

`StoredStates[i]` holds cached intermediate automaton intersection states: `Set<Nonterminal * invState * rangeEndState * rangeEndVertex>`. Each tuple records that at GSS vertex `i`, the parser is inside the inverted DFA of block `Nonterminal` at state `invState`, and this traversal originated from the block's final state `rangeEndState` at graph vertex `rangeEndVertex`. When a shift creates a new edge **from** GSS vertex `i`, its stored states are consumed and each tuple is continued via product BFS through the inverted RSM block. The `rangeEndState`/`rangeEndVertex` pair is propagated so PIntermediate entries can be placed at the correct granularity during the BFS.

### RnglrGSS Module

```fsharp
module RnglrGSS =
    val linearIndex: vertexCount: int -> lrState: int -> inputVertex: int -> int
    val init: lrStateCount: int -> vertexCount: int -> RnglrGSS<'t, 'nt>
    val addEdge: gss: RnglrGSS<'t, 'nt> -> fromIdx: int -> toIdx: int -> label: Symbol<'t, 'nt>
                 -> Set<Nonterminal<'nt> * int * int * int>
    val getStoredStates: gss: RnglrGSS<'t, 'nt> -> gssIdx: int -> Set<Nonterminal<'nt> * int * int * int>
    val setStoredStates: gss: RnglrGSS<'t, 'nt> -> gssIdx: int
                         -> states: Set<Nonterminal<'nt> * int * int * int> -> unit
    val outgoingEdges: gss: RnglrGSS<'t, 'nt> -> gssIdx: int -> (int * Symbol<'t, 'nt>) list
```

| Function | Description |
|----------|-------------|
| `linearIndex vertexCount lrState inputVertex` | Maps `(lrState, inputVertex)` to a linear GSS index: `lrState * vertexCount + inputVertex` |
| `init lrStateCount vertexCount` | Pre-allocates the GSS with all `\|Q_lr\| * \|V\|` vertices and an empty edge matrix |
| `addEdge gss fromIdx toIdx label` | Adds an edge `fromIdx → toIdx` labeled by `label`. Returns and clears `StoredStates[fromIdx]` |
| `getStoredStates gss gssIdx` | Reads `StoredStates[gssIdx]` without clearing. Used during product BFS to check existing states |
| `setStoredStates gss gssIdx states` | Writes `storedStates` for a GSS vertex. Used during product BFS to cache intersection states |
| `outgoingEdges gss gssIdx` | Enumerates all `(targetIdx, symbol)` pairs for edges departing from `gssIdx` |

## Functions

### Rnglr.buildPathIndex

```fsharp
val buildPathIndex:
    freshStart: Nonterminal<'nt> -> rsm: RSM<'t, 'nt> -> inputGraph: Graph<int, Option<'t>>
    -> PathIndex<'t, 'nt>
```

The core RNGLR algorithm:

1. **Initialization**: Extends the RSM with a fresh start nonterminal `S'`, builds the LR(0) parsing table, pre-allocates the path index matrix (size `K_rsm × K_rsm` where `K_rsm = rsmStateCount * vertexCount`) and the GSS, and builds inverted RSM block data (reverse transition maps for backwards traversal).

2. **Main loop** — vertex-by-vertex, layered shift-then-reduce with fixpoint:
   - **Shift phase**: For each terminal edge `v --t--> vNext` in the input graph, if the current LR state has a Shift action on `t`, create a GSS edge `(targetLrState, vNext) --t--> (lrState, v)`. Consume stored states from the source GSS vertex and continue each stored state through product BFS. Enqueue the target `(lrState, nextVertex)` for reduction processing.
   - **Reduction phase** (fixpoint at each vertex): For pending `(lrState, vertex)` pairs, call `processNode` which:
     - Finds all items in the current LR state whose RsmState is a final state of its block → these generate reduce candidates.
     - Calls `findPredecessors` for each reduce candidate: runs **product BFS** from the current GSS vertex through the inverted RSM block, starting from each final state and traversing backwards following GSS outgoing edges.
     - For each predecessor found, calls `processReduction` to apply the Goto action and create a GSS edge from the goto target to the predecessor.
     - `processReduction` recursively cascades: each new GSS vertex may trigger further reductions.

   The fixpoint at each vertex processes all `(lrState, vertex)` pairs, including newly created ones from cascading reductions.

3. **Product BFS** (`productBfs`): Given an inverted RSM block and starting `(gssIdx, invState, endState, endVertex)` tuples, follows GSS edges **backwards** (using inverse RSM transitions) to find all predecessors. At each forward step (from `currInv` along `(nextInv, rSym)`), adds:
   - **PTerminal** for `RTerm` transitions: `(globalNextInv, vNext) → (globalCurrInv, vCurr)`
   - **PNonterminal** for `RNonterm` transitions: `(globalNextInv, vNext) → (globalCurrInv, vCurr)`
   - **PIntermediate** when `(globalCurrInv, vCurr) ≠ (globalEnd, endVertex)`: bridges the split between `(globalNextInv, vNext) → (globalCurrInv, vCurr)` and the overall range `(globalNextInv, vNext) → (globalEnd, endVertex)`

   When the product BFS reaches the block's start state, the predecessor (GSS vertex coordinates + nextInv state) is collected and returned.

   Stored states are updated at intermediate GSS vertices (those not in the original start set) to enable the passing mechanism: when a shift later creates an edge from that vertex, the stored state tuple `(nonterminal, invState, endState, endVertex)` is consumed and the product BFS continues.

### Rnglr.isAccepted

```fsharp
val isAccepted: pathIndex: PathIndex<'t, 'nt> -> extRsm: RSM<'t, 'nt> -> vertexCount: int -> bool
```

Checks whether the input graph is accepted. Inspects the path index cell `(startGlobalState, 0) → (finalGlobalState, vertexCount - 1)` where `S'`'s block is a two-state wrapper `0 --RNonterm(S)--> 1`. If this cell is non-empty, the graph contains a path in the language.

### processReduction (private)

```fsharp
val processReduction:
    reduceNt: Nonterminal<'nt> -> finalRsmState: int ->
    lrStatePre: int -> gssIdxPre: int -> vPre: int -> vEnd: int -> depth: int -> unit
```

Handles a single reduction at a predecessor found by `productBfs`:

1. Looks up the LR Goto table for `(lrStatePre, reduceNt)` → `gotoTarget`.
2. Creates a GSS edge `(gotoTarget, vEnd) --N(reduceNt)--> gssIdxPre`, deduplicating by `(reduceNt, gssIdxPre)` per target GSS vertex.
3. **PEpsilonNonterminal**: If the predecessor vertex equals the end vertex (`vPre = vEnd`) **and** the final RSM state equals the block's global start state, adds `PEpsilonNonterminal(reduceNt)` at `(globalStart, vPre) → (finalRsmState, vEnd)`. This handles epsilon derivations (start = final state in the block).
4. Does **not** add `PNonterminal` here — that is handled later when the `S'` block's final state triggers a reduce and `productBfs` traverses the inverted `S'` block, adding `PNonterminal(S)` to the root range.
5. Cascades recursively: calls `processNode` on the new `(gotoTarget, vEnd)` GSS vertex to process any further reductions.

### findPredecessors (private)

```fsharp
val findPredecessors: gssIdx: int -> nt: Nonterminal<'nt> -> (int * int * int * int) list
```

Finds all GSS predecessors for a reduction of nonterminal `nt` at GSS vertex `gssIdx`. Runs `productBfs` through the inverted RSM block of `nt`, starting from each final state at the current GSS vertex. Returns `(gssIdxPre, lrStatePre, vPre, globalInvNext)` tuples.

Special case: if the block's start state is also a final state (epsilon-capable block) and no predecessors were found by BFS, returns a self-loop predecessor so that `processReduction` can record `PEpsilonNonterminal`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| S' reduces via Goto, not freshStart exclusion | `productBfs` traverses the inverted S' block to add `PNonterminal(S)` at the root range. The original approach of checking `reduceNt <> freshStart` in `processReduction` was removed. |
| `productBfs` adds PNonterminal for RNonterm transitions | Unlike GLL where PNonterminal is added at call/return, RNGLR discovers nonterminal relationships during backwards GSS traversal — every inverse RNonterm step corresponds to a recognized nonterminal. |
| PEpsilonNonterminal only when vPre = vEnd && finalRsmState = globalStart | Epsilon derivations happen when the start state of a block is also a final state. The condition `vPre = vEnd` means no input was consumed. `finalRsmState = globalStart` confirms the reduction path is the zero-length one. |
| PNonterminal only from productBfs, not processReduction | In `processReduction`, the Goto transition creates a GSS edge. The PNonterminal entry for the reduced nonterminal is discovered later when the product BFS passes through an inverse RNonterm transition of the caller block. This separates the structural traversal from the path index recording. |
| Stored states in mutable array outside Graph type | Same reason as GLL: `Graph.vertexMap` returns value-copy structs, preventing in-place mutation. Stored states are in a separate `Set<...>[]` array, indexed by linear GSS index. |
| Vertices pre-allocated as `\|Q_lr\| * \|V\|` | All possible GSS vertices exist from initialization; adjacency is determined by edge placement. This avoids dynamic vertex creation during parsing. |
| Deduplication of reduction cascades via processedGotos | The array `processedGotos[gssIdx] : Set<Nonterminal * gssIdxPre>` prevents reprocessing the same `(reduceNt, predecessor)` pair at the same GSS vertex, avoiding infinite cascades. |
| Layered shift-then-reduce with per-vertex fixpoint | The algorithm processes all pending `(lrState, vertex)` pairs at vertex `v` (including those created by cascading reductions) before advancing to vertex `v + 1`. This ensures completeness at each position. |

## Cross-References

- **SPPF**: RNGLR builds the path index during execution; SPPF construction from the index is a separate step. See the SPPF documentation (`sppf.md`) and the GLL `buildSppfFromIndex` for the analogous top-down construction pattern.
- **PathIndex**: The path index matrix is shared between GLL and RNGLR. See `path-index.md` for the type definitions (`PathIndex`, `PathIndexEntry`, `RangeKey`) and utility functions.
- **LR Parsing**: The LR(0) automaton for RNGLR is built by `RnglrLR.buildLR0Table` which adapts standard LR closure and goto operations to RSM items (`RnglrItem`). See `lr-parser.md` for the standard LR infrastructure (`LRAction`, automaton construction patterns).
- **GLL**: RNGLR is the LR-based counterpart to GLL. Both build path indices and share the same `PathIndexEntry` type. GLL uses a descriptor-based worklist with pop/return semantics; RNGLR uses backwards GSS traversal with product BFS. See `gll.md`.
- **RSM**: The RSM model including the extended RSM (`S'` wrapper) is used by both parsers. See `rsm.md`.

## Book References

- Section sec:CFPQ_RNGLR — RNGLR for CFPQ over RSMs
- Chapter 6, `03_RecursiveAutomata.tex` — RSM definition
- Section sec:CFPQ_GLL — GLL parsing (counterpart algorithm, shared path index type)
- `RnglrLR.fs` — LR(0) table construction for RSM items
