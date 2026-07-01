namespace FLPQ.Printers

open System.Collections.Generic
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for CYK algorithm tables.
module CykTeX =

    let private cellToTeX (symbolPrinter: Symbol<'t, 'nt> -> string) (cell: Cyk.CykCell<'t, 'nt>) : string =
        match cell with
        | None -> @"\cdot"
        | Some symbols ->
            symbols
            |> Seq.map symbolPrinter
            |> String.concat ", "
            |> fun s -> "\\{" + s + "\\}"

    /// Convert a CYK working table to TeX with highlighted cells.
    let tableToTeXStyled
        (symbolPrinter: Symbol<'t, 'nt> -> string)
        (table: Matrix<Cyk.CykCell<'t, 'nt>>)
        (highlights: Matrix.Highlight list)
        : string =
        MatrixTeX.toTeXStyled true true (cellToTeX symbolPrinter) table highlights []

    /// Convert a CYK working table to TeX.
    let tableToTeX (symbolPrinter: Symbol<'t, 'nt> -> string) (table: Matrix<Cyk.CykCell<'t, 'nt>>) : string =
        tableToTeXStyled symbolPrinter table []
