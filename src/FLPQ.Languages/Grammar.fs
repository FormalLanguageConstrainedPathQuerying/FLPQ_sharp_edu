namespace FLPQ.Languages

open System
open System.IO
open FSharpPlus.Data

/// A terminal symbol, wrapping a user-defined type 't.
type Terminal<'t> = Terminal of 't

/// A nonterminal symbol, wrapping a user-defined type 'nt.
type Nonterminal<'nt> = Nonterminal of 'nt

/// A grammar symbol: a terminal, a nonterminal, or epsilon.
type Symbol<'t, 'nt> =
    | T of Terminal<'t>
    | N of Nonterminal<'nt>
    | Epsilon

/// Right-hand side of a production rule.
/// Either a non-empty list of symbols or epsilon.
type Rhs<'t, 'nt> =
    | Symbols of NonEmptyList<Symbol<'t, 'nt>>
    | EpsilonRhs

module Rhs =

    let toList (rhs: Rhs<'t, 'nt>) : Symbol<'t, 'nt> list =
        match rhs with
        | Symbols nel -> NonEmptyList.toList nel
        | EpsilonRhs -> [ Epsilon ]

    let toSymbols (rhs: Rhs<'t, 'nt>) : Symbol<'t, 'nt> list =
        match rhs with
        | Symbols nel -> NonEmptyList.toList nel
        | EpsilonRhs -> []

    let isEpsilon (rhs: Rhs<'t, 'nt>) : bool =
        match rhs with
        | EpsilonRhs -> true
        | _ -> false

    let length (rhs: Rhs<'t, 'nt>) : int =
        match rhs with
        | Symbols nel -> NonEmptyList.length nel
        | EpsilonRhs -> 0

/// A production rule: left-hand side nonterminal produces a sequence of symbols.
type Rule<'t, 'nt> =
    { lhs: Nonterminal<'nt>
      rhs: Rhs<'t, 'nt> }

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
                EpsilonRhs
            else
                rhsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList
                |> List.map classifyToken
                |> NonEmptyList.ofList
                |> Symbols

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
                Rhs.toSymbols r.rhs
                |> List.choose (function
                    | N nt -> Some nt
                    | _ -> None)

            r.lhs :: rhsNts)
        |> Set.ofList

    let private terminalsOf (g: Grammar<string, string>) : Set<Terminal<string>> =
        g.rules
        |> List.collect (fun r ->
            Rhs.toSymbols r.rhs
            |> List.choose (function
                | T t -> Some t
                | _ -> None))
        |> Set.ofList

    let private freshGen (existing: Set<Nonterminal<string>>) : unit -> Nonterminal<string> =
        let mutable used = existing

        fun () ->
            let rec loop n =
                let candidate = Nonterminal($"N_CNF_{n}")

                if Set.contains candidate used then
                    loop (n + 1)
                else
                    used <- Set.add candidate used
                    candidate

            loop 1

    let private computeNullable (rules: Rule<string, string> list) : Set<Nonterminal<string>> =
        let rec loop (current: Set<Nonterminal<string>>) =
            let newNullable =
                rules
                |> List.filter (fun r ->
                    Rhs.toList r.rhs
                    |> List.forall (function
                        | N nt -> Set.contains nt current
                        | Epsilon -> true
                        | T _ -> false))
                |> List.map (fun r -> r.lhs)
                |> Set.ofList

            let updated = Set.union current newNullable

            if Set.count updated > Set.count current then
                loop updated
            else
                updated

        rules
        |> List.filter (fun r -> Rhs.isEpsilon r.rhs)
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
                    | Symbols nel when NonEmptyList.length nel = 1 ->
                        match NonEmptyList.head nel with
                        | N _ -> true
                        | _ -> false
                    | _ -> false)
                |> List.collect (fun r ->
                    let a = r.lhs

                    match r.rhs with
                    | Symbols nel ->
                        match NonEmptyList.head nel with
                        | N b ->
                            current
                            |> Set.filter (fun (x, y) -> y = a)
                            |> Set.toList
                            |> List.map (fun (x, _) -> (x, b))
                        | _ -> []
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

        let hasEps =
            g.rules |> List.exists (fun r -> r.lhs = g.start && Rhs.isEpsilon r.rhs)

        let newStart =
            if hasEps then
                let fresh = freshGen (nonterminalsOf g) ()
                fresh
            else
                g.start

        let startRule =
            { lhs = newStart
              rhs = NonEmptyList.create (N g.start) [] |> Symbols }

        let startEpsRule =
            if hasEps then
                [ { lhs = newStart; rhs = EpsilonRhs } ]
            else
                []

        let newRules =
            g.rules
            |> List.collect (fun r ->
                if Rhs.isEpsilon r.rhs then
                    []
                else
                    let symbols = Rhs.toSymbols r.rhs

                    let nullableIndices =
                        symbols
                        |> List.indexed
                        |> List.choose (fun (idx, sym) ->
                            match sym with
                            | N nt when Set.contains nt nullable -> Some idx
                            | _ -> None)

                    let combos = combinations nullableIndices

                    combos
                    |> List.choose (fun keepIndices ->
                        let newRhsList =
                            symbols
                            |> List.indexed
                            |> List.filter (fun (idx, sym) ->
                                match sym with
                                | T _ -> true
                                | N nt ->
                                    let isNullable = Set.contains nt nullable
                                    (not isNullable) || List.contains idx keepIndices
                                | Epsilon -> true)
                            |> List.map snd

                        if newRhsList.IsEmpty then
                            None
                        else
                            Some
                                { lhs = r.lhs
                                  rhs = NonEmptyList.ofList newRhsList |> Symbols }))

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
                    | Symbols nel ->
                        if NonEmptyList.length nel = 1 then
                            match NonEmptyList.head nel with
                            | N _ -> None
                            | _ -> Some { lhs = a; rhs = r.rhs }
                        else
                            Some { lhs = a; rhs = r.rhs }
                    | EpsilonRhs -> Some { lhs = a; rhs = r.rhs }))

        let allRules = List.distinct (newRules @ g.rules)

        let withoutUnits =
            allRules
            |> List.filter (fun r ->
                match r.rhs with
                | Symbols nel ->
                    NonEmptyList.length nel <> 1
                    || (match NonEmptyList.head nel with
                        | N _ -> false
                        | _ -> true)
                | EpsilonRhs -> true)

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
            |> List.map (fun (term, nt) ->
                { lhs = nt
                  rhs = NonEmptyList.create (T term) [] |> Symbols })

        let newRules =
            g.rules
            |> List.map (fun r ->
                let symbols = Rhs.toSymbols r.rhs

                let newSymbols =
                    symbols
                    |> List.map (fun sym ->
                        match sym with
                        | T t when symbols.Length > 1 ->
                            match Map.tryFind t terminalToNt with
                            | Some nt -> N nt
                            | None -> T t
                        | _ -> sym)

                { lhs = r.lhs
                  rhs =
                    if newSymbols.IsEmpty then
                        EpsilonRhs
                    else
                        NonEmptyList.ofList newSymbols |> Symbols })

        { g with
            rules = List.distinct (termRules @ newRules) }

    let private binarize (g: Grammar<string, string>) : Grammar<string, string> =
        let fresh = freshGen (nonterminalsOf g)

        let newRules =
            g.rules
            |> List.collect (fun r ->
                match r.rhs with
                | EpsilonRhs -> [ r ]
                | Symbols nel ->
                    let syms = NonEmptyList.toList nel

                    match syms with
                    | [ _ ] -> [ r ]
                    | [ _; _ ] -> [ r ]
                    | first :: rest ->
                        let rec breakDown (prevNt: Nonterminal<string>) (remaining: Symbol<string, string> list) =
                            match remaining with
                            | [ s1; s2 ] ->
                                [ { lhs = prevNt
                                    rhs = NonEmptyList.create s1 [ s2 ] |> Symbols } ]
                            | s1 :: more ->
                                let newNt = fresh ()

                                let restRules = breakDown newNt more

                                { lhs = prevNt
                                  rhs = NonEmptyList.create s1 [ N newNt ] |> Symbols }
                                :: restRules
                            | _ -> []

                        let newNt = fresh ()
                        let rhsRules = breakDown newNt rest

                        { lhs = r.lhs
                          rhs = NonEmptyList.create first [ N newNt ] |> Symbols }
                        :: rhsRules
                    | _ -> [ r ])

        { g with
            rules = List.distinct newRules }

    /// Transform a grammar into Chomsky Normal Form.
    /// Resulting grammar has only rules of the form:
    /// A -> BC (two nonterminals), A -> a (one terminal), or S -> eps (only start).
    /// Binarization is applied first to reduce the number of combinations
    /// generated during epsilon elimination for long rules with nullable nonterminals.
    let toCnf (g: Grammar<string, string>) : Grammar<string, string> =
        let s1 = binarize g
        let s2 = eliminateEpsilon s1
        let s3 = eliminateUnit s2
        let s4 = replaceTerminals s3
        s4
