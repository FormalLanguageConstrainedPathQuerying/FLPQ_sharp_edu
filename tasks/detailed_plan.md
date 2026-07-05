# Detailed Plan: Task 117 — FLPQ.Cli.Tests Runner Tests

## Status
Partially complete. The following already exists:
- `FLPQ.Cli.Tests` project created and added to `FLPQ.slnx`
- `CliSummaryTests.fs` moved from `FLPQ.Printers.Tests`
- `AlgorithmTypesTests.fs` — tests for `displayName` and CLI arg parsing
- `HelpersTests.fs` — tests for `readFile`, `writeOutputFile`, `cleanOutputDir`, `readIfExists`, `naturalSortKey`
- `SummaryTests.fs` — tests for `algorithmKind` and `algorithmLower`
- `ErrorPathTests.fs` — error-path tests
- `FLPQ.Printers.Tests` no longer references `FLPQ.Cli` (reverse dependency removed)

## Remaining Work

### Runner-level unit tests

Create four test files that verify each runner produces expected output structure when given valid inputs:

#### 1. `CykRunnerTests.fs`
- Test: `runCyk` with example grammar + input produces step directories with `table.tex`
- Test: `runCyk` produces `input.tex`, `grammar_original.tex`, `grammar_cnf.tex`
- Test: `runCyk` with empty input (epsilon) doesn't crash

#### 2. `ValiantRunnerTests.fs`
- Test: `runValiant` with example grammar + input produces step directories with `table.tex`
- Test: `runValiant` produces `input.tex`, `grammar_original.tex`, `grammar_cnf.tex`
- Test: `runValiant` with empty input (epsilon) doesn't crash

#### 3. `LLRunnerTests.fs`
- Test: `runLL` with example grammar + input produces step directories with `tree_and_stack.dot` and `input.tex`
- Test: `runLL` produces `grammar_original.tex`, `ll_table.tex`
- Test: `runLL` with k=1 succeeds
- Test: `runLL` with LR grammar (it will fail to build LL table — test that it throws appropriately)

#### 4. `LRRunnerTests.fs`
- Test: `runLR` with LR grammar + input produces step directories with `tree_and_stack.dot` and `input.tex`
- Test: `runLR` produces `grammar_original.tex`, `lr_table.tex`, `lr_automaton.tikz.tex`
- Test: `runLR` with `--use-dot` flag (dot mode) produces `lr_automaton.dot`
- Test: `runLR` with LR0, SLR1, CLR1 all succeed

### Update fsproj
Add new test files to `<Compile>` items in `FLPQ.Cli.Tests.fsproj`.

### Design Decisions

#### Data files
Use `example_grammar.bnf`/`example_input.txt` (already copied to output) for CYK/Valiant/LL tests.
Use `example_lr_grammar.bnf`/`example_lr_input.txt` for LR tests — need to add these to fsproj as Content items.

#### Test approach
- Each test creates a temp directory, runs the runner, verifies file structure, cleans up
- Focus on verifying correct output file structure (not content golden testing — that's in Printers.Tests)
- Test that runners don't crash with valid inputs

#### Temp directory cleanup
- Use `Path.GetTempPath()` + `Path.GetRandomFileName()` pattern (consistent with existing tests)
- Clean up in test body

### Verification
- `dotnet build` — all projects compile
- `dotnet test --filter "Category!=Summary"` — runner tests pass (Summary tests need lualatex which may not be available)
- `dotnet fantomas . --check` — formatting is correct
