# TeX and Tikz Conventions

**Tags:** guide, visualization, tex, tikz, nicematrix, lualatex, golden
**Kind:** guide

> **Abstract:** Conventions and gotchas for generating TeX/LaTeX and Tikz output in this project: nicematrix v6+ `\Block` syntax, the lualatex triple-check for compilation success, `array` environment math-mode rules (`\colorbox`/`\mbox`), the template catalog in `data/`, golden-test template fragility, and Tikz `\graph` with the `graphdrawing` library (preamble, edge labels, loop edges, arrow heads, `as` escaping).

## Contents

- [nicematrix v6+ (TeX Live 2024+) `\Block` Syntax](#nicematrix-v6-te-live-2024-block-syntax)
- [lualatex Error Detection](#lualatex-error-detection)
- [`array` Environment Conventions](#array-environment-conventions)
- [Template Catalog](#template-catalog)
- [Golden Test Template Fragility](#golden-test-template-fragility)
- [Tikz `\graph` with `graphdrawing` Library](#tikz-graph-with-graphdrawing-library)
- [See Also](#see-also)

## nicematrix v6+ (TeX Live 2024+) `\Block` Syntax

**Problem**: nicematrix versions 6.x+ use a different `\Block` syntax. The old positional syntax `\Block[draw=red]{r1-c1-r2-c2}{}` was removed.

**New syntax**: `\Block[draw=red]{rows-cols}{content}`

- `rows`: number of rows the block spans
- `cols`: number of columns the block spans
- `content`: the content displayed in the block (overrides the top-left cell content)

**Placement**: `\Block` must be placed at the top-left cell of the block within the
matrix, not before the matrix.

Reference implementation: `Matrix.fs` `toTeXStyled` function, which generates
`\Block[draw=color]{rowCount-colCount}{styledCellContent}` at the appropriate cell.

## pNiceMatrix Block-Splitting Rules (Lines Only)

To draw thin lines that split a `pNiceMatrix` into blocks (e.g. the Kronecker product's q × q grid
of n × n blocks), use nicematrix's **`vlines`/`hlines`** environment options — these draw a full
rule at each named column/row boundary. (An earlier recipe appended `\vrule width 0.4pt` to a cell
and inserted `\noalign{\hrule height 0.4pt}` lines; the in-cell `\vrule` is clipped to the cell and
does NOT render as a full-height column rule, so the `vlines`/`hlines` options are the correct
mechanism. `\cmidrule` (booktabs) is also not available in the templates.)

`vlines={p1,p2,...}` draws a vertical rule at the left boundary of each 1-indexed column `p`;
`hlines={p1,p2,...}` draws a horizontal rule at the top boundary of each 1-indexed row `p`. For a
`pNiceMatrix[first-row,first-col]`, 1-indexed position 1 is the first-col/first-row, so 0-indexed
data position `d` is 1-indexed `d + 2`. To split a q × q grid of n × n blocks, the internal
boundaries are `vlines = { k*n + 2 for k in 1..q-1 }` (likewise `hlines`). An empty list (`{}`)
draws no rules. Reference implementation:
`KroneckerStepViz.kMatrixWithRules` (see [KroneckerStepViz](../../kronecker-step-viz.md)), which
emits these for the K product mask. Verified by the Kronecker step-template TeX compile test.

## lualatex Error Detection

**Problem**: `lualatex -interaction=nonstopmode` can exit with code 0 even when the TeX source contains undefined control sequences or errors. Relying solely on the exit code misses compilation failures.

**Solution**: Always triple-check:

1. Exit code is 0
2. No line in stdout starts with `!` (TeX error marker) or contains `Fatal error` or `Error:`
3. The output PDF exists and is non-empty (size > 0)

This triple check is implemented in `FLPQ.Printers.ExternalTools.latexSucceeded`. See
`src/FLPQ.Printers/ExternalTools.fs`.

## `array` Environment Conventions

The `array` environment (used inside `\[...\]` or `$$...$$`) is already in **math mode**. This has two consequences:

### Do NOT wrap cell content in `$...$`

Cells in `array` are in math mode — wrapping them in `$...$` is redundant and causes
errors with superscripts/subscripts. For example, `$R^{0,0}_{1,1}$` inside `array`
triggers "Missing $ inserted" because LaTeX tries to enter text mode on the `$` and then
re-enter math mode for `^{...}`.

**Correct:**

```latex
\begin{array}{cccc}
1 & 0 & 1 & R^{0,0}_{1,1} \\
\end{array}
```

**Wrong:**

```latex
\begin{array}{cccc}
$1$ & $0$ & $1$ & $R^{0,0}_{1,1}$ \\
\end{array}
```

The exception is inside `\mbox{}` (see below), where content IS in text mode and needs
`$...$` to re-enter math mode.

### `\colorbox` requires `\mbox{}` wrapper

`\colorbox` is a **text-mode** command. Using it directly inside `array` (math mode)
causes "Missing $ inserted" errors. Wrap it in `\mbox{}`:

**Correct:**

```latex
\mbox{\colorbox{yellow!20}{$0$}}
```

**Wrong:**

```latex
\colorbox{yellow!20}{$0$}
```

Inside `\mbox`, content is in text mode, so the cell value must be wrapped in `$...$` to
re-enter math mode.

### `xcolor` package

`\colorbox` requires the `xcolor` package. The `tex_template.tex` and
`tex_tabular_template.tex` templates do NOT include `xcolor`. Use
`tex_color_template.tex` (which adds `xcolor`) or include `\usepackage{xcolor}` in a
dedicated template for color-dependent TeX tests.

## Template Catalog

Project templates live in `data/`. Each serves a specific purpose:

| Template | Packages | Math Wrapper | Use Case |
| --- | --- | --- | --- |
| `tex_template.tex` | `nicematrix`, `graphicx` | `\[ ... \]` | General TeX tests, nicematrix-dependent content |
| `tex_color_template.tex` | `xcolor`, `nicematrix`, `graphicx` | `\[ ... \]` | Color-dependent TeX tests (`\colorbox`, `\cellcolor`) |
| `tex_tabular_template.tex` | `amsmath` | none | Non-matrix TeX (tabular, LL tables) |
| `tex_tikz_template.tex` | `standalone`, `tikz`, `graphdrawing` | none (standalone) | Tikz graphics compilation |
| `tex_flpqdot_probe_template.tex` | `standalone`, `tikz`, `graphs, graphdrawing, quotes`, flpqdot router hook | none (standalone) | flpqdot router probe tests (layout metrics via `\typeout` lines) |
| `tex_summary_template.tex` | `xcolor[table]`, `nicematrix`, `tikz`, `amsmath`, `amssymb`, `babel`, `graphicx` | none (full document) | Merged summary generation |

When adding color-dependent TeX output, create a dedicated template rather than
modifying an existing shared template (see below).

## Golden Test Template Fragility

Golden tests that use `GoldenHelpers.wrapInTemplate` embed the full template preamble
into the reference file. **Changing a template silently breaks ALL golden tests that
embed it** — the reference files compare the full document content, including the
preamble.

**Rule**: when new TeX output requires an additional package (e.g., `xcolor` for `\colorbox`):

1. Create a **dedicated template** with the new package.
2. Use that template in `compileTexStringWithTemplate` for compilation-only tests.
3. Do NOT add packages to shared templates (`tex_template.tex`, `tex_tabular_template.tex`) unless you are prepared to regenerate all dependent golden reference files.
4. If golden tests are affected, run `verifyGolden` tests to regenerate the reference files, then visually inspect the diff before committing.

Reference implementation: `tex_color_template.tex` was created for GLL descriptor table
tests to avoid breaking MatrixTeX, ValiantTrace, and PathIndex golden tests that embed
`tex_template.tex`.

## Tikz `\graph` with `graphdrawing` Library

### Required Preamble

```latex
\documentclass{standalone}
\usepackage{amsmath}
\usepackage{tikz}
\usetikzlibrary{graphs, graphdrawing, quotes, babel, arrows.meta}
\usegdlibrary{layered}
\tikzset{>={Latex[width=3mm,length=3mm]}}
\begin{document}
__CONTENT__  % \begin{tikzpicture}...\end{tikzpicture}
\end{document}
```

The `standalone` class crops the PDF to the content bounding box, suitable for
`\includegraphics` inclusion.

### `quotes` Library for Edge Labels

Edge labels in `\graph` syntax (`s0 ->["label"] s1`) require the `quotes` Tikz library.
Without it, Tikz interprets the quoted string as an unknown key.

Additionally, the `babel` library is required when the document uses the `babel`
package (e.g., for multi-language support like Russian). Without `babel`, `"` within
`[...]` is misinterpreted by the active `"` character, causing silent compilation
failures.

### `amsmath` Required for `aligned` Environment

When state content uses `\begin{aligned}...\end{aligned}` (e.g., for LR item rendering),
the template preamble must include `\usepackage{amsmath}`. Without it, lualatex produces
"Environment aligned undefined" errors.

### Loop Edges with `loop above`

Self-loops should use the `loop above` edge attribute for proper visual placement:

```tikz
s3 ->["a",loop above] s3;
```

Without `loop above`, self-loops are drawn as small curves that may overlap with the
node label.

### Arrow Head Size

Enlarged arrow heads require the `arrows.meta` library:

```latex
\usetikzlibrary{arrows.meta}
\tikzset{>={Latex[width=3mm,length=3mm]}}
```

### `as` Key Escaping

The `as={...}` key in Tikz `\graph` nodes takes raw text. When the content is LaTeX math (`$...$`), it must **not** be escaped — backslashes, underscores, etc., are interpreted by LaTeX. Escaping would break LaTeX commands.

Only plain-text edge labels (not in math mode) should be escaped for LaTeX special
characters.

### lualatex Required for `graphdrawing`

The `graphdrawing` library with `layered` algorithm requires lualatex (Lua-based graph
layout engine). pdflatex cannot use `\usegdlibrary{layered}`.

### Multiple Edges with `layered layout`

The `layered layout` algorithm works well with simple `string` state identifiers (`s0`,
`s1`, ...). Using complex identifiers (containing dots, commas, or special characters) in
node names may cause parsing issues. Always use simple alphanumeric identifiers and put
content in `as={...}`.

### The `dot layout` Key (flpqdot Library)

Generated graphs use the project-owned `dot layout` key (from the flpqdot library,
loaded via `\usegdlibrary{flpqdot}`) rather than the stock `layered layout`. It reuses
the same Sugiyama pipeline but swaps in a channel edge router for smooth, non-overshooting,
`dot`-like Bézier edges. See [the flpqdot library](../tikz-gd-library.md) for the design and the
loading/staging mechanism.

### Gotchas Writing Lua in `\directlua{...}`

When TeX reads a `\directlua{...}` argument it **tokenizes** the content, so a few Lua
idioms that are fine in a `.lua` module break inside the block:

- `%` starts a **TeX comment** (it comments out the rest of the line and can leave the
  group unclosed). Avoid `%` in the block — build strings by concatenation
  (`"x=" .. tostring(n)`) instead of `string.format("x=%d", n)`.
- `\X` (e.g. `"\1"`) is read as a **TeX control sequence** (`Undefined control sequence`). Use a non-backslash separator (e.g. `"->"`) for string keys.
- The `#` (length) operator is **unreliable** in a `\directlua` block (errors with
  `attempt to get length of a number value`) even though it works in module functions.
  Count with `pairs`/`ipairs` instead.
- `--` **line comments break the chunk** (TeX tokenizes the `--` and the following text,
  corrupting the Lua so it fails with `'end' expected near <eof>`). Keep explanatory
  comments in a `%` TeX comment *outside* the block, not inside it.

Lua loaded via `require` (a `.lua` module) is not TeX-tokenized, so none of these apply
there — the flpqdot library modules use `#`, `--`, and `"\1"` freely.

## See Also

- [Testing](testing.md)
- [Tools](tools.md)
