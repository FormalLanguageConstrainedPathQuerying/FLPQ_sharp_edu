namespace FLPQ.Printers

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
