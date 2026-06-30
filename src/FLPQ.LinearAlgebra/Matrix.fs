namespace FLPQ.LinearAlgebra

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

    let fold (folder: 'acc -> 'a -> 'acc) (state: 'acc) (m: Matrix<'a>) : 'acc =
        let mutable acc = state

        for i in 0 .. m.rows - 1 do
            for j in 0 .. m.cols - 1 do
                acc <- folder acc m.data.[i, j]

        acc

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

    type Highlight = { row: int; col: int; color: string }

    type SubmatrixBlock =
        { startRow: int
          startCol: int
          rowCount: int
          colCount: int
          borderColor: string option
          fillColor: string option }

    let toTeXStyled
        (showRowNumbers: bool)
        (showColNumbers: bool)
        (cellPrinter: 'a -> string)
        (m: Matrix<'a>)
        (highlights: Highlight list)
        (blocks: SubmatrixBlock list)
        : string =
        let dataRowOffset = if showColNumbers then 1 else 0
        let dataColOffset = if showRowNumbers then 1 else 0
        let totalRows = if showColNumbers then m.rows + 1 else m.rows
        let totalCols = if showRowNumbers then m.cols + 1 else m.cols

        let highlightSet =
            highlights
            |> List.map (fun h -> (h.row + dataRowOffset, h.col + dataColOffset, h.color))
            |> Set.ofList

        let blockMap =
            blocks
            |> List.map (fun b ->
                let r = b.startRow + dataRowOffset
                let c = b.startCol + dataColOffset

                let opts = ResizeArray<string>()

                match b.borderColor with
                | Some bc -> opts.Add(sprintf "draw=%s" bc)
                | None -> ()

                match b.fillColor with
                | Some fc -> opts.Add(sprintf "fill=%s" fc)
                | None -> ()

                let options =
                    if opts.Count = 0 then
                        ""
                    else
                        "[" + String.concat "," opts + "]"

                (r, c), (options, b.rowCount, b.colCount))
            |> List.groupBy fst
            |> List.map (fun (pos, cmds) -> pos, cmds |> List.head |> snd)
            |> Map.ofList

        let sb = System.Text.StringBuilder()
        sb.Append(@"\begin{pNiceMatrix}") |> ignore
        sb.AppendLine() |> ignore

        for row in 0 .. totalRows - 1 do
            let cells =
                [ for col in 0 .. totalCols - 1 do
                      let content =
                          if showRowNumbers && col = 0 then
                              if showColNumbers && row = 0 then
                                  ""
                              else
                                  (row + 1).ToString()
                          elif showColNumbers && row = 0 then
                              (col + 1).ToString()
                          else
                              let dataRow = row - dataRowOffset
                              let dataCol = col - dataColOffset
                              cellPrinter m.data.[dataRow, dataCol]

                      let hc =
                          highlightSet |> Set.toList |> List.tryFind (fun (r, c, _) -> r = row && c = col)

                      match Map.tryFind (row, col) blockMap with
                      | Some(blockOpts, rowCount, colCount) ->
                          sprintf
                              @"\Block%s{%d-%d}{%s}"
                              blockOpts
                              rowCount
                              colCount
                              (match hc with
                               | Some(_, _, color) -> sprintf @"\cellcolor{%s}{%s}" color content
                               | None -> content)
                      | None ->
                          match hc with
                          | Some(_, _, color) -> sprintf @"\cellcolor{%s}{%s}" color content
                          | None -> content ]

            let line = String.Join(" & ", cells) + @" \\"
            sb.Append(line).AppendLine() |> ignore

        sb.Append(@"\end{pNiceMatrix}") |> ignore
        sb.ToString()

    let toTeX (showRowNumbers: bool) (showColNumbers: bool) (cellPrinter: 'a -> string) (m: Matrix<'a>) : string =
        toTeXStyled showRowNumbers showColNumbers cellPrinter m [] []
