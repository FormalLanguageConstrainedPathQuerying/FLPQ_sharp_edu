module CrossParserEquivalenceTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
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
            gllAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``GLL and CYK agree on Dyck1 grammar2`` (s: string) =
            let g = dyck1.Grammars[1].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module GllVsCykAplus =
        [<Property>]
        let ``GLL and CYK agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``GLL and CYK agree on APlus grammar4`` (s: string) =
            let g = aplus.Grammars[1].Grammar
            let input = TestHelpers.stringToTerminals s
            let gllAccepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted
            gllAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module RnglrVsCyk =
        [<Property>]
        let ``RNGLR and CYK agree on Dyck1 grammar1`` (s: string) =
            let g = dyck1.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            let rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted
            rnglrAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module RnglrVsCykAplus =
        [<Property>]
        let ``RNGLR and CYK agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            let rnglrAccepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted
            rnglrAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

module GllVsRnglr =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Dyck1 =
        [<Property>]
        let ``GLL and RNGLR agree on Dyck1 grammar1`` (s: string) =
            let g = dyck1.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            let gllAccepts = TestHelpers.acceptsWithScc GLL.buildPathIndex PathIndex.isAccepted

            let rnglrAccepts =
                TestHelpers.acceptsWithScc Rnglr.buildPathIndex PathIndex.isAccepted

            let gllOk, gllScc = gllAccepts (TestHelpers.grammarToRsm g) input
            let rnglrOk, rnglrScc = rnglrAccepts (TestHelpers.grammarToRsm g) input
            gllOk = rnglrOk && gllScc = rnglrScc

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module APlus =
        [<Property>]
        let ``GLL and RNGLR agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            let gllAccepts = TestHelpers.acceptsWithScc GLL.buildPathIndex PathIndex.isAccepted

            let rnglrAccepts =
                TestHelpers.acceptsWithScc Rnglr.buildPathIndex PathIndex.isAccepted

            let gllOk, gllScc = gllAccepts (TestHelpers.grammarToRsm g) input
            let rnglrOk, rnglrScc = rnglrAccepts (TestHelpers.grammarToRsm g) input
            gllOk = rnglrOk && gllScc = rnglrScc

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
