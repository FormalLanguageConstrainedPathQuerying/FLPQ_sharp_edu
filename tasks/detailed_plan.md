# Detailed Plan: Task 281 — Belyanin RPQ visualization

## Task description (verbatim)

281. Add Belyanin RPQ algorithm visualization to the CLI. Same structure as other algorithms: input, results, steps, and summary. Input is a regexp and a graph. Visualize both the regexp and the respective DFA. TikZ by default, DOT as fallback for all figures. Same visualization of matrices: use matrix with set of symbols (sets of vertex paths), not boolean matrices — look at Valiant visualization for example. For Belyanin one step is one iteration of the main loop (one step of BFS-like traversal). Visualize matrix operations (multiplications for simultaneous frontier propagation) and partial results in graph and automaton: current frontier. Highlight computed parts in the input graph: each step collects information necessary to highlight partial paths, not just start and final vertex — use a custom semiring over sets of vertex sequences to track intermediate points (look at Valiant custom semirings for inspiration; book 02_BFS.tex path semiring with I_simple). Use RNGLR step layout for inspiration, tuned for the RPQ algorithm. Use adjustbox for TikZ. Reuse shared infrastructure from task 280: CLI regexp/graph arguments, Regexp.toDfa, path semiring module, example data files.

## Design decisions (recorded before implementation)

1. **Combined multi-source trace.** One run over all sources: M[q,v] holds the set of
   simple paths from *any* source to v at automaton state q; a path's head identifies
   its source. Paths from different sources never interfere (distinct vertex sequences,
   DFA determinism), so the combined run is the union of the per-source runs and still
   terminates in at most |V|-1 iterations (simple paths). Per-source reachability is
   recovered from the final P: v is reachable from s iff there exists q in FinalStates
   and a path p in P[q,v] with head p = s. One step sequence for visualization;
   consistent with Arroyuelo's single evaluation.
2. **Path-level maskFilter.** M \<- M \\ P per cell (Set.difference), not pair-level.
   Pair-level masking would drop unprocessed simple paths that share a (q,v) with an
   already-accumulated path. Each simple path is produced exactly once (its prefix is
   processed once; the DFA makes the automaton side deterministic), so the mask is
   defensive — kept for book fidelity (I^P_reach, algo:RPQ_BFS_semiring).
3. **Per-label blocks in matrices.tex.** Only labels with a non-empty Select are shown
   (all-empty products add noise); both multiplications of the propagation step are
   shown: `(N^a)^T (x) M = <Select>` and `* G^a = <Extend>`; NewM is the union of the
   shown Extends. The book's I_simple drop is visible as a non-empty Select with an
   empty Extend (example: iteration 4, label b — candidate [0;1;2;3;2] dropped).
4. **step_0 = initialization** (per global plan): M holds the trivial path [s] at
   (StartState, s) for each source s; P empty; no per-label blocks.
5. **result.tex derives from the path trace.** Per-source reachable vertices come from
   the final P via path heads at final states — single source of truth (the trace); the
   runner does not call the Boolean evaluate separately. Equivalence with
   `BelyaninRPQ.evaluate` holds on acyclic graphs and is a property test.
6. **Shared rendering extraction.** `pathHighlights` + `renderGraph` move from
   ArroyueloStepVisualizer into a new `RpqGraphViz` module (reuse checklist Q5 — both
   step visualizers and both runners need them); the legend `colorBox`/`coloredEdge`
   helpers are hoisted in SummaryTeX so both RPQ legends share them; the RPQ header
   section is one shared function for both kinds (legends differ only in the
   highlighted-element description).
7. **DFA figures.** `Regexp.toDfa` produces no dead state (REmpty derivatives are
   skipped), so the example DFA has 3 states (q0 start, q1, q2 final). Step figures
   highlight frontier states — q with non-empty M[q,\*] — via the S5
   `dfaToTikzWithHighlights`/`dfaToDotWithHighlights`.

**Example trace (hand-computed, used in tests):** regexp `a / (b | c)* / a`, graph
`0 2 / 0 a 1 / 1 b 2 / 2 c 3 / 3 b 2 / 3 a 4 / 2 a 5` (source v_0):

- step_0 (init): M = {(q0,v0): [0]}, P empty
- step_1: label a, Extend[q1,v1] = [0;1]; NewM = {(q1,v1): [0;1]}
- step_2: labels b, c, a have non-empty Select; b, Extend[q1,v2] = [0;1;2];
  NewM = {(q1,v2): [0;1;2]}
- step_3: c, Extend[q1,v3] = [0;1;2;3]; a, Extend[q2,v5] = [0;1;2;5]; NewM = both
- step_4: b has non-empty Select, empty Extend (I_simple drops [0;1;2;3;2]);
  a, Extend[q2,v4] = [0;1;2;3;4]; NewM = {(q2,v4): [0;1;2;3;4]}
- step_5: all Select empty (q2 has no transitions); NewM empty — loop ends

6 steps total; reachable from v_0 via final state q2: {v_4, v_5} (matches Arroyuelo).

## Subtasks

### S1: BelyaninRPQ path-semiring trace (evaluateWithTrace) [done — da9fd42]

**Code:** `src/FLPQ.RPQ/BelyaninRPQ.fs`:

- +`BelyaninLabelStep<'t when 't: comparison>` record `{ Label: AutomatonLabel<'t>; Select: Matrix<Set<int list>>; Extend: Matrix<Set<int list>> }` — one label's
  propagation in an iteration (Select = (N^a)^T (x) M, Extend = Select * G^a).
- +`BelyaninTraceStep<'t when 't: comparison>` record `{ Index: int; IsInit: bool; M: Matrix<Set<int list>>; P: Matrix<Set<int list>>; Labels: BelyaninLabelStep<'t> list; NewM: Matrix<Set<int list>> }` — M after maskFilter (init: initial M), P
  after accumulation (init: empty), per-label products for labels with non-empty
  Select (init: []), next frontier (init: = M).
- +`evaluateWithTrace : DFA<'t, int> -> NFA<'t, int> -> BelyaninTraceStep<'t> list * Matrix<Set<int list>>` — combined multi-source run (decision 1): init
  M[StartState, s] = {[s]} per source; loop while M non-empty: path-level maskFilter
  (decision 2), P \<- P + M, per label a in `BooleanDecomposition.decomposeNonEmptySet`
  (skip AEpsilon): N^a from dfa.Transitions, Select = `PathSemiring.boolSelectRows`,
  Extend = `PathSemiring.mxm` of Select with the label's adjacency as a path matrix
  (single-edge cells, like ArroyueloRPQ's boolToPathMatrix); NewM = union of Extends.
  Returns steps + final P.
- +`reachableFromPaths : DFA<'t, int> -> Matrix<Set<int list>> -> int array -> (int * int list) list` — per-source reachable vertices from P via path heads at
  final states (decision 5), sources sorted, vertex lists sorted.
- Reuse: `PathSemiring.{boolSelectRows, mxm, addMatrices, isEmpty}`,
  `BooleanDecomposition.decomposeNonEmptySet`, `Dfa`/`Nfa`. No new linear-algebra code.

**Tests:** New `tests/FLPQ.RPQ.Tests/BelyaninTraceTests.fs`:

- Facts on the example (hand-computed trace above): step count = 6; init step shape
  (M cell [q0, v0] = {[0]}, P empty, Labels []); per-step key cells (step_1 Extend
  [q1,v1]; step_3 both Extends; step_5 all empty).
- I_simple fact: step_4 label b has non-empty Select and empty Extend (the book's
  dropped candidate [0;1;2;3;2]); no stored path in any P cell repeats a vertex.
- Well-formedness fact: every path in P[q,v] starts at a source and ends at v.
- Property (reuse `AcyclicRpqGenerators.AcyclicRegexAndGraph`): on acyclic graphs,
  per-source reachability from the trace's P equals `BelyaninRPQ.evaluate`'s row.

**Docs:** Update `docs/developer/belyanin-rpq.md` — trace function signatures,
decisions 1-3 and 5 (path-level mask, combined sources, result derivation), book
reference to the path semiring with I_simple.

**Spec:**

- The Boolean `evaluate` is unchanged; existing RPQTests pass without modification.
- evaluateWithTrace terminates in at most |V|-1 iterations on any input (simple paths).
- No TeX/dot strings or file I/O in FLPQ.RPQ (trace records are plain data).

### S2: RpqGraphViz shared module (extraction from ArroyueloStepVisualizer) [done — 44d01df]

**Code:**

- New `src/FLPQ.Printers/RpqGraphViz.fs`: move `pathHighlights`
  (`Matrix<Set<int list>> -> Set<int> * Set<int * int>`) and `renderGraph`
  (terminalPrinter, graph, highlighted vertices/edges -> dot * tikz) plus their
  private helpers (`graphEdgeSet`, `graphEdgeLabel`) from ArroyueloStepVisualizer.
- `ArroyueloStepVisualizer`: delete moved code, call `RpqGraphViz.pathHighlights` /
  `RpqGraphViz.renderGraph`.
- `ArroyueloRunner`: root graph artifact uses `RpqGraphViz.renderGraph`.
- FLPQ.Printers.fsproj: add Compile item (explicit item list).

**Tests:** Existing Arroyuelo tests pass unchanged (golden files byte-identical —
moving code does not change output); +`pathHighlights` fact in a new
`tests/FLPQ.Printers.Tests/RpqGraphVizTests.fs` (matrix with two paths -> expected
vertex/edge sets; empty matrix -> empty sets).

**Docs:** New `docs/developer/rpq-graph-viz.md`; update `docs/developer/FLPQ.Printers.md`
hub (module list + metadata cross-refs); update `docs/main.md` navigation.

**Spec:**

- Pure move: no behavior change; Arroyuelo golden tests must pass without regeneration.
- RpqGraphViz owns the "RPQ input graph with path highlights" rendering; both step
  visualizers and both runners call it (single source of truth).

### S3: BelyaninStepVisualizer + step templates + Helpers writer [done — 01860ab]

**Code:**

- New `src/FLPQ.Printers/BelyaninStepVisualizer.fs`:
  - `[<Struct>] type BelyaninVisualizationStep = { AutomatonDot: string; AutomatonTikz: string; Matrices: string; GraphDot: string; GraphTikz: string }`.
  - `renderSteps : (string -> string) -> DFA<string, int> -> NFA<string, int> -> BelyaninTraceStep<string> list -> BelyaninVisualizationStep list`:
    automaton = DFA with frontier states highlighted (q with non-empty M[q,\*]) via
    `AutomatonDot.dfaToDotWithHighlights` / `AutomatonTikz.dfaToTikzWithHighlights`
    (state labels q_i, TikZ `$q_i$`, circle shape); matrices = renderMatrices; graph
    = `RpqGraphViz.renderGraph` with `RpqGraphViz.pathHighlights step.M`.
  - `renderMatrices`: init step shows M and P blocks; iteration shows M (frontier
    after I_simple), P (accumulated), per label block `(N^a)^T (x) M = <Select>` then
    `* G^a = <Extend>` (labels in trace order, decision 3), and `New M = <NewM>`.
    All matrices via `PathSemiringTeX.matrixToTeX` with v_i row/col labels.
- New `data/Belyanin_step_template.tex` + `data/Belyanin_step_tikz_template.tex`:
  two-column minipage layout (RNGLR/Arroyuelo pattern) — left: automaton figure
  (`__STEP_AUTOMATON_PDF__` / `__STEP_AUTOMATON_TIKZ__`) + `__MATRICES__` in a
  center group; right: graph figure (`__STEP_GRAPH_PDF__` / `__STEP_GRAPH_TIKZ__`).
- `Helpers.fs`: `findBelyaninStepTemplate` / `findBelyaninStepTikzTemplate` (one-line
  wrappers over `findTemplateFile`) + `writeBelyaninStepsVisualization` (same pattern
  as writeArroyueloStepsVisualization: step_k dirs, automaton/matrices/graph files).
- FLPQ.Printers.fsproj: Compile item; FLPQ.Cli.Tests.fsproj: Content items for the
  two templates (pattern of the Arroyuelo templates).

**Tests:** New `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs`:

- Golden tests: renderSteps on the example trace — DOT and TikZ artifacts per step
  (CREATE_GOLDEN_FILES workflow), plus targeted assertions: init automaton highlights
  only q0; step_4 automaton highlights q2; step_4 graph highlights path [0;1;2;3;4]
  vertices/edges; matrices contain the I_simple empty Extend block.

**Docs:** New `docs/developer/belyanin-step-viz.md` (pattern of arroyuelo-step-viz.md);
update `docs/developer/FLPQ.Printers.md`; update `docs/main.md`.

**Spec:**

- No algorithm logic in the visualizer (plain data in, strings out).
- Both DOT and TikZ rendered per step; the runner writes one format per step.
- Templates use the same placeholder-filling pattern as arroyueloStepSection.

### S4: BelyaninRunner + CLI wiring + summary integration [done — 5c755d7]

Merged from the original S4 (runner + dispatch) and S5 (summary): `algorithmToKind`
must cover `BelyaninRPQ` for the build to pass, and the end-to-end `-s` test needs
both the runner and the summary section — the two are coupled.

**Code:**

- New `src/FLPQ.Cli/BelyaninRunner.fs`: `runBelyanin (regexpFile, graphFile, outputDir, useDot) : unit` — parse regexp (`RpqInput.parseRegexpFile`),
  `Regexp.toDfa`; parse graph (`GraphReader.parseGraphFile`);
  `BelyaninRPQ.evaluateWithTrace`; write `regexp.tex` (RegexpTeX), `dfa.tikz.tex` |
  `dfa.dot` (no highlights, q_i labels), `graph.tikz.tex` | `graph.dot`
  (`RpqGraphViz.renderGraph`, empty highlights), `result.tex` (final P matrix via
  `PathSemiringTeX.matrixWithVertexLabels` + per-source reachable lines from
  `reachableFromPaths`), step dirs via `BelyaninStepVisualizer.renderSteps` +
  `Helpers.writeBelyaninStepsVisualization`; status line
  `Belyanin RPQ: %d steps, sources: [...] -> reachable: [...]`.
- `AlgorithmTypes.fs`: +`BelyaninRPQ` case (display "Belyanin RPQ", directory
  "belyaninrpq"); update the algorithm help text.
- `Program.fs`: RPQ dispatch branch handles both `ArroyueloRPQ` and `BelyaninRPQ`
  (same -r/--graph requirements).
- FLPQ.Cli.fsproj: Compile item for BelyaninRunner.fs.
- `SummaryTeX.fs`: +`SummaryKind.BelyaninRPQ`; hoist `colorBox`/`coloredEdge` out of
  `arroyueloColorLegend` (shared private helpers); +`belyaninColorLegend` (lightblue
  = frontier automaton states, yellow/red = frontier path vertices/edges, dot = empty
  cell); the ArroyueloRPQ header branch becomes a shared `rpqHeaderSection` used by
  both kinds (legend parameterized); +`belyaninStepSection` (fills Belyanin step
  templates, pattern of arroyueloStepSection); `StepTemplates` record +`Belyanin`,
  +`BelyaninTikz`; buildContent step dispatch for the new kind.
- `Summary.fs`: `algorithmToKind` +BelyaninRPQ; template loading block (pattern of
  Arroyuelo's); StepTemplates construction.

**Tests:** New `tests/FLPQ.Cli.Tests/BelyaninRunnerTests.fs`:

- File existence in TikZ mode and DOT mode (regexp.tex, dfa.*, graph.*, result.tex,
  step_0..step_5 dirs with their files).
- result.tex: final P matrix present; `v_0` reachable line lists v_4, v_5.
- Status line: `Belyanin RPQ: 6 steps, sources: [v_0] -> reachable: [v_4, v_5]`.
- ProgramDispatchTests: +dispatch facts for BelyaninRPQ (pattern of Arroyuelo's).

Summary tests:

- `SummaryTexSectionTests`: +header section test (Belyanin legend lines, regexp/DFA/
  graph heads in both modes) and +step section test (template placeholders filled).
- `CliSummaryTests`: +end-to-end lualatex compilation for BelyaninRPQ in TikZ mode
  and DOT mode (pattern of the Arroyuelo tests).
- `TexCompilationTests`: migrate buildContent call sites to the extended StepTemplates
  record (compile check).

**Docs:** Update `docs/developer/FLPQ.Cli.md` (runner module), `docs/user/cli.md`
(Belyanin RPQ usage + output structure), `docs/project/architecture.md` (file listing),
`docs/developer/summary-tex.md` (new kind, shared RPQ header).

**Spec:**

- Same output structure as Arroyuelo (global plan "Output Structure" section).
- No separate Boolean evaluate call in the runner (decision 5).
- Missing -r/--graph -> Argu error with usage (existing behavior, no new code).
- One shared RPQ header function — no duplicated branch bodies (decision 6).
- Merged summary compiles with lualatex in both modes (adjustbox-wrapped TikZ).

## Verification (end of task)

- Full CLI runs on the example data in TikZ and DOT modes, with `-s`: PDF produced.
- All test projects green; hard gate STATUS: PASS.
