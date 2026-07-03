# CLI Console Application

## Overview

Command-line interface for running parsing algorithms with visualization output.
With the `--summary` (`-s`) flag, the CLI additionally compiles all Dot and TeX
artifacts via Graphviz and pdflatex, merges them into a single TeX document per
algorithm, and produces a final visualization PDF. This replaces the former
`run_viz.py` script.

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
| `-s` / `--summary` | Build summary PDF (compiles Dot via Graphviz and TeX via pdflatex) | off |

## Output structure

### Step artifacts

Each algorithm writes step subdirectories (`step_0/`, `step_1/`, ...):

| Algorithm | Files per step | Description |
|-----------|---------------|-------------|
| **CYK** | `table.tex` | `pNiceMatrix` table with optional yellow cell highlights for newly-populated cells |
| **Valiant** | `table.tex` (+ `bool_decomp_*.tex` on last step) | `pNiceMatrix` table with cell printer rendering sets (empty sets as `\cdot`) |
| **LL** | `tree_and_stack.dot`, `input.tex` | DOT graph of derivation tree with stack overlay; TeX input row with current position underlined |
| **LR** | `tree_and_stack.dot`, `input.tex` | Same format as LL — DOT graph includes LR state frames in the stack chain |

Root-level artifacts per algorithm:

| Algorithm | Files |
|-----------|-------|
| **CYK** | `grammar_original.tex`, `grammar_cnf.tex`, `input.tex` |
| **Valiant** | `grammar_original.tex`, `grammar_cnf.tex`, `input.tex` |
| **LL** | `grammar_original.tex`, `ll_table.tex` |
| **LR** | `grammar_original.tex`, `lr_table.tex`, `lr_automaton.dot` |

DOT files are rendered via Graphviz (dashed edges for stack chain, green fill for start states, double circle for final states). TeX files use `pNiceMatrix` from the `nicematrix` package and must be placed in math mode (`\[...\]`) to compile.

### Summary (`--summary`)

When `-s` is passed, after writing the step artifacts the CLI also:

1. Compiles every `*.dot` file (per-step + `lr_automaton.dot` for LR) to PDF via Graphviz.
2. Builds a merged TeX document per algorithm by substituting `__ALGORITHM__` and `__CONTENT__` in `data/tex_summary_template.tex`.
3. Compiles the merged TeX **twice** with pdflatex (for table-of-contents and cross-references).
4. Fails with exit code 1 if any Dot or TeX compilation produces errors (exit code, stdout markers, or empty PDF).

Layout under `<output>/results/<algorithm-lower>/`:

```
<output>/results/cyk/
  dot_pdfs/                         # Per-step Dot→PDF files (LL/LR)
  cyk_merged.tex                    # Merged TeX source
  merged_tex_build/                 # pdflatex working directory (aux/log/pdf)
  cyk_visualization.pdf             # Final compiled PDF
```

## Example usage

Grammars and inputs are located in the `data/` folder:

```bash
# Run CYK and build the summary PDF
dotnet run --project src/FLPQ.Cli -c Release -- \
    -a CYK -g data/example_grammar.bnf -i data/example_input.txt -o ./viz_output -s

# Same for the other algorithms: Valiant, LL, LR
dotnet run --project src/FLPQ.Cli -c Release -- \
    -a LR -g data/example_grammar.bnf -i data/example_input.txt -o ./viz_output -s
```

## Requirements

- .NET 10.0 SDK
- Graphviz (`dot` command) — required for `--summary`
- LaTeX (`pdflatex`) — required for `--summary`

## Design decisions

- LR mode uses SLR(1) table.
- TeX step files contain only visualization code (no document headers).
- Grammar file reading reuses `Grammar.parseGrammarFromFile`.
- Summary uses `landscape` layout to accommodate wide matrices without overflow.
- `pdflatex` is run twice for correct table-of-contents and cross-references.
- DOT files are compiled to PDFs separately and included as `\includegraphics` in the merged TeX (not converted to TikZ).
- External tool invocations (Dot, pdflatex) are wrapped by `FLPQ.Printers.ExternalTools`, shared between the CLI and the test suite.
