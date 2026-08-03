namespace FLPQ.Printers

open FLPQ.Languages

/// Common TeX rendering helpers for parsing algorithm tables (CYK, Valiant).
module ParsingTableTeX =

    /// Render a collection of items as a TeX set: \cdot for empty, \{...\} otherwise.
    let setToTeX (itemPrinter: 'a -> string) (items: 'a seq) : string =
        let arr = items |> Seq.toArray

        if arr.Length = 0 then
            @"\cdot"
        else
            arr
            |> Array.map itemPrinter
            |> String.concat ", "
            |> fun s -> "\\{" + s + "\\}"

    /// Render an optional collection of items as a TeX set.
    /// None → \cdot, Some(items) → \{...\}.
    let optionSetToTeX (itemPrinter: 'a -> string) (opt: 'a seq option) : string =
        match opt with
        | None -> @"\cdot"
        | Some items -> setToTeX itemPrinter items

    /// Render a set of nonterminals as a TeX cell: \cdot for empty, \{...\} otherwise.
    let ntCellToTeX (nonterminalPrinter: 'nt -> string) (s: Set<Nonterminal<'nt>>) : string =
        setToTeX (SymbolTeX.nonterminalContent nonterminalPrinter) s

    /// Render a boolean as TeX: 1 for true, \cdot for false.
    let boolToTeX (b: bool) : string = if b then "1" else @"\cdot"

    /// Render a set of SPPF parsing entries as TeX tuples.
    let sppfEntryCellToTeX (nonterminalPrinter: 'nt -> string) (entries: Set<SppfParsingEntry<'nt>>) : string =
        setToTeX
            (fun (nt, k, prodIdx) ->
                let ntStr = SymbolTeX.nonterminalContent nonterminalPrinter nt

                $"({ntStr}, {k}, {prodIdx})")
            entries
