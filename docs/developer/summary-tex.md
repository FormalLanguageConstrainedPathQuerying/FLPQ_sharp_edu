# SummaryTeX Module

**Tags:** visualization, tex, summary, rendering, latex, cli, pdf
**Kind:** visualization
**Module:** SummaryTeX
**Source:** `src/FLPQ.Printers/SummaryTeX.fs`
**Depends on:** GrammarTeX, MatrixTeX, ExternalTools
**Used by:** FLPQ.Cli (summary generation)

> **Abstract:** Generates TeX content for merged summary documents produced by the CLI. Provides LaTeX helper functions (`wrapMath`, `wrapCenter`, `wrapTikzCenter`, `wrapTikzAdjustbox`, `wrapTikzAdjustboxColumn`, `wrapStepAdjustbox`, `includePdf`, `section`) and structured section builders (`headerSection`, `tableStepSection`, `stackStepSection`, `gllStepSection`, `rnglrStepSection`, `arroyueloStepSection`, `belyaninStepSection`, `buildContent`) that assemble per-step artifacts (tables, stacks, dot-generated PDFs, Tikz diagrams) into a single compilable TeX document. Operates purely on string content — file I/O is handled by the CLI.

## Contents

- [Overview](#overview)
- [Function Signatures](#function-signatures)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Overview

The SummaryTeX module assembles per-algorithm visualization artifacts into one merged TeX document per algorithm. The merged document includes:

- Algorithm header (original grammar, CNF grammar, input string)
- LL/LR parsing table and automaton (for LL/LR algorithms)
- Per-step sections (tables for CYK/Valiant; stack+tree picture for LL/LR — inline TikZ in default mode, dot-compiled PDF with `--use-dot`; side-by-side template layout for GLL/RNGLR where the step's GSS and RSM figures are adjustbox-wrapped in TikZ mode; two-column template layout for Arroyuelo RPQ where each step shows the regexp tree + matrix equation on the left and the highlighted graph on the right, see [Arroyuelo step visualization](arroyuelo-step-viz.md); two-column template layout for Belyanin RPQ where each step shows the query DFA with frontier states highlighted + the matrix stack on the left and the highlighted graph on the right, see [Belyanin step visualization](belyanin-step-viz.md))
- Color legend (for GLL/RNGLR/Arroyuelo RPQ/Belyanin RPQ) — a tabular mapping of each highlight color to its meaning. The GLL legend includes an `orange!30` row for "Stored pops handling triggered at GSS vertex" (see [GLL module](gll.md)); the RNGLR legend includes an `orange!30` row for "Passing reductions handling triggered at GSS vertex" (see [RNGLR module](rnglr.md)); the Arroyuelo RPQ legend maps the current tree node (`lightblue!20`), result-path vertices (`yellow!20`), result-path edges (red), and `\cdot` = empty matrix cell; the Belyanin RPQ legend maps frontier automaton states (`lightblue!20`), frontier-path vertices (`yellow!20`), frontier-path edges (red), and `\cdot` = empty matrix cell.
- Query regexp, query DFA, and input graph sections at the head (for both RPQ algorithms) — one shared header for Arroyuelo and Belyanin: the query formula from `regexp.tex` in math mode, the query DFA and the input graph as inline TikZ wrapped in a width-only adjustbox in Tikz mode or dot-compiled `dot_pdfs/dfa.pdf` / `dot_pdfs/graph.pdf` in DOT mode (see [Arroyuelo RPQ module](arroyuelo-rpq.md) and [Belyanin RPQ module](belyanin-rpq.md)).
- Extended RSM figure at the head (for GLL/RNGLR in Tikz mode) — the full extended RSM from `ext_rsm.tikz.tex` (blocks stacked top-to-bottom, see [RSM visualization](rsm-viz.md)), wrapped in adjustbox. In DOT mode the head uses the dot-compiled `ext_rsm.pdf` instead (for both GLL and RNGLR; the plain RSM figure is no longer part of the RNGLR summary).
- LR automaton section at the head (for RNGLR) — the RNGLR LR automaton (see [RNGLR module](rnglr.md)) rendered by [RnglrAutomatonTikz](automaton-viz.md): inline TikZ wrapped in an adjustbox limited to `\textwidth` and `\textheight` in Tikz mode, dot-compiled `dot_pdfs/lr_automaton.pdf` in DOT mode. Placed between the extended RSM figure and the RNGLR parsing table.
- SPPF section at the end (all algorithms) — exactly one "SPPF (Shared Packed Parse Forest)" subsection per document: inline TikZ from `sppf.tikz.tex` in Tikz mode, dot-compiled `dot_pdfs/sppf.pdf` otherwise. No algorithm includes SPPF in the header.

## Function Signatures

### LaTeX Helpers

```fsharp
val wrapMath: string -> string
val wrapCenter: string -> string
val wrapTikzCenter: string -> string
val wrapTikzAdjustbox: bool -> string -> string
val wrapTikzAdjustboxColumn: string -> string
val wrapStepAdjustbox: string -> string
val includePdf: string -> string
val section: string -> string
```

### Section Builders

```fsharp
type StepTemplates = { Gll:string; GllTikz:string; Rnglr:string; RnglrTikz:string; Arroyuelo:string; ArroyueloTikz:string; Belyanin:string; BelyaninTikz:string }

val headerSection: vizDir:string -> algoKind:SummaryKind -> lrAutomatonPdf:string option -> lrAutomatonTikz:string option -> rsmPdfs:(string*string) list -> useTikz:bool -> string list
val tableStepSection: stepDir:string -> stepNum:int -> wrap:(string->string) -> string list
val stackStepSection: stepDir:string -> stepNum:int -> stepName:string -> useTikz:bool -> string list
val gllStepSection: stepDir:string -> stepNum:int -> template:string -> tikzTemplate:string -> useTikz:bool -> string list
val rnglrStepSection: stepDir:string -> stepNum:int -> template:string -> tikzTemplate:string -> useTikz:bool -> string list
val arroyueloStepSection: stepDir:string -> stepNum:int -> template:string -> tikzTemplate:string -> useTikz:bool -> string list
val belyaninStepSection: stepDir:string -> stepNum:int -> template:string -> tikzTemplate:string -> useTikz:bool -> string list
val sppfSection: vizDir:string -> useTikz:bool -> string list
val buildContent: algo:string -> algoKind:SummaryKind -> vizDir:string -> stepCount:int -> lrAutomatonPdf:string option -> lrAutomatonTikz:string option -> rsmPdfs:(string*string) list -> templates:StepTemplates -> useTikz:bool -> string list
```

- `algoKind`: `TablePerStep` (CYK/Valiant), `LL`, `LR`, `GLL`, `RNGLR`, `ArroyueloRPQ`, or `BelyaninRPQ`
- `StepTemplates` bundles the per-algorithm step templates (DOT and TikZ variants); fields are empty strings for algorithms that do not use a step template.
- `buildContent` assembles the complete merged TeX for one algorithm
- `wrapTikzAdjustbox limitHeight tikz`: wraps `tikz` in a centered adjustbox limited to at most `\textwidth`; when `limitHeight` is true it is additionally limited to at most `\textheight`. Shrink-only — small figures are never upscaled and node font metrics are preserved.
- `wrapTikzAdjustboxColumn tikz`: wraps `tikz` in a centered adjustbox limited to at most `\linewidth` (the current column width). For RPQ step figures, which sit inside minipage columns where `\textwidth` is still the full page width (task 282, S3).
- `wrapStepAdjustbox tex`: wraps a filled RPQ step template in an adjustbox limited to at most `\textwidth` and `0.9\textheight`. The 10% height headroom accommodates the step's `\subsection*` heading, which must stay outside the box — sectioning commands cannot run inside an adjustbox (task 282, S3).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Separate module in Printers | TeX content generation is formatting logic, not CLI orchestration |
| String-based algo kind | Avoids dependency on CLI-specific `Algorithm` DU; testable independently |
| File I/O via `readIfExists` | Headers/steps read existing artifact files; module produces only string content |
| `\includegraphics` for dot PDFs | References PDFs compiled by CLI via ExternalTools; module assumes they exist |
| `wrapTikzCenter` with resizebox | Ensures Tikz diagrams fit page width in merged summary (LR automaton, GLL input string) |
| `wrapTikzAdjustbox` for step figures and SPPF | `\begin{adjustbox}{max width=\textwidth}` shrinks over-wide figures but never upscales small ones, preserving natural size and font metrics; resizebox would scale node text along with the picture. Used by `stackStepSection` (LL/LR stack-trees), by `gllStepSection`/`rnglrStepSection` for the per-step GSS and RSM figures (RSM for GLL only), and by `sppfSection` for the SPPF — the latter passes `limitHeight = true`, adding `max totalheight=\textheight` so tall SPPF forests also fit the page height. Note: a minipage does **not** change `\textwidth` (it stays the full page width), so this wrap does not confine a figure to its step column — the RPQ steps therefore use `wrapTikzAdjustboxColumn` instead (task 282, S3); the GLL/RNGLR wraps are kept as-is per user guidance. Remaining resizebox inclusions: LR automaton, GLL input string (GLL-wide migration is task 244) |
| Mode-aware extended RSM head for RNGLR | The RNGLR head shows the extended RSM in both modes — `ext_rsm.tikz.tex` embedded in Tikz mode, dot-compiled `ext_rsm.pdf` in DOT mode. The plain RSM figure (`rsm_blocks`) is no longer produced by the runner or shown in the summary (task 279) |
| SPPF-style adjustbox for the RNGLR LR automaton | The RNGLR LR automaton head reuses the SPPF wrap (`wrapTikzAdjustbox true`): shrink-only to `\textwidth` plus `max totalheight=\textheight`, so wide item-list states and tall state stacks both fit the page (task 279) |
| `stackStepSection` per-mode picture | Default mode embeds the step's `tree_and_stack.tikz.tex` inline (same-layer stack frontier), wrapped in adjustbox; `--use-dot` includes the dot-compiled PDF. Mirrors the GLL/RNGLR `useTikz` switch |
| Extended RSM head section for GLL/RNGLR Tikz mode | `headerSection` reads `ext_rsm.tikz.tex` from the viz dir and wraps it in `wrapTikzAdjustbox` (tall stacked figures keep natural size); skipped gracefully when the file is absent so older viz dirs still build. DOT mode keeps the dot-compiled PDF entries |
| Single trailing SPPF section for all algorithms | `sppfSection` is the only SPPF placement (TikZ in Tikz mode, DOT PDF fallback); GLL/RNGLR previously also carried a duplicate header entry with the DOT-compiled PDF (removed in task 269). RPQ algorithms never produce sppf files, so the section is skipped for them |
| Shared mode-aware RPQ head | One `rpqHeaderSection` serves both RPQ algorithms (Arroyuelo, Belyanin): the query regexp (`regexp.tex`, math mode), the query DFA, and the input graph — inline TikZ wrapped in a width-only adjustbox in Tikz mode, dot-compiled `dot_pdfs/dfa.pdf` / `dot_pdfs/graph.pdf` in DOT mode. Each figure is skipped gracefully when its source file is absent; only the color legend differs between the two kinds (task 280, task 281) |
| `arroyueloStepSection` two-column layout | Each Arroyuelo RPQ step fills `data/Arroyuelo_step_(tikz_)template.tex`: left minipage = regexp tree figure + matrix equation, right minipage = graph with the step's result-path highlights. Tree and graph figures are adjustbox-wrapped in TikZ mode; DOT mode includes the dot-compiled PDFs (task 280) |
| `belyaninStepSection` two-column layout | Each Belyanin RPQ step fills `data/Belyanin_step_(tikz_)template.tex`: left minipage = query DFA figure with frontier states highlighted + matrix stack, right minipage = graph with the current frontier-path highlights. Automaton and graph figures are adjustbox-wrapped in TikZ mode; DOT mode includes the dot-compiled PDFs (task 281) |
| RPQ step figures wrap to the column width (`wrapTikzAdjustboxColumn`) | The step figures sit inside minipage columns of `0.52\textwidth` / `0.46\textwidth`, but a minipage does not change `\textwidth` — the old `wrapTikzAdjustbox` (max width=`\textwidth`) let an over-wide figure reach the full page width and overflow its column. `\linewidth` is the column width inside the minipage, so the column-width wrap confines the figure to its column. The DOT-mode templates' `\includegraphics` use `width=\linewidth` for the same reason (task 282, S3) |
| Whole RPQ step wrapped in `wrapStepAdjustbox` | The left-column matrix stack (up to 8 `pNiceMatrix` blocks) exceeded `\textheight`, producing `Overfull \vbox` on the step pages. Wrapping the entire filled step template in an adjustbox limited to `max width=\textwidth, max totalheight=0.9\textheight` shrinks over-tall steps to fit one page (shrink-only — short steps are never scaled). The 10% headroom accommodates the step's `\subsection*` heading, which must stay outside the box: sectioning commands cannot run inside an adjustbox (verified — LaTeX error "Something's wrong--perhaps a missing \\item"). Verified end-to-end: both RPQ summaries (TikZ and DOT) compile with no Overfull boxes in the lualatex log (task 282, S3) |

## See Also

- [GrammarTeX module](grammar-tex.md) — grammar rendering used in headers
- [ExternalTools module](external-tools.md) — Dot/TeX compilation for summary PDFs
- [CLI user documentation](../user/cli.md) — `--summary` flag usage
