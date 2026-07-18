# ExternalTools Module

**Tags:** utility, tex, dot, compilation, lualatex, graphviz, file-io
**Kind:** utility
**Module:** ExternalTools
**Source:** `src/FLPQ.Printers/ExternalTools.fs`
**Depends on:** _(system tools: dot, lualatex)_
**Used by:** FLPQ.Cli (summary generator), FLPQ.Printers.Tests (compilation checks)

> **Abstract:** Wraps external command-line tools used to verify and assemble visualization output: Graphviz `dot` (compiles Dot source to PDFs, parses `-Tplain` output for structural assertions) and `lualatex` (compiles TeX source to PDFs with strict multilayered error detection). Shared between the test suite and CLI summary generator. Provides both string-based and file-based compilation variants.

## Contents

- [Purpose](#purpose)
- [Type Definitions](#type-definitions)
- [Function Signatures](#function-signatures)
- [Strict Error Detection](#strict-error-detection)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Purpose

The module bridges the gap between generated TeX/Dot strings and compilable PDFs. Both the test suite (verifying that generated output is valid) and the CLI (assembling the final visualization PDF) need external tool access. This module centralizes that logic with consistent error detection.

### `DotInfo`
```fsharp
type DotInfo =
    { nodeCount: int; edgeCount: int
      nodeLabels: string list; edgeLabels: string list }
```
Parsed information from Graphviz `-Tplain` output. Used by visualization tests to assert structural properties.

## Function Signatures

### Dot Compilation
```fsharp
val compileDotStringToInfo : string -> DotInfo
val compileDotString : string -> bool
val compileDotFileToPdf : dotPath:string -> pdfPath:string -> bool
```

### TeX Compilation
```fsharp
val compileTexStringWithTemplate : templatePath:string -> tex:string -> bool
val compileTexFile : texPath:string -> outputDir:string -> bool
val compileTexFileTwice : texPath:string -> outputDir:string -> bool
```

## Strict Error Detection

A lualatex run succeeds only when ALL of:
1. Exit code is 0.
2. No stdout line starts with `!` or contains `Fatal error` or `Error:`.
3. The output PDF exists and is non-empty.

Relying on exit code alone is insufficient — lualatex may exit 0 even when errors occur.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Module in `FLPQ.Printers` | Both CLI and tests already depend on Printers; no new project needed |
| Split API: string-based + file-based | Tests use string-based (in-memory); CLI uses file-based (preserves PDFs) |
| Bool returns + stderr messages | CLI can continue processing other algorithms and report aggregate exit code |
| Shared `runProcess` helper | Consistent process invocation: no shell, redirected output, 30s timeout |

## See Also

- [SummaryTeX module](summary-tex.md) — uses ExternalTools for batch compilation in summary
- [Automaton visualization](automaton-viz.md) — produces Dot/Tikz compiled via ExternalTools
- [Test categories](guides/test-categories.md) — Graphviz and TeX test categories
