# Detailed Plan: Task 293 — CFPQ project with Azimov matrix-based algorithm

## Task description (verbatim)

293. Add project for CFPQ, similarly to RPQ. First algorithm is an Azimov matrix-based algirithm. Generic semiring-parametrized core and three semirings (and respective enty-points): reachability boolean semiring. Single-path semiring, all-paths semiring. Categiry of tests. 1. Property with all-pairs RPQ. As far as regualr languages are subclacc of context-free, RPQ and CFPQ must return equals result on arbitrary graph and language. 2. Property with paring algorithm (eg Valiant). As far as linera input is a partial case of graph and Valiant table contains results for all substrings, results of Valiant and Azimov must be equal for all strings and grammars. 3. Use small graphs from https://test.pypi.org/project/flpq-data/ It cantains reference reachable pairs for CFPQ problem. Do not inline data into F# code. Use flpq-data package as a source of data and reference values. Integrate this packege into project and CI.
     **[USER GUIDANCE]**: One common computational core (LAGraph_CFPQ_core style: one matrix per nonterminal, T[A][i,j] = semiring element iff a path i->j is derivable from A; init from terminal/epsilon rules, fixed point over binary rules via indexed matrix product) with three distinct semirings: reachability (bool cells), singlePath (element = PathIndex {middle, height}; add = min derivation-tree height, mult(k,x,y) = {middle=k, height=max+1}, init on edge = {middle=i, height=1}; answer extracted by following middle pointers — cf. LAGraph PR #8), allPaths (element = set of split vertices with a base-case sentinel; add = union, mult(k,x,y) = {k}; the result matrix contains information about all paths, not all walks or trails — enumeration yields exactly all simple paths — cf. LAGraph PR #9). CLI: single `-a AzimovCFPQ` plus `--semantics reachability|singlePath|allPaths` (the existing RPQ semantics flag becomes a shared enum; allPaths is rejected for RPQ algorithms). Reference data: the 5 smallest flpq-data graphs with known reference counts (generations, travel, skos, sampleproject, univ; at most 179 nodes), materialized by a Python tool from the pinned flpq-data package (test.pypi.org) into a gitignored data/cfpq/ directory and integrated into CI. Full `-s` summary support with per-iteration step artifacts (TablePerStep).

## Context (verified facts)

- Book: `tex/part_03_GraphAnalysis/chapter_12_CFPQ/02_MatrixBased.tex` (`sec:CFPQ_MatrixBased`,
  listing `alg:graphParse`). Algorithm: init T[i,j] from edges (A→x rules) and ε-rules on the
  diagonal; then `while T changes: T ← T ∪ (T×T)` where `M3[i,j] = ⋃_k M1[i,k] * M2[k,j]` and
  `S1*S2 = {N3 | N3→N1N2 ∈ P}`. Worked example: 4-vertex graph, grammar
  S→AB|AS₁, A→a, B→b, S₁→SB; fixed point at k=13 (T₁₃=T₁₂), one new nonterminal per iteration.
- The book's single set-valued matrix T is stored here as **one matrix per nonterminal**
  (LAGraph_CFPQ_core style): `T[A][i,j] = semiring element ⟺ A ∈ T_book[i,j]`. Equivalent;
  documented in code comments.
- **Batch iteration semantics**: each iteration computes all binary-rule products from a
  snapshot of the current matrices, then add-accumulates. This is exactly the book's
  `T ← T ∪ (T×T)` and reproduces the worked example's T₀…T₁₃ sequence (13 loop applications,
  12 productive) — in-place (Gauss-Seidel) accumulation would converge faster and break the
  golden trace. Same least fixed point either way; batch is chosen for book traceability.
- LAGraph PR #8 (single path): `PathIndex{middle,height}`; bottom {0,0}; add = min height
  (tie: smaller middle); mult(k,x,y) = {middle=k, height=max+1}; init on edge = {middle=i,
  height=1}. Extraction: height==1 → ε if i==j else the terminal edge (i,t,j); else split at
  Middle with height-consistency check `h == max(hB,hC)+1`, recurse both sides. The extracted
  walk may repeat vertices on cyclic graphs (min-height derivation, not necessarily simple).
- LAGraph PR #9 (all paths): element = sorted dedup array of split vertices; sentinel
  GrB_INDEX_MAX marks the base case (direct terminal/ε derivation); add = ordered union
  (sentinel may coexist with splits); mult(k,x,y) = {k} (ignores operands); init = {sentinel}.
  At the fixed point a cell holds exactly the set of valid split vertices (+ sentinel iff a
  direct derivation exists). Enumeration with visited-vertex filtering yields **exactly all
  simple paths** (not walks or trails) — per user guidance.
- flpq-data v6.0.0.dev15 (test.pypi.org; Python >=3.11,\<3.14): `graph_dir(name)` downloads +
  sha256-verifies + unpacks `<name>.tar.gz` → `graph/*.mtx` (per-label Boolean MatrixMarket,
  0-based: `%%MatrixMarket matrix coordinate pattern general`, optional `%%GraphBLAS type bool`, `rows cols nnz`, then `i j` pairs) and `queries/cfpq/<q>/<q>.cnf` + `results.mtx`.
  `.cnf` format: one production per line (tab-separated, LHS first; bare LHS = ε), blank lines
  ignored, last two lines `Count:` + start symbol; LHS symbols = nonterminals, others =
  terminals. Templates may contain indexed placeholders (`load_i`, `_r`) that MUST be
  materialized over the graph's edge labels — done in Python via
  `flpq_data.materialize_grammar`; F# parses plain grammars only and rejects `_i` symbols.
  Bundled `reachable_pairs.csv`: `graph,grammar,category,query_class,num_reachable_pairs`.
- Pinned graphs (5 smallest with known reference counts): generations (129 v), travel (131),
  skos (144), sampleproject (148), univ (179); 13 graph×grammar cases in the CSV.
- Reusable code: `Matrix.mxmi` (indexed product, op_mult:(i,k,j,a,b)->s),
  `Grammar.toCnf (freshNonterminal:int->'nt)`, `RsmBuilder.buildRSMWithStart (Map<Nonterminal<string>,Regexp<string,string>>)`, `RsmToGrammar.convert`,
  `Regexp.toDfa`, `ArroyueloRPQ.evaluateBooleanWithTrace` → (steps, full |V|×|V| bool matrix),
  `Valiant.parseWithTable (fresh) (g) (terminals) : ParsingTable<'nt> * bool` where
  `ParsingTable<'nt> = Matrix<Set<Nonterminal<'nt>>>`, `MatrixTeX.toTeXStyled` (highlight sets),
  `SummaryKind.TablePerStep` (generic: reads `step_N/table.tex`), `InputGraphTikz`/`InputGraphDot`
  (NFA graph figures), `IntersectionGenerators.NfaArb`, `RegexGenerators.RegexpPattern`,
  `TestHelpers.nfaFromEdges`, `LanguageRegistry` (grammars + GenString).
- CLI: `AlgorithmTypes.Algorithm` + `RpqSemantics { Reachability | SimplePath }` (Argu enum,
  case names are the CLI values); runners in `src/FLPQ.Cli/*Runner.fs`; dispatch in
  `Program.fs`; summary kind mapping in `Summary.fs`.

## Subtasks

### S1: FLPQ.CFPQ project skeleton + CfpqInput (MatrixMarket graph, .cnf grammar, results.mtx) [done — commit 26f2879]

**Code:** New `src/FLPQ.CFPQ/FLPQ.CFPQ.fsproj` (net10.0, GenerateDocumentationFile,
WarningsAsErrors FS3569; ProjectReferences FLPQ.LinearAlgebra + FLPQ.Languages; FSharpPlus
1.9.1 — mirror `src/FLPQ.RPQ/FLPQ.RPQ.fsproj`). New `src/FLPQ.CFPQ/CfpqInput.fs`:

- `type CfpqGraph<'t> when 't: comparison = { VertexCount: int; Labels: Map<'t, Matrix<bool>> }`
- `parseMatrixMarketFile (path: string) : Matrix<bool>` — Boolean coordinate
  pattern MatrixMarket; skip `%%` comment lines after the first header line; validate header
  kind + non-negative dims; 0-based `i j` pairs within bounds; returns the dense matrix with
  `true` in occupied cells.
- `parseGraphDir (dir: string) : CfpqGraph<string>` — every `*.mtx` in dir, filename without extension
  = label; all files must agree on (rows, cols); VertexCount = rows.
- `parseCnfFile (path: string) : Grammar<string, string>` — plain .cnf format (see Context);
  builds `Grammar` with LHS symbols as nonterminals; bare LHS line = ε rule; rejects indexed
  placeholders (`_i` suffix on any symbol) with a message pointing to
  `tools/flpq_data/export.py`; requires the `Count:` trailer + start symbol.
- `parseResultsFile (path: string) : Matrix<bool>` — via parseMatrixMarketFile.
  Add FLPQ.CFPQ to `FLPQ.slnx`.

**Tests:** New `tests/FLPQ.CFPQ.Tests/` project (mirror `tests/FLPQ.RPQ.Tests/FLPQ.RPQ.Tests.fsproj`;
references FLPQ.CFPQ, FLPQ.Languages, FLPQ.LinearAlgebra, FLPQ.TestUtilities) +
`CfpqInputTests.fs`: MatrixMarket parse (valid with comment lines, empty nnz, bad header,
out-of-bounds pair → error); parseGraphDir (multi-label ok, dimension mismatch → error);
parseCnfFile (book grammar, ε rule, missing trailer → error, `_i` placeholder → error);
parseResultsFile (known 12-pair file shape).

**Docs:** New `docs/developer/cfpq-input.md`; new hub `docs/developer/FLPQ.CFPQ.md` (kind: hub);
update `docs/main.md` project table.

**Spec:**

- MatrixMarket: first line must be `%%MatrixMarket matrix coordinate pattern general`;
  subsequent `%%` lines are comments; then `rows cols nnz`; then exactly nnz lines of two
  integers (0-based). Duplicate pairs collapse in the Set.
- .cnf: symbols split on tabs/whitespace; a symbol is a nonterminal iff it is an LHS of some
  production; start symbol must be a nonterminal.
- All parsers throw with actionable messages (file + line where possible).

### S2: CfpqSemiring (three semirings) + AzimovCfpq core [done — commit d68b5c3]

**Code:** New `src/FLPQ.CFPQ/CfpqSemiring.fs`:

- `type Semiring<'elem> = { Add: 'elem -> 'elem -> 'elem; Mult: int -> 'elem -> 'elem -> 'elem; InitPath: int -> int -> 'elem }` — cells are `'elem option` (None = no path); helpers
  `addCell : 'elem option -> 'elem option -> 'elem option`,
  `multCell : int -> 'elem option -> 'elem option -> 'elem option`.
- `ReachabilitySemiring : Semiring<bool>` (Add=||, Mult(k,x,y)=x&&y, InitPath=true).
- `type PathIndex = { Middle: int; Height: int }`;
  `SinglePathSemiring : Semiring<PathIndex>` per LAGraph PR #8 (see Context).
- `extractSinglePath (rules: WcnfRule<'t,'nt> list) (graph: CfpqGraph<'t>) (matrices: Map<Nonterminal<'nt>, Matrix<PathIndex option>>) (nt: Nonterminal<'nt>) (i: int) (j: int) : Trans<'t> list option` — edge sequence as `Trans` records (From, Label, To);
  None = no path; [] = ε; mirrors LAGraph PR #8 extraction (height==1 base cases, else split
  at Middle with height-consistency check).
- `type AllPathsElem = Base | Splits of NonEmptyList<int>` (sorted, deduped, never empty; Base = direct-derivation
  sentinel); `AllPathsSemiring : Semiring<AllPathsElem>` per LAGraph PR #9 (Add = ordered
  union with Base absorbing into Splits [k] as Splits [k] ∪ {sentinel-as-max}; Mult(k,_,_)=
  Splits [k]; InitPath=Base).
- `enumerateAllPaths (rules) (graph) (matrices) (nt) (i) (j) : Trans<'t> list list` — exactly
  all simple paths: Base → direct derivations (ε if i==j and A→ε; one edge per terminal rule
  A→t with matching edge); each split k with a binary rule A→BC → recurse B:(i,k) × C:(k,j)
  with visited-vertex filtering so concatenated walks are simple.
  New `src/FLPQ.CFPQ/AzimovCfpq.fs`:
- `type WcnfRule<'t,'nt> = Epsilon of Nonterminal<'nt> | Terminal of Nonterminal<'nt> * 't | Binary of Nonterminal<'nt> * Nonterminal<'nt> * Nonterminal<'nt>`;
  `classifyRules (g: Grammar<'t,'nt>) : WcnfRule<'t,'nt> list` — validates WCNF (rejects other
  RHS shapes).
- `type CfpqCell<'nt> = { Nt: Nonterminal<'nt>; Row: int; Col: int }`;
  `type CfpqStep<'nt,'elem> = { Iteration: int; ChangedCells: CfpqCell<'nt> list; Matrices: Map<Nonterminal<'nt>, Matrix<'elem option>> }`;
  `type CfpqResult<'nt,'elem> = { Matrices: Map<Nonterminal<'nt>, Matrix<'elem option>>; Steps: CfpqStep<'nt,'elem> list; Iterations: int }`.
- `evaluate (semiring: Semiring<'elem>) (freshNt: int -> 'nt) (grammar: Grammar<'t,'nt>) (graph: CfpqGraph<'t>) : CfpqResult<'nt,'elem>` — book algorithm (sec:CFPQ_MatrixBased): toCnf with the
  caller-supplied fresh-nonterminal function (the `Valiant.parse` pattern); classify; init from
  terminal rules (cells of the label's adjacency matrix get InitPath(i,j)) and ε rules
  (diagonal); batch fixed point: each iteration computes every binary-rule product
  `mxmi(addCell, multCell∘Mult, None, T[B], T[C])` from a snapshot, then add-accumulates into
  T[A] tracking changed cells; stops when an iteration changes nothing (Iterations counts that
  final no-change application; Steps holds productive iterations only).
- `answer (result: CfpqResult<'nt,bool>) (start: Nonterminal<'nt>) : Matrix<bool>`.

**Tests:** `CfpqSemiringTests.fs`: add/mult/init/bottom behavior for all three semirings;
singlePath extraction on the book example (S: 2→3 = [2-a-0, 0-b-3]; ε case; no path → None);
allPaths enumeration on a small cyclic graph — assert set equality with brute-force DFS
simple-path enumeration filtered by Valiant acceptance; WCNF classification (valid + invalid).

**Docs:** New `docs/developer/cfpq-semiring.md`, `docs/developer/azimov-cfpq.md`; update hub.

**Spec:**

- Semiring ops table (element / Add / Mult(k,x,y) / InitPath / answer):
  reachability: bool / || / x&&y / true / T[Start] bool matrix.
  singlePath: PathIndex / min height (tie middle) / {k, max+1} / {i,1} / extractSinglePath per pair.
  allPaths: AllPathsElem / ordered union / Splits[k] / Base / enumerateAllPaths per pair.
- Batch semantics is mandatory for book traceability (see Context).
- Complexity note in docs: O(|rules| · n³) per iteration, ≤ |N|·n² productive iterations.

### S3: Book-example golden test + core unit tests [done — commit d3d421d]

**Code:** None (tests only).

**Tests:** `AzimovCfpqBookExampleTests.fs` in FLPQ.CFPQ.Tests:

- Golden (reachability): book graph — a-edges 0→1, 1→2, 2→0; b-edges 0→3, 3→0 (the
  matrix-consistent reading of the worked example; the TikZ figure's 2↔3 b-edges are an
  inconsistency recorded in S8) + grammar S→AB|AS₁, A→a, B→b, S₁→SB. Assert: per-nonterminal
  final matrices equal the book's T₁₃ exactly (A: [0,1],[1,2],[2,0]; B: [0,3],[3,0]; S:
  [0,0],[0,3],[1,0],[1,3],[2,0],[2,3]; S₁: [0,0],[0,3],[1,0],[1,3],[2,0],[2,3]); Iterations = 13;
  Steps.Length = 12; per-step changed cells equal the book sequence \[(S,2,3),(S₁,2,0),(S,1,0),
  (S₁,1,3),(S,0,3),(S₁,0,0),(S,2,0),(S₁,2,3),(S,1,3),(S₁,1,0),(S,0,0),(S₁,0,3)\].
- singlePath on the same example: extraction returns Some for every true cell of T[Start];
  each extracted edge sequence is a valid walk in the graph whose label word Valiant-accepts
  from S; endpoints match.
- allPaths on the same example: for every pair, enumerated paths == brute-force simple paths
  (DFS) filtered by Valiant acceptance; includes both direct b-edge and two-edge derivations.

**Docs:** None new (covered by azimov-cfpq.md).

**Spec:** The book's T₀…T₁₃ values are the golden oracle; any mismatch is an algorithm bug,
not a test bug.

### S4: Property test — CFPQ reachability equals all-pairs RPQ [done — commit dd9a935]

**Code:** None (tests only).

**Tests:** `CfpqVsRpqPropertyTests.fs` in FLPQ.CFPQ.Tests:

- `[<Property>]`: random NFA (reuse `IntersectionGenerators.NfaArb` or the RegexAndGraph
  generator used by RPQ tests) + random regexp (`RegexGenerators.RegexPattern`): build a
  single-block RSM via `RsmBuilder.buildRSMWithStart (Map [(Nonterminal "S"), regexp]) (Nonterminal "S")` → `RsmToGrammar.convert` → `AzimovCfpq.evaluate ReachabilitySemiring`;
  assert `answer result (Nonterminal "S")` equals the full |V|×|V| matrix from
  `ArroyueloRPQ.evaluateBooleanWithTrace`.
- Comment: regular languages are a subclass of context-free languages; both algorithms
  implement complete reachability semantics, so results must coincide on arbitrary graph and
  language (task clause 1).

**Docs:** None new.

**Spec:** Exact matrix equality (all pairs), not just per-source rows.

### S5: Property test — Azimov equals Valiant's full parsing table [done — commit 6c0b5f4]

**Code:** None (tests only).

**Tests:** `CfpqVsValiantPropertyTests.fs` in FLPQ.CFPQ.Tests:

- `[<Property>]`: random string + grammar from `LanguageRegistry` (all grammars; toCnf
  precomputed once per grammar outside the property body): build the linear path graph
  (|s|+1 vertices, edge i→i+1 labeled s[i]) → `AzimovCfpq.evaluate ReachabilitySemiring` on
  the CNF grammar; `Valiant.parseWithTable` on the original grammar. Assert for every cell
  (i,j) with 0 ≤ i < j ≤ |s| and every original nonterminal A: A ∈ valiantTable[i,j] ⟺
  azimovT[A][i,j] = Some \_; and acceptance equality (valiantAccepted ⟺ T[Start][0,|s|]).
  Diagonal/ε cells: compare only if Valiant's table defines them (verify ParsingTable layout
  at execution; otherwise check nullable nonterminals separately).
- Comment: a linear input is a partial case of a graph and Valiant's table contains results
  for all substrings, so both must agree for all strings and grammars (task clause 2).

**Docs:** None new.

**Spec:** toCnf preserves each original nonterminal's language, which is why the CNF-based
Azimov table can be compared cell-wise with Valiant's table over the original grammar.

### S6: flpq-data export tool + CI integration + reference tests [done — commit ad40d4f]

**Code:**

- New `tools/flpq_data/export.py`: pins `EXPECTED_VERSION = "6.0.0.dev15"` (asserts
  `flpq_data.__version__`); pinned graphs = generations, travel, skos, sampleproject, univ;
  per graph: `flpq_data.graph_dir(name)` → copy `graph/*.mtx` to `data/cfpq/<name>/graph/`;
  per query dir: `materialize_grammar(<q>.cnf, graph)` → write the materialized plain .cnf to
  `data/cfpq/<name>/queries/<q>/<q>.cnf` + copy `results.mtx`; read bundled
  `reachable_pairs.csv` rows (query_class == cfpq) → `data/cfpq/<name>/reference.json`
  (grammar → num_reachable_pairs); write `data/cfpq/manifest.json`
  `{version, graphs: {name: {vertex_count, grammars: {q: count}}}}`; idempotent; prints a
  summary.
- `.gitignore`: add `data/cfpq/`.
- `.github/workflows/ci.yml`: Python 3.10 → 3.12 (flpq-data needs ≥3.11); new step after
  "Install markdown toolchain": `pip install --extra-index-url https://test.pypi.org/simple/ "flpq-data==6.0.0.dev15"` then `python3 tools/flpq_data/export.py` (before Test).
- `tools/README.md`: document the export tool + local usage.

**Tests:** `CfpqReferenceTests.fs` in FLPQ.CFPQ.Tests: read `data/cfpq/manifest.json`; for each
graph×grammar: parseGraphDir + parseCnfFile → Azimov reachability → assert T[Start] equals
`results.mtx` exactly AND the true-cell count equals num_reachable_pairs. Missing data/cfpq →
fail with an actionable message (run `python3 tools/flpq_data/export.py` after installing the
pinned package) — no Skip: CI always materializes the data, and the hard gate requires it
locally too.

**Docs:** `tools/README.md`; note the data dependency in `docs/main.md`.

**Spec:** No reference data is inlined into F# code; the pinned package is the single source
of data and reference values (task clause 3). Reference tests assert exact matrix equality,
not just counts.

**Finding (verified against all 13 cases):** the package's CFPQ queries run over the stored
edges **plus reverse edges with the `_r` label suffix** (`flpq_data.add_reverse_edges`; see
`cnf_template.py`: `available = stored | {f"{label}_r" for label in stored}`). Exporting only
the stored `graph/*.mtx` makes every reference comparison fail (e.g. generations/
nested_parentheses_type: 0 vs 12 pairs). The export tool therefore materializes the effective
graph via `add_reverse_edges` + `graph_to_mtx_dir`, so `data/cfpq/<g>/graph/` is self-contained
and `CfpqInput.parseGraphDir` needs no special handling.

### S7: CfpqMatrixTeX + CLI (AzimovCFPQ, --semantics) + summary [done — commit e9d56a6]

**Code:**

- New `src/FLPQ.Printers/CfpqMatrixTeX.fs`: per-nonterminal matrix rendering (nicematrix;
  cells ∅ / element text) with per-step new-cell highlighting — reuse
  `MatrixTeX.toTeXStyled` highlight-set support.
- `src/FLPQ.Cli/AlgorithmTypes.fs`: add `AzimovCFPQ` to `Algorithm`; rename `RpqSemantics` →
  `PathQuerySemantics` adding case `AllPaths` (Argu parses case names, so CLI values are
  unchanged for RPQ); update usage text.
- New runners in `src/FLPQ.Cli/`: `AzimovCfpqReachabilityRunner.fs`,
  `AzimovCfpqSinglePathRunner.fs`, `AzimovCfpqAllPathsRunner.fs` — each: parse .cnf + graph
  dir, run the core with its semiring, write root artifacts (grammar_original.tex,
  grammar_cnf.tex, graph figure via NFA reuse of InputGraphTikz/InputGraphDot, result.tex) +
  `step_N/table.tex` per productive iteration. result.tex: reachability = T[Start] boolean
  matrix + reachable pairs; singlePath = one extracted edge sequence per reachable pair;
  allPaths = all simple paths per reachable pair.
- `src/FLPQ.Cli/Program.fs`: dispatch AzimovCFPQ by semantics (default reachability);
  AllPaths for Arroyuelo/Belyanin → clear error (allPaths is CFPQ-only).
- `src/FLPQ.Printers/SummaryTeX.fs`: add `SummaryKind.AzimovCFPQ` (header: grammar_original +
  grammar_cnf + graph figure; steps: reuse the generic tableStepSection exactly like
  TablePerStep); `src/FLPQ.Cli/Summary.fs`: map AzimovCFPQ → that kind.
- New committed example data for CLI/tests: `data/cfpq_example/graph/{a,b}.mtx` +
  `data/cfpq_example/query.cnf` (the book's worked example — small hand-written files, not
  flpq-data reference values).

**Tests:** `AzimovCfpqCliTests.fs` in FLPQ.Cli.Tests (mirror existing runner tests): run each
of the three runners on data/cfpq_example; assert root artifacts + step dirs exist and
result.tex content is correct; `-s` summary generation produces a compilable merged TeX
(lualatex compile test if the project pattern exists); AllPaths rejected for RPQ algorithms.

**Docs:** Update `docs/user/cli.md` (new algorithm, semantics values, CFPQ input formats),
`docs/developer/FLPQ.Cli.md`; new `docs/developer/cfpq-matrix-tex.md`; update
`docs/developer/FLPQ.Printers.md` hub.

**Spec:** CLI: `-a AzimovCFPQ -q <file.cnf> -i <graph dir> [-o out] [-s] [--use-dot] [--semantics reachability|singlePath|allPaths]`. The three runners are the three entry points
named in the task.

### S8: Documentation + book fixes

**Code:** None (docs-only subtask — code-specific gates skipped per AGENTS.md).

**Tests:** None.

**Docs:**

- `tasks/fixes_for_book.md`: record the chapter_12_CFPQ/02_MatrixBased.tex inconsistency —
  the TikZ figure `figures/edge_labelled_graph_a_3_loop_b_2_loop.tex` shows b-edges 2↔3, but
  the worked example's T₀…T₁₃ sequence is only consistent with b-edges 0↔3 (T₀ has B at
  [0,3] and [3,0]).
- Final pass: `docs/main.md` architecture entry for FLPQ.CFPQ; hub completeness; cross-links
  between cfpq-input / cfpq-semiring / azimov-cfpq / cfpq-matrix-tex docs.
- Verify every touched `.md` is mdformat-clean.

**Spec:** Follow the documentation skill; one source of truth — algorithm description lives in
azimov-cfpq.md, semiring definitions in cfpq-semiring.md, input formats in cfpq-input.md.

## Design Notes (discovered during planning)

### Semiring design (user guidance, authoritative)

Common computational core (one matrix per nonterminal; init from terminal/ε rules; batch
fixed point over binary rules via indexed matrix product) with three distinct semirings:
reachability (bool), singlePath (PathIndex {middle, height}, min derivation-tree height,
extractable — LAGraph PR #8), allPaths (sorted split-vertex sets + base sentinel; the result
matrix contains information about all paths, not all walks or trails — enumeration yields
exactly all simple paths — LAGraph PR #9). CLI: single `-a AzimovCFPQ` + `--semantics`;
allPaths rejected for RPQ algorithms.

### flpq-data .cnf templates are not WCNF and may be indexed

Productions like `S -> type_r N1` or `S -> type_r type` appear in the shipped templates, and
some grammars use indexed placeholders (`load_i`, `_r`) that must be instantiated per graph
edge label. Decision: materialization happens in Python (export tool calls
`flpq_data.materialize_grammar`); F# parses plain grammars only and rejects `_i` symbols with
an actionable error. The core converts any plain CFG to WCNF via `Grammar.toCnf` before the
fixed point, so the book's algorithm (which assumes binary/terminal/ε rules) applies
unchanged.

### Batch vs in-place fixed point

The book iterates `T ← T ∪ (T×T)` (Jacobi-style: products from the previous T). In-place
per-rule accumulation (LAGraph's schedule) reaches the same least fixed point but with a
different per-iteration sequence. Batch semantics is implemented so the worked example's
T₀…T₁₃ golden trace (13 applications, 12 productive, one new nonterminal each) reproduces
exactly.

### split_tasks.py hrs bug (fixed on dev before this task)

`repair()` picked the first top-level hr in set-iteration order instead of the earliest,
which re-indented entries 288–292 into item 279 when task 293's entry added a new top-level
`---`. Fixed with `min()` over in-region separators (dev commit b1fbea3); log restored
(b46fc15).
