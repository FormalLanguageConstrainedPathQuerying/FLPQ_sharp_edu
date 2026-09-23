# PathSemiringTeX Module

**Tags:** visualization, tex, rpq, matrix, path-semiring
**Kind:** visualization
**Module:** PathSemiringTeX
**Source:** `src/FLPQ.Printers/PathSemiringTeX.fs`
**Depends on:** Matrix (FLPQ.LinearAlgebra), MatrixTeX, ParsingTableTeX
**Used by:** Arroyuelo RPQ summary/steps (task 280), Belyanin RPQ summary/steps (task 281)
**Book reference:** Chapter 11, Section sec:RPQ_BFS (matrix cells holding vertex sequences)

> **Abstract:** TeX rendering of path semiring matrices — the book's RPQ working matrices whose cells hold sets of vertex paths instead of booleans. A cell renders as a TeX set of vertex tuples (`\{(v_0, v_1), (v_0, v_2)\}`); an empty cell renders as `\cdot` (Valiant's convention). The full matrix wraps `MatrixTeX.toTeXStyled` with adjustbox so wide matrices shrink to the text width.

## Contents

- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [Book Reference](#book-reference)
- [See Also](#see-also)

## Module Functions

```fsharp
val pathToTeX: int list -> string
val pathSetCellToTeX: Set<int list> -> string
val matrixToTeX: (int -> string) -> (int -> string) -> Matrix<Set<int list>> -> string
```

- `pathToTeX p` — a vertex sequence as a TeX tuple: `[0; 2; 3]` → `(v_0, v_2, v_3)`. Vertex names are math-mode subscripts (`v_0`), matching the book's graph figures.
- `pathSetCellToTeX cell` — a set of paths as a TeX set: `\{(v_0, v_1), (v_0, v_2)\}`; the empty set renders as `\cdot`. Reuses `ParsingTableTeX.setToTeX` with `pathToTeX` as the item printer, so set formatting (braces, ordering) is shared with parsing tables.
- `matrixToTeX rowLabel colLabel m` — a path semiring matrix with vertex row/column headers (e.g. `fun i -> sprintf "v_%d" i`), wrapped in adjustbox. Delegates to `MatrixTeX.toTeXStyled` with `useAdjustbox = true`, `useRectangleColor = false`.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Cells are math-mode TeX only (no document-level commands) | Cell content is placed inside pNiceMatrix cells and the summary's math wrappers; document-level commands would break both contexts |
| Empty cell renders as `\cdot` | Matches Valiant's convention for empty table cells, keeping RPQ matrices visually consistent with parsing tables |
| Vertex names are math-mode subscripts (`v_0`) | Matches the book's graph figures and matrix headers; requires `MatrixTeX.toTeXStyled` to emit custom row/column labels as-is (see matrix.md) |
| Set formatting reuses `ParsingTableTeX.setToTeX` | One source of truth for "render a set of items as a TeX set" across parsing tables and RPQ matrices |
| adjustbox wrapping | Path cells are wide (vertex tuples); the book's matrices must fit the text width in the generated PDF |

## Book Reference

Chapter 11, `02_BFS.tex`: Section sec:RPQ_BFS — the path semiring working matrix whose cells hold sets of vertex sequences; figure in subsec:RPQ_BFS_example shows the same layout.

## See Also

- [PathSemiring module](path-semiring.md) — the semiring this module renders
- [Matrix module (TeX printing)](matrix.md#tex-printing) — pNiceMatrix rendering, label printer contract
- [FLPQ.Printers hub](FLPQ.Printers.md) — also hosts ParsingTableTeX (shared set-cell formatting)
