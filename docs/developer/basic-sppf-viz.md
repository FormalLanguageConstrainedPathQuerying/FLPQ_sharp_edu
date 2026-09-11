# BasicSPPF Visualization (Dot and Tikz)

**Tags:** visualization, sppf, dot, tikz, graphviz, cyk, valiant, derivation-tree
**Kind:** visualization
**Module:** BasicSppfDot, BasicSppfTikz
**Source:** `src/FLPQ.Printers/BasicSppfDot.fs`, `src/FLPQ.Printers/BasicSppfTikz.fs`
**Depends on:** BasicSppf, AutomatonTikz
**Used by:** CykRunner, ValiantRunner

> **Abstract:** Renders the basic (Rekers-style) Shared Packed Parse Forest produced by CYK and Valiant. `BasicSppfDot` emits a Graphviz DOT digraph; `BasicSppfTikz` emits a TikZ `tikzpicture` using graphdrawing with a top-down layered layout. The two renderers share the same node semantics but differ in label formatting: the Tikz renderer uses LaTeX math mode for nonterminal and terminal labels, while the DOT renderer uses plain text.

## Contents

- [Overview](#overview)
- [Supported Formats](#supported-formats)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The basic SPPF is a graph of four node kinds (see `BasicSppf`):

- **Nonterminal** node — a nonterminal spanning a range `[l, r]`.
- **Terminal** node — a terminal matched at positions `(l, r)`.
- **Epsilon** node — an empty derivation at position `p`.
- **Production** node — a grammar production (split point `k`, rule index) combining children.

Both renderers walk the SPPF graph, emit one node per vertex (highlighting the root),
then emit one edge per transition. Node identity is the vertex index (`n0`, `n1`, ...).

## Supported Formats

### DOT

`digraph BasicSPPF { ... }` with `rankdir=TB`. Node shapes by kind: nonterminal
`rectangle`, terminal `circle`, epsilon `circle`, production `oval`. The root node
gets `style=filled, fillcolor=lightgreen`. Labels are plain text, e.g. `S [0,2]`,
`a_{0,1}`.

### Tikz

A `tikzpicture` using `\graph [layered layout, nodes={draw}, grow'=down, ...]`.
The root node gets `fill=green!30`. Labels use LaTeX math mode for nonterminal and
terminal names:

- Nonterminal `N_1` spanning `[0,2]` → `$N_1$ [0,2]`
- Terminal `a` at `(0,1)` → `$a_{0,1}$`

The `[l,r]` span of a nonterminal stays outside math mode as literal text. Epsilon
and production labels are not math-mode and keep their plain rendering.

## Function Signatures

- `BasicSppfDot.toDot: (terminalPrinter: 't -> string) -> (nonterminalPrinter: 'nt -> string) -> BasicSPPF<'t, 'nt> -> string`
- `BasicSppfTikz.toTikz: (terminalPrinter: 't -> string) -> (nonterminalPrinter: 'nt -> string) -> BasicSPPF<'t, 'nt> -> string`

Both take a terminal printer and a nonterminal printer to convert symbol values to
display strings, and return a complete standalone rendering snippet.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Math mode for nonterminal/terminal labels in Tikz only | CNF nonterminal names (`N_1`) and terminal subscripts (`a_{l,r}`) render correctly as math; DOT is plain text, so it keeps literal labels |
| Nonterminal span `[l,r]` outside math mode | The span is plain positional metadata, not a math expression |
| Epsilon/production labels left unescaped-math | They are not part of the math-mode convention; existing plain rendering is preserved |

## See Also

- [CYK parsing](cyk.md) — CYK produces the parsing table feeding `BasicSppf.fromParsingTable`
- [Valiant parsing](valiant.md) — Valiant produces the same table
- [SPPF Parsing Table types](sppf-parsing-table.md) — enriched table format
- [Automaton visualization](automaton-viz.md) — the `AutomatonTikz.escapeLatex` helper reused here
