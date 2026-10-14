# Test Categories

**Tags:** guide, testing, categories, graphviz, tex, lualatex, external-tools
**Kind:** guide

> **Abstract:** Documents xUnit `[<Trait("Category", "...")>]` attributes that group tests requiring external tools: `Graphviz` (requires `dot`), `TeX` (requires `lualatex`), `Summary` (requires `lualatex`). Categories allow selective execution in CI and document external tool dependencies at the test level.

## Contents

- [Categories](#categories)
- [Environment requirements](#environment-requirements)
- [Why categories exist](#why-categories-exist)
- [Adding a new category](#adding-a-new-category)

## See Also

- [ExternalTools module](../external-tools.md) — Graphviz and lualatex wrappers
- [Quality Standards](quality-standards.md)

Tests in this project use xUnit `[<Trait("Category", "...")>]` attributes to group tests that require external tools. Categories are cross-cutting — they span multiple test files and modules.

## Categories

| Category | Required tool | Affected tests | Purpose |
| --- | --- | --- | --- |
| `Graphviz` | `dot` (Graphviz) | `AutomatonVisualizationTests`, `DerivationTreeVisualizationTests`, `LLVisualizerTests`, `LRVisualizerTests`, `ExternalToolsTests`, `TexCompilationTests` | Compile generated DOT graphs to verify structural correctness (`-Tplain` parsing) and produce PDFs |
| `TeX` | `lualatex` | `AutomatonVisualizationTests`, `DerivationTreeTikzTests`, `ExternalToolsTests`, `TexCompilationTests` | Compile generated TeX/Tikz fragments in a standalone document to verify correctness and strict error detection |
| `Summary` | `lualatex` | `CliSummaryTests` | End-to-end CLI `--summary` producing merged TeX documents, compiled to PDF |

## Environment requirements

The categorized tests require these external tools on the system PATH, with the version constraints below (verified 2026-10-13):

| Tool | Requirement | Notes |
| --- | --- | --- |
| `dot` (Graphviz) | Must support `-Tpdf`, `-Tjson`, `-Tplain` | Ubuntu 24.04's 2.42.x and 26.04's 14.1.2 both qualify; Debian's 14.x packaging does not ship a plain `pdf` format (only `pdf:cairo` via the poppler plugin) |
| `lualatex` (TeX Live) | TeX Live with nicematrix — generated output compiles on both nicematrix 6 and 7 | The summary templates use babel, xcolor, nicematrix, adjustbox, amsmath, geometry, hyperref, graphicx, tikz (+ graphs/graphdrawing/quotes/babel/arrows.meta/shapes.geometric libraries) |
| .NET coverage profiler | dotnet-coverage ≥ 18.11 | Older versions (≤ 18.10) link `libxml2.so.2`, which no longer exists on Ubuntu 26.04 (libxml2 2.15 has SONAME `libxml2.so.16`), so coverage collection fails with "Profiler was not initialized" |

## Why categories exist

Tests in these categories require external executables (`dot`, `lualatex`) on the system PATH. If a tool is missing, the test fails. Categories allow:

- **Documentation**: each test declares its external dependency via `[<Trait>]`
- **Selective execution**: CI or environments without the tools can exclude them with `--filter "Category!=Graphviz&Category!=TeX&Category!=Summary"`
- **Local development**: the `quality-gates` skill runs all tests without filters — developers are expected to have the tools installed

## Adding a new category

When a new test requires an external tool:

1. Add `[<Trait("Category", "Name")>]` to the test
2. Document the category, tool, and affected test files in this file
3. Update `docs/developer/external-tools.md` if a new tool is introduced

## See Also

- [ExternalTools module](../external-tools.md) — tool wrappers
- [Quality Standards](quality-standards.md)
