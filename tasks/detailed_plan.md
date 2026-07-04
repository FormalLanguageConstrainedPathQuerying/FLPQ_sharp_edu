# Detailed Plan: Task 103 — Switch TeX compilation to lualatex

## Problem

All TeX compilation currently uses `pdflatex`. The project needs to switch to `lualatex` for better Unicode handling and compatibility with Tikz graphdrawing (tasks 104-106). 

Also note: AGENTS.md line 33 still references `pdflatex` as an example tool.

## Goal

Replace all `pdflatex` usage with `lualatex` throughout the codebase. Update templates to be lualatex-compatible.

## Design Decisions

### Decision 1: Template changes

Current templates use `[utf8]{inputenc}` and `[T2A]{fontenc}` — these are pdflatex-specific packages. For lualatex, do NOT use `fontspec` (which adds complexity). Instead, simply remove `inputenc` and `fontenc` packages. lualatex natively handles UTF-8 without them. Also remove `russian` from `babel` options since we don't need Cyrillic support (leave `english`).

### Decision 2: Error detection

The `pdflatexSucceeded` function checks for `!`, `Fatal error`, and `Error:` in stdout. lualatex produces similar error patterns, so the detection logic should remain the same. Rename to `latexSucceeded`.

### Decision 3: compileTexFileTwice

The function calls `compileTexFile` twice. For summaries with TOC and cross-references, lualatex also requires double compilation. No change needed.

### Decision 4: Test coverage

All TeX-related tests:
- `TexCompilationTests.fs` — 7 tests compile snippets with `compileTexStringWithTemplate`
- `ExternalToolsTests.fs` — tests the core compilation functions
- `CliSummaryTests.fs` — tests full pipeline (PDLaTeX tests would be handled by CLI; run via `runCli` — currently marked `[<Trait("Category", "Summary")>]`, not TexCompilation)

We need to verify that lualatex is available in the test environment first. If `lualatex` is not in PATH, we need to handle gracefully (or add skip logic). Let's check.

## Implementation Checklist

### 1. Check lualatex availability
- [ ] Run `which lualatex`

### 2. `src/FLPQ.Printers/ExternalTools.fs` — Core changes
- [ ] Rename `pdflatexSucceeded` to `latexSucceeded` (internal function)
- [ ] Replace `"pdflatex"` with `"lualatex"` in `compileTexStringWithTemplate` (line 168)
- [ ] Replace `"pdflatex"` with `"lualatex"` in `compileTexFile` (line 189)
- [ ] Update error messages (lines 197, 206)

### 3. Template files
- [ ] `data/tex_template.tex` — remove `inputenc`, remove `fontenc` (not needed for lualatex)
- [ ] `data/tex_tabular_template.tex` — same as above (but this file only has `amsmath`, `preview`, no `inputenc`)
- [ ] `data/tex_summary_template.tex` — remove `inputenc`/`fontenc`, switch babel to english only

### 4. Tests — Rename and verify
- [ ] `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — update test names (cosmetic, rename "pdflatex" to "lualatex")
- [ ] `tests/FLPQ.Printers.Tests/ExternalToolsTests.fs` — update test names if needed

### 5. Documentation
- [ ] `docs/technologies.md` — update pdflatex → lualatex
- [ ] `docs/architecture.md` — update references
- [ ] `docs/external-tools.md` — update references, mention lualatex
- [ ] `docs/FLPQ.Cli.md` — update references
- [ ] `docs/cli.md` — update references
- [ ] `tasks/knowledge_base.md` — update references

### 6. Final Checks
- [ ] Format: `dotnet fantomas . --check`
- [ ] Build: `dotnet build FLPQ.slnx -c Release`
- [ ] Tests: `dotnet test`
- [ ] All existing TexCompilation tests pass with lualatex
