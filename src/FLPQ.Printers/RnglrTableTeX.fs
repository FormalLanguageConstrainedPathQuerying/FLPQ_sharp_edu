namespace FLPQ.Printers

open FLPQ.Languages

/// TeX rendering for RNGLR parsing tables.
/// Book reference: sec:CFPQ_RNGLR.
module RnglrTableTeX =

    let private actionStr (action: RnglrAction<'nt>) (nonterminalPrinter: 'nt -> string) : string =
        match action with
        | RnglrAction.Shift n -> sprintf "$s_%d$" n
        | RnglrAction.Reduce(Nonterminal nt) -> sprintf "$r_{%s}$" (nonterminalPrinter nt)
        | RnglrAction.Accept -> "acc"

    let private stateCount (table: RnglrTable<'t, 'nt>) : int =
        let mutable maxState = -1

        for (s, _) in Map.keys table.action do
            maxState <- max maxState s

        for (s, _) in Map.keys table.goto do
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
            table.action
            |> Map.keys
            |> Seq.choose (fun (_, sym) ->
                match sym with
                | Symbol.T(Terminal t) -> Some t
                | _ -> None)
            |> Seq.distinct
            |> List.ofSeq

        let nonterminals =
            table.goto |> Map.keys |> Seq.map snd |> Seq.distinct |> List.ofSeq

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
                    match Map.tryFind (state, Symbol.T(Terminal t)) table.action with
                    | Some a -> actionStr a nonterminalPrinter
                    | None -> ""

                sb.Append(@" & " + cell) |> ignore

            let endCell =
                match Map.tryFind (state, Symbol.Epsilon) table.action with
                | Some a -> actionStr a nonterminalPrinter
                | None -> ""

            sb.Append(@" & " + endCell) |> ignore

            for nt in nonterminals do
                let cell =
                    match Map.tryFind (state, nt) table.goto with
                    | Some n -> string n
                    | None -> ""

                sb.Append(@" & " + cell) |> ignore

            let rowEnd = if state = nStates - 1 then @" \\ [1ex]" else @" \\"
            sb.AppendLine(rowEnd) |> ignore

        sb.AppendLine(@"\end{tabular}") |> ignore
        sb.AppendLine(@"\end{center}") |> ignore

        sb.ToString()
