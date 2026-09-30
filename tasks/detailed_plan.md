# Detailed Plan: Task 285 — Improve Belyanin RPQ steps layout and rendering

## Task description (verbatim)

```
285. Improve Belynin RPQ steps layout and rendering. 0. Highlight start vertex in graph with green (same as initial state in automata) 1. Use different colors for initial and target vertices highlighting on each step. I propose to use light blue (same as for automata frontier) for initial vertices (they all part of frontier) and light yellow for target (used now). Highlight transitions in automata too (not paths like in graph, only current transition edge). 2. Step layout: for initial step (step 0) one line of three blocks (minipages). First one is curreny frontier (F) and current visited (V). Second is automata with highlighted current states. Third is graph with highlighted current vertices and previously discovered parts of path. For other steps: three blocks of lines. First line exactly the same as for initial step (represents F,V automata and graph at the start of step). Second block represents multiplications (transitions): for each active 'a' symbol two lines: transition in automata, transition in graph. Each line consists of two blocks: math and figure. First line for symbol: (N^a)^T \\otimes F = \<representation_of_N^a^T> \\otimes \<representation_of_F> = <representation of result> (exactly part of currently rendered formula), and figure with highlighted transitions in automata. Second line: the same for graph (with explicitly represented full formula, not partial '\\times G^a' like now. Note that we use \\otimes for multiplication). Third block: simialr to the first one: new frontier, new visited, and rendered automata and graph with partial paths in graph and highlighted current reached vertices in both graph and automata (in correspondance with new frontier).
     **[USER GUIDANCE]**: Name the per-label intermediate result F^a (no prime): first per-label line is '(N^a)^T \\otimes F = \<N^a^T> \\otimes <F> = \<F^a>', second line is 'F^a \\otimes G^a = \<F^a> \\otimes \<G^a> = <Extend>' (all \\otimes, no \\times). Green start-vertex highlight goes to both the root Input Graph header artifact (shared with Arroyuelo RPQ) and every step's graph figures.
```

## Scope and constraints

Rendering-only task: `BelyaninStepVisualizer`, the shared renderers it uses
(`GssTikz`/`GssDot`, `AutomatonTikz`/`AutomatonDot`, `RpqGraphViz`), the step
templates, `Helpers.writeBelyaninStepsVisualization`, and
`SummaryTeX.belyaninStepSection` + legend. The algorithm
(`BelyaninRPQ.evaluateWithTrace`) is **untouched** — every new highlight set is
derived from the existing trace (`M`, `P`, per-label `N`/`G`/`Select`/`Extend`,
`NewM`).

Book reference: Chapter 11, `02_BFS.tex`, algorithm `algo:RPQ_BFS_semiring` —
per-label term `(N^a)^T ⊗̄ M ⊗ G^a`; the visualization splits it into two
explicit products per label (task 284 established the first half; this task
renames its result to F^a and makes the second half a full formula).

### Design decisions (confirmed with the user)

1. **Colors** (TikZ / DOT):
   - Graph start vertex (source, `graph.StartStates`): green — `fill=green!30` /
     `fillcolor=green`, exactly the automaton initial-state color in each format.
     Applied to the root Input Graph header artifact (shared with Arroyuelo) and
     to every Belyanin step graph figure.
   - Frontier/"initial" vertices `{v : F[q,v]≠∅}`: light blue — `fill=lightblue!20` /
     `fillcolor=lightblue`, same as the automaton frontier highlight.
   - Target vertices `{v : Extend_a[*,v]≠∅}` (newly reached): light yellow — existing
     `highlightedVertices` tier (`fill=yellow!20` / `fillcolor=lightyellow`), color unchanged.
   - Current transition edges: red bold — existing edge tiers (`red, thick` /
     `color=red, penwidth=2.0`). In automaton figures: only the *used* a-transitions;
     in per-label graph figures: only the *followed* a-edges.
   - Partial path edges: light red — existing `pathEdges` tier (`red!40` / `#FF9999`).
2. **Vertex fill precedence** (new tiers inserted into the existing chain):
   start (green) > current (lightblue, existing single vertex) ≥ frontier (lightblue,
   new set) > storedPop (orange) > highlighted (yellow) > plain.
3. **Step layout**:
   - Step 0: one line of three minipages `[F, V] | automaton | graph`.
   - Step k>0: three lines — line 1 identical to the step-0 line but at the *start*
     of the step (V = visited before the step); line 2 = per-label transition rows;
     line 3 = `[New F, V] | automaton | graph` at the end of the step.
   - Line 1/line 3 graphs: green start vertex, lightblue frontier vertices, lightred
     path edges (line 1: edges of F's paths; line 3: edges of New F's paths); no red
     edges, no yellow vertices.
   - Per-label rows (trace order), each two lines of `[math | figure]`:
     - `(N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]` | automaton with used a-transitions red bold.
       Used transition `q→q′` ⟺ `N^a[q,q′] ∧ F[q,*]≠∅`.
     - `F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]` | graph with followed a-edges red bold,
       their from-endpoints lightblue, targets `{v : Extend_a[*,v]≠∅}` lightyellow,
       F's path edges lightred. Followed edge `u→v` ⟺ `G^a[u,v] ∧ F^a[*,u]≠∅`.
4. **V at the start of a step** = visited before the step = `P \ F` (cell-wise set
   difference). Exact inverse of the trace's `P ← P ⊕ F` (the trace stores P after the
   add), so no trace change is needed. V at the end of the step = the stored `P`.
5. **F^a naming** (user guidance): the per-label intermediate result (Select) is named
   `F^a` — no prime, per-label superscript. All multiplication signs are `\otimes`
   (the old `\times G^a` fragment is gone).
6. **DOT mode** is fully supported (same layout, dot-compiled PDFs).
7. Arroyuelo step graphs are unchanged (only the shared root Input Graph gains green
   sources). GLL/RNGLR renderers get the new parameters with `Set.empty` at their call
   sites — output byte-identical, goldens must stay green.

### Step directory layout (TikZ mode; DOT: `.dot` instead of `.tikz.tex`)

```
step_k/
  matrices_start.tex            $F$ + matrix(M), $V$ + matrix(P \ M)
  automaton_start.tikz.tex      DFA, frontier states(M) lightblue
  graph_start.tikz.tex          green start, lightblue {v: M[*,v]≠∅}, lightred pathEdges(M)
  label_i_select.tex            (N^a)^T ⊗ F = [N^a^T] ⊗ [F] = [F^a]
  label_i_extend.tex            F^a ⊗ G^a = [F^a] ⊗ [G^a] = [Extend]
  label_i_automaton.tikz.tex    DFA, used a-transitions red bold (no state highlights)
  label_i_graph.tikz.tex        green start, red followed edges, lightblue from-endpoints,
                                lightyellow targets, lightred pathEdges(M)
  matrices_end.tex              $New F$ + matrix(NewM), $V$ + matrix(P)   [k>0]
  automaton_end.tikz.tex        DFA, frontier states(NewM) lightblue      [k>0]
  graph_end.tikz.tex            green start, lightblue {v: NewM[*,v]≠∅}, lightred pathEdges(NewM) [k>0]
```

Step 0 has only the first three files. Label index `i` = position in the trace's
`Labels` list (Map iteration order over the boolean decomposition).

### Templates (6 files replace the current 2)

- `data/Belyanin_step_(tikz_)template.tex` — line-1 minipages
  (`__MATRICES_START__`, `__AUTOMATON_START_TIKZ__|PDF__`, `__GRAPH_START_TIKZ__|PDF__`)
  - `__LABEL_ROWS__` + `__ROW_END__`. Init step: both placeholders empty.
- `data/Belyanin_row_end_(tikz_)template.tex` — line-3 minipages
  (`__MATRICES_END__`, `__AUTOMATON_END_TIKZ__|PDF__`, `__GRAPH_END_TIKZ__|PDF__`).
- `data/Belyanin_label_row_(tikz_)template.tex` — one per-label block: two lines of
  `[math | figure]` (`__SELECT_FORMULA__`, `__LABEL_AUTOMATON_TIKZ__|PDF__`,
  `__EXTEND_FORMULA__`, `__LABEL_GRAPH_TIKZ__|PDF__`).

Matrix minipages keep the `\begingroup \setlength{\textwidth}{\linewidth}` group so the
adjustbox-wrapped matrices shrink to the column; figures keep
`SummaryTeX.wrapTikzAdjustboxColumn`; the whole filled step keeps
`SummaryTeX.wrapStepAdjustbox`. Starting widths: state lines 0.34/0.33/0.33, label
lines 0.56/0.44 (verified by the no-Overfull TeX test).

## Subtasks

> **Restructure note (before S3):** the original S3/S4/S5 are inseparable for a green,
> compilable commit — the step-dir file layout, `Helpers.writeBelyaninStepsVisualization`,
> the six templates, and `SummaryTeX.belyaninStepSection` form one coupled pipeline: any
> change to one breaks the merged-summary tests until the others follow. They are executed
> as a **single atomic subtask S3** (one commit). The original S6 (documentation sweep)
> becomes S4.

### S1: Graph renderer vertex tiers (green start, lightblue frontier)

- [x] Done — commit c8b46fe

**Code:** `src/FLPQ.Printers/GssTikz.fs` — `toTikzFromSets` gains
`startVertices: Set<int>` and `frontierVertices: Set<int>` parameters (inserted after
`highlightedVertices`, grouping the vertex tiers); vertex fill precedence start
(`fill=green!30`) > current > frontier (`fill=lightblue!20`) > storedPop > highlighted.
`src/FLPQ.Printers/GssDot.fs` — same two parameters on `toDotFromSets`; start
`fillcolor=green`, frontier `fillcolor=lightblue`. `src/FLPQ.Printers/RpqGraphViz.fs` —
`renderGraph` gains the two parameters (passed through) and a new public helper
`frontierVertices (m: Matrix<Set<int list>>) : Set<int>` = `{v : ∃q. m[q,v]≠∅}`.
Call sites updated: `GllStepVisualizer` (4), `RnglrStepVisualizer` (2),
`ArroyueloStepVisualizer` (1 via renderGraph + 2 direct Gss calls pass `Set.empty`),
`BelyaninStepVisualizer` (temporary `Set.empty` until S3),
`Helpers.writeRpqRootArtifacts` (root Input Graph: `startVertices = graph.StartStates`,
frontier empty — user guidance).

**Tests:** `GssDotVisualizationTests`/`GssTikz` tests: start vertex renders green,
frontier vertex lightblue, precedence (start beats frontier, frontier beats
highlighted), empty sets change nothing. `RpqGraphVizTests`: pass-through +
`frontierVertices` on a small path matrix. Existing GLL/RNGLR/Arroyuelo goldens must
stay byte-identical (run the full Printers suite).

**Docs:** `docs/developer/gss-dot.md`, `docs/developer/gss-tikz.md` (new parameters +
precedence), `docs/developer/rpq-graph-viz.md` (renderGraph signature, frontierVertices).

**Spec:**

- TikZ vertex options: start → `fill=green!30`; frontier → `fill=lightblue!20` (same
  value as the existing current-vertex tier); a vertex in several tiers renders the
  highest-precedence fill only.
- DOT: start → `style=filled, fillcolor=green`; frontier → `style=filled, fillcolor=lightblue`.
- `frontierVertices` is the graph-side counterpart of the visualizer's existing
  `frontierStates` (automaton side).

### S2: DFA renderer highlighted transition edges

- [x] Done — commit 0906b6c

**Code:** `src/FLPQ.Printers/AutomatonTikz.fs` — `dfaToTikzWithHighlights` gains a
trailing `highlightedEdges: Set<int*int>` parameter; an edge in the set renders
`s%d ->[\"label\", red, thick] s%d` (same style as GssTikz highlighted edges; label and
loop attributes preserved). `src/FLPQ.Printers/AutomatonDot.fs` — same parameter on
`dfaToDotWithHighlights`; edge renders `[label=\"...\", color=red, penwidth=2.0]`.
`dfaToTikz`/`dfaToDot` pass `Set.empty` (byte-identical output). Call sites:
`BelyaninStepVisualizer` (temporary `Set.empty` until S3), `Helpers.writeRpqRootArtifacts`
(`Set.empty`).

**Tests:** `AutomatonVisualizationTests`: exactly the given edges render red bold in
both formats; a highlighted edge keeps its label and loop attribute; empty-set
byte-identity with the plain renderers (update existing equivalence tests for the new
parameter).

**Docs:** `docs/developer/automaton-viz.md` (new parameter, style, precedence note:
edge highlight is independent of state fills).

**Spec:**

- Only terminal-labeled edges are candidates (epsilon edges are never highlighted —
  Belyanin's per-label matrices contain no epsilon transitions).
- Edge highlight does not interact with state fills; both can apply to the same edge's
  endpoints.

### S3: Belyanin step pipeline — new artifacts, templates, section builder (atomic: old S3+S4+S5)

- [x] Done — commit 1723482

**Code (visualizer):** `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — replace
`BelyaninVisualizationStep` with:

```fsharp
type BelyaninLabelVisual =
    { SelectFormula: string
      ExtendFormula: string
      AutomatonDot: string
      AutomatonTikz: string
      GraphDot: string
      GraphTikz: string }

type BelyaninVisualizationStep =
    { MatricesStart: string
      MatricesEnd: string
      AutomatonStartDot: string
      AutomatonStartTikz: string
      GraphStartDot: string
      GraphStartTikz: string
      Labels: BelyaninLabelVisual list
      AutomatonEndDot: string
      AutomatonEndTikz: string
      GraphEndDot: string
      GraphEndTikz: string }
```

`renderSteps` per trace step (init step: `MatricesEnd`/end figures = `""`,
`Labels = []`):

- `V_before = Matrix.map2 Set.difference step.P step.M`.
- `MatricesStart`: `$\text{F}$` + `matrixWithStateVertexLabels step.M`, then
  `$\text{V}$` + `matrixWithStateVertexLabels V_before`.
- `MatricesEnd`: `$\text{New } F =$` + matrix(step.NewM), then `$\text{V}$` +
  matrix(step.P).
- Start/end automaton: `dfaTo…WithHighlights (frontierStates M|NewM) Set.empty`.
- Start graph: `renderGraph (startVertices = graph.StartStates) (frontierVertices = RpqGraphViz.frontierVertices step.M) Set.empty (pathEdges step.M) Set.empty`; end graph analogous with `step.NewM`.
- Per label `ls` (trace order):
  - `usedAutoEdges = {(q,q') : ls.N.[q,q'] ∧ rowNonEmpty(step.M, q)}`; automaton figure
    = `dfaTo…WithHighlights Set.empty usedAutoEdges`.
  - `followedEdges = {(u,v) : ls.G.[u,v] ∧ colNonEmpty(ls.Select, u)}`;
    `targets = {v : colNonEmpty(ls.Extend, v)}`; graph figure = `renderGraph graph.StartStates (from-endpoints of followedEdges) targets (pathEdges step.M) followedEdges`.
  - `SelectFormula`: `$(N^{a})^T \otimes F =$` + `boolMatrixToTeX q q (transpose ls.N)`
    - `$\otimes$` + matrix(step.M) + `$=$` + matrix(ls.Select).
  - `ExtendFormula`: `$F^{a} \otimes G^{a} =$` + matrix(ls.Select) + `$\otimes$` +
    `boolMatrixToTeX v v ls.G` + `$=$` + matrix(ls.Extend).
- Private helpers: `rowNonEmpty`, `colNonEmpty` (over `Matrix<Set<int list>>`).

**Tests:** `BelyaninStepVisualizationTests` — regenerate goldens for every artifact
(`belyanin_step_{i}_matrices_start.tex`, `_matrices_end.tex`, `_automaton_start.tikz`,
`_graph_start.tikz`, `_label_{j}_select.tex`, `_label_{j}_extend.tex`,
`_label_{j}_automaton.tikz`, `_label_{j}_graph.tikz`, `_automaton_end.tikz`,
`_graph_end.tikz`) via `CREATE_GOLDEN_FILES=1`, then copy to
`tests/FLPQ.Printers.Tests/GoldenData/`. Structural facts (rewrite existing ones):
start/end automaton DOT highlight exactly the frontier states of M/NewM; per-label
automaton DOT highlights exactly the used a-transitions (red count + per-edge check);
per-label graph DOT: red bold = followed edges, lightblue = from-endpoints,
lightyellow = targets, lightred = pathEdges(M) minus followed; start/end graphs: green
start vertex present, lightblue = frontier vertices of M/NewM, no red edges; formula
files contain `$(N^{a})^T \otimes F =$` / `$F^{a} \otimes G^{a} =$` with exactly three
pNiceMatrix blocks each and no `\times`; matrices_start has 2 blocks (F, V) with V =
P\\M content, matrices_end has 2 blocks (New F, V).

**Docs:** `docs/developer/belyanin-step-viz.md` — rewrite Abstract, Data Structure,
Module Functions, Step Artifacts (new file layout + highlight sets per figure), Design
Decisions (F^a naming, V_before derivation, used/followed edge definitions, color
table).

**Spec:**

- All highlight sets derive from the trace only; no cross-step data.
- The init step's single line shows F = initial frontier, V = empty, automaton with the
  start state lightblue (frontier), graph with the green source(s) + lightblue source
  vertex(ices) + no path edges (trivial paths).
- A label whose Select is non-empty but whose Extend is entirely empty (I_simple drop,
  e.g. example step 4 label b) still renders both rows: followed edges red, targets
  empty.

**Code (templates + writer):** `data/Belyanin_step_template.tex`, `data/Belyanin_step_tikz_template.tex`
(replaced), `data/Belyanin_row_end_template.tex`, `data/Belyanin_row_end_tikz_template.tex`,
`data/Belyanin_label_row_template.tex`, `data/Belyanin_label_row_tikz_template.tex`
(new) per the template spec above. `src/FLPQ.Cli/Helpers.fs` —
`writeBelyaninStepsVisualization` writes the new file set (skips empty end artifacts);
new finders `findBelyaninRowEndTemplate(Tikz)`, `findBelyaninLabelRowTemplate(Tikz)`;
existing `findBelyaninStepTemplate(Tikz)` repurposed.

**Tests:** `BelyaninStepVisualizationTests` template-fill section: fill the real
templates for every rendered step (mirroring `SummaryTeX.belyaninStepSection`), assert
no leftover `__` placeholders; lualatex compile of every filled step inside the summary
template (Category TeX); no `Overfull` in any log. `BelyaninRunnerTests`: update
"one step dir per trace step" to the new file list (step 0: 3 files; steps 1..5:
start trio + per-label quads + end trio).

**Docs:** `docs/developer/belyanin-step-viz.md` Step Templates section (new files,
placeholders, widths); `docs/user/cli.md` Belyanin RPQ step-artifact table.

**Spec:**

- DOT templates use `\includegraphics[width=\linewidth,keepaspectratio]{__…_PDF__}`;
  TikZ templates embed the figure source wrapped by the section builder (not the
  template).
- Label-row concatenation order = numeric label index order.
- Templates are mdformat-clean where applicable (`.tex` files are not mdformat-checked;
  only `.md` is).

**Code (section builder + legend):** `src/FLPQ.Printers/SummaryTeX.fs` — `StepTemplates` gains
`BelyaninRowEnd`, `BelyaninRowEndTikz`, `BelyaninLabelRow`, `BelyaninLabelRowTikz`;
`belyaninStepSection` rewritten: step 0 fills the step template with empty
`__LABEL_ROWS__`/`__ROW_END__`; step k>0 reads start/end artifacts, scans
`label_(\d+)_select.tex` in the step dir (numeric order), fills one label-row template
per label (TikZ figures wrapped in `wrapTikzAdjustboxColumn`; DOT PDF paths
`dot_pdfs/{stepName}_label_{i}_automaton.pdf` / `_graph.pdf`), concatenates rows with
blank lines, fills `__ROW_END__` from the row-end template; output stays
`[ header; wrapStepAdjustbox filledTemplate; "" ]`. `belyaninColorLegend` replaced:
green!30 start vertex/initial state, lightblue!20 frontier states + frontier (initial)
vertices, yellow!20 target vertices, red edge current transition edges, red!40 edge
frontier path edges, `$\cdot$` empty cell. `src/FLPQ.Cli/Summary.fs` — load the four
new templates for `BelyaninRPQ`.

**Tests:** `SummaryTexSectionTests` — update the two `belyaninStepSection` tests to the
new placeholders; new facts: init step leaves `__LABEL_ROWS__`/`__ROW_END__` empty,
regular step interleaves label rows in numeric order, missing label files are skipped
gracefully; legend test asserts the new rows (green start vertex row present, old
"Vertices of the current frontier paths" wording gone).

**Docs:** `docs/developer/summary-tex.md` — StepTemplates record, belyaninStepSection
description, legend description.

**Spec:**

- Section builder stays mode-aware and file-driven (no rendering logic); it only reads
  step-dir artifacts and fills templates.
- Label count = number of `label_i_select.tex` files present; the visualizer and the
  section builder must agree on the index-based naming.

### S4: Documentation sweep (old S6)

- [x] Done — commit 22ef187

**Code:** none.

**Tests:** none (documentation-only subtask — code gates skipped per AGENTS.md, but
mdformat is run on every touched `.md`).

**Docs:** verify cross-references and consistency across `docs/developer/belyanin-step-viz.md`,
`rpq-graph-viz.md`, `gss-dot.md`, `gss-tikz.md`, `automaton-viz.md`, `summary-tex.md`,
`arroyuelo-rpq.md` (root graph now shows green sources), `docs/user/cli.md`; run
`mdformat` on all touched files.

**Spec:**

- One source of truth: the color table lives in `belyanin-step-viz.md`; other docs
  link to it instead of restating colors.
- `cli.md` step-artifact table lists the new per-step file names.

## Verification

- Full test suite green, including byte-identical GLL/RNGLR/Arroyuelo goldens.
- TeX compile tests (Category TeX) pass with no Overfull boxes for every Belyanin step.
- Manual check: run the CLI on `data/example_regexp.txt` + `data/example_graph.txt`
  (TikZ and DOT), inspect `viz_output/BRPQ` step dirs and the merged PDF.
- Quality gates (`tools/hard_gate.py`) STATUS: PASS — per-project ≥90% line and branch,
  total ≥95%.
