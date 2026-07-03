namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for CYK algorithm tables.
module CykTeX =

    let private cellToTeX (cell: Set<Nonterminal<'nt>>) : string =
        ParsingTableTeX.setToTeX (fun (Nonterminal n) -> string n) cell

    /// Convert a CYK working table to TeX with highlighted cells.
    let tableToTeXStyled (table: ParsingTable<'nt>) (highlights: Matrix.Highlight list) : string =
        MatrixTeX.toTeXStyled true true cellToTeX table highlights []

    /// Convert a CYK working table to TeX.
    let tableToTeX (table: ParsingTable<'nt>) : string = tableToTeXStyled table []
