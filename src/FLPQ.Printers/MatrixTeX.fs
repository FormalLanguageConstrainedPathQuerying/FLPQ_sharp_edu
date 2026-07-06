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
        : string =
        let pniceOptions = ResizeArray<string>()

        if not (List.isEmpty highlights) then
            pniceOptions.Add("color-inside")

        if showColNumbers then
            pniceOptions.Add("first-row")
            pniceOptions.Add(@"code-for-first-row = \arabic{jCol}")

        if showRowNumbers then
            pniceOptions.Add("first-col")
            pniceOptions.Add(@"code-for-first-col = \arabic{iRow}")

        let options =
            if pniceOptions.Count = 0 then
                ""
            else
                "[" + String.concat "," pniceOptions + "]"

        let dataRowOffset = if showColNumbers then 1 else 0
        let dataColOffset = if showRowNumbers then 1 else 0

        let totalRows = Matrix.rows matrix + dataRowOffset
        let totalCols = Matrix.cols matrix + dataColOffset

        let highlightSet =
            highlights
            |> List.map (fun h -> (h.row + dataRowOffset, h.col + dataColOffset, "yellow"))
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
                let r = b.startRow + dataRowOffset
                let c = b.startCol + dataColOffset

                let opts = ResizeArray<string>()

                match b.label with
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

                (r, c), (blockOptions, b.rowCount, b.colCount))
            |> List.groupBy fst
            |> List.map (fun (pos, cmds) -> pos, cmds |> List.head |> snd)
            |> Map.ofList

        let sb = System.Text.StringBuilder()
        sb.Append(sprintf @"\begin{pNiceMatrix}%s" options).AppendLine() |> ignore

        for row in 0 .. totalRows - 1 do
            let cells =
                [ for col in 0 .. totalCols - 1 do
                      let content =
                          if showColNumbers && row = 0 then
                              ""
                          elif showRowNumbers && col = 0 then
                              ""
                          else
                              let dataRow = row - dataRowOffset
                              let dataCol = col - dataColOffset
                              cellPrinter (Matrix.get matrix dataRow dataCol)

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
        toTeXStyled showRowNumbers showColNumbers cellPrinter matrix [] []
