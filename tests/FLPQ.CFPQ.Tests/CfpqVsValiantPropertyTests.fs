module CfpqVsValiantPropertyTests

open FsCheck.Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Properties(Arbitrary = [| typeof<CfpqTestHelpers.CfpqGrammarWordGenerators> |])>]
module CfpqVsValiantProperty =

    // A linear input is a partial case of a graph and Valiant's table contains results for all
    // substrings, so both must agree for all strings and grammars (task clause 2). Valiant's final
    // table is n x n with cell (i, j) holding the nonterminals deriving s[i..j]; epsilon
    // derivations are not in the table, so diagonal cells are checked against nullability.
    [<Property>]
    let ``Azimov on a linear path graph equals Valiant's full parsing table``
        (c: CfpqTestHelpers.GrammarCase)
        (word: Terminal<string> list)
        =
        let n = List.length word
        let graph = CfpqTestHelpers.linearPathGraph (List.map (fun (Terminal t) -> t) word)

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
