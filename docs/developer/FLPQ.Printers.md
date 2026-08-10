# FLPQ.Printers

**Tags:** visualization, tex, dot, tikz, rendering, matrix, grammar, automaton, derivation-tree, ll, lr, cyk, valiant, summary
**Kind:** hub
**Source:** `src/FLPQ.Printers/`
**Depends on:** FLPQ.LinearAlgebra, FLPQ.Languages
**Used by:** FLPQ.Cli
**Book reference:** _(rendering — no direct book reference)_

> **Abstract:** TeX and Dot printing/visualization library centralizing all output formatting logic. Follows the data-then-print pattern: algorithms produce structured F# data (trace steps, tables, trees, automata), printers consume that data and produce TeX or Dot strings, and the CLI writes those strings to files. Separates rendering from algorithm logic — algorithms never contain formatting code, printers never contain algorithm logic.

## Contents

- [Project](#project)
- [Modules](#modules)
- [Design](#design)
- [See Also](#see-also)

## Project

- **Type**: F# class library (`net10.0`)
- **Path**: `src/FLPQ.Printers/`
- **Dependencies**: `FLPQ.LinearAlgebra`, `FLPQ.Languages`

## Modules

| Module | Description |
|--------|-------------|
| [SymbolTeX](symbol-tex.md) | Unified TeX rendering for grammar symbols |
| [ParsingTableTeX](parsing-table-tex.md) | Common TeX rendering for parsing algorithm tables (CYK, Valiant) |
| [MatrixTeX](matrix-tex.md) | TeX rendering for matrices using nicematrix |
| [TeXRenderer](tex-renderer.md) | Shared TeX rendering for parser stacks and input |
| [GrammarTeX](grammar-tex.md) | TeX rendering for grammar rules |
| [DerivationTreeDot](derivation-tree-dot.md) | Dot rendering for derivation trees |
| [AutomatonDot / AutomatonTikz / LRAutomatonTikz](automaton-viz.md) | Dot and Tikz rendering for finite automata; specialized Tikz renderer for LR automata |
| [BasicSppfDot / BasicSppfTikz](basic-sppf-viz.md) | Dot and Tikz rendering for basic (Rekers-style) SPPF |
| [InputGraphDot](input-graph-dot.md) | Dot rendering for the GLL input graph with input position highlighting |
| [InputGraphDot](input-graph-dot.md) | Dot rendering for the GLL input graph with input position highlighting |
| [CykTeX](cyk-tex.md) | TeX rendering for CYK algorithm tables |
| [ValiantTeX](valiant-tex.md) | TeX rendering for Valiant trace steps |
| [LLTableTeX](ll-table-tex.md) | TeX rendering for LL parsing tables |
| [LRTableTeX](lr-table-tex.md) | TeX rendering for LR parsing tables |
| [LLStepVisualizer](ll-step-visualizer.md) | LL parser step-by-step visualization |
| [LRStepVisualizer](lr-step-visualizer.md) | LR parser step-by-step visualization |
| [ExternalTools](external-tools.md) | Graphviz and lualatex wrappers (shared by CLI and tests) |
| [SummaryTeX](summary-tex.md) | TeX content generation for merged summary documents |

## Design

The printer library follows the data-then-print pattern:
1. Algorithms produce structured F# data (trace steps, tables, trees, automata)
2. Printers consume that data and produce TeX or Dot strings
3. The CLI or tests write those strings to files

This ensures a clean separation: algorithms never contain formatting logic, and printers never contain algorithm logic.

## See Also

- [FLPQ.Languages](FLPQ.Languages.md) — source data types (grammars, automata, trees)
- [FLPQ.Cli](FLPQ.Cli.md) — CLI application that invokes printers
- [Automaton visualization](automaton-viz.md) — Dot and Tikz for automata
- [SummaryTeX module](summary-tex.md) — merged TeX document generation
