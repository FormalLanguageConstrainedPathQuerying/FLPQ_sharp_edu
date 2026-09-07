# Task 260: Canonical level-based RNGLR + reworked step visualization

## Task description (verbatim)

Make RNGLR more canonical: implement canonical level-based logic. Without additional
descriptors. Each step is a level (input position) handling: shift and then do all possible
reductions (continue reductions while possible). Then move to next level. Rework
visualization. Create new template for step layout: two columns only (no descriptor queue),
no new descriptors in last column.

## Design decisions

### D1. Level-based driver (replaces descriptor queues + recursive cascade)

Current structure (tasks 201/225): per-vertex `Queue<RnglrDescriptor>` + recursive
`processNode` ↔ `processReduction` cascade with a depth guard (1000). Each GSS vertex is
"processed" (shift + reduce) when dequeued or reached recursively.

New structure — strict left-to-right level loop, no descriptors, no recursion:

```
for v in 0 .. vertexCount - 1:
    levelReductions <- Set.empty
    shifted = HashSet<int>()            // LR states already shifted at this level
    again = true
    while again:                        // rounds until the level stabilizes
        again <- false
        // Phase 1 — SHIFT: shift every not-yet-shifted GSS vertex at position v
        for lrState in RnglrGSS.verticesAt gss v:      // snapshot (List)
            if shifted.Add(lrState):
                shiftNode lrState v
        // Phase 2 — REDUCE to fixpoint; report whether a new GSS vertex appeared at v
        again <- reduceAtLevel v
```

`reduceAtLevel v`:
```
newVertexAtV = false
changed = true
while changed:                          // full-rescan fixpoint
    changed = false
    for lrState in RnglrGSS.verticesAt gss v:         // fresh snapshot each pass
        for (reduceNt, finalRsmState) in getReduceNtWithStates lrState:
            stepReduceNt / levelReductions updated (same semantics as today)
            preds = findPredecessors (getOrCreateVertex gss lrState v) reduceNt
            for pred in preds:
                (newEdge, newVertex) = processReduction ... vEnd = v
                if newEdge: changed <- true
                if newVertex.IsSome: newVertexAtV <- true
return newVertexAtV
```

`shiftNode lrState v`: body of the current `processNode` shift phase (terminal shifts,
`addEdge`, storedStates consumption, passing continuations via `productBfs` →
`processReduction` with `vEnd = vNext`). No descriptor enqueueing.

### D2. Why rounds are required (correctness argument)

A reduction creates a goto vertex `(gotoTarget, v)` at the *same* position. That goto state
must be able to shift the next input symbol — e.g. grammar `S -> A b`, `A -> a`, input `ab`:
after reducing A at position 1, the goto state must shift `b`. A single "shift once, then
reduce" pass would never shift the goto vertex. The round loop guarantees every vertex that
appears at level v is eventually shifted (Phase 1 of a later round) and reduced (Phase 2).

### D3. Why full-rescan reduce fixpoint (completeness argument)

`findPredecessors` (product BFS) depends on the current GSS, which grows during the level.
A vertex whose reduction edge appears late could open new predecessor paths for *other*
vertices' reductions. Re-running every reducible vertex's BFS each pass until no new GSS
edge is added makes the level processing complete independent of edge-addition order. The
`storedStates` passing mechanism is kept (it accelerates continuations and is the event
task 261 will visualize); with full rescan it is a redundant safety net, not a correctness
dependency.

### D4. Termination

- Inner fixpoint: each pass either adds a new `(gotoVertex, (reduceNt, predGssIdx))` key to
  `processedGotos` (finite set) or stops.
- Outer rounds: a round repeats only when a *new* GSS vertex appears at v; at most
  |Q_lr| distinct LR states can appear at one position.
- No recursion → the depth guard (1000) is removed entirely.

### D5. Passing continuations across levels

A shift at level v consumes storedStates at the new vertex `(target, v+1)`; the continuation
`processReduction` calls create goto vertices at position `v+1`. They are picked up
automatically when level `v+1` starts (they appear in `verticesAt gss (v+1)`). No cross-level
recursion or signaling needed.

### D6. Behavior preservation requirement

The final `PathIndex` must be byte-identical to the current implementation for all inputs —
verified by the full test suite (acceptance facts, CYK/GLL equivalence properties, tree
yield checks, SCC-count invariants in `CrossParserEquivalenceTests`). If any test fails, the
driver is wrong — fix the driver, do not weaken tests.

### D7. Type changes

- Remove `RnglrDescriptor` (RnglrTypes.fs).
- Slim `RnglrParsingStep`: remove `PendingQueues`, `CurrentLrState`, `CurrentDescriptor`,
  `HandledDescriptors`, `NewDescriptors`, `AttemptedDescriptors`. Keep: `ActiveGssVertices`,
  `ActiveGssEdges`, `ActiveGssEdgeSymbols`, `NewGssVertices`, `NewGssEdges`,
  `PathIndexMatrix`, `ChangedCells`, `InputVertex`, `ActiveShiftTerminals`,
  `ActiveReduceNonterminals`, `LevelReductions`.
- Add `RnglrGSS.verticesAt (gss) (v) : int list` — snapshot of LR states having a GSS vertex
  at input position v (List, safe against concurrent VertexInfo growth).

### D8. Visualization changes

- `RnglrVisualizationStep`: remove `DescriptorsTable`, `NewDescriptors`. Keep
  `GssDot`, `GssTikz`, `PathIndex`, `Input`, `InputTikz`, `LrTable`.
- Remove `rnglrDescriptorToTeX`, `descriptorsTableToTeX`, `newDescriptorsToTeX` from
  `RnglrStepVisualizer.fs`.
- No "current GSS node" highlight anymore (there is no current descriptor): pass `None` to
  the `currentVertex` parameter of `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`.
- LR table: `tableToTeXWithHighlightsTabularOnly` called with `currentLrState = None`
  (row highlight no longer applies); green (active actions) and red (level reductions) cell
  highlights are kept.
- New two-column step template (both DOT and TikZ variants): left column — GSS figure + LR
  table; right column — input figure + path index. No descriptor table, no new descriptors.
- `Helpers.writeRnglrStepsVisualization`: per-step files become `gss.dot|tikz`,
  `input.dot|tikz`, `path_index.tex`, `lr_table.tex` (drop `descriptors_table.tex`,
  `new_descriptors.tex`).
- `SummaryTeX.rnglrStepSection`: fill only the remaining placeholders.
- `rnglrColorLegend`: drop rows "Current descriptor in descriptors table", "Current GSS
  node", "Genuinely new descriptors", "Already-handled descriptors attempted again". Keep:
  yellow (modified path index cells), green!30 (current input position), yellow!30 (newly
  added GSS vertices), red edge (newly added GSS edges).

### D9. Step semantics

Step 0 = initial state (empty GSS, empty path index, `InputVertex = -1`) — unchanged.
Steps 1..N = one per input position v (after the level fully stabilizes). Per-step
`ActiveShiftTerminals` / `ActiveReduceNonterminals` accumulate over the whole level
(including all rounds); `LevelReductions` accumulates over the level and resets per level.

## Reuse analysis

- Reused unchanged: `RnglrGSS` module (create/getOrCreateVertex/getVertexInfo/addEdge/
  getStoredStates/setStoredStates/outgoingEdges), `invertRsmTransitions`, `InvBlockData`,
  `productBfs`, `findPredecessors`, `getReduceNtWithStates`, `toRsmSym`,
  `PathIndex.addWithTracking`, `GraphHelpers.collectActiveGssForDict`,
  `collectEdgeSymbols`, `RnglrLR.buildLR0Table`.
- Reused in visualization: `GssDot.toDotFromSets`, `GssTikz.toTikzFromSets`,
  `PathIndexTeX.toTeXWithHighlights`, `InputGraphDot.toDot`, `InputGraphTikz.toTikz`,
  `RnglrTableTeX.tableToTeXWithHighlightsTabularOnly`.
- New: level-based driver in `Rnglr.fs` (replaces queue + recursion),
  `RnglrGSS.verticesAt`, slimmed `RnglrParsingStep`, two-column templates, updated legend.
- Removed: `RnglrDescriptor`, descriptor-table/new-descriptors rendering, depth guard.

## Risks

- R1: Processing-order change could alter storedStates consumption timing → different path
  index. Mitigation: D6 — full test suite (incl. SCC invariants) verifies byte-identical
  results; fix the driver if anything fails.
- R2: Full-rescan fixpoint may be slower than the current cascade. Mitigation: test sizes are
  small; optimize only if tests time out.

## Subtasks

- [x] S1: Rework RNGLR core to canonical level-based driver — committed `9dbe922`
- [x] S2: Remove RnglrDescriptor; rework step visualization and templates — committed `6eb0edc`
- [x] S3: Update tests and golden data — committed `3d7bba1`
- [x] S4: Update developer documentation — this commit

---

### S1: Rework RNGLR core to canonical level-based driver

**Code:** `src/FLPQ.Languages/Rnglr.fs` — replace `pending` queue + recursive
`processNode`/`processReduction` with the level loop of D1 (extract `shiftNode`,
`reduceAtLevel`; `processReduction` returns `(newEdge: bool, newVertexGotoTarget: int option)`
instead of recursing; remove depth guard). Add `RnglrGSS.verticesAt` in
`src/FLPQ.Languages/RnglrTypes.fs`. Keep the `RnglrParsingStep` type and `onStep` signature
unchanged for now — descriptor fields are filled with empty values (`PendingQueues` = array of
empty lists, `CurrentDescriptor`/`CurrentLrState` = None, descriptor sets = empty).
**Tests:** No test changes. Full existing suite must pass unchanged (behavior preservation,
D6): RnglrTests, GllTests, CrossParserEquivalenceTests (SCC invariants), Printers tests
(goldens identical — step 0 goldens are initial-state and unaffected).
**Docs:** None (docs updated in S4).

**Spec:**
- `for v in 0 .. vertexCount - 1`: reset `levelReductions`; `shifted = HashSet<int>()`;
  `again = true`; while again: Phase 1 shifts every `lrState` from `verticesAt gss v` not in
  `shifted` (via `shiftNode`); Phase 2 `again <- reduceAtLevel v`.
- `shiftNode lrState v`: identical body to the current shift phase of `processNode`
  (iterate `graphEdges.[v]`, consult `lrTable.Action`, `getOrCreateVertex` for source and
  target, `addEdge`, consume storedStates → `productBfs` continuation → `processReduction`
  with `vEnd = vNext`). Update `stepShiftTerminals`. No queue enqueueing.
- `reduceAtLevel v`: full-rescan fixpoint per D1; update `stepReduceNt` and
  `levelReductions` when a reduce action is considered (same point as today); return whether
  a new GSS vertex appeared at v.
- `processReduction`: same body minus the recursive `processNode` call; record
  `existedBefore = gss.VertexLookup.ContainsKey (gotoTarget, vEnd)` before
  `getOrCreateVertex`; return `(isNew, if isNew && not existedBefore then Some gotoTarget else None)`.
- Initial `onStep` call (step 0) and per-level `onStep` calls keep the current 14-parameter
  signature; descriptor parameters get empty values.
- `buildPathIndex` / `buildPathIndexWithSteps` public signatures unchanged.

### S2: Remove RnglrDescriptor; rework step visualization and templates

**Code:**
- `src/FLPQ.Languages/RnglrTypes.fs`: delete `RnglrDescriptor`; slim `RnglrParsingStep` per D7.
- `src/FLPQ.Languages/Rnglr.fs`: slim the `onStep` callback to 9 parameters (activeVerts,
  activeEdges, edgeSymbols, piMatrix, changedCells, inputVertex, shiftTerminals,
  reduceNonterminals, levelReds); drop `pendingSnapshot`, `handledAccum`, `prevAttempted`.
- `src/FLPQ.Printers/RnglrStepVisualizer.fs`: slim `RnglrVisualizationStep` per D8; delete
  `rnglrDescriptorToTeX`, `descriptorsTableToTeX`, `newDescriptorsToTeX`; pass `None` for
  `currentVertex` in GSS DOT/TikZ; pass `None` for `currentLrState` to the LR table.
- `data/RNGLR_step_template.tex` + `data/RNGLR_step_tikz_template.tex`: new two-column layout
  per D8 (left: GSS + LR table at 0.3\textwidth; right: input + path index).
- `src/FLPQ.Cli/Helpers.fs`: `writeRnglrStepsVisualization` — drop descriptor file writes.
- `src/FLPQ.Printers/SummaryTeX.fs`: `rnglrStepSection` — drop descriptor placeholders;
  `rnglrColorLegend` — drop the four descriptor/current rows per D8.
- Minimal test edits for compilation (the test project references the removed fields, so the
  solution must compile at the end of this subtask):
  - `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs`: remove
    `DescriptorsTables` / `NewDescriptors` from `RnglrVizData` and delete the two descriptor
    golden tests.
  - `tests/FLPQ.Printers.Tests/TexCompilationTests.fs`: stop writing
    `descriptors_table.tex` / `new_descriptors.tex` in the two RNGLR step-writing sites
    (the other two sites are GLL and keep their descriptor files).
  - `tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs`: step-file expectation becomes
    `[ "gss.dot"; "path_index.tex"; "input.dot"; "lr_table.tex" ]`.
**Tests:** Full suite green; remaining RNGLR goldens unaffected (GSS DOT output is
byte-identical — the removed `currentVertex` highlight was already `None`-equivalent for the
step-0 goldens, and non-step-0 GSS goldens do not exist).
**Docs:** None (S4).

**Spec:**
- Template placeholders: `__STEP_GSS_PDF__`/`__STEP_GSS_TIKZ__`, `__LR_TABLE__`,
  `__STEP_INPUT_PDF__`/`__STEP_INPUT_TIKZ__`, `__PATH_INDEX__`. Column widths 0.5 / 0.48
  with `~` separator, mirroring the existing three-column template style.
- `RnglrVisualizationStep = { GssDot; GssTikz; PathIndex; Input; InputTikz; LrTable }`.

### S3: Update tests and golden data

**Code:** None (tests + data only).
**Tests:** (descriptor-field test edits and runner expectation already landed in S2 for
compilability/green gate)
- Golden data: delete `rnglr_descriptors_table_step0.tex`,
  `rnglr_new_descriptors_step0.tex`.
- Verify the merged RNGLR summary compiles end-to-end with the new two-column template
  (lualatex) via the existing TexCompilationTests.
**Docs:** None.

**Spec:**
- Zero skipped tests; all RNGLR-related tests green; summary compilation test proves the new
  template compiles end-to-end.

### S4: Update developer documentation

**Code:** None.
**Tests:** None (docs-only subtask — code gates skipped per AGENTS.md).
**Docs:** `docs/developer/rnglr.md`:
- Abstract: replace "per-vertex RnglrDescriptor worklist queues with a recursive
  shift-then-reduce cascade" with the level-based description.
- Algorithm section: describe the level loop (Phase 1 shift / Phase 2 full-rescan reduce
  fixpoint / rounds until stable), passing continuations across levels, acceptance check.
- Type definitions: remove `RnglrDescriptor` section; document `RnglrParsingStep` (level-step
  snapshot fields).
- GSS module functions table: add `verticesAt`.
- Design decisions: replace rows about per-vertex queues, recursive cascade, RnglrDescriptor,
  depth guard with: level-based driver without descriptors; round loop until level stabilizes
  (goto vertices must be shifted); full-rescan reduce fixpoint (completeness under growing
  GSS); no recursion (depth guard removed).

**Spec:**
- Doc must match the implemented structure exactly (D1–D5); book reference
  sec:CFPQ_GLR kept (the level-based string specialization of the book's per-vertex
  MakeReductions/Push/ApplyPassingReductions scheme).
