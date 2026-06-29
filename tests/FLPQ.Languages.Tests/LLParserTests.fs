module LLParserTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars


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
            let result = LLParser.parse grammar1 table 1 (Tokenizer.tokenize s)
            Assert.True(result.IsSome, s)

    [<Fact>]
    let ``LL(1) parser rejects grammar1 reject strings`` () =
        let table = LLParser.buildTable grammar1 1

        for s in grammar1Reject do
            let result = LLParser.parse grammar1 table 1 (Tokenizer.tokenize s)
            Assert.True(result.IsNone, s)

    [<Fact>]
    let ``LL(1) parser leaves match input string`` () =
        let table = LLParser.buildTable grammar1 1

        for s in grammar1Accept do
            match LLParser.parse grammar1 table 1 (Tokenizer.tokenize s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                Assert.Equal(s, leafTokens)
            | None -> Assert.Fail($"Failed to parse: {s}")

    [<Fact>]
    let ``LL(1) tree structure for simple parse`` () =
        let table = LLParser.buildTable grammar1 1

        match LLParser.parse grammar1 table 1 (Tokenizer.tokenize "a b") with
        | Some tree ->
            match tree with
            | Node(Nonterminal "S", _) -> ()
            | _ -> Assert.Fail("Expected root S")
        | None -> Assert.Fail("Failed to parse a b")

    [<Fact>]
    let ``LL(1) table for grammar8 expression grammar detects conflict`` () =
        Assert.Throws<System.Exception>(fun () -> LLParser.buildTable grammar8 1 |> ignore)


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        let private table = LLParser.buildTable grammar1 1

        [<Property>]
        let ``LL parser leaves match input for grammar1`` (s: string) =
            match LLParser.parse grammar1 table 1 (Tokenizer.tokenize s) with
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
                LLParser.parse grammar1 llTable 1 (Tokenizer.tokenize s) |> Option.isSome

            let slrResult =
                LRParser.parse augGrammar1 slrTable (Tokenizer.tokenize s) |> Option.isSome

            llResult = slrResult

        [<Property>]
        let ``LL(1) and Valiant agree on grammar1 acceptance`` (s: string) =
            let llResult =
                LLParser.parse grammar1 llTable 1 (Tokenizer.tokenize s) |> Option.isSome

            let valResult = Valiant.parse grammar1 (Tokenizer.tokenizeStrings s)
            llResult = valResult
