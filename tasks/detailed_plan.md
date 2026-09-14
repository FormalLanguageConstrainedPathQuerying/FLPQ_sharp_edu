# Detailed Plan: Task 274 — Reorder RNGLR level driver to canonical reduce-then-shift order

## Task description (verbatim)

274. Reorder the RNGLR level driver to canonical order: at each level (input position) first apply all possible reductions to fixpoint, then execute shifts from all vertices of the level in a single pass; remove the round loop and shifted-state tracking. Canonical references: Scott & Johnstone 2006, Algorithm 1e PARSE SYMBOL (ACTOR/REDUCER loop until A and R empty, then SHIFTER) and book sec:CFPQ_GLR (MakeReductions → Push → ApplyPassingReductions). Reverses the shift-then-reduce decision of tasks 225/260. Visualization keeps one step per level; output for `data/example_input_a_a_a.txt` + `data/example_grammar_a_a_a.bnf` must stay byte-identical; all existing RNGLR tests pass unchanged.

## Context (analysis findings)

- Canonical order evidence:
  - Scott & Johnstone 2006 ("Right Nulled GLR Parsers", `papers_pdf/` in the book repo),
    Algorithm 1e: `PARSE SYMBOL(i) { A = Ui; while A ≠ ∅ or R ≠ ∅ { if A ≠ ∅ do ACTOR(i) else do REDUCER(i) }; do SHIFTER(i) }`.
    ACTOR only *collects* shifts into Q; REDUCER applies reductions to fixpoint (new nodes re-enter A);
    SHIFTER executes all collected shifts as a batch after A and R are empty. Prose: "at each string
    position, all possible reductions are performed before executing the shift operations."
  - Book sec:CFPQ_GLR (`tex/part_03_GraphAnalysis/chapter_12_CFPQ/07_GLR_Based.tex`), main loop per
    dequeued vertex: `MakeReductions(v)` → `Push(v)` → `ApplyPassingReductions(v)`.
- Current driver (`src/FLPQ.Languages/Rnglr.fs`, level loop): `[shift unshifted vertices at v] → [reduceAtLevel v]` repeated in rounds until no new vertex appears at v. Opposite of canonical.
- Why the reorder is behavior-preserving for all observable outputs:
  - Per-level work is the same set of edges in either order: reductions at level v traverse only GSS
    positions ≤ v; shifts create vertices only at v+1 and are invisible to level-v reductions.
  - Step snapshots (`RnglrParsingStep`) are all sets (order-independent); one step per level unchanged.
  - Passing-reduction triggers (StoredStates consumption) fire in the same cases: deposits at position w
    happen only during levels > w; consumption happens when edges from that vertex are added — the same
    vertices, same level, same sets under either order.
- No rounds needed in canonical order: reduce-to-fixpoint handles all reduction-created vertices at v;
  the single shift pass then covers every vertex at v (shifts create no new vertices at v).
- Vertex IDs may renumber for other inputs (goto targets created before shift targets within a level);
  labels change, semantics don't. For the reference example (S → a | S S | S S S, input "aaa") all five
  steps were traced: IDs identical → output byte-identical.
- History: tasks 225 and 260 chose shift-then-reduce as "canonical"; this task reverses that decision
  based on the primary source (the paper) and the book's own algorithm order.

## Reuse decisions

- Reused as-is: `reduceAtLevel`, `shiftNode`, `processReduction`, `productBfs`, `findPredecessors`
  (only the driver call order changes; no function bodies change except `reduceAtLevel`'s now-unused
  return value).
- No new functions, types, or test helpers. Existing test suite is the verification vehicle.
- Docs: `docs/developer/rnglr.md` is the single source of truth for the RNGLR algorithm description —
  updated in place, no new doc file.

## Subtasks

### S1: Reorder level driver to canonical reduce-then-shift — [done] commit bbb7dcc

**Code:** `src/FLPQ.Languages/Rnglr.fs` only:

- Level loop (currently lines ~421-433): replace the round loop
  (`while again { for lrState in verticesAt gss v do if shifted.Add(lrState) then shiftNode lrState v; again <- reduceAtLevel v }`) with canonical order:
  `reduceAtLevel v` (fixpoint) followed by a single pass
  `for lrState in RnglrGSS.verticesAt gss v do shiftNode lrState v`.
- Delete the `shifted` HashSet and the `again` mutable.
- Change `reduceAtLevel` signature from `(v: int) : bool` to `(v: int) : unit` — the
  `newVertexAtV` signal existed only to drive rounds; drop the flag and its accumulation.
- Update comments/doc-comments to describe canonical order with references:
  - level-driver comment block (currently "first shift every not-yet-shifted GSS vertex at v, then
    reduce to fixpoint; repeat rounds until the level stabilizes") → "first apply all possible
    reductions at v to fixpoint, then execute shifts from all vertices of the level in a single pass;
    book reference: sec:CFPQ_GLR (MakeReductions → Push → ApplyPassingReductions), Scott & Johnstone
    2006 Algorithm 1e PARSE SYMBOL".
  - `buildPathIndex` doc comment ("shift all unshifted GSS vertices, then reduce to fixpoint") →
    canonical wording.
  - `reduceAtLevel` doc comment (drop "Returns true when a new GSS vertex appeared at v — it must be
    shifted in the next round of the level loop").
  - `shiftNode` doc comment stays accurate (single-vertex shift phase); adjust only if it references
    rounds.

**Tests:** All existing RNGLR tests pass unchanged: `tests/FLPQ.Languages.Tests/RnglrTests.fs`
(acceptance, tree yield, right-nullable, reduction cascade, property-based,
`RnglrPassingReductions`, SPPF) and `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs`
(golden step 0 + compilation). No new tests in this subtask.

**Docs:** None (S2 owns documentation updates).

**Spec:**

- New level loop body:
  ```fsharp
  for v in 0 .. vertexCount - 1 do
      levelReductions <- Set.empty

      // Canonical order (Scott & Johnstone 2006, Algorithm 1e PARSE SYMBOL; book sec:CFPQ_GLR):
      // first apply all possible reductions at v to fixpoint, then execute shifts from all
      // vertices of the level in a single pass. Shifts create no new vertices at v, so one
      // shift pass suffices — no rounds.
      reduceAtLevel v

      for lrState in RnglrGSS.verticesAt gss v do
          shiftNode lrState v
  ```
- `verticesAt` returns a snapshot list; during the shift pass no new vertices are created at v
  (shifts target v+1), so the snapshot remains complete.
- Step capture (`onStep`) and per-level set resets stay exactly where they are (after the shift pass).
- `shiftNode`, `processReduction`, `productBfs`, `findPredecessors` bodies: untouched.

### S2: Update docs/developer/rnglr.md to canonical order — [done] commit ca630c4

**Code:** None.

**Tests:** mdformat check on the changed file (part of quality gates).

**Docs:** `docs/developer/rnglr.md`:

- Abstract (line 11): "first shift, then all possible reductions to fixpoint" → "first apply all
  possible reductions to fixpoint, then execute shifts from all vertices of the level in a single
  pass".
- Algorithm section: swap Phase 1/Phase 2 — Phase 1 becomes reduce-to-fixpoint (`reduceAtLevel`),
  Phase 2 becomes the single shift pass; delete the "Rounds" bullet (no rounds in canonical order);
  keep the passing-reduction tracking bullet (still accurate).
- Design decisions table: remove the "Round loop until the level stabilizes" row; update the
  "Level-based driver without descriptors" row to state the canonical order, cite Scott & Johnstone
  2006 Algorithm 1e PARSE SYMBOL and book sec:CFPQ_GLR, and note that this reverses the
  shift-then-reduce decision of tasks 225/260 (rationale: the paper's PARSE SYMBOL applies reductions
  to fixpoint before SHIFTER executes collected shifts).

**Spec:**

- Single source of truth: only `docs/developer/rnglr.md` describes the driver order; no duplication
  elsewhere.
- File must be mdformat-clean before commit.

### S3: End-to-end verification (tests + byte-identical reference visualization) — [done]

**Result:** Full suite 961 passed / 0 failed / 0 skipped across all six test projects
(LinearAlgebra 51, GraphAnalysis 33, Languages 505, Printers 186, RPQ 27, Cli 159). Reference
visualization regenerated with the exact task command: all 29 `.tex`/`.dot` artifacts byte-identical
to the pre-change baseline; the two graphviz PDFs differ only in embedded CreationDate metadata
(content verified identical via pdftotext). `RnglrPassingReductions` facts pass.

**Code:** None.

**Tests:**

- Full test suite: `dotnet test` across all test projects — 0 failures, 0 skipped.
- Reference visualization regression: save current `viz_output/GLR` (pre-change baseline), rebuild the
  CLI, re-run the exact command
  `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -s -o viz_output/GLR -a rnglr -i data/example_input_a_a_a.txt -g data/example_grammar_a_a_a.bnf`, then diff the regenerated tree
  against the baseline — must be byte-identical (all step files, summary TeX, table, SPPF).
- Passing-reduction regression: `RnglrPassingReductions` facts (epsilon-edge trigger path) pass.

**Docs:** None.

**Spec:**

- Baseline is captured before the S1 change lands on this branch (viz_output/GLR currently on disk was
  generated by the pre-change binary).
- Any byte difference blocks the task: report it, do not paper over it.
- `viz_output/` is not tracked; the diff is a local verification artifact only.
