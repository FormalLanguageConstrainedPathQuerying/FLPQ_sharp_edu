# CfpqInput Module

**Tags:** cfpq, context-free, parsing, matrix-market, cnf, utility
**Kind:** utility
**Module:** CfpqInput
**Source:** `src/FLPQ.CFPQ/CfpqInput.fs`
**Depends on:** FLPQ.Languages (Grammar, NFA), FLPQ.LinearAlgebra (Matrix)
**Used by:** AzimovCfpq, CFPQ CLI runners (S7)
**Book reference:** Chapter 12, Section sec:CFPQ_MatrixBased

> **Abstract:** Parses CFPQ inputs: labeled directed graphs in the Boolean MatrixMarket format (one file per edge label), context-free grammars in the plain `.cnf` template format, and reference result matrices. The formats are those produced by the pinned `flpq-data` package (see [tools](guides/tools.md)).

## Contents

- [Purpose](#purpose)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Purpose

The Azimov CFPQ algorithm takes a labeled directed graph and a context-free grammar. This module turns the on-disk `flpq-data` export into in-memory values:

1. A graph directory of `*.mtx` files (one Boolean coordinate-pattern MatrixMarket file per edge label) becomes a `CfpqGraph`: the vertex count plus one adjacency matrix per label.
2. A plain `.cnf` grammar file becomes a `Grammar<string, string>`: one production per line, the left-hand side first; a bare left-hand side is an epsilon rule; the last two non-blank lines are `Count:` and the start symbol.
3. A reference `results.mtx` file becomes a `Matrix<bool>`.

## Function Signatures

```fsharp
type CfpqGraph<'t> when 't: comparison = { VertexCount: int; Labels: Map<'t, Matrix<bool>> }

val parseMatrixMarketText: string -> Matrix<bool>
val parseMatrixMarketFile: string -> Matrix<bool>
val parseGraphDir: string -> CfpqGraph<string>
val parseCnfText: string -> Grammar<string, string>
val parseCnfFile: string -> Grammar<string, string>
val parseResultsFile: string -> Matrix<bool>
val toNfa: CfpqGraph<string> -> NFA<string, int>
```

- `parseMatrixMarketText` — parses the flpq-data Boolean format: a `%%MatrixMarket matrix coordinate pattern general` header, optional `%%` comment lines, a `rows cols nnz` line, then exactly `nnz` 0-based `i j` entry lines. Returns the dense Boolean matrix with `true` in the occupied cells (duplicates collapse).
- `parseGraphDir` — every `*.mtx` file in the directory becomes the adjacency matrix of its label (file name without extension); all files must agree on dimensions. Labels are strings, so the result is a `CfpqGraph<string>`.
- `parseCnfText` — parses a plain `.cnf` grammar. A symbol is a nonterminal iff it is the left-hand side of some production; all other symbols are terminals. The start symbol must be a nonterminal.
- `parseResultsFile` — parses a reference result matrix (a Boolean MatrixMarket file).
- `toNfa` — views a string-labeled graph as an NFA: one transition per labeled edge, no start or final states. This is the adapter that lets the CFPQ runners reuse the NFA-based RPQ graph visualizer (`RpqGraphViz.renderGraph`) for the input-graph figure.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| One MatrixMarket parser shared by graphs and results | Graph adjacency files and reference `results.mtx` files use the identical flpq-data format; one parser is the single source of truth |
| Label = file name without extension | The export tool writes `<label>.mtx` per edge label; no separate label manifest is needed |
| Nonterminal = production left-hand side | The `.cnf` format has no explicit nonterminal/terminal declaration; the left-hand-side convention matches how flpq-data templates are written |
| Bare left-hand side = epsilon rule | flpq-data encodes `A -> eps` as a line containing only `A` |
| Indexed placeholders (`_i` suffix) rejected | Templates like `load_i` are not grammars until materialized over a concrete graph; materialization is done in Python by `tools/flpq_data/export.py`, so the F# parser only accepts plain grammars and fails fast on unmaterialized templates |
| All errors are `ArgumentException` with a descriptive message | Input parsing is a utility layer; callers (CLI, tests) distinguish bad input from algorithmic failure by exception type |

## See Also

- [AzimovCfpq module](azimov-cfpq.md) — the algorithm that consumes the parsed graph and grammar
- [CfpqSemiring module](cfpq-semiring.md) — the semirings the algorithm is parameterized by
- [FLPQ.CFPQ hub](FLPQ.CFPQ.md) — project overview
- [Tools guide](guides/tools.md) — the flpq-data export tool
- [Grammar module](grammar.md) — the `Grammar` type
