namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

module ValiantTeX =

    let sppfStepToTeX (nonterminalPrinter: 'nt -> string) (step: Valiant.ValiantSppfTraceStep<'nt>) : string =
        let highlights =
            [ for (i, j) in step.ChangedCells do
                  let ci = i
                  let cj = j

                  if ci >= 0 && ci < Matrix.rows step.Table && cj >= 0 && cj < Matrix.cols step.Table then
                      yield
                          ({ Row = ci
                             Col = cj
                             Label = Matrix.CurrentCell }
                          : Matrix.Highlight) ]

        let startRow = step.Target.Row - step.Target.Size + 1
        let endRow = step.Target.Row
        let startCol = step.Target.Col
        let endCol = step.Target.Col + step.Target.Size - 1

        let clippedStartRow = max 0 startRow
        let clippedEndRow = min (Matrix.rows step.Table - 1) endRow
        let clippedStartCol = max 0 startCol
        let clippedEndCol = min (Matrix.cols step.Table - 1) endCol

        let targetBlock =
            if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                let sb: Matrix.SubmatrixBlock =
                    { StartRow = clippedStartRow
                      StartCol = clippedStartCol
                      RowCount = clippedEndRow - clippedStartRow + 1
                      ColCount = clippedEndCol - clippedStartCol + 1
                      Label = Matrix.CurrentStepSubmatrix }

                [ sb ]
            else
                []

        let multipliedBlocks =
            step.Multiplied
            |> List.mapi (fun idx (m1, m2) ->
                let sr1 = max 0 (m1.Row - m1.Size + 1)
                let sc1 = max 0 m1.Col
                let sr2 = max 0 (m2.Row - m2.Size + 1)
                let sc2 = max 0 m2.Col

                let b1: Matrix.SubmatrixBlock =
                    { StartRow = sr1
                      StartCol = sc1
                      RowCount = min m1.Size (Matrix.rows step.Table - sr1)
                      ColCount = min m1.Size (Matrix.cols step.Table - sc1)
                      Label = Matrix.Submatrix(idx * 2 + 1) }

                let b2: Matrix.SubmatrixBlock =
                    { StartRow = sr2
                      StartCol = sc2
                      RowCount = min m2.Size (Matrix.rows step.Table - sr2)
                      ColCount = min m2.Size (Matrix.cols step.Table - sc2)
                      Label = Matrix.Submatrix(idx * 2 + 2) }

                [ b1; b2 ])
            |> List.concat

        MatrixTeX.toTeXStyled
            true
            true
            (ParsingTableTeX.sppfEntryCellToTeX nonterminalPrinter)
            step.Table
            highlights
            (targetBlock @ multipliedBlocks)
            None
            None
            true
            true

    let sppfModifiedStepToTeX
        (nonterminalPrinter: 'nt -> string)
        (step: Valiant.ModifiedValiantSppfTraceStep<'nt>)
        : string =
        match step with
        | Valiant.LayerForwardSppf(table, _layerSize, submatrices) ->
            let n = Matrix.rows table

            let blocks =
                submatrices
                |> List.mapi (fun idx m ->
                    let startRow = m.Row - m.Size + 1
                    let endRow = m.Row
                    let startCol = m.Col - 1
                    let endCol = m.Col + m.Size - 2

                    let clippedStartRow = max 0 startRow
                    let clippedEndRow = min (n - 1) endRow
                    let clippedStartCol = max 0 startCol
                    let clippedEndCol = min (n - 1) endCol

                    if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                        let block: Matrix.SubmatrixBlock =
                            { StartRow = clippedStartRow
                              StartCol = clippedStartCol
                              RowCount = clippedEndRow - clippedStartRow + 1
                              ColCount = clippedEndCol - clippedStartCol + 1
                              Label = Matrix.Submatrix idx }

                        Some block
                    else
                        None)
                |> List.choose id

            MatrixTeX.toTeXStyled
                false
                false
                (ParsingTableTeX.sppfEntryCellToTeX nonterminalPrinter)
                table
                []
                blocks
                None
                None
                true
                true

        | Valiant.LayerBackwardSppf(table, _layerSize, submatrices, changedCells) ->
            let n = Matrix.rows table

            let blocks =
                submatrices
                |> List.mapi (fun idx m ->
                    let startRow = m.Row - m.Size + 1
                    let endRow = m.Row
                    let startCol = m.Col - 1
                    let endCol = m.Col + m.Size - 2

                    let clippedStartRow = max 0 startRow
                    let clippedEndRow = min (n - 1) endRow
                    let clippedStartCol = max 0 startCol
                    let clippedEndCol = min (n - 1) endCol

                    if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                        let block: Matrix.SubmatrixBlock =
                            { StartRow = clippedStartRow
                              StartCol = clippedStartCol
                              RowCount = clippedEndRow - clippedStartRow + 1
                              ColCount = clippedEndCol - clippedStartCol + 1
                              Label = Matrix.Submatrix idx }

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
                              ({ Row = ci
                                 Col = cj
                                 Label = Matrix.CurrentCell }
                              : Matrix.Highlight) ]

            MatrixTeX.toTeXStyled
                false
                false
                (ParsingTableTeX.sppfEntryCellToTeX nonterminalPrinter)
                table
                highlights
                blocks
                None
                None
                true
                true

    let sppfStepToTeXAsNt (nonterminalPrinter: 'nt -> string) (step: Valiant.ValiantSppfTraceStep<'nt>) : string =
        let highlights =
            [ for (i, j) in step.ChangedCells do
                  let ci = i
                  let cj = j

                  if ci >= 0 && ci < Matrix.rows step.Table && cj >= 0 && cj < Matrix.cols step.Table then
                      yield
                          ({ Row = ci
                             Col = cj
                             Label = Matrix.CurrentCell }
                          : Matrix.Highlight) ]

        let startRow = step.Target.Row - step.Target.Size + 1
        let endRow = step.Target.Row
        let startCol = step.Target.Col
        let endCol = step.Target.Col + step.Target.Size - 1

        let clippedStartRow = max 0 startRow
        let clippedEndRow = min (Matrix.rows step.Table - 1) endRow
        let clippedStartCol = max 0 startCol
        let clippedEndCol = min (Matrix.cols step.Table - 1) endCol

        let targetBlock =
            if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                let sb: Matrix.SubmatrixBlock =
                    { StartRow = clippedStartRow
                      StartCol = clippedStartCol
                      RowCount = clippedEndRow - clippedStartRow + 1
                      ColCount = clippedEndCol - clippedStartCol + 1
                      Label = Matrix.CurrentStepSubmatrix }

                [ sb ]
            else
                []

        let multipliedBlocks =
            step.Multiplied
            |> List.mapi (fun idx (m1, m2) ->
                let sr1 = max 0 (m1.Row - m1.Size + 1)
                let sc1 = max 0 m1.Col
                let sr2 = max 0 (m2.Row - m2.Size + 1)
                let sc2 = max 0 m2.Col

                let b1: Matrix.SubmatrixBlock =
                    { StartRow = sr1
                      StartCol = sc1
                      RowCount = min m1.Size (Matrix.rows step.Table - sr1)
                      ColCount = min m1.Size (Matrix.cols step.Table - sc1)
                      Label = Matrix.Submatrix(idx * 2 + 1) }

                let b2: Matrix.SubmatrixBlock =
                    { StartRow = sr2
                      StartCol = sc2
                      RowCount = min m2.Size (Matrix.rows step.Table - sr2)
                      ColCount = min m2.Size (Matrix.cols step.Table - sc2)
                      Label = Matrix.Submatrix(idx * 2 + 2) }

                [ b1; b2 ])
            |> List.concat

        MatrixTeX.toTeXStyled
            true
            true
            (ParsingTableTeX.sppfEntryAsNtCellToTeX nonterminalPrinter)
            step.Table
            highlights
            (targetBlock @ multipliedBlocks)
            None
            None
            true
            true

    let sppfModifiedStepToTeXAsNt
        (nonterminalPrinter: 'nt -> string)
        (step: Valiant.ModifiedValiantSppfTraceStep<'nt>)
        : string =
        match step with
        | Valiant.LayerForwardSppf(table, _layerSize, submatrices) ->
            let n = Matrix.rows table

            let blocks =
                submatrices
                |> List.mapi (fun idx m ->
                    let startRow = m.Row - m.Size + 1
                    let endRow = m.Row
                    let startCol = m.Col - 1
                    let endCol = m.Col + m.Size - 2

                    let clippedStartRow = max 0 startRow
                    let clippedEndRow = min (n - 1) endRow
                    let clippedStartCol = max 0 startCol
                    let clippedEndCol = min (n - 1) endCol

                    if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                        let block: Matrix.SubmatrixBlock =
                            { StartRow = clippedStartRow
                              StartCol = clippedStartCol
                              RowCount = clippedEndRow - clippedStartRow + 1
                              ColCount = clippedEndCol - clippedStartCol + 1
                              Label = Matrix.Submatrix idx }

                        Some block
                    else
                        None)
                |> List.choose id

            MatrixTeX.toTeXStyled
                false
                false
                (ParsingTableTeX.sppfEntryAsNtCellToTeX nonterminalPrinter)
                table
                []
                blocks
                None
                None
                true
                true

        | Valiant.LayerBackwardSppf(table, _layerSize, submatrices, changedCells) ->
            let n = Matrix.rows table

            let blocks =
                submatrices
                |> List.mapi (fun idx m ->
                    let startRow = m.Row - m.Size + 1
                    let endRow = m.Row
                    let startCol = m.Col - 1
                    let endCol = m.Col + m.Size - 2

                    let clippedStartRow = max 0 startRow
                    let clippedEndRow = min (n - 1) endRow
                    let clippedStartCol = max 0 startCol
                    let clippedEndCol = min (n - 1) endCol

                    if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                        let block: Matrix.SubmatrixBlock =
                            { StartRow = clippedStartRow
                              StartCol = clippedStartCol
                              RowCount = clippedEndRow - clippedStartRow + 1
                              ColCount = clippedEndCol - clippedStartCol + 1
                              Label = Matrix.Submatrix idx }

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
                              ({ Row = ci
                                 Col = cj
                                 Label = Matrix.CurrentCell }
                              : Matrix.Highlight) ]

            MatrixTeX.toTeXStyled
                false
                false
                (ParsingTableTeX.sppfEntryAsNtCellToTeX nonterminalPrinter)
                table
                highlights
                blocks
                None
                None
                true
                true
