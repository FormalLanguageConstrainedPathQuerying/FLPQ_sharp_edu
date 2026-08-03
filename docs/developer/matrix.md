# Matrix Module

**Tags:** data-structure, matrix, linear-algebra, sparse-matrix
**Kind:** data-structure
**Module:** Matrix
**Source:** `src/FLPQ.LinearAlgebra/Matrix.fs`
**Depends on:** _(none)_
**Used by:** LinearAlgebra, Graph, Automaton, Valiant, Cyk, PathIndex, SPPF, GLL, RNGLR, all RPQ
**Book reference:** Chapter 1, Section 07_MatricesAndVectors.tex

> **Abstract:** Provides a generic matrix type `Matrix<'a>` wrapping a standard 2D array with explicit row and column counts. Supports creation (from functions, constants, arrays, diagonals), element-wise transformation (map, map2), folding, transposition, column reduction, and TeX printing with cell highlighting and submatrix block borders via nicematrix. Foundation type for all FLPQ modules.

## Contents

- [Data Structure](#data-structure)
- [Type Definition](#type-definition)
- [Module Functions](#module-functions)
- [TeX Printing](#tex-printing)
- [Design Decisions](#design-decisions)
- [See Also](#see-also)

## Data Structure

`Matrix<'a>` is a record wrapping a standard 2D array:

```fsharp
type Matrix<'a> = { rows: int; cols: int; data: 'a[,] }
```

The matrix is the fundamental data structure in the book — all graph adjacency matrices, automaton transition matrices, parsing tables, and path index matrices are instances of this type. Structural equality (record equality) ensures two matrices with the same dimensions and equal data are equal — essential for testing.

## Type Definition

- **`rows`, `cols`**: explicit dimensions stored alongside `data`. Avoids recomputing `Array2D.length1`/`length2` on every access.
- **`data`**: a standard `'a[,]` 2D array holding cell values.
- **Structural equality**: F# records have structural equality by default, so two matrices compare equal when their dimensions and data match.

## Module Functions

### Dimension Accessors
```fsharp
val rows: Matrix<'a> -> int
val cols: Matrix<'a> -> int
```

### Creation Functions
```fsharp
val create: rows:int -> cols:int -> f:(int -> int -> 'a) -> Matrix<'a>
val init: rows:int -> cols:int -> value:'a -> Matrix<'a>
val ofArray2D: arr:'a[,] -> Matrix<'a>
val diagonal: size:int -> indices:Set<int> -> one:'a -> zero:'a -> Matrix<'a>
val reduceByColumn: op:('a -> 'a -> 'a) -> init:'a -> m:Matrix<'a> -> 'a[]
```

### Transformation Functions
```fsharp
val map: f:('a -> 'b) -> Matrix<'a> -> Matrix<'b>
val map2: f:('a -> 'b -> 'c) -> a:Matrix<'a> -> b:Matrix<'b> -> Matrix<'c>
val fold: folder:('acc -> 'a -> 'acc) -> state:'acc -> m:Matrix<'a> -> 'acc
val transpose: Matrix<'a> -> Matrix<'a>
```

### Indexed Operations
```fsharp
val map2i: f:(int -> int -> 'a -> 'b -> 'c) -> a:Matrix<'a> -> b:Matrix<'b> -> Matrix<'c>
val mxmi:
    op_add:(int -> int -> 's -> 's -> 's) ->
    op_mult:(int -> int -> int -> 'a -> 'b -> 's) ->
    zero:'s ->
    a:Matrix<'a> ->
    b:Matrix<'b> ->
    Matrix<'s>
```

`map2i` is like `map2` but passes row and column indices to `f`: `f i j a[i,j] b[i,j]`.

`mxmi` computes indexed matrix multiplication `C = A × B`. For each cell `C[i,j]`:
- Iterates over inner dimension `k`
- Calls `op_mult i k j a[i,k] b[k,j]` to produce a term
- Folds terms with `op_add i j acc term` starting from `zero`

Precondition: `a.cols = b.rows`. Throws `ArgumentException` on dimension mismatch.

## TeX Printing

### `toTeX`
```fsharp
val toTeX:
    showRowNumbers: bool ->
    showColNumbers: bool ->
    cellPrinter: ('a -> string) ->
    m: Matrix<'a> ->
    string
```
Generates a LaTeX string using the `pNiceMatrix` environment from the nicematrix package. Cells are separated by ` & `, rows by ` \\`. Numbering is 1-based (standard matrix notation).

### `toTeXStyled`
```fsharp
type Highlight = { row: int; col: int; color: string }
type SubmatrixBlock =
    { startRow: int; startCol: int; rowCount: int; colCount: int
      borderColor: string option; fillColor: string option }

val toTeXStyled:
    showRowNumbers: bool -> showColNumbers: bool ->
    cellPrinter: ('a -> string) -> m: Matrix<'a> ->
    highlights: Highlight list -> blocks: SubmatrixBlock list -> string
```
Extended TeX printing with cell highlighting and submatrix block borders using nicematrix `\Block` commands.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Record wrapper over raw 2D array | Explicit `rows`/`cols` avoid recomputation; structural equality aids testing |
| `rows`/`cols` as functions, not direct field access | Consistent functional API surface; can be replaced with computed properties later |
| `map2` throws on dimension mismatch | Unambiguous error signaling; caller must ensure matching dimensions |
| 1-based numbering in TeX output | Standard matrix notation (row 1, column 1 is the top-left element) |
| Cell printer as function parameter | Maximum flexibility: caller controls how any element type renders in TeX |
| `\Block` uses `{rows-cols}` dimension syntax | Compatible with nicematrix v6+ (2024+) |

## See Also

- [LinearAlgebra module](linear-algebra.md) — mxm, kron over Matrix type
- [BooleanDecomposition module](boolean-decomposition.md) — decompose/recompose
- [Graph module](graph.md) — uses Matrix for edge storage
- [PathIndex module](path-index.md) — K×K matrix over Set entries
