namespace FLPQ.Printers

open FLPQ.Languages
open FLPQ.LinearAlgebra

/// TeX rendering for path index matrices.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module PathIndexTeX =

    /// Converts a set of path index entries to a TeX set: \cdot for empty, \{...\} otherwise.
    let private cellPrinter
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (entries: Set<PathIndexEntry<'t, 'nt>>)
        : string =
        let entryToTeX entry =
            match entry with
            | PathIndexEntry.PTerminal(Terminal t) -> terminalPrinter t
            | PathIndexEntry.PNonterminal(Nonterminal nt) -> nonterminalPrinter nt
            | PathIndexEntry.PEpsilonNonterminal(Nonterminal nt) -> sprintf @"%s^{\varepsilon}" (nonterminalPrinter nt)
            | PathIndexEntry.PIntermediate(s, p) -> sprintf @"I_{%d,%d}" s p

        ParsingTableTeX.setToTeX entryToTeX entries

    /// Renders a path index as a parametrized matrix using the nicematrix package.
    /// Row and column labels show (rsm_state, input_position) pairs.
    let toTeX
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (pathIndex: PathIndex<'t, 'nt>)
        : string =
        let cp = cellPrinter terminalPrinter nonterminalPrinter

        let labelPrinter (idx: int) =
            let state = idx / pathIndex.VertexCount
            let vertex = idx % pathIndex.VertexCount
            $"%d{state},%d{vertex}"

        let matrix =
            MatrixTeX.toTeXStyled false false cp pathIndex.Matrix [] [] (Some labelPrinter) (Some labelPrinter)

        matrix

    /// Renders a path index matrix with specified cells highlighted.
    /// Changed cells are highlighted in yellow for step-by-step visualization.
    let toTeXWithHighlights
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (pathIndex: PathIndex<'t, 'nt>)
        (changedCells: Set<int * int>)
        : string =
        let cp = cellPrinter terminalPrinter nonterminalPrinter

        let labelPrinter (idx: int) =
            let state = idx / pathIndex.VertexCount
            let vertex = idx % pathIndex.VertexCount
            $"%d{state},%d{vertex}"

        let highlights =
            changedCells
            |> Set.toList
            |> List.map (fun (row, col) ->
                { Matrix.Row = row
                  Matrix.Col = col
                  Matrix.Label = Matrix.CurrentCell })

        let matrix =
            MatrixTeX.toTeXStyled false false cp pathIndex.Matrix highlights [] (Some labelPrinter) (Some labelPrinter)

        matrix
