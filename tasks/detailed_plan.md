# Detailed Plan: Task 275 — RNGLR GSS same-input-position nodes on the same layer

## Task description (verbatim)

275. Improve RNGLR GSS visualization: all GSS nodes at the same input position must be drawn on the same layer, preserving the current left-to-right global layout (input position 0 stays rightmost; higher positions to its left). The position-layering is opt-in via a new trailing `positionOf: (int -> int) option` parameter on `GssDot.toDotFromSets` and `GssTikz.toTikzFromSets`; RNGLR passes `Some (fun idx -> snd (vertexInfo idx))`, GLL passes `None` (unchanged). DOT: when `positionOf` is `Some`, emit one `{rank=same; ...}` subgraph per input position grouping that position's vertices. TikZ: when `positionOf` is `Some`, emit one `{ [same layer] ... }` collection per input position AND switch the graph grow direction from `grow'=right` to `grow=left`, because pgf's same-layer cluster chaining reverses the orientation under the default grow direction (verified: `grow=left` keeps position 0 rightmost, `grow'=right` flips it). Refactor `AutomatonTikz.layeredGraphOptions` to take a grow-direction argument with named defaults so the GSS renderer can select `grow=left` without duplicating option text. Tests: DOT coordinate test asserting same-position nodes share an x-coordinate and position 0 is rightmost; TikZ structural test asserting correct same-layer groupings and `grow=left`; existing GLL/RNGLR tests pass unchanged.

## Context (analysis findings)

- A GSS vertex is a `(lrState, inputVertex)` pair; `vertexInfo idx : int -> int * int` gives
  `(lrState, inputVertex)`. The **input position** of a vertex is `snd (vertexInfo idx)`.
- Every RNGLR GSS edge points from a HIGHER input position to a LOWER one (or the same):
  shift `targetGssIdx(vNext) -> shiftGssIdx(v)` with `vNext > v`; reduction
  `gotoGssIdx(vEnd) -> gssIdxPre(vPre)` with `vEnd >= vPre`. So positions form left-to-right
  columns and the natural layered layout already orders them by position — but nodes sharing a
  position are scattered across adjacent layers (the bug).
- Current orientation (both engines, verified on `an_bn` step 3 and `a_a_a` step 4): input
  position 0 is **rightmost**, higher positions to its left. This is the layout to preserve.
- DOT: adding one `{rank=same; ...}` subgraph per position groups same-position nodes onto a
  single layer AND preserves orientation (position 0 stays rightmost). Verified on both examples.
- TikZ (pgf `layered layout`, pure-Lua Sugiyama): adding `{ [same layer] ... }` collections
  under the default `grow'=right` **reverses** the orientation (position 0 becomes leftmost) —
  pgf chains same-layer clusters in declaration order with `minimum_levels=1`, which flips the
  effective direction. Switching to `grow=left` + same-layer collections groups correctly AND
  keeps position 0 rightmost. Verified on both examples; declaration order of the collections is
  irrelevant (ascending and descending give the same result), so they are emitted in ascending
  position order for determinism.
- Shared renderers: `GssDot.toDotFromSets` and `GssTikz.toTikzFromSets` are used by BOTH GLL
  (`GllStepVisualizer`) and RNGLR (`RnglrStepVisualizer`). The layering is opt-in so GLL output
  is byte-identical (it passes `None`).
- `AutomatonTikz.layeredGraphOptions shape` currently hard-codes `grow'=right`; it is used by
  `tikzHeader` (NFA/DFA/GSS/InputGraph) and directly by `RsmTikz`. Refactor to take a grow
  direction so GSS can select `grow=left` without duplicating the option text.

## Reuse decisions

- Reused as-is: `GssDot.toDotFromSets`, `GssTikz.toTikzFromSets` (extend, do not fork),
  `AutomatonTikz.tikzHeaderWithOptions`, `ExternalTools.runProcess` (new position parser reuses it).
- Existing same-layer idioms reused: DOT `{rank=same; ...}` (`DerivationTreeDot.fs:105`),
  TikZ `{ [same layer] ... }` (`DerivationTreeTikz.fs:76,142`).
- No new modules. Docs updated in place: `docs/developer/rnglr.md`, `docs/developer/automaton-viz.md`.

## Subtasks

### S1: Parameterize the layered TikZ grow direction in AutomatonTikz

**Code:** `src/FLPQ.Printers/AutomatonTikz.fs` — change
`layeredGraphOptions (shape)` to `layeredGraphOptions (shape) (growDirection)`; add constants
`defaultGrowDirection = "grow'=right"` and `gssLayeredGrowDirection = "grow=left"`; update
`tikzHeader` to pass `defaultGrowDirection`. `src/FLPQ.Printers/RsmTikz.fs` — update the direct
`layeredGraphOptions "circle"` call to pass `AutomatonTikz.defaultGrowDirection`.
**Tests:** No behavior change; existing automaton/RSM/GSS TikZ tests must still compile and pass.
**Docs:** `docs/developer/automaton-viz.md` — update the layout line to note the grow direction is
parameterized (default `grow'=right`; GSS uses `grow=left` when position layers are active).

**Spec:**

- `layeredGraphOptions shape growDirection` =
  `sprintf "layered layout, nodes={draw, %s}, %s, level sep=2cm, sibling sep=1.5cm" shape growDirection`.
- `defaultGrowDirection = "grow'=right"` (unchanged default for NFA/DFA/RSM/InputGraph).
- `gssLayeredGrowDirection = "grow=left"` (reserved for the GSS renderer in S3).
- `tikzHeader shape sb` calls `tikzHeaderWithOptions (layeredGraphOptions shape defaultGrowDirection) sb`.
- `RsmTikz` passes `AutomatonTikz.defaultGrowDirection` so its output is byte-identical.

### S2: Add opt-in position layers to the GSS DOT renderer

**Code:** `src/FLPQ.Printers/GssDot.fs` — add trailing parameter
`positionOf: (int -> int) option` to `toDotFromSets`. After all vertices and edges are emitted,
when `Some posFn`, group every rendered vertex by `posFn v` and emit one
`  {rank=same; vA; vB; ...};` subgraph per position (positions in ascending order, vertices sorted).
Update GLL callers (`GllStepVisualizer.fs:153,233`) and the two test callers
(`GssDotTests.fs:110,135`) to pass `None`.
**Tests:** `tests/FLPQ.Printers.Tests/GssDotTests.fs` — update the two existing `toDotFromSets`
calls (pass `None`); add a unit test that a synthetic `positionOf` produces one `{rank=same; ...}`
subgraph per position with the correct members.
**Docs:** none in this subtask (API doc lands with S4/S7).

**Spec:**

- Collect the set of all rendered vertices (active + edge-referenced + current) already tracked by
  `renderedVertices`.
- For each distinct position `p` (ascending), let `vs = sorted [v in rendered | posFn v = p]`;
  emit `sprintf "  {rank=same; %s;}" (String.concat "; " (vs |> List.map (sprintf "v%d")))`.
- Emit subgraphs for every position, including singletons (uniform, explicit layering).
- When `positionOf` is `None`, output is byte-identical to today.

### S3: Add opt-in position layers to the GSS TikZ renderer

**Code:** `src/FLPQ.Printers/GssTikz.fs` — add trailing parameter
`positionOf: (int -> int) option` to `toTikzFromSets`. When `Some`, open the graph with
`AutomatonTikz.tikzHeaderWithOptions (AutomatonTikz.layeredGraphOptions shape AutomatonTikz.gssLayeredGrowDirection) sb`
(`grow=left`) and, after the edges, emit one `    { [same layer] vA, vB, ... };` collection per
position (ascending, vertices sorted). When `None`, keep the current `AutomatonTikz.tikzHeader shape sb`.
Update GLL callers (`GllStepVisualizer.fs:172,252`) to pass `None`.
**Tests:** No new test here (structural TikZ test lands in S6); existing GSS/automaton TikZ tests
must still compile and pass.
**Docs:** none in this subtask.

**Spec:**

- Determine `hasLayers = positionOf.IsSome` before opening the header; select the grow direction
  accordingly (`gssLayeredGrowDirection` when `hasLayers`, else `defaultGrowDirection`).
- Reuse the same `allVertices` set already computed for node emission as the grouping source.
- For each distinct position `p` (ascending), let `vs = sorted [v in allVertices | posFn v = p]`;
  emit `sprintf "    { [same layer] %s };" (String.concat ", " (vs |> List.map (sprintf "v%d")))`.
- When `positionOf` is `None`, output is byte-identical to today.

### S4: Wire RNGLR to pass the input position as the layer function

**Code:** `src/FLPQ.Printers/RnglrStepVisualizer.fs` — in `renderStep`, pass
`Some (fun idx -> snd (vertexInfo idx))` as the new trailing argument to both
`GssDot.toDotFromSets` and `GssTikz.toTikzFromSets`. `renderSteps` delegates to `renderStep`, so no
further change.
**Tests:** Existing RNGLR visualization tests still pass (fills, compilation, step-0 golden).
**Docs:** `docs/developer/rnglr.md` — add a Design Decisions row: GSS nodes at the same input
position are constrained to one layer (`{rank=same}` / `{ [same layer] }`) via the opt-in
`positionOf` parameter; TikZ uses `grow=left` so position 0 stays rightmost (pgf cluster chaining
reverses orientation under the default grow direction).

**Spec:**

- The layer function is exactly the input vertex: `fun idx -> snd (vertexInfo idx)`.
- GLL call sites remain `None` — GLL output is unchanged.

### S5: Expose Graphviz node positions for layout tests

**Code:** `src/FLPQ.Printers/ExternalTools.fs` — add
`compileDotStringToNodePositions (dot: string) : Map<string, float * float>` that runs
`dot -Tjson` (reusing the private `runProcess`) and maps each node name to its `(x, y)` from the
`pos` field. Throws on non-zero exit, mirroring `compileDotStringToInfo`.
**Tests:** `tests/FLPQ.Printers.Tests/ExternalToolsTests.fs` — a test that a small two-node digraph
yields two positions with distinct coordinates.
**Docs:** `docs/developer/external-tools.md` — add the new function to the API list.

**Spec:**

- Parse the JSON `objects` array; for each object with both `name` and `pos`, split `pos` on `,`
  into `(x, y)` floats and record `name -> (x, y)`.
- Return a `Map<string, float * float>`.

### S6: Tests for same-position layering and orientation

**Code:** none (tests only).
**Tests:**

- `tests/FLPQ.Printers.Tests/RnglrStepVisualizationTests.fs`:
  - Extend the private `RnglrVizData` / `renderViz` to also capture `GssTikzs`.
  - DOT coordinate test: render a multi-position RNGLR step (e.g. `a_a_a`), get node positions via
    `ExternalTools.compileDotStringToNodePositions`, and assert (a) all nodes at the same input
    position share an x-coordinate (within 1e-3), and (b) position 0 is rightmost (its x is the
    maximum) with x strictly decreasing as position increases.
  - TikZ structural test: assert the rendered GSS TikZ contains `grow=left` and one
    `{ [same layer] ... }` collection per input position with exactly that position's vertices.
- `tests/FLPQ.Printers.Tests/GssDotTests.fs`: (added in S2) unit test for rank=same emission.
- Add a non-trivial RNGLR GSS DOT golden (`rnglr_gss_stepN.dot`) locking the new format; generate
  via `CREATE_GOLDEN_FILES=1` and copy into `GoldenData/`.
  **Docs:** none.

**Spec:**

- Orientation assertion: for every pair of positions `p < q`, `maxX(p) > minX(q)` (columns do not
  overlap) and within a position all x are equal; the global max-x column is position 0.
- TikZ grouping assertion: parse the emitted `{ [same layer] ... }` lines and check each position's
  vertex set matches `[v | posFn v = p]`.

### S7: Documentation pass

**Code:** none.
**Tests:** none.
**Docs:**

- `docs/developer/rnglr.md` — Design Decisions row for position-layering (added in S4) plus a short
  note that the GSS figure keeps input position 0 rightmost.
- `docs/developer/automaton-viz.md` — grow-direction parameterization (added in S1).
- `docs/developer/external-tools.md` — new `compileDotStringToNodePositions` (added in S5).

**Spec:**

- Each doc change reflects the exact public API and the "why" (pgf cluster chaining reverses
  orientation under the default grow direction; hence `grow=left` when layers are active).

## Progress

| Subtask | Status | Commit |
| --- | --- | --- |
| S1 | done | `523fc60` |
| S2 | done | `f437023` |
| S3 | done | `236b35a` |
| S4 | done | `de7ab7f` |
| S5 | done | `124ea74` |
| S6 | done | `1e81a65` |
| S7 | done | (this commit) |

S7 is a documentation consistency pass. The three required doc changes already landed with their
own subtasks — grow-direction parameterization in `automaton-viz.md` (S1), the position-layering
Design Decisions row in `rnglr.md` (S4), and `compileDotStringToNodePositions` in
`external-tools.md` (S5). S7 verified each reflects the exact public API and the "why", confirmed
no other doc carries a stale signature, and confirmed GLL output stays byte-identical (it passes
`None`). No further content change was required; this commit records subtask completion.
