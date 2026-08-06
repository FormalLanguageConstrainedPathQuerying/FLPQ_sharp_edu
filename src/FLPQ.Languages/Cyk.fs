namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra

module Cyk =

    /// Data for a single CYK algorithm trace step.
    [<Struct>]
    type CykTraceStep<'nt when 'nt: comparison> =
        { Table: ParsingTable<'nt>
          Highlights: Matrix.Highlight list }

    /// Data for a single CYK algorithm trace step with SPPF entries.
    [<Struct>]
    type CykSppfTraceStep<'nt when 'nt: comparison> =
        { Table: SppfParsingTable<'nt>
          Highlights: Matrix.Highlight list }

    let private findTerminalRules (rules: Rule<'t, 'nt> list) (t: Terminal<'t>) : Nonterminal<'nt> list =
        rules
        |> List.choose (fun rule ->
            match rule.Rhs with
            | Symbols nel when NonEmptyList.length nel = 1 ->
                match NonEmptyList.head nel with
                | Symbol.T t' when t' = t -> Some rule.Lhs
                | _ -> None
            | _ -> None)

    let private findBinaryProductions
        (rules: Rule<'t, 'nt> list)
        (left: Nonterminal<'nt>)
        (right: Nonterminal<'nt>)
        : Nonterminal<'nt> list =
        rules
        |> List.choose (fun rule ->
            match rule.Rhs with
            | Symbols nel when NonEmptyList.length nel = 2 ->
                let syms = NonEmptyList.toList nel

                match syms.[0], syms.[1] with
                | Symbol.N l, Symbol.N r when l = left && r = right -> Some rule.Lhs
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
            let leftSet = table.[i, k]
            let rightSet = table.[k + 1, j]

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
            let producing = findTerminalRules cnf.Rules terminals.[i]

            if not (List.isEmpty producing) then
                let ntSet = Set.ofList producing
                table.[i, i] <- ntSet
                onDiagonalCell i ntSet

        onLengthDone table 1

        for len in 2..n do
            for i in 0 .. n - len do
                let j = i + len - 1
                let accumulated = computeCell cnf.Rules table i j

                if not (Set.isEmpty accumulated) then
                    table.[i, j] <- accumulated

                onCellFound i j accumulated

            onLengthDone table len

        table

    let private cykTable (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : ParsingTable<'nt> =
        cykCore cnf terminals (fun _ _ -> ()) (fun _ _ _ -> ()) (fun _ _ -> ())

    let private tableTrace (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : CykTraceStep<'nt> list =
        let steps = ResizeArray<CykTraceStep<'nt>>()
        let mutable stepHighlights = []

        let onDiagonalCell i _ =
            let h: Matrix.Highlight =
                { Row = i
                  Col = i
                  Label = Matrix.CurrentCell }

            stepHighlights <- h :: stepHighlights

        let onCellFound i j accumulated =
            if not (Set.isEmpty accumulated) then
                let h: Matrix.Highlight =
                    { Row = i
                      Col = j
                      Label = Matrix.CurrentCell }

                stepHighlights <- h :: stepHighlights

        let onLengthDone table _len =
            steps.Add(
                { Table = Matrix.create (Matrix.rows table) (Matrix.cols table) (fun i j -> table.[i, j])
                  Highlights = List.rev stepHighlights }
            )

            stepHighlights <- []

        cykCore cnf terminals onDiagonalCell onCellFound onLengthDone |> ignore
        steps |> List.ofSeq

    let private isAccepted (cnf: Grammar<'t, 'nt>) (table: ParsingTable<'nt>) : bool =
        let n = Matrix.rows table

        if n = 0 then
            false
        else
            Set.contains cnf.Start table.[0, n - 1]

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

    let private findTerminalRulesWithProdIdx
        (rules: Rule<'t, 'nt> list)
        (t: Terminal<'t>)
        : (Nonterminal<'nt> * int) list =
        rules
        |> List.indexed
        |> List.choose (fun (idx, rule) ->
            match rule.Rhs with
            | Symbols nel when NonEmptyList.length nel = 1 ->
                match NonEmptyList.head nel with
                | Symbol.T t' when t' = t -> Some(rule.Lhs, idx)
                | _ -> None
            | _ -> None)

    let private findBinaryProductionsWithProdIdx
        (rules: Rule<'t, 'nt> list)
        (left: Nonterminal<'nt>)
        (right: Nonterminal<'nt>)
        : (Nonterminal<'nt> * int) list =
        rules
        |> List.indexed
        |> List.choose (fun (idx, rule) ->
            match rule.Rhs with
            | Symbols nel when NonEmptyList.length nel = 2 ->
                let syms = NonEmptyList.toList nel

                match syms.[0], syms.[1] with
                | Symbol.N l, Symbol.N r when l = left && r = right -> Some(rule.Lhs, idx)
                | _ -> None
            | _ -> None)

    let private computeCellSppf
        (rules: Rule<'t, 'nt> list)
        (table: SppfParsingTable<'nt>)
        (i: int)
        (j: int)
        : Set<SppfParsingEntry<'nt>> =
        seq { i .. j - 1 }
        |> Seq.collect (fun k ->
            let leftSet = table.[i, k]
            let rightSet = table.[k + 1, j]

            if Set.isEmpty leftSet || Set.isEmpty rightSet then
                Seq.empty
            else
                leftSet
                |> Seq.collect (fun leftEntry ->
                    rightSet
                    |> Seq.collect (fun rightEntry ->
                        findBinaryProductionsWithProdIdx rules leftEntry.Nt rightEntry.Nt
                        |> List.map (fun (lhs, prodIdx) ->
                            { Nt = lhs
                              SplitPoint = k
                              ProdIdx = prodIdx })
                        |> List.toSeq)))
        |> Set.ofSeq

    let private cykSppfCore (cnf: Grammar<'t, 'nt>) (terminals: Terminal<'t> list) : SppfParsingTable<'nt> =
        let n = terminals.Length
        let table = Matrix.init n n Set.empty

        for i in 0 .. n - 1 do
            let producing = findTerminalRulesWithProdIdx cnf.Rules terminals.[i]

            if not (List.isEmpty producing) then
                let entries =
                    producing
                    |> List.map (fun (nt, prodIdx) ->
                        { Nt = nt
                          SplitPoint = i
                          ProdIdx = prodIdx })
                    |> Set.ofList

                table.[i, i] <- entries

        for len in 2..n do
            for i in 0 .. n - len do
                let j = i + len - 1
                let accumulated = computeCellSppf cnf.Rules table i j

                if not (Set.isEmpty accumulated) then
                    table.[i, j] <- accumulated

        table

    let private isSppfAccepted (cnf: Grammar<'t, 'nt>) (table: SppfParsingTable<'nt>) : bool =
        let n = Matrix.rows table

        if n = 0 then
            false
        else
            table.[0, n - 1] |> Set.exists (fun entry -> entry.Nt = cnf.Start)

    /// Run CYK and return an enriched parsing table with SPPF construction data.
    let parseWithSppfInfo
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : SppfParsingTable<'nt> =
        let cnf = Grammar.toCnf freshNonterminal g

        if terminals.IsEmpty then
            Matrix.init 0 0 Set.empty
        else
            cykSppfCore cnf terminals

    /// Run CYK and return the enriched parsing table with acceptance status.
    let parseWithSppfTable
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : SppfParsingTable<'nt> * bool =
        let cnf = Grammar.toCnf freshNonterminal g

        if terminals.IsEmpty then
            let epsAccepted = Grammar.isEpsilonAccepted cnf
            let emptyResult = Matrix.init 0 0 Set.empty
            (emptyResult, epsAccepted)
        else
            let table = cykSppfCore cnf terminals
            let accepted = isSppfAccepted cnf table
            (table, accepted)

    /// Run CYK with SPPF data and return the sequence of working table states with highlights.
    let parseWithSppfTrace
        (freshNonterminal: int -> 'nt)
        (g: Grammar<'t, 'nt>)
        (terminals: Terminal<'t> list)
        : CykSppfTraceStep<'nt> list =
        let cnf = Grammar.toCnf freshNonterminal g
        let steps = ResizeArray<CykSppfTraceStep<'nt>>()
        let mutable stepHighlights = []

        if terminals.IsEmpty then
            []
        else
            let n = terminals.Length
            let table = Matrix.init n n Set.empty

            for i in 0 .. n - 1 do
                let producing = findTerminalRulesWithProdIdx cnf.Rules terminals.[i]

                if not (List.isEmpty producing) then
                    let entries =
                        producing
                        |> List.map (fun (nt, prodIdx) ->
                            { Nt = nt
                              SplitPoint = i
                              ProdIdx = prodIdx })
                        |> Set.ofList

                    table.[i, i] <- entries

                    let h: Matrix.Highlight =
                        { Row = i
                          Col = i
                          Label = Matrix.CurrentCell }

                    stepHighlights <- h :: stepHighlights

            steps.Add(
                { Table = Matrix.create n n (fun i j -> table.[i, j])
                  Highlights = List.rev stepHighlights }
            )

            stepHighlights <- []

            for len in 2..n do
                for i in 0 .. n - len do
                    let j = i + len - 1
                    let accumulated = computeCellSppf cnf.Rules table i j

                    if not (Set.isEmpty accumulated) then
                        table.[i, j] <- accumulated

                        let h: Matrix.Highlight =
                            { Row = i
                              Col = j
                              Label = Matrix.CurrentCell }

                        stepHighlights <- h :: stepHighlights

                steps.Add(
                    { Table = Matrix.create n n (fun i j -> table.[i, j])
                      Highlights = List.rev stepHighlights }
                )

                stepHighlights <- []

            steps |> List.ofSeq
