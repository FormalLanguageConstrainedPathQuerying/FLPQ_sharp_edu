# Detailed Plan — Task 278

## Task description (verbatim)

278. Improve RNGLR rendering. For each big step (reductions to fixpoint + one shift) visualize all substeps separately. Substep is an exactly one action: exactly one reduce or one shift. Improve collected data structure with respect to new visualization requirements. Teplate for visualization is exactly the same as existing templste for step. One change: for LR table highlight exactly one cell: cell with action that visualized on this step.
     **[USER GUIDANCE]**: A reduce substep highlights two cells — the Goto cell (predecessor state, nonterminal) consulted for that edge and the Action cell (triggering state, $) containing the reduce action; still one reduction per substep (one new GSS edge). Output stays flat: each substep is its own step_N directory and summary section (initial state remains step 0); levels with no actions produce nothing.

## Design decisions (confirmed with the user)

- **Substep granularity**: one substep = exactly one new GSS edge — either one
  reduction edge (one `processReduction` call that passes the dedup check) or one
  shift edge (one `shiftNode` edge creation). The big step (level: reduce fixpoint +
  one shift pass) is not rendered as a separate cumulative figure; its substeps are
  emitted in execution order, so the last substep of a level shows the level's final state.
- **LR table highlight** (the only template change):
  - Shift substep: exactly one cell — Action cell `(lrState, terminal)`, `\cellcolor{green!20}`.
  - Reduce substep: exactly two cells — Goto cell `(gotoLrState, nonterminal)` (the lookup
    consulted for this edge) and Action cell `(triggerLrState, $)` (contains `r_A`), both
    `\cellcolor{red!20}`.
  - Initial step (`Action = None`): no highlight (plain table).
- **Output structure**: flat — each substep is its own `step_N/` directory and its own
  summary section via the unchanged `RNGLR_step_template.tex`; initial state stays `step_0`.
  Levels with no actions produce nothing. `Helpers.writeRnglrStepsVisualization`,
  `SummaryTeX.rnglrStepSection`, and `RnglrRunner` need no changes.
- **Trigger LR state**: a reduce substep must know the LR state that holds the complete
  item (the row of the `r_A` action cell). In the fixpoint loop it is the scanned `lrState`;
  in passing-reduction cascades (shiftNode) it is the state of the vertex where the stored
  reduction was originally triggered — not recoverable from the current stored-state tuple,
  so the tuple gains a fifth component `triggerLrState`.
- **Emission rule**: emit a substep exactly when a new GSS edge is added. A deduplicated
  reduction that only adds a fresh PEpsilonNonterminal path-index entry (possible for blocks
  with start ∈ finals and ≥ 2 finals) does not emit; those cells surface in the next
  substep's `ChangedCells` (or are absent from step figures if no later substep exists —
  the final `path_index.tex` file always shows them). Keeps the invariant "substep ⇒ one
  new GSS edge" exact.
- **Algorithm behavior is unchanged**: same driver, same dedup, same fixpoint condition
  (`isNew` only); visualization data collection must not alter the path index or GSS.

## Reuse analysis (reusing skill checklist)

- **Q1/Q3 reuse**: `RnglrStepVisualizer.renderStep/renderSteps`,
  `Helpers.writeRnglrStepsVisualization`, `SummaryTeX.rnglrStepSection`,
  `GssDot`/`GssTikz`/`PathIndexTeX`/`InputGraph*` renderers, and the step template are all
  reused unchanged; only the LR-table rendering inside `renderStep` changes.
- **Q4 generalize**: `RnglrTableTeX.buildTabular` is generalized into a private
  `buildTabularCore` taking two highlight sets (action cells, goto cells); the plain
  `buildTabular` delegates with empty sets — one tabular-building loop instead of two.
- **Q5 extract**: the per-step snapshot logic (active GSS collection, matrix copy, changed
  cells, passing vertices, New\* differencing) moves from `buildPathIndexWithSteps.onStep`
  into a local `emit` in `buildPathIndexCore`, so both `buildPathIndex` (no-op callback)
  and `buildPathIndexWithSteps` share it.
- **Removed (superseded)**: `RnglrTableTeX.tableToTeXWithHighlights`,
  `tableToTeXWithHighlightsTabularOnly`, `buildTabularWithHighlights` — their multi-cell
  level-based highlighting is replaced by per-action highlighting; no production callers
  remain after S3. `RnglrParsingStep.ActiveShiftTerminals / ActiveReduceNonterminals / LevelReductions` are replaced by the single `Action` field.

## Subtasks

### S1: Add RnglrAction type and single-action table highlight function [done — 10b6f4f]

**Code:**

- `src/FLPQ.Languages/RnglrTypes.fs`: new `RnglrAction<'t, 'nt>` DU with
  `[<RequireQualifiedAccess>]`:
  `Reduce of nt: Nonterminal<'nt> * triggerLrState: int * gotoLrState: int` and
  `Shift of terminal: Terminal<'t> * lrState: int`. Doc comment: one RNGLR action = one
  substep; Reduce fields = reduced nonterminal, LR state holding the complete item (row of
  the r_A action cell), predecessor LR state whose Goto is applied for this edge. Book
  reference sec:CFPQ_RNGLR.
- `src/FLPQ.Printers/RnglrTableTeX.fs`: refactor `buildTabular` into private
  `buildTabularCore (terminalPrinter) (nonterminalPrinter) (table) (actionCells: Set<int * Symbol<'t, 'nt>>) (gotoCells: Set<int * Nonterminal<'nt>>)` —
  action-part cell gets `\cellcolor{green!20}` when in `actionCells`, goto-part cell gets
  `\cellcolor{red!20}` when in `gotoCells`; `buildTabular` delegates with empty sets. New
  public functions (doc comments state the highlight rule + xcolor requirement):
  - `tableToTeXWithActionHighlight terminals nonterminals table action : string` (center + tabular)
  - `tableToTeXWithActionHighlightTabularOnly ... : string`
    Mapping: `Shift(t, s)` → actionCells = {(s, T t)}; `Reduce(a, trig, pred)` →
    actionCells = {(trig, Epsilon)}, gotoCells = {(pred, a)}. Old multi-cell highlight
    functions stay until S3.

**Tests:** `tests/FLPQ.Printers.Tests/RnglrTableTexTests.fs` — new facts: shift action
highlights exactly one `\cellcolor{green!20}` cell on the action row/column and no red
cells; reduce action highlights exactly two `\cellcolor{red!20}` cells (goto row + trigger
row `$` column) and no green cells; `TabularOnly` has no center wrapper while the wrapped
variant does.

**Docs:** `docs/developer/rnglr.md` — new `RnglrAction` type section (fields + highlight
mapping) + TOC entry (new public API per the documentation mapping table).

**Spec:**

- Highlight sets are matched per rendered cell: action part iterates terminals then `$`
  (Symbol.Epsilon); goto part iterates nonterminals — membership test against the sets.
- Empty highlight sets reproduce today's `buildTabular` byte-for-byte (existing goldens
  for the plain table must stay green).

### S2: Thread the triggering LR state through stored states [done — e6c04bb]

**Code:**

- `src/FLPQ.Languages/RnglrTypes.fs`: `RnglrGSS.StoredStates` value type becomes the
  labeled 5-tuple `Nonterminal<'nt> * int * int * int * int` (existing four components +
  `triggerLrState`); update the type doc comment (component list) and `RnglrGSS.create`.
- `src/FLPQ.Languages/Rnglr.fs`:
  - `PredecessorInfo` gains `TriggerLrState: int`.
  - `productBfs`: dequeued node's `TriggerLrState` is stored in every deposited tuple and
    copied into every produced predecessor.
  - `findPredecessors`: starts and `epsPredecessors` get `TriggerLrState = vxLrState`
    (the LR state of the reduction vertex).
  - `shiftNode` cascade: destructure the 5-tuple; BFS start gets
    `TriggerLrState = storedTriggerLr`.
    No behavior change — the new component is carried but not yet consumed for emission.

**Tests:** existing RNGLR suites pass unchanged (acceptance cases, passing-reduction
tests, SPPF tests) — this is a behavior-preserving refactor; no new assertions possible
yet (the field is not observable in output).

**Docs:** `docs/developer/rnglr.md` — `RnglrGSS` type section: StoredStates 5-tuple with
component list including `triggerLrState` (changed public API per the mapping table).

**Spec:**

- Component order: `(nonterminal, invState, endRsmState, endInputVertex, triggerLrState)`
  — the existing four keep their positions so the change is a pure extension.
- `getStoredStates` / `setStoredStates` signatures follow the new tuple type automatically.

### S3: Per-action substep collection and single-cell rendering [done — b594b88]

**Code:**

- `src/FLPQ.Languages/RnglrTypes.fs`: `RnglrParsingStep` — replace
  `ActiveShiftTerminals`, `ActiveReduceNonterminals`, `LevelReductions` with
  `Action: RnglrAction<'t, 'nt> option` (None = initial step); update doc comments
  (substep semantics: snapshot after exactly one action; New\* differ against the previous
  substep; PassingReductionVertices accumulate over the substep only). Update
  `RnglrResult.Steps` doc comment (one snapshot per action + initial state).
- `src/FLPQ.Languages/Rnglr.fs`:
  - `buildPathIndexCore` callback becomes `onSubstep: RnglrParsingStep<'t, 'nt> -> unit`.
    Local `emit (action: RnglrAction option) (inputVertex: int)` computes the snapshot:
    `collectActiveGss`, `collectEdgeSymbols`, matrix copy, take+reset `changedCells`,
    take+reset `passingReductionVertices`, New\* differencing against previous emit
    (moved here from `buildPathIndexWithSteps`), then calls `onSubstep`. Move
    `collectActiveGss` / `collectEdgeSymbols` above the emission points.
  - `processReduction` gains a `triggerLrState` parameter; when `isNew` it calls
    `emit (Some (RnglrAction.Reduce (reduceNt, triggerLrState, lrStatePre))) vEnd`;
    still returns `isNew` (fixpoint condition unchanged).
  - `shiftNode`: drop `stepShiftTerminals`; after each shift edge creation call
    `emit (Some (RnglrAction.Shift (Terminal tVal, lrState))) v`; the cascade passes
    `storedTriggerLr` to `processReduction`.
  - `reduceAtLevel`: pass `lrState` as `triggerLrState`; drop `stepReduceNt` /
    `levelReductions` accumulators.
  - Driver: initial `emit None (-1)` before vertex 0 creation; remove the per-level emit
    and all level accumulators.
  - `buildPathIndexWithSteps`: collect via `onSubstep` (no differencing logic left).
- `src/FLPQ.Printers/RnglrTableTeX.fs`: delete `buildTabularWithHighlights`,
  `tableToTeXWithHighlights`, `tableToTeXWithHighlightsTabularOnly` (no callers left).
- `src/FLPQ.Printers/RnglrStepVisualizer.fs`: `renderStep` renders the table as
  `match step.Action with None -> tableToTeXTabularOnly | Some a -> tableToTeXWithActionHighlightTabularOnly`; update module/function doc comments
  (substep semantics, highlight rule).

**Tests:**

- `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs`: hand-built step literal in
  `renderStep labels epsilon edges...` updated to the new record shape (give it
  `Action = Some (RnglrAction.Shift (Terminal "a", 0))`); existing assertions unchanged.
- `tests/FLPQ.Printers.Tests/RnglrTableTexTests.fs`: delete facts exercising the removed
  multi-cell API.
- Goldens: step-0 goldens (`rnglr_gss_step0.dot`, `rnglr_path_index_step0.tex`,
  `rnglr_input_step0.dot`, `rnglr_lr_table_step0.tex`) must stay byte-identical (initial
  step is unchanged); regenerate `rnglr_gss_aaa_last.dot` via `CREATE_GOLDEN_FILES=1` and
  inspect the diff — only New-vertex/New-edge highlighting may differ (last substep vs
  last level).

**Docs:** `docs/developer/rnglr.md` — `RnglrParsingStep` section: new record shape
(`Action` field replacing the three activity sets) and substep semantics;
`buildPathIndexWithSteps` description ("one snapshot per action substep, plus the initial
state").

**Spec:**

- Emission order within a level: fixpoint reductions in execution order, then shifts in
  `verticesAt` × outgoing-edge order; cascade reductions interleave right after the shift
  that consumed their stored states.
- `InputVertex` of a substep = the level where its edge appears (`vEnd` for reductions,
  `v` for shifts).
- `buildPathIndex` (no steps) passes a no-op callback — zero overhead path unchanged in
  behavior.

### S4: Substep semantics tests [done — 41740c9]

**Code:** none.

**Tests:**

- `tests/FLPQ.Languages.Tests/RnglrTests.fs`, new module `RnglrSubsteps`:
  - initial step: `Action = None`, `InputVertex = -1`, empty GSS sets, empty ChangedCells;
  - every non-initial substep has `Action = Some` and `InputVertex` in range;
  - per-level ordering: within each level all Reduce substeps precede Shift substeps
    (canonical order — reduce fixpoint before the shift pass);
  - shift substep: exactly one new GSS edge, its symbol set contains the shifted terminal;
  - reduce substep: at most one new GSS edge; when present its symbol is the reduced
    nonterminal and `Map.contains (gotoLrState, nt) lrTable.Goto`;
  - exact action sequence for `S -> a a` on input `[a; a]`:
    `[Shift(a, 0); Shift(a, 1); Reduce(S, 3, 0)]` (LR states per the golden table:
    0 = start, 1 = after first a, 2 = after S, 3 = reduce S);
  - `buildPathIndexWithSteps ... .PathIndex` equals `buildPathIndex ...` for the same
    grammar/input (matrix + counts).
- `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs`: extend `RnglrVizData` with
  the action list; new facts: a shift substep's table contains exactly one
  `\cellcolor{green!20}` on the action row; a reduce substep's table contains exactly two
  `\cellcolor{red!20}` cells (goto row and trigger row `$` column); the initial step's
  table contains no `\cellcolor`; new golden `rnglr_lr_table_substep_shift.tex` for the
  first shift substep of `S -> a a` on `[a; a]`.

**Docs:** none (tests-only subtask — no source changes, so the documentation mapping
table requires no doc action).

**Spec:**

- Row/column localization in TeX: parse tabular rows by their leading `\hline <state>`
  marker; the `$` column is the last action-part column before the goto part.

### S5: Documentation [done — 3c31e61]

**Code:** none (documentation-only subtask — code gates skipped per AGENTS.md).

**Tests:** none.

**Docs:** `docs/developer/rnglr.md` — the remaining semantic updates that belong to the
whole task rather than one subtask:

- Abstract: mention per-action substep visualization.
- Algorithm §2: replace "emit the level's step snapshot" with the emission rule (one
  snapshot per new GSS edge — reduction or shift — plus the initial state; cascade
  reductions emit right after their consuming shift).
- Design decisions: replace "Visualization steps are per-input-position" with the
  per-action substep decision (flat output, template unchanged except single/two-cell
  table highlight); add the trigger-LR-state-in-stored-states decision; add the
  epsilon-only-change attribution edge case.

**Spec:**

- Keep every statement traceable to code identifiers; no new information that is not in
  the code or this plan.

## Verification

- Per subtask: format + build + affected tests (see `quality-gates` skill).
- Pre-merge: full hard gate (`python3 tools/hard_gate.py`) must show STATUS: PASS —
  including line/branch coverage thresholds (new branches in the visualizer/table core
  are covered by S1/S4 tests).
- Regenerate RNGLR output for `data/example_input_a_a_a.txt` +
  `data/example_grammar_a_a_a.bnf` and eyeball: substep count > level count, each
  `lr_table.tex` highlights one (shift) or two (reduce) cells, final GSS/path index
  identical to before.
