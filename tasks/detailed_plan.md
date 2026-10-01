# Detailed Plan: Task 289 — Split overlapped reciprocal edges in TikZ graph/automaton renderers

## Task description (verbatim)

```
289. Improve automata rendering in TikZ. Now several edges between same vertices overlapped (eg `0 -[a]-> 1`; `1 -[a]-> 0`; `0 -[b]-> 1` visually is one line with several labels). It should be fixed by visual splitting of edges (two edges in our case: `0 -[a,b]-> 1`; `1 -[a]-> 0`). Apply always-on reciprocal-edge bending to all TikZ graph renderers that can contain reciprocal edges (AutomatonTikz covering NFA/DFA/LR/RNGLR, RsmTikz, InputGraphTikz), reusing the existing GssTikz/RpqGraphViz `bend left=15` approach through a shared helper. Self-loops are never bent. Acyclic tree renderers (SppfTikz, BasicSppfTikz, DerivationTreeTikz) are out of scope. Add tests (unit, golden, lualatex-compile) and update documentation.
**[USER GUIDANCE]**: Preserve RSM same-direction edges as-is — RsmTikz keeps one edge line per transition symbol within a direction; only reciprocal pairs are split. GssTikz keeps its opt-out flag for GLL/RNGLR (behavior unchanged for RPQ).
```

## Problem

`AutomatonTikz.transitionEdges` (`src/FLPQ.Printers/AutomatonTikz.fs:82`) merges parallel labels per
matrix cell, so `0 -[a]-> 1` and `0 -[b]-> 1` render as one edge `0 -[a,b]-> 1`. The reverse edge
`1 -[a]-> 0` is emitted as a straight line, and TikZ draws it exactly on top of the first, so the
figure reads as a single line carrying all labels. The fix already exists in `GssTikz`
(`src/FLPQ.Printers/GssTikz.fs:129-140`): when the reverse edge is also drawn, both directions get
`, bend left=15`, turning the pair into two symmetric arcs. `RpqGraphViz` enables it for RPQ graphs
(task 282). No existing golden/snapshot has reciprocal automaton/RSM edges (verified by per-picture
scan), so the change adds new snapshots but rewrites none.

## Reuse decisions (reusing skill)

- Reused: the `bend left=15` reciprocal rule and its wording already present in `GssTikz.fs:129-140`
  and documented in `docs/developer/gss-tikz.md`, `docs/developer/rpq-graph-viz.md`.
- Extracted (Q5): a single shared helper `AutomatonTikz.reciprocalBendAttr` holding the bend
  constant, the self-loop guard, and the `enabled` gate. `GssTikz` adopts it so the rule has one
  source of truth.
- Reused test infrastructure: `ExternalTools.compileTexStringWithTemplate` + `tex_tikz_template.tex`
  for lualatex compile tests, `GoldenHelpers.verifyGolden` for snapshots, `Graph.fromEdges` for the
  InputGraphTikz reciprocal fixture, the minimal-RSM construction pattern from `RsmTikzTests.fs:138-156`.

## Scope

Always-on reciprocal bending in:

| Renderer | Edge kinds |
| --- | --- |
| `AutomatonTikz` | terminal (`transitionEdges`) + epsilon (`epsEdges`); covers NFA/DFA/LR/RNGLR via delegation |
| `RsmTikz` | one line per symbol (unchanged); reciprocal pairs get the bend |
| `InputGraphTikz` | labeled edges |
| `GssTikz` | refactored to reuse the shared helper; keeps `bendReciprocalEdges` opt-out |

Out of scope: `SppfTikz`, `BasicSppfTikz`, `DerivationTreeTikz` (acyclic — reciprocal edges cannot
occur), DOT renderers (Graphviz separates pairs automatically).

---

## S1: Shared reciprocal-bend helper + `AutomatonTikz` + `GssTikz` reuse

**Status:** Resolved

**Code:**

- `src/FLPQ.Printers/AutomatonTikz.fs`:
  - Add `reciprocalBendAttr (enabled: bool) (reverseDrawn: bool) (fromIdx: int) (toIdx: int) : string`
    returning `", bend left=15"` when `enabled && fromIdx <> toIdx && reverseDrawn`, else `""`.
    Place near `escapeLatex`.
  - `transitionEdges`: compute
    `let bendAttr = reciprocalBendAttr true (Option.isSome transitions.[j, i]) i j` inside the
    `Some symbols` arm; emit options in the order `label, hlAttr, bendAttr, loopAttr`; update the
    empty-label arm to use the trimmed bend when present.
  - `epsEdges`: same `bendAttr`; emit `dotted, "$\varepsilon$", bendAttr, loopAttr`.
- `src/FLPQ.Printers/GssTikz.fs`: replace the inline predicate/constant (lines 129-140) with
  `reciprocalBendAttr bendReciprocalEdges (Set.contains (toIdx, fromIdx) activeEdges) fromIdx toIdx`;
  the bare-edge branch trims the leading `", "` from the attribute.

**Tests:**

- `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs`:
  - `reciprocal transitions are bent to separate overlapping edges`: NFA `0-[a]->1, 0-[b]->1, 1-[a]->0`; assert `s0 ->["a, b", bend left=15] s1;` and `s1 ->["a", bend left=15] s0;` and that
    the unbent straight forms are absent.
  - `one-way edges are not bent`: NFA `0-[a]->1`; assert no `bend`.
  - `self-loops are not bent`: NFA `0-[a]->0` (and a loop with a reverse-labelled sibling); assert
    `loop above` present and no `bend` on the loop line.
  - `reciprocal epsilon transitions are bent`: minimal NFA with epsilon both directions; assert
    `dotted, "$\varepsilon$", bend left=15`.
  - `dfaToTikzWithHighlights keeps the bend on a highlighted reciprocal edge`: assert
    `s0 ->["a, b", red, thick, bend left=15] s1;`.
  - `[<Trait("Category","TeX")>]` lualatex compile of the reciprocal NFA.
  - Golden `GoldenData/nfa_reciprocal_edges.tikz` created with `CREATE_GOLDEN_FILES=1`.
- Existing `tests/FLPQ.Printers.Tests/GssDotTests.fs` bend tests must pass unchanged (helper
  adoption is behavior-preserving).

**Docs:**

- `docs/developer/automaton-viz.md`: add a Visual Style bullet for reciprocal edges and a Design
  Decisions row; note self-loops are never bent.
- `docs/developer/gss-tikz.md`: note the reciprocal rule is provided by
  `AutomatonTikz.reciprocalBendAttr`.

**Spec:**

- `fromIdx <> toIdx` guard means self-loops never bend.
- Bend applies to highlighted and path tiers alike; attribute order keeps the label first.
- No public signature changes.

---

## S2: `RsmTikz` reciprocal bending

**Status:** Resolved

**Code:**

- `src/FLPQ.Printers/RsmTikz.fs`: inside the per-symbol loop compute
  `let bendAttr = AutomatonTikz.reciprocalBendAttr true (Option.isSome rsm.Transitions.[j, i]) i j`;
  emit options in the order `label, style, bendAttr, loopAttr`. Same-direction edges stay one line
  per symbol (no label merging).

**Tests:**

- `tests/FLPQ.Printers.Tests/RsmTikzTests.fs`: add `RSM tikz bends reciprocal transitions` building a
  minimal 2-state RSM with `0-[a]->1` and `1-[a]->0` (pattern from the epsilon test at
  `RsmTikzTests.fs:138-156`); assert both `s0 ->["a", bend left=15] s1;` and
  `s1 ->["a", bend left=15] s0;`; also assert a one-way RSM transition is not bent and a self-loop
  is not bent. Add a `[<Trait("Category","TeX")>]` lualatex compile of the reciprocal RSM.

**Docs:**

- `docs/developer/rsm-viz.md`: Edge-labels bullet noting reciprocal transitions are bent
  (`bend left=15`) while same-direction edges remain one line per symbol.

**Spec:**

- Same-direction per-symbol emission unchanged (`RSM tikz emits one edge line per transition symbol`
  must still pass).

---

## S3: `InputGraphTikz` reciprocal bending

**Status:** Resolved

**Code:**

- `src/FLPQ.Printers/InputGraphTikz.fs`: emit
  `v%d ->["%s"%s] v%d;` with
  `AutomatonTikz.reciprocalBendAttr true (Option.isSome inputGraph.Edges.[j, i]) i j`.

**Tests:**

- New `tests/FLPQ.Printers.Tests/InputGraphTikzTests.fs` (add to
  `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj`): build a reciprocal graph with
  `Graph.fromEdges [0;1] (Matrix.init 2 2 None)` setting `[0,1]`/`[1,0]` to `Some "a"`; assert both
  edges carry `, bend left=15`; assert the linear-path graph from `GLL.stringToGraph` has no `bend`
  and `[<Trait("Category","TeX")>]` compiles.

**Docs:**

- New `docs/developer/input-graph-tikz.md` (InputGraphTikz currently has no doc) describing the
  renderer and the reciprocal-bend rule; add it to the module table in
  `docs/developer/FLPQ.Printers.md`.

**Spec:**

- Input graphs are normally linear paths (no reciprocal edges), so the rule is defensive for the
  generic `Graph<int, Option<'t>>` input; it is exercised by the synthetic reciprocal fixture.

---

## Testing strategy summary

| Level | Coverage |
| --- | --- |
| Unit | Exact edge-string assertions for reciprocal/one-way/self-loop/epsilon/highlighted cases in Automaton, RSM, InputGraph |
| Golden | `nfa_reciprocal_edges.tikz` |
| TeX | lualatex compile of reciprocal automaton, RSM, input graph |
| Regression | Existing GssTikz bend tests and all renderer goldens unchanged |

## Execution Log

| Subtask | Commit | Status |
| --- | --- | --- |
| S1 | `8604ea2` | Resolved |
| S2 | `a3293e5` | Resolved |
| S3 | `4867257` | Resolved |
