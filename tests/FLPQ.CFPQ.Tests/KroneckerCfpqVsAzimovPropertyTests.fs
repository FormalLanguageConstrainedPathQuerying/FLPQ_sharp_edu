module KroneckerCfpqVsAzimovPropertyTests

open FsCheck.Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Properties(Arbitrary = [| typeof<IntersectionGenerators>
                            typeof<RegexGenerators>
                            typeof<CfpqTestHelpers.CfpqGrammarWordGenerators> |])>]
module KroneckerCfpqVsAzimovProperty =

    // Equal results for all graphs and languages (task clause 3): a random graph times every
    // registry grammar. The Kronecker side runs on the registry RSM, the Azimov side on the same
    // grammar; the start answers and every shared nonterminal's full matrix must coincide.
    [<Property>]
    let ``Kronecker CFPQ equals Azimov CFPQ for every registry grammar``
        (nfa: NFA<string, int>)
        (c: CfpqTestHelpers.GrammarCase)
        =
        let graph = CfpqTestHelpers.nfaToCfpqGraph nfa

        let kronResult = KroneckerCfpq.evaluate c.Rsm graph

        let azimovResult =
            AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal c.Grammar graph

        CfpqTestHelpers.compareKronVsAzimov c.Rsm c.Cnf kronResult azimovResult

    // Same comparison for regexp-defined languages: a single-block RSM and the grammar converted
    // from it.
    [<Property>]
    let ``Kronecker CFPQ equals Azimov CFPQ for regexp languages``
        (nfa: NFA<string, int>)
        (regexp: Regexp<string, string>)
        =
        let graph = CfpqTestHelpers.nfaToCfpqGraph nfa

        let rsm =
            RsmBuilder.buildRSMWithStart (Map.ofList [ Nonterminal "S", regexp ]) (Nonterminal "S")

        let grammar = RsmToGrammar.convert rsm
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let kronResult = KroneckerCfpq.evaluate rsm graph

        let azimovResult =
            AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal grammar graph

        CfpqTestHelpers.compareKronVsAzimov rsm cnf kronResult azimovResult
