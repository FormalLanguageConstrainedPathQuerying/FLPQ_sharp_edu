namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for Valiant algorithm steps.
module ValiantTeX =

    /// Convert a modified Valiant trace step to TeX with colored submatrix blocks.
    let stepToTeX
        (cellPrinter: Set<Nonterminal<'nt>> -> string)
        (step: Valiant.ModifiedValiantTraceStep<'nt>)
        : string =
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

        MatrixTeX.toTeXStyled false false cellPrinter step.table [] blocks
