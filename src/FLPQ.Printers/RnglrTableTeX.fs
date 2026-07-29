namespace FLPQ.Printers

open FLPQ.Languages

/// TeX rendering for RNGLR parsing tables.
/// Book reference: sec:CFPQ_RNGLR.
module RnglrTableTeX =

    let private actionStr (action: LRAction<Nonterminal<'nt>>) (nonterminalPrinter: 'nt -> string) : string =
        match action with
        | LRAction.Shift n -> sprintf "$s_%d$" n
        | LRAction.Reduce(Nonterminal nt) -> sprintf "$r_{%s}$" (nonterminalPrinter nt)
        | LRAction.Accept -> "acc"

    let private stateCount (table: RnglrTable<'t, 'nt>) : int =
        let mutable maxState = -1

        for (s, _) in Map.keys table.Action do
            maxState <- max maxState s

        for (s, _) in Map.keys table.Goto do
            maxState <- max maxState s

        maxState + 1

    /// Render an RNGLR parsing table as a TeX tabular.
    let tableToTeX
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        : string =
        let sb = System.Text.StringBuilder()

        let terminals =
            table.Action
            |> Map.keys
            |> Seq.choose (fun (_, sym) ->
                match sym with
                | Symbol.T(Terminal t) -> Some t
                | _ -> None)
            |> Seq.distinct
            |> List.ofSeq

        let nonterminals =
            table.Goto |> Map.keys |> Seq.map snd |> Seq.distinct |> List.ofSeq

        let nStates = stateCount table

        let actionCols = String.replicate terminals.Length "c | " + "c"

        let gotoCols =
            if nonterminals.Length > 0 then
                String.replicate (nonterminals.Length - 1) "c | " + "c"
            else
                ""

        let colSpec = @"\begin{tabular}{ c || " + actionCols + @" || " + gotoCols + @" }"

        sb.AppendLine(@"\begin{center}") |> ignore
        sb.AppendLine(colSpec) |> ignore

        sb.Append(@" & ") |> ignore

        for t in terminals do
            sb.Append(terminalPrinter t + @" & ") |> ignore

        sb.Append(@"\$ & ") |> ignore

        for i in 0 .. nonterminals.Length - 1 do
            let (Nonterminal nt) = nonterminals.[i]
            sb.Append(nonterminalPrinter nt) |> ignore

            if i < nonterminals.Length - 1 then
                sb.Append(@" & ") |> ignore

        sb.AppendLine(@" \\ \hline") |> ignore

        for state in 0 .. nStates - 1 do
            sb.Append(sprintf @"\hline %d" state) |> ignore

            for t in terminals do
                let cell =
                    match Map.tryFind (state, Symbol.T(Terminal t)) table.Action with
                    | Some a -> actionStr a nonterminalPrinter
                    | None -> ""

                sb.Append(@" & " + cell) |> ignore

            let endCell =
                match Map.tryFind (state, Symbol.Epsilon) table.Action with
                | Some a -> actionStr a nonterminalPrinter
                | None -> ""

            sb.Append(@" & " + endCell) |> ignore

            for nt in nonterminals do
                let cell =
                    match Map.tryFind (state, nt) table.Goto with
                    | Some n -> string n
                    | None -> ""

                sb.Append(@" & " + cell) |> ignore

            let rowEnd = if state = nStates - 1 then @" \\ [1ex]" else @" \\"
            sb.AppendLine(rowEnd) |> ignore

        sb.AppendLine(@"\end{tabular}") |> ignore
        sb.AppendLine(@"\end{center}") |> ignore

        sb.ToString()

    /// Render an RNGLR parsing table with highlighted cells.
    /// currentLrState: row to highlight with \rowcolor{yellow!20}.
    /// activeActions: cells with these symbols get \cellcolor{green!20}.
    /// levelReductions: GOTO cells for these nonterminals get \cellcolor{red!20}
    ///   (overrides green). Requires \usepackage[table]{xcolor} in the preamble.
    let tableToTeXWithHighlights
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        (currentLrState: int option)
        (activeActions: Set<Symbol<'t, 'nt>>)
        (levelReductions: Set<Nonterminal<'nt>>)
        : string =
        let sb = System.Text.StringBuilder()

        let terminals =
            table.Action
            |> Map.keys
            |> Seq.choose (fun (_, sym) ->
                match sym with
                | Symbol.T(Terminal t) -> Some t
                | _ -> None)
            |> Seq.distinct
            |> List.ofSeq

        let nonterminals =
            table.Goto |> Map.keys |> Seq.map snd |> Seq.distinct |> List.ofSeq

        let nStates = stateCount table

        let actionCols = String.replicate terminals.Length "c | " + "c"

        let gotoCols =
            if nonterminals.Length > 0 then
                String.replicate (nonterminals.Length - 1) "c | " + "c"
            else
                ""

        let colSpec = @"\begin{tabular}{ c || " + actionCols + @" || " + gotoCols + @" }"

        sb.AppendLine(@"\begin{center}") |> ignore
        sb.AppendLine(colSpec) |> ignore

        sb.Append(@" & ") |> ignore

        for t in terminals do
            sb.Append(terminalPrinter t + @" & ") |> ignore

        sb.Append(@"\$ & ") |> ignore

        for i in 0 .. nonterminals.Length - 1 do
            let (Nonterminal nt) = nonterminals.[i]
            sb.Append(nonterminalPrinter nt) |> ignore

            if i < nonterminals.Length - 1 then
                sb.Append(@" & ") |> ignore

        sb.AppendLine(@" \\ \hline") |> ignore

        for state in 0 .. nStates - 1 do
            let isCurrentRow =
                match currentLrState with
                | Some s -> s = state
                | None -> false

            if isCurrentRow then
                sb.Append(sprintf @"\rowcolor{yellow!20} \hline %d" state) |> ignore
            else
                sb.Append(sprintf @"\hline %d" state) |> ignore

            for t in terminals do
                let sym = Symbol.T(Terminal t)
                let isActive = Set.contains sym activeActions

                let cellText =
                    match Map.tryFind (state, sym) table.Action with
                    | Some a -> actionStr a nonterminalPrinter
                    | None -> ""

                let cell =
                    if isActive then
                        sprintf @"\cellcolor{green!20} %s" cellText
                    else
                        cellText

                sb.Append(@" & " + cell) |> ignore

            let endCell =
                match Map.tryFind (state, Symbol.Epsilon) table.Action with
                | Some a -> actionStr a nonterminalPrinter
                | None -> ""

            sb.Append(@" & " + endCell) |> ignore

            for nt in nonterminals do
                let isLevelRed = Set.contains nt levelReductions
                let sym = Symbol.N nt
                let isActive = Set.contains sym activeActions

                let cellText =
                    match Map.tryFind (state, nt) table.Goto with
                    | Some n -> string n
                    | None -> ""

                let cell =
                    if isLevelRed then
                        sprintf @"\cellcolor{red!20} %s" cellText
                    elif isActive then
                        sprintf @"\cellcolor{green!20} %s" cellText
                    else
                        cellText

                sb.Append(@" & " + cell) |> ignore

            let rowEnd = if state = nStates - 1 then @" \\ [1ex]" else @" \\"
            sb.AppendLine(rowEnd) |> ignore

        sb.AppendLine(@"\end{tabular}") |> ignore
        sb.AppendLine(@"\end{center}") |> ignore

        sb.ToString()
