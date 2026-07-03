# Task 97: Refactor FLPQ.Cli

## Goal

1. Move TeX-render-specific functions to Printers project
2. Remove merged file compilation (pdflatex step). Just generate merged TeX file.
3. Split Program.fs into modules (and files). Separate algorithm-specific functions, helpers to create and handle output folders structures, etc.

## Design

### New Printers module: SummaryTeX.fs

Move the following functions from `Program.fs` to a new `SummaryTeX.fs` in `FLPQ.Printers`:

- `wrapMath` — wraps TeX in `\[...\]` + center
- `wrapCenter` — wraps TeX in center environment
- `includePdf` — `\includegraphics` snippet
- `section` — `\subsection*` snippet
- `headerSection` — builds header (grammar, CNF, input, LL/LR table, LR automaton)
- `tableStepSection` — per-step TeX for CYK/Valiant
- `stackStepSection` — per-step TeX for LL/LR
- `buildContent` — assembles full merged TeX content

These functions produce LaTeX string content. They are independent of CLI logic.

### New CLI modules (split Program.fs)

**AlgorithmTypes.fs** — `FLPQ.Cli.AlgorithmTypes`
- `Algorithm` DU (CYK | Valiant | LL | LR)
- `Arguments` DU (IArgParserTemplate)

**Helpers.fs** — `FLPQ.Cli.Helpers`
- `readFile`
- `writeOutputFile`
- `writeStepsVisualization`
- `readIfExists`
- `naturalSortKey`
- `collectSteps`
- `findSummaryTemplate`

**CykRunner.fs** — `FLPQ.Cli.CykRunner`
- `runCyk`

**ValiantRunner.fs** — `FLPQ.Cli.ValiantRunner`
- `runValiant`

**LLRunner.fs** — `FLPQ.Cli.LLRunner`
- `runLL`

**LRRunner.fs** — `FLPQ.Cli.LRRunner`
- `runLR`

**Summary.fs** — `FLPQ.Cli.Summary`
- `SummaryKind` DU (moved from Program.fs, now public)
- `algorithmKind`
- `algorithmLower`
- `compileDotArtifacts` — kept (dot files still compiled to PDF for includegraphics)
- `buildSummary` — simplified: generates merged TeX only, no pdflatex. Uses SummaryTeX functions from Printers.

**Program.fs** — `FLPQ.Cli.Program`
- `runCli` — testable entry point
- `main` — `[<EntryPoint>]`

### Simplification of buildSummary

Before (current):
1. compileDotArtifacts → dot PDFs
2. buildContent → merged TeX content
3. write merged TeX
4. copy to build dir
5. pdflatex twice
6. copy PDF to results

After (task 97):
1. compileDotArtifacts → dot PDFs (kept: needed for includegraphics in TeX)
2. buildContent → merged TeX content (uses SummaryTeX from Printers)
3. write merged TeX file
4. Done. No pdflatex compilation.

### Test updates

`CliSummaryTests.fs` currently checks for PDF output. After changes:
- The `-s` flag generates the merged TeX file only
- Tests verify the merged TeX file exists and is non-empty
- Tests verify exit code is 0

### Compilation order in FLPQ.Cli.fsproj

```
AlgorithmTypes.fs
Helpers.fs
CykRunner.fs
ValiantRunner.fs
LLRunner.fs
LRRunner.fs
Summary.fs
Program.fs
```

### Files to create

- `src/FLPQ.Printers/SummaryTeX.fs`
- `src/FLPQ.Cli/AlgorithmTypes.fs`
- `src/FLPQ.Cli/Helpers.fs`
- `src/FLPQ.Cli/CykRunner.fs`
- `src/FLPQ.Cli/ValiantRunner.fs`
- `src/FLPQ.Cli/LLRunner.fs`
- `src/FLPQ.Cli/LRRunner.fs`
- `src/FLPQ.Cli/Summary.fs`

### Files to modify

- `src/FLPQ.Printers/FLPQ.Printers.fsproj` — add SummaryTeX.fs
- `src/FLPQ.Cli/FLPQ.Cli.fsproj` — replace single Program.fs with new file list
- `src/FLPQ.Cli/Program.fs` — rewrite to thin entry point
- `tests/FLPQ.Printers.Tests/CliSummaryTests.fs` — update assertions
- `docs/FLPQ.Printers.md` — add SummaryTeX docs
- `tasks/detailed_plan.md` — update

## Progress

- [x] 1. Create SummaryTeX.fs in Printers project
- [x] 2. Update FLPQ.Printers.fsproj
- [x] 3. Create AlgorithmTypes.fs
- [x] 4. Create Helpers.fs
- [x] 5. Create CykRunner.fs
- [x] 6. Create ValiantRunner.fs
- [x] 7. Create LLRunner.fs
- [x] 8. Create LRRunner.fs
- [x] 9. Create Summary.fs
- [x] 10. Rewrite Program.fs
- [x] 11. Update FLPQ.Cli.fsproj
- [x] 12. Update CliSummaryTests.fs
- [x] 13. Build, format, run tests
- [x] 14. Update documentation

## Outcome

All 411 tests pass. Task 97 completed successfully.
