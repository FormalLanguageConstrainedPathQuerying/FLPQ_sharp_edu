# Kronecker CFPQ CLI Runner

**Tags:** cli, cfpq, kronecker, runner, reachability
**Kind:** runner
**Source:** `src/FLPQ.Cli/KroneckerCfpqRunner.fs`
**Depends on:** FLPQ.CFPQ (KroneckerCfpq, CfpqInput), FLPQ.Languages (RsmBuilder), FLPQ.Printers (RsmTikz, RsmDot, KroneckerStepViz, RpqGraphViz), Helpers
**Used by:** `Program.fs` dispatch (`-a KroneckerCFPQ`)
**Book reference:** Chapter 12, TensorProduct.tex section2 and listing lst:algo1

> **Abstract:** The CLI entry point for the Kronecker-product-based CFPQ algorithm. It parses an EBNF query file into an RSM (`RsmBuilder.buildRSMFromFile`) and a graph directory of `*.mtx` files, runs `KroneckerCfpq.evaluate` over the Boolean semiring, and writes the root artifacts (the RSM figure plus the input graph) along with one two-line `step_<iteration>/` per productive fixed-point iteration (the Kronecker product, the MS-BFS fronts, and the graph with the new nonterminal edges). Reachability only — `simplePath`/`allPaths` are rejected at dispatch.

## Contents

- [Pipeline](#pipeline)
- [Result File](#result-file)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Pipeline

`runKronecker (queryFile) (inputDir) (outputDir) (useDot)`:

1. Parses the query into an RSM (`RsmBuilder.buildRSMFromFile`) and the graph directory
   (`CfpqInput.parseGraphDir`).
2. Runs `KroneckerCfpq.evaluate rsm graph`.
3. Writes the root artifacts via `Helpers.writeKroneckerCfpqRootArtifacts`: the RSM figure as
   `rsm.tikz.tex` (default, `RsmTikz.rsmToTikz`) or `rsm.dot` (`--use-dot`, `RsmDot.toDot`), and
   the input graph as `graph.tikz.tex` / `graph.dot` — rendered by `RpqGraphViz.renderGraph` over
   `CfpqInput.toNfa graph` with no highlighted vertices or edges (all vertex pairs are queried, so
   there are no sources).
4. Writes the step directories via `Helpers.writeKroneckerStepsVisualization`: one
   `step_<iteration>/` per productive iteration with the two-line step visualization —
   `kron.tex` (line 1: the full Kronecker product `M_1 ⊗ M_2 = K`, the operands with set-of-symbols
   cells and the Boolean result mask with this iteration's newly derived cells highlighted),
   `fronts.tex` (line 2: the MS-BFS initial (seed) and final (visited) fronts, decoded `(q_i, u_j)`
   rows / `(q_i, u_j)` columns, the final front with its accept columns highlighted), and the input
   graph with this iteration's newly added nonterminal edges drawn and highlighted red bold, plus
   every earlier iteration's nonterminal edges preserved in black (the derived graph grows over
   iterations; only the current step's edges are highlighted)
   (`graph.tikz.tex` default, `graph.dot` with `--use-dot`) — rendered by `KroneckerStepViz` over
   `RpqGraphViz.renderGraph` (the current edges are passed as `currentEdges` (red bold); all
   accumulated edges — current plus prior — as `extraEdges`/`extraEdgeLabels`, so prior edges
   appear black and the current ones red bold).
5. Writes `result.tex` via `Helpers.renderResult`: the start block's answer matrix
   (`KroneckerCfpq.answer result rsm`) as a Boolean matrix plus `Reachable pairs: N` and the pair
   list in row-major order.

The status line reports the productive-iteration count and the number of reachable pairs.

## Result File

`result.tex` holds the start block's reachability matrix (the pair `(i, j)` is true iff some path
from `i` to `j` is labeled by a word of `L(start)`) rendered as a Boolean matrix, followed by the
reachable-pair count and the pairs in row-major order.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| A single reachability runner (no simplePath/allPaths) | The Kronecker algorithm computes Boolean reachability only; the split-vertex and path-index semirings live in Azimov's matrix core, so those semantics are rejected at dispatch with a clear message rather than silently ignored |
| The query is an EBNF file parsed to an RSM | The Kronecker algorithm takes an RSM directly (no grammar-to-CNF conversion); `RsmBuilder.buildRSMFromFile` builds the derivative-based RSM whose language is exactly the query |
| Root artifacts are the RSM figure plus the graph (no grammar files) | Unlike the Azimov runners (which show the original and CNF grammars), Kronecker's input is an RSM, so its figure is the RSM itself (`rsm.tikz.tex` / `rsm.dot`) |
| Reuses `renderResult` / `reachablePairs` | The result-file layout is shared with the Azimov reachability runner, keeping one source of truth for CFPQ result I/O |
| The step is a dedicated two-line visualization, not the shared step table | Kronecker's step carries the product operands/result and the MS-BFS fronts (the Azimov `CfpqStep` shape has none of these), so `writeKroneckerStepsVisualization` + `KroneckerStepViz` render a richer step than the shared `writeCfpqSteps` table |

## See Also

- [Kronecker CFPQ algorithm](kronecker-cfpq.md) — the product-construction fixed point this runner drives
- [Azimov CFPQ runners](azimov-cfpq-runners.md) — the grammar-based sibling with the same step/result layout
- [RSM visualization](rsm-viz.md) — `RsmTikz.rsmToTikz` and `RsmDot.toDot`
- [Kronecker CFPQ step visualization](kronecker-step-viz.md) — the two-line step renderer
- [CfpqInput module](cfpq-input.md) — graph-directory parsing and `toNfa`
- [CLI user documentation](../user/cli.md) — command-line usage
