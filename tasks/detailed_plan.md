# Detailed Plan — Task 225

Reorganize RNGLR to canonical shift-then-reduce ordering with per-input-position visualization steps.

## Subtasks

### S1: Reorder `processNode` to shift-first-then-reduce and change step capture to per-input-position

**Code:** `src/FLPQ.Languages/Rnglr.fs` — `buildPathIndexCore` and `processNode`
**Tests:** Run existing `RnglrTests.fs` (all must pass — algorithm behavior verified equivalent)
**Docs:** None yet (S4 covers docs)

**Spec:**
- In `processNode` (lines 278-322): swap the order — move the shift block (lines 295-322) before the reduce block (lines 286-293)
  - The shift block creates terminal GSS edges and descriptors at V+1, consuming storedStates on target vertices
  - The reduce block then runs findPredecessors + processReduction (which may cascade via recursive processNode calls — those calls also do shift-first since we swap the order)
  - storedStates set during productBfs in the reduce phase are consumed by shifts of later descriptors at the same vertex or at V+1
- Change step capture from per-descriptor to per-input-position:
  - Remove the `onStep` call from inside the per-descriptor `while` loop (lines 400-414)
  - Add `onStep` call AFTER the `while` loop for each vertex `v` (after `processed` goes out of scope)
  - Step 0 initial step (lines 349-363) stays unchanged
  - For position-level steps: `CurrentDescriptor = None`, `CurrentLrState = None`, `InputVertex = v`
- Reset mutable accumulators `stepShiftTerminals` and `stepReduceNt` PER VERTEX (after the while loop), not per-descriptor
- `levelReductions` and `prevInputVertex` logic already vertex-aware — no change needed
- `AttemptedDescriptors` for the step = union of all descriptors attempted at this vertex
- `NewDescriptors` for the step = attempted descriptors minus handledBefore (handled at previous vertices)
- `HandledDescriptors` = cumulative handledAccum (all descriptors handled so far)
- PendingQueues snapshot taken BEFORE the while loop at V (showing what's queued initially) — or AFTER (showing remaining). Use AFTER — it will be empty unless cascaded descriptors produced during processing.
- Actually better: take pending snapshot AFTER processing completes. For V there should be no remaining descriptors at V (they were all processed). The snapshot shows descriptors queued for future vertices (V+1, V+2, ...).

### S2: Update RNGLR step visualization for position-level steps

**Code:** `src/FLPQ.Printers/RnglrStepVisualizer.fs`
**Tests:** `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs` (update golden data, update label format tests)
**Docs:** None yet (S4 covers docs)

**Spec:**
- `renderStep` already handles `step.CurrentDescriptor |> Option.map (fun d -> d.GssIdx)` → returns `None` for position-level steps — current GSS node highlight becomes no highlight (acceptable)
- `descriptorsTableToTeX` already handles `currentDescriptor: RnglrDescriptor option` with `None` case — no row gets yellow highlight
- `newDescriptorsToTeX` — unchanged, uses set-based data
- LR table: `CurrentLrState = None` → no row gets yellow highlight. `ActiveShiftTerminals` and `ActiveReduceNonterminals` still get green highlights. `LevelReductions` still get red highlights. The table is still informative (shows which actions were taken, which nonterminals reduced at this level).
- Step 0 golden files unchanged (empty initial state, CurrentDescriptor/CurrentLrState already None)
- Regenerate all golden data files after step 0 (if any) — but current tests only check step 0 golden data
- Update the GSS DOT vertex/edge label format test: vertex labels remain `"idx: (lrState,inputVertex)"`, edge labels remain symbol-based. These tests check format patterns, not specific content — should pass unchanged.
- Update `RNGLR GSS DOT` tests in RnglrStepVisualizationTests.fs: the test enumerates all steps and checks vertex/edge label formats, one blue vertex per step, etc. For position-level steps with no CurrentDescriptor, there's no current GSS node → no blue vertex. Update the test accordingly: either skip the "exactly one blue vertex" assertion for position-level steps, or accept 0 blue vertices when CurrentDescriptor = None.

### S3: Update golden data

**Code:** `tests/FLPQ.Printers.Tests/GoldenData/rnglr_*_step0.*` (may need regeneration), `tests/FLPQ.Printers.Tests/GoldenHelpers.fs`
**Tests:** `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs`
**Docs:** None

**Spec:**
- Run tests, let golden verify create new golden files if needed
- Step 0 golden data should be unchanged (no CurrentDescriptor, CurrentLrState already None at step 0)
- Verify all 6 golden tests for step 0 pass
- If any golden data needs regeneration, copy from output to golden data directory
- Update `GoldenHelpers.fs` regex patterns if RNGLR edge label format changed (should not change — symbols are the same)

### S4: Update documentation

**Code:** None (doc only)
**Tests:** Skip (doc-only subtask per skill)
**Docs:** `docs/developer/rnglr.md`

**Spec:**
- Fix algorithm description (lines 25-26): currently says "shift all terminals at a vertex, then a fixpoint loop of reductions" — this is now CORRECT (implementation matches)
- Fix main loop description (lines 31-33): shift phase first, then reduction fixpoint — already describes the new behavior, just verify correctness
- Update design decisions table (line 129): the current entry says "The cascade ensures reduction-then-shift ordering at each level of recursion" — change to "shift-then-reduce ordering at each level of recursion". storedStates from reductions at V are consumed during shifts of subsequent descriptors at V or during V+1's processing.
- Update step visualization section: note that steps are per-input-position, one step captures all shift+reduce work at a vertex
