module KroneckerCfpqVsValiantPropertyTests

open FsCheck.Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Properties(Arbitrary = [| typeof<CfpqTestHelpers.CfpqGrammarWordGenerators> |])>]
module KroneckerCfpqVsValiantProperty =

    // A linear input is a partial case of a graph and Valiant's table contains results for all
    // substrings, so both must agree for all strings and grammars (task clause 2). Valiant's final
    // table is n x n with cell (i, j) holding the nonterminals deriving s[i..j]; epsilon
    // derivations are not in the table, so diagonal cells are checked against nullability. The
    // comparison runs over the nonterminals shared by the RSM and the CNF conversion: toCnf
    // preserves original names (fresh names are only added for binarization and similar), so a
    // surviving nonterminal has the same language on both sides; dropped ones have no counterpart.
    // The diagonal reference is the RSM's own nullability, not the CNF's: toCnf strips epsilon
    // from nullable non-start symbols, so CNF nullability would miss exactly those diagonals that
    // the Kronecker product (computing the RSM's language) correctly fills.
    [<Property>]
    let ``Kronecker CFPQ on a linear path graph equals Valiant's full parsing table``
        (c: CfpqTestHelpers.GrammarCase)
        (word: Terminal<string> list)
        =
        let n = List.length word
        let graph = CfpqTestHelpers.linearPathGraph (List.map (fun (Terminal t) -> t) word)

        let result = KroneckerCfpq.evaluate c.Rsm graph

        let valiantTable, valiantAccepted =
            Valiant.parseWithTable Grammar.freshStringNonterminal c.Grammar word

        let sharedNts =
            c.CnfNts |> List.filter (fun a -> RSM.nonterminals c.Rsm |> List.contains a)

        let mutable ok = true

        // Off-diagonal cells: for i < j, A derives the substring s[i..j-1] iff Kronecker's cell
        // (i, j) is filled.
        for i in 0 .. n - 1 do
            for j in i + 1 .. n do
                let kronSet =
                    sharedNts
                    |> List.filter (fun a -> (Map.find a result.Matrices).[i, j])
                    |> Set.ofList

                let valiantSet =
                    valiantTable.[i, j - 1] |> Set.filter (fun a -> sharedNts |> List.contains a)

                if kronSet <> valiantSet then
                    ok <- false

        // Diagonal cells: A derives epsilon (the empty path at vertex i) iff A is nullable in the
        // RSM (its block start state is final); see rsmNullable for why the CNF's nullability is
        // the wrong reference here.
        let rsmNull = CfpqTestHelpers.rsmNullable c.Rsm

        for i in 0..n do
            for a in sharedNts do
                let kronHas = (Map.find a result.Matrices).[i, i]

                if kronHas <> Set.contains a rsmNull then
                    ok <- false

        // Upper triangle: the linear graph has no backward edges and no epsilon for i <> j.
        for i in 0..n do
            for j in 0 .. i - 1 do
                for a in sharedNts do
                    if (Map.find a result.Matrices).[i, j] then
                        ok <- false

        // Acceptance: the start nonterminal derives the whole word. For the empty word Valiant's
        // acceptance is Grammar.isEpsilonAccepted on the CNF — transitive nullability of the start,
        // equal to the RSM start block's nullability since the RSM is built from the same grammar.
        let kronAccepted = (KroneckerCfpq.answer result c.Rsm).[0, n]

        if kronAccepted <> valiantAccepted then
            ok <- false

        ok
