# FLPQ.Cli

Console application for running parsing algorithms with visualization output. Depends on `FLPQ.LinearAlgebra` and `FLPQ.Languages`.

## Project

- **Type**: F# console application (`net10.0`)
- **Path**: `src/FLPQ.Cli/`
- **Dependencies**: `FLPQ.LinearAlgebra`, `FLPQ.Languages`, Argu

## Documentation

| Module | Source | Documentation |
|--------|--------|---------------|
| `Program` | `Program.fs` | [CLI console application](cli.md) |

## Role

Command-line interface using Argu for argument parsing. Allows running CYK, Valiant, LL, LR algorithms with file-based I/O and step-by-step visualization output.
