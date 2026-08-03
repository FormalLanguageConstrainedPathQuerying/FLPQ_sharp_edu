namespace FLPQ.LinearAlgebra

open System

type Matrix<'a> =
    private
        { Rows: int
          Cols: int
          Data: 'a[,] }

    member this.Item
        with get (i, j) = this.Data.[i, j]
        and set (i, j) value = this.Data.[i, j] <- value

module Matrix =

    /// Returns the number of rows in the matrix.
    let rows (m: Matrix<'a>) = m.Rows

    /// Returns the number of columns in the matrix.
    let cols (m: Matrix<'a>) = m.Cols

    /// Creates a matrix with the given number of rows and columns,
    /// using the supplied function to initialize each element.
    let create rows cols (f: int -> int -> 'a) : Matrix<'a> =
        let data = Array2D.init rows cols f

        { Rows = rows
          Cols = cols
          Data = data }

    /// Creates a matrix with the given dimensions where all elements are initialized to the same value.
    let init rows cols (value: 'a) : Matrix<'a> = create rows cols (fun _ _ -> value)

    /// Creates a deep copy of the matrix. The backing array is cloned; element values are shared
    /// because they are assumed immutable.
    let copy (m: Matrix<'a>) : Matrix<'a> =
        { Rows = m.Rows
          Cols = m.Cols
          Data = Array2D.copy m.Data }

    /// Creates a matrix from a 2D array. The resulting matrix shares the same backing array.
    let ofArray2D (arr: 'a[,]) : Matrix<'a> =
        { Rows = Array2D.length1 arr
          Cols = Array2D.length2 arr
          Data = arr }

    /// Folds over all elements of the matrix in row-major order.
    /// folder receives the accumulator and the current element.
    let fold (folder: 'acc -> 'a -> 'acc) (state: 'acc) (m: Matrix<'a>) : 'acc =
        let mutable acc = state

        for i in 0 .. m.Rows - 1 do
            for j in 0 .. m.Cols - 1 do
                acc <- folder acc m.[i, j]

        acc

    /// Applies a function to each element of the matrix, returning a new matrix with the same dimensions.
    let map (f: 'a -> 'b) (m: Matrix<'a>) : Matrix<'b> =
        let data = Array2D.map f m.Data

        { Rows = m.Rows
          Cols = m.Cols
          Data = data }

    /// Applies a function element-wise to two matrices of the same dimensions.
    /// Throws ArgumentException if dimensions differ.
    let map2 (f: 'a -> 'b -> 'c) (a: Matrix<'a>) (b: Matrix<'b>) : Matrix<'c> =
        if a.Rows <> b.Rows || a.Cols <> b.Cols then
            invalidArg (nameof b) $"Matrix dimensions must match: ({a.Rows}x{a.Cols}) vs ({b.Rows}x{b.Cols})"

        let data = Array2D.init a.Rows a.Cols (fun i j -> f a.[i, j] b.[i, j])

        { Rows = a.Rows
          Cols = a.Cols
          Data = data }

    /// Returns the transpose of the matrix: rows become columns and columns become rows.
    let transpose (m: Matrix<'a>) : Matrix<'a> =
        let data = Array2D.init m.Cols m.Rows (fun i j -> m.[j, i])

        { Rows = m.Cols
          Cols = m.Rows
          Data = data }

    /// Create a diagonal matrix of the given size.
    /// Positions (i, i) for i in indices receive 'one', all other positions receive 'zero'.
    let diagonal (size: int) (indices: Set<int>) (one: 'a) (zero: 'a) : Matrix<'a> =
        create size size (fun i j -> if i = j && Set.contains i indices then one else zero)

    /// Reduce each column to a single value using the given operation.
    /// Returns an array of length m.cols.
    /// Column j accumulates: op(init, m.data[0,j]), op(result, m.data[1,j]), ...
    let reduceByColumn (op: 'a -> 'a -> 'a) (init: 'a) (m: Matrix<'a>) : 'a[] =
        Array.init m.Cols (fun j ->
            let mutable acc = init

            for i in 0 .. m.Rows - 1 do
                acc <- op acc m.[i, j]

            acc)

    /// Applies a function element-wise to two matrices, providing row and column indices.
    /// Throws ArgumentException if dimensions differ.
    let map2i (f: int -> int -> 'a -> 'b -> 'c) (a: Matrix<'a>) (b: Matrix<'b>) : Matrix<'c> =
        if a.Rows <> b.Rows || a.Cols <> b.Cols then
            invalidArg (nameof b) $"Matrix dimensions must match: ({a.Rows}x{a.Cols}) vs ({b.Rows}x{b.Cols})"

        let data = Array2D.init a.Rows a.Cols (fun i j -> f i j a.[i, j] b.[i, j])

        { Rows = a.Rows
          Cols = a.Cols
          Data = data }

    /// Indexed matrix multiplication: C = A × B.
    /// op_mult i k j leftVal rightVal: combines A[i,k] and B[k,j] with indices.
    /// op_add i j acc newVal: folds results into C[i,j].
    /// zero: initial value for each cell's fold.
    let mxmi
        (op_add: int -> int -> 's -> 's -> 's)
        (op_mult: int -> int -> int -> 'a -> 'b -> 's)
        (zero: 's)
        (a: Matrix<'a>)
        (b: Matrix<'b>)
        : Matrix<'s> =
        let inner = cols a

        if inner <> rows b then
            invalidArg (nameof b) $"Matrix inner dimensions must match: ({rows a}x{cols a}) × ({rows b}x{cols b})"

        let rA = rows a
        let cB = cols b

        create rA cB (fun i j ->
            let mutable acc = zero

            for k in 0 .. inner - 1 do
                let leftVal = a.[i, k]
                let rightVal = b.[k, j]
                let product = op_mult i k j leftVal rightVal
                acc <- op_add i j acc product

            acc)

    /// Labels for cell highlighting in algorithm visualization.
    type HighlightLabel = | CurrentCell

    /// Labels for submatrix regions in algorithm visualization.
    type SubmatrixBlockLabel =
        | CurrentStepSubmatrix
        | Submatrix of int

    /// A highlighted cell in a matrix visualization.
    type Highlight =
        { Row: int
          Col: int
          Label: HighlightLabel }

    /// A rectangular submatrix region in a matrix visualization.
    type SubmatrixBlock =
        { StartRow: int
          StartCol: int
          RowCount: int
          ColCount: int
          Label: SubmatrixBlockLabel }
