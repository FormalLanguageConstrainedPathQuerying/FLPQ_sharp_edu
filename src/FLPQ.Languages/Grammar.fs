namespace FLPQ.Languages

open System
open System.IO
open FSharpPlus.Data

/// A terminal symbol, wrapping a user-defined type 't.
type Terminal<'t> = Terminal of 't

/// A nonterminal symbol, wrapping a user-defined type 'nt.
type Nonterminal<'nt> = Nonterminal of 'nt

/// A named pair of nonterminals, used in Valiant's algorithm for binary rule decomposition.
[<Struct>]
type BinaryPair<'nt> =
    { left: Nonterminal<'nt>
      right: Nonterminal<'nt> }

/// A grammar symbol: a terminal, a nonterminal, or epsilon.
[<RequireQualifiedAccess>]
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

    let toListWithEpsilon (rhs: Rhs<'t, 'nt>) : Symbol<'t, 'nt> list =
        match rhs with
        | Symbols nel -> NonEmptyList.toList nel
        | EpsilonRhs -> [ Symbol.Epsilon ]

    let toNonEpsilonList (rhs: Rhs<'t, 'nt>) : Symbol<'t, 'nt> list =
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
            Symbol.N(Nonterminal token)
        else
            Symbol.T(Terminal token)

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

    let nonterminalsOf (g: Grammar<'t, 'nt>) : Set<Nonterminal<'nt>> =
        g.rules
        |> List.collect (fun r ->
            let rhsNts =
                Rhs.toNonEpsilonList r.rhs
                |> List.choose (function
                    | Symbol.N nt -> Some nt
                    | _ -> None)

            r.lhs :: rhsNts)
        |> Set.ofList

    let terminalsOf (g: Grammar<'t, 'nt>) : Set<Terminal<'t>> =
        g.rules
        |> List.collect (fun r ->
            Rhs.toNonEpsilonList r.rhs
            |> List.choose (function
                | Symbol.T t -> Some t
                | _ -> None))
        |> Set.ofList

    /// Check whether the start symbol of a CNF grammar can produce epsilon.
    let isEpsilonAccepted (cnf: Grammar<'t, 'nt>) : bool =
        cnf.rules |> List.exists (fun r -> r.lhs = cnf.start && Rhs.isEpsilon r.rhs)

    let private computeNullable (rules: Rule<'t, 'nt> list) : Set<Nonterminal<'nt>> =
        let rec loop (current: Set<Nonterminal<'nt>>) =
            let newNullable =
                rules
                |> List.filter (fun r ->
                    Rhs.toListWithEpsilon r.rhs
                    |> List.forall (function
                        | Symbol.N nt -> Set.contains nt current
                        | Symbol.Epsilon -> true
                        | Symbol.T _ -> false))
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

    let private computeUnitPairs (rules: Rule<'t, 'nt> list) (nts: Set<Nonterminal<'nt>>) : Set<BinaryPair<'nt>> =
        let initial = nts |> Set.map (fun a -> { left = a; right = a })

        let rec loop (current: Set<BinaryPair<'nt>>) =
            let newPairs =
                rules
                |> List.filter (fun r ->
                    match r.rhs with
                    | Symbols nel when NonEmptyList.length nel = 1 ->
                        match NonEmptyList.head nel with
                        | Symbol.N _ -> true
                        | _ -> false
                    | _ -> false)
                |> List.collect (fun r ->
                    let a = r.lhs

                    match r.rhs with
                    | Symbols nel ->
                        match NonEmptyList.head nel with
                        | Symbol.N b ->
                            current
                            |> Set.filter (fun bp -> bp.right = a)
                            |> Set.toList
                            |> List.map (fun bp -> { left = bp.left; right = b })
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

    let private eliminateEpsilon (fresh: unit -> 'nt) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let nullable = computeNullable g.rules

        let hasEps =
            g.rules |> List.exists (fun r -> r.lhs = g.start && Rhs.isEpsilon r.rhs)

        let newStart = if hasEps then Nonterminal(fresh ()) else g.start

        let startRule =
            { lhs = newStart
              rhs = NonEmptyList.create (Symbol.N g.start) [] |> Symbols }

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
                    let symbols = Rhs.toNonEpsilonList r.rhs

                    let nullableIndices =
                        symbols
                        |> List.indexed
                        |> List.choose (fun (idx, sym) ->
                            match sym with
                            | Symbol.N nt when Set.contains nt nullable -> Some idx
                            | _ -> None)

                    let combos = combinations nullableIndices

                    combos
                    |> List.choose (fun keepIndices ->
                        let newRhsList =
                            symbols
                            |> List.indexed
                            |> List.filter (fun (idx, sym) ->
                                match sym with
                                | Symbol.T _ -> true
                                | Symbol.N nt ->
                                    let isNullable = Set.contains nt nullable
                                    (not isNullable) || List.contains idx keepIndices
                                | Symbol.Epsilon -> true)
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

    let private eliminateUnit (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let nts = nonterminalsOf g
        let pairs = computeUnitPairs g.rules nts

        let newRules =
            pairs
            |> Set.toList
            |> List.collect (fun bp ->
                g.rules
                |> List.filter (fun r -> r.lhs = bp.right)
                |> List.choose (fun r ->
                    match r.rhs with
                    | Symbols nel ->
                        if NonEmptyList.length nel = 1 then
                            match NonEmptyList.head nel with
                            | Symbol.N _ -> None
                            | _ -> Some { lhs = bp.left; rhs = r.rhs }
                        else
                            Some { lhs = bp.left; rhs = r.rhs }
                    | EpsilonRhs -> Some { lhs = bp.left; rhs = r.rhs }))

        let allRules = List.distinct (newRules @ g.rules)

        let withoutUnits =
            allRules
            |> List.filter (fun r ->
                match r.rhs with
                | Symbols nel ->
                    NonEmptyList.length nel <> 1
                    || (match NonEmptyList.head nel with
                        | Symbol.N _ -> false
                        | _ -> true)
                | EpsilonRhs -> true)

        { g with
            rules = List.distinct withoutUnits }

    let private replaceTerminals (fresh: unit -> 'nt) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let terminals = terminalsOf g

        let terminalToNt =
            terminals
            |> Set.toList
            |> List.map (fun t -> (t, Nonterminal(fresh ())))
            |> Map.ofList

        let termRules =
            terminalToNt
            |> Map.toList
            |> List.map (fun (term, nt) ->
                { lhs = nt
                  rhs = NonEmptyList.create (Symbol.T term) [] |> Symbols })

        let newRules =
            g.rules
            |> List.map (fun r ->
                let symbols = Rhs.toNonEpsilonList r.rhs

                let newSymbols =
                    symbols
                    |> List.map (fun sym ->
                        match sym with
                        | Symbol.T t when symbols.Length > 1 ->
                            match Map.tryFind t terminalToNt with
                            | Some nt -> Symbol.N nt
                            | None -> Symbol.T t
                        | _ -> sym)

                { lhs = r.lhs
                  rhs =
                    if newSymbols.IsEmpty then
                        EpsilonRhs
                    else
                        NonEmptyList.ofList newSymbols |> Symbols })

        { g with
            rules = List.distinct (termRules @ newRules) }

    let private binarize (fresh: unit -> 'nt) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
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
                        let rec breakDown (prevNt: Nonterminal<'nt>) (remaining: Symbol<'t, 'nt> list) =
                            match remaining with
                            | [ s1; s2 ] ->
                                [ { lhs = prevNt
                                    rhs = NonEmptyList.create s1 [ s2 ] |> Symbols } ]
                            | s1 :: more ->
                                let newNt = Nonterminal(fresh ())

                                let restRules = breakDown newNt more

                                { lhs = prevNt
                                  rhs = NonEmptyList.create s1 [ Symbol.N newNt ] |> Symbols }
                                :: restRules
                            | _ -> []

                        let newNt = Nonterminal(fresh ())
                        let rhsRules = breakDown newNt rest

                        { lhs = r.lhs
                          rhs = NonEmptyList.create first [ Symbol.N newNt ] |> Symbols }
                        :: rhsRules
                    | _ -> [ r ])

        { g with
            rules = List.distinct newRules }

    /// A convenience function: generate a fresh nonterminal name from an integer index
    /// for use with string-based grammars (Nonterminal<string>).
    let freshStringNonterminal (i: int) : string = $"N_{i}"

    /// Transform a grammar into Chomsky Normal Form.
    /// Resulting grammar has only rules of the form:
    /// A -> BC (two nonterminals), A -> a (one terminal), or S -> eps (only start).
    /// Binarization is applied first to reduce the number of combinations
    /// generated during epsilon elimination for long rules with nullable nonterminals.
    /// `freshNonterminal` produces a fresh nonterminal from an integer index.
    /// The caller is responsible for ensuring uniqueness (different indices produce different nonterminals).
    let toCnf (freshNonterminal: int -> 'nt) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let counter = ref 0

        let fresh () =
            counter.Value <- counter.Value + 1
            freshNonterminal counter.Value

        let s1 = binarize fresh g
        let s2 = eliminateEpsilon fresh s1
        let s3 = eliminateUnit s2
        let s4 = replaceTerminals fresh s3
        s4
