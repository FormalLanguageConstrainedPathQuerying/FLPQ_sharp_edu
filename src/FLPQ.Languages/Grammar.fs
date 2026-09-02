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
    { Left: Nonterminal<'nt>
      Right: Nonterminal<'nt> }

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
    { Lhs: Nonterminal<'nt>
      Rhs: Rhs<'t, 'nt> }

/// A context-free grammar consisting of production rules and a designated start nonterminal.
type Grammar<'t, 'nt> =
    { Rules: Rule<'t, 'nt> list
      Start: Nonterminal<'nt> }

module Grammar =

    /// End-of-input terminal symbol, used to signal the end of token stream (string-based grammars).
    let eoiTerminal: Terminal<string> = Terminal "$"

    /// End-of-input symbol as a grammar Symbol (string-based grammars).
    let eoiSymbol: Symbol<string, string> = Symbol.T eoiTerminal

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

        { Lhs = lhs; Rhs = rhs }

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

        { Rules = rules
          Start = rules.Head.Lhs }

    /// Parse a grammar from a .bnf file.
    let parseGrammarFromFile (path: string) : Grammar<string, string> = File.ReadAllText(path) |> parseGrammar

    let nonterminalsOf (g: Grammar<'t, 'nt>) : Set<Nonterminal<'nt>> =
        g.Rules
        |> List.collect (fun r ->
            let rhsNts =
                Rhs.toNonEpsilonList r.Rhs
                |> List.choose (function
                    | Symbol.N nt -> Some nt
                    | _ -> None)

            r.Lhs :: rhsNts)
        |> Set.ofList

    let terminalsOf (g: Grammar<'t, 'nt>) : Set<Terminal<'t>> =
        g.Rules
        |> List.collect (fun r ->
            Rhs.toNonEpsilonList r.Rhs
            |> List.choose (function
                | Symbol.T t -> Some t
                | _ -> None))
        |> Set.ofList

    /// Check whether the start symbol of a CNF grammar can produce epsilon.
    let isEpsilonAccepted (cnf: Grammar<'t, 'nt>) : bool =
        cnf.Rules |> List.exists (fun r -> r.Lhs = cnf.Start && Rhs.isEpsilon r.Rhs)

    /// Return the grammar's productions with canonical 1-based production numbers.
    /// Rules of the start nonterminal come first, followed by the remaining rules in
    /// their original order. This is the single source of truth for production numbers
    /// used by CNF rendering (GrammarTeX), CYK/Valiant table cells, and Basic SPPF construction.
    let numberedRules (g: Grammar<'t, 'nt>) : (int * Rule<'t, 'nt>) list =
        let startRules, otherRules = g.Rules |> List.partition (fun r -> r.Lhs = g.Start)

        startRules @ otherRules |> List.mapi (fun i rule -> (i + 1, rule))

    /// Map from a 1-based production number (as produced by `numberedRules`)
    /// to the corresponding production rule. Built once and reused for O(1) lookup.
    let productionNumberMap (g: Grammar<'t, 'nt>) : Map<int, Rule<'t, 'nt>> = numberedRules g |> Map.ofList

    let private computeNullable (rules: Rule<'t, 'nt> list) : Set<Nonterminal<'nt>> =
        let rec loop (current: Set<Nonterminal<'nt>>) =
            let newNullable =
                rules
                |> List.filter (fun r ->
                    Rhs.toListWithEpsilon r.Rhs
                    |> List.forall (function
                        | Symbol.N nt -> Set.contains nt current
                        | Symbol.Epsilon -> true
                        | Symbol.T _ -> false))
                |> List.map (fun r -> r.Lhs)
                |> Set.ofList

            let updated = Set.union current newNullable

            if Set.count updated > Set.count current then
                loop updated
            else
                updated

        rules
        |> List.filter (fun r -> Rhs.isEpsilon r.Rhs)
        |> List.map (fun r -> r.Lhs)
        |> Set.ofList
        |> loop

    let private computeUnitPairs (rules: Rule<'t, 'nt> list) (nts: Set<Nonterminal<'nt>>) : Set<BinaryPair<'nt>> =
        let initial = nts |> Set.map (fun a -> { Left = a; Right = a })

        let rec loop (current: Set<BinaryPair<'nt>>) =
            let newPairs =
                rules
                |> List.filter (fun r ->
                    match r.Rhs with
                    | Symbols nel when NonEmptyList.length nel = 1 ->
                        match NonEmptyList.head nel with
                        | Symbol.N _ -> true
                        | _ -> false
                    | _ -> false)
                |> List.collect (fun r ->
                    let a = r.Lhs

                    match r.Rhs with
                    | Symbols nel ->
                        match NonEmptyList.head nel with
                        | Symbol.N b ->
                            current
                            |> Set.filter (fun bp -> bp.Right = a)
                            |> Set.toList
                            |> List.map (fun bp -> { Left = bp.Left; Right = b })
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
        let nullable = computeNullable g.Rules

        let hasEps =
            g.Rules |> List.exists (fun r -> r.Lhs = g.Start && Rhs.isEpsilon r.Rhs)

        let newStart = if hasEps then Nonterminal(fresh ()) else g.Start

        let startRule =
            { Lhs = newStart
              Rhs = NonEmptyList.create (Symbol.N g.Start) [] |> Symbols }

        let startEpsRule =
            if hasEps then
                [ { Lhs = newStart; Rhs = EpsilonRhs } ]
            else
                []

        let newRules =
            g.Rules
            |> List.collect (fun r ->
                if Rhs.isEpsilon r.Rhs then
                    []
                else
                    let symbols = Rhs.toNonEpsilonList r.Rhs

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
                                { Lhs = r.Lhs
                                  Rhs = NonEmptyList.ofList newRhsList |> Symbols }))

        let allRules = startRule :: (startEpsRule @ newRules)

        { Rules = List.distinct allRules
          Start = newStart }

    let private eliminateUnit (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let nts = nonterminalsOf g
        let pairs = computeUnitPairs g.Rules nts

        let newRules =
            pairs
            |> Set.toList
            |> List.collect (fun bp ->
                g.Rules
                |> List.filter (fun r -> r.Lhs = bp.Right)
                |> List.choose (fun r ->
                    match r.Rhs with
                    | Symbols nel ->
                        if NonEmptyList.length nel = 1 then
                            match NonEmptyList.head nel with
                            | Symbol.N _ -> None
                            | _ -> Some { Lhs = bp.Left; Rhs = r.Rhs }
                        else
                            Some { Lhs = bp.Left; Rhs = r.Rhs }
                    | EpsilonRhs -> Some { Lhs = bp.Left; Rhs = r.Rhs }))

        let allRules = List.distinct (newRules @ g.Rules)

        let withoutUnits =
            allRules
            |> List.filter (fun r ->
                match r.Rhs with
                | Symbols nel ->
                    NonEmptyList.length nel <> 1
                    || (match NonEmptyList.head nel with
                        | Symbol.N _ -> false
                        | _ -> true)
                | EpsilonRhs -> true)

        { g with
            Rules = List.distinct withoutUnits }

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
                { Lhs = nt
                  Rhs = NonEmptyList.create (Symbol.T term) [] |> Symbols })

        let newRules =
            g.Rules
            |> List.map (fun r ->
                let symbols = Rhs.toNonEpsilonList r.Rhs

                let newSymbols =
                    symbols
                    |> List.map (fun sym ->
                        match sym with
                        | Symbol.T t when symbols.Length > 1 ->
                            match Map.tryFind t terminalToNt with
                            | Some nt -> Symbol.N nt
                            | None -> Symbol.T t
                        | _ -> sym)

                { Lhs = r.Lhs
                  Rhs =
                    if newSymbols.IsEmpty then
                        EpsilonRhs
                    else
                        NonEmptyList.ofList newSymbols |> Symbols })

        { g with
            Rules = List.distinct (termRules @ newRules) }

    let private binarize (fresh: unit -> 'nt) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let newRules =
            g.Rules
            |> List.collect (fun r ->
                match r.Rhs with
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
                                [ { Lhs = prevNt
                                    Rhs = NonEmptyList.create s1 [ s2 ] |> Symbols } ]
                            | s1 :: more ->
                                let newNt = Nonterminal(fresh ())

                                let restRules = breakDown newNt more

                                { Lhs = prevNt
                                  Rhs = NonEmptyList.create s1 [ Symbol.N newNt ] |> Symbols }
                                :: restRules
                            | _ -> []

                        let newNt = Nonterminal(fresh ())
                        let rhsRules = breakDown newNt rest

                        { Lhs = r.Lhs
                          Rhs = NonEmptyList.create first [ Symbol.N newNt ] |> Symbols }
                        :: rhsRules
                    | _ -> [ r ])

        { g with
            Rules = List.distinct newRules }

    /// Augment a grammar with a fresh start nonterminal S' -> S.
    /// The augmented grammar has S' -> S as the first rule, making the acceptance
    /// condition explicit: S' is reduced when the original start is fully parsed.
    let augmentGrammar (freshStart: Nonterminal<'nt>) (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        { Rules =
            { Lhs = freshStart
              Rhs = NonEmptyList.create (Symbol.N g.Start) [] |> Symbols }
            :: g.Rules
          Start = freshStart }

    /// A convenience function: generate a fresh nonterminal name from an integer index
    /// for use with string-based grammars (Nonterminal<string>).
    let freshStringNonterminal (i: int) : string = $"N_{i}"

    let private computeGenerating (rules: Rule<'t, 'nt> list) : Set<Nonterminal<'nt>> =
        let rec loop (current: Set<Nonterminal<'nt>>) =
            let newGenerating =
                rules
                |> List.filter (fun r ->
                    Rhs.toNonEpsilonList r.Rhs
                    |> List.forall (function
                        | Symbol.N nt -> Set.contains nt current
                        | Symbol.T _ -> true
                        | Symbol.Epsilon -> true))
                |> List.map (fun r -> r.Lhs)
                |> Set.ofList

            let updated = Set.union current newGenerating

            if Set.count updated > Set.count current then
                loop updated
            else
                updated

        rules
        |> List.filter (fun r ->
            Rhs.toNonEpsilonList r.Rhs
            |> List.forall (function
                | Symbol.N _ -> false
                | Symbol.T _ -> true
                | Symbol.Epsilon -> true))
        |> List.map (fun r -> r.Lhs)
        |> Set.ofList
        |> loop

    let private removeNonGenerating (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let generating = computeGenerating g.Rules

        let newRules =
            g.Rules
            |> List.filter (fun r ->
                Set.contains r.Lhs generating
                && (Rhs.toNonEpsilonList r.Rhs
                    |> List.forall (function
                        | Symbol.N nt -> Set.contains nt generating
                        | _ -> true)))

        { g with Rules = newRules }

    let private removeUnreachable (g: Grammar<'t, 'nt>) : Grammar<'t, 'nt> =
        let rec collectReachable (current: Set<Nonterminal<'nt>>) : Set<Nonterminal<'nt>> =
            let newReachable =
                g.Rules
                |> List.filter (fun r -> Set.contains r.Lhs current)
                |> List.collect (fun r ->
                    Rhs.toNonEpsilonList r.Rhs
                    |> List.choose (function
                        | Symbol.N nt -> Some nt
                        | _ -> None))
                |> Set.ofList

            let updated = Set.union current newReachable

            if Set.count updated > Set.count current then
                collectReachable updated
            else
                updated

        let reachable = collectReachable (Set.singleton g.Start)

        let newRules = g.Rules |> List.filter (fun r -> Set.contains r.Lhs reachable)

        { g with Rules = newRules }

    let allNonterminalsReachable (g: Grammar<'t, 'nt>) : bool =
        let rec collectReachable (current: Set<Nonterminal<'nt>>) : Set<Nonterminal<'nt>> =
            let newReachable =
                g.Rules
                |> List.filter (fun r -> Set.contains r.Lhs current)
                |> List.collect (fun r ->
                    Rhs.toNonEpsilonList r.Rhs
                    |> List.choose (function
                        | Symbol.N nt -> Some nt
                        | _ -> None))
                |> Set.ofList

            let updated = Set.union current newReachable

            if Set.count updated > Set.count current then
                collectReachable updated
            else
                updated

        let reachable = collectReachable (Set.singleton g.Start)
        let allNts = nonterminalsOf g
        Set.isSubset allNts reachable

    let allNonterminalsGenerating (g: Grammar<'t, 'nt>) : bool =
        let generating = computeGenerating g.Rules
        let allNts = nonterminalsOf g
        Set.isSubset allNts generating

    /// Transform a grammar into Chomsky Normal Form.
    /// Resulting grammar has only rules of the form:
    /// A -> BC (two nonterminals), A -> a (one terminal), or S -> eps (only start).
    /// Binarization is applied first to reduce the number of combinations
    /// generated during epsilon elimination for long rules with nullable nonterminals.
    /// As a final step, non-generating and unreachable nonterminals are removed.
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
        let s5 = removeNonGenerating s4
        let s6 = removeUnreachable s5
        s6


/// An extended grammar: the original grammar augmented with a fresh start nonterminal S'.
/// The augmented grammar has S' -> S as the first rule, making the acceptance condition
/// explicit: S' is reduced when the original start is fully parsed.
/// The type preserves the relationship between the original and augmented grammars.
type ExtendedGrammar<'t, 'nt> =
    { OriginalGrammar: Grammar<'t, 'nt>
      FreshStart: Nonterminal<'nt>
      Extended: Grammar<'t, 'nt> }


module ExtendedGrammar =

    /// Creates an extended grammar by augmenting the given grammar with a fresh start nonterminal.
    let create (freshStart: Nonterminal<'nt>) (grammar: Grammar<'t, 'nt>) : ExtendedGrammar<'t, 'nt> =
        let ext = Grammar.augmentGrammar freshStart grammar

        { OriginalGrammar = grammar
          FreshStart = freshStart
          Extended = ext }

    /// Returns the original (non-extended) grammar.
    let originalGrammar (eg: ExtendedGrammar<'t, 'nt>) : Grammar<'t, 'nt> = eg.OriginalGrammar

    /// Returns the fresh start nonterminal (S') used for augmentation.
    let freshStart (eg: ExtendedGrammar<'t, 'nt>) : Nonterminal<'nt> = eg.FreshStart

    /// Returns the extended (augmented) grammar.
    let extGrammar (eg: ExtendedGrammar<'t, 'nt>) : Grammar<'t, 'nt> = eg.Extended

    /// Returns the start nonterminal of the original grammar.
    let originalStart (eg: ExtendedGrammar<'t, 'nt>) : Nonterminal<'nt> = eg.OriginalGrammar.Start
