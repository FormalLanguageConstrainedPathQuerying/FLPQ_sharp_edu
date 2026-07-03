# Task 94: Move run_viz.py to F#

## Goal

Replace the Python `run_viz.py` script with native F# logic in the existing CLI.
After this task, the Python script is removed; the CLI gains a `--summary` flag
that, after generating the per-step TeX/Dot artifacts, also:

1. Compiles every `*.dot` artifact to PDF (via Graphviz).
2. Builds a merged TeX document per algorithm (the current `*_merged.tex`).
3. Compiles the merged TeX twice with `pdflatex` (for ToC / cross-references).
4. Fails with a non-zero exit code if any Dot or TeX compilation produces errors.

## Design

### Shared external-tool wrappers

The compilation helpers currently live in `tests/FLPQ.Printers.Tests/TestUtils.fs`
(`checkDotCompiles`, `checkDotCompilesWithInfo`, `checkTexCompiles`). They must
be reusable from both the tests and the CLI. We move them to a new public module
`FLPQ.Printers.ExternalTools` inside the existing `FLPQ.Printers` project (which
is already referenced by both CLI and tests). `TestUtils.fs` in the test project
is removed; test files call `ExternalTools` directly.

Public API of `ExternalTools`:

- `DotInfo` record (nodeCount, edgeCount, nodeLabels, edgeLabels) — kept for tests
- `compileDotStringToInfo : string -> DotInfo` — throws on failure (was `checkDotCompilesWithInfo`)
- `compileDotString : string -> bool` — was `checkDotCompiles`
- `compileDotFileToPdf : dotPath:string -> pdfPath:string -> bool` — new, for CLI
- `compileTexStringWithTemplate : templatePath:string -> tex:string -> bool` — was `checkTexCompiles`
- `compileTexFile : texPath:string -> outputDir:string -> bool` — new, single pass, for CLI
- `compileTexFileTwice : texPath:string -> outputDir:string -> bool` — new, two passes for cross-refs

Strict error checking (matches existing `checkTexCompiles`):
- exit code 0
- no stdout line starting with `!` or containing `Fatal error` / `Error:`
- output PDF exists and is non-empty

### TeX summary template

New template file `data/tex_summary_template.tex` containing the document
skeleton currently inlined in `run_viz.py` (with `__ALGORITHM__` and
`__CONTENT__` placeholders). This is consistent with the existing
`data/tex_template.tex` and `data/tex_tabular_template.tex` convention.

### Example data

New files in `data/`:

- `data/example_grammar.bnf` — `S -> a S b S\nS -> eps`
- `data/example_input.txt` — `a a b a b b`

### CLI changes

Add a new optional flag `Summary` (`-s` / `--summary`) to the `Arguments` DU.

Per-algorithm summary builder functions in `Program.fs`:

- `buildCykSummary (vizDir) (algoDir) : bool`
- `buildValiantSummary (vizDir) (algoDir) : bool`
- `buildLLSummary (vizDir) (algoDir) : bool`
- `buildLRSummary (vizDir) (algoDir) : bool`

Each:
1. Collects `step_*` directories sorted numerically.
2. Compiles every `*.dot` file under each step (and `lr_automaton.dot` for LR)
   to `<algoDir>/dot_pdfs/<step>_<name>.pdf` via `ExternalTools.compileDotFileToPdf`.
3. Builds the merged TeX content by emitting, per step:
   - For CYK / Valiant: `\[ <table.tex> \]` and any `bool_decomp_*.tex` files.
   - For LL / LR: `\includegraphics{...}` for `tree_and_stack.pdf` and `\[ <input.tex> \]`.
   - Common headers: original grammar, CNF grammar (CYK/Valiant), LL/LR table, LR automaton.
4. Writes `<algoDir>/<algo>_merged.tex` by substituting `__ALGORITHM__` and `__CONTENT__`
   in the summary template.
5. Copies the merged TeX to `<algoDir>/merged_tex_build/` and calls
   `ExternalTools.compileTexFileTwice`.
6. Copies the resulting PDF to `<algoDir>/<algo>_visualization.pdf`.
7. Returns `false` if any compilation step failed; the CLI exits with code 1 in that case.

Each builder returns the list of produced PDFs so `main` can print a summary.

The CLI flow becomes:

```
parse args
run <algorithm> -> writes step artifacts to <output>
if --summary:
    build <algorithm> summary -> writes <output>/results/<algorithm>/...
    exit 1 if any compilation failed
```

### Algorithm-specific actions split

Functions per algorithm handle the differences in what artifacts exist:

- CYK: `grammar_original.tex`, `grammar_cnf.tex`, `input.tex`, per-step `table.tex`.
- Valiant: same as CYK plus per-step `bool_decomp_*.tex` (only on last step).
- LL: `grammar_original.tex`, `ll_table.tex`, per-step `tree_and_stack.dot`, `input.tex`.
- LR: `grammar_original.tex`, `lr_table.tex`, `lr_automaton.dot`, per-step `tree_and_stack.dot`, `input.tex`.

A shared helper `collectSteps` sorts `step_*` directories numerically.
A shared helper `includePdf` produces the `\includegraphics` line for a PDF
relative to the merged TeX location.

### Removing run_viz.py

After the F# implementation is verified to produce the same artifacts and
compile them, delete `run_viz.py`. Update `docs/cli.md` to remove the
"Batch Visualization Script" section and replace it with documentation of the
`--summary` flag.

## Tests

A new test file `tests/FLPQ.Printers.Tests/ExternalToolsTests.fs` (category
`Dot` and `TeX`) verifies:

- `compileDotStringToInfo` parses node/edge counts correctly on a known small graph.
- `compileDotFileToPdf` produces a non-empty PDF file.
- `compileTexFile` succeeds on a minimal TeX file and produces a non-empty PDF.
- `compileTexFileTwice` succeeds and produces a non-empty PDF.

A new test file `tests/FLPQ.Printers.Tests/CliSummaryTests.fs` (category
`Summary`) verifies the end-to-end CLI summary generation on the example
grammar for each algorithm. The test invokes the CLI as a subprocess (via
`dotnet run --project src/FLPQ.Cli -c Release -- -a <algo> -s ...`) and asserts
that the final `<algo>_visualization.pdf` exists and is non-empty, and that the
process exit code is 0. These tests are marked with `[<Trait("Category", "Summary")>]`
and require both `dot` and `pdflatex`; they reuse the existing category-based
filtering convention.

Existing `TexCompilationTests.fs` and `*VisualizerTests.fs` files are updated
to call `ExternalTools` directly instead of `TestUtils`.

## Verification

1. `dotnet build -c Release` succeeds.
2. `dotnet fantomas . --check` passes.
3. `dotnet test` (without `TeX` / `Summary` categories) passes on all OSes.
4. Manual: run `dotnet run --project src/FLPQ.Cli -c Release -- -a CYK -g data/example_grammar.bnf -i data/example_input.txt -o /tmp/viz -s` and verify:
   - `/tmp/viz/results/cyk/cyk_visualization.pdf` exists and is non-empty.
   - No errors in stdout/stderr.
5. Repeat for Valiant, LL, LR.
6. `git rm run_viz.py`.

## Documentation updates

- `docs/cli.md`: replace "Batch Visualization Script" section with `--summary` flag docs.
- `docs/FLPQ.Printers.md`: add `ExternalTools` to module table.
- `docs/external-tools.md`: new file documenting the module.
- `docs/architecture.md`: add `ExternalTools.fs` to FLPQ.Printers file list.
- `docs/technologies.md`: no changes (already lists Graphviz and pdflatex).
- `tasks/knowledge_base.md`: add note about strict pdflatex error detection.

## Steps

1. Create `data/example_grammar.bnf`, `data/example_input.txt`, `data/tex_summary_template.tex`.
2. Create `src/FLPQ.Printers/ExternalTools.fs` with the API above; add to fsproj.
3. Update `tests/FLPQ.Printers.Tests/FLPQ.Printers.Tests.fsproj` to include
   `data/tex_summary_template.tex` as content.
4. Remove `tests/FLPQ.Printers.Tests/TestUtils.fs`; update all test files to use `ExternalTools`.
5. Add `Summary` flag to CLI `Arguments`; implement summary builders; update `main`.
6. Add `ExternalToolsTests.fs` and `CliSummaryTests.fs`.
7. `dotnet build`, `dotnet fantomas .`, `dotnet test`.
8. Manually verify summary generation for all 4 algorithms.
9. `git rm run_viz.py`.
10. Update documentation.
11. Commit.
