# Matrix Module

## Type Definition

```fsharp
type Matrix<'a> = { rows: int; cols: int; data: 'a[,] }
```

A generic matrix type wrapping a standard 2D array with explicit row and column counts.

### Design Rationale

- **Explicit dimensions**: `rows` and `cols` fields store dimensions alongside `data`. This avoids recomputing `Array2D.length1`/`Array2D.length2` on every access and makes dimension invariants visible at the type level.
- **Structural equality**: F# records have structural equality. Two matrices with the same dimensions and equal data are equal — useful for testing.
- **Named fields over tuples**: Following project conventions, fields have explicit names rather than relying on positional access.

## Module Functions

All functions live in the `Matrix` module at the same level as the type.

### Dimension Accessors

```fsharp
val rows: Matrix<'a> -> int
val cols: Matrix<'a> -> int
```

Return the number of rows or columns respectively. Provided as functions rather than direct field access for consistency with the functional API surface.

### Creation Functions

#### `create`
```fsharp
val create: rows:int -> cols:int -> f:(int -> int -> 'a) -> Matrix<'a>
```
Creates a matrix of the given dimensions. The function `f i j` is called for each cell `(i, j)` where `i ∈ [0, rows)` and `j ∈ [0, cols)`. The result is the cell value at that position.

#### `init`
```fsharp
val init: rows:int -> cols:int -> value:'a -> Matrix<'a>
```
Creates a matrix of the given dimensions filled with the constant `value` in every cell. Defined as `create rows cols (fun _ _ -> value)`.

#### `ofArray2D`
```fsharp
val ofArray2D: arr:'a[,] -> Matrix<'a>
```
Wraps an existing 2D array into a `Matrix<'a>`. Dimensions are inferred from the array. The array is **not** copied — the result shares the underlying array.

#### `diagonal`
```fsharp
val diagonal: size:int -> indices:Set<int> -> one:'a -> zero:'a -> Matrix<'a>
```
Creates a diagonal matrix of the given size. Positions `(i, i)` for `i ∈ indices` receive `one`, all other positions receive `zero`. For Boolean matrices, `diagonal n selected true false` produces a selector matrix that, when multiplied with an adjacency matrix, preserves only rows/columns of selected vertices. Used by `Graph.filterOutgoing` and `Graph.filterIncoming`.

#### `reduceByColumn`
```fsharp
val reduceByColumn: op:('a -> 'a -> 'a) -> init:'a -> m:Matrix<'a> -> 'a[]
```
Reduces each column to a single value using the given binary operation. Returns an array of length `m.cols`. Column `j` accumulates: `op(init, m.data[0,j]), op(result, m.data[1,j]), ...`. For Boolean matrices, `reduceByColumn (||) false` performs column-wise OR-reduction, collapsing a `k × n` MS-BFS result into an `n`-element array indicating which columns have any true in any row. Used by `Nfa.intersect` to find product states reachable from any start pair.

### Transformation Functions

#### `map`
```fsharp
val map: f:('a -> 'b) -> Matrix<'a> -> Matrix<'b>
```
Element-wise transformation. Applies `f` to every cell of the input matrix, producing a new matrix of the same dimensions. Implemented via `Array2D.map`.

#### `map2`
```fsharp
val map2: f:('a -> 'b -> 'c) -> a:Matrix<'a> -> b:Matrix<'b> -> Matrix<'c>
```
Element-wise binary operation on two matrices. Applies `f` to corresponding cells `a.data[i,j]` and `b.data[i,j]`.

**Preconditions**:
- Both matrices must have the same dimensions. If not, throws `ArgumentException` with a message describing the mismatch.

**Postcondition**:
- Result has dimensions `a.rows × a.cols`.

#### `fold`
```fsharp
val fold: folder:('acc -> 'a -> 'acc) -> state:'acc -> m:Matrix<'a> -> 'acc
```
Left-to-right, top-to-bottom fold over all matrix cells. Applies `folder` to the accumulator and each cell in row-major order.

#### `transpose`
```fsharp
val transpose: Matrix<'a> -> Matrix<'a>
```
Swaps rows and columns. Element at `(i, j)` in the result equals element at `(j, i)` in the input. Result dimensions are `m.cols × m.rows`.

## TeX Printing

```fsharp
val toTeX:
    showRowNumbers: bool ->
    showColNumbers: bool ->
    cellPrinter: ('a -> string) ->
    m: Matrix<'a> ->
    string
```

Generates a LaTeX string using the `pNiceMatrix` environment from the [nicematrix](https://ctan.org/pkg/nicematrix) package.

### How It Works

The output has the form:

```tex
\begin{pNiceMatrix}
⟨cell₁₁⟩ & ⟨cell₁₂⟩ & ⋯ & ⟨cell₁ₙ⟩ \\
⟨cell₂₁⟩ & ⟨cell₂₂⟩ & ⋯ & ⟨cell₂ₙ⟩ \\
\vdots & \vdots & \ddots & \vdots \\
⟨cellₘ₁⟩ & ⟨cellₘ₂⟩ & ⋯ & ⟨cellₘₙ⟩ \\
\end{pNiceMatrix}
```

Cells are separated by ` & `, rows by ` \\` (standard LaTeX tabular syntax). Each cell is formatted using the `cellPrinter` function.

### Row and Column Numbering

Numbering is **1-based** (standard matrix notation). When enabled, numbering is implemented by manually inserting an extra row and/or column into the matrix content:

- **`showRowNumbers=true`**: An extra column is prepended. Row numbers appear as the first cell of each data row. If column numbers are also shown, the top-left corner cell (intersection of the number column and number row) is left empty.

- **`showColNumbers=true`**: An extra row is prepended at the top. Column numbers appear in the first row, above the data.

- **Corner case** (both enabled): The cell at `(0, 0)` is empty (`""`).

**Example** — 2×2 matrix `[[a,b],[c,d]]` with both row and column numbers:

```tex
\begin{pNiceMatrix}
 & 1 & 2 \\
1 & a & b \\
2 & c & d \\
\end{pNiceMatrix}
```

### Parameter: `cellPrinter`

The function `cellPrinter: 'a -> string` controls how each matrix element is rendered as TeX. For numeric matrices, `string` is sufficient. For matrices with custom types (e.g., sets, options), the caller can provide custom formatting (e.g., mathematical notation like `\cdot` for empty cells).

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Record wrapper over raw 2D array | Explicit `rows`/`cols` avoid recomputation; structural equality aids testing |
| `rows`/`cols` as functions, not direct field access | Consistent functional API surface; can be replaced with computed properties later |
| `map2` throws on dimension mismatch | Unambiguous error signaling; caller must ensure matching dimensions |
| 1-based numbering in TeX output | Standard matrix notation (row 1, column 1 is the top-left element) |
| Manual row/column prepending vs nicematrix `first-row`/`first-col` options | Simpler implementation; avoids dependency on nicematrix options and works with any LaTeX engine |
| Cell printer as function parameter | Maximum flexibility: caller controls how any element type renders in TeX |

### Styled TeX Printing

```fsharp
type Highlight = { row: int; col: int; color: string }

type SubmatrixBlock =
    { startRow: int
      startCol: int
      rowCount: int
      colCount: int
      borderColor: string option
      fillColor: string option }

val toTeXStyled:
    showRowNumbers: bool ->
    showColNumbers: bool ->
    cellPrinter: ('a -> string) ->
    m: Matrix<'a> ->
    highlights: Highlight list ->
    blocks: SubmatrixBlock list ->
    string
```

Extended TeX printing with cell highlighting and submatrix block borders. Highlights color individual cells using `\cellcolor{color}{content}`. Submatrix blocks draw borders around rectangular regions using nicematrix `\Block[draw=color]{rows-cols}{content}` commands placed at the top-left cell of each block.

| Decision | Rationale |
|----------|-----------|
| `\Block` uses `{rows-cols}` dimension syntax | Compatible with nicematrix v6+ (2024+), which removed the old `{r1-c1-r2-c2}` positional syntax |
| Block command embedded in cell content | nicematrix `\Block` must be placed at the block's top-left cell, not before the matrix |
| If multiple blocks start at same cell, only first is kept | nicematrix cannot handle overlapping `\Block` commands at the same position |
