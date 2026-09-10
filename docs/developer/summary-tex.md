# SummaryTeX Module

**Tags:** visualization, tex, summary, rendering, latex, cli, pdf
**Kind:** visualization
**Module:** SummaryTeX
**Source:** `src/FLPQ.Printers/SummaryTeX.fs`
**Depends on:** GrammarTeX, MatrixTeX, ExternalTools
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Generates TeX content for merged summary documents produced by the CLI. Provides LaTeX helper functions (`wrapMath`, `wrapCenter`, `wrapTikzCenter`, `wrapTikzAdjustbox`, `includePdf`, `section`) and structured section builders (`headerSection`, `tableStepSection`, `stackStepSection`, `buildContent`) that assemble per-step artifacts (tables, stacks, dot-generated PDFs, Tikz diagrams) into a single compilable TeX document. Operates purely on string content — file I/O is handled by the CLI.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The SummaryTeX module assembles per-algorithm visualization artifacts into one merged TeX document per algorithm. The merged document includes:
- Algorithm header (original grammar, CNF grammar, input string)
- LL/LR parsing table and automaton (for LL/LR algorithms)
- Per-step sections (tables for CYK/Valiant; stack+tree picture for LL/LR — inline TikZ in default mode, dot-compiled PDF with `--use-dot`)
- Color legend (for GLL/RNGLR) — a tabular mapping of each highlight color to its meaning. The GLL legend includes an `orange!30` row for "Stored pops handling triggered at GSS vertex" (see [GLL module](gll.md)); the RNGLR legend includes an `orange!30` row for "Passing reductions handling triggered at GSS vertex" (see [RNGLR module](rnglr.md)).
- Extended RSM figure at the head (for GLL/RNGLR in Tikz mode) — the full extended RSM from `ext_rsm.tikz.tex` (blocks stacked top-to-bottom, see [RSM visualization](rsm-viz.md)), wrapped in adjustbox. In DOT mode the head uses the dot-compiled PDFs instead (`ext_rsm.pdf` for GLL, `rsm_blocks.pdf` for RNGLR).

## Function Signatures

### LaTeX Helpers
```fsharp
val wrapMath: string -> string
val wrapCenter: string -> string
val wrapTikzCenter: string -> string
val wrapTikzAdjustbox: string -> string
val includePdf: string -> string
val section: string -> string
```

### Section Builders
```fsharp
val headerSection: vizDir:string -> algoKind:SummaryKind -> lrAutomatonPdf:string option -> lrAutomatonTikz:string option -> rsmSppfPdfs:(string*string) list -> useTikz:bool -> string list
val tableStepSection: stepDir:string -> stepNum:int -> string list
val stackStepSection: stepDir:string -> stepNum:int -> stepName:string -> useTikz:bool -> string list
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
| `wrapTikzCenter` with resizebox | Ensures Tikz diagrams fit page width in merged summary (LR automaton, GLL input string, SPPF) |
| `wrapTikzAdjustbox` for LL/LR step figures | `\begin{adjustbox}{max width=\textwidth}` shrinks over-wide figures but never upscales small ones, preserving natural size and font metrics; resizebox would scale node text along with the picture. Used only by `stackStepSection`; other TikZ inclusions keep resizebox (GLL-wide migration is task 244) |
| `stackStepSection` per-mode picture | Default mode embeds the step's `tree_and_stack.tikz.tex` inline (same-layer stack frontier), wrapped in adjustbox; `--use-dot` includes the dot-compiled PDF. Mirrors the GLL/RNGLR `useTikz` switch |
| Extended RSM head section for GLL/RNGLR Tikz mode | `headerSection` reads `ext_rsm.tikz.tex` from the viz dir and wraps it in `wrapTikzAdjustbox` (tall stacked figures keep natural size); skipped gracefully when the file is absent so older viz dirs still build. DOT mode keeps the dot-compiled PDF entries |

## See Also

- [GrammarTeX module](grammar-tex.md) — grammar rendering used in headers
- [ExternalTools module](external-tools.md) — Dot/TeX compilation for summary PDFs
- [CLI user documentation](../user/cli.md) — `--summary` flag usage
