- This file contains user-defined tasks. Do not modify them. Only track status of tasks in this file.

- **Strict rule: When marking a task as done, ONLY prepend `[done] ` to the existing task line (between task number and task formulation). NEVER rewrite, reformulate, shorten, or replace the task description itself. The task text is immutable — only the status tag may change.**

- **User guidance annotations**: When a task requires user clarification (Blocked Work Protocol), guidance may be appended after the task's full formulation as `**[USER GUIDANCE]**: <text>` on an indented line. These are the only permitted additive changes to task descriptions. See the `user-guidance-transfer` skill.

- The book is in TeX. Root of the book in `../../`.

- In some tasks Russian may be used to simplify references to the book that is in Russian.

- Archived: 1-100 in tasks1.md, 101-200 in tasks2.md.

201. [done] Refactor RNGLR algorithm to classical descriptor-based working set structure. Replace per-vertex pending queues and recursive cascade with a single global descriptor queue (descriptors to handle) and handled descriptors set. Main loop takes next descriptor from queue, consults LR table for actions (shift/reduce), performs all defined actions, and enqueues new descriptors for future processing. Remove recursive processNode cascade by enqueuing descriptors for reduction targets instead. Remove the depth guard (1000). All existing tests must pass without changes.

202. [done] Add RNGLR steps visualization. Analogous to GLL steps visualization. Each step is one `processNode` call iteration in the per-vertex loop. Visualize descriptors table, GSS, path index, LR automaton (instead of RSM), input graph, new descriptors. Use the same three-panel side-by-side template for step layout.

     1. Add step data collection to RNGLR algorithm.
        - Add `RnglrParsingStep<'t,'nt>` record type to `RnglrTypes.fs`. Fields: `PendingQueues: RnglrDescriptor list[]` (per-vertex pending queue snapshots), `ActiveGssVertices: Set<int>`, `ActiveGssEdges: Set<int*int>`, `NewGssVertices: Set<int>`, `NewGssEdges: Set<int*int>`, `PathIndexMatrix: Matrix<Set<PathIndexEntry<'t,'nt>>>`, `ChangedCells: Set<int*int>`, `InputVertex: int`, `CurrentLrState: int option`, `CurrentDescriptor: RnglrDescriptor option`, `HandledDescriptors: Set<RnglrDescriptor>`, `NewDescriptors: Set<RnglrDescriptor>`, `AttemptedDescriptors: Set<RnglrDescriptor>`.
        - Add `Rnglr.buildPathIndexWithSteps` — variant of `buildPathIndex` accepting an `onStep` callback. The callback fires at initialization and after each `processNode` call inside the per-vertex loop (`for v in 0..vertexCount-1 do while pending.[v].Count > 0 do ...`). Collects: per-vertex pending queue state, active GSS elements (differenced from previous step via `collectActiveGss` analog), path index matrix copy, changed cells, current descriptor, and descriptor accounting (new/attempted/handled).
        - Share `collectActiveGss` from GLL (or extract common version) — both GLL and RNGLR scan GSS edge matrices to find active vertices/edges.
     2. Add RNGLR step visualizer.
        - New file `src/FLPQ.Printers/RnglrStepVisualizer.fs`. Module `RnglrStepVisualizer` with type `RnglrVisualizationStep` containing rendered strings: `DescriptorsTable`, `NewDescriptors`, `GssDot`, `PathIndex`, `Input`, `LrAutomatonDot`.
        - Descriptors table TeX: columns `lrState \ input` (2 columns vs GLL's 4). Same structure: header with `\hline\hline`, to-handle block, `\hline\hline`, handled block. Current descriptor row gets `\rowcolor{yellow!20}`. Reuse GLL's `newDescriptorsToTeX` pattern — render descriptor as `(lrState, input)`, green background for new, red for reattempts.
        - GSS DOT: reuse `GssDot.toDotFromSets` with RNGLR-specific label printers. Vertex label: `gssIdx: (lrState, inputVertex)` (computed via `lrState * vertexCount + inputVertex`). Edge label: grammar symbol (from `RnglrGSS.outgoingEdges`). Highlight new vertices yellow, new edges red, current node lightblue.
        - LR automaton DOT: reuse `AutomatonDot.dfaToDot` with a wrapper that adds `fillcolor=lightblue` for the current LR state. State visualizer: render LR items via reusable logic from `LRAutomatonTikz`, with state number prefix. Start state has green fill, final (accept) states double border.
        - Path index TeX: reuse `PathIndexTeX.toTeXWithHighlights` (generic over `PathIndex<'t,'nt>`).
        - Input graph DOT: reuse `InputGraphDot.toDot` (generic). Current vertex highlighted lightgreen.
     3. Add RNGLR step template.
        - New file `data/RNGLR_step_template.tex`. Same three-panel `minipage` layout as `data/GLL_step_template.tex`. Placeholders: `__DESCRIPTORS_TABLE__` (left 7%), `__STEP_GSS_PDF__` (middle-top), `__STEP_LR_AUTOMATON_PDF__` (middle-bottom, 47% total), `__STEP_INPUT_PDF__` (right-top), `__PATH_INDEX__` (right-middle), `__NEW_DESCRIPTORS__` (right-bottom, 42% total).
     4. Update CLI runner.
        - `RnglrRunner.fs`: call `Rnglr.buildPathIndexWithSteps` instead of `Rnglr.buildPathIndex`. Render steps via `RnglrStepVisualizer.renderSteps`. Write per-step files.
        - `Helpers.fs`: add `writeRnglrStepsVisualization` — writes per-step files: `descriptors_table.tex`, `new_descriptors.tex`, `gss.dot`, `path_index.tex`, `input.dot`, `lr_automaton.dot`. Add `findRnglrStepTemplate` (same pattern as `findGllStepTemplate`).
     5. Update summary TeX generation.
        - `SummaryTeX.fs`: add `rnglrStepSection` (analogous to `gllStepSection`) — reads per-step files, fills `RNGLR_step_template.tex` placeholders. Add `rnglrColorLegend`. Update `buildContent` to route `SummaryKind.RNGLR` through per-step rendering (replace current static-only RNGLR header). Update `headerSection` to include LR automaton PDF/Tikz + color legend for RNGLR.
        - `Summary.fs`: for RNGLR, load `RNGLR_step_template.tex`, compile DOT files to PDF via `compileDotArtifacts`, pass template to `buildContent`.
     6. Add tests.
        1. RNGLR step output existence test: verify `writeRnglrStepsVisualization` produces all 6 expected files per step directory.
        2. RNGLR step golden test: generate full step visualization for grammar `S -> a a` with input `a a`, save all step artifacts as golden reference, compare.
        3. RNGLR summary compilation test: generate merged TeX with `-s` flag for the same grammar+input, compile with lualatex, verify success.
        4. RNGLR GSS DOT vertex/edge label format test: verify DOT output contains vertex labels of form `idx: (lrState, inputVertex)` and edge labels with grammar symbols. Reuse DOT parsing from existing GLL GSS tests.
        5. Add compilation tests for all parts.
     7. Reuse analysis.
        - Fully reusable as-is: `PathIndexTeX.toTeX`, `PathIndexTeX.toTeXWithHighlights`, `InputGraphDot.toDot`, `AutomatonDot.dfaToDot`, `GssDot.toDotFromSets`, `SppfDot.toDot`, `GrammarTeX.grammarToTeX`, `RnglrTableTeX.tableToTeX`, `ExternalTools.compileDotFileToPdf`, `ExternalTools.compileTexFile`, `Helpers.readFile`, `Helpers.writeOutputFile`, `Helpers.cleanOutputDir`, `Helpers.naturalSortKey`, `SummaryTeX.collectSteps`, all `SummaryTeX` TeX helpers (`wrapMath`, `wrapCenter`, `section`, etc.), `DerivationTreeDot.escapeLabel`, `MatrixTeX.toTeXStyled`, `Matrix.copy`.
        - Adaptable (same structure, new function): GLL's `descriptorsTableToTeX` → RNGLR version (2 columns vs 4), GLL's `newDescriptorsToTeX` → RNGLR version (simpler descriptor format), GLL's `descriptorToTeX` → RNGLR version, `GssDot.toDotFromSets` → RNGLR label printers, `SummaryTeX.gllStepSection` → `rnglrStepSection` (different placeholder names).
        - New required: `RnglrParsingStep` type, `Rnglr.buildPathIndexWithSteps`, `RnglrStepVisualizer` module + `RnglrVisualizationStep` type, `Helpers.writeRnglrStepsVisualization`, `Helpers.findRnglrStepTemplate`, `RNGLR_step_template.tex`, LR automaton DOT with state highlighting wrapper, RNGLR GSS label printers.
     8. All existing RNGLR tests must pass without changes. Step visualization must not alter algorithm behavior — `buildPathIndex` remains unchanged, `buildPathIndexWithSteps` must produce identical `PathIndex` output.

203. [done] Add solution level `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to turn all warnings to errors. Compile all projects, fix all problems.

204. [done] Deduplicate GLL/RNGLR shared algorithm helpers (code review R1, R2, R3).

     1. Extract `collectActiveGss` into shared location — identical logic in `Gll.fs:50` and `Rnglr.fs:314`. Both iterate GSS edge matrix to collect `(vertex, edge)` sets. Place in `GraphHelpers` module (`GllTypes.fs`) or a new shared module.
     2. Extract `addToIndex` into shared location — structurally identical local function in `Gll.fs:110` and `Rnglr.fs:101`. Both compute linear indices, check Set.contains, mutate matrix. Place in `PathIndex` module since it operates on path index.
     3. Extract `linearIndex` formula `state * vertexCount + vertex` — appears in 3 modules: `GSS.linearIndex` (GllTypes.fs:83), `RnglrGSS.linearIndex` (RnglrTypes.fs:60), `PathIndex.linearIndex` (PathIndex.fs:42). Create single shared implementation.
     4. All existing tests must pass without changes.

205. [done] Deduplicate test helpers for GSS DOT visualization (code review R4, R5, R6).

     1. Move `stripQuotes`, `vertexLabelRegex`, `edgeLabelRegex` from `GssDotVisualizationTests.fs` into shared `GoldenHelpers.fs` or a new `DotTestHelpers.fs` in `FLPQ.Printers.Tests`.
     2. Update both `GssDotVisualizationTests.fs` and `RnglrStepVisualizationTests.fs` to use shared helpers.
     3. All existing tests must pass without changes.

206. [done] Deduplicate GLL/RNGLR twin grammar test modules (code review N2).

     1. Extract shared grammar definitions (grammar1-4) and common accept/reject test structures into a shared test module or parameterized test framework.
     2. Both `GllTests.fs` and `RnglrTests.fs` should reference the same grammar definitions rather than duplicating ~400 lines of identical test structure.
     3. Consider using `[<Theory>]`/`[<InlineData>]` or a shared test data module to parameterize tests over algorithm (GLL vs RNGLR).
     4. All existing tests must pass without changes.

207. [done] Deduplicate `regexToDfa` / `buildBlockDfa` (code review N3).

     1. `regexToDfa` in `RPQTests.fs:237` duplicates `RsmBuilder.buildBlockDfa` (`EbnfParser.fs:268`). Both use Brzozowski derivatives.
     2. Either move shared implementation to a common location or have the test reuse `buildBlockDfa`.
     3. All existing tests must pass without changes.

208. [done] Deduplicate SPPF-extraction logic in CLI runners (code review N7).

     1. `gllTree`/`rnglrTree` share ~30 lines of nearly identical SPPF-extraction logic in `GllRunner.fs:26-52` and `RnglrRunner.fs:30-59`.
     2. Extract shared rootRanges construction and `Sppf.buildSppfFromIndex` calls into a common helper for SPPF.
     3. All existing tests must pass without changes.

209. [done] Remove empty XML doc comments (code review R7).

     1. Remove `///` on a line by itself in 5 locations: `MsBfs.fs:7,34`, `KroneckerRPQ.fs:10`, `ArroyueloRPQ.fs:9`, `BelyaninRPQ.fs:10`.

210. [done] Add test coverage for uncovered areas (code review 4.6, 4.7, 4.8, 4.9).

     1. Add direct unit test for `Nfa.epsilonClosure` — cover epsilon cycles, multi-step epsilon chains, self-loops.
     2. Extend RPQ generators beyond `["a"; "b"]` alphabet — add tests with larger alphabets and special characters.
     3. Add property-based tests (`[<Property>]`) for `Graph` operations: `filterOutgoing`, `filterIncoming`, `keepVertices`, `mapVertices`, `mapEdges`, `fromEdges`.
     4. Add dimension-consistency property test for `BooleanDecomposition.recompose` — verify all matrices in decomposition have same dimensions as original.
     5. All existing tests must pass without changes.

211. [done] Add LL(k>1) property-based equivalence tests (code review 4.4/N10).

     1. `LLParserTests.fs` has only `[<Fact>]` tests for k=2/k=3 with hardcoded strings.
     2. Add `[<Property>]` test comparing LL(k>1) acceptance against CYK/Valiant using FsCheck-generated grammars and inputs.
     3. All existing tests must pass without changes.

212. [done] Fix FsCheck property tests without registered Arbitrary (code review N9).

     1. `GllTests.GllCykEquivalence` (line 88) and `GllPropertyTreeYield` (line 520) produce mostly irrelevant inputs without `[<Properties(Arbitrary=...)>]`.
     2. Register custom Arbitrary generators so property tests receive meaningful inputs.
     3. All existing tests must pass without changes.

213. [done] Move `TokenStringGenerators` to shared `Generators.fs` (code review N6).

     1. `TokenStringGenerators` defined inline in `TokenizerTests.fs:144-151` instead of shared `Generators.fs`.
     2. Move to `FLPQ.TestUtilities/Generators.fs` for reuse by other test projects.
     3. All existing tests must pass without changes.

214. [done] Address GoldenHelpers.verifyGolden risk (code review 5.2).

     1. `GoldenHelpers.verifyGolden` creates golden files on first run when they don't exist — buggy output can be captured as golden.
     2. Add a safeguard: require explicit opt-in flag to create golden files.
     3. All existing tests must pass without changes.

215. Use [indexed properties](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/members/indexed-properties) to access Matrix elements instead of `get` and `set`.

     1. Replace all usages of `get` and `set`
     2. All tests must pass.

216. [done] RNGLR GSS visualization — show edge symbols to distinguish from GLL GSS.
     RNGLR GSS is a labeled automaton: edges carry grammar symbols (`RnglrGssEdge { EdgeSymbol: Symbol<'t,'nt> }`), and these symbols drive the BFS-based product intersection with inverted RSM blocks. Current GSS DOT visualization renders edges as bare `"lrState,v → lrState,v"` coordinate pairs — identical to GLL's GSS DOT output — hiding the fundamental structural difference. The visualization must show edge symbols so the RNGLR GSS reads as a labeled directed automaton.

     1. `RnglrTypes.fs` — `RnglrParsingStep`: add field `ActiveGssEdgeSymbols: Map<int*int, NonEmptySet<Symbol<'t,'nt>>>` — maps each `(fromIdx, toIdx)` GSS edge pair to its grammar symbol(s). Multiple symbols on the same vertex pair are possible (different reduce/shift paths produce edges with distinct symbols between the same vertices).
     2. `Rnglr.fs` — in step collection callback (`onStep` in `buildPathIndexWithSteps`), populate `ActiveGssEdgeSymbols` from the GSS: iterate active vertices, call `RnglrGSS.outgoingEdges`, build the `Map<int*int, NonEmptySet<Symbol<'t,'nt>>>` grouping symbols by edge pair.
     3. `RnglrStepVisualizer.fs` — GSS DOT edge label printer: use `ActiveGssEdgeSymbols` to render comma-separated grammar symbols (e.g., `"a"` for terminal, `"S"` for nonterminal, `"a, S"` if multiple symbols share the same edge pair). Replace the current coordinate-pair label (not needed — vertex labels already show coordinates). Vertex labels unchanged for this task.
     4. `GoldenHelpers.fs` — add RNGLR-specific `rnglrEdgeLabelRegex` matching symbol-only labels (e.g., `^"?[A-Za-z0-9ε, ]+"?$`), distinct from the current GLL `edgeLabelRegex` (`^\d+,\d+ → \d+,\d+$`).
     5. `RnglrStepVisualizationTests.fs` — update GSS DOT vertex/edge label format test to use `rnglrEdgeLabelRegex` for RNGLR. Regenerate all 6 RNGLR golden files (`rnglr_gss_step0.dot`, `rnglr_descriptors_table_step0.tex`, `rnglr_new_descriptors_step0.tex`, `rnglr_path_index_step0.tex`, `rnglr_input_step0.dot`, `rnglr_lr_automaton_step0.dot`). All existing RNGLR tests must pass.
     6. `GssDotVisualizationTests.fs` — unchanged (tests GLL GSS only). RNGLR GSS DOT tests remain in `RnglrStepVisualizationTests.fs`.

217. [done] RNGLR Descriptor refactoring — explicit GssIdx + continuous GSS vertex numbering from 0.
     Current `RnglrDescriptor = { LrState; Vertex }` derives the GSS vertex at every use site via formula `lrState * vertexCount + v`. GLL's `Descriptor` carries explicit `GssIdx` — RNGLR must too. Additionally, GSS pre-allocates all `|Q_lr| × |V|` vertices as a dense matrix; refactoring creates vertices lazily on-demand with sequential IDs (0, 1, 2, ...), decoupling GSS indexing from the PathIndex grid formula. PathIndex stays unchanged.
     **Prerequisite**: Task 216 (RNGLR GSS visualization) must be done first — this task inherits the edge-symbol-aware visualization and adapts vertex labeling for the new numbering scheme.

     1. `RnglrTypes.fs` — `RnglrDescriptor`: add `GssIdx: int` field → `{ LrState: int; Vertex: int; GssIdx: int }`. Update XML doc comment to reflect 3-field structure.
     2. `RnglrTypes.fs` — `RnglrGSS` type redesign from pre-allocated matrix to lazy on-demand structure:
        - Replace `GssGraph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t,'nt>>>>` with:
          - `VertexLookup: Dictionary<int*int, int>` — `(lrState, v) → gssIdx`, O(1) lookup
          - `VertexInfo: ResizeArray<int*int>` — reverse mapping `gssIdx → (lrState, v)`, O(1) random access
          - `Edges: Dictionary<int, Dictionary<int, NonEmptySet<RnglrGssEdge<'t,'nt>>>>` — adjacency map, sparse (only allocated for existing vertices)
        - Replace `StoredStates: Set<...>[]` with `StoredStates: Dictionary<int, Set<Nonterminal<'nt> * int * int * int>>` — resizeable, matches dynamic vertex creation
        - Remove `RnglrGSS.linearIndex` (formula no longer needed)
        - Remove `RnglrGSS.init` (no pre-allocation)
        - Add `RnglrGSS.create() : RnglrGSS<'t,'nt>` — returns empty GSS (all dictionaries empty, no vertices)
        - Add `RnglrGSS.getOrCreateVertex(gss, lrState, v) → int` — if `(lrState, v)` already has a GSS vertex, return its ID; otherwise allocate next sequential ID, add to `VertexLookup` and `VertexInfo`, return new ID
        - Add `RnglrGSS.getVertexInfo(gss, gssIdx) → int*int` — returns `(lrState, v)` for a GSS vertex; replaces `Graph.getVertex gssIdx gss.GssGraph`
        - Update signatures (no changes needed): `addEdge`, `outgoingEdges`, `getStoredStates`, `setStoredStates` — they already take `gssIdx: int` and work with indices; adapt internals to Dictionary-based storage
     3. `GllTypes.fs` — `GraphHelpers` module: add `collectActiveGssForDict(edges: Dictionary<int, Dictionary<int, 'a>>) → Set<int> * Set<int*int>` for Dictionary-based edge storage. Same return type as the Matrix-based `collectActiveGss`. Iterates dictionary keys (source vertices) and nested dictionary keys (target vertices) to collect active vertices and edge pairs.
     4. `Rnglr.fs` — core algorithm adaptation:
        - Replace `let gss = RnglrGSS.init lrStateCount vertexCount` → `let gss = RnglrGSS.create()`
        - Remove local `linearIdx` function
        - At each descriptor creation site:
          - Initial descriptor: `let gssIdx = RnglrGSS.getOrCreateVertex gss 0 0` → `{ LrState = 0; Vertex = 0; GssIdx = gssIdx }`
          - Shift target: `let targetGssIdx = RnglrGSS.getOrCreateVertex gss targetLrState vNext` → `{ LrState = targetLrState; Vertex = vNext; GssIdx = targetGssIdx }`
        - Replace all `Graph.getVertex idx gss.GssGraph` → `RnglrGSS.getVertexInfo gss idx` (in `productBfs`, `findPredecessors`)
        - Replace `GraphHelpers.collectActiveGss gss.GssGraph.Edges` → `GraphHelpers.collectActiveGssForDict gss.Edges`
        - `processedGotos`: replace `Set<Nonterminal<'nt> * int> array` of fixed size `lrK` with `Dictionary<int, Set<Nonterminal<'nt> * int>>` — keyed by `gotoGssIdx`, lazy allocation
        - In `productBfs`: use `RnglrGSS.getVertexInfo gss currGss` and `RnglrGSS.getVertexInfo gss nextGss` instead of `Graph.getVertex`
        - In `processReduction`: `gotoGssIdx` via `RnglrGSS.getOrCreateVertex gss gotoTarget vEnd`
        - Edge symbol collection (Task 216 code in step callback): adapt to use `RnglrGSS.outgoingEdges` (unchanged signature, just internal Dictionary-based iteration)
        - All existing algorithm logic preserves behavior — the refactoring changes only how GSS vertices are created and accessed, not what the algorithm computes
     5. `RnglrStepVisualizer.fs` — visualization adaptation:
        - `rnglrDescriptorToTeX`: render 3-field descriptor `(lrState, vertex, gssIdx)` instead of `(lrState, vertex)`
        - `descriptorsTableToTeX`: header becomes `lrState & input & gssIdx` (3 columns instead of 2), `renderRow` renders 3 values
        - `newDescriptorsToTeX`: uses `rnglrDescriptorToTeX` — automatically picks up 3-field format
        - `renderStep` — `currentGssIdx` directly from `step.CurrentDescriptor.Value.GssIdx` instead of computing via formula
        - GSS DOT vertex label printer: use `RnglrGSS.getVertexInfo gss gssIdx` to get `(lrState, v)` for label `"idx: (lrState, v)"`. The visualizer needs access to vertex info — pass a lookup function `(int → int*int)` or carry `VertexInfo` in step data. Vertex indices are now sequential (0, 1, 2, ...) instead of formula-derived (0, vc, 2\*vc, ...).
        - GSS DOT edge label printer: symbols from `ActiveGssEdgeSymbols` (from Task 216) — unchanged
     6. `RnglrRunner.fs` — pass vertex info to visualizer if needed (e.g., `RnglrGSS.getVertexInfo` as a lookup lambda, or carry reverse mapping in step data).
     7. Golden data — all 6 RNGLR golden files regenerated (descriptor now 3 fields, GSS vertex numbering is sequential, edge symbols already present from Task 216). Update `GoldenHelpers.fs` regex patterns if vertex label format changes.
     8. Tests — `RnglrTests.fs` (all algorithm tests), `RnglrRunnerTests.fs` (CLI output), `RnglrStepVisualizationTests.fs` (golden + invariants) — all must pass. No skipped tests.
     9. `docs/developer/rnglr.md` — update type definitions: `RnglrDescriptor` (now 3 fields), `RnglrGSS` (lazy vertex creation, Dictionary-based storage). Update GSS module functions table: remove `linearIndex`, `init`; add `create`, `getOrCreateVertex`, `getVertexInfo`. Update design decisions table: new entry "GSS vertices created on-demand with sequential IDs" replacing "Vertices pre-allocated as |Q_lr| * |V|"; update "RnglrDescriptor struct type" entry to reflect 3-field structure.

218. [done] RNGLR steps visualization — LR table with highlighted actions instead of LR automaton DOT.
     In each RNGLR step, replace the LR automaton DOT figure with the LR parsing table (ACTION/GOTO) and highlight the active state row and the specific cells consumed during this step. Highlighting scheme: current LR state row gets `\rowcolor{yellow!20}`; cells for actions taken this step (shift terminals + reduce nonterminals) get `\cellcolor{green!20}`; accumulated reductions across all descriptors at the current input vertex ("level reductions") get `\cellcolor{red!20}`. The table replaces the automaton completely — no more LR automaton DOT in steps.

     1. `RnglrTypes.fs` — `RnglrParsingStep`: add three fields:
        - `ActiveShiftTerminals: Set<Terminal<'t>>` — terminals shifted this step (those with matching input edges from current vertex and SHIFT action in table)
        - `ActiveReduceNonterminals: Set<Nonterminal<'nt>>` — nonterminals reduced at this LR state (from `getReduceNtWithStates`)
        - `LevelReductions: Set<Nonterminal<'nt>>` — accumulated set of all nonterminals reduced across all descriptors processed at the current input vertex (resets when vertex changes)
     2. `Rnglr.fs` — capture action data during step collection in `buildPathIndexWithSteps`:
        - Add mutable ref cells: `stepShiftTerminals: Set<Terminal<'t>> ref`, `stepReduceNt: Set<Nonterminal<'nt>> ref`, `levelReductions: Set<Nonterminal<'nt>> ref`, `prevInputVertex: int ref`
        - In `processNode`: before the reduce loop, add each `reduceNt` from `getReduceNtWithStates lrState` to both `stepReduceNt` and `levelReductions`. Before the shift loop, at the point where `lrTable.Action` confirms a SHIFT, add `Terminal tVal` to `stepShiftTerminals`. At start of `processNode`, if `v ≠ prevInputVertex.Value`, reset `levelReductions.Value` to empty and set `prevInputVertex.Value ← v`.
        - In step callback (`onStep`): populate the three `RnglrParsingStep` fields from the ref cells, then clear `stepShiftTerminals` and `stepReduceNt` (reset to empty). Do NOT clear `levelReductions` — it persists across steps at same vertex and resets only on vertex change in `processNode`.
     3. `RnglrTableTeX.fs` — add `tableToTeXWithHighlights` function:
        - Signature: `tableToTeXWithHighlights (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (table: RnglrTable<'t,'nt>) (currentLrState: int option) (activeActions: Set<Symbol<'t,'nt>>) (levelReductions: Set<Nonterminal<'nt>>) : string`
        - Same tabular structure as `tableToTeX`. Per-row: if state equals `currentLrState`, wrap row with `\rowcolor{yellow!20}`. Per-cell: if the cell's symbol is in `activeActions`, wrap cell content with `\cellcolor{green!20}`. If the cell corresponds to a nonterminal in `levelReductions`, wrap with `\cellcolor{red!20}`. Color priority: red (level reductions) overrides green (active actions) which overrides yellow (row). Requires `\usepackage[table]{xcolor}` or `\usepackage{colortbl}` in preamble — note this in rendering (callers must include the package).
        - If `currentLrState = None` (initial step before any descriptor), render plain table with no highlights.
        - Keep existing `tableToTeX` unchanged (used for output-independent rendering of the full table, e.g., `rnglr_table.tex` in runner).
     4. `RnglrStepVisualizer.fs` — visualizer changes:
        - `RnglrVisualizationStep`: replace `LrAutomatonDot: string` field with `LrTable: string` (TeX content, not DOT).
        - Remove `lrAutomatonToDot` private function entirely.
        - Remove `symbolToDotLabel` helper (only used by `lrAutomatonToDot`).
        - In `renderStep`: compute `activeActions = (Set.map (fun (Terminal t) -> Symbol.T(Terminal t)) step.ActiveShiftTerminals) + (Set.map (fun nt -> Symbol.N nt) step.ActiveReduceNonterminals)`. Call `RnglrTableTeX.tableToTeXWithHighlights` with `activeActions`, `step.CurrentLrState`, `step.LevelReductions`. Store in `LrTable`.
     5. `Helpers.fs` — `writeRnglrStepsVisualization`:
        - Replace `writeOutputFile (Path.Combine(stepDir, "lr_automaton.dot")) steps.[idx].LrAutomatonDot` → `writeOutputFile (Path.Combine(stepDir, "lr_table.tex")) steps.[idx].LrTable`.
        - Remove any LR automaton DOT → PDF compilation from this function (LR table is inline TeX, no PDF needed).
     6. `data/RNGLR_step_template.tex` — replace LR automaton PDF placeholder:
        - Remove: `\begin{center}\includegraphics[width=\textwidth,keepaspectratio]{__STEP_LR_AUTOMATON_PDF__}\end{center}`
        - Add: `\begin{center}\resizebox{\textwidth}{!}{$__LR_TABLE__$}\end{center}`
     7. `SummaryTeX.fs` / `Summary.fs` — summary generation:
        - `SummaryTeX.rnglrStepSection`: replace `__STEP_LR_AUTOMATON_PDF__` placeholder substitution with `__LR_TABLE__` inline TeX substitution (read `lr_table.tex` file content directly, like `__DESCRIPTORS_TABLE__`).
        - `Summary.fs`: remove LR automaton DOT → PDF compilation for RNGLR (`compileDotArtifacts` for `lr_automaton.dot`). Remove `dot_pdfs/..._lr_automaton.pdf` entries from dot artifact lists.
     8. Tests:
        - `RnglrStepVisualizationTests.fs`: replace golden test "lr_automaton step 0" → "lr_table step 0" (`rnglr_lr_table_step0.tex`). Remove `RNGLR LR automaton DOT compiles` test (no more DOT). Add `RNGLR LR table TeX compiles` test.
        - `RnglrRunnerTests.fs`: check `lr_table.tex` exists instead of `lr_automaton.dot` in per-step output.
        - Golden data: delete `rnglr_lr_automaton_step0.dot`, create `rnglr_lr_table_step0.tex`.
        - All existing RNGLR tests must pass. No skipped tests.

219. [done] Language Registry and test infrastructure refactoring. Create a specification document and typed F# registry module that catalogs all test languages, their grammars (with manually-verified properties: left-recursive, ambiguous, has-epsilon, in-CNF, etc.), accept/reject strings, and string generators. Group grammars strictly by formal language equivalence (same language = same group). Restructure TestGrammars.fs to re-export from the registry. Refactor all algorithm test files (CYK, Valiant, GLL, RNGLR, LL, LR, SharedParsingTests) to use registry types and helpers. Update tests-writer skill with a workflow section describing how to use the registry. No algorithm compatibility bindings in the registry — the developer uses grammar properties + algorithm knowledge to decide compatibility. No undecidable property detection — all properties are manual annotations verified by the developer.

220. [done] Refactor AnnotatedGrammar type. Current design has an IsEbnf boolean flag and uses a dummy parseG "S -> a" placeholder for EBNF-only grammars — ugly and error-prone. Replace with a clean design where the canonical text is the single source of truth, and both CFG (Grammar\<string,string>) and RSM (RSM\<string,string>) representations are derived from it. The AnnotatedGrammar record stores: Text (canonical CFG/EBNF text), Grammar, AugmentedGrammar, Rsm. No IsEbnf flag, no dummy grammars, no EbnfText field. For EBNF-only texts, Grammar is obtained via RsmToGrammar.rsmToGrammar as a fallback when Grammar.parseGrammar fails. In tests, stop calling TestHelpers.grammarToRsm and RsmBuilder.buildRSMFromText — use g.Rsm directly from the registry entry. The TestHelpers.checkReject function should accept RSM instead of Grammar to eliminate the internal grammarToRsm conversion. Drop the IsEbnf filter from collectAcceptFailures/collectRejectFailures — all entries have valid Grammar. No new types — only existing types. All 500 tests must pass with zero regressions.

221. [done] Comprehensive test refactoring: unify GLL/RNGLR tests, consolidate cross-parser equivalence, eliminate TestGrammars indirection, fix broken equivalence tests, merge duplicated generators.
     **Context**: After tasks 219-220 the LanguageRegistry is established as the single source of grammars, accept/reject strings, and generators. However, the test suite still has significant structural problems.

     **Problem 1: Copy-paste duplication between GllTests.fs and RnglrTests.fs**.
     The two files are near-identical mirrors (~360 and ~484 lines). Same submodules, same test structure, same grammars, same inputs — only the `accepts` binding differs. SharedParsingTests.fs parameterizes only 3 categories (epsilon, regex, tree-yield), leaving ~70% of tests duplicated:

     - `GllAcceptance` / `RnglrAcceptance` (~75 / ~80 lines) — basic accept/reject. Inconsistent grammar sources: some use `TestGrammars.grammarS2a`, others use `singleA.Grammars[0].Rsm`.
     - `GllGrammarAcceptanceAndTree` / `RnglrGrammarAcceptanceAndTree` (~50 / ~60 lines) — grammars 11-14. Structurally identical.
     - `GllGrammar159A-D` / `RnglrGrammar159A-D` (~30 / ~50 lines) — four single-test submodules each.
     - `GllRightNullable` / `RnglrRightNullable`, `GllReductionCascade` / `RnglrReductionCascade`, `GllPropertyTreeYield` / `RnglrPropertyTreeYield` — all duplicated.

     **Problem 2: Broken RNGLR vs GLL equivalence tests (always true)**.
     `RnglrTests.fs:110-113` and `:124-127` compare `accepts = accepts` (same binding on both sides). Always returns `true`. Provides zero coverage. Same bug in APlus variant.

     **Problem 3: Cross-parser equivalence scattered across 6 files**.
     12+ pairwise comparisons defined wherever convenient: GLL vs CYK in `GllTests.fs`, RNGLR vs CYK in `RnglrTests.fs`, Valiant vs CYK in both `ValiantTests.fs` and `LLParserTests.fs`, LL vs SLR/CLR/CYK in `LLParserTests.fs`, SLR vs CLR/CYK in `LRParserTests.fs`. No single place to see "do all parsers agree?"

     **Problem 4: TestGrammars.fs is an unnecessary indirection layer**.
     125 lines of aliases that look up from LanguageRegistry by name. After tasks 219-220, the registry has `AnnotatedGrammar` with `Grammar`, `AugmentedGrammar`, `Rsm`, `Text`. Tests should reference the registry directly.

     **Problem 5: Duplicated string generators**.
     `Generators.fs` and `LanguageRegistry.fs` both define `abStringGen`/`AbStringGenerators`, `aStringGen`/`AStringGenerators`, etc. with identical distributions and bounds.

     **Problem 6: Submodule proliferation**.
     10+ submodules per parser, many with a single test (`GllGrammar159A` = 1 test, `GllReductionCascade` = 1 test). Navigation noise.

     **Proposed structure**:

     ```
     tests/FLPQ.Languages.Tests/
       GllTests.fs                    — GLL-specific + shared acceptance via RSM adapter
       RnglrTests.fs                  — RNGLR-specific + shared acceptance via RSM adapter
       CykTests.fs                    — CYK-specific + shared acceptance via Grammar adapter
       LLParserTests.fs               — LL-specific + shared acceptance via Grammar adapter
       LRParserTests.fs               — LR-specific + shared acceptance via Grammar adapter
       ValiantTests.fs                — Valiant-specific + shared acceptance via Grammar adapter
       CrossParserEquivalenceTests.fs — NEW: all pairwise equivalence, consolidated from 6 files
       SharedParsingTests.fs          — DELETE (moved to TestUtilities)
       TestGrammars.fs                — DELETE

     tests/FLPQ.TestUtilities/
       ParsingTestCases.fs            — NEW: moved from SharedParsingTests.fs, extended with AcceptanceCase type, cases for ALL languages
       LanguageRegistry.fs            — Add Gen→Arbitrary bridge, consolidate string generators
       Generators.fs                  — Remove duplicated string generators (keep non-string)
       TestHelpers.fs                 — Unchanged (already parameterized from task 183)
     ```

     **Subtasks**:

     1. Move `SharedParsingTests.fs` to `FLPQ.TestUtilities/ParsingTestCases.fs`. Extend with shared acceptance cases for ALL languages.

        - Define `AcceptanceCase { CaseName: string; LanguageName: string; GrammarName: string; Rsm: RSM<string,string>; Grammar: Grammar<string,string>; Input: string list; ExpectedAccepted: bool }`.
        - Module `AcceptanceCases`: iterate all languages in `LanguageRegistry.allLanguages`, for each language iterate `Grammars`, for each grammar iterate `AcceptStrings` (expected=true) and `RejectStrings` (expected=false). Use prebuilt `AnnotatedGrammar.Rsm` and `AnnotatedGrammar.Grammar` from the registry — no conversion needed.
        - Module `TreeYieldCases`: shared tree-yield test cases (grammar + inputs that should produce trees with matching leaves). Source from `LanguageRegistry.Dyck1.AcceptStrings`, `APlus.AcceptStrings`, etc.
        - Module `RegexEquivalenceCases`: regex patterns + filter functions for DFA comparison.
        - Module `EpsilonCases`: all epsilon grammars from `LanguageRegistry.EpsilonOnly`.
        - Pure data, no `[<Fact>]` attributes, no parser-specific logic.

     2. Refactor `GllTests.fs` to use shared cases.

        - Replace `GllAcceptance` submodule with one `[<Fact>]` per grammar: iterate `ParsingTestCases.AcceptanceCases.forLanguage "Dyck1"` etc., for each case assert `accepts case.Rsm case.Input = case.ExpectedAccepted`. The fact name includes language and grammar names so failures are pinpointed.
        - Replace `GllGrammarAcceptanceAndTree` Grammar1-4 submodules with shared cases loop.
        - Merge `GllGrammar159A-D` into a single `GllTreeYield` submodule using `ParsingTestCases.TreeYieldCases`.
        - Keep GLL-specific tests: descriptor/GSS visualization, step traces.
        - Replace all `TestGrammars.xxx` references with direct `LanguageRegistry.xxx.Grammars[n]` access.

     3. Refactor `RnglrTests.fs` symmetrically.

        - Same pattern as GLL: shared cases for acceptance, tree yield, epsilon, right-nullable, reduction cascade.
        - Fold `RnglrGrammarTests` (lines 406-484) into shared cases — the `accepts` helper already validates SPPF which is stricter than raw RSM→path-index without SPPF validation.
        - Keep RNGLR-specific tests: SPPF construction (`SppfDotTests`), dual Dyck language tests.
        - Replace all `TestGrammars.xxx` references with direct registry access.

     4. Create `CrossParserEquivalenceTests.fs`.

        - Module `VsCyk`: GLL vs CYK, RNGLR vs CYK, Valiant vs CYK, LL vs CYK, SLR vs CYK, CLR vs CYK. Use prebuilt `AnnotatedGrammar.Grammar` for CYK/LL/LR/Valiant and `AnnotatedGrammar.Rsm` for GLL/RNGLR. Property-based with FsCheck generators from the registry.
        - Module `VsDfa`: GLL vs DFA, RNGLR vs DFA for regex-derived grammars. Move from `GllTests.fs` and `RnglrTests.fs`.
        - Module `GllVsRnglr`: direct comparison. Fix the broken test: define separate `gllAccepts = TestHelpers.accepts Gll.buildPathIndex PathIndex.isAccepted` and `rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted`, compare across them using shared cases from registry.
        - Module `LlVsLr`: LL vs SLR, LL vs CLR, SLR vs CLR. Move from `LLParserTests.fs` and `LRParserTests.fs`.
        - Module `ValiantVsCyk`: move from `ValiantTests.fs` and `LLParserTests.fs`.

     5. Refactor `CykTests.fs`, `LLParserTests.fs`, `LRParserTests.fs`, `ValiantTests.fs`.

        - Each uses shared acceptance cases via a Grammar adapter: `Grammar → Terminal list → bool`. One `[<Fact>]` per grammar, iterating `ParsingTestCases.AcceptanceCases.forLanguage "..."`.
        - Remove cross-parser equivalence tests (moved to step 4).
        - Keep algorithm-specific tests: CYK trace/table, LL conflict detection and k>1 resolution, LR automaton structure and conflict types, Valiant matrix comparison and modified Valiant.
        - Replace all `TestGrammars.xxx` references with direct registry access.

     6. Consolidate string generators.

        - In `LanguageRegistry.fs`: add a `GenToArbitrary` helper that wraps `Gen<string>` into `Arbitrary<string>`.
        - In `Generators.fs`: remove `AbStringGenerators`, `AStringGenerators`, `ExprStringGenerators`, `AbcdxyStringGenerators`. Replace with references to the corresponding LanguageRegistry generators via the wrapper.
        - Keep non-string generators in `Generators.fs`: `MatrixGenerators`, `LinearAlgebraGenerators`, `SetMatrixGenerators`, `RandomGraphGenerators`, `RPQGenerators`, `RPQExtendedAlphabetGenerators`, `IntersectionGenerators`, `RegexGenerators`, `RegexAndGraphGenerators`, `StressStringGenerators`, `StressNfaGenerators`, `StressRpqGenerators`, `StressMatrixGenerators`.
        - Update all `[<Properties(Arbitrary = [...])>]` attributes across test files to use the consolidated generators.

     7. Delete `TestGrammars.fs`.

        - Replace all references across test files with direct registry access.
        - Add a `findGrammar` helper to LanguageRegistry: `findGrammar (language: Language) (name: string) : AnnotatedGrammar` for cases where named lookup is clearer than index-based access.

     8. Run all tests. Zero regressions. All existing test assertions must pass. The refactoring changes how tests are organized and how grammars are accessed, but not what is tested or the expected outcomes.

222. [done] Improve LR table inclusion to RNGLR smmary.

     1. In summary do not use additional inner centering.
     2. In summary do not use wrapping to math $ ... $
     3. In summary scale to 0.3\\textwidth.
        So, final correct example:

     ```
     \begin{center}
     \resizebox{0.3\textwidth}{!}{%%
     \begin{tabular}{ c || c | c || c }
     & a & \$ & S \\ \hline
     \hline 0 & $s_1$ &  & 2 \\
     \hline 1 & $r_{S}$ & $r_{S}$ &  \\
     \hline 2 & $s_1$ & acc & 3 \\
     \hline 3 & $s_1$ & $r_{S}$ & 4 \\
     \hline 4 & $s_1$ & $r_{S}$ & 4 \\ [1ex]
     \end{tabular}
     }
     \end{center}
     ```

     Use function for table content generation, and reuse additional wrappers cretaion that reqiored for summary or for step generation.

223. [done] Fix RNGLR summary compilation.

     1. run `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -a rnglr -i data/example_input_a_a_a.txt -g data/example_grammar_a_a_a.bnf -s -o viz_output/glr`
     2. compile result: `lualatex rnglr_merged.tex`
     3. now i got error. Fix it. Error:

     ```
     ! Package xcolor Error: Undefined color `lightblue'.

     See the xcolor package documentation for explanation.
     Type  H <return>  for immediate help.
     ...                                              
                                                       
     l.29 ...htblue!20}{\rule{0pt}{2ex}\rule{1.2em}{0pt}}
                                                       & Current GSS node \\
     ? 
     ```

     4. Add summary compilation test.

224. [done] Implement basic SPPF (Rekers-style) type. Current Sppf.fs implements the RSM-based SPPF with 5 node types. The basic SPPF is simpler — built directly from BNF productions with numbered rules, only 2 structural node types (symbol + production), matching the classical derivation tree structure.

     1. Types in new file `src/FLPQ.Languages/BasicSppf.fs`:
        - `BasicSppfNodeInfo<'t,'nt>`: DU = `Terminal of Terminal<'t> * leftPos: int * rightPos: int` | `Nonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int` | `Epsilon of pos: int` | `Production of ruleIndex: int * leftPos: int * rightPos: int`.
        - `BasicSppfEdgeLabel`: DU = `Derives` (nonterminal → production) | `ChildOf of positionInRhs: int` (production → child node).
        - `BasicSPPF<'t,'nt>`: record = `{ Graph: Graph<BasicSppfNodeInfo<'t,'nt>, Option<BasicSppfEdgeLabel>>; RootIndex: int }`.
     2. Node semantics (per def:basicSPPF):
        - Terminal node ($a\_{i,i+1}$): leaf, stores matched terminal at position $i$.
        - Nonterminal node ($X\_{i,j}$): internal, one node per unique $(X, i, j)$. May have multiple production children (packing alternatives).
        - Epsilon node ($\\varepsilon_i$): leaf, empty derivation at position $i$.
        - Production node (rule $k$): internal, stores rule index and span. Parent is single nonterminal node $(X_k)\_{i,j}$. Children are terminal/nonterminal/epsilon nodes for each element of RHS $\\alpha_k$, covering $\[i, j)$ with split points.
     3. Tree extraction function: similar to existing lazy trees enumeration for existing SPPF.
     4. DOT visualization in `src/FLPQ.Printers/BasicSppfDot.fs`:
        - Nonterminal nodes: rounded rectangles with label `$X_{i,j}$`.
        - Terminal/Epsilon nodes: circles with label `$a_{i,i+1}$` / `$\varepsilon_i$`.
        - Production nodes: ovals with rule number.

225. [done] Reorganize RNGLR to canonical shift-then-reduce ordering with per-input-position visualization steps.

     1. Reorder `processNode` in `Rnglr.fs`: shift first, then reduce. Shift creates terminal GSS edges and descriptors at V+1, then reduce phase cascades through the GSS (which now includes freshly-shifted terminal edges). storedStates from reductions at V are consumed during shifts at V+1 (canonical per-position semantics).
     2. Change step capture from per-descriptor to per-input-position: move the `onStep` call from inside the per-descriptor `while` loop to after the while loop completes for each vertex. Step 0 stays initial empty state. Step 1 = after all descriptors at v=0, step 2 = after v=1, etc.
     3. Reset `stepShiftTerminals`/`stepReduceNt` per-vertex instead of per-descriptor. `levelReductions` logic stays vertex-aware.
     4. Update `CurrentDescriptor`/`CurrentLrState` to `None` for position-level steps (no single descriptor). `AttemptedDescriptors`/`NewDescriptors` reflect all descriptors from the entire vertex.
     5. Update visualization: descriptors table shows ALL descriptors at the vertex in "to handle" block; "current" descriptor row removed.
     6. Update golden data for step 0 (unchanged) and regenerate any other golden files. Update tests that check step-specific content.
     7. Update `docs/developer/rnglr.md`: fix algorithm description (currently says "shift then reduce" but implementation did opposite), update design decisions.
     8. All existing RNGLR tests must pass.

226. [done] Extend CYK with data to construct BasicSPPF: each cell for each nonterminal stores all k that allows to easely reconstruct two childrenb cells, and respective production number.

227. [done] Add indexed operations for Matrix: elementwize operations has access to element indices.

     1. map2i --- as F# map2i or mapi.
     2. mxmi --- op_mult and op_add has access to element indices
     3. Add tests on indexed operations. Both facts and property.

228. [done] extend Valiant and its modifiacton to compute information fo BasicSPPF.

     1. each cell for each nonterminal stores all k that allows to easely reconstruct two childrenb cells, and respective production number.
     2. Use indexed operations for it.
     3. Grammar may be captured using closure.

229. [done] Add table to BasicSPPF function.

     1. CYK and Valiand (+modified) build similar tables
     2. Input is a table, output is a BasicSPPF

230. [done] Add Tests on new Valiant and CYK. Extend existing tests to check complex invariants. Create common check fucntion. Be sure that all respective tests are property tetst.

     1. For same grammar and input CYK, Valiant, Modified Valiant built exactly the same tabales
     2. SPPF extracted from Tables for same grammar and input CYK, Valiant, Modified Valiant, exactly the same
     3. Leaves of tree extracted from SPPF is a input string.
     4. Implement function that treats BasicSPPF as a directed graph and computes number strongly conneted components (SCC).
     5. For same grammar and input CYK, Valiant, Modified Valiant built SPPF with identical numer of SCC.

231. [done] Improve CYK and Valiant visualization.

     1. Improve cell content rendering: sell is a set of tuples of form <nonterminal>,<k>,\<prod_id>
     2. Add final SPPF visualization. Include SPPF to summary.

232. [done] Add one more property check for SPPF checking for GLL and RNGLR

     1. Treat SPPF as directed graph, implement algorithm to compute number of nontrivial strongly connected components (SCC). Nontrivial --- number of vertices more than one. Can algo for BasicSPPF be reused or generalized?
     2. Add invariant: for same grammar and input SPPF from GLL and from CYK has same number of nontrivial SCC.
     3. Add invariant: for same grammar and input SPPF from GLL and from RNGLR has same number of nontrivial SCC.
     4. Add invariant: for same grammar and input SPPF from GYK and from RNGLR has same number of nontrivial SCC.

233. [done] Store AcceptStrings and RejectStrings in LanguageRegistry as `Terminal<string> list` (pre-tokenized). Adapt helpers and callers to consume tokenized strings directly, removing ~175 `tokenizeTerminals`/`List.map Terminal` conversions throughout the test suite. Update `terminalsToGraph`, `accepts`, `checkReject`, `collectAcceptFailures`, `collectRejectFailures` signatures to accept `Terminal<string> list`. Remove `tokenized` and `acceptStrToSpace` helpers from test files.

234. [done] Extend GllVsRnglr property tests on all existing grammars and languages from language registry. Design it to avoid massive code duplication.

     1. Note that some new tests may fail. Fix respective algortihm.
     2. Note that SCC-s count for RNGLR looks more correct. So, be careful with algorithms analysis and fixes.

235. [done] Fix grammar2 filter workaround in CrossParserEquivalenceTests GllVsRnglr. The grammar2 (S -> a S b | eps | S S) is filtered out (`g.Name <> "grammar2"`) because GLL and RNGLR produce different SCC counts. The root cause is that GLL uses PEpsilonNonterminal in places where RNGLR uses PNonterminal, creating structurally different SPPFs. Analyze GLL carefully and fix the PEpsilonNonterminal/PNonterminal discrepancy so that grammar2 produces matching SCC counts without the filter.

236. Reorganize GllVsCyk and RnglrVsCyk tests in CrossParserEquivalenceTests using the GllVsRnglr iteration pattern.

     1. Replace individual `[<Property>]` tests (one per grammar: Dyck1 grammar1/grammar2, APlus grammar3/grammar4) with `[<Fact>]` + `Check.One` tests that iterate over all languages and all their grammars, grouped by string generator type.
     2. Compare acceptance only — CYK does not produce an SCC-compatible SPPF, so no SCC comparison.
     3. GLL/RNGLR: use `TestHelpers.accepts` with `g.Rsm` (already precomputed in AnnotatedGrammar). CYK: use `TestHelpers.cykAccepts` with `g.Grammar`.
     4. Minimal changes — no new types, no new helpers, no CNF precomputation.

237. [done] Improve GLL visualization. Split initialization from steps. Initialization draws initial state with the same template, but no new descriptors, no highlighted vertices in RSM, GSS, input, no highlighted descriptor in descriptors table. Just initial descriptor in it. For steps flow we visualize one descriptor handling. Table contains descriptors at start of step. Current descriptor highlighted. Current position, RSM state, GSS vertex highlighted. Changes highlighted: new vertices and edges in GSS, updated cells in path index.

     1. Add `renderInit` function to `GllStepVisualizer.fs` — renders init step with zero highlights: GSS without `currentVertex`/`highlightedVertices`/`highlightedEdges`, RSM without `highlightedState`, input without position highlight, path index via `PathIndexTeX.toTeX` (no cell highlights), new descriptors as `\{ \emptyset \}` (no green/red boxes).
     2. Dispatch init vs regular step in `renderSteps` — detect `CurrentDescriptor = None` (uniquely identifies init) and call `renderInit` instead of `renderStep`.
     3. Label step 0 as "Initialization" in summary builder — `SummaryTeX.gllStepSection`: when `stepNum = 0`, use section title `"Initialization"` instead of `"Step 0"`.
     4. Fix `verifyGssDots` in `GssDotVisualizationTests.fs` — currently asserts `blueCount == 1` for all dots including init. After fix, first dot (init) must have `blueCount == 0`; remaining dots `blueCount == 1`. Each guarded by `NodeCount > 0`.
     5. Fix step-highlight tests in `GllRunnerTests.fs` — current tests check only hardcoded non-init steps (5, 12, 19). Add tests asserting step 0 has NO highlights. Update existing tests to iterate all step dirs: step ≥ 1 must have highlights; step 0 must NOT.

238. [done] Add tikz visualization for GSS, SPPF, RSM, input graph.

     1. Reuse existing functions for automata to tikz visualization.
     2. Use tikz visualization as default for GLL (steps and summary). Reuse existing CLI flag to switch to dot.
     3. Use tikz visualization as default for RNGLR (steps and summary). Reuse existing CLI flag to switch to dot.

239. [done] Improve GLL Tikz GSS visualization (preserve DOT viaualization "as is")

     1. Use R-based notation for ranges on edges. Like ranges in descriptor table
     2. Use rounded rectangle shape for vertices.

240. [done] Fix toCNF function.

     1. To CNF conversion must include gramar cleanup (as last step of transformations): non-generating and unreachable nonterminals must be removed. Note: order of removing is important. Wrong order leads to incorrect result. Add these two cleanup subcteps. Simple test is grammar S -> a; S -> S S; S -> S S S. Currently it contains N_2 -> a rule in CNF where N_2 is unreachable.
     2. Add tests. Add two functions: one checks that all nonterminals in grammar reachable from start nonterminal, one checks that each nonterminal in grammar can produce terminal or empty string. both applicable for grammar in BNF. Tests: for all grammars in language registry convert grammar to CNF and use cretaed functions to check that there are no non-generating and unreachable nonterminalsin result.

241. [done] Improve CYK and Valiant (+ modified) table rendering. Render each nonempty cell as a set of tuples of form (nonterm, split_point, prod_id).

242. [done] Improve BasicSPPF creation and visualization (rendering).

     1. SPPF must be built only for start nonterminal in respective cell (if string accepted). Not for all nonterminals from all cells.
     2. Do not mark edges with `derives` and numbers.
     3. Production node store not left and right positins, but split point. Render it with respective lable of form `split_point, prod_id`
     4. In rendering: for nontermonal node label use same form as in nonterminal node lable in rsm sppf rendering: `nonterm [from,to]`

243. [done] Add tikz rendering for BasicSPPF.

     1. Use ` \graph` with `layered layout`. (look at automata tikz layout for example)
     2. Use tikz as default for CYK and Valiant (+modified) BasicSPPF visualization. Use existing CLI arg to switch to dot.

244. Inprove GLL rendering

     1. Use adjustbox from package adjustbox to scale tikz figures. `\begin{adjustbox}{max width=\textwidth}\begin{tikzpicture} ... \end{tikzpicture}\end{adjustbox}`
     2. `v0 [label={[font=\tiny, anchor=north west, xshift=-1.01mm, yshift=1.01mm]north west:1} ,as={(0,0)}];`

245. [done] Fix all Language Registry Violations from @tasks/code_review.md Section 7.

     1. Add missing grammars to LanguageRegistry: ClassicArithExpr (with +/\*/(/) operators as terminals), ANB-like grammars, chain/cascade grammars, and any other edge-case grammars used in test files but not yet in the registry.
     2. Fix 7.1 (RV1-RV4): Replace hardcoded Grammar.parseGrammar calls in non-printer test files (GrammarTests.fs, FirstFollowTests.fs, StressTests.fs, PathIndexTeXTests.fs) with LanguageRegistry references.
     3. Fix 7.2 (RV5-RV16): Replace hardcoded Grammar.parseGrammar calls in printer/golden test files (TexCompilationTests.fs, AutomatonVisualizationTests.fs, LLVisualizerTests.fs, LRVisualizerTests.fs, LRStepsGoldenTests.fs, LRTableTeXGoldenTests.fs, GrammarTeXGoldenTests.fs, LLStepsGoldenTests.fs, LLTableTeXGoldenTests.fs, ValiantTraceGoldenTests.fs, CykSummaryGoldenTests.fs, DerivationTreeVisualizationTests.fs) with LanguageRegistry references.
     4. Fix 7.3-7.4 (RV17-RV23): Replace hardcoded RsmBuilder.buildRSMFromText calls in test files with LanguageRegistry references. For files testing the builder itself (EbnfParserTests.fs, RSMTests.fs, RsmToGrammarTests.fs), add representative grammars to the registry and reference them.
     5. Fix 7.5 (RV24-RV26): Remove TestHelpers.grammarToRsm and TestHelpers.grammarToEbnfText. Replace all ~29 call sites of grammarToRsm with direct g.Rsm access. Keep LanguageRegistry.grammarToEbnfText (private) and buildRegexRsm.
     6. Fix 7.6 (RV27): Replace data/\*.bnf file references in CLI tests with temp files created from LanguageRegistry Text entries. Remove the bnf files or ensure they're generated on-demand.
     7. Run all tests — all must pass with zero regressions.

246. [done] Refactor LanguageRegistry: semantic naming, distribute MiscTestGrammars, improve isEbnfText.

     1. Rename all grammars from numbered (`grammar1`, `grammar3`, ...) to semantic names (`dyckAmbiguousEps`, `aPlusRightRecursive`, ...).
     2. Distribute MiscTestGrammars entries to existing languages (SingleA, SingleAB, AStar, Dyck1, ArithExpr) or promote to new full-featured languages (DoubleA, AOrEps, ABPlus, FourTerm, MixedPairs, AX, SingleB).
     3. Keep 5 pure infrastructure grammars (parseGrammar/tocnf tests) in a dedicated `TestInfraGrammars` language.
     4. Improve `isEbnfText`: use the existing EBNF parser's Regexp AST — parse text, walk each rule's AST; if ANY rule contains `RAlt` or `RStar` → true EBNF; if ALL rules are pure concatenation (RTerm, RNonterm, RSeq, REps only) → plain CFG. Ban `+`, `*`, `?`, `|`, `(`, `)` as terminal names.
     5. Update all test call sites for renamed grammar references.
     6. Run all tests — zero regressions.

247. [done] Improve CYK and Valiant tests

     1. For CYK vs Valiant and vs Modified Valiant use the same scheme as for GLL vs RNGLR: for all grammars in language registry for arbitrary string these algorithms myst acceps and rejects simultatiously.
     2. Create common functions to collect all grammars in registry. Use it in all respective points.
     3. Create common helpers if necessary.
     4. Use the same sceme as for GLL and RNGLR: create common function that checks set of invariant
        1. If string accepted that leafs of tree extracted from the basic sppf is an input string
        2. Final table for CYK, Valinat and modified Valiant exactly the same for all accepted and rejected strings.
        3. In basic SPPF for all three algorithms any Production node has one or two childs. Moreover, for any production node with `ruleIndex` = k, length of RHS of production k is exactly a number of childs.
        4. For the same string SPPF-s from these three algorithms structurally equivalent.

248. [done] Fix Valiant SPPF SplitPoint to match CYK absolute-position convention.

     **Analysis:** CYK stores `SplitPoint` as the **absolute** position of the split within the original input string. For cell `[i,j]` representing substring from position `i` to `j`, the split point `k` is a global index where `i ≤ k < j`: left child covers `[i,k]`, right child covers `[k+1,j]` (`Cyk.fs:232,249`).

     Valiant's `mxmSetSppf` (`Valiant.fs:641`) stores `SplitPoint = k` where `k` is the **local** inner-dimension index from `Matrix.mxmi` (0..Size-1 within the submatrix being multiplied). When `writeSliceUnionSppf` (`Valiant.fs:660-663`) writes this result to the global table at global position `(mTarget.Row - mTarget.Size + 1 + i, mTarget.Col + j)`, the SplitPoint stays as local `k`.

     **Why this is wrong:** For a nested submatrix with column offset `m1.Col > 0` (where `m1` is the left submatrix in `doMultiplicationsSppf`), the local `k=0` corresponds to global column `m1.Col + 0`, NOT global column `0`. The SplitPoint stored at the global cell should be `m1.Col + k` to match CYK's absolute convention.

     **Evidence from test data:**

     - n=2 input "ab" at output cell (0,1): CYK SplitPoint=0, old Valiant SplitPoint=0.
       The producing submatrix task has m1.Col=0 at top level → local k=0 = global split position 0. Correct by accident.
     - n=4 input "baba" at output cell (1,2) = substring "ab" at positions 1-2:
       CYK SplitPoint=1 (split between position 1 and 2).
       Valiant SplitPoint=0 (local k=0 from nested submatrix at m1.Col=1, but stored without offset).
       Correct value should be m1.Col + k = 1 + 0 = 1.

     **Downstream impact:** `BasicSppf.fromParsingTable` (`BasicSppf.fs:119-136`) uses `entry.SplitPoint` to split the cell range `[i,j]` into child sub-ranges: `childNtInCell i entry.SplitPoint leftNt` and `childNtInCell (entry.SplitPoint + 1) j rightNt`. With wrong SplitPoints, these lookups target wrong table cells, find no matching nonterminals, and leave Production nodes with missing children.

     **Verification:** After fix, Valiant SPPF tables must be byte-identical to CYK SPPF tables for ALL inputs: add respective invariant checking into CYK vs Valiant tests. For all acceppted and rejected strings tables must be identical. All 492 existing tests must pass. Golden files may need regeneration if any SPPF table rendering tests are affected.

249. [done] Fix BasicSppf.fromParsingTable Production node reuse.

     **Analysis:** `fromParsingTable` (`BasicSppf.fs:103-104`) uses `getOrCreate` to allocate a Production node:

     ```
     let prodNode = getOrCreate (BasicSppfNodeInfo.Production(entry.ProdIdx, entry.SplitPoint))
     ```

     The `getOrCreate` function (`BasicSppf.fs:71-78`) deduplicates by node info value, reusing existing vertices when available. For Production nodes keyed by `(ProdIdx, SplitPoint)`, when the same pair appears across **different parent Nonterminal cells** (`processCell` called with different `(i,j)` ranges), the SAME Production vertex gets edges accumulated from multiple parents.

     **Example:** For grammar with terminal rule `N -> a` (ProdIdx=0), this rule appears at positions 0 and 1 in a 2-char input "aa". Both produce entries with `ProdIdx=0` but different SplitPoints (0 and 1). At position 0: `Production(0, 0)` is created. At position 1: `Production(0, 1)` is created. These have different SplitPoints so `getOrCreate` creates distinct nodes. Now, when binary rules combine these, `Production(0, 0)` might be referenced from different parent cells — sharing accumulates spurious children.

     **Fix:** Replace `getOrCreate(Production(...))` with direct vertex allocation per occurrence:

     Nonterminal and Terminal nodes keep their `getOrCreate` deduplication — sharing is correct for these (same Nonterminal at same position IS the same node). Production nodes are context-dependent (their parent Nonterminal determines which child cells are visited).

     **Verification:** After fix + task 248, `extractDerivationTree` must produce correct tree leaves (tree yield = input string), and `validateProductionChildren` must pass for all three algorithms (CYK, Valiant, Modified Valiant). The `BasicSppfTests` must all pass.

250. [done] Complete invariant checks and structural SPPF equivalence for CYK/Valiant/Modified Valiant.

     **Prerequisites:** Tasks 248 and 249.

     **Context:** Task 247 partially implemented the cross-parser tests but omitted several invariant checks due to the issues fixed in tasks 248-249. After those fixes, all three algorithms should produce identical SPPF tables and structurally equivalent SPPF graphs.

     1. Create `LanguageRegistry.allCompatibleGrammars : AnnotatedGrammar list` — collects all grammars from all languages where `not g.Properties.IsRsmDerived && not g.Properties.DoesNotCoverFullLanguage`. Use this in `checkLanguages` helper in `CrossParserEquivalenceTests.CykVsValiantVsModifiedValiant`.

     2. Restore full invariant checks in `checkCykValiantEquivalence`:

        1. **Tree leaves match input:** For accepted strings, build BasicSPPF from each of the three SPPF tables via `fromParsingTable`, extract a derivation tree via `extractDerivationTree`, verify `DerivationTree.leaves` equals the input string (as string list). Verify for all three algorithms separately — each must produce a correct tree.
        2. **SPPF tables byte-identical (including SplitPoint):** Replace the current nonterminal-only comparison with full cell-by-cell equality: `cykSppfTable.[i,j] = valSppfTable.[i,j] = modSppfTable.[i,j]`. After task 248, this MUST hold — SplitPoint values are now identical.
        3. **Production children count = RHS length:** For each of the three SPPFs, call `BasicSppf.validateProductionChildren sppf cnf`. Verifies that every Production node has 1-2 children, and child count equals the RHS length of the referenced production rule (`cnf.Rules.[ruleIndex]`).
        4. **SPPF graphs structurally equivalent:** Build BasicSPPF from each table. Verify graph identical by synchronious traversing from root.
        5. **Guard for empty/rejected input:** Skip SPPF checks when `not accepted || n == 0`.

     3. Update the `checkLanguages` helper in `CrossParserEquivalenceTests.CykVsValiantVsModifiedValiant` to use `LanguageRegistry.allCompatibleGrammars` instead of inline `lang.Grammars |> List.filter`.

     4. Run all tests — zero regressions. All 7 cross-parser test groups must pass with the restored invariants.

     5. If any invariant fails for a specific algorithm, that algorithm has a residual bug — fix it before marking the task done.

251. [done] Add more SPPF invariant checking to `checkCykValiantEquivalence`

     1. There is only one vertex without incoming edges in SPPF. This vertex is labelled with start nonterminal, leftPos in 0, and right pos is input length
     2. For any Production vertex with two child lCH and rCH, splitPoint of the vertex is equal to lCH.RightPos, and splitPoint of the vertex is equal to rCH.LeftPos

252. [done] Improve Valinat (and modified Valiant) table rendering.

     1. Use \\rectanglecolor instead of \\Block to higlight submatrices
     2. Wrap matrix with `\begin{adjustbox}{max width=\textwidth}...\end{adjustbox}`
     3. Use `$` instead of `\[` and `\]` inside adjustbox
     4. Complete example:

     ```
      \begin{center}
        \begin{adjustbox}{max width=\textwidth}
        $
        \begin{pNiceMatrix}[color-inside,first-row,code-for-first-row = \arabic{jCol},first-col,code-for-first-col = \arabic{iRow}]
        \CodeBefore
        \rectanglecolor{blue!20}{1-3}{2-4}
        \rectanglecolor{red!10}{1-5}{2-6}
        \rectanglecolor{green!20}{3-5}{4-6}
        \Body
        &  &  &  &  &  &  &  &  \\
        & \cdot & \{(S, 0, 1)\} & \{(N_1, 0, 0), (S, 0, 2)\} & \{(N_1, 0, 0), (N_1, 1, 0), (S, 0, 2), (S, 0, 3), (S, 1, 2)\} & {\cellcolor{yellow}{\{(N_1, 1, 0), (N_1, 2, 0), (S, 1, 2), (S, 1, 3), (S, 2, 2)\}}} & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \{(S, 1, 1)\} & \{(N_1, 1, 0), (S, 1, 2)\} & \cellcolor{yellow}{\{(N_1, 1, 0), (N_1, 2, 0), (S, 1, 2), (S, 1, 3), (S, 2, 2)\}} & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \cdot & \{(S, 2, 1)\} & {\{(N_1, 2, 0), (S, 2, 2)\}} & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \cdot & \cdot & \{(S, 3, 1)\} & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot \\
        & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot & \cdot \\
        \end{pNiceMatrix}
        $
        \end{adjustbox}
      \end{center}
     ```

253. [done] Improve CYK and Valiant (+ modified) and its visualization

     1. Unify with appf and without sppf versions: table always with data to built SPPF. No separsted version that does not compute data for SPPF.
     2. Add visualization flag `-no-sppf-table` that for CYK, Valinat, Modified Valinat render tables without data for SPPF: each cell is a set of nonterminals, not set of triples. So, data for SPPF must be computed always, but render only if required.
     3. Design tests carefully. Add necessary tests. Do not miss tests thet checks property wothout SPPF. Migrate and itegrate them.

254. [done] Render grammars with production numbers. Use alignat\* environment. Use double-& between second and third columns. Add space after number. Example:

     ```
     \begin{alignat*}{3}
     1) \ & N_2 &&\rightarrow \varepsilon  \\
     2) \ & N_2 &&\rightarrow N_3\ N_1     \\
     3) \ & N_2 &&\rightarrow S\ S         \\
     4) \ & N_3 &&\rightarrow a            \\
     5) \ & N_4 &&\rightarrow b            \\
     6) \ & N_1 &&\rightarrow S\ N_4       \\
     7) \ & N_1 &&\rightarrow b            \\
     8) \ & S &&\rightarrow N_3\ N_1       \\
     9) \ & S &&\rightarrow S\ S           \\
     \end{alignat*}
     ```

255. [done] Fix productions numbering for CYK and Valiant. Numbers of productions in CNF grammar rendering must by synchronized with productions numbers used in SPPF and in respective tuples in matrix cells. For now we have a problem. For example, for grammar S -> a | S S | S S S, CNF is S -> a; S -> S S ; S -> S -> N1 ; N1 -> S S . Last production has number 4. But in cells we see (N1,0,0) that means we use production with number 0. The same in SPPF. I propose to use 1-based numbering as in CNF rendering. To prevent such problens we can create number-to-production map once and use it wherever it necessary.

256. [done] Use math mode for node lables in SPPF for CYK and Valiant. For tikz rendering only. Not for DOT.

     1. Use math mode for nonterminal names: `$N_1$`
     2. Use math mode for terminals: `$a_{2,3}$`

257. [done] Fix modified Valiant visualization: align grid to power of two and restructure trace steps by layer.

     1. Modified Valiant trace must render on a full `tableSize × tableSize` grid (`tableSize = nextPowerOfTwo(n+1)`), exactly like classical Valiant. Replace `snapshot table n` with `copyFullTable table tableSize` in the trace path only; final computed tables (`parseModifiedWithSppfInfo/Table`) stay `snapshot table n` and must remain byte-identical to classical Valiant and CYK.
     2. Trace steps must be: one initialization step showing 1×1 diagonal blocks (terminal cells `(i,i+1)` for `i < n`), then one step per layer with 2×2, 4×4, … blocks — no repeated block size.
     3. Layer blocks highlighted with a single light-red color (reuse `Matrix.CurrentStepSubmatrix` → red!10), not the multi-color `Submatrix idx` palette.
     4. Changed cells per layer highlighted in yellow (reuse `Matrix.CurrentCell`), same as CYK. Populate `changedCells` via a full-table diff of the layer's before/after states.
     5. Remove the `-1` column shift in `sppfModifiedStepToTeX`/`sppfModifiedStepToTeXAsNt` (blocks and highlights) so submatrices map to their true global cells on the full grid.
     6. Regenerate the modified Valiant trace golden file and add regression tests: power-of-two alignment, one-step-per-layer with correct layer sizes, and non-empty changed cells.

258. [done] For CYK and Valiant (and modified) add early exit condition for main loop. The condition is matrix doex not modified over iteration. It is a case for non-derivable strings. In these cases steps (for vizualization) mast be equals to iterations that modifie matrix. For example, for grammar S -> eps | S S | a S b and input 'a b a a' only firs iteration is progressive, so early exit triggered and only initial matrix and result of firs iteration must be visualized. The condition for eraly exit may be based on tracking of modified cells: if no modified cells, we can exit main loop.
     **[USER GUIDANCE]**: Task is incorrect — the condition "no modified cells over an iteration -> exit" is unsound for CYK: a later span length can still be filled from two shorter spans (counterexample: grammar S->XY/X->AB/Y->CD/A->a/B->b/C->c/D->d, input abcd — len=3 fills nothing but len=4 fills the accepting cell). User confirmed: do not implement.

259. [done] For GLL visualization, for each step track GSS vertices where stored pops handling triggers. Highlight these vertices with new color. Add this color to summary legend together with other colors.

260. [done] Make RNGLR more canonical: implement canonical level-based logic. Without additional descriptors. Each step is a level (input position) handling: shift and then do all possible reductions (continue reductions while possible). Then move to next level. Rework visualization. Create new template for step layout: two colums only (no descriptorc queue), no new descriptors in last columt.

261. [done] For RNGLR visualization, for each step track GSS vertices where passing reductions handling triggers. Highlight these vertices with new color. Add this color to summary legend together with other colors. Share color with one used for GLL stored pops.

262. [done] Add Tikz rendering for LL and LR steps visualization (tree_and_stack). Use graphdrawing layered layout with `{ [same layer] ... }` to preserve the same-level stack frontier layout (equivalent of DOT `{rank=same}`); dashed chain edges stay drawn but non-constraining. TikZ by default, `--use-dot` switches per-step output to DOT (LL runner gains the flag). Update summary generation for both modes. Add golden tests, TeX compilation tests, and a same-layer coordinate test comparing against the DOT layout baseline.

263. [done] When I compile summary tex file for LL (generated with `dotnet src/FLPQ.Cli/bin/Release/net10.0/FLPQ.Cli.dll -s -a LL -o viz_output/LL/ -i data/example_input_an_bn.txt -g data/example_grammar.bnf`) I get number of errors. Fix it. Check that other algorithms visualization not broken. Add tests to prevent such problem in the future.

264. [done] Use `\begin{adjustbox}{max width=\textwidth}...\end{adjustbox}` instead of `\resizebox{0.98\textwidth}{!}{%%` in LL and LR steps visualization to scale tikz figure.

265. [done] Improve TikZ visualization of RSM in GLL and RNGLR. Layout components (blocks) top-to-bottom, preserving left-to-right layout inside each component.

     1. Rework `RsmTikz.extendedRsmToTikz`: use pgf-gd connected-component packing (`components go down left aligned`) so blocks stack vertically; intra-block layout unchanged (layered layout, grow'=right). Start block S' on top, remaining blocks in global-state appearance order. Plain nonterminal label to the left of each block's start state. No inter-block edges exist in the RSM data structure (verified) — if any are detected during implementation, report it.
     2. Add full extended RSM TikZ figure at head of summary for RNGLR and for GLL TikZ mode (currently missing there). No RNGLR step layout changes.
     3. Tests: compilation tests (with/without highlighted state), structural assertions, RNGLR runner file-existence test, summary compilation tests covering the new head section.

266. [done] Fix task logging: restore lost task entries and harden the workflow so that every task is reliably logged in `tasks/tasks.md` with no losses.

     1. Restore tasks 1-100 to `tasks/tasks1.md` verbatim from `ac729de~1:tasks/tasks.md` (the version before `ac729de` removed them and replaced them with the header note). Add a one-line provenance note at the top of the file.
     2. Restore entries 263 and 264 in `tasks/tasks.md` verbatim, tagged `[done]`, from the "Task description (verbatim)" sections of `b23a09a:tasks/detailed_plan.md` and `58c4f40:tasks/detailed_plan.md`. Insert between 262 and 265.
     3. Harden workflow instructions so task entries cannot be lost again: (a) AGENTS.md working loop — new gate: before creating a feature branch, verify the chosen task has an entry in `tasks/tasks.md`; if not, add it verbatim and commit on `dev` (`docs(tasks): add task NNN`); step 8 — before marking `[done]`, verify the entry exists, restore verbatim from `detailed_plan.md` if missing. (b) git-workflow skill — pre-merge check: grep the task number in `tasks/tasks.md`; if missing, STOP and restore verbatim from `detailed_plan.md`; clarify that task-log commits happen on `dev`, never on a feature branch. (c) planning skill — detailed plan MUST begin with a `## Task description (verbatim)` section quoting the `tasks.md` entry; verify/create the entry before decomposition.
     4. Delete untracked leftovers: `.opencode/plans/ll-lr-tikz-stack-viz.md`, `data/example_input_a_a_a_a.txt`.

267. [done] Fix code-review skill ordering: review runs before the hard gate, not after. Update the skill's intro line and Prerequisites section, which currently require quality gates to pass before code review (contradicting the AGENTS.md working loop: step 6 review, then step 7 gate).

268. [done] Improve task log formatting and splitting. `tasks/tasks.md` must contain at most 100 tasks; older tasks are archived in `tasks/tasks<N>.md` files (tasks1.md = 1-100, tasks2.md = 101-200, ...); task numbering is continuous, no reset.

     1. Create helper `tools/split_tasks.py`. On every run it must: (a) parse entry heads in `tasks/tasks.md` (numbered lines, strictly increasing; lines inside fenced code blocks skipped); (b) repair head-line indentation — dedent heads not at column 0 to column 0, leading whitespace only, entry text never touched; (c) normalize with mdformat; (d) verify structure preservation and abort on violation — repair stage: changed lines differ only in leading whitespace and are exactly the repaired heads; format stage: markdown-it-py AST deep equality modulo style (token types/sequence, block text, ordered-list start numbers, fence bytes compared; bullet markers and emphasis delimiters excluded) — no renumbering, no list/block flattening, no new nesting or sections, no missing data; marker/emphasis/whitespace normalization allowed; split stage: moved blocks byte-identical to the formatted source; (e) if more than 100 entries, move the oldest 100 to the next `tasks/tasks<N>.md` with a one-line provenance header and update the archive-reference line in `tasks.md`. Output to `tmp/split-tasks.txt`, exit code authoritative, `--dry-run` supported.
     2. Run it on the current files: repair + format `tasks/tasks.md` and `tasks/tasks1.md` in place; create `tasks/tasks2.md` (101-200); leave 201+ in `tasks/tasks.md`. Verify all invariants and idempotency (second run makes no changes).
     3. Extend instructions and skills so task log files stay well-formatted continuously with mdformat — no tricks or workarounds: `tools/quality_check.py` format step gains `mdformat --check tasks/tasks*.md`; AGENTS.md working loop runs the script after every task-log change (add entry, `[done]`, USER GUIDANCE) and commits the result on dev; pre-merge and `[done]` existence checks search all `tasks/tasks*.md`; git-workflow never-checkout rule extended to all `tasks/tasks*.md`; planning/subtask-loop/user-guidance-transfer reference "the task-log file containing the entry"; document the tool in `tools/README.md` and `docs/developer/guides/tools.md`.

269. [done] Fix duplicated SPPF section in GLL/RNGLR summary and broken math rendering in TikZ SPPF/RSM figures. The GLL and RNGLR merged summaries contain SPPF twice — once in the header (DOT-compiled PDF via `rsmSppfPdfs`) and once at the end (`sppfSection`, TikZ in tikz mode). The trailing TikZ version renders node labels as literal TeX source (`S^\varepsilon @1`, `[s0,v0]\to[s4,v6]`) because `SppfTikz.toTikz` passes whole labels through `AutomatonTikz.escapeLatex`, which escapes the math commands.

     1. Remove the SPPF entry from the GLL/RNGLR summary header: drop `("SPPF", "dot_pdfs/sppf.pdf")` from the pdfs lists in `Summary.fs`. The trailing `sppfSection` becomes the single SPPF location (TikZ in tikz mode, DOT PDF fallback otherwise), consistent with CYK/Valiant summaries.
     2. Fix `SppfTikz.toTikz`: escape only terminal/nonterminal names via `AutomatonTikz.escapeLatex`; structural TeX stays intact and math commands are placed in math mode — epsilon node label `$S^{\varepsilon}$ @1`, range/intermediate labels `[s0,v0]$ \to $[s4,v6]`.
     3. Fix the same bug class in RSM/automaton TikZ: `RsmTikz` epsilon edge label becomes `$\varepsilon$` (not routed through escapeLatex; names still escaped); `AutomatonTikz.epsEdges` `\varepsilon` becomes `$\varepsilon$`.
     4. Tests: regression assertions that SPPF TikZ output contains `$\varepsilon$`/`$\to$` and no `\textbackslash`; RSM TikZ epsilon edge label is `$\varepsilon$`; existing TeX compilation tests (SPPF tikz, RSM tikz, GLL/RNGLR merged summaries) keep passing; regenerate GLL + RNGLR summaries and verify exactly one SPPF section.

270. [done] Improve RSM rendering. 1. remove 'call' word from nonterminal edges. Just terminal name. 2. Remove nonterminal name from state. Just number. Current state lable has form N_i. Transform it to just i. 3. Carefully check nodes styling. Now, in GLL steps visualization in case when final state is a part of current gss node, we miss doublecircle for this finak state. Light blue is correct, we use it for current node, but if this node is final, it must be double cyrcled, as regular funal state. Add tests: for all steps in GLL, all rendered to tikz RSMs must have exactly the same number of doubly cyrcled nodes. It must be equal to total number of final states in the input extended RSM.

271. [done] Improve GLL and GLR steps rendering. Wrap GSS and RSM (rsm for GLL only) in each step with `\begin{adjustbox}{max width=\textwidth} ... \end{adjustbox}`. Use existing wrapper function.

272. [done] For all tikz-based SPPF visualization in all algorithms (CYK, Valiant, GLL, RNGLR) replace resizebox with ajustbox. Use ajustbox with following parameters `\begin{adjustbox}{max width=\textwidth, max totalheight=\textheight} ... \end{ajustbox}`. Generalize existing ajustbox wrapper function to use it with and without max totalheight limit.

273. [done] For GLL and RNGLR Tikz-based SPPF rendering use down indices in the following nodes. For intermediate nodes use 'I\_{m,p}' instead of 'I(m,p)'. For ranges in itermediate and range nodes use '[s_i,v_j] \\to [s_k,v_l]' instead of '[si,vj] \\to [sk,vk]'

274. [done] Reorder the RNGLR level driver to canonical order: at each level (input position) first apply all possible reductions to fixpoint, then execute shifts from all vertices of the level in a single pass; remove the round loop and shifted-state tracking. Canonical references: Scott & Johnstone 2006, Algorithm 1e PARSE SYMBOL (ACTOR/REDUCER loop until A and R empty, then SHIFTER) and book sec:CFPQ_GLR (MakeReductions → Push → ApplyPassingReductions). Reverses the shift-then-reduce decision of tasks 225/260. Visualization keeps one step per level; output for `data/example_input_a_a_a.txt` + `data/example_grammar_a_a_a.bnf` must stay byte-identical; all existing RNGLR tests pass unchanged.

275. [done] Improve RNGLR GSS visualization: all GSS nodes at the same input position must be drawn on the same layer, preserving the current left-to-right global layout (input position 0 stays rightmost; higher positions to its left). The position-layering is opt-in via a new trailing `positionOf: (int -> int) option` parameter on `GssDot.toDotFromSets` and `GssTikz.toTikzFromSets`; RNGLR passes `Some (fun idx -> snd (vertexInfo idx))`, GLL passes `None` (unchanged). DOT: when `positionOf` is `Some`, emit one `{rank=same; ...}` subgraph per input position grouping that position's vertices. TikZ: when `positionOf` is `Some`, emit one `{ [same layer] ... }` collection per input position AND switch the graph grow direction from `grow'=right` to `grow=left`, because pgf's same-layer cluster chaining reverses the orientation under the default grow direction (verified: `grow=left` keeps position 0 rightmost, `grow'=right` flips it). Refactor `AutomatonTikz.layeredGraphOptions` to take a grow-direction argument with named defaults so the GSS renderer can select `grow=left` without duplicating option text. Tests: DOT coordinate test asserting same-position nodes share an x-coordinate and position 0 is rightmost; TikZ structural test asserting correct same-layer groupings and `grow=left`; existing GLL/RNGLR tests pass unchanged.

276. [done] Raise line-coverage thresholds in the hard gate and add tests to reach them. 1. In `tools/hard_gate.py` raise `PER_PROJECT_THRESHOLD` from 85.0 to 90.0 and `TOTAL_THRESHOLD` from 90.0 to 95.0 (line coverage); update the script docstring, `docs/developer/guides/tools.md` threshold references and example output, and the quality-gates skill where numbers are mentioned. 2. Add tests until every source project reaches >= 90% line coverage and the total reaches >= 95%. Baseline (full-solution merged coverage): FLPQ.Languages 88.65% (6810/7682), FLPQ.Cli 95.30%, FLPQ.Printers 93.29%, FLPQ.LinearAlgebra 100%, FLPQ.GraphAnalysis 100%, FLPQ.RPQ 93.01%, TOTAL 91.04% (12636/13880). Biggest gaps: Sppf.fs, BasicSppf.fs, PathIndex.fs, EbnfParser.fs, LRParser.fs in FLPQ.Languages; MatrixTeX.fs, SummaryTeX.fs, ExternalTools.fs, RnglrTableTeX.fs in FLPQ.Printers.

     [done] 277. Add branch coverage to the hard gate and add tests to reach it. 1. Extend `tools/hard_gate.py` to parse branch coverage from `tmp/coverage.cobertura` — sum covered/total from `condition-coverage="(n/m)"` on lines within classes of source packages (verified to exactly match coverlet's own package-level `branch-rate`) — and gate it: per project >= 90%, total >= 95%; report per-project branch % alongside line % in the coverage step. Update `docs/developer/guides/tools.md` and the quality-gates skill. 2. Add tests until every source project reaches >= 90% branch coverage and the total reaches >= 95%. Baseline: FLPQ.Languages 90.35% (1872/2072), FLPQ.Cli 87.18% (136/156), FLPQ.Printers 94.16%, FLPQ.LinearAlgebra 100%, FLPQ.GraphAnalysis 100%, FLPQ.RPQ 84.31% (172/204), TOTAL 91.36% (3446/3772). Biggest gaps: BasicSppf.fs (86 uncovered branches), Sppf.fs (44), Automaton.fs (26) in FLPQ.Languages; ValiantTeX.fs (24) in FLPQ.Printers; Helpers.fs (12) in FLPQ.Cli; GraphReader.fs (14) + BelyaninRPQ.fs (12) in FLPQ.RPQ.

---

[done] 278. Improve RNGLR rendering. For each big step (reductions to fixpoint + one shift) visualize all substeps separately. Substep is an exactly one action: exactly one reduce or one shift. Improve collected data structure with respect to new visualization requirements. Teplate for visualization is exactly the same as existing templste for step. One change: for LR table highlight exactly one cell: cell with action that visualized on this step.
**[USER GUIDANCE]**: A reduce substep highlights two cells — the Goto cell (predecessor state, nonterminal) consulted for that edge and the Action cell (triggering state, $) containing the reduce action; still one reduction per substep (one new GSS edge). Output stays flat: each substep is its own step_N directory and summary section (initial state remains step 0); levels with no actions produce nothing.

279. [done] Improve RNGLR summary. First, remove dot visualization of RSM from tikz-based summary. Check that dot-base summary contains dot-visualized extended RSM, beceuse currently I see not-extended DOT rsm in tikz-based summary, while included tikz visualization is correct: it is for extended RSM. Second: add LR automata visualization at the start of summary. Use ajustbox for LR automata (look at SPPF for example). Order: extended RSM, LR automata, LR table.

     [done] 280. Add Arroyuelo RPQ algorithm visualization to the CLI. Same structure as other algorithms: input, results, steps, and summary. Input is a regexp and a graph. Visualize both the regexp and the respective DFA. TikZ by default, DOT as fallback for all figures. Same visualization of matrices: use matrix with set of symbols (sets of vertex paths), not boolean matrices — look at Valiant visualization for example. For Arroyuelo one step is one node of the regexp tree processing; highlight the current node of the tree. Visualize the operation over matrices (for transitive closure no inner steps: just TC(M)=M' where M and M' are visualized). Highlight computed parts in the input graph: each step collects information necessary to highlight partial paths, not just start and final vertex — use a custom semiring over sets of vertex sequences to track intermediate points (look at Valiant custom semirings for inspiration). Use adjustbox for TikZ. This task also creates the shared infrastructure for task 281: CLI regexp/graph arguments, Regexp.toDfa helper, path semiring module, example regexp and graph data files.

     [done] 281. Add Belyanin RPQ algorithm visualization to the CLI. Same structure as other algorithms: input, results, steps, and summary. Input is a regexp and a graph. Visualize both the regexp and the respective DFA. TikZ by default, DOT as fallback for all figures. Same visualization of matrices: use matrix with set of symbols (sets of vertex paths), not boolean matrices — look at Valiant visualization for example. For Belyanin one step is one iteration of the main loop (one step of BFS-like traversal). Visualize matrix operations (multiplications for simultaneous frontier propagation) and partial results in graph and automaton: current frontier. Highlight computed parts in the input graph: each step collects information necessary to highlight partial paths, not just start and final vertex — use a custom semiring over sets of vertex sequences to track intermediate points (look at Valiant custom semirings for inspiration; book 02_BFS.tex path semiring with I_simple). Use RNGLR step layout for inspiration, tuned for the RPQ algorithm. Use adjustbox for TikZ. Reuse shared infrastructure from task 280: CLI regexp/graph arguments, Regexp.toDfa, path semiring module, example data files.
