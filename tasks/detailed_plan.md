# Detailed Plan: Task 100 — Golden Tests for LR Table Rendering

## Changes

### 1. New file: `tests/FLPQ.Printers.Tests/LRTableTeXGoldenTests.fs`
- Follow the same pattern as `GrammarTeXGoldenTests.fs`
- Private module with grammar definitions (grammar1, grammar7)
- For each grammar, build augmented grammar, then generate tex for:
  - LR(0) table (`LRParser.buildLR0Table`)
  - SLR(1) table (`LRParser.buildSLR1Table`)
  - CLR(1) table (`LRParser.buildCLR1Table`)
- Golden file naming: `lr0_grammar1_table.tex`, `slr1_grammar1_table.tex`, `clr1_grammar1_table.tex`, etc.
- Tests first run: generate golden files, fail with message to copy
- After copying to `GoldenData/`, tests verify exact match

### 2. New golden reference files in `tests/FLPQ.Printers.Tests/GoldenData/`
- `lr0_grammar1_table.tex`
- `slr1_grammar1_table.tex`
- `clr1_grammar1_table.tex`
- `lr0_grammar7_table.tex`
- `slr1_grammar7_table.tex`
- `clr1_grammar7_table.tex`

## Verification
- Run tests to generate golden files
- Copy generated golden files to `GoldenData/`
- Re-run tests — all must pass
- Full test suite must pass
- Formatting check must pass
