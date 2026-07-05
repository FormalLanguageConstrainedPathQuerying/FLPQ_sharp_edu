namespace FLPQ.Languages

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

    let private computeCell
        (rules: Rule<'t, 'nt> list)
        (table: ParsingTable<'nt>)
        (i: int)
        (j: int)
        : Set<Nonterminal<'nt>> =
        seq { i .. j - 1 }
        |> Seq.collect (fun k ->
            let leftSet = table.data.[i, k]
            let rightSet = table.data.[k + 1, j]

            if Set.isEmpty leftSet || Set.isEmpty rightSet then
                Seq.empty
            else
                leftSet
                |> Seq.collect (fun leftNt ->
                    rightSet
                    |> Seq.collect (fun rightNt -> findBinaryProductions rules leftNt rightNt |> List.toSeq)))
        |> Set.ofSeq

    let private cykCore
        (cnf: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        (onDiagonalCell: int -> Set<Nonterminal<'nt>> -> unit)
        (onCellFound: int -> int -> Set<Nonterminal<'nt>> -> unit)
        (onLengthDone: ParsingTable<'nt> -> int -> unit)
        : ParsingTable<'nt> =
        let n = terminals.Length
        let table = Matrix.init n n Set.empty

        for i in 0 .. n - 1 do
            let producing = findTerminalRules cnf.rules terminals.[i]

            if not (List.isEmpty producing) then
                let ntSet = Set.ofList producing
                table.data.[i, i] <- ntSet
                onDiagonalCell i ntSet

        onLengthDone table 1

        for len in 2..n do
            for i in 0 .. n - len do
                let j = i + len - 1
                let accumulated = computeCell cnf.rules table i j

                if not (Set.isEmpty accumulated) then
                    table.data.[i, j] <- accumulated

                onCellFound i j accumulated

            onLengthDone table len

        table

    let private cykTable (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : ParsingTable<'nt> =
        cykCore cnf terminals (fun _ _ -> ()) (fun _ _ _ -> ()) (fun _ _ -> ())

    let private tableTrace (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : CykTraceStep<'nt> list =
        let steps = ResizeArray<CykTraceStep<'nt>>()
        let mutable stepHighlights = []

        let onDiagonalCell i _ =
            let h: Matrix.Highlight = { row = i; col = i; color = "yellow" }
            stepHighlights <- h :: stepHighlights

        let onCellFound i j accumulated =
            if not (Set.isEmpty accumulated) then
                let h: Matrix.Highlight = { row = i; col = j; color = "yellow" }
                stepHighlights <- h :: stepHighlights

        let onLengthDone table _len =
            steps.Add(
                { table =
                    { rows = table.rows
                      cols = table.cols
                      data = Array2D.copy table.data }
                  highlights = List.rev stepHighlights }
            )

            stepHighlights <- []

        cykCore cnf terminals onDiagonalCell onCellFound onLengthDone |> ignore
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
