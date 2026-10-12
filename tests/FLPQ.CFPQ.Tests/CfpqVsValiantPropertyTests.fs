module CfpqVsValiantPropertyTests

open FSharpPlus.Data
open FsCheck
open FsCheck.Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

/// A registry grammar with toCnf precomputed once (outside the property body). The comparison
/// runs over the CNF grammar's nonterminals: toCnf may drop original nonterminals (useless-symbol
/// elimination, e.g. all-epsilon grammars collapse to a single fresh start), and both algorithms
/// run on the same CNF conversion, so every CNF nonterminal is present on both sides.
type GrammarCase =
    { Name: string
      Grammar: Grammar<string, string>
      Cnf: Grammar<string, string>
      CnfNts: Nonterminal<string> list
      CnfNullable: Set<Nonterminal<string>>
      Alphabet: string list }

/// Terminal alphabet of a grammar (sorted, deduplicated).
let private terminalAlphabet (g: Grammar<string, string>) : string list =
    g.Rules
    |> List.collect (fun r ->
        match r.Rhs with
        | Rhs.EpsilonRhs -> []
        | Rhs.Symbols nel ->
            NonEmptyList.toList nel
            |> List.choose (function
                | Symbol.T(Terminal t) -> Some t
                | _ -> None))
    |> List.distinct
    |> List.sort

/// Nullable nonterminals (those deriving epsilon) by the standard fixed point.
let private nullableNts (g: Grammar<string, string>) : Set<Nonterminal<string>> =
    let rec step (acc: Set<Nonterminal<string>>) : Set<Nonterminal<string>> =
        let added =
            g.Rules
            |> List.filter (fun r ->
                match r.Rhs with
                | Rhs.EpsilonRhs -> true
                | Rhs.Symbols nel ->
                    NonEmptyList.toList nel
                    |> List.forall (function
                        | Symbol.N nt -> Set.contains nt acc
                        | _ -> false))
            |> List.map (fun r -> r.Lhs)
            |> Set.ofList
            |> Set.filter (fun nt -> not (Set.contains nt acc))

        if Set.isEmpty added then
            acc
        else
            step (Set.union acc added)

    step Set.empty

/// All registry grammars with toCnf precomputed once per grammar.
let private cases: GrammarCase list =
    LanguageRegistry.allGrammars
    |> List.map (fun ag ->
        let g = ag.Grammar
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

        { Name = ag.Name
          Grammar = g
          Cnf = cnf
          // The same nonterminal set AzimovCfpq.evaluate initializes matrices for.
          CnfNts = Set.toList (Set.union (Grammar.nonterminalsOf cnf) (Set.singleton cnf.Start))
          CnfNullable = nullableNts cnf
          Alphabet = terminalAlphabet g })

/// Linear path graph for a word: |s|+1 vertices, edge i -> i+1 labeled s[i].
let private linearPathGraph (word: string list) : CfpqInput.CfpqGraph<string> =
    let n = List.length word

    { VertexCount = n + 1
      Labels =
        word
        |> List.mapi (fun i t -> t, (i, i + 1))
        |> List.groupBy fst
        |> List.map (fun (t, cells) ->
            t, Matrix.create (n + 1) (n + 1) (fun i j -> cells |> List.exists (fun (_, (a, b)) -> (a, b) = (i, j))))
        |> Map.ofList }

let private genCase: Gen<GrammarCase * Terminal<string> list> =
    MyGen.elements cases
    |> MyGen.bind (fun c ->
        if List.isEmpty c.Alphabet then
            MyGen.constant (c, [])
        else
            MyGen.choose (0, 5)
            |> MyGen.bind (fun len ->
                MyGen.listOfLength len (MyGen.elements c.Alphabet)
                |> MyGen.map (fun ts -> c, List.map Terminal ts)))

type CfpqVsValiantGenerators =
    static member Case() : Arbitrary<GrammarCase * Terminal<string> list> = MyArb.fromGen genCase

[<Properties(Arbitrary = [| typeof<CfpqVsValiantGenerators> |])>]
module CfpqVsValiantProperty =

    // A linear input is a partial case of a graph and Valiant's table contains results for all
    // substrings, so both must agree for all strings and grammars (task clause 2). Valiant's final
    // table is n x n with cell (i, j) holding the nonterminals deriving s[i..j]; epsilon
    // derivations are not in the table, so diagonal cells are checked against nullability.
    [<Property>]
    let ``Azimov on a linear path graph equals Valiant's full parsing table``
        (c: GrammarCase)
        (word: Terminal<string> list)
        =
        let n = List.length word
        let graph = linearPathGraph (List.map (fun (Terminal t) -> t) word)

        let result =
            AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal c.Grammar graph

        let valiantTable, valiantAccepted =
            Valiant.parseWithTable Grammar.freshStringNonterminal c.Grammar word

        let mutable ok = true

        // Off-diagonal cells: for i < j, A derives the substring s[i..j-1] iff Azimov's cell (i, j)
        // is filled. Compared for every CNF nonterminal (both algorithms run on the same conversion).
        for i in 0 .. n - 1 do
            for j in i + 1 .. n do
                let azimovSet =
                    c.CnfNts
                    |> List.filter (fun a -> Option.isSome result.Matrices.[a].[i, j])
                    |> Set.ofList

                let valiantSet =
                    valiantTable.[i, j - 1] |> Set.filter (fun a -> c.CnfNts |> List.contains a)

                if azimovSet <> valiantSet then
                    ok <- false

        // Diagonal cells: A derives epsilon (the empty path at vertex i) iff A is nullable.
        for i in 0..n do
            for a in c.CnfNts do
                let azimovHas = Option.isSome result.Matrices.[a].[i, i]

                if azimovHas <> Set.contains a c.CnfNullable then
                    ok <- false

        // Upper triangle: the linear graph has no backward edges and no epsilon for i <> j.
        for i in 0..n do
            for j in 0 .. i - 1 do
                for a in c.CnfNts do
                    if Option.isSome result.Matrices.[a].[i, j] then
                        ok <- false

        // Acceptance: the start nonterminal derives the whole word.
        let azimovAccepted = Option.isSome result.Matrices.[c.Cnf.Start].[0, n]

        if azimovAccepted <> valiantAccepted then
            ok <- false

        ok
