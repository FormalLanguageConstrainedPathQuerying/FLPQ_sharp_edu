# CfpqMatrixTeX Module

**Tags:** visualization, tex, cfpq, matrix, azimov
**Kind:** visualization
**Module:** CfpqMatrixTeX
**Source:** `src/FLPQ.Printers/CfpqMatrixTeX.fs`
**Depends on:** Matrix (FLPQ.LinearAlgebra), MatrixTeX, FLPQ.Languages (Nonterminal, Trans), FLPQ.CFPQ (CfpqCell)
**Used by:** Azimov CFPQ CLI runners (task 293)
**Book reference:** Chapter 12, Section sec:CFPQ_MatrixBased (per-nonterminal matrices T_A)

> **Abstract:** TeX rendering of Azimov's per-nonterminal CFPQ matrices. Generic over the semiring element type and the nonterminal payload — the module takes an element printer (and a nonterminal-name printer for steps) and renders `Map<Nonterminal<'nt>, Matrix<'elem option>>` values, so it depends on FLPQ.CFPQ only for the `CfpqCell` cell-address record, not for any semiring element type. One nonterminal matrix is a pNiceMatrix with `v_i` row/column labels wrapped in adjustbox; one fixed-point iteration stacks all nonterminal matrices vertically with a bold name above each and the cells changed in that iteration highlighted yellow. Also renders extracted edge sequences as text (`v_0 -a-> v_1`).

## Contents

- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Module Functions

```fsharp
val cellToTeX: ('elem -> string) -> 'elem option -> string
val matrixToTeX: ('elem -> string) -> Matrix<'elem option> -> string
val pathToText: ('t -> string) -> Trans<'t> list -> string
val stepToTeX: (Nonterminal<'nt> -> string) -> ('elem -> string) -> Map<Nonterminal<'nt>, Matrix<'elem option>> -> CfpqCell<'nt> list -> string
```

- `cellToTeX printer cell` — one matrix cell: `None` renders as `\varnothing`, `Some e` through the element printer. Each runner supplies its own printer: `\bullet`/`\cdot` for reachability, `v_k^h` for singlePath, split-vertex lists (with `\bullet` for a direct derivation) for allPaths.
- `matrixToTeX printer m` — one nonterminal matrix as a pNiceMatrix with `v_i` row and column labels, wrapped in adjustbox so wide matrices shrink to the text width. Delegates to `MatrixTeX.toTeXStyled`.
- `pathToText labelPrinter path` — an edge sequence as a list of `Trans` records: `[ {From=2;Label="a";To=0}; {From=0;Label="b";To=3} ]` → `v_2 -a-> v_0 -b-> v_3`; the empty sequence renders as `\varepsilon`. Edge labels go through `labelPrinter` (the runners pass `id`).
- `stepToTeX ntName printer matrices changedCells` — all nonterminal matrices of one fixed-point iteration, stacked vertically: a bold nonterminal name (through `ntName`) above each matrix, with the cells from `changedCells` highlighted (`\cellcolor{yellow}` via `MatrixTeX.toTeXStyled` highlights). The output is the content of a step's `table.tex`.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Generic over `'elem` and `'nt` with printer parameters | The three CFPQ semirings have different cell types; one renderer serves all runners. The only FLPQ.CFPQ dependency is the `CfpqCell` record (a cell address), so the module never names a semiring element type or a concrete nonterminal payload |
| Empty cell renders as `\varnothing` | The book's T_A matrices show empty cells explicitly; `\varnothing` distinguishes "no path" from a present element |
| `v_i` row/column labels | Matches the book's graph figures and matrix headers |
| adjustbox wrapping per matrix | The matrices are n×n with vertex labels; they must fit the text width in the generated PDF |
| Step = stacked matrices with changed cells highlighted | Mirrors the book's T₀…T₁₃ presentation: every nonterminal matrix is shown at each iteration, and the highlights mark what the iteration added |
| `pathToText` emits plain text, not TeX | It is used in `result.tex` lines outside math mode; the runners compose it with vertex names as text |

## See Also

- [Azimov CFPQ algorithm](azimov-cfpq.md) — produces the matrices and steps this module renders
- [CfpqSemiring module](cfpq-semiring.md) — the semirings whose elements the runners print
- [MatrixTeX module](matrix-tex.md) — the pNiceMatrix rendering with highlights this module wraps
