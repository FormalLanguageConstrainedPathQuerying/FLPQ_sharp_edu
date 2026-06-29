# CLI Console Application

## Overview

Command-line interface for running parsing algorithms with visualization output.

## Types

- `Algorithm` — DU: `CYK | Valiant | LL | LR`
- `Arguments` — Argu argument type with `IArgParserTemplate`

## Command-line flags

| Flag | Description | Default |
|------|-------------|---------|
| `-a` / `--algorithm` | Parsing algorithm | (required) |
| `-g` / `--grammar` | Grammar file (.bnf) | (required) |
| `-i` / `--input` | Input string file | (required) |
| `-o` / `--output` | Output directory | `output` |
| `-k` / `--lookahead` | LL(k) lookahead | 1 |

## Output structure

Each algorithm writes step subdirectories (`step_0/`, `step_1/`, ...):
- **CYK**: `table.tex`
- **Valiant**: `table.tex`
- **LL**: `tree.dot`, `stack.tex`, `input.tex`
- **LR**: `tree.dot`, `stack.tex`, `input.tex`

## Design decisions

- LR mode uses SLR(1) table
- TeX files contain only visualization code (no document headers)
- Grammar file reading reuses `Grammar.parseGrammarFromFile`
