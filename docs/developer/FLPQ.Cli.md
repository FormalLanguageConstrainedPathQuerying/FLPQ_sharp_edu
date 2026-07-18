# FLPQ.Cli

**Tags:** cli, console, runner, cyk, valiant, ll, lr, gll, rnglr, rpq, summary, tex, dot
**Kind:** hub
**Source:** `src/FLPQ.Cli/`
**Depends on:** FLPQ.Languages, FLPQ.Printers, Argu
**Used by:** _(none — top-level application)_
**Book reference:** _(application — no direct book reference)_

> **Abstract:** Console application for running parsing algorithms (CYK, Valiant standard/modified, LL(k), LR(0)/SLR(1)/CLR(1), GLL, RNGLR) with file-based I/O and step-by-step visualization output to TeX/Dot/Tikz. Supports merged summary PDF generation via the `--summary` flag, compiling all outputs through Graphviz and lualatex. Uses Argu for argument parsing.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Role](#role)
- [See Also](#see-also)

## Project

- **Type**: F# console application (`net10.0`)
- **Path**: `src/FLPQ.Cli/`
- **Dependencies**: `FLPQ.Languages`, `FLPQ.Printers`, Argu

## Modules

| Module | Source | Documentation |
|--------|--------|---------------|
| `Program` | `Program.fs` | [CLI console application](../user/cli.md) |

## Role

Command-line interface using Argu for argument parsing. Allows running CYK, Valiant, LL, LR0, SLR1, CLR1 algorithms with file-based I/O and step-by-step visualization output. With the `--summary` (`-s`) flag it also builds a merged TeX document per algorithm, compiles all Dot files to PDF via Graphviz and the merged TeX to PDF via lualatex, replacing the former `run_viz.py` script. The `--use-dot` flag switches LR automaton rendering from the default Tikz back to Graphviz dot.

## See Also

- [CLI user documentation](../user/cli.md) — command-line usage
- [FLPQ.Languages](FLPQ.Languages.md) — parsing algorithms available in CLI
- [FLPQ.Printers](FLPQ.Printers.md) — visualization output formats
