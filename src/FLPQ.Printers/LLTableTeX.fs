namespace FLPQ.Printers

open FLPQ.Languages

/// TeX rendering for LL parsing tables.
module LLTableTeX =

    let private renderSet (symbolPrinter: Symbol<'t, 'nt> -> string) (s: Set<Symbol<'t, 'nt> list>) : string =
        if Set.isEmpty s then
            @"$\varnothing$"
        else
            let elements =
                s
                |> Set.toSeq
                |> Seq.map (fun syms ->
                    syms
                    |> List.map (fun sym ->
                        match sym with
                        | Epsilon -> @"\varepsilon"
                        | _ -> symbolPrinter sym)
                    |> String.concat " ")
                |> Seq.sort
                |> String.concat ", "

            @"$\{ " + elements + @" \}$"

    let private renderRule (symbolPrinter: Symbol<'t, 'nt> -> string) (rule: Rule<'t, 'nt>) : string =
        let lhs = symbolPrinter (N rule.lhs)

        let rhs =
            if Rhs.isEpsilon rule.rhs then
                @"\varepsilon"
            else
                Rhs.toSymbols rule.rhs
                |> List.map (fun sym ->
                    match sym with
                    | Epsilon -> @"\varepsilon"
                    | _ -> symbolPrinter sym)
                |> String.concat " "

        @"$" + lhs + @" \rightarrow " + rhs + @"$"

    /// Render the LL(k) parsing table as a TeX tabular.
    let tableToTeX
        (symbolPrinter: Symbol<'t, 'nt> -> string)
        (g: Grammar<'t, 'nt>)
        (k: int)
        (firstMap: Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>)
        (followMap: Map<Nonterminal<'nt>, Set<Symbol<'t, 'nt> list>>)
        (table: Map<Nonterminal<'nt> * Symbol<'t, 'nt> list, int>)
        : string =
        let sb = System.Text.StringBuilder()

        let nts = Grammar.nonterminalsOf g |> Set.toList
        let terms = Grammar.terminalsOf g |> Set.toList

        let colSpec =
            let termCols = String.replicate terms.Length "c | "
            @"\begin{tabular}{ r || c | c || " + termCols + @"c }"

        sb.AppendLine(@"\begin{center}") |> ignore
        sb.AppendLine(colSpec) |> ignore

        sb.Append(@"N & $\operatorname{First}$ & $\operatorname{Follow}$") |> ignore

        for t in terms do
            sb.Append(@" & " + symbolPrinter (T t)) |> ignore

        sb.Append(@" & $\$ $ \\ \hline") |> ignore

        for nt in nts do
            let firstSet = firstMap |> Map.tryFind nt |> Option.defaultValue Set.empty
            let followSet = followMap |> Map.tryFind nt |> Option.defaultValue Set.empty

            sb.Append(@"$" + symbolPrinter (N nt) + @"$") |> ignore
            sb.Append(@" & " + renderSet symbolPrinter firstSet) |> ignore
            sb.Append(@" & " + renderSet symbolPrinter followSet) |> ignore

            for t in terms do
                let key = (nt, [ T t ])

                let cell =
                    match Map.tryFind key table with
                    | Some ruleIdx -> renderRule symbolPrinter g.rules.[ruleIdx]
                    | None -> ""

                sb.Append(@" & " + cell) |> ignore

            let endKey = (nt, [ Epsilon ])

            let endCell =
                match Map.tryFind endKey table with
                | Some ruleIdx -> renderRule symbolPrinter g.rules.[ruleIdx]
                | None -> ""

            sb.Append(@" & " + endCell + @" \\ \hline") |> ignore

        sb.AppendLine(@"\end{tabular}") |> ignore
        sb.AppendLine(@"\end{center}") |> ignore

        sb.ToString()
