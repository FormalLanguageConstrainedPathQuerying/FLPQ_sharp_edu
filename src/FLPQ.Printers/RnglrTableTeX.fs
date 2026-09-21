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

    /// A table cell to highlight and its color. ShiftAction → \cellcolor{green!20};
    /// ReduceAction / ReduceGoto → \cellcolor{red!20}.
    type private CellHighlight<'t, 'nt when 't: comparison and 'nt: comparison> =
        | ShiftAction of state: int * symbol: Symbol<'t, 'nt>
        | ReduceAction of state: int * symbol: Symbol<'t, 'nt>
        | ReduceGoto of state: int * nonterminal: Nonterminal<'nt>

    /// Build the TeX tabular environment with optional cell highlights.
    let private buildTabularCore
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        (highlights: Set<CellHighlight<'t, 'nt>>)
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
                let sym = Symbol.T(Terminal t)

                let cellText =
                    match Map.tryFind (state, sym) table.Action with
                    | Some a -> actionStr a nonterminalPrinter
                    | None -> ""

                let cell =
                    if Set.contains (ShiftAction(state, sym)) highlights then
                        sprintf @"\cellcolor{green!20} %s" cellText
                    elif Set.contains (ReduceAction(state, sym)) highlights then
                        sprintf @"\cellcolor{red!20} %s" cellText
                    else
                        cellText

                sb.Append(@" & " + cell) |> ignore

            let endCellText =
                match Map.tryFind (state, Symbol.Epsilon) table.Action with
                | Some a -> actionStr a nonterminalPrinter
                | None -> ""

            let endCell =
                if Set.contains (ShiftAction(state, Symbol.Epsilon)) highlights then
                    sprintf @"\cellcolor{green!20} %s" endCellText
                elif Set.contains (ReduceAction(state, Symbol.Epsilon)) highlights then
                    sprintf @"\cellcolor{red!20} %s" endCellText
                else
                    endCellText

            sb.Append(@" & " + endCell) |> ignore

            for nt in nonterminals do
                let cellText =
                    match Map.tryFind (state, nt) table.Goto with
                    | Some n -> string n
                    | None -> ""

                let cell =
                    if Set.contains (ReduceGoto(state, nt)) highlights then
                        sprintf @"\cellcolor{red!20} %s" cellText
                    else
                        cellText

                sb.Append(@" & " + cell) |> ignore

            let rowEnd = if state = nStates - 1 then @" \\ [1ex]" else @" \\"
            sb.AppendLine(rowEnd) |> ignore

        sb.AppendLine(@"\end{tabular}") |> ignore

        sb.ToString()

    /// Build only the TeX tabular environment (no center wrapper).
    let private buildTabular
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        : string =
        buildTabularCore terminalPrinter nonterminalPrinter table Set.empty

    /// Render an RNGLR parsing table as a self-contained TeX block (center + tabular).
    let tableToTeX
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        : string =
        let tabular = buildTabular terminalPrinter nonterminalPrinter table
        [ @"\begin{center}"; tabular; @"\end{center}" ] |> String.concat "\n"

    /// Render an RNGLR parsing table as a TeX tabular without any wrapper.
    let tableToTeXTabularOnly
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        : string =
        buildTabular terminalPrinter nonterminalPrinter table

    /// Maps one RNGLR action to the table cells it highlights:
    /// Shift → Action cell (lrState, terminal); Reduce → Goto cell (gotoLrState, nt) plus
    /// Action cell (triggerLrState, $).
    let private actionHighlightCells (action: RnglrAction<'t, 'nt>) : Set<CellHighlight<'t, 'nt>> =
        match action with
        | RnglrAction.Shift(Terminal t, lrState) -> set [ ShiftAction(lrState, Symbol.T(Terminal t)) ]
        | RnglrAction.Reduce(nt, triggerLrState, gotoLrState) ->
            set [ ReduceAction(triggerLrState, Symbol.Epsilon); ReduceGoto(gotoLrState, nt) ]

    /// Render an RNGLR parsing table with the cells of one action highlighted (center + tabular).
    /// Shift: the Action cell (lrState, terminal) gets \cellcolor{green!20}.
    /// Reduce: the Goto cell (gotoLrState, nonterminal) and the Action cell (triggerLrState, $)
    ///   get \cellcolor{red!20}. Requires \usepackage[table]{xcolor} in the preamble.
    let tableToTeXWithActionHighlight
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        (action: RnglrAction<'t, 'nt>)
        : string =
        let highlights = actionHighlightCells action

        let tabular = buildTabularCore terminalPrinter nonterminalPrinter table highlights
        [ @"\begin{center}"; tabular; @"\end{center}" ] |> String.concat "\n"

    /// Render an RNGLR parsing table with the cells of one action highlighted as a TeX tabular
    /// without any wrapper. Highlight rule: see tableToTeXWithActionHighlight.
    let tableToTeXWithActionHighlightTabularOnly
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (table: RnglrTable<'t, 'nt>)
        (action: RnglrAction<'t, 'nt>)
        : string =
        buildTabularCore terminalPrinter nonterminalPrinter table (actionHighlightCells action)
