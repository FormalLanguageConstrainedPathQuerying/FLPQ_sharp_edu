# CLI Console Application

## Overview

Command-line interface for running parsing algorithms with visualization output.
With the `--summary` (`-s`) flag, the CLI additionally compiles all Dot and TeX
artifacts via Graphviz and lualatex, merges them into a single TeX document per
algorithm, and produces a final visualization PDF. This replaces the former
`run_viz.py` script.

## Types

- `Algorithm` — DU: `CYK | Valiant | LL | LR0 | SLR1 | CLR1`
- `Arguments` — Argu argument type with `IArgParserTemplate`

## Command-line flags

| Flag | Description | Default |
|------|-------------|---------|
| `-a` / `--algorithm` | Parsing algorithm | (required) |
| `-g` / `--grammar` | Grammar file (.bnf) | (required) |
| `-i` / `--input` | Input string file | (required) |
| `-o` / `--output` | Output directory | `output` |
| `-k` / `--lookahead` | LL(k) lookahead | 1 |
| `-s` / `--summary` | Build merged TeX summary document | off |
| `--use-dot` | Use Graphviz dot for LR automaton rendering (default: Tikz) | off |

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
| **LR** | `grammar_original.tex`, `lr_table.tex`, `lr_automaton.tikz.tex` (default, Tikz standalone) or `lr_automaton.dot` (with `--use-dot`) |
| **GLL** | `grammar_original.tex`, `grammar_ebnf.tex`, `input.tex`, `rsm_blocks.dot`, `ext_rsm.dot`, `path_index.tex`, `sppf.dot` |
| **RNGLR** | `grammar_original.tex`, `grammar_ebnf.tex`, `input.tex`, `rsm_blocks.dot`, `ext_rsm.dot`, `rnglr_table.tex`, `path_index.tex`, `sppf.dot` |

DOT files are rendered via Graphviz (dashed edges for stack chain, green fill for start states, double circle for final states). TeX files use `pNiceMatrix` from the `nicematrix` package and must be placed in math mode (`\[...\]`) to compile.

### Summary (`--summary`)

When `-s` is passed, after writing the step artifacts the CLI also:

1. Compiles every `*.dot` file (per-step + `lr_automaton.dot` for LR `--use-dot` mode) to PDF via Graphviz.
2. For LR in default Tikz mode: compiles `lr_automaton.tikz.tex` (standalone Tikz document) to PDF via lualatex, producing `dot_pdfs/lr_automaton.pdf` (same path as dot mode).
3. Builds a merged TeX document per algorithm by substituting `__ALGORITHM__` and `__CONTENT__` in `data/tex_summary_template.tex`.
4. Compiles the merged TeX **twice** with lualatex (for table-of-contents and cross-references).
5. Fails with exit code 1 if any Dot or TeX compilation produces errors (exit code, stdout markers, or empty PDF).

Layout under `<output>/results/<algorithm-lower>/`:

```
<output>/results/cyk/
  dot_pdfs/                         # Per-step Dot→PDF files (LL/LR)
  cyk_merged.tex                    # Merged TeX source
  merged_tex_build/                 # lualatex working directory (aux/log/pdf)
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
- LaTeX (`lualatex`) — required for `--summary`

## Design decisions

- LR algorithm variants (LR0, SLR1, CLR1) are all served by `LRRunner`.
- The specific variant name (e.g., "SLR(1)") is included in the merged summary via `AlgorithmTypes.displayName`.
- LR automaton is rendered as Tikz by default (standalone document compiled to PDF). The `--use-dot` flag switches to Graphviz dot.
- TeX step files contain only visualization code (no document headers).
- Grammar file reading reuses `Grammar.parseGrammarFromFile`.
- Summary uses `landscape` layout to accommodate wide matrices without overflow.
- `lualatex` is run twice for correct table-of-contents and cross-references.
- External tool invocations (Dot, lualatex) are wrapped by `FLPQ.Printers.ExternalTools`, shared between the CLI and the test suite.
