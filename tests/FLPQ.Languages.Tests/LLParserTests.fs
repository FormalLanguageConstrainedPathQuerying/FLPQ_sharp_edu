module LLParserTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open FLPQ.TestUtilities

let private g (lang: Language) (name: string) = lang.Grammars |> List.find (fun g -> g.Name = name)
let private acceptStrToSpace (ss: (string list) list) = ss |> List.map (String.concat " ")

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private expr = LanguageRegistry.ArithExpr
let private twoTrack = LanguageRegistry.TwoTrackDyck
let private ll2Test = LanguageRegistry.LL2Test
let private ll3Test = LanguageRegistry.LL3Test

let private grammar1 = (g dyck1 "grammar1").Grammar
let private augGrammar1 = (g dyck1 "grammar1").AugmentedGrammar
let private grammar1Accept = dyck1.AcceptStrings |> acceptStrToSpace
let private grammar1Reject = dyck1.RejectStrings |> acceptStrToSpace

let private grammar3 = (g aplus "grammar3").Grammar
let private grammar8 = (g expr "grammar8").Grammar

let private grammar9 = (g twoTrack "grammar9").Grammar
let private grammar9Accept = twoTrack.AcceptStrings |> acceptStrToSpace
let private grammar9Reject = twoTrack.RejectStrings |> acceptStrToSpace

let private grammar10 = (g twoTrack "grammar10").Grammar
let private grammar10Accept = grammar9Accept
let private grammar10Reject = grammar9Reject

let private ll2Grammar = (g ll2Test "ll2Grammar").Grammar
let private ll3Grammar = (g ll3Test "ll3Grammar").Grammar


module FactTests =

    [<Fact>]
    let ``LL(1) table for grammar1 has no conflicts`` () =
        let table = LLParser.buildTable grammar1 1
        Assert.True(Map.count table > 0)

    [<Fact>]
    let ``LL(1) table for grammar3 detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar3 1 |> ignore)

    [<Fact>]
    let ``LL(1) parser accepts grammar1 strings`` () =
        let table = LLParser.buildTable grammar1 1

        for s in grammar1Accept do
            let result = LLParser.parse grammar1 table 1 (Tokenizer.tokenizeTerminals s)
            Assert.True(result.IsSome, s)

    [<Fact>]
    let ``LL(1) parser rejects grammar1 reject strings`` () =
        let table = LLParser.buildTable grammar1 1

        for s in grammar1Reject do
            let result = LLParser.parse grammar1 table 1 (Tokenizer.tokenizeTerminals s)
            Assert.True(result.IsNone, s)

    [<Fact>]
    let ``LL(1) parser leaves match input string`` () =
        let table = LLParser.buildTable grammar1 1

        for s in grammar1Accept do
            match LLParser.parse grammar1 table 1 (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                Assert.Equal(s, leafTokens)
            | None -> Assert.Fail($"Failed to parse: {s}")

    [<Fact>]
    let ``LL(1) tree structure for simple parse`` () =
        let table = LLParser.buildTable grammar1 1

        match LLParser.parse grammar1 table 1 (Tokenizer.tokenizeTerminals "a b") with
        | Some tree ->
            match tree with
            | Node(Nonterminal "S", children) -> Assert.Equal(4, children.Length)
            | _ -> Assert.Fail("Expected root S with children")
        | None -> Assert.Fail("Failed to parse a b")

    [<Fact>]
    let ``LL(1) tree is properly nested with intermediate nonterminals`` () =
        let table = LLParser.buildTable grammar1 1

        match LLParser.parse grammar1 table 1 (Tokenizer.tokenizeTerminals "a b") with
        | Some tree ->
            match tree with
            | Node(Nonterminal "S",
                   [ Leaf(Symbol.T(Terminal "a"))
                     Node(Nonterminal "S", [ Leaf(Symbol.Epsilon) ])
                     Leaf(Symbol.T(Terminal "b"))
                     Node(Nonterminal "S", [ Leaf(Symbol.Epsilon) ]) ]) -> ()
            | _ -> Assert.Fail(sprintf "Unexpected tree structure: %A" tree)
        | None -> Assert.Fail("Failed to parse a b")

    [<Fact>]
    let ``LL(1) table for grammar8 expression grammar detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar8 1 |> ignore)

    [<Fact>]
    let ``LL(1) table for grammar9 detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar9 1 |> ignore)

    [<Fact>]
    let ``LL(2) table for grammar9 also detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar9 2 |> ignore)

    [<Fact>]
    let ``Valiant and CYK agree on grammar9 acceptance`` () =
        for s in grammar9Accept do
            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal grammar9 (Tokenizer.tokenizeTerminals s)

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal grammar9 (Tokenizer.tokenizeTerminals s)

            if cykResult <> valResult then
                Assert.True(false, userMessage = s)

        for s in grammar9Reject do
            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal grammar9 (Tokenizer.tokenizeTerminals s)

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal grammar9 (Tokenizer.tokenizeTerminals s)

            if cykResult <> valResult then
                Assert.True(false, userMessage = s)

    [<Fact>]
    let ``Valiant parseWithTable for grammar9 returns correct dimension`` () =
        let input = "a b c"

        let table, accepted =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar9 (Tokenizer.tokenizeTerminals input)

        Assert.True(accepted)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.rows table)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.cols table)

    [<Fact>]
    let ``LL(1) table for grammar10 detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar10 1 |> ignore)

    [<Fact>]
    let ``LL(2) table for grammar10 also detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar10 2 |> ignore)

    [<Fact>]
    let ``Valiant and CYK agree on grammar10 acceptance`` () =
        for s in grammar10Accept do
            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal grammar10 (Tokenizer.tokenizeTerminals s)

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal grammar10 (Tokenizer.tokenizeTerminals s)

            if cykResult <> valResult then
                Assert.True(false, userMessage = s)

        for s in grammar10Reject do
            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal grammar10 (Tokenizer.tokenizeTerminals s)

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal grammar10 (Tokenizer.tokenizeTerminals s)

            if cykResult <> valResult then
                Assert.True(false, userMessage = s)

    [<Fact>]
    let ``Valiant parseWithTable for grammar10 returns correct dimension`` () =
        let input = "a b c"

        let table, accepted =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar10 (Tokenizer.tokenizeTerminals input)

        Assert.True(accepted)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.rows table)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.cols table)


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Grammar1PropertyTests =

        let private table = LLParser.buildTable grammar1 1

        [<Property>]
        let ``LL parser leaves match input for grammar1`` (s: string) =
            match LLParser.parse grammar1 table 1 (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true


module CrossParserPropertyTests =

    let private llTable = LLParser.buildTable grammar1 1
    let private slrTable = LRParser.buildSLR1Table augGrammar1 Grammar.eoiSymbol

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Grammar1Agreement =

        [<Property>]
        let ``LL(1) and SLR(1) agree on grammar1 acceptance`` (s: string) =
            let llResult =
                LLParser.parse grammar1 llTable 1 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let slrResult =
                LRParser.parse augGrammar1 slrTable (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            llResult = slrResult

        [<Property>]
        let ``LL(1) and Valiant agree on grammar1 acceptance`` (s: string) =
            let llResult =
                LLParser.parse grammar1 llTable 1 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            llResult = valResult


module LLHigherKTests =

    let private ll2Grammar = ll2Grammar
    let private ll2Lang = LanguageRegistry.LL2Test

    let private ll2Accept = ll2Lang.AcceptStrings |> List.map (String.concat " ")

    let private ll2Reject = ll2Lang.RejectStrings |> List.map (String.concat " ")

    [<Fact>]
    let ``LL(k=1) detects conflict for grammar requiring k=2`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable ll2Grammar 1 |> ignore)

    [<Fact>]
    let ``LL(k=2) resolves conflict for grammar requiring k=2`` () =
        let table = LLParser.buildTable ll2Grammar 2
        Assert.True(Map.count table > 0)

    [<Fact>]
    let ``LL(2) parser accepts correct strings`` () =
        let table = LLParser.buildTable ll2Grammar 2

        for s in ll2Accept do
            let result = LLParser.parse ll2Grammar table 2 (Tokenizer.tokenizeTerminals s)
            Assert.True(result.IsSome, s)

    [<Fact>]
    let ``LL(2) parser rejects incorrect strings`` () =
        let table = LLParser.buildTable ll2Grammar 2

        for s in ll2Reject do
            let result = LLParser.parse ll2Grammar table 2 (Tokenizer.tokenizeTerminals s)
            Assert.True(result.IsNone, s)

    [<Fact>]
    let ``LL(2) leaves match input for k=2 grammar`` () =
        let table = LLParser.buildTable ll2Grammar 2

        for s in ll2Accept do
            match LLParser.parse ll2Grammar table 2 (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                Assert.Equal(s, leafTokens)
            | None -> Assert.Fail($"Failed to parse: {s}")

    let private ll3Grammar = ll3Grammar
    let private ll3Lang = LanguageRegistry.LL3Test

    [<Fact>]
    let ``LL(k=2) detects conflict for grammar requiring k=3`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable ll3Grammar 2 |> ignore)

    [<Fact>]
    let ``LL(k=3) resolves conflict for grammar requiring k=3`` () =
        let table = LLParser.buildTable ll3Grammar 3
        Assert.True(Map.count table > 0)

    [<Fact>]
    let ``LL(3) parser accepts correct strings`` () =
        let table = LLParser.buildTable ll3Grammar 3

        for s in ll3Lang.AcceptStrings do
            let result = LLParser.parse ll3Grammar table 3 (s |> List.map Terminal)
            Assert.True(result.IsSome, $"{s}")

    [<Fact>]
    let ``LL(3) parser rejects incorrect strings`` () =
        let table = LLParser.buildTable ll3Grammar 3

        for s in ll3Lang.RejectStrings do
            let result = LLParser.parse ll3Grammar table 3 (s |> List.map Terminal)
            Assert.True(result.IsNone, $"{s}")

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbcxdString> |])>]
    module LL2PropertyTests =

        let private ll2Table = LLParser.buildTable ll2Grammar 2

        [<Property>]
        let ``LL(2) and CYK agree on acceptance`` (s: string) =
            let llResult =
                LLParser.parse ll2Grammar ll2Table 2 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal ll2Grammar (Tokenizer.tokenizeTerminals s)

            llResult = cykResult

        [<Property>]
        let ``LL(2) and Valiant agree on acceptance`` (s: string) =
            let llResult =
                LLParser.parse ll2Grammar ll2Table 2 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal ll2Grammar (Tokenizer.tokenizeTerminals s)

            llResult = valResult

        [<Property>]
        let ``LL(2) leaves match input when accepted`` (s: string) =
            match LLParser.parse ll2Grammar ll2Table 2 (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbcdxyString> |])>]
    module LL3PropertyTests =

        let private ll3Table = LLParser.buildTable ll3Grammar 3

        [<Property>]
        let ``LL(3) and CYK agree on acceptance`` (s: string) =
            let llResult =
                LLParser.parse ll3Grammar ll3Table 3 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal ll3Grammar (Tokenizer.tokenizeTerminals s)

            llResult = cykResult

        [<Property>]
        let ``LL(3) and Valiant agree on acceptance`` (s: string) =
            let llResult =
                LLParser.parse ll3Grammar ll3Table 3 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let valResult =
                Valiant.parse Grammar.freshStringNonterminal ll3Grammar (Tokenizer.tokenizeTerminals s)

            llResult = valResult

        [<Property>]
        let ``LL(3) leaves match input when accepted`` (s: string) =
            match LLParser.parse ll3Grammar ll3Table 3 (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true
