# SummaryTeX Module

**Tags:** visualization, tex, summary, rendering, latex, cli, pdf
**Kind:** visualization
**Module:** SummaryTeX
**Source:** `src/FLPQ.Printers/SummaryTeX.fs`
**Depends on:** GrammarTeX, MatrixTeX, ExternalTools
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Generates TeX content for merged summary documents produced by the CLI. Provides LaTeX helper functions (`wrapMath`, `wrapCenter`, `wrapTikzCenter`, `includePdf`, `section`) and structured section builders (`headerSection`, `tableStepSection`, `stackStepSection`, `buildContent`) that assemble per-step artifacts (tables, stacks, dot-generated PDFs, Tikz diagrams) into a single compilable TeX document. Operates purely on string content — file I/O is handled by the CLI.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The SummaryTeX module assembles per-algorithm visualization artifacts into one merged TeX document per algorithm. The merged document includes:
- Algorithm header (original grammar, CNF grammar, input string)
- LL/LR parsing table and automaton (for LL/LR algorithms)
- Per-step sections (tables for CYK/Valiant, stack+tree PDFs for LL/LR)

## Function Signatures

### LaTeX Helpers
```fsharp
val wrapMath: string -> string
val wrapCenter: string -> string
val wrapTikzCenter: string -> string
val includePdf: string -> string
val section: string -> string
```

### Section Builders
```fsharp
val headerSection: vizDir:string -> algoKind:string -> lrAutomatonPdf:string option -> lrAutomatonTikz:string option -> string list
val tableStepSection: stepDir:string -> stepNum:int -> string list
val stackStepSection: stepDir:string -> stepNum:int -> stepName:string -> string list
val buildContent: algo:string -> algoKind:string -> vizDir:string -> stepCount:int -> lrAutomatonPdf:string option -> lrAutomatonTikz:string option -> string list
```
- `algoKind`: `"table"` (CYK/Valiant), `"ll"`, or `"lr"`
- `buildContent` assembles the complete merged TeX for one algorithm

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Separate module in Printers | TeX content generation is formatting logic, not CLI orchestration |
| String-based algo kind | Avoids dependency on CLI-specific `Algorithm` DU; testable independently |
| File I/O via `readIfExists` | Headers/steps read existing artifact files; module produces only string content |
| `\includegraphics` for dot PDFs | References PDFs compiled by CLI via ExternalTools; module assumes they exist |
| `wrapTikzCenter` with resizebox | Ensures Tikz diagrams fit page width in merged summary |

## See Also

- [GrammarTeX module](grammar-tex.md) — grammar rendering used in headers
- [ExternalTools module](external-tools.md) — Dot/TeX compilation for summary PDFs
- [CLI user documentation](../user/cli.md) — `--summary` flag usage
