# Detailed Plan: Task 98 — Improve LR Table Rendering

## Changes

### File: `src/FLPQ.Printers/LRTableTeX.fs`

**Issue 1: Column separator — three bars between ACTION and GOTO should be two**
- Current: `actionCols = String.replicate (terminals.Length + 1) "c | "` produces trailing ` | ` which combines with `||` to create `|||` (three bars).
- Fix: Build ACTION and GOTO column specs without trailing ` | `, join explicitly with ` || `.

**Issue 2: Row hlines — data rows currently produce double hlines between them**
- Current: Data rows both start with `\hline` and end with `\\ \hline`, producing two hlines between every data row.
- Fix: Remove `\hline` from the end of data rows. Header row keeps `\\ \hline`, data rows start with `\hline`, but data rows end with just `\\` (or `\\ [1ex]` for the last row). Result: 2 hlines between header and first data row, 1 hline between data rows.

## Tests to Update
- Tests in `TexCompilationTests.fs` already check structural elements but don't check hline count. After the fix, the generated TeX should still compile. The structural assertions (contains `$s_`, `$r_`, `acc`, `S`, `E`, `T`, `F`) remain valid.

## Verification
- Run `dotnet build FLPQ.slnx -c Debug`
- Run `dotnet test` (TeX tests with "TeX" trait will run locally if TeX is installed)
- Run `dotnet fantomas . --check`
