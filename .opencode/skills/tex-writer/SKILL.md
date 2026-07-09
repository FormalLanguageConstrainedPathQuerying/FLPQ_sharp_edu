---
name: tex-writer
description: Use when generating TeX/LaTeX output, Tikz graphics, or compiling LaTeX documents. Covers nicematrix \Block syntax, lualatex error detection, Tikz \graph with graphdrawing, preamble requirements, and compilation verification.
---

# TeX and LaTeX Writing

## nicematrix v6+ (TeX Live 2024+) `\Block` Syntax

**Problem**: nicematrix versions 6.x+ use a different `\Block` syntax. The old positional syntax `\Block[draw=red]{r1-c1-r2-c2}{}` was removed.

**New syntax**: `\Block[draw=red]{rows-cols}{content}`
- `rows`: number of rows the block spans
- `cols`: number of columns the block spans
- `content`: the content displayed in the block (overrides the top-left cell content)

**Placement**: `\Block` must be placed at the top-left cell of the block within the matrix, not before the matrix.

Reference implementation: `Matrix.fs` `toTeXStyled` function, which generates `\Block[draw=color]{rowCount-colCount}{styledCellContent}` at the appropriate cell.

## lualatex Error Detection

**Problem**: `lualatex -interaction=nonstopmode` can exit with code 0 even when the TeX source contains undefined control sequences or errors. Relying solely on the exit code misses compilation failures.

**Solution**: Always triple-check:

1. Exit code is 0
2. No line in stdout starts with `!` (TeX error marker) or contains `Fatal error` or `Error:`
3. The output PDF exists and is non-empty (size > 0)

This triple check is implemented in `FLPQ.Printers.ExternalTools.latexSucceeded`. See `src/FLPQ.Printers/ExternalTools.fs`.

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

The `standalone` class crops the PDF to the content bounding box, suitable for `\includegraphics` inclusion.

### `quotes` Library for Edge Labels

Edge labels in `\graph` syntax (`s0 ->["label"] s1`) require the `quotes` Tikz library. Without it, Tikz interprets the quoted string as an unknown key.

Additionally, the `babel` library is required when the document uses the `babel` package (e.g., for multi-language support like Russian). Without `babel`, `"` within `[...]` is misinterpreted by the active `"` character, causing silent compilation failures.

### `amsmath` Required for `aligned` Environment

When state content uses `\begin{aligned}...\end{aligned}` (e.g., for LR item rendering), the template preamble must include `\usepackage{amsmath}`. Without it, lualatex produces "Environment aligned undefined" errors.

### Loop Edges with `loop above`

Self-loops should use the `loop above` edge attribute for proper visual placement:

```tikz
s3 ->["a",loop above] s3;
```

Without `loop above`, self-loops are drawn as small curves that may overlap with the node label.

### Arrow Head Size

Enlarged arrow heads require the `arrows.meta` library:

```latex
\usetikzlibrary{arrows.meta}
\tikzset{>={Latex[width=3mm,length=3mm]}}
```

### `as` Key Escaping

The `as={...}` key in Tikz `\graph` nodes takes raw text. When the content is LaTeX math (`$...$`), it must **not** be escaped — backslashes, underscores, etc., are interpreted by LaTeX. Escaping would break LaTeX commands.

Only plain-text edge labels (not in math mode) should be escaped for LaTeX special characters.

### lualatex Required for `graphdrawing`

The `graphdrawing` library with `layered` algorithm requires lualatex (Lua-based graph layout engine). pdflatex cannot use `\usegdlibrary{layered}`.

### Multiple Edges with `layered layout`

The `layered layout` algorithm works well with simple `string` state identifiers (`s0`, `s1`, ...). Using complex identifiers (containing dots, commas, or special characters) in node names may cause parsing issues. Always use simple alphanumeric identifiers and put content in `as={...}`.
