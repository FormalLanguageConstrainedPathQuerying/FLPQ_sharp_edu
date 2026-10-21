# Kronecker CFPQ Step Visualization

**Tags:** visualization, cfpq, kronecker-product, matrix, step-visualization
**Kind:** visualization
**Module:** KroneckerStepViz
**Source:** `src/FLPQ.Printers/KroneckerStepViz.fs`
**Depends on:** MatrixTeX, PathSemiringTeX, StepTeX, KroneckerCfpq (FLPQ.CFPQ)
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

One edge-label-matrix cell as a set of symbols: `None` → `\varnothing`; `Some set` → the
comma-joined symbols, terminals via the terminal printer and nonterminals in `\text{...}`
(`\varepsilon` for `AEpsilon`).

### `kronBody: ('t -> string) -> (Nonterminal<'nt> -> string) -> CfpqCell<'nt> list -> Matrix<Option<...>> -> Matrix<Option<...>> -> Matrix<bool> -> string`

The line-1 body: `$ M_1 \otimes M_2 = K $`. `M_1` (RSM transitions, `q_i` labels) and `M_2`
(edge-label matrix, `v_i` labels) use `symbolSetCellToTeX`; `K` is the Boolean product mask
(`\bullet`/`\cdot`) with the step's `changedCells` highlighted yellow. Row/column labels are
`q_i`/`v_i` with multi-digit subscripts braced (`q_{15}`, not `q_15`). The whole row is wrapped
in `StepTeX.wrapFormula`.

### `frontsBody: CfpqKronStep<'nt,'t> -> int -> string`

The line-2 body: the initial (seed) front above the final (visited) front. Rows and columns are
product-state pairs `(q_i, u_j)` (`q_<s>, v_<v>`); a row is the BFS from source pair
`(start(A), v)`, labeled with the nonterminal `A` replaced by its start state `q_i` (consistent
with the column's `(q_i, u_j)` labels). The final front highlights its `FinalStateCols` columns
yellow.

### `renderStep: ('t -> string) -> (Nonterminal<'nt> -> string) -> int -> CfpqKronStep<'nt,'t> -> string -> string -> KroneckerVisualizationStep`

`renderStep terminal ntName n step graphDot graphTikz` builds the two line bodies (`kronBody`,
`frontsBody`) and pairs them with the caller-supplied graph figure.

## Design Decisions

| Decision | Rationale |
| --- | --- |
| Pure renderer (no I/O, no algorithm recomputation) | Mirrors the Belyanin/Arroyuelo seam: the step already carries every matrix; the renderer only formats. The graph figure is passed in so the renderer stays decoupled from `CfpqInput`/`RpqGraphViz` |
| Reuse `MatrixTeX.toTeXStyled`, `PathSemiringTeX`, `StepTeX` | No new matrix primitive: labeled matrices with column highlights via `MatrixTeX`, the Boolean mask via `PathSemiringTeX.boolMatrixToTeXBody`, and shrink-to-fit wrapping via `StepTeX` |
| `M_1`/`M_2` set cells are "for visualization only" | The book shows the product operands with their symbol-set cells; `M_1` is constant and `M_2` is the per-iteration snapshot, so the operands are self-contained in the step |
| Decoded `(q_i, u_j)` front labels (rows and columns) | The raw product indices are opaque; decoding makes the MS-BFS fronts readable. Rows are the BFS source pairs `(start(A), v)` labeled `(q_i, u_j)` (the nonterminal replaced by its start state, consistent with the columns), so a row is directly readable as the product state the BFS starts from |
| Final front highlights `FinalStateCols` | The MS-BFS accept-reading is "reachable iff some final-state column is visited"; highlighting those columns makes the reading visible |
| `K` highlights the step's changed cells | The newly derived product edges (this iteration's nonterminal derivations) are highlighted in the product mask so the reader can see which cells the iteration added |
| Multi-digit subscripts braced (`q_{15}`) | Unbraced `q_15` renders as `q` with subscript `1` followed by a `5`; bracing renders the full index as the subscript |

## See Also

- [Kronecker CFPQ algorithm](kronecker-cfpq.md) — produces the steps this module renders
- [Kronecker CFPQ CLI runner](kronecker-cfpq-runner.md) — writes the per-step artifacts
- [CfpqMatrixTeX](cfpq-matrix-tex.md) — the sibling CFPQ matrix renderer
- [MS-BFS module](msbfs.md) — the `msBfsWithTrace` that produces the fronts
