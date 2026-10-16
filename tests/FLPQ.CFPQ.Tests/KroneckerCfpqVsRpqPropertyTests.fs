module KroneckerCfpqVsRpqPropertyTests

open FsCheck.Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.RPQ
open FLPQ.TestUtilities

[<Properties(Arbitrary = [| typeof<IntersectionGenerators>; typeof<RegexGenerators> |])>]
module KroneckerCfpqVsRpqProperty =

    // Regular languages are a subclass of context-free languages; both algorithms implement
    // complete reachability semantics, so the results must coincide on arbitrary graph and
    // language (task clause 1).
    [<Property>]
    let ``Kronecker CFPQ reachability equals all-pairs RPQ`` (nfa: NFA<string, int>) (regexp: Regexp<string, string>) =
        let v = List.length nfa.States

        if v = 0 then
            true
        else
            // Kronecker side: graph from the NFA's transitions; single-block RSM for the regexp
            // (the block is the regexp's DFA).
            let cfpqGraph = CfpqTestHelpers.nfaToCfpqGraph nfa

            let rsm =
                RsmBuilder.buildRSMWithStart (Map.ofList [ Nonterminal "S", regexp ]) (Nonterminal "S")

            let result = KroneckerCfpq.evaluate rsm cfpqGraph

            let actual = KroneckerCfpq.answer result rsm

            // RPQ side: the full |V| x |V| all-pairs matrix.
            let _, expected = ArroyueloRPQ.evaluateBooleanWithTrace nfa regexp

            // Exact matrix equality (all pairs), not just per-source rows.
            let mutable ok = true

            for i in 0 .. v - 1 do
                for j in 0 .. v - 1 do
                    if actual.[i, j] <> expected.[i, j] then
                        ok <- false

            ok
