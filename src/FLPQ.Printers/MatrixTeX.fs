namespace FLPQ.Printers

open System
open FLPQ.LinearAlgebra

/// TeX rendering for matrices using the nicematrix package.
module MatrixTeX =

    let toTeXStyled
        (showRowNumbers: bool)
        (showColNumbers: bool)
        (cellPrinter: 'a -> string)
        (matrix: Matrix<'a>)
        (highlights: Matrix.Highlight list)
        (blocks: Matrix.SubmatrixBlock list)
        (rowLabelPrinter: (int -> string) option)
        (colLabelPrinter: (int -> string) option)
        : string =
        let pniceOptions = ResizeArray<string>()

        if not (List.isEmpty highlights) then
            pniceOptions.Add("color-inside")

        let hasRowHdr = showRowNumbers || rowLabelPrinter.IsSome
        let hasColHdr = showColNumbers || colLabelPrinter.IsSome

        if hasColHdr then
            pniceOptions.Add("first-row")

            match colLabelPrinter with
            | Some _ -> ()
            | None -> pniceOptions.Add(@"code-for-first-row = \arabic{jCol}")

        if hasRowHdr then
            pniceOptions.Add("first-col")

            match rowLabelPrinter with
            | Some _ -> ()
            | None -> pniceOptions.Add(@"code-for-first-col = \arabic{iRow}")

        let options =
            if pniceOptions.Count = 0 then
                ""
            else
                "[" + String.concat "," pniceOptions + "]"

        let dataRowOffset = if hasColHdr then 1 else 0
        let dataColOffset = if hasRowHdr then 1 else 0

        let totalRows = Matrix.rows matrix + dataRowOffset
        let totalCols = Matrix.cols matrix + dataColOffset

        let highlightSet =
            highlights
            |> List.map (fun h -> (h.Row + dataRowOffset, h.Col + dataColOffset, "yellow"))
            |> Set.ofList

        let blockColor idx =
            let colors =
                [ "red"
                  "blue"
                  "green"
                  "orange"
                  "purple"
                  "brown"
                  "cyan"
                  "magenta"
                  "teal"
                  "olive" ]

            colors.[idx % colors.Length]

        let blockFillColor idx = sprintf "%s!20" (blockColor idx)

        let blockMap =
            blocks
            |> List.map (fun b ->
                let r = b.StartRow + dataRowOffset
                let c = b.StartCol + dataColOffset

                let opts = ResizeArray<string>()

                match b.Label with
                | Matrix.CurrentStepSubmatrix ->
                    opts.Add("draw=red")
                    opts.Add("fill=red!10")
                | Matrix.Submatrix idx ->
                    opts.Add(sprintf "draw=%s" (blockColor idx))
                    opts.Add(sprintf "fill=%s" (blockFillColor idx))

                let blockOptions =
                    if opts.Count = 0 then
                        ""
                    else
                        "[" + String.concat "," opts + "]"

                (r, c), (blockOptions, b.RowCount, b.ColCount))
            |> List.groupBy fst
            |> List.map (fun (pos, cmds) -> pos, cmds |> List.head |> snd)
            |> Map.ofList

        let sb = System.Text.StringBuilder()

        if totalCols > 10 then
            sb.Append(sprintf @"\setcounter{MaxMatrixCols}{%d}" totalCols).AppendLine()
            |> ignore

        sb.Append(sprintf @"\begin{pNiceMatrix}%s" options).AppendLine() |> ignore

        for row in 0 .. totalRows - 1 do
            let cells =
                [ for col in 0 .. totalCols - 1 do
                      let isCorner = (hasColHdr || hasRowHdr) && row = 0 && col = 0
                      let isColHdr = hasColHdr && row = 0 && col > 0
                      let isRowHdr = hasRowHdr && col = 0 && row > 0

                      let content =
                          if isCorner then
                              ""
                          elif isColHdr then
                              match colLabelPrinter with
                              | Some printer -> sprintf @"\text{%s}" (printer (col - 1))
                              | None -> ""
                          elif isRowHdr then
                              match rowLabelPrinter with
                              | Some printer -> sprintf @"\text{%s}" (printer (row - 1))
                              | None -> ""
                          else
                              let dataRow = row - dataRowOffset
                              let dataCol = col - dataColOffset
                              cellPrinter matrix.[dataRow, dataCol]

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

            if not (List.isEmpty cells) then
                let line = String.Join(" & ", cells) + @" \\"
                sb.Append(line).AppendLine() |> ignore

        sb.Append(@"\end{pNiceMatrix}") |> ignore
        sb.ToString()

    let toTeX (showRowNumbers: bool) (showColNumbers: bool) (cellPrinter: 'a -> string) (matrix: Matrix<'a>) : string =
        toTeXStyled showRowNumbers showColNumbers cellPrinter matrix [] [] None None
