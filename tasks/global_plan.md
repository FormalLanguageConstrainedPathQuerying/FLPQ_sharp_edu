# Global Plan: Tasks 102--106

## Task Summary

| ID | Description | Type | Dependencies |
|----|-------------|------|-------------|
| 102 | Golden tests for CYK merged summary generation | Test | None |
| 103 | Switch TeX compilation from pdflatex to lualatex | Infrastructure | None |
| 104 | Tikz-based visualization for automata | Feature | None |
| 105 | Tikz-based visualization for LR automata | Feature | 104 (uses AutomatonTikz) |
| 106 | CLI option to switch LR automata rendering (Tikz vs dot) | Feature | 104, 105 |

## Dependencies Graph

```
Task 102 (CYK golden tests) ── independent, uses existing SummaryTeX
Task 103 (lualatex switch)   ── independent, affects all TeX compilation
Task 104 (Tikz automata)     ── independent, new module in Printers
Task 105 (Tikz LR automata)  ── depends on 104 (reuses AutomatonTikz)
Task 106 (CLI switch)        ── depends on 104, 105 (uses both Tikz renderers)
```

## Potential Conflicts

| Task | Files Modified | Conflicts With |
|------|---------------|----------------|
| 102 | `tests/FLPQ.Printers.Tests/CykSummaryGoldenTests.fs` (new), `tests/FLPQ.Printers.Tests/GoldenData/*.tex` (new) | None |
| 103 | `src/FLPQ.Printers/ExternalTools.fs`, `data/tex_*.tex` (3 templates), `data/tex_summary_template.tex`, doc `.md` files | Potential merge conflict with 106 if CLI template changes |
| 104 | `src/FLPQ.Printers/AutomatonTikz.fs` (new), `src/FLPQ.Printers/FLPQ.Printers.fsproj`, `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs`, `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` | None |
| 105 | `src/FLPQ.Printers/LRAutomatonTikz.fs` (new), `src/FLPQ.Printers/FLPQ.Printers.fsproj`, `tests/FLPQ.Printers.Tests/` | Minor conflict with 104 (same .fsproj compilation list) |
| 106 | `src/FLPQ.Cli/AlgorithmTypes.fs`, `src/FLPQ.Cli/Program.fs`, `src/FLPQ.Cli/LRRunner.fs`, `src/FLPQ.Cli/Summary.fs`, `src/FLPQ.Cli/FLPQ.Cli.fsproj` | None (after 104, 105 merged) |

## Execution Order

1. **Task 103** — lualatex switch (foundational, affects all TeX)
2. **Task 102** — CYK golden tests (independent, uses existing infrastructure)
3. **Task 104** — Tikz automata visualization (new module)
4. **Task 105** — Tikz LR automata visualization (builds on 104)
5. **Task 106** — CLI switch (builds on 104, 105)

Tasks 102 and 103 are fully independent and could be done in either order.
Tasks 104 → 105 → 106 form a dependency chain.

## Shared Infrastructure

- **verifyGolden helper**: Currently duplicated in `GrammarTeXGoldenTests.fs` and `LRTableTeXGoldenTests.fs`. Task 102 should extract this into a shared `GoldenHelpers.fs` module and refactor existing golden tests to use it.
- **Tikz compilation**: Tasks 104 and 105 need a `compileTikzString` function in `ExternalTools.fs` (or similar) that wraps Tikz code in `standalone` documentclass and compiles with lualatex. This can be implemented once in task 104 and reused in 105.
- **LR item rendering**: Task 105 reuses `SymbolTeX` and `GrammarTeX` for LR item content. The `aligned` environment rendering is purely Tikz-side (LaTeX code in the `as` key).

## Architecture Alignment

- **Task 102**: Golden tests follow the existing pattern from `GrammarTeXGoldenTests.fs`. Reference files in `tests/FLPQ.Printers.Tests/GoldenData/`. Tests generate merged TeX for CYK and compare with golden reference.
- **Task 103**: Switch from pdflatex to lualatex everywhere. Update templates to use lualatex-compatible packages (e.g., `fontspec` instead of `fontenc`/`inputenc`). Update error detection in `ExternalTools.fs`.
- **Task 104**: New `AutomatonTikz.fs` in `src/FLPQ.Printers/`, following the same interface pattern as `AutomatonDot.fs` (parametrized by label printer and state visualizer). Uses Tikz `\graph` with `graphdrawing` library.
- **Task 105**: New `LRAutomatonTikz.fs` in `src/FLPQ.Printers/`. Special style: rectangle nodes, `aligned` state content with LR items, state numbers.
- **Task 106**: Add `--use-dot` flag to CLI. Default: Tikz rendering. When `--use-dot` specified, fall back to existing dot-based rendering.

## Detailed Task Plans

### Task 103 (lualatex switch)

Files to modify:
- `src/FLPQ.Printers/ExternalTools.fs` — replace "pdflatex" with "lualatex" (lines 168, 189); rename `pdflatexSucceeded` to `latexSucceeded`; update error pattern detection if needed
- `data/tex_template.tex` — remove `inputenc`, use `fontspec` if needed for Unicode
- `data/tex_tabular_template.tex` — remove `inputenc`
- `data/tex_summary_template.tex` — remove `inputenc`/`fontenc`, use `fontspec`; remove `babel` or switch to lualatex-compatible
- `tests/FLPQ.Printers.Tests/TexCompilationTests.fs` — update test names (cosmetic)
- Documentation files — update references from pdflatex to lualatex

### Task 102 (CYK golden tests)

Files to create/modify:
- `tests/FLPQ.Printers.Tests/GoldenHelpers.fs` (new) — extract shared `verifyGolden` from existing golden test files
- `tests/FLPQ.Printers.Tests/GrammarTeXGoldenTests.fs` — use shared `verifyGolden`
- `tests/FLPQ.Printers.Tests/LRTableTeXGoldenTests.fs` — use shared `verifyGolden`
- `tests/FLPQ.Printers.Tests/CykSummaryGoldenTests.fs` (new) — golden tests for CYK merged summaries
- `tests/FLPQ.Printers.Tests/GoldenData/cyk_*_summary.tex` (new) — golden reference files
- `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — add new compilation order entries

The golden tests will generate merged summaries for CYK using `SummaryTeX.buildContent` directly (or through the CLI pipeline), then compare with golden files. Test data: grammar1 (`S -> a S b S | eps`) with input `aababb`, grammar7 (expression grammar) with input `x + x`.

### Task 104 (Tikz automata)

Files to create/modify:
- `src/FLPQ.Printers/AutomatonTikz.fs` (new) — `nfaToTikz` and `dfaToTikz` functions
- `src/FLPQ.Printers/FLPQ.Printers.fsproj` — add compilation entry
- `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — add Tikz compilation tests
- `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` — add any new test file entries

Interface: same parameters as `AutomatonDot` — `labelPrinter: 't -> string`, `stateVisualizer: int -> 's -> string`.

### Task 105 (Tikz LR automata)

Files to create/modify:
- `src/FLPQ.Printers/LRAutomatonTikz.fs` (new) — `lr0AutomatontoTikz(aug, dfa)` and `lr1AutomatontoTikz(aug, dfa)`
- `src/FLPQ.Printers/FLPQ.Printers.fsproj` — add compilation entry
- `tests/FLPQ.Printers.Tests/AutomatonVisualizationTests.fs` — add LR Tikz tests

LR items rendering: `<lhs>` `\to` `<rhs>` `\cdot` for dot position. For LR(1): add `,` `<lookahead>`.

### Task 106 (CLI switch)

Files to create/modify:
- `src/FLPQ.Cli/AlgorithmTypes.fs` — add `UseDot` argument case
- `src/FLPQ.Cli/Program.fs` — parse new flag, pass to LR runner
- `src/FLPQ.Cli/LRRunner.fs` — accept rendering mode, branch between Tikz and dot
- `src/FLPQ.Cli/Summary.fs` — handle Tikz-based automaton in summary (standalone Tikz to PDF, or inline)
