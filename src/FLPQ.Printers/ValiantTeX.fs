namespace FLPQ.Printers

open System.Text.RegularExpressions
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for Valiant algorithm steps.
module ValiantTeX =

    let private shortNtName (Nonterminal n) =
        Regex.Replace(string n, @"N_CNF_(\d+)", @"N_{$1}")

    let private ntSetToTeX (s: Set<Nonterminal<'nt>>) : string =
        if Set.isEmpty s then
            @"\cdot"
        else
            s
            |> Set.toSeq
            |> Seq.map shortNtName
            |> String.concat ", "
            |> fun x -> @"\{" + x + @"\}"

    let private boolToTeX (b: bool) : string = if b then "1" else @"\cdot"

    /// Convert a Valiant trace step to TeX with highlighted current submatrix.
    let stepToTeX (step: Valiant.ValiantTraceStep<'nt>) : string =
        let highlights =
            match step.currentSubmatrix with
            | Some m ->
                [ for i in m.A - m.Size + 1 .. m.A do
                      for j in m.B .. m.B + m.Size - 1 do
                          if i < step.table.rows && j < step.table.cols then
                              yield ({ row = i; col = j; color = "yellow" }: Matrix.Highlight) ]

            | None -> []

        let blocks =
            match step.currentSubmatrix with
            | Some m ->
                let startRow = m.A - m.Size + 1
                let endRow = m.A
                let startCol = m.B - 1
                let endCol = m.B + m.Size - 2

                let clippedStartRow = max 0 startRow
                let clippedEndRow = min (step.table.rows - 1) endRow
                let clippedStartCol = max 0 startCol
                let clippedEndCol = min (step.table.cols - 1) endCol

                if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                    [ ({ startRow = clippedStartRow
                         startCol = clippedStartCol
                         rowCount = clippedEndRow - clippedStartRow + 1
                         colCount = clippedEndCol - clippedStartCol + 1
                         borderColor = Some "red"
                         fillColor = None }
                      : Matrix.SubmatrixBlock) ]
                else
                    []
            | None -> []

        MatrixTeX.toTeXStyled true true ntSetToTeX step.table highlights blocks

    /// Render a boolean decomposition matrix for a single nonterminal.
    let boolDecompToTeX (nt: Nonterminal<'nt>) (mat: Matrix<bool>) : string =
        @"\text{" + shortNtName nt + "}\n" + MatrixTeX.toTeX true true boolToTeX mat

    /// Convert a modified Valiant trace step to TeX with colored submatrix blocks.
    let modifiedStepToTeX (step: Valiant.ModifiedValiantTraceStep<'nt>) : string =
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

        let n = step.table.rows

        let blocks =
            step.submatrices
            |> List.mapi (fun idx m ->
                let color = colors.[idx % colors.Length]

                let startRow = m.A - m.Size + 1
                let endRow = m.A

                let startCol = m.B - 1
                let endCol = m.B + m.Size - 2

                let clippedStartRow = max 0 startRow
                let clippedEndRow = min (n - 1) endRow
                let clippedStartCol = max 0 startCol
                let clippedEndCol = min (n - 1) endCol

                if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                    let block: Matrix.SubmatrixBlock =
                        { startRow = clippedStartRow
                          startCol = clippedStartCol
                          rowCount = clippedEndRow - clippedStartRow + 1
                          colCount = clippedEndCol - clippedStartCol + 1
                          borderColor = Some color
                          fillColor = None }

                    Some block
                else
                    None)
            |> List.choose id

        MatrixTeX.toTeXStyled false false ntSetToTeX step.table [] blocks
