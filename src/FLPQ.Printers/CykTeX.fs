namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for CYK algorithm tables.
module CykTeX =

    let private cellToTeX (cell: Cyk.CykCell<'t, 'nt>) : string =
        ParsingTableTeX.optionSetToTeX SymbolTeX.toLaTeX (cell |> Option.map (fun hs -> hs :> seq<_>))

    /// Convert a CYK working table to TeX with highlighted cells.
    let tableToTeXStyled (table: Matrix<Cyk.CykCell<'t, 'nt>>) (highlights: Matrix.Highlight list) : string =
        MatrixTeX.toTeXStyled true true cellToTeX table highlights []

    /// Convert a CYK working table to TeX.
    let tableToTeX (table: Matrix<Cyk.CykCell<'t, 'nt>>) : string = tableToTeXStyled table []
