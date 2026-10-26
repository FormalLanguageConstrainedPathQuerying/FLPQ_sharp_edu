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

    let private gllAccepts =
        TestHelpers.acceptsWithScc GLL.buildPathIndex PathIndex.isAccepted

    let private rnglrAccepts =
        TestHelpers.acceptsWithScc Rnglr.buildPathIndex PathIndex.isAccepted

    let private checkGllVsRnglr (lang: Language) (g: AnnotatedGrammar) (input: Terminal<string> list) : unit =
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
                (TestHelpers.terminalsToString input)

    [<Fact>]
    let ``GLL and RNGLR agree on AB-string languages (Dyck1, AltAB, ANB, ANBN, AStarBStar)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.Dyck1
              LanguageRegistry.AltAB
              LanguageRegistry.ANB
              LanguageRegistry.ANBN
              LanguageRegistry.AStarBStar ]
            typeof<GenToArbitrary.AbString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on APlus`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.APlus ]
            typeof<GenToArbitrary.AString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on AStar (bounded input)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.AStar ]
            typeof<GenToArbitrary.AStarString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on expression language (ArithExpr)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.ArithExpr ]
            typeof<GenToArbitrary.ExprString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on OpExpr language`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.OpExpr ]
            typeof<GenToArbitrary.OpExprString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on multi-symbol languages (TwoTrackDyck, DualDyck)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.TwoTrackDyck; LanguageRegistry.DualDyck ]
            typeof<GenToArbitrary.AbcdxyString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on PolyAlphabet languages (LL2Test, LL3Test)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.LL2Test; LanguageRegistry.LL3Test ]
            typeof<GenToArbitrary.PolyAlphabetString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

    [<Fact>]
    let ``GLL and RNGLR agree on constrained languages (SingleA, SingleAB, EpsilonOnly)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.SingleA
              LanguageRegistry.SingleAB
              LanguageRegistry.EpsilonOnly ]
            typeof<GenToArbitrary.AbString>
            TestHelpers.singleCharTokenize
            checkGllVsRnglr

module VsDfa =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbcxdString> |])>]
    module GllVsDfa =
        [<Property(MaxTest = 50)>]
        let ``S -> a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aStar
                s

        [<Property(MaxTest = 50)>]
        let ``S -> a* a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aStarAStar
                s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aOrBStar
                s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* (a | c)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aOrBStar_aOrCStar
                s

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module RnglrVsDfaA =
        [<Property(MaxTest = 50)>]
        let ``S -> a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aStar
                s

        [<Property(MaxTest = 50)>]
        let ``S -> a* a* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aStarAStar
                s

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module RnglrVsDfaAb =
        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aOrBStar
                s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* (a | c)* matches DFA`` (s: string) =
            ParsingTestCases.Runners.runRegexEquivalenceTests
                (TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted)
                ParsingTestCases.RegexEquivalenceCases.aOrBStar_aOrCStar
                s

module CykVsValiantVsModifiedValiant =

    let private checkCykValiant (_lang: Language) (g: AnnotatedGrammar) (input: Terminal<string> list) : unit =
        TestHelpers.checkCykValiantEquivalence g.Grammar input

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on AB-string languages`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.Dyck1
              LanguageRegistry.AltAB
              LanguageRegistry.ANB
              LanguageRegistry.ANBN
              LanguageRegistry.AStarBStar
              LanguageRegistry.SingleA
              LanguageRegistry.SingleAB
              LanguageRegistry.EpsilonOnly ]
            typeof<GenToArbitrary.AbString>
            TestHelpers.singleCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on APlus`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.APlus ]
            typeof<GenToArbitrary.AString>
            TestHelpers.singleCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on AStar (bounded input)`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.AStar ]
            typeof<GenToArbitrary.AStarString>
            TestHelpers.singleCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on expression language`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.ArithExpr ]
            typeof<GenToArbitrary.ExprString>
            TestHelpers.multiCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on operator expression language`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.OpExpr ]
            typeof<GenToArbitrary.OpExprString>
            TestHelpers.multiCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on multi-symbol languages`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.TwoTrackDyck; LanguageRegistry.DualDyck ]
            typeof<GenToArbitrary.AbcdxyString>
            TestHelpers.singleCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on PolyAlphabet languages`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.LL2Test; LanguageRegistry.LL3Test ]
            typeof<GenToArbitrary.PolyAlphabetString>
            TestHelpers.singleCharTokenize
            checkCykValiant

    [<Fact>]
    let ``CYK, Valiant, and Modified Valiant agree on constrained languages`` () =
        TestHelpers.checkLanguages
            [ LanguageRegistry.DoubleA
              LanguageRegistry.AOrEps
              LanguageRegistry.ABPlus
              LanguageRegistry.FourTerm
              LanguageRegistry.MixedPairs
              LanguageRegistry.AX
              LanguageRegistry.SingleB
              LanguageRegistry.TestInfraGrammars ]
            typeof<GenToArbitrary.AbString>
            TestHelpers.singleCharTokenize
            checkCykValiant
