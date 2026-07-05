namespace FLPQ.Printers

open FLPQ.Languages

/// Unified TeX rendering for grammar symbols.
/// Only the symbol content is printed, without type wrappers.
module SymbolTeX =

    /// Return the content of a terminal using the provided printer.
    let terminalContent (terminalPrinter: 't -> string) (Terminal t) : string = terminalPrinter t

    /// Return the content of a nonterminal using the provided printer.
    let nonterminalContent (nonterminalPrinter: 'nt -> string) (Nonterminal nt) : string = nonterminalPrinter nt

    /// Convert a grammar symbol to its TeX representation using the provided printers.
    let toLaTeX (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (sym: Symbol<'t, 'nt>) : string =
        match sym with
        | Symbol.T t -> terminalContent terminalPrinter t
        | Symbol.N nt -> nonterminalContent nonterminalPrinter nt
        | Symbol.Epsilon -> @"\varepsilon"
