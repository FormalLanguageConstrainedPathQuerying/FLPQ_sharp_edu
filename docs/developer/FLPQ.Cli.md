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
| `ArroyueloRunner` | `ArroyueloRunner.fs` | [Arroyuelo step visualization](arroyuelo-step-viz.md) — writes the root artifacts (regexp, DFA, graph, result) and one directory per trace step |
| `BelyaninRunner` | `BelyaninRunner.fs` | [Belyanin step visualization](belyanin-step-viz.md) — writes the root artifacts (regexp, DFA, graph, result) and one directory per BFS iteration |

## Role

Command-line interface using Argu for argument parsing. Allows running CYK, Valiant, LL, LR0, SLR1, CLR1, GLL, RNGLR algorithms with file-based I/O and step-by-step visualization output. RPQ algorithms take a regexp file (`-r`) and a graph file (`--graph`) instead of grammar/input; `ArroyueloRunner` evaluates the query over the path semiring with a trace and writes the root artifacts plus one directory per regexp tree node (see [Arroyuelo step visualization](arroyuelo-step-viz.md)), and `BelyaninRunner` runs Belyanin's BFS-like traversal with a trace, writing the same root artifacts plus one directory per BFS iteration (see [Belyanin step visualization](belyanin-step-viz.md)). The RPQ summaries (`-s`) embed the color legend, the query regexp, the query DFA, and the input graph in a shared header, followed by one two-column section per step (see [SummaryTeX module](summary-tex.md)). With the `--summary` (`-s`) flag it also builds a merged TeX document per algorithm, compiles all Dot files to PDF via Graphviz and the merged TeX to PDF via lualatex, replacing the former `run_viz.py` script. The `--use-dot` flag switches LR automaton rendering from the default Tikz back to Graphviz dot. In Tikz mode the GLL and RNGLR summaries embed the full extended RSM figure (`ext_rsm.tikz.tex`, blocks stacked top-to-bottom) at the head of the document, and the RNGLR summary additionally embeds its LR automaton as an adjustbox-wrapped TikZ figure; in DOT mode the head uses the dot-compiled PDFs instead (`ext_rsm.pdf` for GLL/RNGLR, `lr_automaton.pdf` for RNGLR).

## See Also

- [CLI user documentation](../user/cli.md) — command-line usage
- [FLPQ.Languages](FLPQ.Languages.md) — parsing algorithms available in CLI
- [FLPQ.Printers](FLPQ.Printers.md) — visualization output formats
