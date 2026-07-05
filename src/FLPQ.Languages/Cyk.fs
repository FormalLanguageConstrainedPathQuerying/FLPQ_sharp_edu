namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra

module Cyk =

    /// Data for a single CYK algorithm trace step.
    [<Struct>]
    type CykTraceStep<'nt when 'nt: comparison> =
        { table: ParsingTable<'nt>
          highlights: Matrix.Highlight list }

    let private findTerminalRules (rules: Rule<'t, 'nt> list) (t: Terminal<'t>) : Nonterminal<'nt> list =
        rules
        |> List.choose (fun rule ->
            match rule.rhs with
            | Symbols nel when NonEmptyList.length nel = 1 ->
                match NonEmptyList.head nel with
                | T t' when t' = t -> Some rule.lhs
                | _ -> None
            | _ -> None)

    let private findBinaryProductions
        (rules: Rule<'t, 'nt> list)
        (left: Nonterminal<'nt>)
        (right: Nonterminal<'nt>)
        : Nonterminal<'nt> list =
        rules
        |> List.choose (fun rule ->
            match rule.rhs with
            | Symbols nel when NonEmptyList.length nel = 2 ->
                let syms = NonEmptyList.toList nel

                match syms.[0], syms.[1] with
                | N l, N r when l = left && r = right -> Some rule.lhs
                | _ -> None
            | _ -> None)

    let private cykTable (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : ParsingTable<'nt> =
        let n = terminals.Length
        let table = Matrix.init n n Set.empty

        for i in 0 .. n - 1 do
            let producing = findTerminalRules cnf.rules terminals.[i]

            if not (List.isEmpty producing) then
                table.data.[i, i] <- Set.ofList producing

        for len in 2..n do
            for i in 0 .. n - len do
                let j = i + len - 1
                let mutable accumulated = HashSet<Nonterminal<'nt>>()

                for k in i .. j - 1 do
                    let leftSet = table.data.[i, k]
                    let rightSet = table.data.[k + 1, j]

                    if not (Set.isEmpty leftSet) && not (Set.isEmpty rightSet) then
                        for leftNt in leftSet do
                            for rightNt in rightSet do
                                let producers = findBinaryProductions cnf.rules leftNt rightNt

                                for nt in producers do
                                    accumulated.Add(nt) |> ignore

                if accumulated.Count > 0 then
                    table.data.[i, j] <- Set.ofSeq accumulated

        table

    let private tableTrace (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : CykTraceStep<'nt> list =
        let n = terminals.Length
        let table = Matrix.init n n Set.empty
        let steps = ResizeArray<CykTraceStep<'nt>>()

        let mutable stepHighlights = []

        for i in 0 .. n - 1 do
            let producing = findTerminalRules cnf.rules terminals.[i]

            if not (List.isEmpty producing) then
                table.data.[i, i] <- Set.ofList producing
                let h: Matrix.Highlight = { row = i; col = i; color = "yellow" }
                stepHighlights <- h :: stepHighlights

        steps.Add(
            { table =
                { rows = table.rows
                  cols = table.cols
                  data = Array2D.copy table.data }
              highlights = List.rev stepHighlights }
        )

        for len in 2..n do
            stepHighlights <- []

            for i in 0 .. n - len do
                let j = i + len - 1
                let mutable accumulated = HashSet<Nonterminal<'nt>>()

                for k in i .. j - 1 do
                    let leftSet = table.data.[i, k]
                    let rightSet = table.data.[k + 1, j]

                    if not (Set.isEmpty leftSet) && not (Set.isEmpty rightSet) then
                        for leftNt in leftSet do
                            for rightNt in rightSet do
                                let producers = findBinaryProductions cnf.rules leftNt rightNt

                                for nt in producers do
                                    accumulated.Add(nt) |> ignore

                if accumulated.Count > 0 then
                    table.data.[i, j] <- Set.ofSeq accumulated
                    let h: Matrix.Highlight = { row = i; col = j; color = "yellow" }
                    stepHighlights <- h :: stepHighlights

            steps.Add(
                { table =
                    { rows = table.rows
                      cols = table.cols
                      data = Array2D.copy table.data }
                  highlights = List.rev stepHighlights }
            )

        steps |> List.ofSeq

    let private isAccepted (cnf: Grammar<'t, 'nt>) (table: ParsingTable<'nt>) : bool =
        let n = table.rows

        if n = 0 then
            false
        else
            Set.contains cnf.start table.data.[0, n - 1]

    /// Parse pre-tokenized input using CYK algorithm.
    let parse (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : bool =
        let cnf = Grammar.toCnf freshNonterminal g

        if terminals.IsEmpty then
            Grammar.isEpsilonAccepted cnf
        else
            let table = cykTable cnf terminals
            isAccepted cnf table

    /// Run CYK and return the final table and acceptance status.
    let parseWithTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : ParsingTable<'nt> * bool =
        let cnf = Grammar.toCnf freshNonterminal g

        if terminals.IsEmpty then
            let epsAccepted = Grammar.isEpsilonAccepted cnf
            let emptyResult: ParsingTable<'nt> = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let table = cykTable cnf terminals

            let accepted = isAccepted cnf table
            (table, accepted)

    /// Run CYK and return the sequence of working table states with highlights.
    let parseWithTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : CykTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        tableTrace cnf terminals
