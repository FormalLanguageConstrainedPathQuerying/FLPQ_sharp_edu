namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for Valiant algorithm steps.
module ValiantTeX =

    /// Convert a Valiant trace step to TeX with highlighted current submatrix.
    let stepToTeX (nonterminalPrinter: 'nt -> string) (step: Valiant.ValiantTraceStep<'nt>) : string =
        let highlights =
            match step.currentSubmatrix with
            | Some m ->
                [ for i in m.row - m.Size + 1 .. m.row do
                      for j in m.col .. m.col + m.Size - 1 do
                          if i < Matrix.rows step.table && j < Matrix.cols step.table then
                              yield
                                  ({ row = i
                                     col = j
                                     label = Matrix.CurrentCell }
                                  : Matrix.Highlight) ]

            | None -> []

        let blocks =
            match step.currentSubmatrix with
            | Some m ->
                let startRow = m.row - m.Size + 1
                let endRow = m.row
                let startCol = m.col - 1
                let endCol = m.col + m.Size - 2

                let clippedStartRow = max 0 startRow
                let clippedEndRow = min (Matrix.rows step.table - 1) endRow
                let clippedStartCol = max 0 startCol
                let clippedEndCol = min (Matrix.cols step.table - 1) endCol

                if clippedStartRow <= clippedEndRow && clippedStartCol <= clippedEndCol then
                    [ ({ startRow = clippedStartRow
                         startCol = clippedStartCol
                         rowCount = clippedEndRow - clippedStartRow + 1
                         colCount = clippedEndCol - clippedStartCol + 1
                         label = Matrix.CurrentStepSubmatrix }
                      : Matrix.SubmatrixBlock) ]
                else
                    []
            | None -> []

        MatrixTeX.toTeXStyled true true (ParsingTableTeX.ntCellToTeX nonterminalPrinter) step.table highlights blocks

    /// Render a boolean decomposition matrix for a single nonterminal.
    let boolDecompToTeX (nonterminalPrinter: 'nt -> string) (nt: Nonterminal<'nt>) (mat: Matrix<bool>) : string =
        @"\mathrm{"
        + SymbolTeX.nonterminalContent nonterminalPrinter nt
        + "}\n"
        + MatrixTeX.toTeX true true ParsingTableTeX.boolToTeX mat

    /// Convert a modified Valiant trace step to TeX with colored submatrix blocks.
    let modifiedStepToTeX (nonterminalPrinter: 'nt -> string) (step: Valiant.ModifiedValiantTraceStep<'nt>) : string =
        let n = Matrix.rows step.table

        let blocks =
            step.submatrices
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

        MatrixTeX.toTeXStyled false false (ParsingTableTeX.ntCellToTeX nonterminalPrinter) step.table [] blocks
