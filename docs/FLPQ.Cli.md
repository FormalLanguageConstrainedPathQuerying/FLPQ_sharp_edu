# FLPQ.Cli

Console application for running parsing algorithms with visualization output, including optional summary PDF generation. Depends on `FLPQ.Languages` and `FLPQ.Printers`.

## Project

- **Type**: F# console application (`net10.0`)
- **Path**: `src/FLPQ.Cli/`
- **Dependencies**: `FLPQ.Languages`, `FLPQ.Printers`, Argu

## Documentation

| Module | Source | Documentation |
|--------|--------|---------------|
| `Program` | `Program.fs` | [CLI console application](cli.md) |

## Role

Command-line interface using Argu for argument parsing. Allows running CYK, Valiant, LL, LR algorithms with file-based I/O and step-by-step visualization output. With the `--summary` (`-s`) flag it also builds a merged TeX document per algorithm, compiles all Dot files to PDF via Graphviz and the merged TeX to PDF via lualatex, replacing the former `run_viz.py` script.
