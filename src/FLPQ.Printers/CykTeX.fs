namespace FLPQ.Printers

open System.Collections.Generic
open System.Text.RegularExpressions
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for CYK algorithm tables.
module CykTeX =

    let private shortNtName (Nonterminal n) =
        Regex.Replace(string n, @"N_CNF_(\d+)", @"N_{$1}")

    let private shortSymbolPrinter (sym: Symbol<'t, 'nt>) : string =
        match sym with
        | T(Terminal t) -> string t
        | N nt -> shortNtName nt
        | Epsilon -> @"\varepsilon"

    let private cellToTeX (cell: Cyk.CykCell<'t, 'nt>) : string =
        match cell with
        | None -> @"\cdot"
        | Some symbols ->
            symbols
            |> Seq.map shortSymbolPrinter
            |> String.concat ", "
            |> fun s -> "\\{" + s + "\\}"

    /// Convert a CYK working table to TeX with highlighted cells.
    let tableToTeXStyled (table: Matrix<Cyk.CykCell<'t, 'nt>>) (highlights: Matrix.Highlight list) : string =
        MatrixTeX.toTeXStyled true true cellToTeX table highlights []

    /// Convert a CYK working table to TeX.
    let tableToTeX (table: Matrix<Cyk.CykCell<'t, 'nt>>) : string = tableToTeXStyled table []
