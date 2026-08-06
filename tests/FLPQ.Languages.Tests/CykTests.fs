module CykTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.GraphAnalysis
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

let private g (lang: Language) (name: string) =
    lang.Grammars |> List.find (fun g -> g.Name = name)

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private expr = LanguageRegistry.ArithExpr
let private twoTrack = LanguageRegistry.TwoTrackDyck

let private grammar1 = (g dyck1 "ambiguousEps").Grammar
let private grammar2 = (g dyck1 "ambiguousWithConcat").Grammar
let private grammar3 = (g aplus "rightRecursive").Grammar
let private grammar4 = (g aplus "leftRecursive").Grammar
let private grammar5 = (g aplus "ambiguousBinaryTernary").Grammar
let private grammar6 = (g expr "ambiguous").Grammar
let private grammar7 = (g expr "leftAssoc").Grammar
let private grammar8 = (g expr "rightAssoc").Grammar
let private grammar9 = (g twoTrack "variantA").Grammar
let private grammar10 = (g twoTrack "variantB").Grammar

module Grammar1Tests =

    [<Fact>]
    let ``CYK accepts expected strings`` () =
        let lang = LanguageRegistry.Dyck1

        let failures =
            TestHelpers.collectAcceptFailures (fun g input -> Cyk.parse Grammar.freshStringNonterminal g input) lang

        Assert.Empty(failures)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        let lang = LanguageRegistry.Dyck1

        let failures =
            TestHelpers.collectRejectFailures (fun g input -> Cyk.parse Grammar.freshStringNonterminal g input) lang

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
            TestHelpers.collectAcceptFailures (fun g input -> Cyk.parse Grammar.freshStringNonterminal g input) lang

        Assert.Empty(failures)

    [<Fact>]
    let ``CYK rejects expected strings`` () =
        let lang = LanguageRegistry.APlus

        let failures =
            TestHelpers.collectRejectFailures (fun g input -> Cyk.parse Grammar.freshStringNonterminal g input) lang

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
                Cyk.parse Grammar.freshStringNonterminal g s,
                $"""{String.concat " " (s |> List.map (fun (Terminal x) -> x))}"""
            )


module Grammar6Tests =

    let grammars = [ grammar6; grammar7; grammar8 ]

    [<Fact>]
    let ``CYK accepts expected expression strings`` () =
        let failures =
            TestHelpers.collectAcceptFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g input)
                LanguageRegistry.ArithExpr

        Assert.Empty(failures)

    [<Fact>]
    let ``CYK rejects expected expression strings`` () =
        let failures =
            TestHelpers.collectRejectFailures
                (fun g input -> Cyk.parse Grammar.freshStringNonterminal g input)
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


module CykSppfTests =

    [<Fact>]
    let ``enriched table for aplus grammar with input 'a' has correct entry`` () =
        let table =
            Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar3 [ Terminal "a" ]

        let entries = table.[0, 0]
        Assert.False(Set.isEmpty entries, "Cell (0,0) should have entries")

        let startEntry = entries |> Set.filter (fun entry -> entry.Nt = grammar3.Start)

        Assert.NotEmpty startEntry
        Assert.True(Set.exists (fun entry -> entry.Nt = grammar3.Start) startEntry)

        for entry in startEntry do
            Assert.Equal(0, entry.SplitPoint)

            let rule =
                (Grammar.toCnf Grammar.freshStringNonterminal grammar3).Rules.[entry.ProdIdx]

            let isTerminalRule =
                match rule.Rhs with
                | Symbols nel when NonEmptyList.length nel = 1 ->
                    match NonEmptyList.head nel with
                    | Symbol.T(Terminal t) -> t = "a"
                    | _ -> false
                | _ -> false

            Assert.True(isTerminalRule, sprintf "Rule at index %d should be a terminal rule for 'a'" entry.ProdIdx)

    [<Fact>]
    let ``enriched table for dyck grammar with input 'ab' has correct structure`` () =
        let table =
            Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 [ Terminal "a"; Terminal "b" ]

        Assert.False(Set.isEmpty table.[0, 0], "Cell (0,0) should have entries")
        Assert.False(Set.isEmpty table.[1, 1], "Cell (1,1) should have entries")
        Assert.False(Set.isEmpty table.[0, 1], "Cell (0,1) should have entries")

        Assert.True(table.[0, 0] |> Set.exists (fun e -> e.SplitPoint = 0), "Terminal entries at (0,0) should have k=0")

        Assert.True(table.[1, 1] |> Set.exists (fun e -> e.SplitPoint = 1), "Terminal entries at (1,1) should have k=1")

        Assert.True(
            table.[0, 1] |> Set.exists (fun e -> e.SplitPoint = 0),
            "Binary entries at (0,1) should have split point k=0"
        )

    [<Fact>]
    let ``parseWithSppfTable returns true for accepted input 'ab'`` () =
        let _, accepted =
            Cyk.parseWithSppfTable Grammar.freshStringNonterminal grammar1 [ Terminal "a"; Terminal "b" ]

        Assert.True(accepted)

    [<Fact>]
    let ``parseWithSppfTable returns false for rejected input 'a'`` () =
        let _, accepted =
            Cyk.parseWithSppfTable Grammar.freshStringNonterminal grammar1 [ Terminal "a" ]

        Assert.False(accepted)

    [<Fact>]
    let ``enriched table equals parsing table nonterminals for same input`` () =
        let terminals = [ Terminal "a"; Terminal "b" ]
        let table = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 terminals

        let parseTable, _ =
            Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 terminals

        let n = Matrix.rows table

        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                let sppfNonterminals = table.[i, j] |> Set.map (fun e -> e.Nt)

                let parseNonterminals = parseTable.[i, j]
                let bothEmpty = Set.isEmpty sppfNonterminals && Set.isEmpty parseNonterminals
                let sameNonterminals = sppfNonterminals = parseNonterminals

                Assert.True(
                    bothEmpty || sameNonterminals,
                    sprintf "Cell (%d,%d): SPPF has %A, parse has %A" i j sppfNonterminals parseNonterminals
                )

    [<Fact>]
    let ``parseWithSppfTable handles empty input`` () =
        let _, accepted = Cyk.parseWithSppfTable Grammar.freshStringNonterminal grammar3 []

        Assert.False(accepted)

        let _, acceptedEps =
            Cyk.parseWithSppfTable Grammar.freshStringNonterminal (LanguageRegistry.EpsilonOnly.Grammars[0].Grammar) []

        Assert.True(acceptedEps)

    [<Fact>]
    let ``fromParsingTable builds SPPF with correct root`` () =
        let table =
            Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 [ Terminal "a"; Terminal "b" ]

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1
        let sppf = BasicSppf.fromParsingTable cnf table

        let rootInfo = Graph.getVertex sppf.RootIndex sppf.Graph

        match rootInfo with
        | BasicSppf.BasicSppfNodeInfo.Nonterminal(nt, 0, 2) -> Assert.Equal(cnf.Start, nt)
        | _ -> Assert.True(false, "Root should be Nonterminal(start, 0, 2)")

    [<Fact>]
    let ``fromParsingTable SPPF tree leaves match input`` () =
        let input = [ Terminal "a"; Terminal "b" ]
        let table = Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar1 input

        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar1
        let sppf = BasicSppf.fromParsingTable cnf table

        let tree = BasicSppf.extractDerivationTree sppf
        let leaves = DerivationTree.leaves tree
        Assert.Equal<string>([ "a"; "b" ], leaves)
