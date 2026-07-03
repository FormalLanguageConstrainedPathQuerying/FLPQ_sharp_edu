namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for CYK algorithm tables.
module CykTeX =

    /// Convert a CYK working table to TeX with highlighted cells.
    let tableToTeXStyled (table: ParsingTable<'nt>) (highlights: Matrix.Highlight list) : string =
        MatrixTeX.toTeXStyled true true ParsingTableTeX.ntCellToTeX table highlights []

    /// Convert a CYK working table to TeX.
    let tableToTeX (table: ParsingTable<'nt>) : string = tableToTeXStyled table []
