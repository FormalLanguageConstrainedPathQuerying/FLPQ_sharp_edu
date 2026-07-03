# FLPQ.Printers

Te x and Dot printing/visualization library for the FLPQ project.

## Overview

FLPQ.Printers centralizes all output formatting logic, separating it from algorithm implementations.
Algorithms collect data as F# data structures; printers convert that data to TeX (for matrix and table rendering)
or Dot (for graph/tree visualization).

## Dependencies

- `FLPQ.LinearAlgebra` — Matrix types and operations
- `FLPQ.Languages` — Grammar, parsing, and automaton types

## Modules

| Module | Description |
|--------|-------------|
| [SymbolTeX](symbol-tex.md) | Unified TeX rendering for grammar symbols |
| [ParsingTableTeX](parsing-table-tex.md) | Common TeX rendering for parsing algorithm tables (CYK, Valiant) |
| [MatrixTeX](matrix-tex.md) | TeX rendering for matrices using nicematrix |
| [TeXRenderer](tex-renderer.md) | Shared TeX rendering for parser stacks and input |
| [GrammarTeX](grammar-tex.md) | TeX rendering for grammar rules |
| [DerivationTreeDot](derivation-tree-dot.md) | Dot rendering for derivation trees |
| [AutomatonDot](automaton-dot.md) | Dot rendering for finite automata |
| [CykTeX](cyk-tex.md) | TeX rendering for CYK algorithm tables |
| [ValiantTeX](valiant-tex.md) | TeX rendering for Valiant trace steps |
| [LLTableTeX](ll-table-tex.md) | TeX rendering for LL parsing tables |
| [LRTableTeX](lr-table-tex.md) | TeX rendering for LR parsing tables |
| [LLStepVisualizer](ll-step-visualizer.md) | LL parser step-by-step visualization |
| [LRStepVisualizer](lr-step-visualizer.md) | LR parser step-by-step visualization |
| [ExternalTools](external-tools.md) | Graphviz and pdflatex wrappers (shared by CLI and tests) |

## Design

The printer library follows the data-then-print pattern:
1. Algorithms produce structured F# data (trace steps, tables, trees, automata)
2. Priners consume that data and produce TeX or Dot strings
3. The CLI or tests write those strings to files

This ensures a clean separation: algorithms never contain formatting logic, and printers never contain algorithm logic.
