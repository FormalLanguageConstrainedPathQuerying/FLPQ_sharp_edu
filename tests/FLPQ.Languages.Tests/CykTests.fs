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
            Assert.True(Cyk.parse grammar1 s, s)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        for s in grammar1Reject do
            Assert.False(Cyk.parse grammar1 s, s)


module Grammar2Tests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 1`` (s: string) =
            Cyk.parse grammar1 s = Cyk.parse grammar2 s


module Grammar3Tests =

    [<Fact>]
    let ``CYK accepts expected strings`` () =
        for s in grammar3Accept do
            Assert.True(Cyk.parse grammar3 s, s)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        for s in grammar3Reject do
            Assert.False(Cyk.parse grammar3 s, s)


module Grammar4Tests =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 3`` (s: string) =
            Cyk.parse grammar3 s = Cyk.parse grammar4 s


module Grammar5Tests =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 3`` (s: string) =
            Cyk.parse grammar3 s = Cyk.parse grammar5 s


module FactTests =

    [<Fact>]
    let ``parseWithTrace returns non-empty list for non-empty input`` () =
        let trace = Cyk.parseWithTrace grammar3 "a a a"
        Assert.NotEmpty(trace)

    [<Fact>]
    let ``tableToTeX contains pNiceMatrix`` () =
        let trace = Cyk.parseWithTrace grammar1 "a b"
        let tex = Cyk.tableToTeX (fun s -> string s) trace.[0]
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.Contains(@"\end{pNiceMatrix}", tex)

    [<Fact>]
    let ``tableToTeX prints empty cells as cdot`` () =
        let trace = Cyk.parseWithTrace grammar3 "a a"

        let tex = Cyk.tableToTeX (fun s -> string s) trace.[0]
        Assert.Contains(@"\cdot", tex)

    [<Fact>]
    let ``parse handles single character`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a
        "

        Assert.True(Cyk.parse g "a")
        Assert.False(Cyk.parse g "b")

    [<Fact>]
    let ``parse handles longer accepted string`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S a
        S -> a
        S -> eps
        "

        Assert.True(Cyk.parse g "a a a")
        Assert.True(Cyk.parse g "a a a a a")
        Assert.True(Cyk.parse g "a a a a")
        Assert.True(Cyk.parse g "a")
        Assert.True(Cyk.parse g "a a")


module Grammar6Tests =

    let grammars = [ grammar6; grammar7; grammar8 ]

    [<Fact>]
    let ``CYK accepts expected expression strings`` () =
        for g in grammars do
            for s in exprAccept do
                Assert.True(Cyk.parse g s, s)

    [<Fact>]
    let ``CYK rejects expected expression strings`` () =
        for g in grammars do
            for s in exprReject do
                Assert.False(Cyk.parse g s, s)


module Grammar6PropertyTests =

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module AgreementTests =

        [<Property>]
        let ``grammar6 and grammar7 agree on expression strings`` (s: string) =
            Cyk.parse grammar6 s = Cyk.parse grammar7 s

        [<Property>]
        let ``grammar6 and grammar8 agree on expression strings`` (s: string) =
            Cyk.parse grammar6 s = Cyk.parse grammar8 s

        [<Property>]
        let ``grammar7 and grammar8 agree on expression strings`` (s: string) =
            Cyk.parse grammar7 s = Cyk.parse grammar8 s
