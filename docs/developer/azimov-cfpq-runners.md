# Azimov CFPQ CLI Runners

**Tags:** cli, cfpq, azimov, runner, reachability, simple-path, all-paths
**Kind:** runner
**Source:** `src/FLPQ.Cli/AzimovCfpqReachabilityRunner.fs`, `AzimovCfpqSinglePathRunner.fs`, `AzimovCfpqAllPathsRunner.fs`
**Depends on:** FLPQ.CFPQ (CfpqInput, CfpqSemiring, AzimovCfpq), FLPQ.Printers (CfpqMatrixTeX, PathSemiringTeX, GrammarTeX, RpqGraphViz), Helpers
**Used by:** `Program.fs` dispatch (`-a AzimovCFPQ`)
**Book reference:** Chapter 12, Section sec:CFPQ_MatrixBased, listing alg:graphParse

> **Abstract:** The three CLI entry points for Azimov's matrix-based CFPQ algorithm, one per semiring. All three parse the same inputs — a `.cnf` grammar (`-q`) and a graph directory of `*.mtx` files (`-i`) — run `AzimovCfpq.evaluate` over their semiring, and write the same root artifacts plus one `step_<iteration>/table.tex` per productive fixed-point iteration (the per-nonterminal matrices with the changed cells highlighted). They differ only in the element printer for the step tables and in `result.tex`: the Boolean answer matrix with the reachable pairs (reachability), one extracted witness path per pair (simplePath, LAGraph PR #8), or all simple paths per pair (allPaths, LAGraph PR #9).

## Contents

- [Shared Pipeline](#shared-pipeline)
- [Per-Runner Result Files](#per-runner-result-files)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Shared Pipeline

Each runner's `runAzimov (queryFile) (inputDir) (outputDir) (useDot)`:

1. Parses the grammar (`CfpqInput.parseCnfFile`) and the graph (`CfpqInput.parseGraphDir`).
2. Computes the same CNF conversion `evaluate` applies internally — `Grammar.toCnf Grammar.freshStringNonterminal` — so the answer is queried at `cnf.Start` and path extraction/enumeration walk the CNF rules.
3. Runs `AzimovCfpq.evaluate <semiring> Grammar.freshStringNonterminal grammar graph`.
4. Writes the root artifacts via `Helpers.writeCfpqRootArtifacts`: `grammar_original.tex`, `grammar_cnf.tex`, and the input graph as `graph.tikz.tex` (default) or `graph.dot` (`--use-dot`) — rendered by `RpqGraphViz.renderGraph` over `CfpqInput.toNfa graph` with no highlighted vertices or edges (all vertex pairs are queried, so there are no sources).
5. Writes the step directories via `Helpers.writeCfpqSteps`: one `step_<iteration>/table.tex` per productive iteration, rendering that iteration's matrix snapshot (`CfpqStep.Matrices`) with the changed cells highlighted.

## Per-Runner Result Files

| Runner | Semiring | Element printer (step tables) | `result.tex` |
| --- | --- | --- | --- |
| `AzimovCfpqReachabilityRunner` | `ReachabilitySemiring` | `\bullet` for every present cell | `T[Start]` as a Boolean matrix (`PathSemiringTeX.boolMatrixToTeX`) plus `Reachable pairs: N` and the pair list in row-major order |
| `AzimovCfpqSinglePathRunner` | `SinglePathSemiring` | `v_k^h` (middle vertex, derivation height) | One line per reachable pair: `v_i, v_j: <path>`, where `<path>` is the edge sequence from `AzimovCfpq.extractSinglePath` rendered by `CfpqMatrixTeX.pathToText` |
| `AzimovCfpqAllPathsRunner` | `AllPathsSemiring` | `\bullet` for a direct derivation, else the sorted split vertices (the sentinel renders as `d`) | One line per reachable pair: `v_i, v_j: N path(s)`, followed by one indented line per simple path from `AzimovCfpq.enumerateAllPaths` |

The status line reports the productive-iteration count and, per runner, the number of reachable pairs (plus the total simple-path count for allPaths).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Three thin runners over one generic core | The semiring is the only thing that changes; `evaluate`, the step tables, and the root artifacts are shared, so there is one source of truth for the algorithm's I/O |
| Runners recompute the CNF instead of receiving it from `evaluate` | `evaluate` converts internally and returns matrices keyed by CNF nonterminals; recomputing the deterministic conversion (`N_%d`) is how a caller learns the start symbol and the rules to walk for extraction/enumeration |
| The graph figure reuses `RpqGraphViz.renderGraph` via `CfpqInput.toNfa` | The CFPQ graph is an edge-labeled directed graph, which is exactly what the RPQ graph renderer draws; `toNfa` is the adapter (one transition per labeled edge, no start/final states) |
| Step directories are named `step_<iteration>` with 1-based iterations | Matches the book's T₁…T₁₃ numbering of productive iterations and the CLI's `naturalSortKey` ordering |
| `allPaths` is rejected for the RPQ algorithms in `Program.fs` | The `PathQuerySemantics` DU is shared by RPQ and CFPQ dispatch, but only Azimov implements it; failing fast with a clear message beats silently ignoring the flag |

## See Also

- [Azimov CFPQ algorithm](azimov-cfpq.md) — the generic core these runners drive
- [CfpqMatrixTeX module](cfpq-matrix-tex.md) — the step-table and path rendering
- [CfpqInput module](cfpq-input.md) — the `.cnf`/graph-directory parsing and `toNfa`
- [CLI user documentation](../user/cli.md) — command-line usage
