namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

module ValiantTeX =

    let stepToTeX (nonterminalPrinter: 'nt -> string) (step: Valiant.ValiantTraceStep<'nt>) : string =
        let highlights =
            [ for (i, j) in step.changedCells do
                  let ci = i
                  let cj = j - 1

                  if ci >= 0 && ci < Matrix.rows step.table && cj >= 0 && cj < Matrix.cols step.table then
                      yield
                          ({ row = ci
                             col = cj
                             label = Matrix.CurrentCell }
                          : Matrix.Highlight) ]

        let startRow = step.target.row - step.target.Size + 1
        let endRow = step.target.row
        let startCol = step.target.col - 1
        let endCol = step.target.col + step.target.Size - 2

        let clippedStartRow = max 0 startRow
        let clippedEndRow = min (Matrix.rows step.table - 1) endRow
        let clippedStartCol = max 0 startCol
        let clippedEndCol = min (Matrix.cols step.table - 1) endCol

        let targetBlock =
            if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                [ ({ startRow = clippedStartRow
                     startCol = clippedStartCol
                     rowCount = clippedEndRow - clippedStartRow + 1
                     colCount = clippedEndCol - clippedStartCol + 1
                     label = Matrix.CurrentStepSubmatrix }
                  : Matrix.SubmatrixBlock) ]
            else
                []

        let multipliedBlocks =
            step.multiplied
            |> List.mapi (fun idx (m1, m2) ->
                let sr1 = max 0 (m1.row - m1.Size + 1)
                let sc1 = max 0 (m1.col - 1)
                let sr2 = max 0 (m2.row - m2.Size + 1)
                let sc2 = max 0 (m2.col - 1)

                let b1: Matrix.SubmatrixBlock =
                    { startRow = sr1
                      startCol = sc1
                      rowCount = min m1.Size (Matrix.rows step.table - sr1)
                      colCount = min m1.Size (Matrix.cols step.table - sc1)
                      label = Matrix.Submatrix idx }

                let b2: Matrix.SubmatrixBlock =
                    { startRow = sr2
                      startCol = sc2
                      rowCount = min m2.Size (Matrix.rows step.table - sr2)
                      colCount = min m2.Size (Matrix.cols step.table - sc2)
                      label = Matrix.Submatrix idx }

                [ b1; b2 ])
            |> List.concat

        MatrixTeX.toTeXStyled
            true
            true
            (ParsingTableTeX.ntCellToTeX nonterminalPrinter)
            step.table
            highlights
            (targetBlock @ multipliedBlocks)

    let modifiedStepToTeX (nonterminalPrinter: 'nt -> string) (step: Valiant.ModifiedValiantTraceStep<'nt>) : string =
        match step with
        | Valiant.LayerForward(table, _layerSize, submatrices) ->
            let n = Matrix.rows table

            let blocks =
                submatrices
                |> List.mapi (fun idx m ->
                    let startRow = m.row - m.Size + 1
                    let endRow = m.row
                    let startCol = m.col - 1
                    let endCol = m.col + m.Size - 2

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
                              label = Matrix.Submatrix idx }

                        Some block
                    else
                        None)
                |> List.choose id

            MatrixTeX.toTeXStyled false false (ParsingTableTeX.ntCellToTeX nonterminalPrinter) table [] blocks

        | Valiant.LayerBackward(table, _layerSize, submatrices, changedCells) ->
            let n = Matrix.rows table

            let blocks =
                submatrices
                |> List.mapi (fun idx m ->
                    let startRow = m.row - m.Size + 1
                    let endRow = m.row
                    let startCol = m.col - 1
                    let endCol = m.col + m.Size - 2

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
                              label = Matrix.Submatrix idx }

                        Some block
                    else
                        None)
                |> List.choose id

            let highlights =
                [ for (i, j) in changedCells do
                      let ci = i
                      let cj = j - 1

                      if ci >= 0 && ci < n && cj >= 0 && cj < n then
                          yield
                              ({ row = ci
                                 col = cj
                                 label = Matrix.CurrentCell }
                              : Matrix.Highlight) ]

            MatrixTeX.toTeXStyled false false (ParsingTableTeX.ntCellToTeX nonterminalPrinter) table highlights blocks
