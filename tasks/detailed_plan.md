# Task 262: TikZ rendering for LL and LR steps visualization (tree_and_stack)

## Task description (verbatim)

Add Tikz rendering for LL and LR steps visualization (tree_and_stack). Use graphdrawing layered
layout with `{ [same layer] ... }` to preserve the same-level stack frontier layout (equivalent of
DOT `{rank=same}`); dashed chain edges stay drawn but non-constraining. TikZ by default,
`--use-dot` switches per-step output to DOT (LL runner gains the flag). Update summary generation
for both modes. Add golden tests, TeX compilation tests, and a same-layer coordinate test
comparing against the DOT layout baseline.

## Context and analysis

### Current state

- `DerivationTreeDot.fs` renders the combined stack-tree as DOT: full partial derivation tree +
  dashed `constraint=false` chain between stack frontier nodes + `{rank=same; ...}` group
  (LL: `toDotWithLLStack`, LR: `toDotWithLRStack`).
- Per-step output is always `step_N/tree_and_stack.dot` (`Helpers.writeStepsVisualization`).
- The summary includes each step's DOT-compiled PDF
  (`SummaryTeX.stackStepSection` → `includePdf "dot_pdfs/<step>_tree_and_stack.pdf"`).
- LL runner has no `useDot` flag; LR runner's `useDot` only switches the automaton rendering.

### How the same-level layout is preserved in TikZ (verified empirically)

The graphdrawing `layered layout` (Sugiyama method — same pseudo-code as Graphviz dot) has a
native **`same layer` collection**: `{ [same layer] n1, n2, ... };` inside the `\graph` body.
Verified in the installed package source (`pgf/gd/layered/Sugiyama.lua`):

- Nodes of a `same layer` cluster are contracted into one node before ranking; edges between two
  members of the same cluster become self-loops and are removed before ranking (== DOT
  `constraint=false`), then restored for drawing. So the dashed chain is drawn but does not
  constrain layout.
- Longest-path ranking places the frontier cluster at depth = max path length to any member —
  the common row, exactly as DOT does.

Empirical verification (lualatex + `\pgfpointanchor{n}{center}\typeout{...\the\pgf@x \the\pgf@y}`
coordinate dumps, compared against `dot -Tplain` baselines from the golden files):

| Case | Result |
|------|--------|
| LL step 3 (frontier at mixed depths) | frontier nodes share one y; x-order = natural tree order — matches DOT exactly |
| LR step 3 (5 frames, one subtree) | all frames share one y; **chain edges must be emitted in reverse stack order** (bottom→top) for frames to appear left-to-right in stack order; with DOT's direction the crossing-minimization scrambles the order |
| LL with reversed chain | breaks tree ordering — LL must keep DOT's chain direction |
| single node + `{ [same layer] n }` | compiles fine (step 0 case) |
| multiple subtrees of different depths | frames keep stack order; subtrees hang below their frame |

Additional findings:

- Default layered-layout orientation is already root-on-top (no `grow` option needed).
- `ellipse` shape requires `\usetikzlibrary{..., shapes.geometric}` and must be written as
  `shape=ellipse` (bare `ellipse` is not a pgfkey). Both TeX templates need the library added.
- This PGF build (3.1.10) has no `\pgfgetx`; coordinate extraction uses
  `\makeatletter\pgfpointanchor{n}{center}\typeout{... x=\the\pgf@x y=\the\pgf@y}\makeatother`.

### Reuse checklist results

- **Reused**: `AutomatonTikz.tikzFooter` (identical `\end{tikzpicture}` footer);
  `SummaryTeX.wrapTikzCenter` (resizebox wrapper for summary embedding);
  `ExternalTools.compileTexStringWithTemplate` pattern; `GoldenHelpers.verifyGolden`;
  `SymbolTeX.toLaTeX` labels passed through unescaped (math mode, like `LRAutomatonTikz`).
- **Generalized (Q5)**: `GoldenHelpers.combineStepsDot` → shared `combineSteps` +
  `combineStepsDot`/`combineStepsTikz`; `ExternalTools.compileTexStringWithTemplate` gains a
  log-returning variant `compileTexStringWithTemplateLog` (old function delegates to it).
- **New**: `DerivationTreeTikz.fs` module (no existing TikZ renderer for derivation trees);
  local `tikzHeader` in the new module (graph options differ from `AutomatonTikz.tikzHeader`:
  no `grow'=right`, `nodes={draw, shape=ellipse}`) — keeps the shared header's 5 call sites
  untouched.

### Rendering conventions (1:1 with DOT)

- Node IDs: same pre-order counter as DOT (`n1, n2, ...`) — goldens correspond 1:1.
- Leaves: `rectangle`; nonterminals: default ellipse (global `nodes={draw, shape=ellipse}`);
  LR state frames: `rectangle, fill=lightgray`, label `sN` (plain text, as in DOT).
- Graph options: `[layered layout, nodes={draw, shape=ellipse}, level sep=2cm, sibling sep=1.5cm]`.
- LL chain edges: same order as DOT (`stackIds.[i] -> stackIds.[i+1]`, dashed).
- LR chain edges: **reversed** stack order (last frame → first frame, dashed) — required for
  correct left-to-right frame ordering (see empirical table above). Documented in module docs.
- `{ [same layer] ... };` emitted whenever the frontier is non-empty (mirrors DOT, which always
  emits it; single-node clusters verified to compile).

## Subtasks

### S1: DerivationTreeTikz renderer + template library fix [done — 59b0fa5]

**Code:** New `src/FLPQ.Printers/DerivationTreeTikz.fs`:
- `toTikzWithLLStack: (Symbol<'t,'nt> -> string) -> DerivationTree<'t,'nt> -> LLStackLeaf<'t,'nt> list -> string`
  — pre-order traversal with path→id map (mirrors `DerivationTreeDot.toDotWithLLStack`); node
  declarations (`as={<label>}`, leaves `rectangle`), tree edges, frontier located by
  `LLStackLeaf.Path`, dashed chain in DOT order, `{ [same layer] ... };`.
- `toTikzWithLRStack: (Symbol<'t,'nt> -> string) -> LRStackFrame<'t,'nt> list -> string`
  — frames in stack order; `LRSymbol` → subtree, `LRState s` → gray rectangle `sN`; dashed chain
  in **reversed** stack order; `{ [same layer] ... };`.
- Private `tikzHeader`/reuse of `AutomatonTikz.tikzFooter`.
- Modify `data/tex_tikz_template.tex` and `data/tex_summary_template.tex`: add `shapes.geometric`
  to `\usetikzlibrary{...}`.
- `src/FLPQ.Printers/ExternalTools.fs`: add `compileTexStringWithTemplateLog: string -> string ->
  bool * string` (returns lualatex stdout for coordinate extraction); `compileTexStringWithTemplate`
  delegates to it. Pulled forward from S5 — required by S1's empirical verification gate.

**Tests:** New `tests/FLPQ.Printers.Tests/DerivationTreeTikzTests.fs`:
- Structural `[<Fact>]`: LL step output contains `\begin{tikzpicture}`, `{ [same layer]`,
  dashed chain edges, correct node count; LR step output contains gray state boxes and same-layer
  statement.
- TeX-category `[<Trait("Category", "TeX")>]`: compile the LL grammar1 "a b" step-3 TikZ and the
  LR grammar3 "aa" step-3 TikZ via `compileTexStringWithTemplate` with
  `data/tex_tikz_template.tex` (the two canonical mixed-depth cases).

**Docs:** Extend `docs/developer/derivation-tree-viz.md`: TikZ rendering section — same-layer
mechanism, chain-direction decision (LL = DOT order, LR = reversed, with rationale), node styling
table, template library requirement.

**Spec:**
- Output is a complete `\begin{tikzpicture}...\end{tikzpicture}` block (same contract as
  `AutomatonTikz`/`GssTikz`).
- Labels from the symbol visualizer are inserted verbatim inside `as={...}` (already LaTeX math).
- Empty frontier → no chain edges, no same-layer statement (plain tree).
- Node ID assignment must match `DerivationTreeDot` exactly (same traversal order) so DOT and
  TikZ goldens reference the same node numbers.

### S2: VisualizationStep.TreeAndStackTikz + visualizer wiring [done — 27c7432]

**Code:**
- `src/FLPQ.Printers/VisualizationTypes.fs`: add `TreeAndStackTikz: string` to
  `VisualizationStep`; update doc comment.
- `LLStepVisualizer.renderStep`: populate via `DerivationTreeTikz.toTikzWithLLStack`.
- `LRStepVisualizer.renderStep`: populate via `DerivationTreeTikz.toTikzWithLRStack`.

**Tests:** Extend `LLVisualizerTests`/`LRVisualizerTests`: every step's `TreeAndStackTikz`
contains `\begin{tikzpicture}` and (for steps with a non-empty frontier) `{ [same layer]`.
Existing DOT golden tests must pass unchanged.

**Docs:** Update `docs/developer/visualization-types.md` (field list).

**Spec:**
- Both fields always populated (GLL/RNGLR pattern: writer picks by mode).
- No change to DOT output (goldens stable).

### S3: CLI wiring — useDot for LL/LR step output [done — b523580]

**Code:**
- `src/FLPQ.Cli/Helpers.fs`: `writeStepsVisualization` gains `useDot: bool`; writes
  `tree_and_stack.tikz.tex` (default) or `tree_and_stack.dot` (`--use-dot`) + `input.tex`.
- `src/FLPQ.Cli/LLRunner.fs`: `runLL` gains `useDot: bool`, passes to writer.
- `src/FLPQ.Cli/LRRunner.fs`: pass existing `useDot` to `writeStepsVisualization`.
- `src/FLPQ.Cli/Program.fs`: pass `useDot` to `runLL`.

**Tests:** Update `tests/FLPQ.Cli.Tests/LLRunnerTests.fs` / `LRRunnerTests.fs`: default mode
produces `tree_and_stack.tikz.tex`; `useDot=true` produces `tree_and_stack.dot`; no file of the
other mode is written.

**Docs:** Update `docs/user/cli.md`: LL/LR output-file tables and `--use-dot` row (now also
switches per-step rendering for LL and LR).

**Spec:**
- Default (no flag): TikZ files. `--use-dot`: DOT files. One or the other, never both
  (GLL/RNGLR pattern).
- `input.tex` is written in both modes (unchanged).

### S4: Summary integration for both modes [done — 905158f]

**Code:**
- `src/FLPQ.Printers/SummaryTeX.fs`: `stackStepSection` gains `useTikz: bool`; when true, read
  `step_N/tree_and_stack.tikz.tex` and emit `wrapTikzCenter tikz` instead of the dot-PDF include.
  Update the call site in `buildContent`.
- `src/FLPQ.Cli/Summary.fs`: no change expected (dot artifacts are compiled only when present;
  TikZ mode simply has no step `.dot` files) — verify and note.

**Tests:** Extend `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` (or `SummaryTests.fs`): LL and LR
summaries in default (TikZ) mode compile via lualatex and the merged TeX contains the inline
tikzpicture (`\graph [layered layout`) for each step; DOT-mode summary still includes the PDF.

**Docs:** Update `docs/developer/summary-tex.md` (stack-step section behavior per mode).

**Spec:**
- TikZ mode: each step section = header + `wrapTikzCenter` of the step's tikzpicture + input row.
- DOT mode: unchanged (PDF include).
- Missing tikz file in a step → skip the picture line (same tolerance as GLL `readIfExists`).

### S5: Golden tests, full-step TeX compilation, same-layer coordinate test [done — 74a7e41]

**Code:**
- `tests/FLPQ.Printers.Tests/GoldenHelpers.fs`: extract shared
  `combineSteps (VisualizationStep -> string) -> VisualizationStep list -> string`;
  `combineStepsDot` delegates; add `combineStepsTikz`.
- `src/FLPQ.Printers/ExternalTools.fs`: `compileTexStringWithTemplateLog` was pulled forward
  into S1 (required by S1's empirical verification gate); only the golden-helper change remains here.

**Tests:**
- Golden: `LLStepsGoldenTests.fs` + `LRStepsGoldenTests.fs` gain TikZ variants for
  grammar1 "a b", grammar1 "aababb" (LL) and grammar3 "aa", grammar7 "xplusx" (LR); golden files
  `ll_grammar1_ab.tikz`, `ll_grammar1_aababb.tikz`, `lr_grammar3_aa.tikz`,
  `lr_grammar7_xplusx.tikz` created via `CREATE_GOLDEN_FILES=1` and reviewed.
- TeX-category: every step of the four cases compiles via
  `compileTexStringWithTemplate` (in `TexCompilationTests.fs`).
- Same-layer coordinate test (TeX category, new section in `DerivationTreeTikzTests.fs`):
  for LL grammar1 "a b" and LR grammar3 "aa": render each step's TikZ; extract the same-layer
  node list from the generated source; inject
  `\makeatletter\pgfpointanchor{n}{center}\typeout{COORD n x=\the\pgf@x y=\the\pgf@y}\makeatother`
  lines before `\end{tikzpicture}`; compile with `compileTexStringWithTemplateLog`; parse the
  `COORD` lines; assert (a) all same-layer nodes share one y value, (b) LL: x-order of frontier
  nodes equals pre-order tree order of their paths, (c) LR: x-order equals stack order
  (top→bottom = left→right).

**Docs:** Update `docs/developer/guides/test-categories.md` if the affected-test lists change
(`DerivationTreeTikzTests` joins the TeX category list).

**Spec:**
- Coordinate parsing: `\the\pgf@x` prints `<float>pt`; regex per line; tolerance 1e-6 pt for
  y-equality.
- The coordinate test must fail if a same-layer node drifts to another level (regression guard
  for the core layout feature).

## Execution order and dependencies

S1 → S2 → S3 → S4 → S5 (each builds on the previous; all independently committable).
