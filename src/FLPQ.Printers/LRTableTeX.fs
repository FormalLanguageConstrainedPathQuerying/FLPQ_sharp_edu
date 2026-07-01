namespace FLPQ.Printers

open FLPQ.Languages

/// TeX rendering for LR parsing tables.
/// Works for LR(0), SLR(1), and CLR(1) tables.
module LRTableTeX =

    let private actionStr (action: LRAction) : string =
        match action with
        | Shift n -> sprintf "$s_%d$" n
        | Reduce r -> sprintf "$r_%d$" r
        | Accept -> "acc"

    let private allActionsFor (table: LRTable<'t, 'nt>) (state: int) (sym: Symbol<'t, 'nt>) : LRAction list =
        let fromMap =
            match Map.tryFind (state, sym) table.action with
            | Some a -> [ a ]
            | None -> []

        let fromConflicts =
            table.conflicts
            |> List.collect (fun c ->
                match c with
                | ShiftReduce(s, sym', shiftTo, reduceRule) when s = state && sym' = sym ->
                    [ Shift shiftTo; Reduce reduceRule ]
                | ReduceReduce(s, sym', r1, r2) when s = state && sym' = sym -> [ Reduce r1; Reduce r2 ]
                | _ -> [])

        (fromConflicts @ fromMap) |> List.distinct

    let private actionCell (table: LRTable<'t, 'nt>) (state: int) (sym: Symbol<'t, 'nt>) : string =
        let actions = allActionsFor table state sym

        match actions with
        | [] -> ""
        | [ a ] -> actionStr a
        | _ -> actions |> List.map actionStr |> String.concat ", "

    let private gotoCell (table: LRTable<'t, 'nt>) (state: int) (nt: Nonterminal<'nt>) : string =
        match Map.tryFind (state, nt) table.goto with
        | Some n -> string n
        | None -> ""

    let private stateCount (table: LRTable<'t, 'nt>) : int =
        let mutable maxState = -1

        for (s, _) in Map.keys table.action do
            maxState <- max maxState s

        for (s, _) in Map.keys table.goto do
            maxState <- max maxState s

        maxState + 1

    /// Render an LR parsing table as a TeX tabular.
    let tableToTeX
        (symbolPrinter: Symbol<'t, 'nt> -> string)
        (aug: Grammar<'t, 'nt>)
        (table: LRTable<'t, 'nt>)
        : string =
        let sb = System.Text.StringBuilder()

        let augmentedStart = aug.rules.[0].lhs

        let terminals =
            aug.rules
            |> List.collect (fun r ->
                Rhs.toSymbols r.rhs
                |> List.choose (fun sym ->
                    match sym with
                    | T t -> Some t
                    | _ -> None))
            |> List.distinct

        let nonterminals =
            aug.rules
            |> List.map (fun r -> r.lhs)
            |> List.distinct
            |> List.filter (fun nt -> nt <> augmentedStart)

        let nStates = stateCount table

        let colSpec =
            let actionCols = String.replicate (terminals.Length + 1) "c | "
            let gotoCols = String.replicate nonterminals.Length "c | "

            @"\begin{tabular}{ c || "
            + actionCols
            + @"|| "
            + gotoCols.TrimEnd(' ', '|')
            + @" }"

        sb.AppendLine(@"\begin{center}") |> ignore
        sb.AppendLine(colSpec) |> ignore

        sb.Append(@" & ") |> ignore

        for t in terminals do
            sb.Append(symbolPrinter (T t) + @" & ") |> ignore

        sb.Append(@"\$ & ") |> ignore

        for i in 0 .. nonterminals.Length - 1 do
            let nt = nonterminals.[i]
            sb.Append(symbolPrinter (N nt)) |> ignore

            if i < nonterminals.Length - 1 then
                sb.Append(@" & ") |> ignore

        sb.AppendLine(@" \\ \hline") |> ignore

        for state in 0 .. nStates - 1 do
            sb.Append(sprintf @"\hline %d" state) |> ignore

            for t in terminals do
                let cell = actionCell table state (T t)
                sb.Append(@" & " + cell) |> ignore

            let endCell = actionCell table state Epsilon
            sb.Append(@" & " + endCell) |> ignore

            for nt in nonterminals do
                let cell = gotoCell table state nt
                sb.Append(@" & " + cell) |> ignore

            let rowEnd = if state = nStates - 1 then @" \\ [1ex]" else @" \\ \hline"
            sb.AppendLine(rowEnd) |> ignore

        sb.AppendLine(@"\end{tabular}") |> ignore
        sb.AppendLine(@"\end{center}") |> ignore

        sb.ToString()
