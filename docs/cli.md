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

| Algorithm | Files per step | Description |
|-----------|---------------|-------------|
| **CYK** | `table.tex` | `pNiceMatrix` table with optional yellow cell highlights for newly-populated cells |
| **Valiant** | `table.tex` | `pNiceMatrix` table with cell printer rendering sets (empty sets as `\cdot`) |
| **LL** | `tree_and_stack.dot`, `input.tex` | DOT graph of derivation tree with stack overlay; TeX input row with current position underlined |
| **LR** | `tree_and_stack.dot`, `input.tex` | Same format as LL — DOT graph includes LR state frames in the stack chain |

DOT files are rendered via Graphviz (dashed edges for stack chain, green fill for start states, double circle for final states). TeX files use `pNiceMatrix` from the `nicematrix` package and must be placed in math mode (`\[...\]`) to compile.

## Batch Visualization Script (`run_viz.py`)

Located at the repository root. Runs specified algorithms via the CLI, compiles all DOT files to PDFs, and merges all TeX fragments into a single compilable document with step-by-step visualization.

### Usage

```bash
# Run example: CYK + Valiant on grammar S->SS|eps|aSb with input aababb
python3 run_viz.py --example

# Run specific algorithms with custom grammar and input
python3 run_viz.py --algorithms CYK Valiant --grammar my_grammar.bnf --input my_input.txt --output ./my_output
```

### Output

```
<output_dir>/results/
  cyk/
    dot_pdfs/          # Per-step DOT→PDF files (if applicable)
    cyk_merged.tex     # Combined TeX source
    cyk_visualization.pdf  # Final compiled PDF
  valiant/
    ...
```

### Requirements

- Python 3.7+
- .NET 10.0 SDK (to build and run the CLI)
- Graphviz (`dot` command) for DOT→PDF conversion
- LaTeX (`pdflatex`) for TeX→PDF compilation

## Design decisions

- LR mode uses SLR(1) table
- TeX files contain only visualization code (no document headers)
- Grammar file reading reuses `Grammar.parseGrammarFromFile`
- Batch script uses `landscape` layout to accommodate wide matrices without overflow
- `pdflatex` is run twice for correct table-of-contents and cross-references
- DOT files are compiled to PDFs separately and included as `\includegraphics` in the merged TeX (not converted to TikZ)
