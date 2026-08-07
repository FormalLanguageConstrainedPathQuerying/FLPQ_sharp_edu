module CrossParserEquivalenceTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus

module VsCyk =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module GllVsCyk =
        [<Property>]
        let ``GLL and CYK agree on Dyck1 grammar1`` (s: string) =
            let g = dyck1.Grammars[0].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts dyck1.Grammars[0].Rsm input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``GLL and CYK agree on Dyck1 grammar2`` (s: string) =
            let g = dyck1.Grammars[1].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts dyck1.Grammars[1].Rsm input = TestHelpers.cykAccepts g input

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module GllVsCykAplus =
        [<Property>]
        let ``GLL and CYK agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts aplus.Grammars[0].Rsm input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``GLL and CYK agree on APlus grammar4`` (s: string) =
            let g = aplus.Grammars[1].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts aplus.Grammars[1].Rsm input = TestHelpers.cykAccepts g input

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module RnglrVsCyk =
        [<Property>]
        let ``RNGLR and CYK agree on Dyck1 grammar1`` (s: string) =
            let g = dyck1.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            let rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted
            rnglrAccepts dyck1.Grammars[0].Rsm input = TestHelpers.cykAccepts g input

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module RnglrVsCykAplus =
        [<Property>]
        let ``RNGLR and CYK agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            let rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted
            rnglrAccepts aplus.Grammars[0].Rsm input = TestHelpers.cykAccepts g input

module GllVsRnglr =

    module private Helpers =
        let private gllAccepts =
            TestHelpers.acceptsWithScc GLL.buildPathIndex PathIndex.isAccepted

        let private rnglrAccepts =
            TestHelpers.acceptsWithScc Rnglr.buildPathIndex PathIndex.isAccepted

        let checkLanguages (langs: Language list) (arbType: System.Type) =
            let config = Config.QuickThrowOnFailure.WithMaxTest(100).WithArbitrary([ arbType ])

            Check.One(
                config,
                fun (s: string) ->
                    let rawInput = s.Replace(" ", "")
                    let input = TestHelpers.stringToTerminals rawInput

                    langs
                    |> List.iter (fun lang ->
                        lang.Grammars
                        |> List.iter (fun g ->
                            let gllOk, gllScc = gllAccepts g.Rsm input
                            let rnglrOk, rnglrScc = rnglrAccepts g.Rsm input

                            if gllOk <> rnglrOk || gllScc <> rnglrScc then
                                failwithf
                                    "GLL=%b(%d) RNGLR=%b(%d) for %s/%s input='%s'"
                                    gllOk
                                    gllScc
                                    rnglrOk
                                    rnglrScc
                                    lang.Name
                                    g.Name
                                    rawInput))
            )

    [<Fact>]
    let ``GLL and RNGLR agree on AB-string languages (Dyck1, AltAB, ANB, ANBN, AStarBStar)`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.Dyck1
              LanguageRegistry.AltAB
              LanguageRegistry.ANB
              LanguageRegistry.ANBN
              LanguageRegistry.AStarBStar ]
            typeof<GenToArbitrary.AbString>

    [<Fact>]
    let ``GLL and RNGLR agree on A-string languages (APlus, AStar)`` () =
        Helpers.checkLanguages [ LanguageRegistry.APlus; LanguageRegistry.AStar ] typeof<GenToArbitrary.AString>

    [<Fact>]
    let ``GLL and RNGLR agree on expression language (ArithExpr)`` () =
        Helpers.checkLanguages [ LanguageRegistry.ArithExpr ] typeof<GenToArbitrary.ExprString>

    [<Fact>]
    let ``GLL and RNGLR agree on OpExpr language`` () =
        Helpers.checkLanguages [ LanguageRegistry.OpExpr ] typeof<GenToArbitrary.OpExprString>

    [<Fact>]
    let ``GLL and RNGLR agree on multi-symbol languages (TwoTrackDyck, DualDyck)`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.TwoTrackDyck; LanguageRegistry.DualDyck ]
            typeof<GenToArbitrary.AbcdxyString>

    [<Fact>]
    let ``GLL and RNGLR agree on PolyAlphabet languages (LL2Test, LL3Test)`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.LL2Test; LanguageRegistry.LL3Test ]
            typeof<GenToArbitrary.PolyAlphabetString>

    [<Fact>]
    let ``GLL and RNGLR agree on constrained languages (SingleA, SingleAB, EpsilonOnly)`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.SingleA
              LanguageRegistry.SingleAB
              LanguageRegistry.EpsilonOnly ]
            typeof<GenToArbitrary.AbString>

module VsDfa =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbcxdString> |])>]
    module GllVsDfa =
        [<Property(MaxTest = 50)>]
        let ``S -> a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a")
                "a *"
                s

        [<Property(MaxTest = 50)>]
        let ``S -> a* a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a")
                "a * a *"
                s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a" || c = "b")
                "( a | b ) *"
                s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* (a | c)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a" || c = "b" || c = "c")
                "( a | b ) * ( a | c ) *"
                s

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module RnglrVsDfaA =
        [<Property(MaxTest = 50)>]
        let ``S -> a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a")
                "a *"
                s

        [<Property(MaxTest = 50)>]
        let ``S -> a* a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a")
                "a * a *"
                s

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module RnglrVsDfaAb =
        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a" || c = "b")
                "( a | b ) *"
                s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* (a | c)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                (fun c -> c = "a" || c = "b" || c = "c")
                "( a | b ) * ( a | c ) *"
                s

module CykVsValiantVsModifiedValiant =

    module private Helpers =
        let checkLanguages (langs: Language list) (arbType: System.Type) (tokenize: string -> Terminal<string> list) =
            let config = Config.QuickThrowOnFailure.WithMaxTest(100).WithArbitrary([ arbType ])

            Check.One(
                config,
                fun (s: string) ->
                    let input = tokenize s

                    langs
                    |> List.iter (fun lang ->
                        lang.Grammars
                        |> List.filter TestHelpers.isCykValiantCompatible
                        |> List.iter (fun g -> TestHelpers.checkCykValiantEquivalence g.Grammar input))
            )

        let singleCharTokenize (s: string) =
            s.Replace(" ", "") |> TestHelpers.stringToTerminals

        let multiCharTokenize (s: string) = Tokenizer.tokenizeTerminals s

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on AB-string languages`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.Dyck1
              LanguageRegistry.AltAB
              LanguageRegistry.ANB
              LanguageRegistry.ANBN
              LanguageRegistry.AStarBStar
              LanguageRegistry.SingleA
              LanguageRegistry.SingleAB
              LanguageRegistry.EpsilonOnly ]
            typeof<GenToArbitrary.AbString>
            Helpers.singleCharTokenize

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on A-string languages`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.APlus; LanguageRegistry.AStar ]
            typeof<GenToArbitrary.AString>
            Helpers.singleCharTokenize

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on expression language`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.ArithExpr ]
            typeof<GenToArbitrary.ExprString>
            Helpers.multiCharTokenize

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on operator expression language`` () =
        Helpers.checkLanguages [ LanguageRegistry.OpExpr ] typeof<GenToArbitrary.OpExprString> Helpers.multiCharTokenize

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on multi-symbol languages`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.TwoTrackDyck; LanguageRegistry.DualDyck ]
            typeof<GenToArbitrary.AbcdxyString>
            Helpers.singleCharTokenize

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on PolyAlphabet languages`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.LL2Test; LanguageRegistry.LL3Test ]
            typeof<GenToArbitrary.PolyAlphabetString>
            Helpers.singleCharTokenize

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on constrained languages`` () =
        Helpers.checkLanguages
            [ LanguageRegistry.DoubleA
              LanguageRegistry.AOrEps
              LanguageRegistry.ABPlus
              LanguageRegistry.FourTerm
              LanguageRegistry.MixedPairs
              LanguageRegistry.AX
              LanguageRegistry.SingleB
              LanguageRegistry.TestInfraGrammars ]
            typeof<GenToArbitrary.AbString>
            Helpers.singleCharTokenize
