# Kronecker CFPQ Step Visualization

**Tags:** visualization, cfpq, kronecker-product, matrix, step-visualization
**Kind:** visualization
**Module:** KroneckerStepViz
**Source:** `src/FLPQ.Printers/KroneckerStepViz.fs`
**Depends on:** MatrixTeX, StepTeX, KroneckerCfpq (FLPQ.CFPQ)
**Used by:** KroneckerCfpqRunner (FLPQ.Cli)

> **Abstract:** TeX rendering of one productive Kronecker CFPQ fixed-point iteration as two
> stacked lines. Line 1 is the full Kronecker product `$ M_1 \otimes M_2 = K $` (the RSM
> transition matrix and the current edge-label matrix with set-of-symbols cells, and the Boolean
> product mask, the step's newly derived cells highlighted); line 2 is the MS-BFS initial (seed)
> and final (visited) fronts, decoded with `(q_i, u_j)` rows and `(q_i, u_j)` columns
> (product-state pairs, the row's nonterminal replaced by its start state), the final front with
> its accept columns highlighted. The renderer is pure — it only formats the step's stored
> matrices and passes the graph figure through (built by the caller via `RpqGraphViz`).

## Contents

- [Data Structure](#data-structure)
- [Module Functions](#module-functions)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Data Structure

```fsharp
type KroneckerVisualizationStep =
    { Kron: string; Fronts: string; GraphDot: string; GraphTikz: string }
```

`Kron` is the line-1 body (the product row, wrapped in a shrink-to-fit, top-aligned adjustbox);
`Fronts` is the line-2 body (the two stacked fronts). `GraphDot`/`GraphTikz` are the pass-through
graph figures.

## Module Functions

### `symbolSetCellToTeX: ('t -> string) -> (Nonterminal<'nt> -> string) -> Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t,'nt>>>> -> string`

One edge-label-matrix cell as a set of symbols: `None` → `\cdot` (a dot, like empty cells in the
other matrices); `Some set` → the comma-joined symbols, terminals via the terminal printer and
nonterminals in `\text{...}` (`\varepsilon` for `AEpsilon`).

### `kronBody: ('t -> string) -> (Nonterminal<'nt> -> string) -> int -> Set<int * int> -> Matrix<Option<...>> -> Matrix<Option<...>> -> Matrix<bool> -> string`

The line-1 body: `$ M_1 \otimes M_2 = K $`. `M_1` (RSM transitions, `q_i` labels) and `M_2`
(edge-label matrix, `v_i` labels) use `symbolSetCellToTeX`; `K` is the Boolean product mask
(`\bullet`/`\cdot`) with the cells that turned on for the first time this iteration (the `K`
diff against the previous iteration's `K`, passed as `kNewCells`) highlighted yellow. `K` is a
product-state matrix (RSM state × vertex), so its row/column labels are the product-state pairs
`(q_i, v_j)` — the same pair labeling as the fronts, with multi-digit subscripts braced
(`q_{15}, v_{15}`); `M_1`/`M_2` keep their per-space `q_i`/`v_i` labels (they live in a single
index space). `K` is qn × qn = a q × q grid of n × n blocks (the Kronecker product structure),
so `kronBody` draws thin block-splitting rules (a vertical rule after every n-th data column and
a horizontal rule after every n-th data row) to make the block structure visible. The whole row
is wrapped in `StepTeX.wrapFormula`.

### `frontsBody: CfpqKronStep<'nt,'t> -> int -> string`

The line-2 body: the initial (seed) front above the final (visited) front. Rows and columns are
product-state pairs `(q_i, u_j)` (`q_<s>, v_<v>`); a row is the BFS from source pair
`(start(A), v)`, labeled with the nonterminal `A` replaced by its start state `q_i` (consistent
with the column's `(q_i, u_j)` labels). The final front highlights its RSM final-state cells
(the filled cells in the `FinalStateCols` columns — the accept-reading of the MS-BFS result)
yellow.

### `renderStep: ('t -> string) -> (Nonterminal<'nt> -> string) -> int -> CfpqKronStep<'nt,'t> -> Set<int * int> -> string -> string -> KroneckerVisualizationStep`

`renderStep terminal ntName n step kNewCells graphDot graphTikz` builds the two line bodies
(`kronBody`, `frontsBody`) and pairs them with the caller-supplied graph figure. `kNewCells` is
the set of `K` cells that turned on for the first time this iteration (the diff against the
previous iteration's `K`); the caller (the CLI writer) walks the steps in order to compute it.
The final front highlights its own `FinalStateCols` cells (no parameter needed).

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Pure renderer (no I/O, no algorithm recomputation) | Mirrors the Belyanin/Arroyuelo seam: the step already carries every matrix; the renderer only formats. The graph figure is passed in so the renderer stays decoupled from `CfpqInput`/`RpqGraphViz` |
| Reuse `MatrixTeX.toTeXStyled`, `StepTeX` | No new matrix primitive for the operands/fronts: labeled matrices with per-cell highlights via `MatrixTeX`, shrink-to-fit wrapping via `StepTeX`. The K mask is built directly (via `kMatrixWithRules`) because `MatrixTeX.toTeXStyled` has no block-rule support |
| `M_1`/`M_2` set cells are "for visualization only" | The book shows the product operands with their symbol-set cells; `M_1` is constant and `M_2` is the per-iteration snapshot, so the operands are self-contained in the step |
| Decoded `(q_i, u_j)` front labels (rows and columns) | The raw product indices are opaque; decoding makes the MS-BFS fronts readable. Rows are the BFS source pairs `(start(A), v)` labeled `(q_i, u_j)` (the nonterminal replaced by its start state, consistent with the columns), so a row is directly readable as the product state the BFS starts from |
| Final front highlights the RSM final-state cells | The MS-BFS accept-reading is "the path language is recognized iff some (RSM final state, vertex) product state is reached". `frontsBody` highlights the filled cells of the final front whose column is in `FinalStateCols` (the (final f, vertex v) product-state columns) — per-cell, so only the actually-reached final states are lit, not the whole columns. |
| `K` highlights the newly filled cells (`kNewCells`) | The reader wants to see what the iteration added to the product mask. `kNewCells` = `trueCells(K) \ trueCells(prev K)`. (An earlier version highlighted "cells derived by the changed cells" via `M1.[p, start(A)]`; that set is structurally empty for block-structured RSMs — every nonterminal is its own block with no transitions into a block start — so the diff-vs-previous signal is the one that is actually visible.) |
| Multi-digit subscripts braced (`q_{15}`) | Unbraced `q_15` renders as `q` with subscript `1` followed by a `5`; bracing renders the full index as the subscript |
| `K` draws block-splitting rules (lines only) | `K` is qn × qn = a q × q grid of n × n blocks; the reader wants to see the Kronecker product structure. Thin rules (no fill) separate the blocks: a vertical `\vrule` at the right edge of the last column of every block except the last (in the header row) and a horizontal `\noalign{\hrule}` before the first row of every block except the first. The K body is built directly (not via `MatrixTeX.toTeXStyled`, which has no rule support). The header row must carry an **empty corner cell** (the pNiceMatrix top-left, matching `MatrixTeX.toTeXStyled`) — without it the first column label is consumed as the corner and every label shifts right by one. Verified by the step-template TeX compile test |

## See Also

- [Kronecker CFPQ algorithm](kronecker-cfpq.md) — produces the steps this module renders
- [Kronecker CFPQ CLI runner](kronecker-cfpq-runner.md) — writes the per-step artifacts
- [CfpqMatrixTeX](cfpq-matrix-tex.md) — the sibling CFPQ matrix renderer
- [MS-BFS module](msbfs.md) — the `msBfsWithTrace` that produces the fronts
