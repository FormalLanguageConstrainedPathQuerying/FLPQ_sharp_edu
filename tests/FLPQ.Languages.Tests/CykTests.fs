module CykTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars


module Grammar1Tests =

    [<Fact>]
    let ``CYK accepts expected strings`` () =
        for s in grammar1Accept do
            Assert.True(Cyk.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s), s)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        for s in grammar1Reject do
            Assert.False(Cyk.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s), s)


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
        for s in grammar3Accept do
            Assert.True(Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s), s)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        for s in grammar3Reject do
            Assert.False(Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s), s)


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
        let g =
            Grammar.parseGrammar
                "
        S -> a
        "

        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a"))
        Assert.False(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "b"))

    [<Fact>]
    let ``parse handles longer accepted string`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S a
        S -> a
        S -> eps
        "

        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a a"))
        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a a a a"))
        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a a a"))
        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a"))
        Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals "a a"))


module Grammar6Tests =

    let grammars = [ grammar6; grammar7; grammar8 ]

    [<Fact>]
    let ``CYK accepts expected expression strings`` () =
        for g in grammars do
            for s in exprAccept do
                Assert.True(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s), s)

    [<Fact>]
    let ``CYK rejects expected expression strings`` () =
        for g in grammars do
            for s in exprReject do
                Assert.False(Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s), s)


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
