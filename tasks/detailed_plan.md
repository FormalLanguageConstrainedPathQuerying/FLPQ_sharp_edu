# Task 259: GLL stored-pops vertex highlighting

## Reuse Analysis

- Reuse `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` — extend their existing
  highlight mechanism (already has `highlightedVertices`/`currentVertex`) with a new
  `storedPopVertices` set. No new DOT/TikZ emitter needed.
- Reuse the existing `onStep` callback plumbing in `Gll.buildPathIndexCore` — add one
  more tracked set (`storedPopVertices`) mirroring the existing `changedCells` ref pattern.
- Reuse `gllColorLegend` in `SummaryTeX.fs` — append one legend row.
- Task 261 (RNGLR passing reductions) will reuse the same color; keep the color choice a
  single constant so it is shared. RNGLR callers of `toDotFromSets`/`toTikzFromSets` pass
  `Set.empty` for now.

## Color choice

New highlight color: **orange** (`fillcolor=orange` in DOT, `fill=orange!30` in TikZ).
Distinct from existing palette (lightblue=current, lightyellow/yellow!30=new vertex,
red=new edge, yellow=changed cell, green!30=input position). Legend text:
"Stored pops handling triggered at GSS vertex".

## When stored pops trigger (verified empirically)

A GSS vertex `(N_start, v)` gets a stored-pop highlight when `GSS.addEdge` returns a
non-empty set — i.e. nonterminal N at input position v has already been completed (popped)
and a *new* caller now re-enters it. Minimal triggering grammar:
`S -> A | B`, `A -> a`, `B -> C`, `C -> A` with input `a`. Here A is called at pos 0 by both
S->A and (later, after A has popped) by C via S->B->C, so the second addEdge to `(A_start,0)`
returns the stored pop. Verified: step 6 highlights vertex 0 orange in both DOT and TikZ.

## Subtasks

- [x] S1: Track + render stored-pop GSS vertices — committed `98a5e87`
- [x] S2: Add color to GLL summary legend + documentation — this commit

---

### S1: Track + render stored-pop GSS vertices

**Code:** `src/FLPQ.Languages/GllTypes.fs` (`GLLParsingStep.StoredPopVertices`),
`src/FLPQ.Languages/Gll.fs` (`buildPathIndexCore` tracking + `onStep` arity),
`src/FLPQ.Printers/GssDot.fs` (`toDotFromSets`), `src/FLPQ.Printers/GssTikz.fs`
(`toTikzFromSets`), `src/FLPQ.Printers/GllStepVisualizer.fs` (`renderStep`, `renderInit`),
`src/FLPQ.Printers/RnglrStepVisualizer.fs` (pass `Set.empty`)
**Tests:** `tests/FLPQ.Cli.Tests/GllRunnerTests.fs` (orange in dot + tikz steps, none in
step 0), `tests/FLPQ.Printers.Tests/GssDotTests.fs` (unit test on `toDotFromSets` priority)
**Docs:** none (S2 covers docs)

**Spec:**
- `GllTypes.fs`: add `StoredPopVertices: Set<int>` field to `GLLParsingStep`.
- `Gll.fs`: track `storedPopVertices` ref; in Case 2, when `GSS.addEdge` returns non-empty
  storedPops, add `gssTarget`; pass to `onStep`; reset per step. Update `buildPathIndex`
  no-op lambda and `buildPathIndexWithSteps` record construction.
- `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`: new required param
  `storedPopVertices: Set<int>`. Priority current > stored-pop (orange) > highlighted > normal.
- `GllStepVisualizer.renderStep`/`renderInit`: pass `step.StoredPopVertices` / `Set.empty`.
- `RnglrStepVisualizer.renderStep`: pass `Set.empty` (RNGLR passing reductions = task 261).

---

### S2: Add color to GLL summary legend + documentation

**Code:** `src/FLPQ.Printers/SummaryTeX.fs` (`gllColorLegend`)
**Tests:** none new (legend is string content; covered by existing summary compilation)
**Docs:** `docs/developer/gll.md` (step snapshot field + color scheme),
`docs/developer/summary-tex.md` (legend row)

**Spec:**
- Append legend row to `gllColorLegend`:
  `colorBox "orange!30", "Stored pops handling triggered at GSS vertex"`.
- Document `GLLParsingStep.StoredPopVertices` and the orange highlight in gll.md.
- Note the new legend row in summary-tex.md.
