# Detailed Plan: Task 270 — Improve RSM rendering

## Task description (verbatim)

270. Improve RSM rendering. 1. remove 'call' word from nonterminal edges. Just terminal name. 2. Remove nonterminal name from state. Just number. Current state lable has form N_i. Transform it to just i. 3. Carefully check nodes styling. Now, in GLL steps visualization in case when final state is a part of current gss node, we miss doublecircle for this finak state. Light blue is correct, we use it for current node, but if this node is final, it must be double cyrcled, as regular funal state. Add tests: for all steps in GLL, all rendered to tikz RSMs must have exactly the same number of doubly cyrcled nodes. It must be equal to total number of final states in the input extended RSM.

## Scope decisions

- **Items 1–2 apply to both RSM renderers** (`RsmTikz.extendedRsmToTikz`,
  `RsmDot.extendedRsmToDot`, and `RsmDot.toDot` for the per-block "call" prefix):
  the task says "Improve RSM rendering" without restricting to one format, and the
  two renderers must stay mutually consistent (same data, same label semantics).
  The book's own RSM figure (`fig_leftrec-rsm.tex`) labels call edges with the bare
  nonterminal/start-state name and numbers states plainly — the change moves code
  closer to the book.
- **Item 3 applies to `RsmTikz`** (the "double circle" is a TikZ node option;
  `RsmDot.extendedRsmToDot` already adds `peripheries=2` independently of the
  highlight, so DOT is already correct).
- **The block-identity label stays**: every block's start state keeps its plain
  nonterminal label to the left (`label=left:Nt` in TikZ; cluster label in DOT).
  Item 2 targets the *state content* (`N_i` → `i`); without the left label the
  stacked blocks would be unidentifiable (the book labels each block frame).
- **Styling rule after the change** (TikZ): `double, double distance=1.5pt` is
  emitted for *every* final state, regardless of highlight/start status; fill
  priority is lightblue (highlighted) > green (block start) > red (final).

## Reuse analysis (reusing skill)

- No new modules or files in `src/`; all changes are in-place edits of
  `RsmTikz.fs` / `RsmDot.fs`.
- New tests reuse: `LanguageRegistry` grammars, `GLL.buildPathIndexWithSteps`,
  `GllStepVisualizer.renderSteps` (same pattern as `GssDotVisualizationTests.fs`),
  existing regex helpers style of `RsmTikzTests.fs`.
- Docs: `docs/developer/rsm-viz.md` is the single canonical doc for both renderers —
  only it is updated (no new module → no hub/architecture updates per the mapping
  table).

## Subtasks

### S1: RsmTikz — numeric state labels, bare nonterminal edge labels, double circle for all final states [done — d490f97]

**Code:** `src/FLPQ.Printers/RsmTikz.fs`
**Tests:** `tests/FLPQ.Printers.Tests/RsmTikzTests.fs` — new facts pinning the
new semantics (see Spec); existing facts keep passing.
**Docs:** none here (doc update in S4).

**Spec:**

- State content: `as={<globalIdx>}` — bare number, no nonterminal prefix, no S′
  prime form. Remove the now-unused `isFreshStart` local (its only use was the
  prime-form label); `freshStart` stays (drives block order).
- Nonterminal edge label: `escapeLatex (nonterminalPrinter nt)` — drop the
  `"call "` prefix. Terminal and epsilon labels unchanged.
- Node options restructured so `double, double distance=1.5pt` is emitted for
  every final state independent of highlight/start; option order per node:
  `as={i}`, \[`label=above:Start` if start and not highlighted\],
  \[`label=left:Nt` if start\], \[`double, double distance=1.5pt` if final\],
  \[fill: lightblue!20 if highlighted, else green!30 if start, else red!30 if
  final\].
- Update the module doc comment and `extendedRsmToTikz` doc comment to describe
  the new label/styling semantics.
- Tests (new private helpers + facts in `RsmTikzTests.fs`):
  - node-declaration regex capturing the option list (`s(\d+) \[([^\]]*)\];`);
    `doubleCircledCount` counting declarations whose options contain a `double`
    option (word-boundary match, so `double distance` is not double-counted).
  - fact: state content is the bare global index — every declaration matches
    `s<i> [as={i}` and no declaration contains an escaped nonterminal prefix.
  - fact: nonterminal edges carry the bare name — no `call ` substring in edge
    lines; a known call edge (S′ start → S′ final, labeled with the original
    start block's name) is present.
  - fact `RSM tikz highlighted final state is double circled with lightblue fill`:
    for the ANBN classic extended RSM, render with `highlightedState = Some f`
    for a final state `f`; assert `f`'s declaration line contains both `double`
    and `fill=lightblue!20`, and total double-circled count equals
    `Set.count ersm.ExtendedRsm.FinalStates`.

### S2: RsmDot — same label semantics for consistency [done — c1aea1f]

**Code:** `src/FLPQ.Printers/RsmDot.fs`
**Tests:** new file `tests/FLPQ.Printers.Tests/RsmDotTests.fs` (registered in
`FLPQ.Printers.Tests.fsproj`) — RsmDot had no direct tests; facts cover the
changed label semantics for both entry points.
**Docs:** none here (doc update in S4).

**Spec:**

- `blockToSubgraph`: nonterminal edge label drops the `"call "` prefix.
- `extendedRsmToDot`: node label becomes the bare global index (`label="<i>"`);
  remove the now-unused `ntName` destructuring; keep `isFreshStart` (still drives
  `fontcolor=blue`) and the existing independent `peripheries=2` for finals.
- Tests: facts asserting (a) `extendedRsmToDot` node labels are bare indices,
  (b) no `call ` prefix in any edge label of both `toDot` and
  `extendedRsmToDot`, (c) a highlighted final state keeps `peripheries=2`
  together with `fillcolor=lightblue`.

### S3: Tests — per-step GLL RSM TikZ double-circle invariant [done — d497280]

**Code:** none.
**Tests:** `tests/FLPQ.Printers.Tests/RsmTikzTests.fs`
**Docs:** none.

**Spec:**

- New private helper: a GLL step renderer returning `(ersm, RsmTikz per step)`
  via `GllStepVisualizer.renderSteps` (pattern of
  `GssDotVisualizationTests.renderGssDots`).
- New fact `every GLL step RSM tikz has exactly the final states doubly circled`:
  for several registry grammars with accepted inputs (ANBN classic `a a b b`,
  DoubleA singleRule `a a`, Dyck1 ebnfStar `a b a b`, APlus rightRecursive
  `a a`), render all GLL steps and assert for every step that the double-circled
  count equals `Set.count ersm.ExtendedRsm.FinalStates` (hence identical across
  steps). The ANBN/DoubleA inputs guarantee some step highlights a final state,
  exercising the S1 fix.

### S4: Docs — rsm-viz.md label/styling semantics [done — f9e53a4]

**Code:** none.
**Tests:** none (mdformat-clean required).
**Docs:** `docs/developer/rsm-viz.md`

**Spec:**

- Overview: "A `call N` transition" → describe the edge as labeled by the bare
  nonterminal name; S′ block edge labeled with the original start's name.
- Tikz **Labels** bullet: state content is the bare global index; start states
  keep `label=left:Nt`; `double, double distance=1.5pt` on every final state
  (including highlighted ones); fill priority lightblue > green > red.
- Tikz **Edge labels** bullet: nonterminal calls as the bare `Nt` name.
- DOT bullets: extended RSM node labels are bare global indices; call edges bare
  names.
- Design Decisions table: add/adjust a row for the numeric-label + always-double
  finals rule (book alignment: states numbered, call edges labeled by the called
  block's name).
