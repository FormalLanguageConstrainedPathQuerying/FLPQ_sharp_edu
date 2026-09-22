# Global Plan: RPQ Algorithm Visualization (Tasks 280, 281)

## Tasks

| ID | Title | Summary |
| --- | --- | --- |
| 280 | Arroyuelo RPQ visualization + shared RPQ-viz infrastructure | Add `ArroyueloRPQ` to the CLI: input (regexp + graph), results, per-tree-node steps, merged summary. One step = one regexp-tree node (post-order), current node highlighted; matrix operation visualized (TC as single `TC(M)=M'`); partial paths highlighted in the graph via a custom path semiring. Also creates all shared infrastructure for task 281: CLI regexp/graph arguments, `Regexp.toDfa`, `PathSemiring` module, path-matrix TeX printer, example data files. |
| 281 | Belyanin RPQ visualization | Add `BelyaninRPQ` to the CLI with the same structure (input, results, steps, summary). One step = one main-loop iteration (BFS-like frontier propagation); visualizes per-label matrix multiplications `(N^a)^T ⊗ M ⊗ G^a`, current frontier in the graph and in the automaton. Step layout tuned from the RNGLR two-column template. Reuses all shared infrastructure from task 280. |

## Book References

- Arroyuelo: Chapter 11, `03_Arroyuelo.tex` (sec:RPQ_Arroyuelo) — regexp → Boolean matrix
  translation M(ε)=I, M(a)=M_a, M(E1|E2)=M(E1)+M(E2), M(E1/E2)=M(E1)×M(E2),
  M(E*)=I+M(E)^+; post-order evaluation of the regexp syntax tree.
- Belyanin: Chapter 11, `02_BFS.tex` (sec:RPQ_BFS, algo:RPQ_BFS_semiring) — BFS over
  (q,v) pairs with two |Q|×|V| matrices M (front) and P (accumulated); path semiring
  carrier 2^{V*} with index operator I_simple (book example figure
  `figures/02_BFS/algorithm_step.tex` shows paths in matrix cells).

## Dependencies and Execution Order

1. **280 first** — it owns the shared infrastructure (CLI arguments, `Regexp.toDfa`,
   `PathSemiring`, path-matrix TeX, data files, summary plumbing pattern).
2. **281 second** — builds on 280's infrastructure; no code of its own is needed by 280.

No parallelism: both tasks modify the same CLI/summary files (`AlgorithmTypes.fs`,
`Program.fs`, `Summary.fs`, `SummaryTeX.fs`, `Helpers.fs`, `docs/user/cli.md`).
Strictly sequential execution avoids merge conflicts.

## Conflicts / Overlapping Changes

| File | 280 | 281 |
| --- | --- | --- |
| `src/FLPQ.Cli/AlgorithmTypes.fs` | +`ArroyueloRPQ` case, +`Regexp`/`GraphFile` args | +`BelyaninRPQ` case |
| `src/FLPQ.Cli/Program.fs` | RPQ dispatch branch (shared shape) | second algorithm in the same branch |
| `src/FLPQ.Cli/Summary.fs`, `src/FLPQ.Printers/SummaryTeX.fs` | +kind, header, step section, template record refactor | +kind, header, step section |
| `src/FLPQ.Cli/Helpers.fs` | +template finders, +step writer (Arroyuelo) | +template finders, +step writer (Belyanin) |
| `docs/user/cli.md`, `docs/main.md`, `docs/project/architecture.md`, hub docs | updated | updated |

Because 281 starts only after 280 is merged to dev, there is no concurrent editing.

## Shared Infrastructure (created in 280, reused by 281)

### CLI arguments (`AlgorithmTypes.fs`, `Program.fs`)

- New Argu cases: `[<AltCommandLine("-r")>] Regexp of string` (regexp file, EBNF format),
  `[<AltCommandLine("--graph")>] GraphFile of string` (graph file, `GraphReader` format).
- Grammar (`-g`) and input (`-i`) become required only for parsing algorithms: the
  `GetResult` calls move from the top of `runCli` into the dispatch branches. RPQ
  algorithms require `-r` and `--graph`; parsing algorithms require `-g` and `-i`.
  Missing required argument → Argu error with usage (existing behavior).
- New `Algorithm` DU cases: `ArroyueloRPQ`, `BelyaninRPQ` (display names
  "Arroyuelo RPQ", "Belyanin RPQ"; lower-case directory names `arroyuelorpq`,
  `belyaninrpq`).

### Regexp input format and DFA construction

- Regexp file: EBNF rule(s) `S -> <expr>` parsed by the existing
  `EbnfParser.parseEbnf`; the **first rule's RHS** is the query regexp
  (`Regexp<string, string>`). Documented in `docs/user/cli.md`.
- New shared function `Regexp.toDfa : Regexp<'t,'nt> -> DFA<'t,int>` in
  `src/FLPQ.Languages/EbnfParser.fs`: alphabet = terminals from `Regexp.symbols`
  (fallback `["a"]` when empty, matching the test helper), derivation over
  `RsmSymbol.RTerm`. Generalizes the test-local `regexToDfa` in
  `tests/FLPQ.RPQ.Tests/RPQTests.fs`, which is replaced by a call to it.

### Path semiring (`src/FLPQ.RPQ/PathSemiring.fs`) — new module

Custom semiring over sets of vertex sequences (the book's "path semiring",
carrier 2^{V*}; Valiant's `Set<SppfParsingEntry>` cells are the inspiration for
tracking witnesses inside matrix cells):

- A path is an `int list` (vertex sequence, endpoints included); a matrix cell is
  `Set<int list>`.
- Zero = empty set; addition = set union.
- Cell product for (i,k)×(k,j): for p1 in A[i,k], p2 in B[k,j] with
  `last p1 = k = head p2`, candidate `p1 @ (List.tail p2)`; kept only if simple
  (no repeated vertex).
- Boolean-mask products for Belyanin: `(N^a)^T ⊗ M` (boolean rows select path rows)
  and `M ⊗ G^a` (extend each path by the graph-edge target vertex, keep only
  simple extensions — this is where I_simple filtering happens naturally).
- `isSimple`, `extend : int list -> int -> int list option` (append iff new),
  identity cell `[i]` for ε.
- Matrix operations built on `LinearAlgebra.mxm` / `Matrix.map2` with the semiring
  ops (no new generic linear-algebra code).

**Semantics decision (documented in both algorithm docs):** cells store **simple
paths only**, so every cell is finite and Belyanin's loop terminates in ≤ |V|-1
iterations. Consequence: on cyclic graphs the boolean projection of the path
result may be a strict subset of full reachability (walks revisiting a vertex are
excluded by I_simple — exactly the book's example where candidate (v1,v2,v1) is
dropped). On **acyclic** graphs every walk is simple, so the boolean projection
coincides with the existing Boolean `evaluate` results — this is the equivalence
requirement, tested as a property.

### Path-matrix TeX (`src/FLPQ.Printers/PathSemiringTeX.fs`) — new module

- `pathToTeX : int list -> string` → `(v_0, v_2, v_3)` (math mode).
- `pathSetCellToTeX : Set<int list> -> string` → `\{(v_0, v_1), ...\}`, empty set →
  `\cdot` (same convention as Valiant's empty cells).
- `matrixToTeX : Matrix<Set<int list>> -> rowLabels -> colLabels -> string` wrapping
  `MatrixTeX.toTeXStyled` (pNiceMatrix, vertex-index row/col headers, adjustbox).

### Regexp TeX (`src/FLPQ.Printers/RegexpTeX.fs`) — new module

Renders the regexp AST as a math formula: `a / (b \mid c)^* / d`, ε as
`\varepsilon`, terminals escaped via `AutomatonTikz.escapeLatex`. Used for the
input section and for labeling tree nodes.

### Regexp tree figures (`src/FLPQ.Printers/RegexpTreeDot.fs`, `RegexpTreeTikz.fs`) — new modules

Render the regexp AST as a tree (node label = subexpression via `RegexpTeX`), with
the current node highlighted (DOT: `fillcolor=lightblue`; TikZ: `fill=lightblue!20`).
TikZ reuses `AutomatonTikz.tikzHeader/tikzFooter` primitives; DOT reuses
`DerivationTreeDot.escapeLabel`.

### Graph figures — reuse, no new module

The input graph (NFA with states = vertices) is rendered by the **existing**
`GssDot.toDotFromSets` / `GssTikz.toTikzFromSets`: all vertices/edges passed as
active, step paths as highlighted vertices/edges, `storedPopVertices = Set.empty`,
`currentVertex = None`, `positionOf = None`. Vertex label printer = vertex index;
edge label printer = comma-joined terminal labels of the pair.

### DFA figures — generalization of existing renderers

`AutomatonTikz.dfaToTikz` and `AutomatonDot.dfaToDot` gain highlighted-state
variants (`dfaToTikzWithHighlights`, `dfaToDotWithHighlights`); the existing
functions delegate with an empty highlight set (single source of truth in the
private `stateDeclarations` helper, which gains a highlight parameter).

### Step visualizers — new modules (one per task)

- 280: `src/FLPQ.Printers/ArroyueloStepVisualizer.fs` —
  `ArroyueloVisualizationStep { TreeDot; TreeTikz; Matrices; GraphDot; GraphTikz }`.
- 281: `src/FLPQ.Printers/BelyaninStepVisualizer.fs` —
  `BelyaninVisualizationStep { AutomatonDot; AutomatonTikz; Matrices; GraphDot; GraphTikz }`.

### Step templates (data/)

Two-column minipage layout modeled on `data/RNGLR_step_template.tex` /
`RNGLR_step_tikz_template.tex`:

- 280: `data/Arroyuelo_step_template.tex` + `data/Arroyuelo_step_tikz_template.tex`
  — left: regexp tree (current node highlighted) + matrix operation; right: input
  graph with partial paths highlighted.
- 281: `data/Belyanin_step_template.tex` + `data/Belyanin_step_tikz_template.tex`
  — left: DFA with frontier states highlighted + M/P matrices; right: input graph
  with frontier paths highlighted + per-label propagation products.
- TikZ figures inside templates are wrapped via the existing
  `SummaryTeX.wrapTikzAdjustbox false` (adjustbox, shrink-only to \textwidth) —
  the "use adjustbox for tikz" requirement.

### Summary plumbing (`SummaryTeX.fs`, `Summary.fs`, `Helpers.fs`)

- New `SummaryKind` cases: `ArroyueloRPQ`, `BelyaninRPQ`.
- Header for both kinds: color legend + regexp (math) + DFA figure + graph figure.
- Step sections `arroyueloStepSection` / `belyaninStepSection` fill the templates
  (pattern of `rnglrStepSection`).
- **Refactor:** `SummaryTeX.buildContent` currently takes 4 positional template
  strings; with two more algorithms it would reach 15+ parameters. Replace the
  template parameters with a single `StepTemplates` record
  `{ Gll; GllTikz; Rnglr; RnglrTikz; Arroyuelo; ArroyueloTikz; Belyanin; BelyaninTikz }`
  (done in 280, used by both).
- `Helpers.fs`: `findArroyueloStepTemplate(Tikz)`, `findBelyaninStepTemplate(Tikz)`
  (same candidate-list pattern), `writeArroyueloStepsVisualization`,
  `writeBelyaninStepsVisualization`.

### Data files (created in 280, used by both)

- `data/example_regexp.txt` — EBNF rule with a non-trivial expression
  (alternation + concatenation + star), e.g. `S -> a / (b | c)* / a`.
- `data/example_graph.txt` — small labeled graph in `GraphReader` format with an
  explicit start-vertex line, containing a cycle (to exercise I_simple filtering)
  and multiple labels.

## Output Structure (both algorithms, "same structure")

```
<output>/
  regexp.tex                  # query regexp as math formula
  dfa.tikz.tex | dfa.dot      # query DFA (TikZ default, DOT fallback)
  graph.tikz.tex | graph.dot  # input graph, no highlights
  result.tex                  # final matrix of path sets + reachable vertices per source
  step_0/ ... step_N/
    <per-step files, see step visualizers>
<output>/results/<algo-lower>/   # with -s: dot_pdfs/, <algo>_merged.tex, PDF
```

- Arroyuelo steps: `step_k` = k-th post-order node. Files: `tree.*`,
  `matrices.tex` (operation: base matrix / M1⊕M2=M3 / M1×M2=M3 / TC(M)=M'),
  `graph.*` (all paths of the node's result matrix highlighted).
- Belyanin steps: `step_0` = initialization (M with [v_s] at start states, P empty);
  `step_k` = k-th main-loop iteration. Files: `automaton.*` (frontier states
  highlighted), `matrices.tex` (M after I_simple, P, per-label products, new M),
  `graph.*` (frontier paths highlighted).

## Equivalence Requirements

- 280: boolean projection of the path-semiring result matrix (source rows) equals
  `ArroyueloRPQ.evaluate` on **acyclic** graphs — FsCheck property; plus golden
  tests for step artifacts and a lualatex compilation test of the merged summary.
- 281: same against `BelyaninRPQ.evaluate` on acyclic graphs; plus a fact test
  reproducing the book example (candidate path dropped by I_simple) and golden +
  compilation tests.

## Reuse Analysis (reusing skill checklist)

| Need | Reused / Generalized | New |
| --- | --- | --- |
| Regexp AST, parsing, DFA from regexp | `Regexp`, `EbnfParser.parseEbnf`, `Regexp.buildDfaFromRegex` | `Regexp.toDfa` (generalizes test helper) |
| Graph input | `GraphReader.parseGraphFile` | — |
| Boolean RPQ results (equivalence baseline) | `ArroyueloRPQ.evaluate`, `BelyaninRPQ.evaluate` | trace variants in the same modules |
| Matrix rendering with set cells | `MatrixTeX.toTeXStyled` (Valiant pattern) | `PathSemiringTeX` cell printer + wrapper |
| Graph figure with highlights | `GssDot.toDotFromSets`, `GssTikz.toTikzFromSets` | — |
| DFA figure | `AutomatonDot.dfaToDot`, `AutomatonTikz.dfaToTikz` (generalized with highlights) | highlight variants |
| Tree figure | `AutomatonTikz` primitives, `DerivationTreeDot.escapeLabel` | `RegexpTreeDot/Tikz` |
| Regexp formula | `Regexp.toString` (inspiration only — needs math mode + escaping) | `RegexpTeX` |
| Step layout template | `data/RNGLR_step_template.tex` pattern, `SummaryTeX.rnglrStepSection` pattern | 4 new template files |
| Summary assembly | `SummaryTeX.buildContent`, `wrapTikzAdjustbox`, `compileDotArtifacts` | 2 new kinds + step sections; template record refactor |
| Runner/step-writing pattern | `RnglrRunner.fs`, `Helpers.writeRnglrStepsVisualization` | 2 new runners + 2 writers |
| Test helpers | `TestHelpers.nfaFromEdges`, `TestHelpers.buildDfa`, FsCheck generators in `FLPQ.TestUtilities` | acyclic-graph generator for equivalence properties |

## Rendering vs Algorithm Separation

Algorithm logic (path semiring, trace collection) lives in `FLPQ.RPQ`; all
rendering lives in `FLPQ.Printers`; file I/O and dispatch in `FLPQ.Cli`. Trace
step records are plain data (matrices + highlight sets) — no TeX strings inside
`FLPQ.RPQ`.
