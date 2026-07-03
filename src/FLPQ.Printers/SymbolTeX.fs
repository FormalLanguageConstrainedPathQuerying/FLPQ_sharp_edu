namespace FLPQ.Printers

open FLPQ.Languages

/// Unified TeX rendering for grammar symbols.
/// Only the symbol content is printed, without type wrappers.
module SymbolTeX =

    /// Convert a grammar symbol to its TeX representation.
    let toLaTeX (sym: Symbol<'t, 'nt>) : string =
        match sym with
        | T(Terminal t) -> string t
        | N(Nonterminal nt) -> string nt
        | Epsilon -> @"\varepsilon"
