module CykTests

open Xunit
open FLPQ.Core

module Grammar1Tests =

    let grammarText =
        "
        S -> a S b S
        S -> eps
        "

    let g = Grammar.parseGrammar grammarText

    [<Fact>]
    let ``accepts abab`` () = Assert.True(Cyk.parse g "abab")

    [<Fact>]
    let ``accepts ab`` () = Assert.True(Cyk.parse g "ab")

    [<Fact>]
    let ``accepts empty string`` () = Assert.True(Cyk.parse g "")

    [<Fact>]
    let ``accepts aabb`` () = Assert.True(Cyk.parse g "aabb")

    [<Fact>]
    let ``accepts aababb`` () = Assert.True(Cyk.parse g "aababb")

    [<Fact>]
    let ``rejects aa`` () = Assert.False(Cyk.parse g "aa")

    [<Fact>]
    let ``rejects bb`` () = Assert.False(Cyk.parse g "bb")

    [<Fact>]
    let ``rejects abb`` () = Assert.False(Cyk.parse g "abb")

    [<Fact>]
    let ``rejects abba`` () = Assert.False(Cyk.parse g "abba")

    [<Fact>]
    let ``rejects b`` () = Assert.False(Cyk.parse g "b")

    [<Fact>]
    let ``rejects a`` () = Assert.False(Cyk.parse g "a")

    [<Fact>]
    let ``rejects ababa`` () = Assert.False(Cyk.parse g "ababa")


module Grammar2Tests =

    let grammarText =
        "
        S -> a S b
        S -> eps
        S -> S S
        "

    let g = Grammar.parseGrammar grammarText

    let testStrings =
        [ ""
          "ab"
          "abab"
          "aabb"
          "aababb"
          "aa"
          "bb"
          "abb"
          "abba"
          "b"
          "a"
          "ababa" ]

    [<Fact>]
    let ``accepts same strings as grammar 1`` () =
        let g1 = Grammar1Tests.g

        for s in testStrings do
            Assert.Equal(Cyk.parse g1 s, Cyk.parse g s)


module Grammar3Tests =

    let grammarText =
        "
        S -> a S
        S -> a
        "

    let g = Grammar.parseGrammar grammarText

    [<Fact>]
    let ``accepts a`` () = Assert.True(Cyk.parse g "a")

    [<Fact>]
    let ``accepts aa`` () = Assert.True(Cyk.parse g "aa")

    [<Fact>]
    let ``accepts aaaa`` () = Assert.True(Cyk.parse g "aaaa")

    [<Fact>]
    let ``accepts aaaaa`` () = Assert.True(Cyk.parse g "aaaaa")

    [<Fact>]
    let ``rejects empty string`` () = Assert.False(Cyk.parse g "")

    [<Fact>]
    let ``rejects b`` () = Assert.False(Cyk.parse g "b")


module Grammar4Tests =

    let grammarText =
        "
        S -> S a
        S -> a
        "

    let g = Grammar.parseGrammar grammarText

    let testStrings = [ ""; "a"; "aa"; "aaa"; "aaaa"; "aaaaa"; "b"; "ab" ]

    [<Fact>]
    let ``accepts same strings as grammar 3`` () =
        let g3 = Grammar3Tests.g

        for s in testStrings do
            Assert.Equal(Cyk.parse g3 s, Cyk.parse g s)


module Grammar5Tests =

    let grammarText =
        "
        S -> S S
        S -> S S S
        S -> a
        "

    let g = Grammar.parseGrammar grammarText

    let testStrings = [ ""; "a"; "aa"; "aaa"; "aaaa"; "aaaaa"; "b"; "ab" ]

    [<Fact>]
    let ``accepts same strings as grammar 3`` () =
        let g3 = Grammar3Tests.g

        for s in testStrings do
            Assert.Equal(Cyk.parse g3 s, Cyk.parse g s)


module FactTests =

    [<Fact>]
    let ``parseWithTrace returns non-empty list for non-empty input`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let trace = Cyk.parseWithTrace g "aaa"
        Assert.NotEmpty(trace)

    [<Fact>]
    let ``tableToTeX contains pNiceMatrix`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S b S
        S -> eps
        "

        let trace = Cyk.parseWithTrace g "ab"
        let tex = Cyk.tableToTeX (fun s -> string s) trace.[0]
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.Contains(@"\end{pNiceMatrix}", tex)

    [<Fact>]
    let ``tableToTeX prints empty cells as cdot`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let trace = Cyk.parseWithTrace g "aa"

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
