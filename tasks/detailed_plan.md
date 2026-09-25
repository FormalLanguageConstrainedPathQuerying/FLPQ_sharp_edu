# Detailed Plan: Task 284 — Improve Belyanin RPQ rendering

## Task description (verbatim)

```
284. Improve Belyanin RPQ rendering. 1. Use F for frontier (instead M), use V for visited (instead P). 2. EXplicitly write all matrices: '(N^a)^T \otimes F = <explicit_representation_of_N^a^T> \otimes <explicit_representation_of_F> = <explicit_representation_of_result>' 3. Fix indices in matrices. Currently v_i used for both coluns and rows, but come of them are automata states, so must be q_i. 4. Improve graphs: currently in graph traverced edges highlighted with shift on 1 step: edges from current step highlighted at next step. Moreover, highlight edge on this step with color different from path. Eg save red for current edge, and light red for path. Add bold for current edge. Highlight only vertices touched on current step (from and to), not all vertices in path.
```

## Scope and constraints

The task changes only the **rendering** of Belyanin's RPQ evaluation
(`BelyaninStepVisualizer` + the shared pieces it uses). The algorithm
(`BelyaninRPQ.evaluateWithTrace`) keeps its book-faithful names (the book's
listing `algo:RPQ_BFS_semiring` uses M/P); only rendered labels change to the
user-requested F/V notation.

Four requirements, mapped to subtasks:

1. **F/V notation** (S4): rendered matrix titles become `$\text{F}$` (frontier,
   was M), `$\text{V}$` (visited/accumulated, was P), `$\text{New } F =$` (was
   New M), and the per-label product line becomes `$(N^{a})^T \otimes F =$`.

2. **Explicit matrices** (S1 + S2 + S4): every matrix in a per-label block is
   written out, not just the results:

   ```
   $(N^{a})^T \otimes F =$
   [N^a^T]          boolean |Q|x|Q| matrix, q_i rows and columns
   $\otimes$
   [F]              path matrix (the step's frontier, re-rendered)
   $=$
   [Select]         path matrix
   $\times G^{a} =$
   [G^a]            boolean |V|x|V| matrix, v_i rows and columns
   $=$
   [Extend]         path matrix
   ```

   N^a and G^a are stored in the trace (`BelyaninLabelStep` gains `N` and `G`
   fields) so the trace stays the single source of truth for rendering.

3. **Indices** (S2 + S4): |Q|x|V| path matrices get q_i row labels and v_i
   column labels (`PathSemiringTeX.matrixWithStateVertexLabels`); boolean
   matrices get matching square labels (N^a^T: q_i x q_i, G^a: v_i x v_i) via a
   new `PathSemiringTeX.boolMatrixToTeX` (cells `\bullet`/`\cdot`, the book's
   figure convention). The runner's result matrix (final P) also switches to
   q/v labels. Arroyuelo's |V|x|V| matrices keep v/v labels (unchanged).

4. **Graph highlights** (S3 + S5): two edge tiers + restricted vertex
   highlight:

   - **current edges** (red, bold) = the last edge of every path in the step's
     `NewM` — exactly the edges traversed during this iteration. This fixes the
     one-step shift: today the figure highlights all edges of the step's M
     paths, whose newest edge was traversed at the *previous* step.
   - **path edges** (light red) = all edges of the step's M paths (the frontier
     being processed).
   - **highlighted vertices** (lightyellow, color unchanged) = endpoints of the
     current edges only — not all vertices of the frontier paths.
   - Initialization step: no highlights at all (trivial paths have no edges).

   The generic set-based renderers (`GssDot.toDotFromSets`,
   `GssTikz.toTikzFromSets`) gain one parameter, `pathEdges` (light-red tier);
   the strong tier becomes bold in TikZ (`red, thick` — matching the book's
   figure, where the current walk is drawn `MyRed, thick`). DOT strong edges
   are already bold (`penwidth=2.0`). Colors: strong = `red` / `color=red`;
   path = `red!40` (TikZ) / `#FF9999` (DOT, same RGB).

Constraints:

- GLL and RNGLR rendered output must stay byte-identical (they pass no
  highlighted edges in TikZ; their DOT strong-edge style is unchanged).
- Arroyuelo's graph figures share `RpqGraphViz`; its red edges gain the TikZ
  boldness (same shared renderer, book-consistent) — its goldens are
  regenerated in S3. No other Arroyuelo output changes.
- Belyanin automaton figures are unchanged (frontier-state highlighting stays).
- Every subtask compiles and passes its tests; goldens are regenerated within
  the subtask that changes the corresponding output.

## Reuse analysis (reusing skill)

- `MatrixTeX.toTeXStyled` — reused for boolean matrices (no new matrix engine);
  custom row/col label printers already supported.
- `PathSemiringTeX.matrixToTeX` — reused for the new q/v-labeled path matrices.
- `GssDot.toDotFromSets` / `GssTikz.toTikzFromSets` — extended with the
  `pathEdges` tier instead of duplicating graph rendering in RpqGraphViz.
- `RpqGraphViz` — stays the single RPQ graph entry point; new edge-set helpers
  live here next to `pathHighlights`.
- `BelyaninRPQ.dfaLabelMatrix` + `BooleanDecomposition.decomposeNonEmptySet` —
  already compute N^a/G^a inside `evaluateWithTrace`; the trace now stores them.
- No new modules, no new files.

## Subtasks

### S1: Store N^a and G^a in the Belyanin trace [done, bb8e327]

**Code:** `src/FLPQ.RPQ/BelyaninRPQ.fs` — extend
`BelyaninLabelStep<'t>` to `{ Label; N: Matrix<bool>; G: Matrix<bool>; Select; Extend }` (N = `dfaLabelMatrix dfa label`, G = the per-label graph matrix from
the existing decomposition). Fill both fields in `evaluateWithTrace` where the
label loop already has `label` and `gMat`. Update the record's doc comment.

**Tests:** `tests/FLPQ.RPQ.Tests/BelyaninTraceTests.fs` — new facts asserting
the stored matrices on the example: step 1 label a has N with exactly
N[0,1] = true (q0 -a-> q1) and G with exactly edges (0,1), (2,5), (3,4);
step 2 labels a/b/c have G equal to the per-label graph edge sets and N equal
to the DFA per-label transition sets.

**Docs:** `docs/developer/belyanin-rpq.md` — `BelyaninLabelStep` signature and
description gain N/G.

**Spec:**

- N is stored untransposed (the book's N^a); the visualizer transposes at
  render time for the `(N^a)^T` factor.
- Epsilon labels are skipped as today; no label step is created for them.
- Boolean `evaluate` is untouched.

### S2: PathSemiringTeX — q/v-labeled path matrices and boolean matrices [done, 3e0156a]

**Code:** `src/FLPQ.Printers/PathSemiringTeX.fs` — add
`matrixWithStateVertexLabels (m: Matrix<Set<int list>>) : string`
(`matrixToTeX` with q_i row / v_i column labels) and
`boolMatrixToTeX (rowLabel: int -> string) (colLabel: int -> string) (m: Matrix<bool>) : string` (`MatrixTeX.toTeXStyled` with a `\bullet`/`\cdot`
cell printer, adjustbox-wrapped). Keep `matrixWithVertexLabels` (Arroyuelo's
|V|x|V| matrices).

**Tests:** `tests/FLPQ.Printers.Tests/PathSemiringTexTests.fs` — new facts:
`matrixWithStateVertexLabels` emits q_i row headers and v_i column headers;
`boolMatrixToTeX` renders true cells as `\bullet`, false as `\cdot`, with the
given labels, wrapped in adjustbox.

**Docs:** `docs/developer/path-semiring-tex.md` — new function signatures +
descriptions; `docs/developer/FLPQ.Printers.md` hub one-liner for
PathSemiringTeX mentions boolean matrices.

**Spec:**

- Boolean cell printer: `true -> @"\bullet"`, `false -> @"\cdot"` (the book's
  figure `figures/02_BFS/algorithm_step.tex` uses bullets for true entries).
- Both functions are pure TeX strings, no document-level commands (module
  convention).

### S3: GSS renderers — light-red path-edge tier + bold strong edges [done, a735252]

**Code:** `src/FLPQ.Printers/GssTikz.fs`, `src/FLPQ.Printers/GssDot.fs` — add a
`pathEdges: Set<int * int>` parameter to `toTikzFromSets` / `toDotFromSets`
(inserted after `highlightedEdges`). Edge rendering tiers, in priority order:
strong (`highlightedEdges`) → TikZ `red, thick`, DOT `color=red, penwidth=2.0`
(DOT unchanged); path (`pathEdges`) → TikZ `red!40`, DOT `color="#FF9999"`;
else plain. An edge in both sets renders as strong. Update every call site to
pass `Set.empty` for the new parameter: `RpqGraphViz.renderGraph` (2),
`ArroyueloStepVisualizer.renderSteps` (2), `GllStepVisualizer` (4),
`RnglrStepVisualizer` (2).

**Tests:** `tests/FLPQ.Printers.Tests/GssDotTests.fs` — update the TikZ
red-edge assertion to the new `red, thick` string; new facts: a path-edge
renders light-red in both formats (`red!40` / `color="#FF9999"`), a strong edge
takes priority over a path edge, plain edges are unchanged. All other suites
(GssDotVisualizationTests, Gll/Rnglr step tests) must pass with byte-identical
goldens. Regenerate the Arroyuelo graph TikZ goldens (red edges gain `thick`)
via the golden workflow; Arroyuelo DOT output is byte-identical.

**Docs:** none new (no GssDot/GssTikz module docs exist); check
`docs/developer/FLPQ.Printers.md` hub for stale one-liners.

**Spec:**

- TikZ strong-edge string: `v%d ->["%s", red, thick%s%s] v%d` (bend/loop attrs
  appended after, as today).
- TikZ path-edge string: `v%d ->["%s", red!40%s%s] v%d`.
- DOT path-edge attrs: `label="%s", color="#FF9999"` (quoted — an unquoted #
  starts a DOT comment and breaks graphviz compilation).
- Empty-label edges keep the current behavior per tier (strong/path tiers emit
  the quoted label; plain tier omits it).

### S4: Belyanin matrices — F/V notation, explicit products, q/v indices [done, 1c35145]

**Code:** `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — `renderMatrices`:
titles `$\text{F}$`, `$\text{V}$`, `$\text{New } F =$`; all path matrices via
`PathSemiringTeX.matrixWithStateVertexLabels`. `labelBlock`: the explicit chain
from Scope item 2 — `$(N^{a})^T \otimes F =$`, `boolMatrixToTeX` of
`Matrix.transpose ls.N` with q/q labels, `$\otimes$`, the step's F matrix,
`$=$`, Select, `$\times G^{a} =$`, `boolMatrixToTeX` of `ls.G` with v/v labels,
`$=$`, Extend. `src/FLPQ.Cli/BelyaninRunner.fs` — `renderResult` uses
`matrixWithStateVertexLabels` for the final matrix (rows are automaton states).

**Tests:** `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` —
update the pNiceMatrix block-count fact (init: 2; iteration: 2 + 5 x labels +
1); new facts: every non-init step contains the explicit chain markers
(`\otimes F =`, `\times G^{a} =`) per label, path matrices carry q_i row
headers and v_i column headers while boolean N^a^T/G^a blocks carry q/q and
v/v headers respectively, and no matrix block uses a v_i row header for a
|Q|x|V| matrix. Regenerate the six `belyanin_step_*_matrices.tex` goldens via
the golden workflow. The lualatex compile + no-Overfull facts must pass on all
steps (taller matrix stacks shrink via the step adjustbox).

**Docs:** `docs/developer/belyanin-step-viz.md` — Matrices artifact description
(F/V titles, explicit per-label chain, q/v labels); `tasks/fixes_for_book.md` —
note that the book's M/P notation should become F (frontier) / V (visited) to
match the rendering.

**Spec:**

- The F matrix is re-rendered inside every label block (explicit operands per
  the task), even though it equals the top-level F block.
- Init step renders only the F and V blocks (no label blocks, no New F).
- Step 5 (zero labels) renders F, V, New F only.

### S5: Belyanin graph — current-step edges, path edges, endpoint vertices [done, a1f0783]

**Code:** `src/FLPQ.Printers/RpqGraphViz.fs` — add `pathVertices`,
`pathEdges`, `pathLastEdges` (last edge of every stored path with >= 2
vertices), `edgeEndpoints`; refactor `pathHighlights` to reuse them; change
`renderGraph` to `(terminalPrinter, graph, highlightedVertices, pathEdges, currentEdges)`. `src/FLPQ.Printers/BelyaninStepVisualizer.fs` — wire the graph
figure: currentEdges = `pathLastEdges step.NewM`, pathEdges = `pathEdges step.M`, highlightedVertices = `edgeEndpoints currentEdges`.
`src/FLPQ.Printers/ArroyueloStepVisualizer.fs` — pass
`(pathVerts, Set.empty, pathEdgeHl)` (output unchanged after S3).

**Tests:** `tests/FLPQ.Printers.Tests/RpqGraphVizTests.fs` — new facts for
`pathEdges`, `pathLastEdges` (single-vertex paths contribute nothing),
`edgeEndpoints`; update `renderGraph` call sites to the 5-arg signature; new
fact: a path edge renders light-red and a current edge renders strong in both
formats. `tests/FLPQ.Printers.Tests/BelyaninStepVisualizationTests.fs` —
replace the frontier-paths graph facts with: red (strong) DOT edges = exactly
the last edges of NewM paths, light-red DOT edges = exactly the edges of M
paths, yellow vertices = exactly their endpoints; per-step expected sets are
computed from the trace. Regenerate the six `belyanin_step_*_graph.tikz`
goldens. The lualatex compile + no-Overfull facts must pass.

**Docs:** `docs/developer/rpq-graph-viz.md` — new function signatures,
renderGraph signature, two-tier highlight design decision;
`docs/developer/belyanin-step-viz.md` — Graph artifact description (current vs
path edges, endpoint-only vertices, init step has no highlights).

**Spec:**

- `pathLastEdges`: for a path `[v0; ...; vk]` with k >= 1 contribute
  `(v_{k-1}, vk)`; trivial paths contribute nothing.
- Current edges are computed from `NewM`, not M — this is the one-step-shift
  fix: the edges traversed during iteration k are the final edges of the
  paths produced by that iteration.
- Initialization step: NewM = M holds only trivial paths, so all three
  highlight sets are empty.

## Verification

- Per subtask: format + build + affected test suites (subtask-loop skill).
- Pre-merge: full hard gate (`tools/hard_gate.py`) — format, lint, build,
  tests with coverage (line >= 90% per project / 95% total, branch likewise),
  markdown checks. New code paths are covered by the new facts above.
- Golden diff review: only `belyanin_step_*_matrices.tex`,
  `belyanin_step_*_graph.tikz`, and `arroyuelo_step_*_graph.tikz` may change;
  every other golden must be byte-identical.
