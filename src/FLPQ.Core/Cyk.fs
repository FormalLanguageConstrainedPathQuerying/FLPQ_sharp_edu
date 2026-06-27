namespace FLPQ.Core

open System.Collections.Generic

module Cyk =

    type CykCell = Option<HashSet<Symbol<string, string>>>

    let private isNonterminal (sym: Symbol<string, string>) =
        match sym with
        | N _ -> true
        | _ -> false

    let private nonterminalValue (sym: Symbol<string, string>) : Nonterminal<string> =
        match sym with
        | N nt -> nt
        | _ -> failwith "Expected nonterminal"

    let private tokenize (input: string) : Symbol<string, string> list =
        input.ToCharArray()
        |> Array.map (fun c -> T(Terminal(c.ToString())))
        |> Array.toList

    let private findProducingRules
        (rules: Rule<string, string> list)
        (target: Symbol<string, string>)
        : Nonterminal<string> list =
        rules |> List.filter (fun r -> r.rhs = [ target ]) |> List.map (fun r -> r.lhs)

    let private findBinaryProductions
        (rules: Rule<string, string> list)
        (left: Symbol<string, string>)
        (right: Symbol<string, string>)
        : Nonterminal<string> list =
        rules
        |> List.filter (fun r -> r.rhs = [ left; right ])
        |> List.map (fun r -> r.lhs)

    let private cykTable (cnf: Grammar<string, string>) (tokens: Symbol<string, string> list) : Matrix<CykCell> =
        let n = tokens.Length
        let emptyCell: CykCell = None

        let table = Matrix.init n n emptyCell

        for i in 0 .. n - 1 do
            let token = tokens.[i]
            let producing = findProducingRules cnf.rules token

            match producing with
            | [] -> ()
            | nts ->
                let cell = nts |> List.map (fun nt -> N nt) |> HashSet |> Some

                table.data.[i, i] <- cell

        for len in 2..n do
            for i in 0 .. n - len do
                let j = i + len - 1
                let mutable accumulated = HashSet<Symbol<string, string>>()

                for k in i .. j - 1 do
                    let leftCell = table.data.[i, k]
                    let rightCell = table.data.[k + 1, j]

                    match leftCell, rightCell with
                    | Some leftSet, Some rightSet ->
                        for leftSym in leftSet do
                            for rightSym in rightSet do
                                let producers = findBinaryProductions cnf.rules leftSym rightSym

                                for nt in producers do
                                    accumulated.Add(N nt) |> ignore
                    | _ -> ()

                if accumulated.Count > 0 then
                    table.data.[i, j] <- Some accumulated

        table

    let private tableTrace (cnf: Grammar<string, string>) (tokens: Symbol<string, string> list) : Matrix<CykCell> list =
        let n = tokens.Length
        let emptyCell: CykCell = None

        let table = Matrix.init n n emptyCell
        let steps = ResizeArray<Matrix<CykCell>>()

        for i in 0 .. n - 1 do
            let token = tokens.[i]
            let producing = findProducingRules cnf.rules token

            match producing with
            | [] -> ()
            | nts ->
                let cell = nts |> List.map (fun nt -> N nt) |> HashSet |> Some

                table.data.[i, i] <- cell

        steps.Add(
            { rows = table.rows
              cols = table.cols
              data = Array2D.copy table.data }
        )

        for len in 2..n do
            for i in 0 .. n - len do
                let j = i + len - 1
                let mutable accumulated = HashSet<Symbol<string, string>>()

                for k in i .. j - 1 do
                    let leftCell = table.data.[i, k]
                    let rightCell = table.data.[k + 1, j]

                    match leftCell, rightCell with
                    | Some leftSet, Some rightSet ->
                        for leftSym in leftSet do
                            for rightSym in rightSet do
                                let producers = findBinaryProductions cnf.rules leftSym rightSym

                                for nt in producers do
                                    accumulated.Add(N nt) |> ignore
                    | _ -> ()

                if accumulated.Count > 0 then
                    table.data.[i, j] <- Some accumulated

            steps.Add(
                { rows = table.rows
                  cols = table.cols
                  data = Array2D.copy table.data }
            )

        steps |> List.ofSeq

    let private isAccepted (cnf: Grammar<string, string>) (table: Matrix<CykCell>) : bool =
        let n = table.rows

        if n = 0 then
            false
        else
            match table.data.[0, n - 1] with
            | Some cell -> cell.Contains(N cnf.start)
            | None -> false

    /// Check whether a string is accepted by a grammar using the CYK algorithm.
    /// The grammar is first converted to CNF. The input string is tokenized character by character.
    let parse (g: Grammar<string, string>) (input: string) : bool =
        if input = "" then
            let cnf = Grammar.toCnf g
            cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && r.rhs = [])
        else
            let cnf = Grammar.toCnf g
            let tokens = tokenize input
            let table = cykTable cnf tokens
            isAccepted cnf table

    /// Run CYK and return the sequence of working table states (one per diagonal).
    /// The grammar is first converted to CNF.
    let parseWithTrace (g: Grammar<string, string>) (input: string) : Matrix<CykCell> list =
        let cnf = Grammar.toCnf g
        let tokens = tokenize input
        tableTrace cnf tokens

    /// Convert a CYK working table to TeX.
    /// Empty cells (None) are printed as \cdot.
    let tableToTeX (symbolPrinter: Symbol<string, string> -> string) (table: Matrix<CykCell>) : string =
        let cellPrinter (cell: CykCell) : string =
            match cell with
            | None -> @"\cdot"
            | Some symbols ->
                symbols
                |> Seq.map symbolPrinter
                |> String.concat ", "
                |> fun s -> "\\{" + s + "\\}"

        Matrix.toTeX true true cellPrinter table
