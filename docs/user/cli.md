# CLI Console Application

## Overview

Command-line interface for running parsing algorithms with visualization output.
With the `--summary` (`-s`) flag, the CLI additionally compiles all Dot and TeX
artifacts via Graphviz and lualatex, merges them into a single TeX document per
algorithm, and produces a final visualization PDF. This replaces the former
`run_viz.py` script.

## Types

- `Algorithm` — DU: `CYK | Valiant | ValiantModified | LL | LR0 | SLR1 | CLR1 | GLL | RNGLR | ArroyueloRPQ | BelyaninRPQ`
- `Arguments` — Argu argument type with `IArgParserTemplate`

## Command-line flags

| Flag | Description | Default |
| --- | --- | --- |
| `-a` / `--algorithm` | Algorithm (all algorithms take the unified `-q`/`-i` query/input pair) | (required) |
| `-q` / `--query` | Query file: grammar (.bnf) for parsing algorithms, regexp (EBNF; the first rule's RHS is the query) for RPQ algorithms | (required) |
| `-i` / `--input` | Input file: input string for parsing algorithms, graph (start vertices line + `from label to` edges) for RPQ algorithms | (required) |
| `-o` / `--output` | Output directory | `output` |
| `-k` / `--lookahead` | LL(k) lookahead | 1 |
| `-s` / `--summary` | Build merged TeX summary document | off |
| `--use-dot` | Use Graphviz dot for LR automaton and LL/LR per-step tree_and_stack rendering (default: Tikz) | off |
| `--no-sppf-table` | Render CYK/Valiant table cells as sets of nonterminal names without SPPF split points and production indices | off |

## Output structure

### Step artifacts

Each algorithm writes step subdirectories (`step_0/`, `step_1/`, ...):

| Algorithm | Files per step | Description |
| --- | --- | --- |
| **CYK** | `table.tex` | `pNiceMatrix` table with optional yellow cell highlights for newly-populated cells |
| **Valiant** | `table.tex` (+ `bool_decomp_*.tex` on last step) | `pNiceMatrix` table with cell printer rendering sets (empty sets as `\cdot`) |
| **LL** | `tree_and_stack.tikz.tex` (default) or `tree_and_stack.dot` (`--use-dot`), `input.tex` | Derivation tree with stack overlay: Tikz graphdrawing picture with a same-layer constraint on the stack frontier, or DOT graph; TeX input row with current position underlined |
| **LR** | `tree_and_stack.tikz.tex` (default) or `tree_and_stack.dot` (`--use-dot`), `input.tex` | Same format as LL — the picture includes LR state frames in the stack chain |
| **Arroyuelo RPQ** | `matrices.tex`, `tree.tikz.tex` (default) or `tree.dot` (`--use-dot`), `graph.tikz.tex` (default) or `graph.dot` (`--use-dot`) | One directory per regexp tree node in post-order: the matrix equation for the node's operation, the regexp tree with the current node highlighted, and the graph with the vertices/edges of the step's result paths highlighted |
| **Belyanin RPQ** | `matrices.tex`, `automaton.tikz.tex` (default) or `automaton.dot` (`--use-dot`), `graph.tikz.tex` (default) or `graph.dot` (`--use-dot`) | One directory per BFS iteration (plus `step_0` for the initialization): the frontier/accumulated matrices with the per-label propagation products, the query DFA with the current frontier states highlighted, and the graph with the vertices/edges of the current frontier paths highlighted |

Root-level artifacts per algorithm:

| Algorithm | Files |
| --- | --- |
| **CYK** | `grammar_original.tex`, `grammar_cnf.tex`, `input.tex` |
| **Valiant** | `grammar_original.tex`, `grammar_cnf.tex`, `input.tex` |
| **LL** | `grammar_original.tex`, `ll_table.tex` |
| **LR** | `grammar_original.tex`, `lr_table.tex`, `lr_automaton.tikz.tex` (default, Tikz standalone) or `lr_automaton.dot` (with `--use-dot`) |
| **GLL** | `grammar_original.tex`, `grammar_ebnf.tex`, `input.tex`, `rsm_blocks.dot`, `ext_rsm.tikz.tex` (default) or `ext_rsm.dot` (`--use-dot`), `path_index.tex`, `sppf.dot` |
| **RNGLR** | `grammar_original.tex`, `grammar_ebnf.tex`, `input.tex`, `ext_rsm.tikz.tex` (default Tikz mode) or `ext_rsm.dot` (`--use-dot`), `lr_automaton.tikz.tex` (default) or `lr_automaton.dot` (`--use-dot`), `rnglr_table.tex`, `path_index.tex`, `sppf.dot` |
| **Arroyuelo RPQ** | `regexp.tex` (query formula), `dfa.tikz.tex` (default) or `dfa.dot` (`--use-dot`) — the query's DFA with `q_i` state labels, `graph.tikz.tex` (default) or `graph.dot` (`--use-dot`) — the input graph without highlights, `result.tex` — final path semiring matrix plus one line per source listing its reachable vertices |
| **Belyanin RPQ** | Same as Arroyuelo RPQ: `regexp.tex`, `dfa.tikz.tex` (default) or `dfa.dot` (`--use-dot`), `graph.tikz.tex` (default) or `graph.dot` (`--use-dot`), `result.tex` — final path semiring matrix plus one line per source listing its reachable vertices |

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
    -a CYK -q data/example_grammar.bnf -i data/example_input.txt -o ./viz_output -s

# Same for the other algorithms: Valiant, LL, LR
dotnet run --project src/FLPQ.Cli -c Release -- \
    -a LR -q data/example_grammar.bnf -i data/example_input.txt -o ./viz_output -s

# RPQ algorithms (Arroyuelo, Belyanin): the query is a regexp and the input is a graph
dotnet run --project src/FLPQ.Cli -c Release -- \
    -a ArroyueloRPQ -q data/example_regexp.txt -i data/example_graph.txt -o ./viz_output

dotnet run --project src/FLPQ.Cli -c Release -- \
    -a BelyaninRPQ -q data/example_regexp.txt -i data/example_graph.txt -o ./viz_output -s
```

## Requirements

- .NET 10.0 SDK
- Graphviz (`dot` command) — required for `--summary`
- LaTeX (`lualatex`) — required for `--summary`

## Design decisions

- LR algorithm variants (LR0, SLR1, CLR1) are all served by `LRRunner`.
- The specific variant name (e.g., "SLR(1)") is included in the merged summary via `AlgorithmTypes.displayName`.
- LR automaton is rendered as Tikz by default (standalone document compiled to PDF). The `--use-dot` flag switches to Graphviz dot.
- LL/LR per-step tree_and_stack pictures are rendered as Tikz by default (graphdrawing layered layout with a same-layer constraint on the stack frontier — the equivalent of DOT's `{rank=same}`). The `--use-dot` flag switches to Graphviz dot. Only one format is written per step.
- TeX step files contain only visualization code (no document headers).
- Tikz output uses `graphdrawing` (`\graph [layered layout, ...]`). When embedding the generated snippets into another document, that document must load `\usetikzlibrary{graphs,graphdrawing}` and `\usegdlibrary{layered}` and be compiled with `lualatex`.
- Grammar file reading reuses `Grammar.parseGrammarFromFile`.
- Summary uses `landscape` layout to accommodate wide matrices without overflow.
- `lualatex` is run twice for correct table-of-contents and cross-references.
- External tool invocations (Dot, lualatex) are wrapped by `FLPQ.Printers.ExternalTools`, shared between the CLI and the test suite.
