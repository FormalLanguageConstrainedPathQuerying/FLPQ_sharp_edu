module CykTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

open TestGrammars

open TestGrammars

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

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
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

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 3`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s) = Cyk.parse
                Grammar.freshStringNonterminal
                grammar4
                (Tokenizer.tokenizeTerminals s)


module Grammar5Tests =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
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

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
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
