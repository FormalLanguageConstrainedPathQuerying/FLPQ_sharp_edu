# Code Review Report

## Task 282 Review (2026-09-24)

Scope: full branch diff vs dev — `src/FLPQ.Cli/AlgorithmTypes.fs` + `Program.fs` + `ArroyueloRunner.fs` + `BelyaninRunner.fs` (S1: unified `-q`/`-i` CLI), `src/FLPQ.Printers/GssTikz.fs` (S2: trailing `bendReciprocalEdges` parameter) + `RpqGraphViz.fs` (passes `true`) + `GllStepVisualizer.fs` / `RnglrStepVisualizer.fs` / `ArroyueloStepVisualizer.fs` (pass `false`), `src/FLPQ.Printers/SummaryTeX.fs` (S3: `wrapTikzAdjustboxColumn`, `wrapStepAdjustbox`, RPQ step sections) + `data/{Arroyuelo,Belyanin}_step_template.tex` (`\linewidth` includegraphics), tests (`GssDotTests`, `RpqGraphVizTests`, `SummaryTexSectionTests`, both step-visualization test files, `CliSummaryTests`, `ErrorPathTests`, `ProgramDispatchTests`, `TestGrammarFiles`, `GoldenHelpers`, `TestHelpers`, `AutomatonVisualizationTests` golden names, `xunit.runner.json`), regenerated Belyanin graph goldens, and docs (`cli.md`, `FLPQ.Cli.md`, `rpq-graph-viz.md`, `arroyuelo-step-viz.md`, `belyanin-step-viz.md`, `rnglr.md`, `summary-tex.md`).

**Findings resolved this review (commit 0522b05):**

- §13 (no duplication) — the Arroyuelo/Belyanin runners copy-pasted the ~14-line root-artifact block (regexp.tex + dfa/graph in DOT or TikZ form). Extracted to `Helpers.writeRpqRootArtifacts`; both runners call it. Output files byte-identical (runner tests pass unchanged).
- §15 (no stubbed tests) — `ErrorPathTests.empty output directory is handled` had no assertion and could never fail (`runCli` swallows all exceptions); now asserts a non-zero exit code like its siblings.
- §13/§17 (no duplication / shared helpers) — `countOccurrences` existed twice (private in `GoldenHelpers` and in `CliSummaryTests`). Canonical definition moved to shared `TestHelpers` (FLPQ.TestUtilities); `GoldenHelpers` delegates, `CliSummaryTests` uses it directly.
- §14 (language registry) — the ANBN classic grammar text + 4-token accept string were hardcoded literals in `CliSummaryTests` (8 sites) and `ProgramDispatchTests` (2 sites) while the same file already derived them from `LanguageRegistry.ANBN`. Bindings moved to `TestGrammarFiles`; all literal sites use them.
- §6 (doc comments) — added summaries to `ArroyueloVisualizationStep` / `BelyaninVisualizationStep`, `AutomatonTikz.escapeLatex` / `nodeOptions` / `tikzFooter`, and `InputGraphDot.toDot`.
- Consistency — the two RPQ-header facts in `SummaryTexSectionTests` used an inline Split-based count in a file that now opens `GoldenHelpers`; they use `countOccurrences` like every other fact.

**Verified:** build 0 errors; suites green — FLPQ.Printers.Tests 340, FLPQ.Cli.Tests 232 (incl. all Belyanin/Arroyuelo goldens, both per-step no-Overfull lualatex compiles, and the four end-to-end merged-summary no-Overfull compiles); Fantomas clean; mdformat clean; commit gate PASS.

**Findings against the constraint sources:**

- §4 (tuples ≤ 2) — no tuple expression larger than 2 items introduced by this branch; `writeRpqRootArtifacts` takes 5 scalar parameters (not a tuple).
- §6 (doc comments) — all new public API carries XML docs (`bendReciprocalEdges` documented at the function, both new SummaryTeX wrappers, `writeRpqRootArtifacts`, the two step records); the review added the missing ones listed above.
- §7 (genericity) — no new hardcoding introduced; the branch's new code follows the existing RPQ-layer convention (string terminals in the visualization layer, see open items).
- §9 (separation) — `GssTikz`/`SummaryTeX` produce strings only; runners do file I/O via `Helpers`; the bend decision is a rendering parameter, not algorithm state.
- §13 (no duplication) — see resolved findings; the two RPQ runners still share only the one-line `vertexName` helper (below the >3-line threshold, accepted in the task 281 review).
- §15/§16 (test fidelity / Fact vs Property) — every new fact asserts a concrete property: bent/straight edge lines in TikZ, DOT unchanged, exact adjustbox variant counts per filled step, lualatex compilation with no Overfull boxes (per-step and end-to-end), non-zero CLI exit codes. All deterministic `[<Fact>]`.
- §19 (test coverage) — every changed behavior has a fact: bend logic (`GssDotTests`, `RpqGraphVizTests`), column-width/step wraps (`SummaryTexSectionTests`), page fit (both step-visualization test files + `CliSummaryTests`), CLI unification (`ProgramDispatchTests`, `ErrorPathTests`).
- §20 (documentation) — `rpq-graph-viz.md`, `arroyuelo-step-viz.md`, `belyanin-step-viz.md`, `rnglr.md`, `summary-tex.md` (new wrappers, RPQ step-fit decisions, corrected the false `\textwidth`-in-minipage claim), `cli.md`, `FLPQ.Cli.md` all match the implemented behavior.
- §21 (book traceability) — S2/S3 changes are rendering-layer refinements of the book's worked example (Chapter 11); book references retained on both RPQ step visualizers and the shared graph renderer.

**Open items (pre-existing, not fixed in this task):**

- GLL/RNGLR (user guidance: "Do not touch GLL and RNGLR for now"): dead `symbolVisualizer` parameter in `GllStepVisualizer.renderStep/renderInit/renderSteps` (declared, passed through, never used); missing module doc comment on `RnglrStepVisualizer`; hardcoded ANBN literals in `GllRunnerTests` / `RnglrRunnerTests` (~28 sites).
- Tuple sizes > 2 in pre-existing code: `RnglrTypes.fs` stored-state 5-tuple (consciously accepted with documentation in the task 278 review), `Valiant.fs` 3-tuple list, `Sppf.fs` 4-tuple option, `ExternalTools.fs` 3-tuple accumulation, `GllTypes.fs` 4-tuple hash. Refactoring these is a cross-cutting change beyond this task's scope (and Rnglr/GLL are user-protected).
- RPQ visualization layer hardcodes `string` terminals (`RpqGraphViz`, both step visualizers, `RegexpTeX`) while the algorithms are generic — a design choice consciously accepted in the task 281 review ("consistent with every other step visualizer"); generalizing is a cross-cutting refactor beyond this task's scope.

---

## Task 281 Review (2026-09-24)

Scope: full branch diff vs dev — `src/FLPQ.RPQ/BelyaninRPQ.fs` (S1: path-semiring trace `evaluateWithTrace`, `reachableFromPaths`, `BelyaninLabelStep`/`BelyaninTraceStep`), `src/FLPQ.Printers/RpqGraphViz.fs` (S2: new shared module extracted from ArroyueloStepVisualizer), `src/FLPQ.Printers/BelyaninStepVisualizer.fs` + `data/Belyanin_step_(tikz_)template.tex` (S3), `src/FLPQ.Cli/BelyaninRunner.fs` + `AlgorithmTypes.fs` + `Program.fs` + `Summary.fs` + `src/FLPQ.Printers/SummaryTeX.fs` (S4: runner, dispatch, `SummaryKind.BelyaninRPQ`, shared `rpqHeaderSection`, `belyaninStepSection`), tests (`BelyaninTraceTests`, `RpqGraphVizTests`, `BelyaninStepVisualizationTests` + 18 goldens, `BelyaninRunnerTests`, dispatch/summary/section test additions), and docs (`belyanin-rpq.md`, `rpq-graph-viz.md`, `belyanin-step-viz.md`, `path-semiring-tex.md`, `summary-tex.md`, `FLPQ.Cli.md`, `cli.md`, `architecture.md`).

**Findings resolved this review:**

- §2/§13 (style idiom / no duplication) — FSharpLint FL0087 on three `sprintf` calls without interpolation holes in `BelyaninStepVisualizer.renderMatrices` (the `$\text{M}$`/`$\text{P}$`/`$\text{New } M =$` captions); replaced with plain verbatim strings. Rendered output is byte-identical, so the 18 Belyanin goldens pass unchanged.
- §23 (idiom) — FSharpLint FL0055 in `BelyaninStepVisualizationTests.fs`: `let _ = Assert.Single lines` → `ignore (Assert.Single lines)`.

Both fixes committed together (commit 5707596). A full-solution FSharpLint run (`FLPQ.slnx`) found no other warnings in any file.

**Verified:** build 0 errors; suites green — FLPQ.Cli.Tests 228, FLPQ.Printers.Tests 333 (incl. all Belyanin goldens and both lualatex end-to-end summary compiles), FLPQ.RPQ.Tests 79; full-solution FSharpLint 0 warnings after the fix commit; Fantomas clean; mdformat clean; manual CLI runs on the example data in TikZ and DOT modes with `-s` produce the root artifacts, six step dirs, and all dot-compiled PDFs.

**Findings against the constraint sources:**

- §4 (tuples ≤ 2) — no tuple expression larger than 2 items introduced; `BelyaninVisualizationStep` is a struct, trace steps are records.
- §6 (doc comments) — all new public API carries XML docs (`evaluateWithTrace`, `reachableFromPaths`, both trace records, `RpqGraphViz.pathHighlights`/`renderGraph`, `BelyaninStepVisualizer.renderSteps` + the step struct, `belyaninStepSection`, `matrixWithVertexLabels`); private helpers follow each file's existing convention. `runBelyanin` has no doc comment, matching `runArroyuelo` (module-level doc covers both).
- §7 (genericity) — the trace API is generic in the terminal type (`BelyaninTraceStep<'t when 't: comparison>`); the visualizer hardcodes `string` terminals, consistent with every other step visualizer (presentation layer over the book's string-labeled example).
- §9 (separation) — `BelyaninRPQ` produces plain data (trace records + final P); all TeX/DOT strings live in `FLPQ.Printers`; the runner does file I/O only.
- §13 (no duplication) — `RpqGraphViz` is shared by both step visualizers and both runners; `matrixWithVertexLabels` is shared by both matrix renderers; `rpqHeaderSection` + hoisted `colorBox`/`coloredEdge`/`legendTable` serve all four legends and both RPQ headers; the test helper `runRpqWithSummary` serves both RPQ summary test families. The two runners share only the one-line `vertexName` helper (below the >3-line threshold).
- §15/§16 (test fidelity / Fact vs Property) — every new fact asserts a concrete property: hand-computed 6-step trace cells, the I_simple drop at step 4, per-source reachable sets, status-line strings, equivalence with the Boolean `evaluate`, template placeholder filling in both modes, header section ordering, and lualatex compilation. The acyclic equivalence test is a `[<Property>]` reusing `AcyclicRpqGenerators` from the shared registry.
- §19 (test coverage) — every new/changed module has tests: `BelyaninRPQ` trace → `BelyaninTraceTests`; `RpqGraphViz` → `RpqGraphVizTests`; `BelyaninStepVisualizer` → `BelyaninStepVisualizationTests`; `BelyaninRunner`/dispatch/summary → `BelyaninRunnerTests`, `ProgramDispatchTests`, `SummaryTests`, `SummaryTexSectionTests`, `CliSummaryTests`.
- §20 (documentation) — new module docs (`belyanin-rpq.md`, `rpq-graph-viz.md`, `belyanin-step-viz.md`) plus updates to `path-semiring-tex.md`, `summary-tex.md` (shared RPQ head, new kind/section), `FLPQ.Cli.md`, `cli.md` (algorithm list, artifact tables, usage), and `architecture.md`; hub/navigation links updated in S2/S3.
- §21 (book traceability) — `BelyaninRPQ.fs` references Chapter 11 `02_BFS.tex` / `algo:RPQ_BFS_semiring`; the visualizer and step docs reference the same; the I_simple drop is documented against the book's path semiring.

**No blocking findings.** No open items carried over from prior reports.

---

## Task 279 Review (2026-09-22)

Scope: full branch diff vs dev — `src/FLPQ.Printers/RnglrAutomatonTikz.fs` (new: RNGLR LR automaton TikZ renderer), `src/FLPQ.Cli/RnglrRunner.fs` (mode-aware `ext_rsm.*` + `lr_automaton.*` artifacts, `rsm_blocks.dot` removed), `src/FLPQ.Cli/Summary.fs` (RNGLR header visuals: extended RSM PDF + LR automaton detection), `src/FLPQ.Printers/SummaryTeX.fs` (RNGLR head order Color Legend → Extended RSM → LR Automaton → Table → Path Index; SPPF-style adjustbox for the automaton), tests (`RnglrAutomatonTikzTests`, `RnglrRunnerTests`, `SummaryTests`, `SummaryTexSectionTests`, `CliSummaryTests`, both RNGLR end-to-end tests in `TexCompilationTests`), and docs (`automaton-viz.md`, `FLPQ.Printers.md`, `summary-tex.md`, `FLPQ.Cli.md`, `rnglr.md`, `cli.md`).

**Findings resolved this review:**

- §13 (no duplication) — the RNGLR branch of `Summary.fs` duplicated the LR branch's automaton detection (~15 lines of tikz-over-dot file probing), and both GLL/RNGLR branches duplicated the ext_rsm.pdf entry list. Extracted private helpers `lrAutomatonVisuals` (TikZ source when `lr_automaton.tikz.tex` exists, else `dot_pdfs/lr_automaton.pdf` when `lr_automaton.dot` exists) and `extRsmPdfs`; all four match branches now share them. Behavior unchanged; SummaryTests + CliSummaryTests still pass (commit 1ac9e2e).

**Verified:** build 0 errors; FSharpLint 0 warnings on all four touched projects (`FLPQ.Printers`, `FLPQ.Cli` and both test projects — linted with `.fsproj` arguments); Fantomas clean; mdformat clean; targeted suites pass — SummaryTexSectionTests 21, SummaryTests + CliSummaryTests 53, RNGLR Printers tests 38 (including both lualatex end-to-end compiles).

**Findings against the constraint sources:**

- §4 (tuples ≤ 2) — `lrAutomatonVisuals` returns a 2-item tuple; no larger tuples introduced.
- §6 (doc comments) — all public API in `RnglrAutomatonTikz` carries XML docs (`renderRnglrItem`, `renderRnglrStateContent`, `rnglrAutomatonToTikz`); the new `Summary.fs` helpers are private with short doc comments, consistent with the file's style.
- §11 (variants as thin layers) — `RnglrAutomatonTikz` delegates to `AutomatonTikz.dfaToTikz` (shape "rectangle") and reuses `LRAutomatonTikz.stateContentToTikzAs`; the DOT runner labels reuse `renderRnglrItem`, so DOT and TikZ state labels match.
- §13 (no duplication) — see resolved finding; the remaining 2-line `rsmPdfs |> List.collect ...` pattern shared by the GLL/RNGLR header branches is below the >3-line threshold and stays inline.
- §14 (language registry) — all new test inputs come from registry grammars (ANBN classic, DoubleA singleRule); no hand-built RSMs.
- §15/§16 (test fidelity / Fact vs Property) — every new fact asserts a concrete property: per-mode file presence/absence, IndexOf ordering of the head sections, exact adjustbox variant counts (width-only `1 + sections.Length`; max-totalheight 1 for CYK/Valiant/GLL, 2 for RNGLR), includegraphics presence/absence, and lualatex compilation. All deterministic `[<Fact>]`.
- §19 (test coverage) — the new module `RnglrAutomatonTikz` has a dedicated test file including a lualatex compile fact; every changed behavior (runner artifacts, summary detection, header layout) has a corresponding fact.
- §20 (documentation) — `automaton-viz.md` gained a RnglrAutomatonTikz section + module row; `summary-tex.md` overview bullets and two design-decision rows; `FLPQ.Cli.md` role paragraph; `cli.md` RNGLR artifact row; `rnglr.md` See Also. All match the implemented behavior.
- §21 (book traceability) — `RnglrAutomatonTikz.fs` carries `Book reference: sec:CFPQ_RNGLR`; the header layout decisions are documented in `summary-tex.md`.

**No blocking findings.** No open items carried over from prior reports.

---

## Task 278 Review (2026-09-21)

Scope: full branch diff vs dev — `src/FLPQ.Languages/Rnglr.fs` + `RnglrTypes.fs` (per-action substep emission, `RnglrAction`, `triggerLrState` threaded through stored states and `PredecessorInfo`), `src/FLPQ.Printers/RnglrTableTeX.fs` (single-action cell highlight API replacing the multi-cell one) + `RnglrStepVisualizer.fs` (`renderStep` switches on `step.Action`), tests (`RnglrSubsteps` module, visualizer substep facts, new golden `rnglr_lr_table_substep_shift.tex`, regenerated `rnglr_gss_aaa_last.dot`, shared `GoldenHelpers.countOccurrences`), and `docs/developer/rnglr.md`.

**Findings resolved this review:**

- §4 (tuples ≤ 2) — FSharpLint FL0051 on five 3-item tuple expressions in test code: `RnglrTests.fs` `runWithSteps` return value and the three `cases` entries now use private structs `RnglrRun { Result; Ers; Graph }` and `SubstepCase { Name; Rsm; Input }`; `RnglrStepVisualizationTests.fs` reduce-fact extraction now uses private struct `ReduceCells { Nt; TriggerLrState; GotoLrState }`.
- §23 (naming/idiom) — FSharpLint FL0034 on two redundant lambdas: `Rnglr.fs` `(fun step -> steps.Add step)` → `steps.Add`; `RnglrTests.fs` `Matrix.fold (fun acc cellEqual -> acc && cellEqual) true` → `Matrix.fold ((&&)) true`.

**Verified:** build 0 errors; full suites 626 Languages + 268 Printers passed / 0 failed / 0 skipped; FSharpLint 0 warnings on all four touched projects (`FLPQ.Languages`, `FLPQ.Printers`, `FLPQ.Languages.Tests`, `FLPQ.Printers.Tests` — linted with `.fsproj` arguments, since directory arguments make fsharplint analyze a vacuous `<inline source>`); Fantomas clean; mdformat clean.

**Findings against the constraint sources:**

- §4 (tuples ≤ 2) — the `StoredStates` 5-item tuple (`Nonterminal * int * int * int * int`) is a type annotation inside `Set<...>`, not a tuple expression; FSharpLint does not flag it and the element order is documented at the type site and in `docs/developer/rnglr.md`. All new tuple *expressions* are ≤ 2 items.
- §6 (doc comments) — all new public API carries XML docs: `RnglrAction`, `tableToTeXWithActionHighlight` / `...TabularOnly`; the changed `RnglrParsingStep` / `RnglrResult` / `RnglrGSS` docs were updated to the substep semantics. Test-local helpers follow each file's existing convention (doc-less in `GoldenHelpers.fs`, doc-commented in `RnglrTests.fs`).
- §13 (no duplication) — the old multi-cell highlight tabular builder was deleted, not kept alongside the new one; `buildTabular` now delegates to `buildTabularCore` with an empty highlight set. `countOccurrences` moved from a test-local copy into shared `GoldenHelpers` and is reused by both RNGLR test modules.
- §15/§16 (test fidelity) — every new fact asserts concrete structure: exact action sequences, per-substep cell counts (`Assert.Equal(1, ...)` / `Assert.Equal(2, ...)`), row/column localization of each highlighted cell via parsed tabular rows, and path-index equality with the step-less `buildPathIndex`. All deterministic `[<Fact>]` with registry grammars — no FsCheck needed.
- §14 (language registry) — all new test inputs come from registry grammars (DoubleA, APlus `ambiguousWithSingleRule`, Dyck1); no hardcoded RSMs.
- §20 (documentation) — `docs/developer/rnglr.md` abstract, algorithm emission rule, `RnglrAction` section, type docs, and three new design-decision rows (per-action substeps, `triggerLrState` in stored states, epsilon-only-change non-emission) all match the implemented behavior.
- §21 (book traceability) — `sec:CFPQ_RNGLR` references retained on `RnglrAction`, `buildPathIndexWithSteps`, and the emission logic; the passing-reduction trigger keeps its `sec:CFPQ_GLR` reference.

**No blocking findings.** No open items carried over from prior reports.

---

## Task 277 Review (2026-09-18)

Scope: `tools/hard_gate.py` (S1 — branch-coverage parsing via each line's `condition-coverage="(n/m)"`, new `_gate_status` helper gating line and branch per project and total), `tests/FLPQ.Cli.Tests/` (S2 — new `RunnerTestHelpers.withCapturedOutput` stdout-capture helper, the four runner helpers now return `(outDir, capturedOutput)`, two new Rejected-status facts, shared `[<Xunit.Collection("ConsoleCapture")>]`), `tests/FLPQ.RPQ.Tests/RPQTests.fs` (S3 — six edge-case facts for Belyanin/GraphReader/Kronecker), and docs (`docs/developer/guides/tools.md` step-5 description + three example outputs, `.opencode/skills/quality-gates/SKILL.md` coverage row).

**Findings resolved this review:** none. A full pass over the changed surface (tool, both test modules, docs) found zero problems against the constraint sources; no fix commit was required.

**Verified:** hard gate `STATUS: PASS` (all 12 steps) — build 0 errors; all six test projects 0 failed / 0 skipped; coverage gate PASS on every project for both line and branch (FLPQ.RPQ branch 90.2% (184/204), FLPQ.Cli branch 92.3% (144/156), TOTAL line 98.6% / branch 96.6% (3864/4000)); FSharpLint 0 warnings on the two changed test projects; Fantomas clean; mdformat clean.

**Findings against the constraint sources:**

- §4 (tuples ≤ 2) — the runner helpers' new return type is a 2-item tuple `string * string` (outDir, captured output); no larger tuples introduced.
- §6 (doc comments) — the new public helper `RunnerTestHelpers.withCapturedOutput` carries an XML doc comment; the per-module runner helpers remain `private` (consistent with the files' existing convention).
- §13 (no duplication) — stdout capture exists only in `withCapturedOutput` and is reused by all four runner helpers; no temp-dir or capture logic is duplicated. The four per-module runner helpers differ only in the runner function and dot/tikz flag and write arbitrary grammar text, so they correctly do not collapse into `runWithInput` (which is fixed to the example grammar).
- §15/§16 (test fidelity / Fact vs Property) — every new test asserts a real property: the two Rejected facts assert the exact status substring (`GLL: Rejected` / `RNGLR: Rejected`) in captured output; the six RPQ facts assert concrete matrix values, state counts, start-state sets, transition presence, and matrix dimensions. All are deterministic `[<Fact>]` with hardcoded inputs (no FsCheck), which is correct for edge-case branch coverage.
- §19 (test coverage) — unchanged: no new src module; the branch-coverage task adds tests only.
- §20 (documentation) — `tools.md` step-5 description and all three example outputs are arithmetically consistent (each percentage matches its fraction) and match `hard_gate.py`'s actual output format, including the `_gate_status` BLOCKED-reason wording ("line and branch below N%").

**No blocking findings.** No open items carried over from prior reports.

---

## Task 276 Review (2026-09-18)

Scope: full branch diff vs dev — coverage tests S1–S10 across all six test projects plus `FLPQ.TestUtilities` (new `LrConflictFixture`, `RsmFixtures`, `withTempDir` helper, registry additions), `tools/hard_gate.py` (line-coverage thresholds 85/90 → 90/95), `docs/developer/guides/tools.md` (example outputs), and one src fix in `src/FLPQ.Printers/RnglrStepVisualizer.fs` (epsilon edge-label escaping).

**Findings resolved this review:**

Round 1 (20 findings):

- §9/§23 (correctness) — `RnglrStepVisualizer.fs` double-escaped epsilon edge labels into literal `\textbackslash varepsilon` text; the printer now pre-escapes via `AutomatonTikz.escapeLatex`, emits `$\varepsilon$` for `AEpsilon`, and calls `GssTikz.toTikzFromSets` with `skipEscaping = true`, mirroring the RsmTikz math-mode-label convention.
- §13 (no duplication) — ten temp-dir try/finally copies in `ExternalToolsTests` plus a private copy in `SummaryTexSectionTests` consolidated into shared `TestHelpers.withTempDir`; four hand-built RSMs (GllTests ×2, RnglrTests, RsmDotTests) replaced by shared `RsmFixtures.mkRsm`; the LR conflict grammar duplicated across `LRTableTeXGoldenTests` and three `LRParserTests` facts replaced by shared `LrConflictFixture`.
- §15 (test fidelity) — weak assertions strengthened: EbnfParserTests file tests asserted only list length / `StateCount > 0`, now assert content equality and the concrete RSM shape of "S -> a b" (StartBlock S, 3 states, final {2}); the toCnf epsilon test now asserts no `Symbol.Epsilon` survives in CNF rules; the Belyanin epsilon test now uses a vertex reachable only via an epsilon edge and asserts it is not reached.
- §13/§15 — duplicated runner-input helpers and four near-identical runner tests in CykRunnerTests/ValiantRunnerTests consolidated into shared `RunnerTestHelpers.runWithInput`; the two noSppfTable variants became one parameterized Theory asserting AsNt tables contain no tuples while plain tables do; `Assert.Throws<System.Exception>` replaced by `TestHelpers.assertThrows` with message checks (LRRunnerTests, RPQTests).
- §20 (documentation) — `tools.md` example coverage numbers were arithmetically infeasible (per-project covered lines exceeded the total); examples now feasible and consistent with `hard_gate.py`'s output format.

Round 2 (12 findings):

- §7 (genericity) — `withTempDir` pinned to `unit`; now `(f: string -> 'a) : 'a`.
- §4 (tuples ≤ 2) — `RsmFixtures.mkRsm` transitions parameter was a 3-item tuple list; now `Trans<AutomatonLabel<RsmSymbol<string, string>>> list` reusing the existing record, call sites updated.
- §6 (doc comments) — `LrConflictFixture` public values gained XML docs.
- §13 — remaining temp-dir copies consolidated: EbnfParserTests `writeTempEbnf`, CykSummaryGoldenTests, and four TexCompilationTests bodies now use `withTempDir`.
- §14 (language registry) — RnglrTableTexTests inline grammar moved to a new SingleAB "threeRule" registry entry; three BasicSppfTests hardcoded grammars/inputs now from SingleAB; the "a a b" and "a" runner inputs now derived from `Dyck1.RejectStrings` (new `[a; a; b]` entry added).
- §23 — unused opens removed from RsmDotTests.

Round 3 (1 finding):

- §14 — the new SingleAB "threeRule" entry declared `IsInCnf = false`; all three productions are in CNF form per the documented definition, now `true`.

Gate-driven fix (surfaced by FSharpLint, pre-existing on dev but inside a changed project):

- §4/§13 — `SppfValidatorTests.fs` `mkSppf` took `(int * SppfEdgeLabel * int)` edge tuples (FL0051) and repeated one identical `failwith "expected invariant violation"` message 18 times (FL0072 failwithBadUsage); edges now `Trans<SppfEdgeLabel>` records and each failure message is unique.

**Verified:** build 0 errors; full suite 1175 passed / 0 failed / 0 skipped (Cli 171, GraphAnalysis 33, Languages 619, LinearAlgebra 51, Printers 266, RPQ 35); FSharpLint 0 warnings on the full solution; Fantomas clean; mdformat clean.

**Findings against the constraint sources:**

- §6 (doc comments) — all new public API (`withTempDir`, `mkRsm`, `LrConflictFixture` values, `RunnerTestHelpers.runWithInput`) carries XML docs; the src change keeps its book-reference comment (sec:CFPQ_RNGLR step visualization).
- §7 (genericity) — no new hardcoded types in algorithm code; the one src change is a printer (presentation layer), where concrete string escaping is inherent.
- §13 (no duplication) — after consolidation, temp-dir lifecycle exists only in `TestHelpers.withTempDir`, runner-input setup only in `RunnerTestHelpers.runWithInput`, and hand-built RSM/LR-conflict construction only in the two TestUtilities fixtures.
- §14 (language registry) — all test grammars and accept/reject strings in the changed surface come from the registry; the two new registry entries ("threeRule", Dyck1 `[a; a; b]`) carry verified properties.
- §15/§16 (test fidelity / Fact vs Property) — every strengthened test asserts a real property of the result (content equality, concrete automaton shape, absence/presence of rendered tuples, unreachable-via-epsilon vertex); deterministic facts stay `[<Fact>]`.
- §19 (test coverage) — unchanged: every src module retains its correspondent; the src fix is covered by RnglrStepVisualizationTests.
- §20 (documentation) — `tools.md` examples match the tool's actual output format and thresholds; no new module, so no hub/architecture doc change needed.

**No blocking findings.** The third pass over the round-2 changed surface found one problem (IsInCnf), fixed; the remaining edits (unique failwith messages, property flip) are string-level changes verified by build plus the full suite.

---

## Task 275 Review (2026-09-15)

Scope: `src/FLPQ.Printers/AutomatonTikz.fs` (`layeredGraphOptions` gains a grow-direction argument; new `defaultGrowDirection` / `gssLayeredGrowDirection` constants), `src/FLPQ.Printers/GssDot.fs` + `GssTikz.fs` (trailing opt-in `positionOf: (int -> int) option`; per-position `{rank=same; ...}` / `{ [same layer] ... }` emission; TikZ switches to `grow=left` when layers are active), `src/FLPQ.Printers/RnglrStepVisualizer.fs` (passes `Some (fun idx -> snd (vertexInfo idx))`), `src/FLPQ.Printers/GllStepVisualizer.fs` + `RsmTikz.fs` (call sites updated to the new signatures / default grow direction), `src/FLPQ.Printers/ExternalTools.fs` (new `compileDotStringToNodePositions`), tests (`GssDotTests`, `ExternalToolsTests`, `RnglrStepVisualizationTests` + golden `rnglr_gss_aaa_last.dot`), docs (`automaton-viz.md`, `rnglr.md`, `external-tools.md`).

**Findings resolved this review:**

- §23 (naming semantics) / correctness — `GllStepVisualizer.fs` first `gssTikz` edge-label printer computed the target vertex as a state (`v2 = to_ / vertexCount`) instead of a vertex (`v2 = to_ % vertexCount`), corrupting GLL GSS TikZ edge labels. The other three printers in the file already used `%`; this was an accidental edit made while adding the `positionOf=None` arguments. Restored to `%` so all four are consistent and `rangeToTeX` receives `(fromState, fromVertex, toState, toVertex)`.
- §9 (separation / consistency) — `GssTikz.fs` same-layer grouping used `allVertices` (active + edge-referenced), which omits a current vertex not already among them; such a vertex is still drawn and would float outside its position layer. `GssDot` groups over `renderedVertices`, which includes the current vertex. The TikZ grouping now uses `allVertices` plus the current vertex, mirroring DOT so every drawn node lands in exactly one position layer.

**Verified:** build 0 warnings / 0 errors; full suite 967 passed / 0 failed / 0 skipped across all six projects (RPQ 27, GraphAnalysis 33, LinearAlgebra 51, Cli 159, Printers 192, Languages 505) — `FLPQ.Cli.Tests` exercises the GLL runner affected by finding 1 and passes; FSharpLint 0 warnings on FLPQ.Printers; COMMIT_GATE: PASS.

**Findings against the constraint sources:**

- §6 (doc comments) — `defaultGrowDirection`, `gssLayeredGrowDirection`, `layeredGraphOptions`, and `compileDotStringToNodePositions` carry doc comments; the `positionOf` parameter is documented on both `toDotFromSets` and `toTikzFromSets` with the "why" (pgf same-layer cluster chaining reverses orientation under the default grow direction, hence `grow=left`).
- §13 (no duplication) — the per-position grouping idiom (`Seq.groupBy posFn |> Map.ofSeq`) is a one-liner reused in both renderers; the emission differs by format (`; ` vs `, `, subgraph syntax) so no shared helper is warranted. The test's `renderRnglr`/`renderViz` split reuses the registry grammar rather than duplicating it.
- §15/§16 (test fidelity / Fact vs Property) — the new facts assert real properties: DOT coordinate test checks same-position nodes share one x, columns strictly decrease with position, and position 0 is rightmost; TikZ structural test parses each `{ [same layer] ... }` collection and compares it to the per-position grouping of independently-declared nodes. Deterministic, correctly `[<Fact>]`; no stubs or tautologies.
- §19 (test coverage) — `GssDot`, `ExternalTools`, and the RNGLR step visualizer all retain dedicated correspondents; the new golden locks the non-trivial multi-position DOT format.
- §20 (documentation completeness) — `automaton-viz.md` (grow-direction parameterization), `rnglr.md` (position-layering design decision + orientation note), and `external-tools.md` (`compileDotStringToNodePositions`) are updated in place; no new module, so no hub/architecture change needed.
- §21 (book traceability) — the layering is a rendering concern; the doc comments and `rnglr.md` row explain the pgf orientation rationale rather than citing an algorithm listing, consistent with other visualization tasks.

**No blocking findings.** Second pass over the changed surface (both renderers, all four GLL edge-label printers, the new position parser, both test files, the golden, and the three docs) found zero additional problems.

---

## Task 274 Review (2026-09-14)

Scope: `src/FLPQ.Languages/Rnglr.fs` (level driver reordered to canonical reduce-then-shift: round loop and `shifted` HashSet removed; `reduceAtLevel` now returns `unit`; `processReduction` return simplified from `bool * int option` to `bool`, dropping the consumer-less `newVertexGotoTarget` component and the `existedBefore` lookup), `docs/developer/rnglr.md` (canonical order described throughout; "Rounds" design-decision row removed; Scott & Johnstone 2006 added to Book Reference).

**Findings resolved this review:** none — the review pass found no problems.

**Verified:** full suite 961 passed / 0 failed / 0 skipped (all six test projects, including `RnglrPassingReductions` and all cross-parser equivalence tests); reference visualization for `data/example_input_a_a_a.txt` + `data/example_grammar_a_a_a.bnf` regenerated with the exact task command — all 29 `.tex`/`.dot` artifacts byte-identical to the pre-change baseline (the two graphviz PDFs differ only in embedded CreationDate metadata, content verified via pdftotext); FSharpLint 0 warnings on FLPQ.Languages; COMMIT_GATE: PASS.

**Findings against the constraint sources:**

- §6 (doc comments) — `buildPathIndex` doc comment and the three private phase functions (`processReduction`, `shiftNode`, `reduceAtLevel`) carry updated comments matching the new signatures and order.
- §13 (no duplication) — pure reorder plus dead-code removal; no logic added or copied.
- §18 (equivalence tests) — the change is a reordering of an existing algorithm, not a variant: behavior preservation is established by the unchanged full suite (acceptance, property-based tree-yield, cross-parser equivalence) and the byte-identical reference visualization.
- §20 (documentation completeness) — `docs/developer/rnglr.md` is the single source of truth for the driver order and is updated in place; no new module or public API, so no hub/architecture changes needed.
- §21 (book traceability) — the level-loop comment and `reduceAtLevel` doc now cite Scott & Johnstone 2006 Algorithm 1e PARSE SYMBOL and book sec:CFPQ_GLR (MakeReductions → Push → ApplyPassingReductions); the doc's Book Reference section lists the paper with its book citation key.
- §23 (naming semantics) — `reduceAtLevel : int -> unit` and `processReduction : ... -> bool` names match their behavior; both `processReduction` call sites (`reduceAtLevel`, `shiftNode`'s `|> ignore`) are consistent with the simplified return.

**No blocking findings.** Second pass over the changed surface (driver loop, both phase functions, all comments, doc file) found zero additional problems.

---

## Task 273 Review (2026-09-14)

Scope: `src/FLPQ.Printers/SppfTikz.fs` (range/intermediate node labels switch to subscript indices — new local `rangeLabel` helper, `SppfRange` and `SppfIntermediate` arms), `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` (stale task-269 `$\to$` assertion replaced with new-format assertions), `tests/FLPQ.Printers.Tests/SppfTikzTests.fs` (new — 3 facts) + fsproj registration.

**Findings resolved this review:**

- §14 (language registry) — the new `SppfTikzTests` initially hardcoded the 4-token input `[ "a"; "a"; "b"; "b" ]` inline. The exact string is ANBN's 4-token accept string in the registry; the test now derives it from `LanguageRegistry.ANBN.AcceptStrings` (`1d7545d`).

**Verified:** SppfTikzTests 3/3 (rendered label counts equal the actual `SppfRange`/`SppfIntermediate` node counts in the ANBN classic "aabb" SPPF, so every node of both kinds is checked against the new format); `TexCompilationTests.SPPF tikz compiles with lualatex` passes — the subscript math mode (`$[s_{i},v_{j}]\to[s_{k},v_{l}]$`, `$I_{m,p}$`) compiles end-to-end; FSharpLint 0 warnings on both changed projects; COMMIT_GATE: PASS.

**Findings against the constraint sources:**

- §6 (doc comments) — `rangeLabel` is a local function (no doc comment required); the public `toTikz` doc comment is unchanged and still accurate.
- §13 (no duplication) — the range notation previously duplicated across the two match arms now lives in the single `rangeLabel` helper; the test regexes restate the expected output format (spec assertions, not logic).
- §15/§16 (test fidelity / Fact vs Property) — the facts assert exact rendered-label counts against node counts plus absence of both pre-task-273 formats; deterministic, correctly `[<Fact>]`.
- §19 (test coverage) — `SppfTikz` gains a dedicated correspondent test file (previously only covered indirectly by the TeX compilation test).
- §20 (documentation completeness) — no doc update required: no new module, no public API change; no developer doc describes the SppfTikz label format (consistent with task 269, which changed these same labels without docs).
- §21 (book traceability) — the `rangeLabel` comment references sec:CFPQ_GLL, matching the module's book reference.

**No blocking findings.** Second pass over the changed surface (renderer, both test files, fsproj, runner tests for stale assertions) found zero additional problems. Pre-existing note (unchanged by this task): `TexCompilationTests.fs` and `RsmTikzTests.fs` still hardcode the same 4-token ANBN input inline — registry-eligible data; migrating them is a separate cleanup.

---

## Task 272 Review (2026-09-14)

Scope: `src/FLPQ.Printers/SummaryTeX.fs` (`wrapTikzAdjustbox` gains a leading `limitHeight: bool`; the five existing call sites — ext-RSM head, LL/LR stack step, GLL per-step GSS+RSM, RNGLR per-step GSS — pass `false`, and `sppfSection` switches from `wrapTikzCenter` to `wrapTikzAdjustbox true`), `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` (+1 shared helper, +4 facts — one per algorithm), `docs/developer/summary-tex.md` (helper signature, flag-semantics note, two design-decision rows).

**Findings resolved this review:** none — the review pass found no problems. (A verbatim-string escape issue in the first draft of `wrapTikzAdjustbox` — a regular string literal turned `\textheight` into `<TAB>extheight` — was caught during S2 implementation by the four new facts and fixed before this review; it is recorded here as evidence that the facts exercise the real emitted TeX.)

**Verified:** CliSummaryTests — the 4 new SPPF facts pass, and the pre-existing adjustbox facts (LL inline-TikZ, SLR1 step stack-trees, GLL/RNGLR per-step) still pass unmodified; TexCompilationTests tikz-mode merged-summary compilation facts (GLL, RNGLR) compile the new SPPF wrapping end-to-end with lualatex. FSharpLint 0 warnings on both changed projects; COMMIT_GATE: PASS.

**Findings against the constraint sources:**

- §6 (doc comments) — `wrapTikzAdjustbox` and `sppfSection` carry updated doc comments stating the flag semantics and the SPPF adjustbox wrap.
- §13 (no duplication) — the SPPF reuses the generalized `wrapTikzAdjustbox` (one new parameter, no second wrapper); the four facts share a single helper.
- §15/§16 (test fidelity / Fact vs Property) — the facts assert the exact max-totalheight adjustbox variant in the real end-to-end merged TeX; that variant is unique to the SPPF section, so the assertion is precise and not a tautology. Deterministic, correctly `[<Fact>]`.
- §19 (test coverage) — all four algorithms' SPPF wrapping is now covered (CYK/Valiant via `runWithSummary`, GLL/RNGLR via `runWithSummaryEBNF`).
- §20 (documentation completeness) — `summary-tex.md` signature, flag note, and design-decision rows updated; SPPF moved out of the resizebox scope.
- §21 (book traceability) — rendering-only change to an existing wrapper; no new book algorithm/example involved.

**No blocking findings.** Second pass over the changed surface (wrapper, five call sites, SPPF switch, four facts, docs) found zero additional problems.

---

## Task 271 Review (2026-09-12)

Scope: `src/FLPQ.Printers/SummaryTeX.fs` (`gllStepSection` wraps the step's GSS and RSM TikZ figures, `rnglrStepSection` wraps the step's GSS figure, both via the existing `wrapTikzAdjustbox`; doc comments extended), `data/GLL_step_tikz_template.tex` + `data/RNGLR_step_tikz_template.tex` (redundant `\begin{center}` around the GSS/RSM placeholders removed — the wrapper provides centering), `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` (+2 facts, shared `countOccurrences`/`stepSections` helpers, registry-sourced ANBN grammar/input), `docs/developer/summary-tex.md` (overview bullet, `wrapTikzAdjustbox` design-decision row, corrected stale section-builder signatures).

**Findings resolved this review:**

- §14 (language registry) — the two new facts initially hardcoded the EBNF grammar text and input string inline. The exact grammar exists in the registry as ANBN classic and "a a b b" is one of its accept strings; the facts now derive both from `LanguageRegistry.ANBN` (`df1f809`).
- §23 (naming semantics) — the comment describing the test grammar showed pipe notation while the registry text is newline-separated; reworded to describe the rules without implying a source-text form (`6e3348e`).

**Verified:** CliSummaryTests 20/20, TexCompilationTests 37/37 (the tikz-mode merged-summary compilation facts exercise the new wrapping end-to-end); FSharpLint 0 warnings on both changed projects; COMMIT_GATE: PASS. Empirical rendering check: GLL (4-token input) went from 8 overfull hboxes (17.4pt, GSS figures) to 0; RNGLR from 3 overfull hboxes (112.9pt, GSS figures) to 0; both merged documents compile with lualatex. The mechanism is documented in `summary-tex.md`: LaTeX minipage redefines `\textwidth` to the column width inside its scope, so `max width=\textwidth` shrinks an over-wide figure exactly to fit its column.

**Findings against the constraint sources:**

- §6 (doc comments) — both modified public functions carry extended doc comments stating the TikZ-mode adjustbox wrap and the DOT-mode PDF behavior.
- §13 (no duplication) — the two facts share `countOccurrences`/`stepSections`; the wrap is a one-line composition of the existing helper at each call site, no new wrapper.
- §15/§16 (test fidelity / Fact vs Property) — the facts assert exact per-step adjustbox counts (2 for GLL: GSS+RSM; 1 for RNGLR: GSS) plus global counts (1+2N / 1+N) that pin the head Extended RSM figure; deterministic end-to-end assertions, correctly `[<Fact>]`.
- §19 (test coverage) — `SummaryTeX` keeps its correspondents (`CliSummaryTests`, `TexCompilationTests`); DOT mode stays covered by the unchanged compilation facts.
- §20 (documentation completeness) — `summary-tex.md` is the single doc for this module; overview, design decisions, and signatures updated (the stale `buildContent`/`tableStepSection` signatures and missing `gllStepSection`/`rnglrStepSection`/`sppfSection` entries were corrected as part of the same edit).
- §21 (book traceability) — rendering-only change; no book algorithm/example is involved, consistent with the rest of `SummaryTeX.fs`.

**No blocking findings.** Second pass over the changed surface found only the comment nit listed above; after its fix a final pass found zero problems. Pre-existing note (unchanged by this task): the five older GLL/RNGLR facts in `CliSummaryTests.fs` still use inline `"S -> a S b | eps"` / `"a a b b"` — same registry-eligible data; migrating them is a separate cleanup.

---

## Task 270 Review (2026-09-12)

Scope: `src/FLPQ.Printers/RsmTikz.fs` (bare numeric state content, bare nonterminal edge labels, `double, double distance=1.5pt` on every final state), `src/FLPQ.Printers/RsmDot.fs` (same label semantics; highlighted finals keep `peripheries=2`), tests (`RsmTikzTests.fs` +4 facts including the per-step GLL invariant, new `RsmDotTests.fs` with 3 facts, shared `callLabeledNonterminals` in `TestHelpers.fs`), docs (`rsm-viz.md` label/styling semantics + Design Decisions row), skills (FS0691 named-argument quirk in fsharp-coder).

**Findings resolved this review:**

- §13 (no duplication) — the `callLabeledNonterminals` generator was duplicated between `RsmTikzTests.fs` and `RsmDotTests.fs`. Extracted to `TestHelpers.callLabeledNonterminals`, generic in `'t`/`'nt`, used by both files.
- §15 (test fidelity) — the DOT edge-label assertions matched the loose substring `"label="`, which would also pass for terminal edges with decorated labels. Tightened to the exact `[label="<Nt>"]` form.

**Verified:** RsmTikzTests 10/10, RsmDotTests 3/3; full build green (COMMIT_GATE: PASS). The per-step GLL invariant fact renders every step for four registry grammars with accepted inputs and asserts the double-circled count equals `Set.count ersm.ExtendedRsm.FinalStates` at each step — the ANBN/DoubleA inputs highlight a final state mid-derivation, exercising the S1 fix.

**Findings against the constraint sources:**

- §9 (separation) — all changes stay in the printers and their tests; no algorithm logic touched.
- §14 (language registry) — every new test uses registry grammars (ANBN classic, DoubleA singleRule, Dyck1 ebnfStar, APlus rightRecursive); no manual fixture was needed for the label semantics.
- §19 (test coverage) — both changed renderers have correspondent test files; the TikZ file covers the highlight interplay (lightblue fill + double circle), DOT covers it via `peripheries=2`.
- §20 (documentation completeness) — `rsm-viz.md` documents bare-index state content, bare call-edge names, the always-double finals rule with fill priority, and the book-alignment rationale in Design Decisions.

**No blocking findings.** Second pass over the changed surface (both renderers, both test files, shared helper, docs, skill) found zero additional problems.

---

## Task 269 Review (2026-09-11)

Scope: `src/FLPQ.Cli/Summary.fs` (removed the duplicate SPPF header entry for GLL/RNGLR), `src/FLPQ.Printers/SppfTikz.fs` (math-mode node labels; removed dead `getShape`/`shape`), `src/FLPQ.Printers/RsmTikz.fs` + `AutomatonTikz.fs` (math-mode epsilon edge labels), `src/FLPQ.Printers/SummaryTeX.fs` (parameter rename), tests (`CliSummaryTests.fs` +2 facts, `TexCompilationTests.fs` 4 fixture updates +1 assertion block, `RsmTikzTests.fs` +1 fact, `AutomatonVisualizationTests.fs` 2 strengthened facts), docs (`summary-tex.md`, `automaton-viz.md`, `rsm-viz.md`).

**Findings resolved this review:**

- §15/§23 (test fidelity / naming semantics) — the four merged-summary compilation tests in `TexCompilationTests.fs` still passed `("SPPF", "dot_pdfs/sppf.pdf")` through the header path, a configuration the CLI no longer produces after S1. Fixed: DOT-mode tests write `sppf.dot` so the trailing `sppfSection` renders `dot_pdfs/sppf.pdf`; tikz-mode tests write a real `sppf.tikz.tex` (`Sppf.buildSppfFromExtendedRsm` + `SppfTikz.toTikz`, exactly as the runners do) and pass header lists matching the CLI (empty for GLL tikz mode, RSM-only for RNGLR). The dead stub-PDF machinery was removed from the GLL tikz test.
- §13/§22 (no dead code / clarity) — `SppfTikz.toTikz` carried a `getShape` function and per-node `shape` binding that were never used (node options only carry `as=`/`fill=`, so all nodes rendered as default circles regardless of type). Removed both; no behavior change.
- §23 (naming semantics) — `LrVisuals.RsmSppfPdfs` / `headerSection`'s `rsmSppfPdfs` / local `rsmSppfLines` no longer carry SPPF entries after S1. Renamed to `HeaderVisuals.RsmPdfs` / `rsmPdfs` / `rsmFigureLines` across `Summary.fs`, `SummaryTeX.fs`, `TexCompilationTests.fs`, and the `summary-tex.md` signature.

**Verified:** FLPQ.Printers.Tests 176/176, FLPQ.Cli.Tests 153/153 (0 skipped); regenerated GLL + RNGLR summaries (tikz and DOT modes) — exactly one trailing SPPF section each, math-mode labels (`$S^{\varepsilon}$`, `[s0,v0]$\to$[s4,v6]`), zero `\textbackslash`, both merged documents compile with lualatex.

**Findings against the constraint sources:**

- §9 (separation) — all changes stay in printers/CLI orchestration; no algorithm logic touched.
- §14 (language registry) — new tests reuse registry grammars (`ANBN classic`, `DoubleA singleRule`); the one manual fixture (2-state RSM with an explicit `AEpsilon` edge in `RsmTikzTests.fs`) is justified: EBNF-derived RSMs encode epsilon as "start state is final" and never produce `AEpsilon` edges, so no registry grammar can exercise that rendering arm.
- §19 (test coverage) — every changed module has correspondent tests; the SPPF TikZ compilation test now also asserts the math-mode label properties.
- §20 (documentation completeness) — `summary-tex.md` documents the single trailing SPPF section and the renamed signature; `automaton-viz.md`/`rsm-viz.md` document the math-mode epsilon edge style with the verified rationale (bare `\varepsilon` in text mode renders as an empty box — confirmed empirically with lualatex, not a compile error).

**No blocking findings.** Second and third passes over the changed surface found only the naming leftovers listed above; after their fixes a final pass found zero problems.

---

## Task 268 Review (2026-09-11)

Scope: tools + docs/skills only (no `.fs` changes). `tools/mdformat_tasklog/` (new plugin package — ordered-list numbering preservation, `---` hr renderer, GFM table support with pipe re-escaping), `tools/split_tasks.py` (new repair → mdformat → verify → split pipeline), `tools/common.py` (`tracked_md_files`), `tools/{quality_check,hard_gate}.py` (new Markdown format step + renumbering), `.github/workflows/ci.yml` (Python 3.10 setup + mdformat check), one-time normalization of all 76 tracked `.md` files (including frontmatter fixes in 9 SKILL.md files), AGENTS.md + 7 skill files (multi-file task log scheme, continuous mdformat rule).

**Findings resolved this review:**

- Correctness — `split_tasks.py`'s repair shifted escaped indented code blocks by `C(N) - indent`, landing the first line exactly at column `C(N)`. There, a line after a blank line parses as paragraph text, so an escaped code block under an entry with N >= 100 was silently converted to a paragraph (code formatting lost) while every verification passed (whitespace-only repair ✓, repaired-vs-formatted AST equality ✓, structure ✓). Fixed: indented code blocks are now atomic units like fences, and an escaped one shifts by exactly `C(N)` — in-item code strips `C(N)+4` while top-level code strips 4, so the block stays a code block with byte-identical content (the format stage then fences it). Verified pre/post with parser experiments.
- §22 (clarity) — `chr(10).join` inside an f-string expression in `process_file` (a pre-3.12 backslash workaround); replaced with a local `changed` binding.

**Verified:** seven synthetic scenarios through the full pipeline (escaped code single/multi-line/mixed-indent, in-item code untouched, escaped fence, flat paragraph with blank lines, escaped nested list) — all PASS, code blocks end as fenced blocks with exact content; a 105-entry synthetic log exercises repair + gap separator + split end-to-end (archive + active log well-formed, provenance header + archive-reference line correct); the misaligned-chunk guard correctly BLOCKs a gapped log (first 100 entries spanning 1-101); real files: dry-run PASS with zero repairs, full run byte-identical (idempotent).

**Findings against the constraint sources:**

- §13 (no duplication) — gate step logic is duplicated between `quality_check.py`/`hard_gate.py`, matching the established per-script self-contained pattern; the mdformat file scope is shared via `common.tracked_md_files`. The plugin's pipe re-escaping in `text`/`code_inline` shares `_in_table_cell`; no extraction warranted.
- §20 (documentation completeness) — `split_tasks.py` documented in both `tools/README.md` and `docs/developer/guides/tools.md` (setup, pipeline stages, invariants, output contract); the plugin's module docstring documents the install requirement; AGENTS.md + skills document the continuous mdformat rule and the task-log scheme.
- §22 (clarity) — the script is a straight pipeline of named stages; no nontrivial optimizations.

**No blocking findings.** Second pass over the changed surface (plugin renderers, verification functions, split mechanics, docs/skills consistency: gate step numbering 1-6 across hard_gate/quality_check/CI/skills) found zero additional problems.

---

## Task 267 Review (2026-09-10)

Scope: docs-only. `.opencode/skills/code-review/SKILL.md` (intro line + Prerequisites section). Resolves the open item from the Task 266 report: the skill required quality gates to pass before code review, contradicting the AGENTS.md working loop (step 6 review → step 7 gate). User decision: review-before-gate is canonical.

**Findings against the constraint sources:**

- §13 (one source of truth) — after the fix, all workflow documents agree on the order: AGENTS.md (step 6 → step 7), code-review skill (intro + prerequisites), subtask-loop ("After ALL subtasks are committed" — gate at task completion, no ordering claim vs review), git-workflow (gate before merge, no ordering claim vs review). Verified by grep across AGENTS.md and all skills.

**No blocking findings.** Full pass found zero problems.

---

## Task 266 Review (2026-09-10)

Scope: docs-only. `tasks/tasks1.md` (new — tasks 1-100 restored verbatim from `ac729de~1:tasks/tasks.md` lines 6-560, byte-for-byte verified by diff), `tasks/tasks.md` (entries 263/264 restored verbatim from the committed detailed plans, `[done]`-tagged; entry 266 added; 265 logged — committed on dev per the task-log durability rule), `AGENTS.md` (working-loop step 2a entry gate, step 8 existence check, Git Safety durability paragraph, Project Structure table), `.opencode/skills/git-workflow/SKILL.md` (pre-merge task-log check, pre-commit checklist clarification), `.opencode/skills/planning/SKILL.md` (logged-task prerequisite, mandatory "Task description (verbatim)" section).

**Findings against the constraint sources:**

- §13 (no duplication / one source of truth) — the entry-gate rule appears in AGENTS.md (step 2a), the planning skill (prerequisite), and the git-workflow skill (pre-commit clarification). This is the established AGENTS.md↔skill layering (AGENTS.md is the short entry point, skills hold operational detail; cf. the existing "See the `git-workflow` skill for the full procedure" pattern) — each location states its own gate in one sentence, no procedural block is copied. Not a finding.
- §20 (documentation completeness) — `tasks1.md` is referenced from the `tasks.md` header (pre-existing note, now satisfied) and added to the AGENTS.md Project Structure table. No `docs/` page is required for task-log files (no module docs exist for any `tasks/*.md`).
- §22 (clarity) — n/a (no code).

**Verified:** restored text is byte-for-byte identical to its recovery source (`diff` against `ac729de~1` extraction and against the `b23a09a`/`58c4f40` plan sections); entry numbers 1-100 (except pre-existing gaps 52-55) each appear exactly once as an entry head in `tasks1.md`; 263/264 appear exactly once in `tasks.md` between 262 and 265; the three instruction files are mutually consistent (entry gate ↔ prerequisite ↔ pre-commit rule; step-8 check ↔ pre-merge grep).

**Pre-existing, out of scope (not introduced by this task):**

- The `code-review` skill's "Prerequisites" said quality gates must pass *before* code review, while the AGENTS.md working loop runs review (step 6) before the hard gate (step 7); all recent tasks (263/264/265) followed the AGENTS.md order. **Resolved in Task 267** — user confirmed review-before-gate is canonical; the skill was fixed.

**No blocking findings.** Full pass found zero problems in this task's changes.

---

## Task 265 Review (2026-09-10)

Scope: `src/FLPQ.Printers/RsmTikz.fs` (reworked `extendedRsmToTikz`: pgf-gd component packing `components go down left aligned`, S′-first declaration order, `label=left:` on every block start state), `src/FLPQ.Printers/AutomatonTikz.fs` (new `layeredGraphOptions`, `tikzHeaderWithOptions`; `tikzHeader` now delegates), `src/FLPQ.Printers/SummaryTeX.fs` (`headerSection` gains the Extended RSM head section for GLL/RNGLR Tikz mode), `src/FLPQ.Cli/RnglrRunner.fs` (writes `ext_rsm.tikz.tex` in Tikz mode), `data/tex_tikz_template.tex` (xcolor + lightblue definition), tests (`RsmTikzTests.fs` new; `TexCompilationTests.fs` +1 fact, 2 extended facts; `RnglrRunnerTests.fs` +1 fact), docs (`rsm-viz.md` new; `FLPQ.Printers.md`, `main.md`, `InputGraphDot.md`, `cli.md`, `FLPQ.Cli.md`, `summary-tex.md`).

**Resolved this task:**

- §13 (no duplication) — `extRsmTikzSection` initially inlined the `readIfExists` match; refactored to reuse `headerSection`'s existing `maybe` helper.
- §6 (XML doc comments) — new public `tikzHeaderWithOptions` and its delegator `tikzHeader` carry `///` comments.
- §20 (documentation) — `summary-tex.md` updated: overview bullet for the Extended RSM head figure, corrected `headerSection` signature (was missing `rsmSppfPdfs`/`useTikz`), new design-decision row.

**Findings against the constraint sources:**

- §6 (XML doc comments) — `extendedRsmToTikz`'s comment documents the new stacking, ordering, and label semantics; `layeredGraphOptions` documented.
- §7 (genericity) — `RsmTikz.extendedRsmToTikz` stays generic over `'t`/`'nt`; SummaryTeX change is string-level.
- §13 (no duplication) — the RnglrRunner `ext_rsm.tikz.tex` write mirrors GllRunner's identical call; per-runner artifact writing is the established pattern (each runner writes its own `sppf.tikz.tex`, `input.tikz.tex`, ...). The repeated 3-path template lookup in `TexCompilationTests.fs` is a pre-existing pattern shared by ~10 facts; left as-is.
- §14 (language registry) — `RsmTikzTests` reads grammars from `LanguageRegistry` (`ANBN/classic`, `ArithExpr/leftAssoc`, `Dyck1/ebnfStar`); runner facts use inline grammar text, consistent with every existing runner fact.
- §15/§16 (tests) — all new tests assert real properties: packing keys present + exactly one `\graph`; declared-state set equals `0..StateCount-1`; S′ block declared before all others (character offsets); every block start state carries `label=left:<Nt>`; edge-line count equals the transition-matrix symbol count; highlighted fill present + lualatex compilation; summary content contains `components go down left aligned` before full-document compilation. No stubs or tautologies.
- §19 (coverage) — changed modules keep their correspondents: `RsmTikzTests` + `TexCompilationTests` (Printers), `RnglrRunnerTests` (Cli). Hard gate: all projects 0 failed / 0 skipped, total coverage 91.0% (threshold 90%), lint 0 warnings.
- §20 (documentation) — `rsm-viz.md` created (both renderers, component-packing approach with PGF §28.7 reference, book refs); hub row + `main.md` navigation added; dangling `rsm-dot.md` link in `InputGraphDot.md` fixed; `cli.md` output tables updated — the RNGLR row previously listed `ext_rsm.dot`, which RnglrRunner never writes in any mode (replaced with `ext_rsm.tikz.tex`); GLL row gained the mode annotation.
- §21 (book traceability) — `RsmTikz` module comment keeps `sec:CFPQ_GLL`; `rsm-viz.md` references `sec:CFPQ_GLL` / `sec:CFPQ_RNGLR`.
- §22 (clarity) — straight declaration/edge loops; no nontrivial optimizations.

**Verified end-to-end:** lualatex coordinate extraction on generated figures confirmed vertical stacking in declaration order (S′ on top), left-edge alignment, unchanged intra-block layered LTR layout, and start states at layer 0 even for cyclic blocks (`S -> (a S b)*` has a cycle through the start state). GLL and RNGLR merged summaries compile with the new head section. `lightblue` is not a default xcolor name — `tex_tikz_template.tex` now loads xcolor and defines it identically to `tex_summary_template.tex` (no golden embeds the tikz template, so no goldens changed).

**Pre-existing, out of scope (not introduced by this task):**

- `InputGraphDot.md` also links two nonexistent docs (`gll-step-visualizer.md`, `gss-dot.md`) — same class as the fixed `rsm-dot.md` link; separate documentation task.
- `FLPQ.Printers.md` hub still links several module docs that do not exist (pre-existing, noted in Task 263 review).
- GLL Tikz-mode summary includes both an SPPF PDF (head, via `sppf.dot`) and an SPPF TikZ figure (tail, via `sppfSection`) — pre-existing duplication.

**No blocking findings.** Full pass found zero problems; the three findings above were fixed and committed before this report.

---

## Task 264 Review (2026-09-10)

Scope: `src/FLPQ.Printers/SummaryTeX.fs` (new `wrapTikzAdjustbox`, `stackStepSection` switched from `wrapTikzCenter` to it), `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` (LL inline-TikZ test extended with adjustbox assertions, new SLR1 adjustbox fact), `docs/developer/summary-tex.md`. LL/LR step stack-tree figures in the merged summary are now scaled with `\begin{adjustbox}{max width=\textwidth}` (shrink-only) instead of `\resizebox{0.98\textwidth}{!}` (exact-scale, upscales small figures and scales node text).

**Findings against the constraint sources:**

- §6 (XML doc comments) — new public `wrapTikzAdjustbox` carries a `///` comment documenting the shrink-only semantics.
- §7 (genericity) — `wrapTikzAdjustbox : string -> string`, same signature family as its `wrap*` siblings; no algorithm types involved.
- §13 (no duplication) — `wrapTikzAdjustbox` vs `wrapTikzCenter`: 5-line wrappers differing in the scaling construct (`adjustbox` max-width vs `resizebox` exact-width). Intentional, documented divergence: the task scopes adjustbox to LL/LR step figures only; the LR automaton, GLL input string, and SPPF keep resizebox (GLL-wide migration is the separate open task 244). Merging them into one parameterized wrapper would couple four call sites with two different scaling policies.
- §15/§16 (tests) — the extended LL fact asserts both presence of `\begin{adjustbox}{max width=\textwidth}` and absence of `\resizebox{0.98\textwidth}` in the real end-to-end merged TeX (LL summary has no other TikZ figures, so absence is precise); the new SLR1 fact asserts adjustbox presence (presence only — the LR automaton section legitimately keeps resizebox). Existing lualatex compilation facts for LL/SLR1 summaries verify the new wrapper compiles. No stubs or tautologies.
- §19 (coverage) — changed module keeps its correspondent: `CliSummaryTests` (end-to-end, 16 Summary-category tests green).
- §20 (documentation) — `summary-tex.md`: abstract helper list, function signatures, and two design-decision rows (adjustbox rationale + scope; `stackStepSection` row updated to mention the adjustbox wrap).
- §21 (book traceability) — rendering module: no direct book reference, consistent with the FLPQ.Printers hub convention.
- §22 (clarity) — wrapper is a 5-line list concatenation identical in shape to `wrapTikzCenter`; no optimization.

**Verified end-to-end:** LL summary for `example_grammar.bnf` / `example_input_an_bn.txt` — all 16 step figures wrapped in adjustbox, zero resizebox; SLR1 summary — 10 step figures in adjustbox, exactly one resizebox (LR automaton). Both merged TeX files compile with lualatex.

**No blocking findings.** Full pass found zero problems.

---

## Task 263 Review (2026-09-09)

Scope: `src/FLPQ.Printers/TeXRenderer.fs` (new `escapeMath`, `inputRow` now escapes each cell), `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` (2 new facts, 2 updated facts), `tests/FLPQ.Cli.Tests/CliSummaryTests.fs` (2 new end-to-end compilation facts, `mergedTexPath` helper), `docs/developer/visualization-types.md`. Fixes LL/LR merged-summary lualatex failures: the `$` end-of-input marker appended by LL/LR runners was emitted raw inside the math-mode `pNiceMatrix` input row.

**Resolved this task:**

- §13 (no duplication) — `assertMergedTexCompiles` duplicated the merged-TeX path construction and existence check from `assertMergedTexExists`; extracted `mergedTexPath`, `assertMergedTexCompiles` now reuses `assertMergedTexExists` (commit 53292a7).

**Findings against the constraint sources:**

- §6 (XML doc comments) — new public `TeXRenderer.escapeMath` carries a `///` comment; `inputRow`'s comment updated to document the escaping behavior.
- §7 (genericity) — `escapeMath : string -> string` is a string-level operation; `inputRow` stays generic over `'t`.
- §13 (no duplication) — `escapeMath` (math mode) vs `AutomatonTikz.escapeLatex` (text mode): 7 of 9 replacements coincide, but the two are intentionally different at the math/text boundary (`^` → `\text{\^{}}` in math vs `\^` in text; `~` untouched in math). Extracting a shared core would couple `TeXRenderer` to `AutomatonTikz` and parameterize 8+ call sites for a 9-line helper. Accepted as deliberate per-boundary escapers, consistent with the existing pattern (`DerivationTreeDot.escapeLabel`, `BasicSppfDot.escapeLabel`, `SppfDot.escapeLabel` are separate one-liners per DOT boundary).
- §14 (language registry) — S2/S3 tests read grammars from `LanguageRegistry` (`Dyck1/ambiguousEps`, `APlus/rightRecursive`) via the existing `TestGrammarFiles`/registry helpers; no inline grammar text. The S1 escaper test uses raw terminal strings (no grammar involved).
- §15/§16 (tests) — 4 new facts assert real properties: `escapeMath` mapping per special character, `inputRow`+EOI lualatex compilation at every position, and full-pipeline merged-summary compilation for LL and SLR1. Both S2-updated tests and both S3 tests were verified to FAIL against the pre-fix `inputRow` (temporarily reverted) — genuine regression coverage, not tautologies.
- §19 (coverage) — changed module keeps its correspondent: `TexCompilationTests` (escaper + input-row compilation), `CliSummaryTests` (end-to-end). Full Printers suite green: 169 tests.
- §20 (documentation) — `visualization-types.md` TeXRenderer section updated (`escapeMath` entry, escaping note; also corrected the `inputRow` signature, previously documented as `Symbol`-based but actually `Terminal`-based).
- §21 (book traceability) — rendering module: no direct book reference, consistent with the FLPQ.Printers hub convention.
- §22 (clarity) — escaper is a straight chain of 9 `Replace` calls; no optimization.

**Pre-existing, out of scope (not introduced by this task):**

- GLL merged summary for input `a b a b a b` fails with `TeX capacity exceeded [number of strings=476553]` — 31 repeated 50×50 path-index matrices exhaust TeX's string pool; still not finished after >10 min with `\maxstrings=1000000`. Separate performance issue, reported to the user.
- `FLPQ.Printers.md` hub links to 11 module docs that do not exist (`tex-renderer.md`, `symbol-tex.md`, `matrix-tex.md`, …); those modules are documented in other pages (e.g. TeXRenderer in `visualization-types.md`). Pre-existing since the hub table was written; fixing all links is a separate documentation task.

**No blocking findings.** Second full pass found zero new problems; the one finding (test-helper duplication) was fixed and committed before this report.

---

## Task 262 Review (2026-09-09)

Scope: `src/FLPQ.Printers/DerivationTreeTikz.fs` (new), `src/FLPQ.Printers/{VisualizationTypes,LLStepVisualizer,LRStepVisualizer,SummaryTeX,ExternalTools}.fs`, `src/FLPQ.Cli/{Helpers,LLRunner,LRRunner,Program}.fs`, both fsproj files, `data/tex_{tikz,summary}_template.tex`, tests (`DerivationTreeTikzTests.fs` new, golden/compilation/runner/summary test updates, 4 new `.tikz` goldens), docs (`derivation-tree-viz.md` rewritten, `cli.md`, `summary-tex.md`, `visualization-types.md`, `external-tools.md`, `test-categories.md`, `main.md`, `FLPQ.Printers.md`). Adds TikZ rendering of LL/LR per-step stack-trees via graphdrawing `layered layout` with a native `{ [same layer] ... }` frontier constraint (the equivalent of DOT `{rank=same}`), makes TikZ the default per-step output with `--use-dot` opt-out, and embeds the step pictures inline in TikZ-mode summaries.

**Resolved this task:**

- §13 (no duplication) — pre-existing duplicate `InputGraphDot` row in the `FLPQ.Printers.md` hub module table (introduced by task 198); removed (commit 3ff24a6).

**Findings against the constraint sources:**

- §6 (XML doc comments) — `DerivationTreeTikz.toTikzWithLLStack` / `toTikzWithLRStack` and `ExternalTools.compileTexStringWithTemplateLog` carry `///` comments; `compileTexStringWithTemplate` is now a thin wrapper over the `...Log` variant (no duplicated process logic).
- §7 (genericity) — both renderers stay generic over `'t`/`'nt`; test helpers instantiate with `string` (allowed for unit tests).
- §8 (non-empty collections) — `stack: LLStackLeaf list` / `LRStackFrame list` correctly use plain lists: an empty frontier is a legitimate state (e.g., LR step 0, finished LL parse).
- §9 (separation) — parsers produce `LLParsingStep`/`LRParsingStep` data; all rendering stays in `DerivationTreeTikz`; the CLI only writes files.
- §13 (no duplication) — `DerivationTreeTikz` mirrors `DerivationTreeDot`'s structure (same pre-order numbering, same chain logic) — the established paired DOT/TikZ renderer pattern (`AutomatonDot`/`AutomatonTikz`, `GssDot`/`GssTikz`); the only deliberate divergence is the reversed LR chain edge direction, documented in the module and in `derivation-tree-viz.md`. `GoldenHelpers.combineSteps` extracted so DOT/TikZ combinators share one implementation.
- §14 (language registry) — all new tests read grammars from `LanguageRegistry` (`Dyck1`, `APlus`, `ArithExpr`); no inline grammar text.
- §15/§16 (tests) — 8 new `DerivationTreeTikzTests` facts assert real properties (structural markers, DOT/TikZ node-ID equivalence, empty-stack behavior, lualatex compilation, and same-layer coordinate extraction asserting one-level placement plus left-to-right tree/stack order); no stubs or tautologies.
- §19 (coverage) — every changed module keeps its correspondent: `DerivationTreeTikzTests`, `LL/LRVisualizerTests` (new `TreeAndStackTikz` fields), `LL/LRStepsGoldenTests` (4 new TikZ goldens), `TexCompilationTests` (all-step TikZ compilation), `LL/LRRunnerTests` (both output modes), `CliSummaryTests` (inline TikZ vs PDF includes). Full Printers suite green: 167 tests.
- §20 (documentation) — `derivation-tree-viz.md` rewritten for both renderers (same-layer mechanism, chain edge direction, template/library requirements); `cli.md` documents the new default and LL `--use-dot`; `summary-tex.md`, `visualization-types.md`, `external-tools.md`, `test-categories.md`, `main.md` updated.
- §21 (book traceability) — rendering module: no direct book reference, consistent with the FLPQ.Printers hub convention.
- §22 (clarity) — no nontrivial optimizations; the double `List.rev` in `toTikzWithLRStack` is intentional (cluster order vs edge direction) and explained in the doc comment.

**No blocking findings.** Second full pass found zero new problems; the one finding (duplicate doc row, pre-existing) was fixed and committed before this report.

---

## Task 261 Review (2026-09-08)

Scope: `src/FLPQ.Languages/{Rnglr,RnglrTypes}.fs`, `src/FLPQ.Printers/{RnglrStepVisualizer,SummaryTeX}.fs`, `tests/FLPQ.Languages.Tests/RnglrTests.fs`, `tests/FLPQ.Cli.Tests/{CliSummaryTests,RnglrRunnerTests}.fs`, `docs/developer/{rnglr,summary-tex}.md`. Tracks per-step the GSS vertices where passing-reduction handling triggers (a new edge added from a vertex with non-empty stored states — the book's AddEdge trigger, sec:CFPQ_GLR), highlights them orange in DOT/TikZ step visualizations (same color as GLL stored pops, task 259), and adds the color to the RNGLR summary legend.

**Resolved this task:**

- §14 (language registry) — the S3 runner tests hardcoded the triggering grammar text inline although it is the registry entry `Dyck1/ambiguousWithConcat` (`Grammars.[1]`); now read from `LanguageRegistry.Dyck1.Grammars.[1].Text`, with the scenario input extracted to a documented `passingReductionInput` constant (commit 063803c).

**Findings against the constraint sources:**

- §6 (XML doc comments) — new `PassingReductionVertices` field carries a `///` comment; no other new public API.
- §7 (genericity) — tracking stays generic over `'t`/`'nt`; vertex indices are `int` like every other GSS-index set on the step record.
- §8 (non-empty collections) — `PassingReductionVertices: Set<int>` is correct: empty is a legitimate state (step 0, levels without triggers).
- §9 (separation) — `Rnglr.fs` records vertex indices; all orange rendering stays in the printers via the existing `storedPopVertices` parameter of `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` (no renderer changes needed).
- §13 (no duplication) — the step-directory iteration pattern in the new runner tests mirrors the task 259 GLL stored-pops tests; per-runner-file self-containment is the established convention, no shared helper extracted.
- §15/§16 (tests) — four new `[<Fact>]` tests assert real properties (orange present in some non-init step's `gss.dot`/`gss.tikz.tex`, absent at step 0, legend row + `\colorbox{orange!30}` in merged TeX); no stubs or tautologies.
- §19 (coverage) — every changed module keeps its correspondent (`RnglrTests.RnglrPassingReductions`, `CliSummaryTests`, `RnglrRunnerTests`); full suite green: 907 tests, zero regressions; step-0 goldens byte-identical.
- §20 (documentation) — `docs/developer/rnglr.md`: `RnglrParsingStep` field + explanation, level-loop "Passing-reduction tracking" bullet, design-decision row for the shared orange color; `docs/developer/summary-tex.md`: both legend rows documented.
- §21 (book traceability) — tracking comment in `Rnglr.fs` cites sec:CFPQ_GLR AddEdge; docs cite sec:CFPQ_GLR.
- §22 (clarity) — tracking is two `if not (Set.isEmpty consumedStates)` checks at the existing consumption points; no optimization, no behavior change (algorithm output unchanged).

**No blocking findings.** Second full pass found zero new problems; the one finding (registry usage) was fixed and re-verified before this report.

---

## Task 260 Review (2026-09-07)

Scope: `src/FLPQ.Languages/{Rnglr,RnglrTypes}.fs`, `src/FLPQ.Printers/{RnglrStepVisualizer,SummaryTeX}.fs`, `src/FLPQ.Cli/{Helpers,RnglrRunner}.fs`, `data/RNGLR_step{,_tikz}_template.tex`, `tests/FLPQ.Printers.Tests/{RnglrStepVisualizationTests,TexCompilationTests}.fs` + fsproj + GoldenData, `tests/FLPQ.Cli.Tests/RnglrRunnerTests.fs`, `docs/developer/rnglr.md`. Replaces the descriptor-queue + recursive cascade with the canonical level-based driver (shift, then reduce to fixpoint, per input position), removes `RnglrDescriptor` entirely, and rebuilds the step visualization as a two-column layout.

**Resolved this task:**

- §6 (XML doc comments) — `Rnglr.buildPathIndex`, `buildPathIndexWithSteps`, `RnglrStepVisualizer.{RnglrVisualizationStep,renderStep,renderSteps}`, and `SummaryTeX.rnglrStepSection` had no `///` comments (pre-existing gap in files touched by this task); added.
- §21 (book traceability) — the level loop now carries an explicit `sec:CFPQ_GLR` / `sec:CFPQ_RNGLR` reference; public entry points reference `sec:CFPQ_RNGLR`.
- §23 (signature hygiene) — `renderStep`/`renderSteps` took unused `lrStateCount` and `vertexCount` parameters (pre-existing); removed from the signatures and all four call sites, dropping the dead bindings.
- Stale build artifact — a gitignored old copy of the three-column `RNGLR_step_template.tex` sat in the test output dir and shadowed `data/`, so the merged-summary lualatex test compiled unfilled `__DESCRIPTORS_TABLE__` placeholders; added `PreserveNewest` Content items for both RNGLR step templates to `FLPQ.Printers.Tests.fsproj` (matching every other template) and deleted the stale file.
- Pre-existing fsharplint warnings blocking the pre-merge full-solution lint — 4 warnings in `tests/FLPQ.LinearAlgebra.Tests/MatrixTests.fs` mxmi tests (FL0034 lambda-removal x2, FL0065 identity multiplication x2); fixed with `Matrix.create 2 3 (+)` and simplified expected dot products. Full-solution lint now reports `Summary: 0 warnings`.

**Findings against the constraint sources:**

- §7 (genericity) — driver, types, and visualizer stay generic over `'t`/`'nt`; no `string` hardcoding.
- §8 (non-empty collections) — `RnglrGSS.verticesAt` returns `int list`; empty is a legitimate snapshot state, so plain `list` is correct.
- §9 (separation) — `Rnglr.fs` produces step data (`RnglrParsingStep`); all rendering stays in `RnglrStepVisualizer.fs`/`SummaryTeX.fs`.
- §13 (no duplication) — `shiftNode`/`reduceAtLevel`/`processReduction` share no copied logic; the DOT/TikZ template pair mirrors the established GLL pair pattern.
- §15/§16 (tests) — remaining RNGLR tests are goldens, lualatex compilation, and property/equivalence tests; no stubs or tautologies. The two descriptor golden tests were deleted with their data in S3.
- §18 (equivalence) — `CrossParserEquivalenceTests` SCC-count invariants and CYK/GLL/RNGLR acceptance equivalence pass unchanged; PathIndex is byte-identical to the pre-task implementation (full suite green at S1).
- §19 (coverage) — every changed module keeps its test correspondent (`RnglrTests`, `RnglrStepVisualizationTests`, `TexCompilationTests`, `RnglrRunnerTests`).
- §20 (documentation) — `docs/developer/rnglr.md` rewritten to the level-based structure: abstract, algorithm (level loop, rounds, passing continuations), `RnglrParsingStep` type, `verticesAt` in the GSS table, corrected `buildPathIndex` signature (`ExtendedRSM`, not the stale `RSM`), acceptance via `PathIndex.isAccepted` (the doc listed a non-existent `Rnglr.isAccepted`), design-decision rows replaced, `sec:CFPQ_GLR` added to book references.
- §22 (clarity) — no recursion, no depth guard; the driver is a plain level loop with documented phases.

**No blocking findings.** Second full pass over the RNGLR changes found zero new problems; the pre-merge full-solution lint surfaced only the pre-existing MatrixTests warnings above, now fixed.

---

## Task 257 Review (2026-09-03)

Scope: `src/FLPQ.Languages/Valiant.fs`, `src/FLPQ.Printers/ValiantTeX.fs`, `tests/FLPQ.Languages.Tests/ValiantTests.fs`, `tests/FLPQ.Printers.Tests/GoldenData/valiant_modified_grammar1_ab.tex`, `docs/developer/valiant.md`. Aligns the modified Valiant trace to the full power-of-two padded grid, restructures it into an init step + one step per layer, and fixes layer/changed-cell coloring.

**Resolved this task:**

- §13 (no duplication) — `diffCells` and the new `diffWholeTable` shared a "new entries appeared" predicate; extracted `hasNewEntries` (`Valiant.fs`).
- §13 (no duplication) — `sppfModifiedStepToTeX` and `sppfModifiedStepToTeXAsNt` each duplicated the LayerForward/LayerBackward block+highlight rendering (4 near-identical branches); extracted `sppfModifiedStepToTeXWith`, making the two public functions thin wrappers (`ValiantTeX.fs`, −196 lines).

**Findings against the constraint sources:**

- §6 (XML doc comments) — no new public API; `sppfModifiedStepToTeXWith` is `private`.
- §7 (genericity) — all changes stay generic over `'nt`; no `string` hardcoding.
- §9 (separation) — `Valiant.fs` produces trace data (steps, changed cells); rendering stays in `ValiantTeX.fs`.
- §14 (language registry) — new tests use `LanguageRegistry.Dyck1` and `GenToArbitrary.AbString`; no inline grammar/string literals.
- §15/§16 (tests) — three `[<Property>]` tests assert real invariants (power-of-two alignment, init+one-step-per-layer, changed-cells == final-table computed cells), not tautologies.
- §18 (equivalence) — the alignment property cross-checks modified vs classical Valiant trace dimensions; the changed-cells property cross-checks trace against `parseModifiedWithSppfInfo`.
- §19 (coverage) — `parseModifiedWithSppfTrace`/`parseModifiedWithSppfInfo` trace paths are now directly exercised by the new tests.
- §20 (documentation) — `docs/developer/valiant.md` trace description and Design Decisions updated (init step + per-layer steps, `red!10` layer blocks, yellow changed cells).

**No blocking findings.** Final computed tables remain byte-identical to CYK/classical Valiant (verified by `checkCykValiantEquivalence`).

---

## Task 256 Review (2026-09-02)

Scope: `src/FLPQ.Printers/BasicSppfTikz.fs`, `tests/FLPQ.Printers.Tests/BasicSppfDotTests.fs`, `docs/developer/basic-sppf-viz.md` (new), `docs/main.md`. Changes `BasicSppfTikz.toTikz` to render nonterminal names and terminal labels in LaTeX math mode (`$N_1$`, `$a_{l,r}$`); DOT renderer untouched.

**Findings against the constraint sources:**

- §6 (XML doc comments) — `toTikz` retains its `///` doc comment; no new public API introduced.
- §7 (genericity) — `toTikz` stays generic over `'t`/`'nt`; no `string` hardcoding.
- §9 (separation) — rendering change confined to `FLPQ.Printers`; no algorithm or I/O logic touched.
- §13 (no duplication) — math-mode label construction is inline in the existing single `match`; no logic copied.
- §19 (test coverage) — `simple SPPF tikz compiles` now asserts the math-mode labels (`$S$ [0,2]`, `$a_{0,1}$`, `$b_{1,2}$`) and still compiles with lualatex; DOT assertions unchanged.
- §20 (documentation) — created `basic-sppf-viz.md` (referenced from `FLPQ.Printers.md` but previously missing) documenting both renderers and the math-mode convention; added navigation link in `docs/main.md`.
- §21 (book traceability) — `def:basicSPPF` reference preserved in the source.

**No blocking findings.** Epsilon/production labels are intentionally left as plain (unchanged) rendering — the task scopes math mode to nonterminal names and terminals only.

---

## Task 255 Review (2026-09-02)

Scope: `src/FLPQ.Languages/{Grammar,Cyk,Valiant,BasicSppf}.fs`, `src/FLPQ.Printers/GrammarTeX.fs`, `tests/FLPQ.Languages.Tests/{GrammarTests,CykTests}.fs`, `tests/FLPQ.Printers.Tests/GoldenData/{cyk_grammar7_xplusx,valiant_grammar1_abab,valiant_modified_grammar1_ab}*.tex`, and `docs/developer/{grammar,grammar-tex,cyk,valiant,sppf-parsing-table}.md`. Makes `SppfParsingEntry.ProdIdx` a 1-based canonical production number (start-nonterminal-first order) shared by CNF rendering, CYK/Valiant table cells, and Basic SPPF via a number→production map.

**Resolved this task:**

- §22 (clarity) — the initial CYK change recomputed `Grammar.numberedRules cnf` inside the diagonal and span loops; hoisted to a single `let numbered` per run in `cykSppfCore` and `parseWithSppfTrace`.

**Findings against the constraint sources:**

- §13 (no duplication) — the start-first reordering + 1-based numbering was extracted into `Grammar.numberedRules`/`productionNumberMap`; `GrammarTeX.renderGrammar` now consumes `Grammar.numberedRules` instead of its own local reordering.
- §6 (XML doc comments) — both new functions carry `///` comments describing ordering and 1-based semantics.
- §7 (genericity) — `numberedRules`/`productionNumberMap` are generic over `'t`/`'nt`; no `string` hardcoding.
- §9 (separation) — numbering lives in `FLPQ.Languages`; rendering stays in `FLPQ.Printers`; no TeX/IO added to algorithm modules.
- §12 (compile-time safety) — `BasicSppf.fromParsingTable` uses `Map.find` (valid by construction); `validateProductionChildren` uses `Map.tryFind` for defensive validation of arbitrary SPPF data.
- §19 (test coverage) — `NumberingTests` covers ordering, rule completeness, map consistency, and CNF-renderer agreement; the CYK aplus test was updated to the map lookup.
- §20 (documentation) — `grammar.md`, `grammar-tex.md`, `cyk.md`, `valiant.md`, `sppf-parsing-table.md` updated to the 1-based `productionNumber` semantics.
- §23 (naming) — the record field `ProdIdx` retains its historical name although it now holds a 1-based number; documented in `sppf-parsing-table.md` as "1-based canonical productionNumber". Non-blocking.

**No blocking findings.** Cross-algorithm equivalence (CYK ≡ Valiant ≡ modified Valiant SPPF) is preserved by the shared numbering, confirmed by the existing `SppfPropertyTests` / `TestHelpers.checkCykValiantEquivalence` passing.

---

## Task 254 Review (2026-09-01)

Scope: `src/FLPQ.Printers/GrammarTeX.fs`, `tests/FLPQ.Printers.Tests/GoldenData/grammar{1,7,9}_{bnf,cnf}_numbered.tex`, `docs/developer/grammar-tex.md`. Changes `grammarToTeXWithNumbers` from `align*` with `[idx]` prefixes to `alignat*{3}` with 1-based `N)` numbers.

**Findings: none.** Review of the change against the constraint sources:

- §13 (no duplication) — the LHS/RHS content computation shared by both numbered and unnumbered paths was extracted into `renderRuleContent` (`GrammarTeX.fs:10-25`), so the two render branches do not copy the RHS-mapping logic.
- §6 (XML doc comments) — `grammarToTeX` and `grammarToTeXWithNumbers` retain doc comments; the numbered function's comment now states `alignat*` / 1-based.
- §7 (genericity) — still generic over `'t`/`'nt`; no `string` hardcoding introduced.
- §9 (separation) — rendering remains in `FLPQ.Printers`; no algorithm or I/O logic added.
- §19 (test coverage) — both render branches are exercised: `grammarToTeX` by CLI/summary tests, `grammarToTeXWithNumbers` by the 6 `GrammarTeXGoldenTests` numbered cases (all passing).
- §20 (documentation) — `grammar-tex.md` abstract, Output Format, and Design Decisions updated to match the new output.

---

## Task 253 Review (2026-09-01)

Scope: `src/FLPQ.Languages/Cyk.fs`, `src/FLPQ.Languages/Valiant.fs`, `src/FLPQ.Cli/*`, `src/FLPQ.Printers/{ParsingTableTeX,CykTeX,ValiantTeX}.fs`, and CYK/Valiant tests. Covers SPPF unification (with/without SPPF) and the `--no-sppf-table` rendering flag.

**Resolved this task:**

- **D6** — Valiant `complete`/`compute` duplicated for two type variants. Now a single SPPF computation path; the non-SPPF public API (`parse`, `parseWithTable`, `parseWithTrace`, `parseModified*`) are thin wrappers over it (`Cyk.fs:211-242`, `Valiant.fs:601-658`).
- **A5** (partially) — `Valiant.fs` reduced from ~912 to 658 lines by removing the duplicated non-SPPF computation path.

**New tests added this task (test-coverage restore):**

- `CykTests.WrapperEquivalenceTests` — property test verifying `Cyk.parseWithTrace` equals the nonterminal projection of `parseWithSppfTrace`, with identical highlights.
- `ValiantTests.TraceWrapperEquivalenceTests` — property tests for `Valiant.parseWithTrace` and `Valiant.parseModifiedWithTrace` (previously untested wrappers), verifying table projection plus preserved `Target`/`Multiplied`/`ChangedCells`/layer fields.
- `TestHelpers.checkCykValiantEquivalence` — restored explicit SPPF-vs-non-SPPF acceptance and table equivalence checks (wrapper-conversion oracle).

**No blocking findings** in the reviewed changes. The non-SPPF rendering functions (`CykTeX.tableToTeX`, `ValiantTeX.stepToTeX`/`modifiedStepToTeX`) remain as book-reference public API, no longer called by the CLI, which now renders via `sppfTableToTeX*`/`sppfStepToTeX*` (including the `--no-sppf-table` `*AsNt` variants).

---

## Scope

Reviewed all 62 `.fs` source files (`src/`) and 49 `.fs` test files (`tests/`).
Report generated: 2026-08-06. Fresh full-repo review — all prior reports verified and consolidated.
**Status: 56 OPEN issues. FSharpLint: 44+ FL0051 warnings (maxNumberOfItemsInTuple — pre-existing 3-to-6-tuples in ~8 files needing named types, lint timed out before finishing). §20 (documentation completeness) not fully verified.**

Each issue was verified against the current codebase. Issues from prior reports that are now fixed (R1, R3–R6, N7, R7, 4.4/N10, 4.6, 4.8, 4.9, N6, 5.2) or obsolete (N9/GllCykEquivalence) have been removed.

---

## 1. Duplication

| # | Description | Source | Severity | File:Line |
| --- | --- | --- | --- | --- |
| D1 | `addToIndex` — structurally identical local wrapper function in `Gll.fs:111–118` and `Rnglr.fs:104–111`. Both delegate to `PathIndex.addWithTracking`. | §13 | Medium | `Gll.fs:111`, `Rnglr.fs:104` |
| D2 | Twin Grammar test modules — `GllTests.fs` and `RnglrTests.fs` share identical module structure (GllSharedAcceptance, GllTreeYield, GllPropertyTreeYield, etc.). Most logic is shared via `ParsingTestCases` and `TestHelpers`, but near-identical boilerplate remains. | §13 | Medium | `GllTests.fs`, `RnglrTests.fs` |
| D3 | `regexToDfa` (`RPQTests.fs:237`) and `buildBlockDfa` (`EbnfParser.fs:319`) — both call `Regexp.buildDfaFromRegex` with slight parameter wrappings. Conceptual duplication. | §13 | Medium | `RPQTests.fs:237`, `EbnfParser.fs:319` |
| D4 | `escapeLabel` — identical `s.Replace("\"", "\\\"")` defined in 3 files: `DerivationTreeDot.fs:8`, `SppfDot.fs:9`, `BasicSppfDot.fs:10`. Canonical source is `DerivationTreeDot.fs`. | §13 | Medium | `SppfDot.fs:9`, `BasicSppfDot.fs:10` |
| D5 | `countScc` / `countNonTrivialScc` — near-identical Tarjan's SCC implementations in `BasicSppf.fs:254–336`. ~80 lines duplicated; differ only in SCC-counting condition. | §13 | Medium | `BasicSppf.fs:254,294` |
| D6 | Valiant algorithm duplicated for two type variants in `Valiant.fs`. `complete`/`completeSppf`, `compute`/`computeSppf`, etc. share identical recursive structures (~350 near-identical lines). | §13 | Medium | `Valiant.fs` |
| D7 | `ValiantRunner.runValiant` / `ValiantRunner.runValiantModified` share ~80% identical body (grammar parsing, tokenization, I/O, SPPF construction, step iteration). | §13 | Low | `ValiantRunner.fs:9,53` |

## 2. Architecture

| # | Description | Source | Severity | File:Line |
| --- | --- | --- | --- | --- |
| A1 | Missing explicit `ProjectReference` to `FLPQ.GraphAnalysis` in `FLPQ.Printers.fsproj` and `FLPQ.Cli.fsproj`. Both projects use `open FLPQ.GraphAnalysis` / `FLPQ.GraphAnalysis.Graph.*` but rely on transitive propagation through `FLPQ.Languages`. | §9 | Medium | `.fsproj` files |
| A2 | `GraphReader.fs` in `FLPQ.RPQ` — parses graph files into `NFA<string, int>` (type from `FLPQ.Languages`). Could belong in `FLPQ.GraphAnalysis` or `FLPQ.Languages`. | §9,§10 | Low | `GraphReader.fs` |
| A3 | `System.IO` + file-read convenience functions in core algorithm modules: `Grammar.fs:4,118`, `EbnfParser.fs:4,301,391`, `GraphReader.fs:3,85`. Thin wrappers alongside pure algorithm code — acceptable but worth noting. | §9 | Low | Multiple files |
| A4 | `LRTableTeX.allActionsFor` (`LRTableTeX.fs:15–31`) reconstructs conflicts by scanning both `table.Action` map and `table.Conflicts` list independently, then concatenating. Visualization may show actions the parser never takes. | §22 | Low | `LRTableTeX.fs:15` |
| A5 | `Valiant.fs` at 912 lines — oversized. SPPF variant functions could be split into a separate file or unified with a generic representation. | §10 | Low | `Valiant.fs` |

## 3. Naming and Types

| # | Description | Source | Severity | File:Line |
| --- | --- | --- | --- | --- |
| T1 | `VisualizationStep` record (`VisualizationTypes.fs:7`) uses hardcoded `string` fields: `TreeAndStack: string`, `Input: string`. Serialization bridge type — acceptable but breaks genericity chain. | §7,§23 | Low | `VisualizationTypes.fs:7` |
| T2 | `DerivationTree.Node` (`DerivationTree.fs:7`) uses `list` instead of `NonEmptyList` for children. Type-level invariant: a non-leaf node must have at least one child. | §8,§12 | Low | `DerivationTree.fs:7` |

## 4. Genericity (Hardcoded to `string`)

| # | Description | Source | Severity | File:Line |
| --- | --- | --- | --- | --- |
| G1 | `RsmToGrammar.convert` — signature `RSM<string, string> -> Grammar<string, string>`. Uses `sprintf` for nonterminal names. | §7 | Low | `RsmToGrammar.fs:23` |
| G2 | `EbnfParser.parseEbnf` returns `(Nonterminal<string> * Regexp<string, string>) list`. `RsmBuilder.buildRSM` takes `Map<Nonterminal<string>, Regexp<string, string>>`. Inherently text-bound. | §7 | Low | `EbnfParser.fs:298,377` |
| G3 | `Grammar.parseGrammar` returns `Grammar<string, string>`. Uses `System.Char.IsUpper`. Acceptable for text parser. | §7 | Low | `Grammar.fs:103` |
| G4 | `RsmToGrammar.ntName` returns `string`, not generic `'nt`. Input is `Nonterminal<string>`. | §7 | Low | `RsmToGrammar.fs:11` |
| G5 | `RsmBuilder` module (in `EbnfParser.fs:317–392`) hardcoded to `string`. No generic variants. | §7 | Low | `EbnfParser.fs:317` |

## 5. Test Coverage Gaps

| # | Description | Source | Severity | File:Line |
| --- | --- | --- | --- | --- |
| T1 | RPQ generators hardcode alphabet as `["a"; "b"]` in multiple generator types (`Generators.fs:127,191,268,348`). No tests with larger alphabets, numeric labels, or special characters. | §17 | Low | `Generators.fs` |
| T2 | `GllPropertyTreeYield` module (`GllTests.fs:88–122`) has no `[<Properties(Arbitrary=...)>]`. Uses FsCheck's default `Arbitrary<string>` which generates random unicode — most strings are trivially rejected. Compare with `RnglrPropertyTreeYield` which uses `GenToArbitrary.AbString`. | §16,§17 | Low | `GllTests.fs:88` |
| T3 | `GraphTests.fs:253` — `[<Property>]` test `fromEdges produces correct dimensions` takes `()` and uses only hardcoded data. Should be `[<Fact>]`. | §16 | Low | `GraphTests.fs:253` |
| T4 | `PathIndex.fs` (351 lines) — no dedicated unit test file. Only tested indirectly through `PathIndexTeXTests.fs` (golden) and `GllTests.fs`/`RnglrTests.fs` (acceptance). Missing direct tests for `GridIndex`, `RangeKey`, `PathIndex` operations. | §19 | Medium | `PathIndex.fs` |
| T5 | `RnglrTableTeX.fs` (276 lines) — no dedicated unit test file. Only tested indirectly through `RnglrStepVisualizationTests.fs` golden tests. | §19 | Medium | `RnglrTableTeX.fs` |

## 6. Documentation Gaps

| # | Description | Source | Severity | File:Line |
| --- | --- | --- | --- | --- |
| D1 | Missing XML doc comments on all 13 public functions in `FLPQ.Cli/Helpers.fs`. | §6 | High | `Helpers.fs` |
| D2 | Missing XML doc comments on all 6 CLI runner entry-point functions (`runCyk`, `runValiant`, `runGll`, `runLL`, `runLR`, `runRnglr`). | §6 | Medium | `*Runner.fs` |
| D3 | Missing XML doc comments on `FLPQ.Cli/Summary.fs` (`algorithmToKind`, `algorithmLower`, `buildSummary`) and `Program.fs` (`runCli`). | §6 | Low | `Summary.fs`, `Program.fs` |
| D4 | Missing XML doc comments on various `FLPQ.Printers/` rendering functions (AutomatonDot, CykTeX, step visualizers, SppfDot, GssDot, MatrixTeX, LLTableTeX, LRTableTeX, SummaryTeX). | §6 | Low | Multiple files |
| D5 | Missing book reference comments in 8 algorithm files: `Cyk.fs`, `Valiant.fs`, `LLParser.fs`, `LRParser.fs`, `Rnglr.fs`, `FirstFollow.fs`, `Automaton.fs`, and CNF/Grammar transformation functions in `Grammar.fs`. Every implementation must be traceable to a specific algorithm or example in the book. | §21 | Medium | Multiple files |

## 7. Language Registry Violations

> **Constraint source:** §14 — language registry must be single source of truth for all grammars, RSMs, accept/reject strings, generators. Use `AnnotatedGrammar.Text` for grammar text, `.Grammar` for parsed CFGs, `.Rsm` for pre-built RSMs. If a needed grammar is missing, add it to the registry first.

The [tests-writer skill](.opencode/skills/tests-writer/SKILL.md) and [language-registry guide](docs/developer/guides/language-registry.md) mandate that `FLPQ.TestUtilities.LanguageRegistry` is the single source of truth for **all** language-dependent information.

### 7.1 Hardcoded `Grammar.parseGrammar` — Non-Printer Test Files

These files call `Grammar.parseGrammar` with hardcoded strings instead of sourcing grammar text from the registry.

| # | File | ~Locations | Duplicates Registry? | Severity |
| --- | --- | --- | --- | --- |
| RV1 | `GrammarTests.fs` — tests verify `Grammar.parseGrammar` itself. Uses `"S -> a S b S \| eps"` (Dyck1), `"S -> a S \| a"` (APlus), `"S -> a"` (SingleA), `"S -> eps"` (EpsilonOnly), etc. Should use `LanguageRegistry.*.Grammars[n].Text`. Edge-case grammars (unit chains, empty input) must be registered first. | ~36 | Partial | Medium |
| RV2 | `FirstFollowTests.fs` — tests of First/Follow computation. Uses `"S -> a S b S \| eps"` (Dyck1), `"S -> a S \| a"` (APlus), `"S -> a B \| B -> b"` (ANB-like), `"E -> E + T \| T \| T -> x"` (ArithExpr subset), etc. Should use registry grammars. | ~13 | Partial | Medium |
| RV3 | `StressTests.fs:10` — `balancedGrammar` is Dyck1 grammar1. Should use `LanguageRegistry.Dyck1.Grammars[0].Grammar`. Line 123 dynamically generates grammar text — acceptable. | 1 | Yes | Low |
| RV4 | `PathIndexTeXTests.fs` — 4 hardcoded Dyck1 `Grammar.parseGrammar` calls, then bypasses registry RSM via `TestHelpers.grammarToRsm`. | ~4 | Yes | Medium |

### 7.2 Hardcoded `Grammar.parseGrammar` — Printer/Golden Test Files

Every printer/golden test hardcodes grammar strings that duplicate LanguageRegistry entries.

| # | File | ~Locations | Registry Equivalent | Severity |
| --- | --- | --- | --- | --- |
| RV5 | `TexCompilationTests.fs` | ~14 | Dyck1, APlus | Medium |
| RV6 | `AutomatonVisualizationTests.fs` | 4 | Dyck1, APlus | Medium |
| RV7 | `LLVisualizerTests.fs` | 7 | Dyck1, APlus, ANBN, SingleAB | Medium |
| RV8 | `LRVisualizerTests.fs` | 4 | Dyck1, APlus, ArithExpr | Medium |
| RV9 | `LRStepsGoldenTests.fs` | 2 | APlus, ArithExpr | Medium |
| RV10 | `LRTableTeXGoldenTests.fs` | 2 | Dyck1, ArithExpr | Medium |
| RV11 | `GrammarTeXGoldenTests.fs` | 3 | Dyck1, ArithExpr, TwoTrackDyck | Medium |
| RV12 | `LLStepsGoldenTests.fs` | 2 | Dyck1, APlus | Medium |
| RV13 | `LLTableTeXGoldenTests.fs` | 1 | Dyck1 | Low |
| RV14 | `ValiantTraceGoldenTests.fs` | 2 | Dyck1 | Low |
| RV15 | `CykSummaryGoldenTests.fs` | 1 | Dyck1 | Low |
| RV16 | `DerivationTreeVisualizationTests.fs` | 1 | APlus | Low |

**Total: ~89 hardcoded `Grammar.parseGrammar` calls across 16 files.** Each duplicates grammar text already in LanguageRegistry.

### 7.3 Hardcoded `RsmBuilder.buildRSMFromText` — Parser/Infrastructure Test Files

These files test EBNF/RSM-related functions but hardcode EBNF grammar text instead of sourcing from the registry.

| # | File | ~Locations | Duplicates Registry? | Severity |
| --- | --- | --- | --- | --- |
| RV17 | `EbnfParserTests.fs` | ~20 | Partial | Medium |
| RV18 | `RSMTests.fs` | ~17 | Partial (SingleA, SingleAB) | Medium |
| RV19 | `RsmToGrammarTests.fs` | ~10 | Partial | Medium |

### 7.4 Hardcoded `RsmBuilder.buildRSMFromText` — Printer/Golden Test Files

| # | File | ~Locations | Severity |
| --- | --- | --- | --- |
| RV20 | `TexCompilationTests.fs` | ~7 | Medium |
| RV21 | `RnglrTests.fs` | 1 | Low |
| RV22 | `RnglrStepVisualizationTests.fs` | 1 | Low |
| RV23 | `GssDotVisualizationTests.fs` | 1 | Low |

**Total: ~46 hardcoded `RsmBuilder.buildRSMFromText` calls across 7 files.**

### 7.5 `TestHelpers.grammarToRsm` Bypasses Pre-built `g.Rsm` + Hardcoded Utilities

| # | Description | ~Locations | Severity |
| --- | --- | --- | --- |
| RV24 | `TestHelpers.grammarToRsm` reconstructs RSM from text when `AnnotatedGrammar.Rsm` already exists. Called in `GllTests.fs` (~11), `RnglrTests.fs` (~6), `CrossParserEquivalenceTests.fs` (~6), `ParsingTestCases.fs:177`, `PathIndexTeXTests.fs` (~4). Should use `g.Rsm` directly. | ~28 | Medium |
| RV25 | `TestHelpers.fs` itself hardcodes `grammarToEbnfText` (line 87), `grammarToRsm` (line 107), `buildRegexRsm` (line 129). These utility functions bypass the registry. Fix: remove `grammarToRsm` (replaced by `g.Rsm`), move `grammarToEbnfText` to registry (or delete — callers should use `AnnotatedGrammar.Text`). | 3 | Medium |
| RV26 | `grammarToEbnfText` duplicated in `LanguageRegistry.fs:~50` and `TestHelpers.fs:87`. | 2 | Low |

### 7.6 `data/*.bnf` Files Duplicate Registry Grammar Definitions

| # | Description | Severity |
| --- | --- | --- |
| RV27 | 7 `.bnf` files in `data/` duplicate LanguageRegistry entries: `example_grammar.bnf` (Dyck1 grammar1), `example_grammar_amb.bnf` (Dyck1 grammar2), `example_grammar_a_a_a.bnf` (APlus grammar5), `example_grammar_an_bn.bnf` (ANBN), `example_grammar_chain.bnf`, `example_grammar_simple.bnf`, `example_lr_grammar.bnf` (ArithExpr grammar7). Used by `FLPQ.Cli.Tests`. Fix: CLI tests should generate temp files from the registry or read `.Text`. | Low |

## 8. Verification Completeness

This table tracks whether each constraint source (rows §1–§23 from the [Constraint Sources Map](.opencode/skills/code-review/SKILL.md#constraint-sources-map)) was verified during this review. "Verified" means either the tool reported zero violations, or manual inspection confirmed compliance or produced findings.

| § | Constraint | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Naming case | Auto — 0 FSharpLint warnings | FSharpLint |
| 2 | Code style idioms | Auto — 0 FSharpLint warnings | FSharpLint |
| 3 | Tab chars, redundant keywords, unused bindings | Auto — 0 FSharpLint warnings | FSharpLint |
| 4 | Tuples limited to 2 items — more fields require named type | Auto — FSharpLint (`maxNumberOfItemsInTuple`) | FSharpLint — 44+ violations detected (see below) |
| 5 | Formatting | Auto — 0 Fantomas diffs | Fantomas |
| 6 | XML doc comments on public API | Verified | Findings: D1–D4 (Section 6) |
| 7 | Genericity (no hardcoded `string`) | Verified | Findings: G1–G5 (Section 4), T1 (§7+§23) |
| 8 | Non-empty collections by type | Verified | Findings: T2 (§8+§12) |
| 9 | Separation data/presentation | Verified | Findings: A1–A3 (Section 2) |
| 10 | One algorithm per file | Verified | Findings: A5 (Valiant.fs at 912 lines) |
| 11 | Variants as thin layers | Verified | Findings captured by §13: D6, D7 (duplicated variant implementations) |
| 12 | Compile-time safety over runtime checks | Verified | Findings: T2 (list where NonEmptyList would be correct) |
| 13 | No code duplication | Verified | Findings: D1–D7 (Section 1) |
| 14 | Language registry | Verified | Findings: RV1–RV27 (Section 7) |
| 15 | No stubbed tests | Auto — verified | Searched for `Assert.True(true)`, empty test bodies, `[<Fact(Skip=...)>]` — zero matches |
| 16 | Property vs Fact labeling | Verified | Findings: T3 (§16), T2 (§16+§17) |
| 17 | FsCheck generators in shared `Generators.fs` | Verified | Findings: T1, T2 (§17) |
| 18 | Equivalence tests for all variants | Verified | Checked: `ValiantTests.testEquivalence`, `CrossParserEquivalenceTests.fs`, `SppfPropertyTests.fs`, `EbnfParserTests.ParsingEquivalenceTests` |
| 19 | Test coverage | Verified | Findings: T4, T5 (Section 5) |
| 20 | Documentation completeness | **Not fully verified** | Module docs checked for ~39/62 source files via Section 6 D1–D5. ~15 Printer module files, multiple CLI files, and `BasicSppf.fs` not verified for module doc existence per the documentation mapping table in `documentation-conventions.md`. Full audit deferred. |
| 21 | Book traceability | Verified | Findings: D5 (Section 6) |
| 22 | Code clarity | Verified | Findings: A4 (§22) |
| 23 | Naming semantics | Verified | Findings: T1 (§7+§23), T2 (§8+§12) |
