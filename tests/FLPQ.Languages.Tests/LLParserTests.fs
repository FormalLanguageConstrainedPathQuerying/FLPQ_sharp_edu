module LLParserTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars
open FLPQ.TestUtilities


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

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
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
    let private slrTable = LRParser.buildSLR1Table augGrammar1

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
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

    let private ll2Grammar =
        Grammar.parseGrammar
            "
        S -> a b A
        S -> a a B
        A -> c
        B -> d
        "

    let private ll2Accept = [ "a b c"; "a a d" ]

    let private ll2Reject = [ ""; "a"; "a b"; "a a"; "a b d"; "a a c"; "a a" ]

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

    let private ll3Grammar =
        Grammar.parseGrammar
            "
        S -> a b c A
        S -> a b d B
        A -> x
        B -> y
        "

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

        let accept = [ "a b c x"; "a b d y" ]

        for s in accept do
            let result = LLParser.parse ll3Grammar table 3 (Tokenizer.tokenizeTerminals s)
            Assert.True(result.IsSome, s)

    [<Fact>]
    let ``LL(3) parser rejects incorrect strings`` () =
        let table = LLParser.buildTable ll3Grammar 3

        let reject = [ ""; "a"; "a b"; "a b c"; "a b d"; "a b c y"; "a b d x" ]

        for s in reject do
            let result = LLParser.parse ll3Grammar table 3 (Tokenizer.tokenizeTerminals s)
            Assert.True(result.IsNone, s)
