# Detailed Plan: Task 276 — Raise line-coverage thresholds + tests

## Task description (verbatim)

276. Raise line-coverage thresholds in the hard gate and add tests to reach them. 1. In `tools/hard_gate.py` raise `PER_PROJECT_THRESHOLD` from 85.0 to 90.0 and `TOTAL_THRESHOLD` from 90.0 to 95.0 (line coverage); update the script docstring, `docs/developer/guides/tools.md` threshold references and example output, and the quality-gates skill where numbers are mentioned. 2. Add tests until every source project reaches >= 90% line coverage and the total reaches >= 95%. Baseline (full-solution merged coverage): FLPQ.Languages 88.65% (6810/7682), FLPQ.Cli 95.30%, FLPQ.Printers 93.29%, FLPQ.LinearAlgebra 100%, FLPQ.GraphAnalysis 100%, FLPQ.RPQ 93.01%, TOTAL 91.04% (12636/13880). Biggest gaps: Sppf.fs, BasicSppf.fs, PathIndex.fs, EbnfParser.fs, LRParser.fs in FLPQ.Languages; MatrixTeX.fs, SummaryTeX.fs, ExternalTools.fs, RnglrTableTeX.fs in FLPQ.Printers.

## Context and analysis

- "Instruction coverage" = line coverage (the primary metric .NET tooling
  reports; see `tasks/global_plan.md` Interpretation Notes).
- Baseline measured 2026-09-16 from full-solution merged cobertura
  (`tmp/coverage.cobertura`, all 6 test projects). Per-file uncovered-line
  report: `tmp/uncovered-lines.txt`.
- F# compiles local functions into nested classes and emits generic +
  specialized instantiations, so one source line can appear in 2-3 `<line>`
  entries. Executing the code with varied type arguments (string, int) hits
  all entries; tests should use both `string` and `int` symbol types where
  practical to maximize entry coverage.
- Some uncovered lines are defensive/unreachable (e.g. `Sppf.fs:108,134,141,151`
  cache hits guarded by outer memoization; `LRParser.fs:504-505` duplicate
  lookup; `failwith`/`invalidArg` paths). These are excluded from targets —
  the reachable gaps far exceed the required +104 (Languages) / +550 (total)
  line entries.

## Subtasks

### S1: Raise thresholds in hard gate and update docs

**Code:** `tools/hard_gate.py` — `PER_PROJECT_THRESHOLD` 85.0 → 90.0,
`TOTAL_THRESHOLD` 90.0 → 95.0; docstring line 9 ("per-project >= 90% line,
total >= 95% line"). No new logic.
**Tests:** None (tooling). Verify the coverage-gate function against the
existing `tmp/coverage.cobertura` by running the gate's step-5 logic manually
(a short python one-off) — expected: FLPQ.Languages BLOCKED (88.65% < 90%),
TOTAL BLOCKED (91.04% < 95%). Commit gate (`tools/quality_check.py`) must pass.
**Docs:** `docs/developer/guides/tools.md` — update the three example-output
blocks: "(threshold 90%)" → "(threshold 95%)", "BLOCKED (below 85%)" →
"BLOCKED (below 90%)". The quality-gates skill mentions no numbers — verify
and leave unchanged.

**Spec:**

- Only the two constants and docstring change in `hard_gate.py`; the parsing
  loop is untouched (task 277 extends it).
- Example outputs in tools.md stay internally consistent (per-project line
  values unchanged, only threshold text updated).

### S2: FLPQ.Languages — Sppf.fs validator and edge-case tests

**Code:** None (tests only).
**Tests:** New file `tests/FLPQ.Languages.Tests/SppfValidatorTests.fs`
(reuse `Graph.fromEdges`, `Matrix.init`, existing SPPF construction helpers
from `SppfPropertyTests.fs`):

- `checkSppfCoverageInvariant`: build a small PI + SPPF where SPPF has more
  nodes than PI entries for each of the 4 entry kinds → assert `Error` with
  the corresponding message (covers Sppf.fs:396-425).
- `validateRangeNodesHaveChildren`: SPPF with a range node lacking
  PackedAlternative children → `Error` (444-453).
- `validateIntermediateChildren`: intermediate node missing LeftChild /
  RightChild → `Error` (476, 479).
- `validateNonterminalChildren`: nonterminal with 0 SingleChild edges;
  nonterminal whose SingleChild target is a terminal node → `Error` (499-524).
- `validateRangePositions`: range node with fromPos > toPos → `Error` (537).
- `validateIntermediateConnectedness`: intermediate with left child starting
  at wrong coords; right child ending at wrong coords; missing LeftChild /
  RightChild; left child that is a Nonterminal node (getCoords NT case, 561);
  child that is a Terminal (getCoords None case, 562) → `Error` (573-676).
- `buildSppfFromIndex` with `None` blockStart/blockFinals (213), with a
  blockStart missing the nonterminal (212), with blockStart present but
  blockFinals missing it (211) — use a PI built via `PathIndex.add`.
- `enumerateTrees`: SPPF whose root range points to a nonterminal without
  SingleChild edge → empty seq (736); intermediate missing children → empty
  seq (751); ambiguous root where one alternative yields >1 tree → forest
  wrapped in `Node(rootNt, ...)` (769).

**Spec:**

- Every test constructs its SPPF explicitly via `Graph.fromEdges` +
  `{ Graph = ...; RootIndices = [...] }` — no dependence on GLL execution.
- Assertions check the exact error message prefix (e.g. "Range node 0 has no
  children") so regressions in messages are caught.

### S3: FLPQ.Languages — BasicSppf.fs coverage tests

**Code:** None (tests only).
**Tests:** Extend `tests/FLPQ.Languages.Tests/BasicSppfTests.fs` (reuse
`BasicSppf.fromEdges`, existing CNF grammar helpers):

- `fromParsingTable`: empty table n=0 → empty SPPF (59); table with no start
  entry in cell (0, n-1) → empty SPPF (65); CNF grammar with an epsilon
  production → production node with 0 children (134).
- `extractDerivationTree`: root is Terminal → `Leaf` (192); root is Epsilon →
  `Leaf Symbol.Epsilon` (193); root is Production → first child tree
  (195-196).
- `enumerateTrees`: NT root with no production children → `Leaf(Symbol.N nt)`
  (214, 248); Terminal/Epsilon/Production roots (253-259: single-tree, empty,
  multi-tree cases); production with 2 children → `combineChildren`
  first::rest + `combineLists` (227-238); cyclic graph (NT→Prod→same NT) →
  visited-set termination in both `extractSingle` (164) and `extractAll` (203).
- `validateProductionChildren`: production with 0 children and with 3
  children (273-276); production with ruleIndex not in grammar (280-282);
  production whose childCount ≠ RHS length (287-290).
- `countScc` on a cyclic SPPF → back-edge branch (317); `countNonTrivialScc`
  on cyclic and acyclic SPPFs (336-377, never called before).
- `traverseAndCompare`: different vertex counts (383), different node info
  (397), different child counts (410), same children count but different
  child info (418); equal SPPFs → true.
- `validateProductionSplitConsistency`: production whose two children are
  Terminal/Epsilon (leftPos/rightPos variants, 426-436); left child rightPos ≠
  splitPoint (463-465); right child leftPos ≠ splitPoint (468-470).
- `validateSingleRoot`: single root with wrong info → `Error` (499-500); two
  roots without incoming edges → `Error` (501).

**Spec:**

- All SPPFs built via `BasicSppf.fromEdges` with explicit vertex/edge lists.
- CNF grammars for `fromParsingTable` tests built with the existing grammar
  helpers (string symbols); include one int-symbol variant to hit both
  generic instantiation entries.

### S4: FLPQ.Languages — PathIndex, RSM, GllTypes edge tests

**Code:** None (tests only).
**Tests:** Extend `tests/FLPQ.Languages.Tests/GllTests.fs` or add
`PathIndexInvariantTests.fs`:

- `PathIndex.add` + `get` round-trip for all 4 entry kinds (61-64, never
  called before); `addWithTracking` changed-cells tracking.
- `checkCalleeReachabilityInvariant`: PI with PNonterminal(A) whose callee
  cells are all empty → `Error` "all callee cells" (203-217); A in blockStart
  but not blockFinals → `Error` "block metadata not found" (218-230); A in
  neither map → same error via the other branch (231-243).
- `checkNoEpsilonInvariant`: PI containing PEpsilonNonterminal with no
  start/final overlap → `Error` (271-285); with overlap → `Ok` (260-261).
- `isAccepted` false case (empty acceptance cell, 305);
  `checkAcceptanceInvariant`: not accepted → `Ok` (319); accepted but the
  acceptance cell holds only PTerminal → `Error` "does not contain
  PNonterminal" (338, 343-351).
- `RSM.termTransitions` / `RSM.nontermTransitions` on an ExtendedRSM built
  from a test grammar (312, 316 — never called before); assert counts match
  the flat RSM's transition matrix.
- GllTypes: `GssVertex`/descriptor `Equals` and `CompareTo` with a
  wrong-type object → false / `invalidArg` (GllTypes.fs:25, 50).

**Spec:**

- PIs built via `PathIndex.add` on a small matrix (e.g. 2 states × 3
  vertices); ExtendedRSMs via the existing `ExtendedRSM` construction helpers
  used in GLL tests.
- Error-message assertions use `List.head errors |> contains "..."`.

### S5: FLPQ.Languages — EbnfParser, Grammar, FirstFollow, Cyk, Valiant edge tests

**Code:** None (tests only).
**Tests:** Extend `EbnfParserTests.fs`, `GrammarTests.fs`,
`FirstFollowTests.fs`, `CykTests.fs`, `ValiantTests.fs`:

- `Regexp.toString`: all 7 variants (REps, REmpty, RTerm, RNonterm, RSeq,
  RAlt, RStar) with nested compositions (100-115, never called before).
- `Regexp.derive` on REmpty → REmpty (55).
- `parseEbnf` error paths: unexpected character `@` (245); unbalanced
  `S -> (a` (277); `S -> * a` unexpected token in atom (282); extra token
  after rule `S -> a )` (312); invalid rule format `s -> a` lowercase LHS
  (313). Assert exception messages.
- File functions: `parseEbnfFile` and `RsmBuilder.buildRSMFromFile` on a temp
  .ebnf file (322, 412); `buildRSM` direct call (398); `buildRSMWithStart`
  with empty map → `invalidArg` (351); `buildRSMFromText` with empty text →
  `invalidArg` (407).
- `Grammar`: `rhsLength` on EpsilonRhs rule (52); `parseRule`/`fromLines`
  invalid line → `invalidArg` (82); `toCnf` on grammars exercising: epsilon
  RHS (397, 405), unit productions and long-RHS breakdown (433, 450),
  terminal-to-nonterminal mapping (397 Some/None), nullable-keep filter (358),
  block-boundary maps (208-209, 278).
- `FirstFollow`: `concat` with Epsilon-prefixed lists (10); first-set loop
  with a nonterminal missing from firstMap → `set [ [Symbol.Epsilon] ]`
  (56) via a grammar with an undefined RHS nonterminal.
- `Cyk.isAccepted` on empty table → false (108); `parseWithSppfInfo` with
  empty terminals → 0×0 matrix (121).
- `Valiant`: empty-token paths in both public entry points (469, 537);
  empty-mList branch (387) via the internal multiplication driver if
  reachable through a public API, otherwise note as unreachable.

**Spec:**

- Temp files written to `System.IO.Path.GetTempPath()` subdirs and deleted in
  `finally`; no dependency on repo `data/` files for error-path tests.
- Exception assertions use `[<Fact(Expectation = ...)>]`-style try/catch
  helpers consistent with existing `ErrorPathTests.fs` patterns in the Cli
  test project (check and reuse; if none, a local `assertThrows` helper).

### S6: FLPQ.Languages — LRParser conflicts, Rnglr passing reductions, Gll/RnglrLR branches

**Code:** None (tests only).
**Tests:** Extend `LRParserTests.fs`, `RnglrTests.fs`, `GllTests.fs`:

- LR conflict coverage: dangling-else grammar through `buildLR0Table` →
  ShiftReduce conflict recorded (257); grammar `S -> C S | ε`-style where one
  LR(0) state contains both the augmented completed item and another completed
  item → accept-state conflict branch (314 or 295, whichever fires — assert
  the conflict list is non-empty and contains the expected conflict kind);
  `buildLR0Table`/`buildSLR1Table` called with `eoiSymbol = Symbol.Epsilon`
  → eoi-already-present branch (300, 350); CLR(1) table on a grammar that
  yields a duplicate reduce entry → `Some _ -> ()` (420) if reachable.
- `parseWithSteps` with a hand-built `LRTable` whose Action has Reduce but
  GoTo lacks the entry → exception "Goto not found" (492).
- Rnglr passing-reduction path (327-343): grammar with nullable nonterminals
  where a passing reduction consumes stored states and extends predecessors
  via `productBfs` — iterate over `LanguageRegistry` grammars + inputs until
  lines 327-343 are hit (verify via per-file coverage); assert parse result
  matches GLL on the same input.
- Gll reachable branches: EmptyRange stored-pop handling (225), recognized
  range EmptyRange after pop (370), `currentGssIdx` None (474) — drive via
  grammars/inputs from `LanguageRegistry`; RnglrLR small gaps (27, 50, 65, 93,
  110, 171, 175-176) via existing RNGLR test inputs.

**Spec:**

- Conflict tests assert on `table.Conflicts` content (kind + state), not just
  non-emptiness.
- Rnglr passing-reduction test must be deterministic: fix the grammar and
  input in the test, do not rely on generator randomness.
- If a specific branch proves unreachable after two concrete attempts, record
  it in Design Notes with the reason (do not skip silently).

### S7: FLPQ.Printers — TeX table renderers (MatrixTeX, RnglrTableTeX, LR/LL/ParsingTable, ValiantTeX, small gaps)

**Code:** None (tests only).
**Tests:** Extend `MatrixTeXTests.fs`, add `RnglrTableTexTests.fs` section in
existing file, extend `LRTableTeXGoldenTests.fs`, `LLTableTeXGoldenTests.fs`,
`PathIndexTeXTests.fs`, `ValiantTraceGoldenTests.fs`:

- MatrixTeX: matrix with `Submatrix idx` blocks (idx 0..9 to exercise
  `blockColor`) (115-131); block overlapping a highlighted cell → `\Block` +
  `\cellcolor` (205-212).
- RnglrTableTeX: `toTex` direct call (116-117); table with zero nonterminals
  → empty goto spec (55, 73); `buildTabularWithHighlights` with
  `currentLrState = Some s` (row highlight, 177) and `None` (230),
  activeActions (red cell, 184), levelReductions (green cell, 188);
  `toTexWithHighlights` wrapper (256-265).
- LRTableTeX: uncovered 27-28, 92 branches via table shapes (empty action
  cells / goto-only rows).
- LLTableTeX: 10, 37 — empty-table / header-only variants.
- ParsingTableTeX: 23-25, 32 — both branch sides of the cell-content match.
- ValiantTeX: partial branches 145, 149, 162-164, 167, 183-187, 194 — trace
  shapes exercising each (highlighted vs non-highlighted cells, empty rows).
- Small gaps: AutomatonDot 53/61/72 + branch 71; AutomatonTikz 80-83, 102,
  107 + branch 80; GssDot 172-173; GssTikz 75, 77-78, 98-101 + branch 98;
  RsmDot 68, 72, 143, 178, 182; LRAutomatonTikz branch 53; CykTeX 51;
  DerivationTreeDot 95; DerivationTreeTikz 67; TeXRenderer 31;
  GllStepVisualizer 241-245, 261-265; RnglrStepVisualizer 28, 52, 75, 77.

**Spec:**

- Golden-style assertions where output is stable (string contains / equals);
  structural assertions (line counts, presence of `\Block`, `\rowcolor`) for
  highlight tests.
- Reuse existing golden helpers (`GoldenHelpers.fs`) and test grammars from
  `FLPQ.TestUtilities.LanguageRegistry`.

### S8: FLPQ.Printers — SummaryTeX and ExternalTools coverage tests

**Code:** None (tests only).
**Tests:** Extend `TexCompilationTests.fs` / add `SummaryTexSectionTests.fs`,
extend `ExternalToolsTests.fs`:

- SummaryTeX `ToString` for all 5 kinds (18-23, never called before).
- `collectSteps` on a non-existent directory → empty array (140).
- LR/LL summary sections: temp vizDir containing `lr_table.tex`,
  `lr_automaton.pdf` / `lr_automaton.tikz.tex`, per-step `table.tex` → all
  Some branches (186, 190, 199); stack step section with
  `tree_and_stack.tikz.tex` + `input.tex` present (233-254 region).
- GLL step section: temp dir with `descriptors_table.tex`,
  `new_descriptors.tex`, `path_index.tex`, `gss.tikz.tex`, `rsm.tikz.tex`,
  `input.tikz.tex` present (281-312 region).
- RNGLR step section: same pattern with its file set (348-370 region).
- `sppfSection`: tikz present (399-401 region), tikz absent + dot present →
  PDF fallback, both absent → empty.
- ExternalTools: `compileDotToPdf` on malformed DOT → false via failure
  branch (192-198); `compileTexStringWithTemplate` /
  `compileTexStringWithTemplateLog` with a real template + content (238-252,
  never called before); `compileTexFile` success and failure (bad TeX)
  (264-274); `compileTexFileTwice` first-pass-fail → false (282);
  node-position extraction on a DOT graph with an edge label variant (121).

**Spec:**

- Temp directories under `System.IO.Path.GetTempPath()`, cleaned in `finally`.
- TeX-compiling tests follow the existing pattern in `TexCompilationTests.fs`
  (they already invoke lualatex; keep individual test runtime bounded — one
  compile per test, reuse compiled artifacts where the existing helpers allow).

### S9: FLPQ.Cli and FLPQ.RPQ coverage tests

**Code:** None (tests only).
**Tests:** Extend `HelpersTests.fs`, `SummaryTests.fs`, runner test files,
`RPQTests.fs`:

- Cli `Helpers`: each of the 6 `find*Template` functions called from a working
  directory where no candidate path exists → `failwithf "Could not locate"`
  (106, 125, 144, 163, 182, 201). Use `Directory.SetCurrentDirectory` to a
  temp dir with restore in `finally`.
- Cli `Summary`: dot-compilation-failure branch (39, 53) by pointing at a vizDir
  whose .dot files are malformed; `buildSummary` returning false → exit code 1
  path (80-81); GLL header pdfs list with/without `ext_rsm.dot` (101-108, 114).
- Cli runners: LRRunner with `useDot = false` (39) and unexpected algorithm →
  exception (31); `runCli` with args causing a thrown exception → usage + 1
  (Program.fs:52) and summary-failure → 1 (41); ValiantRunner/CykRunner with
  pre-existing step directories (ValiantRunner 35, 97); CykRunner sppf-table
  variants: noSppfTable+empty highlights (37), sppf table+empty highlights
  (41); GllRunner/RnglrRunner on a rejected input → "Rejected" status branch
  (GllRunner 75, RnglrRunner 72).
- RPQ `GraphReader.parseGraph`: empty text (19, 40-side), line with wrong part
  count → exception (24), graph with no edges (65), graph without start line
  → all-vertices default (89), normal graph round-trip; partial branches 55,
  70, 76, 82 via the same set of inputs.
- RPQ `BelyaninRPQ`: empty NFA → empty result (24); graph with terminal-labeled
  transitions exercising the ATerm perLabel branch (39-41); final-state loop
  branches (61, 65).
- RPQ `KroneckerRPQ`: empty sources or empty graph → zero matrix (26);
  unreachable-final path (48); start-pair setup (37).
- RPQ `ArroyueloRPQ`: expression with REmpty (49); terminal absent from graph
  adjacency (54).

**Spec:**

- Cli runner tests reuse the existing runner-test harness patterns (temp
  output dirs, small grammars from `TestGrammarFiles.fs`).
- RPQ tests build NFAs directly (small 2-3 state graphs) — no file I/O except
  `parseGraph` string input.

### S10: Final coverage verification and gap closure

**Code:** None unless a project is still below threshold — then add the
specific missing tests identified by the fresh per-file uncovered report.
**Tests:** Full-solution coverage run (all 6 test projects + merge, same
commands as hard gate steps 4-5), then compute per-project line % and total
with the same counting logic as `hard_gate.py:run_coverage_gate`.
**Docs:** None.

**Spec:**

- Acceptance: every source project ≥ 90% line coverage AND total ≥ 95%.
- If short: regenerate `tmp/uncovered-lines.txt`, pick the highest-yield
  reachable lines, add tests, re-run (bounded to 2 iterations; if still short,
  STOP and report per the blocked-work protocol — do not lower thresholds).
- All existing tests must still pass (0 failed, 0 skipped) in every project.

## Reuse notes

- `FLPQ.TestUtilities`: `LanguageRegistry` (grammars), `Generators`,
  `ParsingTestCases`, `TestHelpers` — reused by S6/S7/S9.
- `GoldenHelpers.fs` (Printers tests) — reused by S7/S8.
- `Graph.fromEdges` / `Matrix.init` — used to hand-build SPPFs/PIs in
  S2-S4.
- Existing per-area test files are extended, not duplicated; new files only
  where no natural home exists (`SppfValidatorTests.fs`,
  `SummaryTexSectionTests.fs`).
