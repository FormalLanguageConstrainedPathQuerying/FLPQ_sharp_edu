namespace FLPQ.Printers

open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TeX rendering for CYK algorithm tables.
module CykTeX =

    /// Convert a CYK working table to TeX with highlighted cells.
    let tableToTeXStyled
        (nonterminalPrinter: 'nt -> string)
        (table: ParsingTable<'nt>)
        (highlights: Matrix.Highlight list)
        : string =
        MatrixTeX.toTeXStyled
            true
            true
            (ParsingTableTeX.ntCellToTeX nonterminalPrinter)
            table
            highlights
            []
            None
            None
            false
            false

    /// Convert a CYK working table to TeX.
    let tableToTeX (nonterminalPrinter: 'nt -> string) (table: ParsingTable<'nt>) : string =
        tableToTeXStyled nonterminalPrinter table []

    /// Convert a CYK SPPF working table to TeX with highlighted cells.
    let sppfTableToTeXStyled
        (nonterminalPrinter: 'nt -> string)
        (table: SppfParsingTable<'nt>)
        (highlights: Matrix.Highlight list)
        : string =
        MatrixTeX.toTeXStyled
            true
            true
            (ParsingTableTeX.sppfEntryCellToTeX nonterminalPrinter)
            table
            highlights
            []
            None
            None
            false
            false

    /// Convert a CYK SPPF working table to TeX.
    let sppfTableToTeX (nonterminalPrinter: 'nt -> string) (table: SppfParsingTable<'nt>) : string =
        sppfTableToTeXStyled nonterminalPrinter table []

    /// Convert a CYK SPPF working table to TeX with highlighted cells, rendering nonterminal names only.
    let sppfTableToTeXStyledAsNt
        (nonterminalPrinter: 'nt -> string)
        (table: SppfParsingTable<'nt>)
        (highlights: Matrix.Highlight list)
        : string =
        MatrixTeX.toTeXStyled
            true
            true
            (ParsingTableTeX.sppfEntryAsNtCellToTeX nonterminalPrinter)
            table
            highlights
            []
            None
            None
            false
            false

    /// Convert a CYK SPPF working table to TeX without SPPF entries (nonterminal names only).
    let sppfTableToTeXAsNt (nonterminalPrinter: 'nt -> string) (table: SppfParsingTable<'nt>) : string =
        sppfTableToTeXStyledAsNt nonterminalPrinter table []
