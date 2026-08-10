namespace FLPQ.Printers

open System
open FLPQ.LinearAlgebra

/// TeX rendering for matrices using the nicematrix package.
module MatrixTeX =

    [<Struct>]
    type private HighlightCell = { Row: int; Col: int; Color: string }

    [<Struct>]
    type private BlockInfo =
        { Options: string
          RowCount: int
          ColCount: int }

    let toTeXStyled
        (showRowNumbers: bool)
        (showColNumbers: bool)
        (cellPrinter: 'a -> string)
        (matrix: Matrix<'a>)
        (highlights: Matrix.Highlight list)
        (blocks: Matrix.SubmatrixBlock list)
        (rowLabelPrinter: (int -> string) option)
        (colLabelPrinter: (int -> string) option)
        (useRectangleColor: bool)
        (useAdjustbox: bool)
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
            |> List.map (fun h ->
                { Row = h.Row + dataRowOffset
                  Col = h.Col + dataColOffset
                  Color = "yellow" })
            |> Set.ofList

        let blockFillColor (label: Matrix.SubmatrixBlockLabel) =
            match label with
            | Matrix.CurrentStepSubmatrix -> "red!10"
            | Matrix.Submatrix idx ->
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

                sprintf "%s!20" colors.[idx % colors.Length]

        let rectangleColors =
            if useRectangleColor then
                blocks
                |> List.map (fun b ->
                    let rowStart = b.StartRow + 1
                    let rowEnd = rowStart + b.RowCount - 1
                    let colStart = b.StartCol + 1
                    let colEnd = colStart + b.ColCount - 1

                    sprintf
                        @"\rectanglecolor{%s}{%d-%d}{%d-%d}"
                        (blockFillColor b.Label)
                        rowStart
                        colStart
                        rowEnd
                        colEnd)
            else
                []

        let blockMap =
            if useRectangleColor then
                Map.empty
            else
                blocks
                |> List.map (fun b ->
                    let r = b.StartRow + dataRowOffset
                    let c = b.StartCol + dataColOffset

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

                    let opts = ResizeArray<string>()

                    match b.Label with
                    | Matrix.CurrentStepSubmatrix ->
                        opts.Add("draw=red")
                        opts.Add("fill=red!10")
                    | Matrix.Submatrix idx ->
                        opts.Add(sprintf "draw=%s" (blockColor idx))
                        opts.Add(sprintf "fill=%s!20" (blockColor idx))

                    let blockOptions =
                        if opts.Count = 0 then
                            ""
                        else
                            "[" + String.concat "," opts + "]"

                    (r, c),
                    { Options = blockOptions
                      RowCount = b.RowCount
                      ColCount = b.ColCount })
                |> List.groupBy fst
                |> List.map (fun (pos, cmds) -> pos, cmds |> List.head |> snd)
                |> Map.ofList

        let sb = System.Text.StringBuilder()

        if not useRectangleColor && totalCols > 10 then
            sb.Append(sprintf @"\setcounter{MaxMatrixCols}{%d}" totalCols).AppendLine()
            |> ignore

        if useAdjustbox then
            sb.AppendLine(@"\begin{adjustbox}{max width=\textwidth}").Append("$").AppendLine()
            |> ignore

        sb.Append(sprintf @"\begin{pNiceMatrix}%s" options).AppendLine() |> ignore

        if useRectangleColor && not (List.isEmpty rectangleColors) then
            sb.AppendLine(@"\CodeBefore") |> ignore

            for rc in rectangleColors do
                sb.AppendLine(rc) |> ignore

            sb.AppendLine(@"\Body") |> ignore

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
                          highlightSet |> Set.toList |> List.tryFind (fun h -> h.Row = row && h.Col = col)

                      match Map.tryFind (row, col) blockMap with
                      | Some block ->
                          sprintf
                              @"\Block%s{%d-%d}{%s}"
                              block.Options
                              block.RowCount
                              block.ColCount
                              (match hc with
                               | Some h -> sprintf @"\cellcolor{%s}{%s}" h.Color content
                               | None -> content)
                      | None ->
                          match hc with
                          | Some h -> sprintf @"\cellcolor{%s}{%s}" h.Color content
                          | None -> content ]

            if not (List.isEmpty cells) then
                let line = String.Join(" & ", cells) + @" \\"
                sb.Append(line).AppendLine() |> ignore

        sb.Append(@"\end{pNiceMatrix}") |> ignore

        if useAdjustbox then
            sb.AppendLine().Append("$").AppendLine().AppendLine(@"\end{adjustbox}")
            |> ignore

        sb.ToString()

    let toTeX (showRowNumbers: bool) (showColNumbers: bool) (cellPrinter: 'a -> string) (matrix: Matrix<'a>) : string =
        toTeXStyled showRowNumbers showColNumbers cellPrinter matrix [] [] None None false false
