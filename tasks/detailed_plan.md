# Detailed Plan — Task 280: Arroyuelo RPQ visualization + shared RPQ-viz infrastructure

## Task description (verbatim)

280. Add Arroyuelo RPQ algorithm visualization to the CLI. Same structure as other algorithms: input, results, steps, and summary. Input is a regexp and a graph. Visualize both the regexp and the respective DFA. TikZ by default, DOT as fallback for all figures. Same visualization of matrices: use matrix with set of symbols (sets of vertex paths), not boolean matrices — look at Valiant visualization for example. For Arroyuelo one step is one node of the regexp tree processing; highlight the current node of the tree. Visualize the operation over matrices (for transitive closure no inner steps: just TC(M)=M' where M and M' are visualized). Highlight computed parts in the input graph: each step collects information necessary to highlight partial paths, not just start and final vertex — use a custom semiring over sets of vertex sequences to track intermediate points (look at Valiant custom semirings for inspiration). Use adjustbox for TikZ. This task also creates the shared infrastructure for task 281: CLI regexp/graph arguments, Regexp.toDfa helper, path semiring module, example regexp and graph data files.

## Design decisions (recorded before implementation)

Book references: Chapter 11 `03_Arroyuelo.tex` (sec:RPQ_Arroyuelo) — matrix translation
M(ε)=I, M(a)=M_a, M(E1|E2)=M(E1)+M(E2), M(E1/E2)=M(E1)×M(E2), M(E\*)=I+M(E)^+,
post-order evaluation of the regexp syntax tree.

- **Path semiring (custom semiring, Valiant-style witnesses):** matrix cells hold
  `Set<int list>` — sets of vertex sequences (paths, endpoints included). This is the
  book's path semiring carrier 2^{V\*} (02_BFS.tex) applied to Arroyuelo's matrix
  translation. Cells are finite because only **simple** paths (no repeated vertex) are
  stored: concatenation drops non-simple candidates, exactly like I_simple in the
  book's Belyanin example (candidate (v1,v2,v1) dropped).
- **Consequence (documented in arroyuelo-rpq.md):** on cyclic graphs the boolean
  projection of the path result may be a strict subset of full reachability (labelled
  walks that revisit vertices are excluded). On **acyclic** graphs every walk is
  simple, so the boolean projection equals `ArroyueloRPQ.evaluate` — this is the
  equivalence requirement (property test).
- **TC as one step:** RStar node visualizes `TC(M) = M'` with exactly two matrices
  (operand M, result M' = I ⊕ TC(M)); no inner squaring steps. TC itself is computed
  by repeated squaring over the path semiring (same loop shape as the existing
  boolean `transitiveClosure`).
- **Steps:** one per AST node, post-order (children before parent); root is the last
  step. Step k's graph figure highlights every vertex/edge of every path in that
  node's result matrix (partial paths, not just endpoints).
- **Regexp input format:** EBNF file; the first rule's RHS is the query regexp. In RPQ
  queries every identifier is an edge label, so `RNonterm(Nonterminal s)` is mapped to
  `RTerm(Terminal s)` (case convention of EBNF grammars does not apply). This makes the
  book example `S -> walk / (O | R)+ / walk` work directly.
- **DFA:** built from the query regexp via Brzozowski derivatives — new shared
  `Regexp.toDfa` generalizing the test-local `regexToDfa` helper.
- **Figures:** TikZ default, DOT fallback (`--use-dot`), all wrapped in adjustbox in
  the summary (existing `wrapTikzAdjustbox false`). Matrices are pNiceMatrix with
  path-set cells (empty cell = `\cdot`, Valiant convention).
- **Reuse (no new modules where existing ones suffice):**
  - graph figure (root + per-step, with highlights) → `GssDot.toDotFromSets` /
    `GssTikz.toTikzFromSets` (all vertices/edges active; path vertices/edges highlighted;
    `storedPopVertices = Set.empty`, `currentVertex = None`, `positionOf = None`)
  - regexp tree figure → same Gss\* renderers (a tree is a graph: nodes = AST node
    indices, edges = parent→child, node label = subexpression)
  - DFA figure → `AutomatonDot.dfaToDot` / `AutomatonTikz.dfaToTikz` generalized with
    highlighted-state variants (existing functions delegate with empty set)
  - matrix rendering → `MatrixTeX.toTeXStyled` (useAdjustbox = true)
  - regexp parsing → `EbnfParser.parseEbnf`; graph reading → `GraphReader.parseGraphFile`
  - step layout → two-column minipage template modeled on `data/RNGLR_step_template.tex`

## Subtasks

### S1: Regexp.toDfa + RpqInput (regexp file parsing for RPQ) [done — a33e174]

**Code:**

- `src/FLPQ.Languages/EbnfParser.fs` — in `module Regexp`: add
  `toDfa : Regexp<'t,'nt> -> DFA<'t,int>`: alphabet = terminals from `Regexp.symbols`
  (fallback `["a"]` when empty), derivation over `RsmSymbol.RTerm`, via existing
  `Regexp.buildDfaFromRegex`.
- New file `src/FLPQ.RPQ/RpqInput.fs` — module `RpqInput`:
  - `mapNontermsToTerms : Regexp<string,string> -> Regexp<string,string>` (RNonterm → RTerm, recursive)
  - `parseRegexpText : string -> Regexp<string,string>` (EbnfParser.parseEbnf → first rule's RHS → mapNontermsToTerms; failwith on empty input)
  - `parseRegexpFile : string -> Regexp<string,string>`
- `tests/FLPQ.RPQ.Tests/RPQTests.fs` — replace the local `regexToDfa` helper with
  `Regexp.toDfa` (delete the helper).

**Tests:**

- New `tests/FLPQ.Languages.Tests/EbnfParserTests.fs` section (or new file
  `RegexpToDfaTests.fs`): for regexps `a`, `a*`, `a|b`, `(ab)*`, `eps`, `a(b|c)*d` —
  DFA accepts exactly the strings in L(regexp) over the regexp's alphabet up to length
  4 (small local regexp-language matcher in the test); `Dfa.isDeterministic` holds.
- New tests for `RpqInput`: uppercase labels become terminals; first rule used when
  multiple rules present; empty text throws.

**Docs:** update `docs/developer/ebnf-parser.md` (new `Regexp.toDfa`), new
`docs/developer/rpq-input.md`, update hub `docs/developer/FLPQ.RPQ.md`,
`docs/main.md` (module index link).

**Spec:**

- `toDfa` must produce the same DFA as the old test helper for all existing test
  regexps (existing cross-algorithm property tests keep passing unchanged).
- `parseRegexpText` fails with a clear message when the EBNF text has no rules.

### S2: PathSemiring module [done — f77c3cb]

**Code:** new file `src/FLPQ.RPQ/PathSemiring.fs` — module `PathSemiring`:

- `type path = int list`; cells are `Set<path>`.
- `zero : Set<int list>`; `isZero : Set<int list> -> bool`
- `isSimple : int list -> bool` (no repeated vertex)
- `extend : int list -> int -> int list option` — append vertex iff not already in the path
- `mulCells : Set<int list> -> Set<int list> -> Set<int list>` — cell product for
  (i,k)×(k,j): `p1 @ List.tail p2` for `last p1 = head p2`, keep only simple results
- `addMatrices`, `mxm` (path × path, via `LinearAlgebra.mxm`), `boolSelectRows`
  ((N^a)^T ⊗ M: union of M[q,v] over q with N^a[q,q']=true), `boolExtendCols`
  (M ⊗ G^a: extend each path by the edge target, keep only simple), `identity : int -> Matrix<Set<int list>>` ([i] on the diagonal), `transitiveClosure : Matrix<Set<int list>> -> Matrix<Set<int list>>` (repeated squaring of I ⊕ M, same
  loop as ArroyueloRPQ's boolean TC), `isEmpty : Matrix<Set<int list>> -> bool`,
  `allPaths : Matrix<Set<int list>> -> Set<int list>`,
  `booleanProjection : Matrix<Set<int list>> -> Matrix<bool>`

**Tests:** new `tests/FLPQ.RPQ.Tests/PathSemiringTests.fs`:

- Facts: isSimple, extend (new vs repeated vertex), mulCells (matching/mismatching
  endpoints, non-simple dropped), mxm on 2×2/3×3 matrices, identity acts as unit in
  mxm, boolSelectRows/boolExtendCols on small matrices, transitiveClosure on a 3-cycle
  and on an acyclic 4-vertex graph.
- Properties: (a) every path produced by mxm is simple and has correct endpoints;
  (b) `booleanProjection (transitiveClosure M)` equals the boolean transitive closure
  of `booleanProjection M` (unlabelled reachability: walk existence ⟺ simple-path
  existence — holds for all graphs, no acyclicity restriction); compare against a
  local boolean TC computed with `MsBfs.boolAdd/boolMul`.

**Docs:** new `docs/developer/path-semiring.md` (kind: data-structure; book reference
Chapter 11 02_BFS.tex path semiring), hub `FLPQ.RPQ.md`, `docs/main.md`.

**Spec:**

- All operations total (no exceptions) on well-formed inputs; empty sets are the zero.
- No new generic linear-algebra code — build on `LinearAlgebra.mxm` / `Matrix.map2`.

### S3: ArroyueloRPQ path-semiring trace [done — b8b2acd]

**Code:** `src/FLPQ.RPQ/ArroyueloRPQ.fs`:

- `type ArroyueloOperation = Base | Alt | Seq | Star`
- `[<Struct>] type ArroyueloTraceStep<'t,'nt when 't: comparison and 'nt: comparison> = { NodeIndex: int; Expr: Regexp<'t,'nt>; Operation: ArroyueloOperation;   Operands: Matrix<Set<int list>> list; Result: Matrix<Set<int list>>;   Children: int list }` — Children = post-order indices of child nodes (empty for
  leaves); the last step is the root.
- `evaluateWithTrace : NFA<'t,int> -> Regexp<'t,'nt> -> ArroyueloTraceStep<'t,'nt> list * Matrix<Set<int list>>`
  — post-order evaluation over the path semiring: REps → identity, REmpty → zero,
  RTerm → per-label adjacency as paths {[i;j]}, RNonterm → zero (unreachable after
  RpqInput mapping, kept for type completeness), RAlt → addMatrices, RSeq → mxm,
  RStar → addMatrices identity (transitiveClosure child). One step per node; Operands
  = [] for Base, [left; right] for Alt/Seq, [child] for Star.
- Existing `evaluate` (boolean) stays unchanged — it is the equivalence baseline.

**Tests:** new section in `tests/FLPQ.RPQ.Tests/RPQTests.fs`:

- Step count = number of AST nodes; last step's Expr = whole regexp; Children indices
  point to earlier steps and match the AST structure (fact on a nested regexp).
- Star step: `Result` equals `PathSemiring.addMatrices (PathSemiring.identity n) (PathSemiring.transitiveClosure operand)` (fact).
- **Equivalence property** (new acyclic generator in
  `tests/FLPQ.TestUtilities/Generators.fs`: edges only from lower to higher vertex
  index, random regexp over the edge labels): for acyclic graphs, source rows of
  `PathSemiring.booleanProjection finalMatrix` equal `ArroyueloRPQ.evaluate`.
- Fact documenting the cyclic difference: query `a a` (RSeq) on 2-cycle v0-a->v1,
  v1-a->v0 with source v0 — boolean `evaluate` says v0 reachable, path trace does not
  (walk v0→v1→v0 is not simple).

**Docs:** update `docs/developer/arroyuelo-rpq.md`: new Type Definitions section
(ArroyueloOperation, ArroyueloTraceStep), evaluateWithTrace signature, design decision
row for simple-path semantics + cyclic-graph consequence.

**Spec:**

- Trace must be deterministic (post-order, no map iteration order leaks into step
  content).
- Per-label adjacency matrices are derived exactly as in `evaluate`
  (`BooleanDecomposition.decomposeNonEmptySet`).

### S4: PathSemiringTeX + RegexpTeX [done — de1fd9c]

**Code:**

- New file `src/FLPQ.Printers/PathSemiringTeX.fs`:
  - `pathToTeX : int list -> string` → `(v_0, v_2, v_3)`
  - `pathSetCellToTeX : Set<int list> -> string` → `\{(v_0, v_1), (v_0, v_2)\}`; empty → `\cdot`
  - `matrixToTeX : (int -> string) -> (int -> string) -> Matrix<Set<int list>> -> string`
    — row/col label printers (e.g. `fun i -> sprintf "v_%d" i`), wraps
    `MatrixTeX.toTeXStyled` with useAdjustbox = true, useRectangleColor = false.
- New file `src/FLPQ.Printers/RegexpTeX.fs`:
  - `toTeX : Regexp<string,string> -> string` — math-mode formula: REps → `\varepsilon`,
    REmpty → `\varnothing`, RTerm t → name (single letter as-is, multi-char via
    `\text{...}`, escaped with `AutomatonTikz.escapeLatex`), RNonterm → `\text{X}`,
    RSeq → `l / r` (parens around non-atomic children), RAlt → `l \mid r`,
    RStar → `(l)^*`.

**Tests:** new `tests/FLPQ.Printers.Tests/PathSemiringTexTests.fs`:

- pathToTeX/pathSetCellToTeX exact-output facts (empty set, single path, multiple paths).
- matrixToTeX: contains pNiceMatrix, adjustbox, vertex row/col headers, `\cdot` for
  empty cells; lualatex compilation of a 3×3 matrix (Trait "TeX").
- New `tests/FLPQ.Printers.Tests/RegexpTexTests.fs`: exact-output facts per constructor
  - nesting parens; lualatex compilation (Trait "TeX").

**Docs:** new `docs/developer/path-semiring-tex.md` and
`docs/developer/rpq-regexp-viz.md` (covers RegexpTeX now; tree renderers added in S5
reuse Gss\* — documented here), hub `FLPQ.Printers.md`, `docs/main.md`.

**Spec:**

- Cell content is math-mode TeX only (no document-level commands) so it can be placed
  inside pNiceMatrix cells and the summary's math wrappers.
- Empty-set rendering `\cdot` matches Valiant's convention.

### S5: DFA highlight generalization + regexp-tree rendering decision [done — b846823]

**Code:**

- `src/FLPQ.Printers/AutomatonDot.fs`: private `stateDeclarations` gains a
  `highlightedStates: Set<int>` parameter (fill: lightblue, overriding start fill);
  add `dfaToDotWithHighlights : ('t -> string) -> (int -> 's -> string) -> DFA<'t,'s> -> Set<int> -> string`;
  existing `dfaToDot` delegates with `Set.empty`.
- `src/FLPQ.Printers/AutomatonTikz.fs`: same for TikZ — private state-declaration
  helper gains highlight parameter (fill=lightblue!20); add
  `dfaToTikzWithHighlights : ('t -> string) -> (int -> 's -> string) -> string -> DFA<'t,'s> -> Set<int> -> string`;
  existing `dfaToTikz` delegates with `Set.empty`.
- Regexp tree figure: **no new module** — the step visualizer (S6) renders the tree
  via `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` (nodes = AST node indices,
  edges = parent→child, node label = subexpression via RegexpTeX, highlighted vertex
  = current node). Record this reuse decision in the docs.

**Tests:**

- `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs`: new facts —
  dfaToDotWithHighlights marks exactly the given states with fillcolor=lightblue and
  leaves others plain; dfaToTikzWithHighlights emits fill=lightblue!20 for exactly the
  given states; existing dfaToDot/dfaToTikz outputs byte-identical to before
  (delegate with empty set).

**Docs:** update `docs/developer/automaton-viz.md` (new functions + highlight
behavior), note the tree-rendering reuse in `docs/developer/rpq-regexp-viz.md`.

**Spec:**

- Existing callers (LRRunner, RnglrRunner, LRAutomatonTikz, RnglrAutomatonTikz)
  unchanged; all existing automaton tests pass without modification.

### S6: ArroyueloStepVisualizer + step templates + Helpers [done — 0ba1d51]

**Code:**

- New file `src/FLPQ.Printers/ArroyueloStepVisualizer.fs`:
  - `type ArroyueloVisualizationStep = { TreeDot: string; TreeTikz: string; Matrices: string; GraphDot: string; GraphTikz: string }`
  - `renderSteps : (string -> string) -> NFA<string,int> -> ArroyueloTraceStep<string,string> list -> ArroyueloVisualizationStep list`
    (terminal printer for graph edge labels).
  - Tree: numbered nodes from the trace steps (NodeIndex + Children), edges
    parent→child, label = `RegexpTeX.toTeX step.Expr`; current node highlighted.
    DOT via `GssDot.toDotFromSets`, TikZ via `GssTikz.toTikzFromSets` (shape
    "rectangle", skipEscaping = true — labels are already TeX).
  - Matrices: vertical stack per operation — Base: one matrix labeled with the node's
    formula; Alt: M(left) `\oplus` M(right) `=` M(node); Seq: M(left) `\times`
    M(right) `=` M(node); Star: `TC(`M(child)`) = ` M(node). Each matrix via
    `PathSemiringTeX.matrixToTeX` with vertex row/col labels; formula labels in math
    mode.
  - Graph: all graph vertices/edges active; highlighted vertices/edges = union over
    all paths in the step's Result (derived via `PathSemiring.allPaths`).
- New files `data/Arroyuelo_step_template.tex` and `data/Arroyuelo_step_tikz_template.tex`
  — two-column minipage layout modeled on `data/RNGLR_step_template.tex`:
  left = `__STEP_TREE_PDF__`/`__STEP_TREE_TIKZ__` + `__MATRICES__`;
  right = `__STEP_GRAPH_PDF__`/`__STEP_GRAPH_TIKZ__`.
- `src/FLPQ.Cli/Helpers.fs`: `writeArroyueloStepsVisualization` (per step dir:
  `tree.tikz.tex`|`tree.dot`, `matrices.tex`, `graph.tikz.tex`|`graph.dot`),
  `findArroyueloStepTemplate`, `findArroyueloStepTikzTemplate` (same candidate-list
  pattern as the RNGLR finders).

**Tests:** new `tests/FLPQ.Printers.Tests/ArroyueloStepVisualizationTests.fs`:

- Golden test: small graph (4 vertices, labels a/b) + regexp `a / (b | a)*` — render
  all steps, verifyGolden each step's tree/matrices/graph artifact (TikZ mode).
- Structural facts: tree DOT of step k contains exactly one lightblue node (the
  current one); matrices TeX contains the right number of pNiceMatrix blocks per
  operation kind; graph DOT highlights in red exactly the edges used by the result
  paths.
- Template fill test: filled tikz template contains no leftover `__` placeholders and
  wraps figures in adjustbox.

**Docs:** new `docs/developer/arroyuelo-step-viz.md` (kind: visualization), hub
`FLPQ.Printers.md`, `docs/main.md`.

**Spec:**

- Step k's graph highlight set is derived only from step k's Result matrix (no
  cross-step leakage).
- Only one figure format written per step (TikZ by default, DOT with --use-dot),
  matching the existing runner convention.

### S7: ArroyueloRunner + CLI wiring + example data files [done — f236c61]

**Code:**

- `src/FLPQ.Cli/AlgorithmTypes.fs`: +`ArroyueloRPQ` DU case (display name "Arroyuelo
  RPQ"); +argument cases `[<AltCommandLine("-r")>] Regexp of string` and
  `[<AltCommandLine("--graph")>] GraphFile of string` with usage text.
- `src/FLPQ.Cli/Program.fs`: move `GetResult Grammar/Input` into the parsing-algorithm
  branch; RPQ branch requires `Regexp` + `GraphFile` (Argu error with usage when
  missing) and dispatches to `ArroyueloRunner.runArroyuelo regexpFile graphFile output useDot`.
- New file `src/FLPQ.Cli/ArroyueloRunner.fs`:
  - `runArroyuelo : string -> string -> string -> bool -> unit`
  - Parse regexp (`RpqInput.parseRegexpFile`), build DFA (`Regexp.toDfa`), parse graph
    (`GraphReader.parseGraphFile`), run `ArroyueloRPQ.evaluateWithTrace`.
  - Root artifacts: `regexp.tex` (formula, math content only), `dfa.tikz.tex`|`dfa.dot`
    (state labels `q_i`), `graph.tikz.tex`|`graph.dot` (no highlights), `result.tex`
    (final matrix via PathSemiringTeX + one line per source: reachable vertices,
    boolean projection).
  - Steps via `ArroyueloStepVisualizer.renderSteps` + `Helpers.writeArroyueloStepsVisualization`.
  - Status line: `Arroyuelo RPQ: <n> steps, sources: ... -> reachable: ...` (printfn,
    captured-output test pattern).
- New data files: `data/example_regexp.txt` (`S -> a / (b | c)* / a`),
  `data/example_graph.txt` (GraphReader format: start line + ~6 edges over labels
  a/b/c, one cycle to exercise simple-path filtering).

**Tests:**

- New `tests/FLPQ.Cli.Tests/ArroyueloRunnerTests.fs`: runArroyuelo on example data —
  TikZ mode writes regexp.tex, dfa.tikz.tex, graph.tikz.tex, result.tex, step dirs
  with tree/matrices/graph files; DOT mode writes the .dot variants; result.tex lists
  the expected reachable vertices (hand-computed for the example).
- `tests/FLPQ.Cli.Tests/ProgramDispatchTests.fs`: +facts — `-a ArroyueloRPQ -r ... --graph ...`
  exits 0; missing `-r`/`--graph` exits 1; parsing algorithms still require -g/-i.

**Docs:** update `docs/developer/FLPQ.Cli.md` (new runner + args),
`docs/user/cli.md` (algorithm list, flags table, output structure for Arroyuelo RPQ,
example usage), `docs/project/architecture.md` (new files).

**Spec:**

- Parsing-algorithm CLI behavior is byte-identical to before (existing dispatch tests
  pass unchanged).
- `result.tex` reachable-vertex lines must match `ArroyueloRPQ.evaluate` for the
  example data (cross-check in the test).

### S8: Summary integration (SummaryKind + StepTemplates refactor) [done — 20d4eaf]

**Code:**

- `src/FLPQ.Printers/SummaryTeX.fs`:
  - +`SummaryKind.ArroyueloRPQ` (ToString "arroyuelorpq").
  - **Refactor:** replace the four positional template parameters of `buildContent`
    with a record `type StepTemplates = { Gll: string; GllTikz: string; Rnglr: string; RnglrTikz: string; Arroyuelo: string; ArroyueloTikz: string }` (Belyanin fields
    added in task 281); update the single caller (`Summary.fs`) and tests.
  - `headerSection`: +ArroyueloRPQ branch — color legend (current tree node
    lightblue; path vertices yellow; path edges red; `\cdot` = empty cell) +
    "Query Regular Expression" (regexp.tex, wrapMath) + "Query DFA" (TikZ:
    dfa.tikz.tex via wrapTikzAdjustbox false; DOT: dot_pdfs/dfa.pdf) + "Input Graph"
    (same pattern).
  - `arroyueloStepSection` (pattern of `rnglrStepSection`): fill
    Arroyuelo_step\_(tikz\_)template placeholders; step 0 title stays "Step 0".
  - `buildContent`: route SummaryKind.ArroyueloRPQ through arroyueloStepSection.
- `src/FLPQ.Cli/Summary.fs`: `algorithmToKind` +ArroyueloRPQ; load Arroyuelo step
  templates (finders from S6); build the StepTemplates record.

**Tests:**

- `tests/FLPQ.Printers.Tests/SummaryTexSectionTests.fs` (or CliSummaryTests): header
  section for ArroyueloRPQ contains regexp/DFA/graph sections in order; step section
  fills all placeholders (no leftover `__`).
- Compilation test (Trait "TeX"): full run on example data → buildSummary → merged TeX
  compiles with lualatex (both TikZ and DOT modes).

**Docs:** update `docs/developer/summary-tex.md` (new kind, StepTemplates record),
`docs/developer/FLPQ.Cli.md`.

**Spec:**

- Existing summary behavior for all other algorithms is unchanged (existing summary
  tests pass without modification; golden summary files, if any, stay byte-identical).
- The merged Arroyuelo summary contains: header (legend, regexp, DFA, graph), one
  section per step (tree + matrices | graph), and the SPPF-less tail (no sppfSection
  for RPQ — it is skipped when sppf files are absent, existing behavior).

### S9: Hard-gate fixes (FLPQ.RPQ lint + FLPQ.Cli branch coverage) [done — 612170d]

The first hard-gate run after S8 reported two BLOCKED steps:

- Lint `src/FLPQ.RPQ`: 5 warnings — FL0038 (`type path = int list`, lowercase type
  alias in PathSemiring.fs) and FL0051 (4× three-item tuple returned by the private
  post-order `loop` in ArroyueloRPQ.fs).
- Coverage `FLPQ.Cli`: branch 89.1% (196/220) < 90%.

Root cause (verified against the IL of FLPQ.Cli.dll): all 12 uncovered branches are
either compiler-generated dead code or untested error paths — no test input can flip
them:

- ArroyueloRunner.fs (4): each `for i in 0 .. n - 1` compiles to a RangeInt32
  enumerator inside try/finally; the finally's `isinst IDisposable` false-side is never
  taken (the enumerator is always disposable), and `Array.map` carries an unreachable
  null-array throw. Multi-source / empty-reachable tests flip none of these.
- Helpers.fs (8): the eight `find*Template` functions are copy-paste duplicates whose
  `None -> failwithf` arm is never exercised (the template always exists in the test
  output directory).

**Code:**

- `src/FLPQ.RPQ/PathSemiring.fs`: drop the lowercase `path` alias; use `int list`
  directly (ArroyueloRPQ.fs already uses `Matrix<Set<int list>>`; no other file
  references the alias).
- `src/FLPQ.RPQ/ArroyueloRPQ.fs`: replace the three-item tuple return of the private
  post-order `loop` with a private record
  `type private LoopState<'t, 'nt> = { NodeIndex: int; Steps: ... list; Result: Matrix<Set<int list>> }`.
- `src/FLPQ.Cli/Helpers.fs`: consolidate the eight duplicated `find*Template` functions
  into one public `findTemplateFile (fileName: string) : string` (same three candidate
  locations, same error message); the eight finders become one-line wrappers. This
  removes 14 branches (8 x 2 -> 1 x 2) and makes the error path testable.

**Tests:**

- `tests/FLPQ.Cli.Tests/HelpersTests.fs`: +`findTemplateFile` fails with the tried
  paths when no candidate exists (covers the previously dead None arm).
- `tests/FLPQ.Cli.Tests/ArroyueloRunnerTests.fs`: +multi-source test (graph with two
  start vertices, one of which reaches nothing) and +no-match regexp test (terminal
  absent from the graph) — behavior coverage for per-source lines and the empty
  reachable list (these flip no branch; the missing branches are dead compiler code).

**Docs:** `docs/developer/path-semiring.md` updated for the removed `path` alias.

**Spec:**

- FLPQ.RPQ lints with zero warnings; FLPQ.Cli branch coverage >= 90%
  (projected 190/206 = 92.2%); all existing tests pass unchanged; hard gate
  STATUS: PASS.

### S10: Remaining test-project lint warnings (gate re-run findings) [done — 58a0c0c]

The S9 gate re-run passed coverage and all source-project lints but was BLOCKED on two
test projects the first gate run never reached (it was still at step 16 when S9 started):

- `tests/FLPQ.Printers.Tests` FL0046: `to_` (trailing underscore to dodge the reserved
  word `to`) in ArroyueloStepVisualizationTests.fs.
- `tests/FLPQ.RPQ.Tests`: interpolated string `$"..."` without interpolation holes in
  PathSemiringTests.fs.

**Code:**

- `tests/FLPQ.Printers.Tests/ArroyueloStepVisualizationTests.fs`: rename the loop
  variables to `(fromV, toV)`.
- `tests/FLPQ.RPQ.Tests/PathSemiringTests.fs`: drop the unused `$` prefix.

**Tests:** the changed files are test files; existing suites must pass unchanged.

**Docs:** none.

**Spec:** both projects lint with zero warnings; hard gate STATUS: PASS.

## Verification (end of task)

- Full CLI run: `dotnet run --project src/FLPQ.Cli -c Release -- -a ArroyueloRPQ -r data/example_regexp.txt --graph data/example_graph.txt -o viz_output/arroyuelo -s` — exit 0, PDF produced.
- Same with `--use-dot` — exit 0.
- Hard gate: STATUS: PASS (format, lint, build, tests, coverage thresholds).
