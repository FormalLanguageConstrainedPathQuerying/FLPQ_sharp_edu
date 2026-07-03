# ExternalTools module

Part of [`FLPQ.Printers`](FLPQ.Printers.md).

## Purpose

Wraps external command-line tools used to verify and assemble visualization output:

- **Graphviz `dot`** — compiles Dot source files into PDFs and parses `-Tplain` output for tests.
- **`pdflatex`** — compiles TeX source into PDFs with strict error detection.

The module is shared between the test suite (strict compilation checks for generated Dot/TeX strings) and the CLI summary generator (batch Dot→PDF and TeX→PDF compilation to produce the final visualization PDF).

## Source

`src/FLPQ.Printers/ExternalTools.fs`

## Types

```fsharp
type DotInfo =
    { nodeCount: int
      edgeCount: int
      nodeLabels: string list
      edgeLabels: string list }
```

Parsed information from a Graphviz `-Tplain` output. Used by visualization tests to assert structural properties of generated graphs (expected number of nodes, edges, labels).

## Functions

### `compileDotStringToInfo : string -> DotInfo`

Compiles a Dot string via `dot -Tplain` and parses the output.

- **Precondition**: `dot` is on the `PATH`.
- **Throws** if `dot` exits with a non-zero code.
- Used by tests to verify that generated Dot source compiles and has the expected structure.

### `compileDotString : string -> bool`

Returns `true` iff `compileDotStringToInfo` succeeds. Convenience wrapper for boolean assertions.

### `compileDotFileToPdf : dotPath:string -> pdfPath:string -> bool`

Compiles a Dot file to PDF via `dot -Tpdf -o pdfPath dotPath`.

- Creates the parent directory of `pdfPath` if it does not exist.
- Returns `true` iff the exit code is 0 and the produced PDF exists and is non-empty.
- Writes diagnostics to `stderr` on failure.
- Used by the CLI summary generator to compile per-step `tree_and_stack.dot` and `lr_automaton.dot`.

### `compileTexStringWithTemplate : templatePath:string -> tex:string -> bool`

Compiles a TeX fragment using a template file. The template must contain the placeholder `__CONTENT__`, which is replaced by `tex`. The combined document is written to a temporary directory and compiled with `pdflatex`.

- Returns `true` iff the compilation succeeds according to `pdflatexSucceeded`.
- Used by tests: the template at `data/tex_template.tex` wraps content in `\[...\]` for standalone math, `data/tex_tabular_template.tex` wraps it for standalone tabular.

### `compileTexFile : texPath:string -> outputDir:string -> bool`

Compiles a TeX file to PDF in the given output directory (single pass) via `pdflatex -interaction=nonstopmode -output-directory=outputDir texPath`.

- Returns `true` iff the compilation succeeds according to `pdflatexSucceeded`.
- The output PDF is named `<texBasename>.pdf` and is left in `outputDir`.
- Writes diagnostics to `stderr` on failure.

### `compileTexFileTwice : texPath:string -> outputDir:string -> bool`

Compiles a TeX file **twice** (for table-of-contents and cross-references). Returns `true` iff both passes succeed. Used by the CLI summary generator.

## Strict error detection (`pdflatexSucceeded`)

A pdflatex run is considered successful only when **all** of the following hold:

1. Exit code is 0.
2. No stdout line starts with `!` (TeX error marker) or contains `Fatal error` or `Error:`.
3. The output PDF exists and is non-empty.

Relying on exit code alone is insufficient because pdflatex may exit 0 even when errors occur. The line-based check matches the behavior previously hardcoded in the test utilities.

## Process invocation

All external processes are invoked via a shared `runProcess : string -> string -> string option -> int * string * string` helper that:

- Disables shell execution.
- Redirects stdout and stderr.
- Waits up to 30 seconds (timeout).
- Returns `(exitCode, stdout, stderr)`.

## Relationship to other modules

- **CLI** (`src/FLPQ.Cli/Program.fs`): calls `compileDotFileToPdf` and `compileTexFileTwice` when `--summary` is active.
- **Tests** (`tests/FLPQ.Printers.Tests/*.fs`): call `compileDotStringToInfo`, `compileDotString`, and `compileTexStringWithTemplate` to assert that generated Dot/TeX compiles correctly.

## Design decisions

- The module lives in `FLPQ.Printers` (not a separate project) because both the CLI and the printer tests already depend on `FLPQ.Printers`. This avoids a new project and a new test project just for two thin process wrappers.
- The API is split into string-based variants (for tests, which generate strings in memory) and file-based variants (for the CLI, which writes intermediate files and wants to preserve the PDFs).
- Errors are reported via `bool` return values and `stderr` messages rather than exceptions, so the CLI can continue processing other algorithms and report an aggregate exit code.
