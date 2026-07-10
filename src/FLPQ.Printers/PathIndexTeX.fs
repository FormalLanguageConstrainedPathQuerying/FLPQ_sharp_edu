namespace FLPQ.Printers

open FLPQ.Languages
open FLPQ.LinearAlgebra

/// TeX rendering for path index matrices.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module PathIndexTeX =

    /// Converts a set of path index entries to a compact TeX string.
    let private cellPrinter
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (entries: Set<PathIndexEntry<'t, 'nt>>)
        : string =
        if Set.isEmpty entries then
            @"\cdot"
        else
            entries
            |> Set.toList
            |> List.map (fun entry ->
                match entry with
                | PathIndexEntry.PTerminal(Terminal t) -> terminalPrinter t
                | PathIndexEntry.PNonterminal(Nonterminal nt) -> sprintf @"R_{%s}" (nonterminalPrinter nt)
                | PathIndexEntry.PEpsilonNonterminal(Nonterminal nt) ->
                    sprintf @"R_{%s}^{\varepsilon}" (nonterminalPrinter nt)
                | PathIndexEntry.PIntermediate(s, p) -> sprintf @"I_{%d,%d}" s p)
            |> String.concat @"\text{---}"

    /// Renders a path index as a parametrized matrix using the nicematrix package.
    /// Row and column labels show (rsm_state, input_position) pairs.
    let toTeX
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (pathIndex: PathIndex<'t, 'nt>)
        : string =
        let cp = cellPrinter terminalPrinter nonterminalPrinter

        let labelPrinter (idx: int) =
            let state = idx / pathIndex.VertexCount
            let vertex = idx % pathIndex.VertexCount
            $"%d{state},%d{vertex}"

        let matrix =
            MatrixTeX.toTeXStyled false false cp pathIndex.Matrix [] [] (Some labelPrinter) (Some labelPrinter)

        sprintf "{\\footnotesize %s}" matrix
