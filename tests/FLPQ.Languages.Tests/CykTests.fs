module CykTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

let private g (lang: Language) (name: string) = lang.Grammars |> List.find (fun g -> g.Name = name)

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private expr = LanguageRegistry.ArithExpr
let private twoTrack = LanguageRegistry.TwoTrackDyck

let private grammar1 = (g dyck1 "grammar1").Grammar
let private grammar2 = (g dyck1 "grammar2").Grammar
let private grammar3 = (g aplus "grammar3").Grammar
let private grammar4 = (g aplus "grammar4").Grammar
let private grammar5 = (g aplus "grammar5").Grammar
let private grammar6 = (g expr "grammar6").Grammar
let private grammar7 = (g expr "grammar7").Grammar
let private grammar8 = (g expr "grammar8").Grammar
let private grammar9 = (g twoTrack "grammar9").Grammar
let private grammar10 = (g twoTrack "grammar10").Grammar

module Grammar1Tests =

    [<Fact>]
    let ``CYK accepts expected strings`` () =
        let lang = LanguageRegistry.Dyck1

        let failures =
            TestHelpers.collectAcceptFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal))
                lang

        Assert.Empty(failures)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        let lang = LanguageRegistry.Dyck1

        let failures =
            TestHelpers.collectRejectFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal))
                lang

        Assert.Empty(failures)


module Grammar2Tests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 1`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar2
                (Tokenizer.tokenizeTerminals s)


module Grammar3Tests =

    [<Fact>]
    let ``CYK accepts expected strings`` () =
        let lang = LanguageRegistry.APlus

        let failures =
            TestHelpers.collectAcceptFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal))
                lang

        Assert.Empty(failures)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        let lang = LanguageRegistry.APlus

        let failures =
            TestHelpers.collectRejectFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal))
                lang

        Assert.Empty(failures)


module Grammar4Tests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 3`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar4
                (Tokenizer.tokenizeTerminals s)


module Grammar5Tests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 3`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar5
                (Tokenizer.tokenizeTerminals s)


module FactTests =

    [<Fact>]
    let ``parseWithTrace returns non-empty list for non-empty input`` () =
        let trace =
            Cyk.parseWithTrace Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals "a a a")

        Assert.NotEmpty(trace)

    [<Fact>]
    let ``parse handles single character`` () =
        let g = (LanguageRegistry.SingleA.Grammars[0]).Grammar

        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a"))
        Assert.False(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "b"))

    [<Fact>]
    let ``parse handles longer accepted string`` () =
        let g = (LanguageRegistry.AStar.Grammars[1]).Grammar

        for s in LanguageRegistry.AStar.AcceptStrings do
            Assert.True(
                Cyk.parse Grammar.freshStringNonterminal g (s |> List.map Terminal),
                $"""{String.concat " " s}"""
            )


module Grammar6Tests =

    let grammars = [ grammar6; grammar7; grammar8 ]

    [<Fact>]
    let ``CYK accepts expected expression strings`` () =
        let failures =
            TestHelpers.collectAcceptFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal))
                LanguageRegistry.ArithExpr

        Assert.Empty(failures)

    [<Fact>]
    let ``CYK rejects expected expression strings`` () =
        let failures =
            TestHelpers.collectRejectFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal))
                LanguageRegistry.ArithExpr

        Assert.Empty(failures)


module Grammar6PropertyTests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.ExprString> |])>]
    module AgreementTests =

        [<Property>]
        let ``grammar6 and grammar7 agree on expression strings`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar7
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``grammar6 and grammar8 agree on expression strings`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar8
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``grammar7 and grammar8 agree on expression strings`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar8
                (Tokenizer.tokenizeTerminals s)
