namespace FLPQ.Core

open System

type Matrix<'a> = { rows: int; cols: int; data: 'a[,] }

module Matrix =

    let rows (m: Matrix<'a>) = m.rows

    let cols (m: Matrix<'a>) = m.cols

    let create rows cols (f: int -> int -> 'a) : Matrix<'a> =
        let data = Array2D.init rows cols f

        { rows = rows
          cols = cols
          data = data }

    let init rows cols (value: 'a) : Matrix<'a> = create rows cols (fun _ _ -> value)

    let ofArray2D (arr: 'a[,]) : Matrix<'a> =
        { rows = Array2D.length1 arr
          cols = Array2D.length2 arr
          data = arr }

    let map (f: 'a -> 'b) (m: Matrix<'a>) : Matrix<'b> =
        let data = Array2D.map f m.data

        { rows = m.rows
          cols = m.cols
          data = data }

    let map2 (f: 'a -> 'b -> 'c) (a: Matrix<'a>) (b: Matrix<'b>) : Matrix<'c> =
        if a.rows <> b.rows || a.cols <> b.cols then
            invalidArg (nameof b) $"Matrix dimensions must match: ({a.rows}x{a.cols}) vs ({b.rows}x{b.cols})"

        let data = Array2D.init a.rows a.cols (fun i j -> f a.data.[i, j] b.data.[i, j])

        { rows = a.rows
          cols = a.cols
          data = data }

    let transpose (m: Matrix<'a>) : Matrix<'a> =
        let data = Array2D.init m.cols m.rows (fun i j -> m.data.[j, i])

        { rows = m.cols
          cols = m.rows
          data = data }

    let toTeX (showRowNumbers: bool) (showColNumbers: bool) (cellPrinter: 'a -> string) (m: Matrix<'a>) : string =
        let printCell row col =
            if showRowNumbers && col = 0 then
                if showColNumbers && row = 0 then
                    ""
                else
                    (row + 1).ToString()
            elif showColNumbers && row = 0 then
                (col + 1).ToString()
            else
                let dataRow = if showColNumbers then row - 1 else row
                let dataCol = if showRowNumbers then col - 1 else col
                cellPrinter m.data.[dataRow, dataCol]

        let totalRows = if showColNumbers then m.rows + 1 else m.rows
        let totalCols = if showRowNumbers then m.cols + 1 else m.cols

        let body =
            [ for row in 0 .. totalRows - 1 do
                  let cells =
                      [ for col in 0 .. totalCols - 1 do
                            printCell row col ]

                  String.Join(" & ", cells) + @" \\" ]

        @"\begin{pNiceMatrix}"
        + "\n"
        + String.Join("\n", body)
        + "\n"
        + @"\end{pNiceMatrix}"
