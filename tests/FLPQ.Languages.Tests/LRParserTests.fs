module LRParserTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars

module FactTests =

    module Grammar1 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar1 strings`` () =
            let table = LRParser.buildSLR1Table augGrammar1

            for s in grammar1Accept do
                Assert.True(LRParser.parse augGrammar1 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in grammar1Reject do
                Assert.True(LRParser.parse augGrammar1 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar1`` () =
            let table = LRParser.buildSLR1Table augGrammar1

            for s in grammar1Accept do
                match LRParser.parse augGrammar1 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar1 strings`` () =
            let table = LRParser.buildCLR1Table augGrammar1

            for s in grammar1Accept do
                Assert.True(LRParser.parse augGrammar1 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in grammar1Reject do
                Assert.True(LRParser.parse augGrammar1 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar1`` () =
            let table = LRParser.buildCLR1Table augGrammar1

            for s in grammar1Accept do
                match LRParser.parse augGrammar1 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

    module Grammar2 =

        [<Fact>]
        let ``grammar2 is not LR(k) — has conflicts in all table types`` () =
            let lr0 = LRParser.buildLR0Table augGrammar2
            let slr1 = LRParser.buildSLR1Table augGrammar2
            let clr1 = LRParser.buildCLR1Table augGrammar2

            Assert.NotEmpty(lr0.conflicts)
            Assert.NotEmpty(slr1.conflicts)
            Assert.NotEmpty(clr1.conflicts)

        [<Fact>]
        let ``SLR(1) table can be built for grammar2`` () =
            let table = LRParser.buildSLR1Table augGrammar2
            Assert.NotNull(table.action)
            Assert.NotNull(table.goto)

        [<Fact>]
        let ``CLR(1) table can be built for grammar2`` () =
            let table = LRParser.buildCLR1Table augGrammar2
            Assert.NotNull(table.action)
            Assert.NotNull(table.goto)

    module Grammar3 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar3 strings`` () =
            let table = LRParser.buildSLR1Table augGrammar3

            for s in grammar3Accept do
                Assert.True(LRParser.parse augGrammar3 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in grammar3Reject do
                Assert.True(LRParser.parse augGrammar3 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar3`` () =
            let table = LRParser.buildSLR1Table augGrammar3

            for s in grammar3Accept do
                match LRParser.parse augGrammar3 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar3 strings`` () =
            let table = LRParser.buildCLR1Table augGrammar3

            for s in grammar3Accept do
                Assert.True(LRParser.parse augGrammar3 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in grammar3Reject do
                Assert.True(LRParser.parse augGrammar3 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar3`` () =
            let table = LRParser.buildCLR1Table augGrammar3

            for s in grammar3Accept do
                match LRParser.parse augGrammar3 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``SLR(1) table has no conflicts for grammar3`` () =
            let table = LRParser.buildSLR1Table augGrammar3
            Assert.Empty(table.conflicts)

        [<Fact>]
        let ``CLR(1) table has no conflicts for grammar3`` () =
            let table = LRParser.buildCLR1Table augGrammar3
            Assert.Empty(table.conflicts)

        [<Fact>]
        let ``LR(0) table has conflicts for grammar3`` () =
            let table = LRParser.buildLR0Table augGrammar3
            Assert.NotEmpty(table.conflicts)

    module Grammar7 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar7 strings`` () =
            let table = LRParser.buildSLR1Table augGrammar7

            for s in exprAccept do
                Assert.True(LRParser.parse augGrammar7 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in exprReject do
                Assert.True(LRParser.parse augGrammar7 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar7`` () =
            let table = LRParser.buildSLR1Table augGrammar7

            for s in exprAccept do
                match LRParser.parse augGrammar7 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar7 strings`` () =
            let table = LRParser.buildCLR1Table augGrammar7

            for s in exprAccept do
                Assert.True(LRParser.parse augGrammar7 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in exprReject do
                Assert.True(LRParser.parse augGrammar7 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar7`` () =
            let table = LRParser.buildCLR1Table augGrammar7

            for s in exprAccept do
                match LRParser.parse augGrammar7 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``SLR(1) table has no conflicts for grammar7`` () =
            let table = LRParser.buildSLR1Table augGrammar7
            Assert.Empty(table.conflicts)

        [<Fact>]
        let ``CLR(1) table has no conflicts for grammar7`` () =
            let table = LRParser.buildCLR1Table augGrammar7
            Assert.Empty(table.conflicts)

    module Grammar8 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar8 strings`` () =
            let table = LRParser.buildSLR1Table augGrammar8

            for s in exprAccept do
                Assert.True(LRParser.parse augGrammar8 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in exprReject do
                Assert.True(LRParser.parse augGrammar8 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar8`` () =
            let table = LRParser.buildSLR1Table augGrammar8

            for s in exprAccept do
                match LRParser.parse augGrammar8 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar8 strings`` () =
            let table = LRParser.buildCLR1Table augGrammar8

            for s in exprAccept do
                Assert.True(LRParser.parse augGrammar8 table (Tokenizer.tokenize s) |> Option.isSome, s)

            for s in exprReject do
                Assert.True(LRParser.parse augGrammar8 table (Tokenizer.tokenize s) |> Option.isNone, s)

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar8`` () =
            let table = LRParser.buildCLR1Table augGrammar8

            for s in exprAccept do
                match LRParser.parse augGrammar8 table (Tokenizer.tokenize s) with
                | Some tree ->
                    let leafTokens = DerivationTree.leaves tree |> String.concat " "
                    Assert.Equal(s, leafTokens)
                | None -> Assert.Fail($"Failed to parse: {s}")

        [<Fact>]
        let ``SLR(1) table has no conflicts for grammar8`` () =
            let table = LRParser.buildSLR1Table augGrammar8
            Assert.Empty(table.conflicts)

        [<Fact>]
        let ``CLR(1) table has no conflicts for grammar8`` () =
            let table = LRParser.buildCLR1Table augGrammar8
            Assert.Empty(table.conflicts)

    module Grammar6 =

        [<Fact>]
        let ``ambiguous grammar6 has conflicts in all table types`` () =
            let lr0 = LRParser.buildLR0Table augGrammar6
            let slr1 = LRParser.buildSLR1Table augGrammar6
            let clr1 = LRParser.buildCLR1Table augGrammar6

            Assert.NotEmpty(lr0.conflicts)
            Assert.NotEmpty(slr1.conflicts)
            Assert.NotEmpty(clr1.conflicts)

    module AutomatonTests =

        [<Fact>]
        let ``LR(0) automaton for grammar3 has expected structure`` () =
            let aut = LRAutomaton.buildLR0 augGrammar3
            Assert.True(aut.states.Length > 1)
            Assert.Equal(0, aut.startState)
            Assert.Equal(1, aut.finalStates.Count)

        [<Fact>]
        let ``LR(1) automaton for grammar3 has expected structure`` () =
            let aut = LRAutomaton.buildLR1 augGrammar3
            Assert.True(aut.states.Length > 1)
            Assert.Equal(0, aut.startState)
            Assert.Equal(1, aut.finalStates.Count)

        [<Fact>]
        let ``LR(0) automaton for grammar7 has expected structure`` () =
            let aut = LRAutomaton.buildLR0 augGrammar7
            Assert.True(aut.states.Length > 1)
            Assert.Equal(0, aut.startState)
            Assert.Equal(1, aut.finalStates.Count)

        [<Fact>]
        let ``LR(1) automaton for grammar8 has expected structure`` () =
            let aut = LRAutomaton.buildLR1 augGrammar8
            Assert.True(aut.states.Length > 1)
            Assert.Equal(0, aut.startState)
            Assert.Equal(1, aut.finalStates.Count)

    module CrossParserTests =

        [<Fact>]
        let ``SLR(1) and CLR(1) agree on grammar3 acceptance`` () =
            let slr = LRParser.buildSLR1Table augGrammar3
            let clr = LRParser.buildCLR1Table augGrammar3

            for s in grammar3Accept @ grammar3Reject do
                let slrResult =
                    LRParser.parse augGrammar3 slr (Tokenizer.tokenize s) |> Option.isSome

                let clrResult =
                    LRParser.parse augGrammar3 clr (Tokenizer.tokenize s) |> Option.isSome

                Assert.Equal(slrResult, clrResult)

        [<Fact>]
        let ``SLR(1) and CLR(1) agree on grammar7 acceptance`` () =
            let slr = LRParser.buildSLR1Table augGrammar7
            let clr = LRParser.buildCLR1Table augGrammar7

            for s in exprAccept @ exprReject do
                let slrResult =
                    LRParser.parse augGrammar7 slr (Tokenizer.tokenize s) |> Option.isSome

                let clrResult =
                    LRParser.parse augGrammar7 clr (Tokenizer.tokenize s) |> Option.isSome

                Assert.Equal(slrResult, clrResult)

        [<Fact>]
        let ``SLR(1) and CLR(1) agree on grammar8 acceptance`` () =
            let slr = LRParser.buildSLR1Table augGrammar8
            let clr = LRParser.buildCLR1Table augGrammar8

            for s in exprAccept @ exprReject do
                let slrResult =
                    LRParser.parse augGrammar8 slr (Tokenizer.tokenize s) |> Option.isSome

                let clrResult =
                    LRParser.parse augGrammar8 clr (Tokenizer.tokenize s) |> Option.isSome

                Assert.Equal(slrResult, clrResult)

        [<Fact>]
        let ``grammar7 and grammar8 CLR(1) agree on expr strings`` () =
            let t7 = LRParser.buildCLR1Table augGrammar7
            let t8 = LRParser.buildCLR1Table augGrammar8

            for s in exprAccept @ exprReject do
                let r7 = LRParser.parse augGrammar7 t7 (Tokenizer.tokenize s) |> Option.isSome
                let r8 = LRParser.parse augGrammar8 t8 (Tokenizer.tokenize s) |> Option.isSome
                Assert.Equal(r7, r8)

module PropertyTests =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Grammar3PropertyTests =

        let private slrTable = LRParser.buildSLR1Table augGrammar3
        let private clrTable = LRParser.buildCLR1Table augGrammar3

        [<Property>]
        let ``SLR(1) parser leaves match input for grammar3`` (s: string) =
            match LRParser.parse augGrammar3 slrTable (Tokenizer.tokenize s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

        [<Property>]
        let ``CLR(1) parser leaves match input for grammar3`` (s: string) =
            match LRParser.parse augGrammar3 clrTable (Tokenizer.tokenize s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

        [<Property>]
        let ``SLR(1) and CLR(1) agree on grammar3`` (s: string) =
            let slrResult =
                LRParser.parse augGrammar3 slrTable (Tokenizer.tokenize s) |> Option.isSome

            let clrResult =
                LRParser.parse augGrammar3 clrTable (Tokenizer.tokenize s) |> Option.isSome

            slrResult = clrResult

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        let private slrTable = LRParser.buildSLR1Table augGrammar1
        let private clrTable = LRParser.buildCLR1Table augGrammar1

        [<Property>]
        let ``SLR(1) parser leaves match input for grammar1`` (s: string) =
            match LRParser.parse augGrammar1 slrTable (Tokenizer.tokenize s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

        [<Property>]
        let ``CLR(1) parser leaves match input for grammar1`` (s: string) =
            match LRParser.parse augGrammar1 clrTable (Tokenizer.tokenize s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

        [<Property>]
        let ``SLR(1) and CLR(1) agree on grammar1`` (s: string) =
            let slrResult =
                LRParser.parse augGrammar1 slrTable (Tokenizer.tokenize s) |> Option.isSome

            let clrResult =
                LRParser.parse augGrammar1 clrTable (Tokenizer.tokenize s) |> Option.isSome

            slrResult = clrResult


module CrossParserPropertyTests =

    let private slrGrammar1 = LRParser.buildSLR1Table augGrammar1
    let private clrGrammar3 = LRParser.buildCLR1Table augGrammar3

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1CrossTests =

        [<Property>]
        let ``SLR(1) and CYK agree on grammar1 acceptance`` (s: string) =
            let slrResult =
                LRParser.parse augGrammar1 slrGrammar1 (Tokenizer.tokenize s) |> Option.isSome

            let cykResult = Cyk.parse grammar1 (Tokenizer.tokenize s)
            slrResult = cykResult

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Grammar3CrossTests =

        [<Property>]
        let ``CLR(1) and CYK agree on grammar3 acceptance`` (s: string) =
            let clrResult =
                LRParser.parse augGrammar3 clrGrammar3 (Tokenizer.tokenize s) |> Option.isSome

            let cykResult = Cyk.parse grammar3 (Tokenizer.tokenize s)
            clrResult = cykResult

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module Grammar78CrossTests =

        let private clr7 = LRParser.buildCLR1Table augGrammar7
        let private clr8 = LRParser.buildCLR1Table augGrammar8

        [<Property>]
        let ``CLR(1) grammar7 and grammar8 agree on expression strings`` (s: string) =
            let r7 = LRParser.parse augGrammar7 clr7 (Tokenizer.tokenize s) |> Option.isSome
            let r8 = LRParser.parse augGrammar8 clr8 (Tokenizer.tokenize s) |> Option.isSome
            r7 = r8
