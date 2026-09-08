# Task 261: RNGLR visualization — highlight GSS vertices where passing reductions handling triggers

## Task description (verbatim)

For RNGLR visualization, for each step track GSS vertices where passing reductions handling
triggers. Highlight these vertices with new color. Add this color to summary legend together
with other colors. Share color with one used for GLL stored pops.

## Context and analysis

### The GLL precedent (task 259)

Task 259 added `GLLParsingStep.StoredPopVertices: Set<int>` — GSS vertices at which
stored-pops handling triggered (a new caller edge arrived at an already-completed callee
vertex, `GSS.addEdge` returned non-empty pops). Rendering reuses the
`storedPopVertices` parameter of `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`, which
fills such vertices **orange** (`fillcolor=orange` in DOT, `fill=orange!30` in TikZ), with
priority current > stored-pop > highlighted > normal. The GLL summary legend has the row
`orange!30 / Stored pops handling triggered at GSS vertex`.

### RNGLR passing-reduction mechanism (current implementation)

- `productBfs` (Rnglr.fs) deposits stored states `(nt, invState, endState, endVertex)` at
  intermediate GSS vertices it traverses (`RnglrGSS.setStoredStates`).
- `RnglrGSS.addEdge` returns and clears `StoredStates[fromIdx]` — the source vertex of the
  new edge. Two call sites:
  1. `shiftNode`: `addEdge(targetGssIdx, shiftGssIdx, t)` — consumed states are processed
     (continuation product BFS + `processReduction`).
  2. `processReduction`: `addEdge(gotoGssIdx, gssIdxPre, N) |> ignore` — consumed states
     are discarded.

Per the book (sec:CFPQ_GLR, AddEdge): "the algorithm stores passing reductions in GSS
vertices and when a new edge is added it continues all reductions that must pass through
this edge" — i.e., handling triggers at the source vertex of a new edge that has stored
passing reductions. Both call sites above are therefore the tracking points.

### Resolved: the mechanism is live (verified empirically in S1)

An initial static analysis suspected both consumption points were dead (it assumed the
product BFS traverses only terminal-labeled GSS edges). That assumption was wrong:
`invertRsmTransitions` matches `AutomatonLabel.ATerm(sym)` for **all** `RsmSymbol`
labels, including `RNonterm` — so the product BFS also traverses reduction edges
(nonterminal-labeled), which connect vertices at the *same* input position. A reduction's
BFS can therefore deposit stored states at a same-position vertex that a later reduction
in the same level consumes.

Empirical verification (all 467 registry grammar × input combinations): passing-reduction
handling triggers for `Dyck1 / ambiguousWithConcat` (`S -> a S b | eps | S S`) on inputs
`a b`, `a b a b`, `a a b b`, etc. — always via the `processReduction` consumption point
(e.g. input `a b`: step 3 consumes state `(S, 2, 3, 2)` at GSS vertex `(lrState=4, v=2)`).
The `shiftNode` consumption point is tracked as well (book-faithful AddEdge trigger) but
did not fire in the experiment.

## Reuse analysis

- Reused as-is: `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` `storedPopVertices`
  parameter (orange fill, priority order) — no changes to these modules.
- Reused pattern: task 259 tracking (`ref Set<int>` in `buildPathIndexCore`, per-step
  capture/reset, onStep parameter), `GllRunnerTests` orange tests, `CliSummaryTests`
  legend test, `gllColorLegend` row format.
- New: `RnglrParsingStep.PassingReductionVertices` field; tracking in `shiftNode` /
  `processReduction`; `rnglrColorLegend` row; RNGLR runner tests.

## Subtasks

### S1: Track passing-reduction vertices in RNGLR (algorithm + step type + visualizer wiring) — [done] commit 9759187

**Code:**
- `src/FLPQ.Languages/RnglrTypes.fs`: add `PassingReductionVertices: Set<int>` field to
  `RnglrParsingStep` with XML doc comment.
- `src/FLPQ.Languages/Rnglr.fs`:
  - `buildPathIndexCore`: new `passingReductionVertices = ref Set.empty<int>`.
    - In `shiftNode`: when `consumedStates` is non-empty, add `targetGssIdx` (the edge
      source whose stored states were consumed) to the set.
    - In `processReduction`: capture the `addEdge` result; when non-empty, add
      `gotoGssIdx` to the set.
  - onStep callback: new trailing parameter `passingReductionVertices: Set<int>`; step 0
    passes `Set.empty`; per-level capture before `onStep`, reset after (same pattern as
    `stepShiftTerminals`).
  - `buildPathIndex`: no-op lambda arity 9 → 10.
  - `buildPathIndexWithSteps`: onStep binds the new parameter; record construction adds
    `PassingReductionVertices`.
- `src/FLPQ.Printers/RnglrStepVisualizer.fs`: `renderStep` passes
  `step.PassingReductionVertices` instead of `Set.empty` to both
  `GssDot.toDotFromSets` and `GssTikz.toTikzFromSets`.

**Tests:** new `RnglrPassingReductions` submodule in
`tests/FLPQ.Languages.Tests/RnglrTests.fs`:
- ``passing-reduction vertices tracked for S -> a S b | eps | S S`` — runs
  `buildPathIndexWithSteps` on Dyck1 `ambiguousWithConcat` with input `a b`; asserts some
  step has non-empty `PassingReductionVertices`.
- ``passing-reduction vertices empty at initial step`` — step 0 is empty.

Additionally, a temporary experiment over all 467 registry grammar × input combinations
verified the trigger set (see "Resolved" section above); it was deleted after use. All
existing tests must pass unchanged (504 Languages + 149 Printers + 139 Cli).

**Docs:** none (docs land in S4 after semantics are confirmed by the checkpoint).

**Spec:**
- Tracking event = book's AddEdge trigger: a new GSS edge is added from a vertex whose
  stored states are non-empty; the vertex recorded is the edge source (`fromIdx`).
- The set accumulates over the whole level (all rounds) and is captured in that level's
  step snapshot; step 0 is empty.
- Algorithm behavior (path index, SPPF, acceptance) must be byte-identical — tracking is
  observation-only.

### S2: Add orange row to RNGLR summary legend — [done]

**Code:** `src/FLPQ.Printers/SummaryTeX.fs` — `rnglrColorLegend`: add
`colorBox "orange!30", "Passing reductions handling triggered at GSS vertex"` (same color
as the GLL stored-pops row, per task: "Share color with one used for GLL stored pops").

**Tests:** `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` — new test
``RNGLR summary color legend includes passing-reductions orange row`` mirroring the GLL
test (merged TeX contains the legend text and `\colorbox{orange!30}`).

**Docs:** `docs/developer/summary-tex.md` — note the RNGLR legend row.

**Spec:**
- Row wording parallels GLL's "Stored pops handling triggered at GSS vertex".
- No changes to `gllColorLegend`.

### S3: Runner-level tests for the orange highlight — [done]

**Tests:** `tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs` — mirror task 259 GLL tests:
- ``runRnglr passing-reductions step highlights GSS vertex orange (dot mode)`` — grammar
  identified by the S1 experiment that triggers passing reductions; assert some non-init
  step's `gss.dot` contains `fillcolor=orange`.
- ``runRnglr passing-reductions step 0 has no orange highlight (dot mode)``.
- ``runRnglr passing-reductions step highlights GSS vertex orange (tikz mode)`` — assert
  `fill=orange!30` in some non-init step's `gss.tikz.tex`.

**Docs:** none.

**Spec:**
- If the S1 experiment found no triggering grammar, this subtask is blocked — report to
  the user (Blocked Work Protocol) instead of weakening assertions.

### S4: Documentation — [done]

**Docs:**
- `docs/developer/rnglr.md`:
  - `RnglrParsingStep` type block: add `PassingReductionVertices` field + explanation.
  - Algorithm section: note that passing-reduction trigger vertices are tracked per level
    (consumption of stored states when a new edge is added from a vertex).
  - Design Decisions: new row — orange highlight for passing-reduction trigger vertices,
    color shared with GLL stored pops (task 259/261).
- `docs/developer/summary-tex.md` (if not fully covered in S2): RNGLR legend row.

**Spec:**
- Follow documentation conventions (metadata tags unchanged; Kind/Module unchanged).

## Verification

- All existing tests pass (zero regressions, zero skipped).
- Hard gate: STATUS PASS (format, build, all test projects, coverage, lint on touched
  projects).
- Golden files: step-0 goldens (`rnglr_gss_step0.dot` etc.) must remain byte-identical —
  step 0 has no passing-reduction vertices.
