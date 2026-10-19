module CfpqEpsilonAcceptanceTests

open Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.TestUtilities

// The empty path on a 1-vertex edgeless graph: the only possible derivation is epsilon, so the
// start's [0, 0] cell is true exactly when the language contains the empty word.
let private emptyPathGraph = CfpqTestHelpers.linearPathGraph []

// Transitively nullable starts (no direct Start -> eps rule): issue 8 repro grammars.
let private nullableStartEntries =
    [ LanguageRegistry.findGrammar LanguageRegistry.TwoTrackDyck "variantA"
      LanguageRegistry.findGrammar LanguageRegistry.DualDyck "ebnfConcat" ]

let private nonNullableStartEntry =
    LanguageRegistry.findGrammar LanguageRegistry.APlus "rightRecursive"

module EpsilonAcceptanceTests =

    [<Fact>]
    let ``Azimov accepts the empty path for a transitively nullable start`` () =
        for entry in nullableStartEntries do
            let cnf = Grammar.toCnf Grammar.freshStringNonterminal entry.Grammar

            let result =
                AzimovCfpq.evaluate
                    CfpqSemiring.ReachabilitySemiring
                    Grammar.freshStringNonterminal
                    entry.Grammar
                    emptyPathGraph

            Assert.True(
                Option.isSome result.Matrices.[cnf.Start].[0, 0],
                $"Azimov must accept the empty path for {entry.Name}"
            )

    [<Fact>]
    let ``Kronecker accepts the empty path for a transitively nullable start`` () =
        for entry in nullableStartEntries do
            let result = KroneckerCfpq.evaluate entry.Rsm emptyPathGraph

            Assert.True(
                (KroneckerCfpq.answer result entry.Rsm).[0, 0],
                $"Kronecker must accept the empty path for {entry.Name}"
            )

    [<Fact>]
    let ``Azimov rejects the empty path for a non-nullable start`` () =
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal nonNullableStartEntry.Grammar

        let result =
            AzimovCfpq.evaluate
                CfpqSemiring.ReachabilitySemiring
                Grammar.freshStringNonterminal
                nonNullableStartEntry.Grammar
                emptyPathGraph

        Assert.False(Option.isSome result.Matrices.[cnf.Start].[0, 0])

    [<Fact>]
    let ``Kronecker rejects the empty path for a non-nullable start`` () =
        let result = KroneckerCfpq.evaluate nonNullableStartEntry.Rsm emptyPathGraph

        Assert.False((KroneckerCfpq.answer result nonNullableStartEntry.Rsm).[0, 0])
