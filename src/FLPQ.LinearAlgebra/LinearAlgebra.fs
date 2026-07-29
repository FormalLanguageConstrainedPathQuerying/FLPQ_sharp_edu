namespace FLPQ.LinearAlgebra

open System

module LinearAlgebra =

    /// Generic matrix-matrix multiplication. Classical triple-nested loop.
    /// Precondition: a.cols = b.rows
    let mxm (a: Matrix<'a>) (b: Matrix<'b>) (opMult: 'a -> 'b -> 'c) (opAdd: 'c -> 'c -> 'c) (zero: 'c) : Matrix<'c> =
        if Matrix.cols a <> Matrix.rows b then
            invalidArg
                (nameof b)
                $"Matrix dimensions incompatible for multiplication: left has {Matrix.cols a} columns, right has {Matrix.rows b} rows"

        Matrix.create (Matrix.rows a) (Matrix.cols b) (fun i j ->
            let mutable acc = zero

            for k in 0 .. Matrix.cols a - 1 do
                acc <- opAdd acc (opMult a.[i, k] b.[k, j])

            acc)

    /// Kronecker product of two matrices.
    /// Result has dimensions (a.rows * b.rows) × (a.cols * b.cols).
    /// Element at (i * b.rows + r, j * b.cols + s) = opMult(a[i,j], b[r,s]).
    let kron (a: Matrix<'a>) (b: Matrix<'b>) (opMult: 'a -> 'b -> 'c) (zero: 'c) : Matrix<'c> =
        let resultRows = Matrix.rows a * Matrix.rows b
        let resultCols = Matrix.cols a * Matrix.cols b

        Matrix.create resultRows resultCols (fun i j ->
            let ai = i / Matrix.rows b
            let bi = i % Matrix.rows b
            let aj = j / Matrix.cols b
            let bj = j % Matrix.cols b
            opMult a.[ai, aj] b.[bi, bj])
