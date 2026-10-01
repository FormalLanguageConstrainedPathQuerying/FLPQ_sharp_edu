# Detailed Plan: Task 288 — Reachability (Boolean) Semantics for Belyanin RPQ

## Task description (verbatim)

```
288. Add reachability (boolean) semantics for Belyanin RPQ alongside the existing simple-path semantics. Keep a single `-a BelyaninRPQ` algorithm and add a `--semantics` (`-semantics`) option with values `reachability` (default; Boolean BFS reachability, the classical RPQ semantics) and `simplePath` (the existing path-semiring trace semantics). Add a Boolean trace over the single first source plus a step visualizer and a runner for `reachability`; preserve the existing simple-path runner and step visualizer (rename runner/visualizer modules to reflect that they work with simple paths). The reachability answer must be computed by `BelyaninRPQ.evaluate` (all sources), never from the path trace; `result.tex` renders the final Boolean F matrix and one line per source. Reachability step artifacts use the same file names and visualization record type as the simple-path version, so the full `-s` summary pipeline and existing styles/templates are reused unchanged. Boolean steps render F/V and per-label select/extend Boolean matrices plus automaton and graph figures: current frontier states/vertices lightblue, target states/vertices lightyellow, current-to-target transitions / followed edges red bold with their symbols, and no path edges (no light-red path highlights). The reachability trace and step visualization are single-source (first start vertex); the result is still computed for all sources. Add unit, cross-algorithm, CLI, summary, golden, and lualatex-compile tests, and update user/developer documentation.
**[USER GUIDANCE]**: Two CLI parameters: `-a` selects the algorithm and `--semantics` selects the semantics (`reachability` or `simplePath`); Boolean = reachability. Old runner/visualizer are renamed to reflect simple paths and kept byte-compatible. Full `-s` summary support for both semantics, reusing the existing Belyanin step templates/styles. Reachability trace/visualization is single-source only (first start vertex). Per-step rendering follows the existing scheme (matrices + graph + automaton); no path edges are drawn (path information is not collected), but used current-to-target transitions in the automaton and followed current-to-target edges in the graph are highlighted red bold with their respective symbols.
```

## Scope and constraints

- Bug being fixed: `BelyaninRunner` derives the reported answer from the
  simple-path trace, so vertices reachable only through a non-simple walk
  (a graph cycle) are missed (e.g. `(a / a)* / b` on
  `data/example_graph_demo_big.txt` misses `v_4`). The Boolean
  `BelyaninRPQ.evaluate` already computes the correct answer.
- `-a BelyaninRPQ` stays a single algorithm; `--semantics`/`-semantics`
  selects `reachability` (default) or `simplePath`.
- Simple-path output (runner + step visualizer + summary) stays
  byte-identical.
- Reachability trace/visualization is single-source (first start vertex, sorted);
  `result.tex` and the status line report every source via `BelyaninRPQ.evaluate`.
- Reachability steps reuse the exact artifact file names and the same
  `BelyaninVisualizationStep` record, so `Helpers.writeBelyaninStepsVisualization`
  and the `-s` summary (`SummaryTeX.belyaninStepSection` + templates) are reused
  unchanged.
- Reachability figures: red bold used automaton transitions and followed graph
  edges (with their symbols), lightblue current frontier, lightyellow targets;
  no light-red path edges (`pathEdges = ∅`).

## Reuse decisions (reusing skill)

- Reused as-is: `BelyaninRPQ.evaluate` (all sources), `MsBfs.maskFilter` /
  `boolAdd` / `boolMul`, `BooleanDecomposition.decomposeNonEmptySet`,
  `BelyaninRPQ.dfaLabelMatrix`, `PathSemiringTeX.boolMatrixToTeXBody`,
  `RpqGraphViz.renderGraph`, `AutomatonDot.dfaToDotWithHighlights` /
  `AutomatonTikz.dfaToTikzWithHighlights`, `Helpers.writeRpqRootArtifacts`,
  `Helpers.writeBelyaninStepsVisualization`, `SummaryTeX.belyaninStepSection`,
  the four Belyanin step templates.
- Extracted (Q5): the Belyanin visualization record types
  (`BelyaninLabelVisual`, `BelyaninBoundaryVisual`, `BelyaninVisualizationStep`)
  and the generic cell-agnostic helpers (labels, adjustbox wrapper, DFA figures,
  row/col non-emptiness) move to a shared module used by both visualizers.
- Renamed (Q4 naming clarity): `BelyaninRunner` → `BelyaninSimplePathRunner`,
  `BelyaninStepVisualizer` → `BelyaninSimplePathStepVisualizer`,
  `BelyaninRPQ.evaluateWithTrace` → `BelyaninRPQ.evaluateSimplePathWithTrace`.

## Target reachability step rendering

```
[F = M]   [V = P \ M]   [graph: blue current, green sources]   [automaton: blue current]
[ (N^a)^T ⊗ F = [N^T] ⊗ [F] = [Select] ]  [automaton: blue current, yellow targets, red used transitions]
[ F^a ⊗ G^a = [Select] ⊗ [G] = [Extend] ]  [graph: blue current, yellow targets, red followed edges]
...
[F = NewM]   [V = P]   ...
```

All matrices are Boolean (`\bullet` / `\cdot`); no path-tuple cells and no
light-red path edges.

---

## S1: Boolean trace in BelyaninRPQ + rename simple-path trace function

**Status:** Resolved

**Code:**

- `src/FLPQ.RPQ/BelyaninRPQ.fs`:
  - Add `BelyaninReachabilityLabelStep<'t>` `{ Label; N: Matrix<bool>; G: Matrix<bool>; Select: Matrix<bool>; Extend: Matrix<bool> }`.
  - Add `BelyaninReachabilityTraceStep<'t>` `{ Index; IsInit; M: Matrix<bool>; P: Matrix<bool>; Labels; NewM: Matrix<bool> }`.
  - Add `evaluateReachabilityWithTrace (dfa) (graph) : BelyaninReachabilityTraceStep<'t> list * Matrix<bool>` mirroring `runSingleSource` (single first source, sorted; empty source set → `([], Matrix.init qCount vCount false)`), recording init step + one step per iteration using `MsBfs.maskFilter` / `boolAdd`, per label `Select = boolMul (transpose N) masked`, `Extend = boolMul Select G`.
  - Rename `evaluateWithTrace` → `evaluateSimplePathWithTrace` (body unchanged).

**Tests:**

- New `tests/FLPQ.RPQ.Tests/BelyaninReachabilityTraceTests.fs`:
  - init step holds `M[start, source] = true`, `P` empty, no labels; `V = P \ F`.
  - structural facts: every `Select`/`Extend` is Boolean; final `P` has a final-state cell at `v_4` for `(a/a)*/b` on the big cyclic graph and at `v_3` for the small cyclic graph (regression for the reported bug).
  - `evaluateReachabilityWithTrace` agrees with `BelyaninRPQ.evaluate` for the traced source on acyclic and cyclic graphs (`[<Fact>]` + property test over `RegexAndGraphGenerators` limited to the first source).
  - empty start-state set yields no steps.
- Update existing references `evaluateWithTrace` → `evaluateSimplePathWithTrace` in `tests/FLPQ.RPQ.Tests/BelyaninTraceTests.fs`, `tests/FLPQ.Cli.Tests/BelyaninRunnerTests.fs`, `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs`.
- Add the new file to `tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj`.

**Docs:**

- `docs/developer/belyanin-rpq.md`: document both trace functions/semantics.

**Spec:**

- Single-source trace; `M`/`P` are `|Q|×|V|` Boolean matrices.
- Loop: `masked = maskFilter M P`; `P' = boolAdd P masked`; `NewM = ∪_a boolMul (boolMul (N^a)^T masked) G^a`; record step; `M = NewM`.
- Init step mirrors `evaluateSimplePathWithTrace`: `M` initial, `P` empty, `Labels = []`, `NewM = M`.

---

## S2: Shared Belyanin visualization types + boolean step visualizer

**Status:** Resolved

**Code:**

- New `src/FLPQ.Printers/BelyaninStepCommon.fs` (module `BelyaninStepCommon`, compiled before both visualizers):
  - move `BelyaninLabelVisual`, `BelyaninBoundaryVisual`, `BelyaninVisualizationStep` here;
  - generic helpers: `labelToTeX`, `stateLabel`, `vertexLabel`, `wrapAdjustbox`, `wrapFormula`, `dfaFigures`, and cell-generic `rowNonEmpty (isZero: 'a -> bool) (m: Matrix<'a>)`, `colNonEmpty`, `frontierStates`, `usedAutoEdges`, `followedEdges`.
- Rename `src/FLPQ.Printers/BelyaninStepVisualizer.fs` → `BelyaninSimplePathStepVisualizer.fs` (module `BelyaninSimplePathStepVisualizer`), using `BelyaninStepCommon` (output byte-identical; pass `PathSemiring.isZero` to the generic helpers).
- New `BelyaninReachabilityStepVisualizer.fs` (module `BelyaninReachabilityStepVisualizer`, `renderSteps terminalPrinter dfa graph steps : BelyaninVisualizationStep list`):
  - Boolean tiles via `PathSemiringTeX.boolMatrixToTeXBody BelyaninStepCommon.stateLabel BelyaninStepCommon.vertexLabel`.
  - `V` tile cell = `P ∧ ¬M`; select/extend lines use `\otimes` with Boolean matrices.
  - Automaton figure: current `frontierStates M` lightblue, targets `frontierStates Select` lightyellow, `usedAutoEdges M N` red bold.
  - Graph figure: frontier vertices (`colNonEmpty M`) lightblue, targets (`colNonEmpty Extend`) lightyellow, `followedEdges G Select` red bold, `pathEdges = ∅`.
  - Boundaries: F/V tiles + automaton (current states) + graph (current vertices, green sources, no edges).
- `src/FLPQ.Printers/FLPQ.Printers.fsproj`: add `BelyaninStepCommon.fs`; replace the old include with the two visualizers.
- `src/FLPQ.Cli/Helpers.fs`: update `BelyaninStepVisualizer.X` → `BelyaninStepCommon.X`.

**Tests:**

- New `tests/FLPQ.Printers.Tests/BelyaninReachabilityStepVisualizationTests.fs`:
  - structural: no `color="#FF9999"` (no path edges); lightblue current states/vertices, lightyellow targets; red bold used/followed edges only; every formula holds three `pNiceMatrix` blocks and Boolean cells (no `(v_` tuples).
  - template fill via `SummaryTeX.belyaninStepSection` (no leftover `__`).
  - golden tests for a small reachability trace (`CREATE_GOLDEN_FILES=1`), `[<Trait("Category","TeX")>]` lualatex compile tests.
- Update `BelyaninStepVisualizationTests.fs` module/type references; existing goldens must pass unchanged.
- Add the new test file to `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj`.

**Docs:**

- `docs/developer/belyanin-step-viz.md`: two visualizers, shared types module, Boolean figure rules.
- `docs/developer/FLPQ.Printers.md`: new module entries.

**Spec:**

- Reachability visualizer returns the same `BelyaninVisualizationStep` list; init step `End = None`, `Labels = []`.
- Only the first trace step's `M` (init) plus one step per Boolean iteration.
- No code path reads path data (`PathSemiring` / `RpqGraphViz.pathEdges` never called by the Boolean visualizer).

---

## S3: CLI runners, `--semantics`, and dispatch

**Status:** Resolved

**Code:**

- `src/FLPQ.Cli/AlgorithmTypes.fs`: add `type RpqSemantics = Reachability | SimplePath` and
  `| [<AltCommandLine("--semantics"); AltCommandLine("-semantics")>] Semantics of RpqSemantics`, default `Reachability`; update `Usage`. Verify Argu parses `reachability` / `simplePath` case-insensitively; if not, adjust the DU case names or use an enum so the user's exact strings parse.
- Rename `src/FLPQ.Cli/BelyaninRunner.fs` → `BelyaninSimplePathRunner.fs` (module `BelyaninSimplePathRunner`), using `BelyaninSimplePathStepVisualizer` and `evaluateSimplePathWithTrace`.
- New `src/FLPQ.Cli/BelyaninReachabilityRunner.fs` (module `BelyaninReachabilityRunner.runBelyanin`):
  - `steps, finalP = BelyaninRPQ.evaluateReachabilityWithTrace dfa graph`;
  - `boolResult = BelyaninRPQ.evaluate dfa graph` (all sources);
  - `writeRpqRootArtifacts`; `result.tex` = Boolean F matrix (`boolMatrixToTeXBody`) + per-source lines from `boolResult`; write steps via `Helpers.writeBelyaninStepsVisualization` + `BelyaninReachabilityStepVisualizer.renderSteps`; status line from `boolResult`.
- `src/FLPQ.Cli/Program.fs`: for `BelyaninRPQ`, read `Semantics` (default `Reachability`) and dispatch to the reachability or simple-path runner; `--semantics` ignored by other algorithms.
- `src/FLPQ.Cli/FLPQ.Cli.fsproj`: update includes.

**Tests:**

- Rename `tests/FLPQ.Cli.Tests/BelyaninRunnerTests.fs` → `BelyaninSimplePathRunnerTests.fs` (module rename, calls `BelyaninSimplePathRunner.runBelyanin`); all assertions unchanged.
- New `tests/FLPQ.Cli.Tests/BelyaninReachabilityRunnerTests.fs`: result.tex source lines include `v_4` (big) / `v_3` (small); status line; step-dir count/files in TikZ and DOT; `--semantics` end-to-end.
- `ProgramDispatchTests.fs`: add `-a BelyaninRPQ --semantics simplePath` and `--semantics reachability` success cases; default reachability.
- `AlgorithmTypesTests.fs`: parse `simplePath` / `reachability` (both `--semantics` and `-semantics`), default when absent.
- Add tracked example data (`data/example_graph_demo_big.txt`, `example_graph_demo_small.txt`, `example_regexp_demo.txt`) linked into `tests/FLPQ.Cli.Tests/FLPQ.Cli.Tests.fsproj`.
- Update `ProgramDispatchTests`/`CliSummaryTests` if the default semantics changes expected content.

**Docs:**

- `docs/user/cli.md`: `--semantics` row, default, output note.
- `docs/developer/FLPQ.Cli.md`: two Belyanin runners + dispatch.

**Spec:**

- Default semantics `reachability`; `-a BelyaninRPQ` on the demo now reports `v_4` / `v_3`.
- `result.tex` reachability form: Boolean `|Q|×|V|` matrix, then `Source v_s: reachable ...` lines derived from `BelyaninRPQ.evaluate`.
- Multi-source: trace/visualization only for the first source; `result.tex` lists all sources.

---

## S4: Summary support for both semantics

**Status:** Resolved

**Code:**

- `src/FLPQ.Cli/Summary.fs`: both semantics map to `SummaryKind.BelyaninRPQ`; keep code as-is (verify). Optionally append the semantics to `displayName` for the summary title and drop the "Frontier path edges" legend row for `reachability` by threading `RpqSemantics` into `buildSummary`/`SummaryTeX` (only if needed for correctness; default is reuse the existing legend).

**Tests:**

- `tests/FLPQ.Cli.Tests/CliSummaryTests.fs`: extend `runRpqWithSummary` to accept extra args; add Belyanin `-s` cases for both `--semantics` values (TikZ + DOT), asserting merged TeX exists and compiles with lualatex, and the header order.
- `tests/FLPQ.Cli.Tests/SummaryTests.fs`: `algorithmToKind`/`algorithmLower` for `BelyaninRPQ` unchanged.

**Docs:**

- `docs/developer/summary-tex.md` (if present) / `docs/developer/FLPQ.Cli.md`: note the summary is semantics-independent for Belyanin.

**Spec:**

- No new templates; reachability artifacts use identical names, so `belyaninStepSection` renders them unchanged.

---

## S5: Documentation consolidation

**Status:** Resolved

**Code:** none.

**Tests:** none.

**Docs:**

- `docs/developer/belyanin-rpq.md`: two semantics, Boolean trace, why the answer must use `evaluate`, `path-semiring.md` caveat framed as the `simplePath` semantics.
- `docs/developer/path-semiring.md`: note simple-path projection is visualization-only; reachability uses the Boolean algorithm.
- `docs/developer/FLPQ.RPQ.md`, `docs/developer/FLPQ.Printers.md`, `docs/project/architecture.md`: new files.
- `docs/user/cli.md`, `docs/developer/FLPQ.Cli.md`: already partially updated in S3; cross-check.
- Run `mdformat` on every touched `.md`.

**Spec:**

- One source of truth: `belyanin-rpq.md` owns the algorithm/semantics description; other docs link to it.

---

## Testing strategy summary

| Level | Coverage |
| --- | --- |
| RPQ unit/property | Boolean trace structure, cyclic regression, agreement with `evaluate` |
| Cross-algorithm | `evaluate` already tested vs Arroyuelo/Kronecker; add trace-vs-evaluate |
| Printers structural + golden | No path edges, red used/labelled edges, blue current, yellow target; template fill; lualatex |
| CLI runner | result.tex lines, status line, step dirs/files in both modes |
| CLI dispatch/args | semantics parse + default, both runner paths |
| Summary | merged TeX for both semantics in TikZ and DOT, lualatex compile |

## Execution Log

| Subtask | Commit | Status |
| --- | --- | --- |
| S1 | `ba6ac13` | Resolved |
| S2 | `d7d040c` | Resolved |
| S3 | `c67ea0a` | Resolved |
| S4 | `b07ad09` | Resolved |
| S5 | _(this commit)_ | Resolved |
