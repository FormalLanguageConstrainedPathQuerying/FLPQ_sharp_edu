namespace FLPQ.Core

open System

module LinearAlgebra =

    /// Generic matrix-matrix multiplication. Classical triple-nested loop.
    /// Precondition: a.cols = b.rows
    let mxm (a: Matrix<'a>) (b: Matrix<'b>) (opMult: 'a -> 'b -> 'c) (opAdd: 'c -> 'c -> 'c) (zero: 'c) : Matrix<'c> =
        if a.cols <> b.rows then
            invalidArg
                (nameof b)
                $"Matrix dimensions incompatible for multiplication: left has {a.cols} columns, right has {b.rows} rows"

        Matrix.create a.rows b.cols (fun i j ->
            let mutable acc = zero

            for k in 0 .. a.cols - 1 do
                acc <- opAdd acc (opMult a.data.[i, k] b.data.[k, j])

            acc)

    /// Kronecker product of two matrices.
    /// Result has dimensions (a.rows * b.rows) × (a.cols * b.cols).
    /// Element at (i * b.rows + r, j * b.cols + s) = opMult(a[i,j], b[r,s]).
    let kron (a: Matrix<'a>) (b: Matrix<'b>) (opMult: 'a -> 'b -> 'c) (zero: 'c) : Matrix<'c> =
        let resultRows = a.rows * b.rows
        let resultCols = a.cols * b.cols

        Matrix.create resultRows resultCols (fun i j ->
            let ai = i / b.rows
            let bi = i % b.rows
            let aj = j / b.cols
            let bj = j % b.cols
            opMult a.data.[ai, aj] b.data.[bi, bj])
