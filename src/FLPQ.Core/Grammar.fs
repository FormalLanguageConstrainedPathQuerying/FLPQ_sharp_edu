namespace FLPQ.Core

open System
open System.IO

/// A terminal symbol, wrapping a user-defined type 't.
type Terminal<'t> = Terminal of 't

/// A nonterminal symbol, wrapping a user-defined type 'nt.
type Nonterminal<'nt> = Nonterminal of 'nt

/// A grammar symbol: either a terminal or a nonterminal.
type Symbol<'t, 'nt> =
    | T of Terminal<'t>
    | N of Nonterminal<'nt>

/// A production rule: left-hand side nonterminal produces a sequence of symbols.
type Rule<'t, 'nt> =
    { lhs: Nonterminal<'nt>
      rhs: Symbol<'t, 'nt> list }

/// A context-free grammar consisting of production rules and a designated start nonterminal.
type Grammar<'t, 'nt> =
    { rules: Rule<'t, 'nt> list
      start: Nonterminal<'nt> }

module Grammar =

    let private classifyToken (token: string) : Symbol<string, string> =
        if System.Char.IsUpper(token[0]) then
            N(Nonterminal token)
        else
            T(Terminal token)

    let private parseLine (line: string) : Rule<string, string> =
        let parts = line.Split("->", 2, StringSplitOptions.None)

        if parts.Length <> 2 then
            invalidArg (nameof line) $"Invalid rule format: {line}"

        let lhs = Nonterminal(parts[0].Trim())
        let rhsStr = parts[1].Trim()

        let rhs =
            if rhsStr = "eps" then
                []
            else
                rhsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList
                |> List.map classifyToken

        { lhs = lhs; rhs = rhs }

    /// Parse a grammar from BNF text.
    /// One rule per line. Empty lines are ignored.
    /// Format: `<nonterm> -> <symbols>` or `<nonterm> -> eps`.
    /// The start nonterminal is the left-hand side of the first rule.
    let parseGrammar (text: string) : Grammar<string, string> =
        let rules =
            text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun s -> s.Trim())
            |> Array.filter (fun s -> s.Length > 0)
            |> Array.map parseLine
            |> Array.toList

        if rules.IsEmpty then
            invalidArg (nameof text) "Grammar must contain at least one rule"

        { rules = rules
          start = rules.Head.lhs }

    /// Parse a grammar from a .bnf file.
    let parseGrammarFromFile (path: string) : Grammar<string, string> = File.ReadAllText(path) |> parseGrammar

    let private nonterminalsOf (g: Grammar<string, string>) : Set<Nonterminal<string>> =
        g.rules
        |> List.collect (fun r ->
            let rhsNts =
                r.rhs
                |> List.choose (function
                    | N nt -> Some nt
                    | _ -> None)

            r.lhs :: rhsNts)
        |> Set.ofList

    let private terminalsOf (g: Grammar<string, string>) : Set<Terminal<string>> =
        g.rules
        |> List.collect (fun r ->
            r.rhs
            |> List.choose (function
                | T t -> Some t
                | _ -> None))
        |> Set.ofList

    let private freshGen (existing: Set<Nonterminal<string>>) : unit -> Nonterminal<string> =
        let mutable counter = 0

        fun () ->
            counter <- counter + 1

            let rec loop n =
                let candidate = Nonterminal($"N_CNF_{n}")

                if Set.contains candidate existing then
                    loop (n + 1)
                else
                    candidate

            loop counter

    let private computeNullable (rules: Rule<string, string> list) : Set<Nonterminal<string>> =
        let rec loop (current: Set<Nonterminal<string>>) =
            let newNullable =
                rules
                |> List.filter (fun r ->
                    r.rhs
                    |> List.forall (function
                        | N nt -> Set.contains nt current
                        | T _ -> false))
                |> List.map (fun r -> r.lhs)
                |> Set.ofList

            let updated = Set.union current newNullable

            if Set.count updated > Set.count current then
                loop updated
            else
                updated

        rules
        |> List.filter (fun r -> r.rhs.IsEmpty)
        |> List.map (fun r -> r.lhs)
        |> Set.ofList
        |> loop

    let private computeUnitPairs
        (rules: Rule<string, string> list)
        (nts: Set<Nonterminal<string>>)
        : Set<Nonterminal<string> * Nonterminal<string>> =
        let initial = nts |> Set.map (fun a -> (a, a))

        let rec loop (current: Set<Nonterminal<string> * Nonterminal<string>>) =
            let newPairs =
                rules
                |> List.filter (fun r ->
                    match r.rhs with
                    | [ N b ] -> true
                    | _ -> false)
                |> List.collect (fun r ->
                    let a = r.lhs

                    match r.rhs with
                    | [ N b ] ->
                        current
                        |> Set.filter (fun (x, y) -> y = a)
                        |> Set.toList
                        |> List.map (fun (x, _) -> (x, b))
                    | _ -> [])
                |> Set.ofList

            let updated = Set.union current newPairs

            if Set.count updated > Set.count current then
                loop updated
            else
                updated

        loop initial

    let private combinations (indices: int list) : int list list =
        let rec loop (remaining: int list) : int list list =
            match remaining with
            | [] -> [ [] ]
            | h :: t ->
                let rest = loop t
                (rest |> List.map (fun xs -> h :: xs)) @ rest

        loop indices

    let private eliminateEpsilon (g: Grammar<string, string>) : Grammar<string, string> =
        let nullable = computeNullable g.rules
        let hasEps = g.rules |> List.exists (fun r -> r.lhs = g.start && r.rhs.IsEmpty)

        let newStart =
            if hasEps then
                let fresh = freshGen (nonterminalsOf g) ()
                fresh
            else
                g.start

        let startRule = { lhs = newStart; rhs = [ N g.start ] }

        let startEpsRule = if hasEps then [ { lhs = newStart; rhs = [] } ] else []

        let newRules =
            g.rules
            |> List.collect (fun r ->
                if r.rhs.IsEmpty then
                    []
                else
                    let nullableIndices =
                        r.rhs
                        |> List.indexed
                        |> List.choose (fun (idx, sym) ->
                            match sym with
                            | N nt when Set.contains nt nullable -> Some idx
                            | _ -> None)

                    let combos = combinations nullableIndices

                    combos
                    |> List.choose (fun keepIndices ->
                        let newRhs =
                            r.rhs
                            |> List.indexed
                            |> List.filter (fun (idx, sym) ->
                                match sym with
                                | T _ -> true
                                | N nt ->
                                    let isNullable = Set.contains nt nullable
                                    (not isNullable) || List.contains idx keepIndices)
                            |> List.map snd

                        if newRhs.IsEmpty then
                            if r.lhs = g.start then None else None
                        else
                            Some { lhs = r.lhs; rhs = newRhs }))

        let allRules = startRule :: (startEpsRule @ newRules)

        { rules = List.distinct allRules
          start = newStart }

    let private eliminateUnit (g: Grammar<string, string>) : Grammar<string, string> =
        let nts = nonterminalsOf g
        let pairs = computeUnitPairs g.rules nts

        let newRules =
            pairs
            |> Set.toList
            |> List.collect (fun (a, b) ->
                g.rules
                |> List.filter (fun r -> r.lhs = b)
                |> List.choose (fun r ->
                    match r.rhs with
                    | [ N _ ] -> None
                    | rhs -> Some { lhs = a; rhs = rhs }))

        let allRules = List.distinct (newRules @ g.rules)

        let withoutUnits =
            allRules
            |> List.filter (fun r ->
                match r.rhs with
                | [ N _ ] -> false
                | _ -> true)

        { g with
            rules = List.distinct withoutUnits }

    let private replaceTerminals (g: Grammar<string, string>) : Grammar<string, string> =
        let terminals = terminalsOf g
        let fresh = freshGen (nonterminalsOf g)

        let terminalToNt =
            terminals |> Set.toList |> List.map (fun t -> (t, fresh ())) |> Map.ofList

        let termRules =
            terminalToNt
            |> Map.toList
            |> List.map (fun (term, nt) -> { lhs = nt; rhs = [ T term ] })

        let newRules =
            g.rules
            |> List.map (fun r ->
                let newRhs =
                    r.rhs
                    |> List.map (fun sym ->
                        match sym with
                        | T t when r.rhs.Length > 1 ->
                            match Map.tryFind t terminalToNt with
                            | Some nt -> N nt
                            | None -> T t
                        | _ -> sym)

                { lhs = r.lhs; rhs = newRhs })

        { g with
            rules = List.distinct (termRules @ newRules) }

    let private binarize (g: Grammar<string, string>) : Grammar<string, string> =
        let fresh = freshGen (nonterminalsOf g)

        let newRules =
            g.rules
            |> List.collect (fun r ->
                match r.rhs with
                | [ _ ] -> [ r ]
                | [ _; _ ] -> [ r ]
                | first :: rest ->
                    let rec breakDown (prevNt: Nonterminal<string>) (remaining: Symbol<string, string> list) =
                        match remaining with
                        | [ s1; s2 ] -> [ { lhs = prevNt; rhs = [ s1; s2 ] } ]
                        | s1 :: more ->
                            let newNt = fresh ()

                            let restRules = breakDown newNt more

                            { lhs = prevNt; rhs = [ s1; N newNt ] } :: restRules
                        | _ -> []

                    let newNt = fresh ()
                    let rhsRules = breakDown newNt rest

                    { lhs = r.lhs
                      rhs = [ first; N newNt ] }
                    :: rhsRules
                | _ -> [ r ])

        { g with
            rules = List.distinct newRules }

    /// Transform a grammar into Chomsky Normal Form.
    /// Resulting grammar has only rules of the form:
    /// A -> BC (two nonterminals), A -> a (one terminal), or S -> eps (only start).
    let toCnf (g: Grammar<string, string>) : Grammar<string, string> =
        let s1 = eliminateEpsilon g
        let s2 = eliminateUnit s1
        let s3 = replaceTerminals s2
        let s4 = binarize s3
        s4
