# SummaryTeX Module

## Module Purpose

Generates TeX content for merged summary documents produced by the CLI.
Provides LaTeX helper functions and structured section builders that
assemble per-step artifacts (tables, stacks, dot-generated PDFs) into
a single compilable TeX document.

## Function Signatures

### `wrapMath`
```fsharp
val wrapMath: string -> string
```
Wraps TeX content in `\[...\]` math display mode inside a `\begin{center}` environment.

### `wrapCenter`
```fsharp
val wrapCenter: string -> string
```
Wraps content in a `\begin{center}...\end{center}` environment.

### `includePdf`
```fsharp
val includePdf: string -> string
```
Generates an `\includegraphics` command in a centered block, referencing a PDF
by relative path (e.g., `../dot_pdfs/lr_automaton.pdf`).

### `section`
```fsharp
val section: string -> string
```
Generates a `\subsection*{...}` heading.

### `headerSection`
```fsharp
val headerSection: vizDir:string -> algoKind:string -> lrAutomatonPdf:string option -> string list
```
Builds the header portion of the merged document: original grammar, CNF grammar
(for table-based algorithms), input string, LL/LR parsing table, and LR automaton PDF.
The `algoKind` parameter is `"table"` (CYK/Valiant), `"ll"`, or `"lr"`.

### `tableStepSection`
```fsharp
val tableStepSection: stepDir:string -> stepNum:int -> string list
```
Builds a per-step section for table-based algorithms (CYK, Valiant), including
the table TeX and any boolean decomposition files (`bool_decomp_*.tex`).

### `stackStepSection`
```fsharp
val stackStepSection: stepDir:string -> stepNum:int -> stepName:string -> string list
```
Builds a per-step section for stack-based algorithms (LL, LR), including
the compiled tree-and-stack PDF and the input TeX.

### `buildContent`
```fsharp
val buildContent: algo:string -> vizDir:string -> stepCount:int -> lrAutomatonPdf:string option -> string list
```
Assembles the complete merged TeX content for one algorithm: algorithm header,
header section, and all per-step sections in sorted order.
The `algo` parameter is a string (`"CYK"`, `"Valiant"`, `"LL"`, `"LR"`).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Separate module in Printers | TeX content generation belongs in the printers layer, not CLI. CLI orchestrates file I/O and algorithm dispatch; content assembly is formatting logic. |
| String-based algo kind | Avoids dependency on CLI-specific `Algorithm` DU; module can be reused or tested independently. |
| File I/O via `readIfExists` | Headers and steps read existing artifact files; module does not create files. Produces only string content. |
| `\includegraphics` for dot PDFs | The merged TeX references PDFs compiled from dot files by the CLI (via `ExternalTools.compileDotFileToPdf`). The module assumes these PDFs exist at the expected relative paths. |
