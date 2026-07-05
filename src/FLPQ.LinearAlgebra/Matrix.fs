namespace FLPQ.LinearAlgebra

open System

type Matrix<'a> =
    private
        { rows: int
          cols: int
          data: 'a[,] }

module Matrix =

    /// Returns the number of rows in the matrix.
    let rows (m: Matrix<'a>) = m.rows

    /// Returns the number of columns in the matrix.
    let cols (m: Matrix<'a>) = m.cols

    /// Returns the element at position (i, j). No bounds checking is performed.
    let get (m: Matrix<'a>) (i: int) (j: int) : 'a = m.data.[i, j]

    /// Sets the element at position (i, j) to the given value. No bounds checking is performed.
    let set (m: Matrix<'a>) (i: int) (j: int) (value: 'a) : unit = m.data.[i, j] <- value

    /// Creates a matrix with the given number of rows and columns,
    /// using the supplied function to initialize each element.
    let create rows cols (f: int -> int -> 'a) : Matrix<'a> =
        let data = Array2D.init rows cols f

        { rows = rows
          cols = cols
          data = data }

    /// Creates a matrix with the given dimensions where all elements are initialized to the same value.
    let init rows cols (value: 'a) : Matrix<'a> = create rows cols (fun _ _ -> value)

    /// Creates a matrix from a 2D array. The resulting matrix shares the same backing array.
    let ofArray2D (arr: 'a[,]) : Matrix<'a> =
        { rows = Array2D.length1 arr
          cols = Array2D.length2 arr
          data = arr }

    /// Folds over all elements of the matrix in row-major order.
    /// folder receives the accumulator and the current element.
    let fold (folder: 'acc -> 'a -> 'acc) (state: 'acc) (m: Matrix<'a>) : 'acc =
        let mutable acc = state

        for i in 0 .. m.rows - 1 do
            for j in 0 .. m.cols - 1 do
                acc <- folder acc (get m i j)

        acc

    /// Applies a function to each element of the matrix, returning a new matrix with the same dimensions.
    let map (f: 'a -> 'b) (m: Matrix<'a>) : Matrix<'b> =
        let data = Array2D.map f m.data

        { rows = m.rows
          cols = m.cols
          data = data }

    /// Applies a function element-wise to two matrices of the same dimensions.
    /// Throws ArgumentException if dimensions differ.
    let map2 (f: 'a -> 'b -> 'c) (a: Matrix<'a>) (b: Matrix<'b>) : Matrix<'c> =
        if a.rows <> b.rows || a.cols <> b.cols then
            invalidArg (nameof b) $"Matrix dimensions must match: ({a.rows}x{a.cols}) vs ({b.rows}x{b.cols})"

        let data = Array2D.init a.rows a.cols (fun i j -> f (get a i j) (get b i j))

        { rows = a.rows
          cols = a.cols
          data = data }

    /// Returns the transpose of the matrix: rows become columns and columns become rows.
    let transpose (m: Matrix<'a>) : Matrix<'a> =
        let data = Array2D.init m.cols m.rows (fun i j -> get m j i)

        { rows = m.cols
          cols = m.rows
          data = data }

    /// Create a diagonal matrix of the given size.
    /// Positions (i, i) for i in indices receive 'one', all other positions receive 'zero'.
    let diagonal (size: int) (indices: Set<int>) (one: 'a) (zero: 'a) : Matrix<'a> =
        create size size (fun i j -> if i = j && Set.contains i indices then one else zero)

    /// Reduce each column to a single value using the given operation.
    /// Returns an array of length m.cols.
    /// Column j accumulates: op(init, m.data[0,j]), op(result, m.data[1,j]), ...
    let reduceByColumn (op: 'a -> 'a -> 'a) (init: 'a) (m: Matrix<'a>) : 'a[] =
        Array.init m.cols (fun j ->
            let mutable acc = init

            for i in 0 .. m.rows - 1 do
                acc <- op acc (get m i j)

            acc)

    /// Labels for cell highlighting in algorithm visualization.
    type HighlightLabel = | CurrentCell

    /// Labels for submatrix regions in algorithm visualization.
    type SubmatrixBlockLabel =
        | CurrentStepSubmatrix
        | Submatrix of int

    /// A highlighted cell in a matrix visualization.
    type Highlight =
        { row: int
          col: int
          label: HighlightLabel }

    /// A rectangular submatrix region in a matrix visualization.
    type SubmatrixBlock =
        { startRow: int
          startCol: int
          rowCount: int
          colCount: int
          label: SubmatrixBlockLabel }
