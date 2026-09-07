# RNGLR for RSM

**Tags:** algorithm, parsing, rnglr, cfpq, lr, rsm, path-index, gss, sppf, product-construction, shift-reduce
**Kind:** algorithm
**Module:** Rnglr
**Source:** `src/FLPQ.Languages/Rnglr.fs`
**Depends on:** Matrix, Graph, RSM, PathIndex, Automaton, RnglrTypes, RnglrLR
**Used by:** FLPQ.Cli, TestHelpers
**Book reference:** Section sec:CFPQ_RNGLR (Chapter 6)

> **Abstract:** Implements Right-Nulled Generalized LR (RNGLR) parsing for Recursive State Machines — the LR-based counterpart to GLL. RNGLR builds a **path index** during execution with a canonical level-based driver: each step processes one input position (level) — first shift, then all possible reductions to fixpoint — and only then moves to the next level. No descriptor worklists. Reductions are processed by traversing the GSS backwards through inverted RSM block DFAs (product construction). The SPPF is built separately from the index. The LR(0) automaton is adapted for RSM items.

## Contents

- [Algorithm](#algorithm)
- [Type Definitions](#type-definitions)
- [GSS Module Functions](#gss-module-functions)
- [Core Function Signatures](#core-function-signatures)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Algorithm

The core algorithm is a strict left-to-right **level loop** over input positions. Each level (input position `v`) is processed in rounds until it stabilizes: **shift** every not-yet-shifted GSS vertex at `v`, then run reductions to fixpoint. Only after the level produces no new GSS vertex does the driver move to `v + 1`. There are no descriptor worklists and no recursion — this is the book's per-vertex MakeReductions/Push/ApplyPassingReductions scheme (sec:CFPQ_GLR) specialized to a single input string.

### buildPathIndex

1. **Initialization**: Extend RSM with fresh start `S'`, build LR(0) parsing table, pre-allocate path index matrix (size K_rsm × K_rsm), create empty GSS, build inverted RSM block data (reverse transition maps). Emit step 0 (initial state: empty GSS, empty path index, `InputVertex = -1`).

2. **Level loop** — for each input position `v` from 0 to vertexCount − 1:
   - **Phase 1 — Shift**: For every LR state with a GSS vertex at `v` that has not been shifted yet this level, follow every terminal edge `v --t--> vNext`: if the LR state has a Shift action on `t`, create the GSS edge and consume stored states for product BFS continuation (passing-reduction continuations across levels).
   - **Phase 2 — Reduce to fixpoint** (`reduceAtLevel`): full-rescan loop over all GSS vertices at `v` — for each reducible item, `findPredecessors` (product BFS through inverted RSM block) → apply Goto → create GSS edge. Repeats until no new GSS edge is added, so every reduction's product BFS runs over the current (growing) GSS.
   - **Rounds**: Phase 1 + Phase 2 repeat while a new GSS vertex appeared at `v` during the reduce phase — a goto target must itself be shifted before the level can stabilize.
   - After stabilization, emit the level's step snapshot (active GSS elements, path index copy, shift/reduce activity accumulated over the whole level).

3. **Product BFS**: Following GSS edges backwards through inverted RSM transitions, adding PTerminal, PNonterminal, and PIntermediate entries to the path index. Reaches block start states to find predecessors.

4. **Acceptance**: Check path index cell `(startGlobalState, 0) → (finalGlobalState, vertexCount - 1)` via `PathIndex.isAccepted`.

### processReduction

Non-recursive; applies a single reduction for one predecessor found by product BFS:
1. Look up LR Goto table for `(lrStatePre, reduceNt)` → gotoTarget.
2. Create GSS edge `(gotoTarget, vEnd) --N(reduceNt)--> gssIdxPre` (deduplicated via `processedGotos`).
3. PEpsilonNonterminal only when `vPre = vEnd` and `finalRsmState = globalStart` (true epsilon).
4. Returns `(newEdge, newVertexGotoTarget)` so the level loop knows whether another reduce pass or another round is needed.

## Type Definitions

### RnglrItem (struct)
```fsharp
[<Struct>]
type RnglrItem<'nt> = { BlockNonterminal: Nonterminal<'nt>; RsmState: int }
```
An LR item over an RSM: a position in a specific RSM block's DFA. Unlike grammar-based LR items (production + dot position), RSM items track which block nonterminal and which state within that block.

### RnglrParsingStep
```fsharp
type RnglrParsingStep<'t, 'nt> =
    { ActiveGssVertices: Set<int>
      ActiveGssEdges: Set<int * int>
      ActiveGssEdgeSymbols: Map<int * int, NonEmptySet<Symbol<'t, 'nt>>>
      NewGssVertices: Set<int>
      NewGssEdges: Set<int * int>
      PathIndexMatrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      ChangedCells: Set<int * int>
      InputVertex: int
      ActiveShiftTerminals: Set<Terminal<'t>>
      ActiveReduceNonterminals: Set<Nonterminal<'nt>>
      LevelReductions: Set<Nonterminal<'nt>> }
```
A level-step snapshot for visualization. One step per input position, captured after the level fully stabilizes (plus step 0 for the initial state). `ActiveShiftTerminals` / `ActiveReduceNonterminals` accumulate over the whole level including all rounds; `LevelReductions` accumulates over the level and resets per level. `NewGssVertices` / `NewGssEdges` are the difference against the previous step's active sets.

### RnglrTable
```fsharp
type RnglrTable<'t, 'nt> =
    { Action: Map<int * Symbol<'t, 'nt>, LRAction<Nonterminal<'nt>>>
      Goto: Map<int * Nonterminal<'nt>, int>
      Automaton: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>> }
```
LR(0) parsing table built from the extended RSM's automaton.

### RnglrGssEdge (struct)
```fsharp
[<Struct>]
type RnglrGssEdge<'t, 'nt> = { EdgeSymbol: Symbol<'t, 'nt> }
```
GSS edge labeled with the grammar symbol recognized at this step. Multiple edges between same pair possible → `NonEmptySet`.

### RnglrGSS
```fsharp
type RnglrGSS<'t, 'nt> =
    { VertexLookup: Dictionary<int * int, int>
      VertexInfo: ResizeArray<int * int>
      Edges: Dictionary<int, Dictionary<int, NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      StoredStates: Dictionary<int, Set<Nonterminal<'nt> * int * int * int>> }
```
The RNGLR Graph-Structured Stack. Vertices are created lazily on-demand with sequential IDs (0, 1, 2, ...). `VertexLookup` maps (lrState, inputVertex) to GSS index; `VertexInfo` provides reverse lookup. Edges use sparse Dictionary-based adjacency. `StoredStates` caches intermediate automaton intersection states.

## GSS Module Functions

| Function | Description |
|----------|-------------|
| `create()` | Returns an empty GSS with no pre-allocated vertices |
| `getOrCreateVertex gss lrState inputVertex` | Returns existing GSS vertex ID or allocates next sequential ID |
| `getVertexInfo gss gssIdx` | Returns (lrState, inputVertex) for a GSS vertex |
| `verticesAt gss inputVertex` | Snapshot of LR states having a GSS vertex at the input position; a list, safe to iterate while new vertices are created |
| `addEdge gss fromIdx toIdx label` | Adds edge, returns and clears StoredStates[fromIdx] |
| `getStoredStates gss gssIdx` | Reads StoredStates[gssIdx] without clearing |
| `setStoredStates gss gssIdx states` | Writes storedStates for a GSS vertex |
| `outgoingEdges gss gssIdx` | Enumerates all (targetIdx, symbol) pairs departing from gssIdx |

## Core Function Signatures

### Rnglr.buildPathIndex
```fsharp
val buildPathIndex:
    freshStart: Nonterminal<'nt> -> ersm: ExtendedRSM<'t, 'nt> -> inputGraph: Graph<int, Option<'t>>
    -> PathIndex<'t, 'nt>
```
Core RNGLR algorithm — builds the path index through the level-based driver (shift + reduce fixpoint per input position).

### Rnglr.buildPathIndexWithSteps
```fsharp
val buildPathIndexWithSteps:
    freshStart: Nonterminal<'nt> -> ersm: ExtendedRSM<'t, 'nt> -> inputGraph: Graph<int, Option<'t>>
    -> RnglrResult<'t, 'nt>
```
Same algorithm, additionally recording one `RnglrParsingStep` snapshot per level (plus the initial state) for step-by-step visualization.

Acceptance is checked with `PathIndex.isAccepted pathIndex extRsm vertexCount`, which inspects the path index cell `(startGlobalState, 0) → (finalGlobalState, vertexCount - 1)`.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| S' reduces via Goto, not freshStart exclusion | `productBfs` traverses inverted S' block to add PNonterminal(S) at root range |
| `productBfs` adds PNonterminal for RNonterm transitions | Every inverse RNonterm step corresponds to a recognized nonterminal |
| PEpsilonNonterminal only when vPre=vEnd && finalRsmState=globalStart | True epsilon derivations only: start=final state in block and no input consumed |
| PNonterminal only from productBfs, not processReduction | Separates structural traversal from path index recording |
| StoredStates as Dictionary<int, Set<...>> not fixed array | Matches dynamic GSS vertex creation; no need to pre-allocate for non-existent vertices |
| GSS vertices created on-demand with sequential IDs | Decouples GSS indexing from PathIndex grid formula; only allocates for actually used vertices |
| Deduplication of cascades via processedGotos (Dictionary) | Prevents reprocessing same (reduceNt, predecessor) pair at same GSS vertex |
| Level-based driver without descriptors | Canonical form of the book's per-vertex scheme: each step is one input position — shift, then reduce to fixpoint — before moving on. No worklist queues, no descriptor bookkeeping; the GSS itself (via `verticesAt`) is the only pending-work representation. storedStates deposited by reductions at V are consumed by shifts at V or V+1 because all work at V completes before V+1 begins |
| Round loop until the level stabilizes | A reduction's goto target is a new GSS vertex at the same position v that must itself be shifted. Phase 1 (shift unshifted vertices) + Phase 2 (reduce fixpoint) repeat while a new GSS vertex appeared at v; termination follows from the finite LR automaton × finite input positions |
| Full-rescan reduce fixpoint (not incremental queue) | The GSS grows during a level, so every pass re-scans all vertices at v until no new GSS edge is added. Guarantees each reduction's product BFS runs over the current GSS — completeness without tracking which vertices changed |
| No recursion in the driver | `processReduction` returns `(newEdge, newVertexGotoTarget)` and the level loop decides what to do next; the old recursive processNode ↔ processReduction cascade (and its 1000 depth guard) is gone |
| Visualization steps are per-input-position | One step captures the cumulative result of processing one level (all rounds of shift + reduce fixpoint). Step 0 is the initial empty state. Shift/reduce activity sets accumulate over the whole level |

## Book Reference

- Section sec:CFPQ_RNGLR — RNGLR for CFPQ over RSMs
- Chapter 6, `03_RecursiveAutomata.tex` — RSM definition
- Section sec:CFPQ_GLR — GLR-based CFPQ (the per-vertex MakeReductions/Push/ApplyPassingReductions scheme that the level-based driver specializes to a single input string)
- Section sec:CFPQ_GLL — GLL parsing (counterpart algorithm, shared path index type)
- `RnglrLR.fs` — LR(0) table construction for RSM items

## See Also

- [GLL for RSM](gll.md) — LL-based counterpart sharing SPPF and PathIndex
- [SPPF module](sppf.md) — SPPF construction from path index
- [PathIndex module](path-index.md) — path index types and operations
- [RSM module](rsm.md) — Recursive State Machine model
- [LR parser](lr-parser.md) — standard LR infrastructure (LRAction, automaton construction)
