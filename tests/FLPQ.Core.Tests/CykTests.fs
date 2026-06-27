module CykTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Core

open TestGrammars


module Grammar1Tests =

    [<Fact>]
    let ``accepts abab`` () = Assert.True(Cyk.parse grammar1 "abab")

    [<Fact>]
    let ``accepts ab`` () = Assert.True(Cyk.parse grammar1 "ab")

    [<Fact>]
    let ``accepts empty string`` () = Assert.True(Cyk.parse grammar1 "")

    [<Fact>]
    let ``accepts aabb`` () = Assert.True(Cyk.parse grammar1 "aabb")

    [<Fact>]
    let ``accepts aababb`` () =
        Assert.True(Cyk.parse grammar1 "aababb")

    [<Fact>]
    let ``rejects aa`` () = Assert.False(Cyk.parse grammar1 "aa")

    [<Fact>]
    let ``rejects bb`` () = Assert.False(Cyk.parse grammar1 "bb")

    [<Fact>]
    let ``rejects abb`` () = Assert.False(Cyk.parse grammar1 "abb")

    [<Fact>]
    let ``rejects abba`` () = Assert.False(Cyk.parse grammar1 "abba")

    [<Fact>]
    let ``rejects b`` () = Assert.False(Cyk.parse grammar1 "b")

    [<Fact>]
    let ``rejects a`` () = Assert.False(Cyk.parse grammar1 "a")

    [<Fact>]
    let ``rejects ababa`` () =
        Assert.False(Cyk.parse grammar1 "ababa")


module Grammar2Tests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Properties =

        [<Property>]
        let ``accepts same strings as grammar 1`` (s: string) =
            Cyk.parse grammar1 s = Cyk.parse grammar2 s


module Grammar3Tests =

    [<Fact>]
    let ``accepts a`` () = Assert.True(Cyk.parse grammar3 "a")

    [<Fact>]
    let ``accepts aa`` () = Assert.True(Cyk.parse grammar3 "aa")

    [<Fact>]
    let ``accepts aaaa`` () = Assert.True(Cyk.parse grammar3 "aaaa")

    [<Fact>]
    let ``accepts aaaaa`` () = Assert.True(Cyk.parse grammar3 "aaaaa")

    [<Fact>]
    let ``rejects empty string`` () = Assert.False(Cyk.parse grammar3 "")

    [<Fact>]
    let ``rejects b`` () = Assert.False(Cyk.parse grammar3 "b")


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
        let trace = Cyk.parseWithTrace grammar3 "aaa"
        Assert.NotEmpty(trace)

    [<Fact>]
    let ``tableToTeX contains pNiceMatrix`` () =
        let trace = Cyk.parseWithTrace grammar1 "ab"
        let tex = Cyk.tableToTeX (fun s -> string s) trace.[0]
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.Contains(@"\end{pNiceMatrix}", tex)

    [<Fact>]
    let ``tableToTeX prints empty cells as cdot`` () =
        let trace = Cyk.parseWithTrace grammar3 "aa"

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

        Assert.True(Cyk.parse g "aaa")
        Assert.True(Cyk.parse g "aaaaa")
        Assert.True(Cyk.parse g "aaaa")
        Assert.True(Cyk.parse g "a")
        Assert.True(Cyk.parse g "aa")
