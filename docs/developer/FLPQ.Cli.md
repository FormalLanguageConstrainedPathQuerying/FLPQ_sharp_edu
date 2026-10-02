# FLPQ.Cli

**Tags:** cli, console, runner, cyk, valiant, ll, lr, gll, rnglr, rpq, arroyuelo, belyanin, summary, tex, dot
**Kind:** hub
**Source:** `src/FLPQ.Cli/`
**Depends on:** FLPQ.Languages, FLPQ.Printers, FLPQ.RPQ, Argu
**Used by:** _(none — top-level application)_
**Book reference:** _(application — no direct book reference)_

> **Abstract:** Console application for running parsing algorithms (CYK, Valiant standard/modified, LL(k), LR(0)/SLR(1)/CLR(1), GLL, RNGLR) and RPQ algorithms (Arroyuelo, Belyanin) with file-based I/O and step-by-step visualization output to TeX/Dot/Tikz. Supports merged summary PDF generation via the `--summary` flag, compiling all outputs through Graphviz and lualatex. Uses Argu for argument parsing.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Role](#role)
- [See Also](#see-also)

## Project

- **Type**: F# console application (`net10.0`)
- **Path**: `src/FLPQ.Cli/`
- **Dependencies**: `FLPQ.Languages`, `FLPQ.Printers`, `FLPQ.RPQ`, Argu

## Modules

| Module | Source | Documentation |
| --- | --- | --- |
| `Program` | `Program.fs` | [CLI console application](../user/cli.md) |
| `ArroyueloSimplePathRunner` | `ArroyueloSimplePathRunner.fs` | [Arroyuelo simple-path step visualization](arroyuelo-step-viz.md) — simplePath semantics (`--semantics simplePath`): path-semiring matrices and result-path highlights, one directory per intermediate trace step |
| `ArroyueloReachabilityRunner` | `ArroyueloReachabilityRunner.fs` | [Arroyuelo reachability step visualization](arroyuelo-reachability-step-viz.md) — default reachability semantics: Boolean matrices and derived M(E) edges, one directory per intermediate trace step |
| `BelyaninSimplePathRunner` | `BelyaninSimplePathRunner.fs` | [Belyanin simple-path step visualization](belyanin-step-viz.md) — simplePath semantics (`--semantics simplePath`): the answer comes from the path-semiring trace, and one directory is written per BFS iteration |
| `BelyaninReachabilityRunner` | `BelyaninReachabilityRunner.fs` | [Belyanin reachability step visualization](belyanin-reachability-step-viz.md) — default reachability semantics: the answer comes from the Boolean `BelyaninRPQ.evaluate`, the Boolean trace covers the first source, and one directory is written per BFS iteration |

## Role

Command-line interface using Argu for argument parsing. Allows running CYK, Valiant, LL, LR0, SLR1, CLR1, GLL, RNGLR algorithms with file-based I/O and step-by-step visualization output. All algorithms take the same unified query/input pair: `-q` is the query (a grammar file for parsing algorithms, a regexp file for RPQ algorithms) and `-i` is the input (an input string for parsing algorithms, a graph file for RPQ algorithms). `-a ArroyueloRPQ` and `-a BelyaninRPQ` select their semantics via `--semantics` (`-semantics`): `reachability` (default — Boolean, the classical RPQ answer computed by `ArroyueloRPQ.evaluate` / `BelyaninRPQ.evaluate`, complete on cyclic graphs) dispatches to `ArroyueloReachabilityRunner` / `BelyaninReachabilityRunner`, and `simplePath` (path-semiring trace, reachable only via an accepting simple path) dispatches to `ArroyueloSimplePathRunner` / `BelyaninSimplePathRunner`. All four write the same root artifacts and one directory per step (intermediate regexp node for Arroyuelo, BFS iteration for Belyanin); see [Arroyuelo simple-path step visualization](arroyuelo-step-viz.md), [Arroyuelo reachability step visualization](arroyuelo-reachability-step-viz.md), [Belyanin simple-path step visualization](belyanin-step-viz.md), and [Belyanin reachability step visualization](belyanin-reachability-step-viz.md). The `--semantics` flag is ignored by the other algorithms. The RPQ summaries (`-s`) embed the color legend, the query regexp, the query DFA, and the input graph in a shared header, followed by one section per step — two-column for Arroyuelo RPQ, one line per substep (start line; select+automaton and extend+graph per active label; end line) for Belyanin RPQ (see [SummaryTeX module](summary-tex.md)). With the `--summary` (`-s`) flag it also builds a merged TeX document per algorithm, compiles all Dot files to PDF via Graphviz and the merged TeX to PDF via lualatex, replacing the former `run_viz.py` script. The `--use-dot` flag switches LR automaton rendering from the default Tikz back to Graphviz dot. In Tikz mode the GLL and RNGLR summaries embed the full extended RSM figure (`ext_rsm.tikz.tex`, blocks stacked top-to-bottom) at the head of the document, and the RNGLR summary additionally embeds its LR automaton as an adjustbox-wrapped TikZ figure; in DOT mode the head uses the dot-compiled PDFs instead (`ext_rsm.pdf` for GLL/RNGLR, `lr_automaton.pdf` for RNGLR).

## See Also

- [CLI user documentation](../user/cli.md) — command-line usage
- [FLPQ.Languages](FLPQ.Languages.md) — parsing algorithms available in CLI
- [FLPQ.Printers](FLPQ.Printers.md) — visualization output formats
