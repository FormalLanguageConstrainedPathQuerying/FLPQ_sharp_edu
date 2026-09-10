# Task 265: Improve TikZ visualization of RSM — components top-to-bottom

## Task description (verbatim)

Improve TikZ visualization of RSM in GLL and RNGLR. Layout components (blocks) top-to-bottom, preserving left-to-right layout inside each component.
1. Rework `RsmTikz.extendedRsmToTikz`: use pgf-gd connected-component packing (`components go down left aligned`) so blocks stack vertically; intra-block layout unchanged (layered layout, grow'=right). Start block S' on top, remaining blocks in global-state appearance order. Plain nonterminal label to the left of each block's start state. No inter-block edges exist in the RSM data structure (verified) — if any are detected during implementation, report it.
2. Add full extended RSM TikZ figure at head of summary for RNGLR and for GLL TikZ mode (currently missing there). No RNGLR step layout changes.
3. Tests: compilation tests (with/without highlighted state), structural assertions, RNGLR runner file-existence test, summary compilation tests covering the new head section.

## Context and analysis

### Verified facts

1. **No inter-block edges exist in the flat RSM transition matrix.**
   `RsmBuilder.buildRSMWithStart` (`src/FLPQ.Languages/EbnfParser.fs:383-386`) copies each
   block's DFA transitions only within its own offset range
   (`transitions.[offset + localState, offset + localTarget]`).
   `RSM.extendWithStart` (`src/FLPQ.Languages/RSM.fs:237`) adds only the intra-S′ edge
   `(oldCount → oldCount+1)`. A `call N` transition stays inside the *caller's* block:
   its target is the continuation state; the jump to N's start state is implicit
   (algorithms resolve it via `BlockStart[N]`, e.g. `Gll.fs:84`).
   **Consequence:** each block is exactly one connected component of the flat graph
   (derivative DFAs contain only states reachable from the block start).

2. **The current left-to-right component arrangement is pgf-gd's default
   connected-component packing** of the single flat `\graph [layered layout]` in
   `RsmTikz.extendedRsmToTikz` (src/FLPQ.Printers/RsmTikz.fs:17). PGF manual §28.7:
   components are laid out individually, sorted by `component order`
   (default: *by first specified node*), and placed relative to each other along
   `component direction` (default 0 = right) with `component sep` padding.

3. **Solution:** change the packing direction only —
   `components go down left aligned` stacks components top-to-bottom with left edges
   aligned; internal growth (`grow'=right`) is untouched, so the layout *inside* each
   component is computed by exactly today's layered algorithm. No per-block subgraphs,
   no manual coordinates, no height estimation, no template changes (no `fit` library).

### Scope decisions (user-confirmed)

| Decision | Choice |
|----------|--------|
| RNGLR scope | Full extended-RSM TikZ at head of summary only; RNGLR step layout untouched |
| Block delimiting | Plain nonterminal label left of each block's start state; no frame |
| Inter-block edges | None exist (verified); nothing to draw or remove |
| Block order | S′ (fresh start) on top; remaining blocks in global-state appearance order |

### Affected artifacts

- `ext_rsm.tikz.tex` (GLL runner, TikZ mode) — new layout automatically.
- Per-step `rsm.tikz.tex` (GLL steps, highlighted current state) — new layout automatically.
- RNGLR: new `ext_rsm.tikz.tex` artifact + "Extended RSM" section at summary head.
- GLL summary head in TikZ mode currently has **no** RSM figure (`Summary.fs:111` looks
  for `ext_rsm.dot`, absent in TikZ mode) — filled by the same new section.
- DOT renderings (`RsmDot`) unchanged. No golden files affected (no RSM TikZ goldens
  exist; templates unchanged).

## Subtasks

### S1: Rework RsmTikz layout to top-to-bottom component stacking — [done, 7bcd147]

Spike results (pre-implementation, lualatex coordinate extraction): `components go down
left aligned` stacks components top-to-bottom in declaration order with left edges
aligned; intra-block layered LTR layout unchanged. Block start states land in layer 0 of
their component even for cyclic blocks (`S -> (a S b)*` has a cycle through the start
state) — the mid-row label fallback was not needed. `lightblue` is not a default xcolor
name: `data/tex_tikz_template.tex` gains `\usepackage{xcolor}` + the same
`\definecolor{lightblue}{rgb}{0.68,0.85,0.90}` as `tex_summary_template.tex` so the
highlighted-state compilation test passes standalone (no golden embeds the tikz template).

**Code:** `src/FLPQ.Printers/RsmTikz.fs` — rework `extendedRsmToTikz`:
- Graph options gain `components go down left aligned, component sep=1.5cm`
  (existing `layered layout, nodes={draw, circle}, grow'=right, level sep=2cm,
  sibling sep=1.5cm` unchanged).
- Node declaration order: S′ block first (its start state declared first within the
  block), then remaining blocks in global-state appearance order; within each non-start
  block the start state is also declared first, other states in local-index order.
  This drives `component order=by first specified node` (pgf-gd default) → S′ on top.
- Each block's start state gains `label=left:<escaped Nt>` (text mode, same escaping as
  node content via `AutomatonTikz.escapeLatex`). On the overall start state this
  coexists with the existing `label=above:Start`.
- Edge emission unchanged (all edges are intra-block — verified).

**Tests:** `tests/FLPQ.Printers.Tests/TexCompilationTests.fs`:
- Existing ``RSM tikz compiles with lualatex`` must pass with the new options.
- New ``RSM tikz with highlighted state compiles with lualatex`` (highlightedState = Some).
- New structural facts in a `RsmTikzTests` section (new file or existing printer test
  file): output contains `components go down left aligned`; exactly one `\graph`;
  node declaration count = `StateCount`; S′ block nodes declared before all others;
  every block's start state carries `label=left:`; edge line count equals the number of
  (i, j, label) triples in the transition matrix.

**Docs:** new `docs/developer/rsm-viz.md` (RsmDot + RsmTikz: formats, component-packing
approach with PGF manual §28.7 reference, book refs sec:CFPQ_GLL / sec:CFPQ_RNGLR);
add module row to `docs/developer/FLPQ.Printers.md` table; fix dangling `rsm-dot.md`
link in `docs/developer/InputGraphDot.md` (points to nonexistent file) → `rsm-viz.md`.

**Spec:**
- Spike first (no commit): hand-write a minimal standalone TikZ with two disconnected
  layered components + `components go down left aligned`, compile with lualatex, confirm
  vertical stacking and left alignment. Then render ANBN "classic" and a Dyck grammar
  RSM to PDF and inspect: stacking direction, label placement on cyclic blocks (start
  state may not sit in layer 0 — if `label=left` lands mid-row, label the component's
  first-declared node instead), spacing. Tune `component sep` if needed (default 1.5em
  is tighter than sibling sep=1.5cm; 1.5cm proposed for visual consistency).
- Node options per state keep current semantics: highlighted → `fill=lightblue!20`;
  overall start → `label=above:Start, fill=green!30`; final → `double, double
  distance=1.5pt, fill=red!30`; fresh-start block content keeps the `S'\_k` prime form.
- Block order source: `RSM.nonterminals` (global-state appearance order) with the
  extended RSM's `StartBlock` (S′) moved to the front.
- Label text: `nonterminalPrinter nt` escaped with `AutomatonTikz.escapeLatex`.

### S2: Extended RSM at head of summary (RNGLR + GLL TikZ mode) — [done]

**Code:**
- `src/FLPQ.Cli/RnglrRunner.fs`: in TikZ mode (`not useDot`) write
  `ext_rsm.tikz.tex` via `RsmTikz.extendedRsmToTikz string string extRsm None`
  (mirrors `GllRunner.fs:52-54`). DOT mode unchanged.
- `src/FLPQ.Printers/SummaryTeX.fs` `headerSection`: in the GLL and RNGLR branches,
  when `useTikz`, read `ext_rsm.tikz.tex` from vizDir (same `readIfExists` pattern as
  the existing `input.tikz.tex` read at SummaryTeX.fs:186) and emit an
  "Extended RSM" section via `wrapTikzAdjustbox` (adjustbox is already loaded by
  `tex_summary_template.tex`; it never upscales, so tall stacked figures keep natural
  size). Section placed where the RSM figure belongs in the head (next to the existing
  `rsmSppfPdfs` lines). DOT mode keeps current behavior (GLL: `ext_rsm.pdf`,
  RNGLR: `rsm_blocks.pdf`). No `Summary.fs` signature changes.

**Tests:**
- `tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs`: new fact — RNGLR TikZ-mode run produces
  `ext_rsm.tikz.tex` (mirrors `GllRunnerTests.fs:296`).
- `tests/FLPQ.Printers.Tests/TexCompilationTests.fs`: extend ``GLL merged summary TeX
  with tikz compiles with lualatex`` and ``RNGLR merged summary TeX with tikz compiles
  with lualatex`` to write `ext_rsm.tikz.tex` into the temp viz dir so the new head
  section is exercised end-to-end.

**Docs:** `docs/user/cli.md` — RNGLR output table gains `ext_rsm.tikz.tex` (TikZ mode);
`docs/developer/FLPQ.Cli.md` — note the summary head now includes the extended RSM
figure in TikZ mode for GLL and RNGLR.

**Spec:**
- The section must be skipped gracefully when `ext_rsm.tikz.tex` is absent
  (`readIfExists` → None → no lines), so older viz dirs still build.
- No changes to `RnglrStepVisualizer`, step templates, or per-step artifacts.

Notes from implementation:
- `docs/user/cli.md` RNGLR row listed `ext_rsm.dot`, which RnglrRunner never
  writes (any mode) — replaced with `ext_rsm.tikz.tex` (default Tikz mode). The
  GLL row gained the same mode annotation for its ext_rsm artifact.
- Summary compilation tests assert `components go down left aligned` in the
  built content so the new head section is verified, not just compiled.

## Execution order

S1 → S2 (S2 consumes S1's renderer; independent files otherwise).
