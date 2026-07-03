namespace FLPQ.Printers

open FLPQ.Languages

/// Unified TeX rendering for grammar symbols.
/// Only the symbol content is printed, without type wrappers.
module SymbolTeX =

    /// Return the content of a terminal.
    let terminalContent (Terminal t) : string = string t

    /// Return the content of a nonterminal.
    let nonterminalContent (Nonterminal nt) : string = string nt

    /// Convert a grammar symbol to its TeX representation.
    let toLaTeX (sym: Symbol<'t, 'nt>) : string =
        match sym with
        | T t -> terminalContent t
        | N nt -> nonterminalContent nt
        | Epsilon -> @"\varepsilon"
