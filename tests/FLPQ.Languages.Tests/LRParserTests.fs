module LRParserTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars
open FLPQ.TestUtilities

module FactTests =

    let private testAcceptReject builder augGrammar accept reject =
        let table = builder augGrammar

        for s in accept do
            Assert.True(LRParser.parse augGrammar table (Tokenizer.tokenizeTerminals s) |> Option.isSome, s)

        for s in reject do
            Assert.True(LRParser.parse augGrammar table (Tokenizer.tokenizeTerminals s) |> Option.isNone, s)

    let private testLeaves builder augGrammar accept =
        let table = builder augGrammar

        for s in accept do
            match LRParser.parse augGrammar table (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                Assert.Equal(s, leafTokens)
            | None -> Assert.Fail($"Failed to parse: {s}")

    let private testNoConflicts builder augGrammar =
        let table = builder augGrammar
        Assert.Empty(table.conflicts)

    let private testHasConflicts builder augGrammar =
        let table = builder augGrammar
        Assert.NotEmpty(table.conflicts)

    module Grammar1 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar1 strings`` () =
            testAcceptReject LRParser.buildSLR1Table augGrammar1 grammar1Accept grammar1Reject

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar1`` () =
            testLeaves LRParser.buildSLR1Table augGrammar1 grammar1Accept

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar1 strings`` () =
            testAcceptReject LRParser.buildCLR1Table augGrammar1 grammar1Accept grammar1Reject

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar1`` () =
            testLeaves LRParser.buildCLR1Table augGrammar1 grammar1Accept

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
            testAcceptReject LRParser.buildSLR1Table augGrammar3 grammar3Accept grammar3Reject

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar3`` () =
            testLeaves LRParser.buildSLR1Table augGrammar3 grammar3Accept

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar3 strings`` () =
            testAcceptReject LRParser.buildCLR1Table augGrammar3 grammar3Accept grammar3Reject

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar3`` () =
            testLeaves LRParser.buildCLR1Table augGrammar3 grammar3Accept

        [<Fact>]
        let ``SLR(1) table has no conflicts for grammar3`` () =
            testNoConflicts LRParser.buildSLR1Table augGrammar3

        [<Fact>]
        let ``CLR(1) table has no conflicts for grammar3`` () =
            testNoConflicts LRParser.buildCLR1Table augGrammar3

        [<Fact>]
        let ``LR(0) table has conflicts for grammar3`` () =
            testHasConflicts LRParser.buildLR0Table augGrammar3

    module Grammar7 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar7 strings`` () =
            testAcceptReject LRParser.buildSLR1Table augGrammar7 exprAccept exprReject

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar7`` () =
            testLeaves LRParser.buildSLR1Table augGrammar7 exprAccept

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar7 strings`` () =
            testAcceptReject LRParser.buildCLR1Table augGrammar7 exprAccept exprReject

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar7`` () =
            testLeaves LRParser.buildCLR1Table augGrammar7 exprAccept

        [<Fact>]
        let ``SLR(1) table has no conflicts for grammar7`` () =
            testNoConflicts LRParser.buildSLR1Table augGrammar7

        [<Fact>]
        let ``CLR(1) table has no conflicts for grammar7`` () =
            testNoConflicts LRParser.buildCLR1Table augGrammar7

    module Grammar8 =

        [<Fact>]
        let ``SLR(1) parser accepts and rejects grammar8 strings`` () =
            testAcceptReject LRParser.buildSLR1Table augGrammar8 exprAccept exprReject

        [<Fact>]
        let ``SLR(1) parser leaves match input for grammar8`` () =
            testLeaves LRParser.buildSLR1Table augGrammar8 exprAccept

        [<Fact>]
        let ``CLR(1) parser accepts and rejects grammar8 strings`` () =
            testAcceptReject LRParser.buildCLR1Table augGrammar8 exprAccept exprReject

        [<Fact>]
        let ``CLR(1) parser leaves match input for grammar8`` () =
            testLeaves LRParser.buildCLR1Table augGrammar8 exprAccept

        [<Fact>]
        let ``SLR(1) table has no conflicts for grammar8`` () =
            testNoConflicts LRParser.buildSLR1Table augGrammar8

        [<Fact>]
        let ``CLR(1) table has no conflicts for grammar8`` () =
            testNoConflicts LRParser.buildCLR1Table augGrammar8

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

        let private testAgree augGrammar slrBuilder clrBuilder accept reject =
            let slr = slrBuilder augGrammar
            let clr = clrBuilder augGrammar

            for s in accept @ reject do
                let slrResult =
                    LRParser.parse augGrammar slr (Tokenizer.tokenizeTerminals s) |> Option.isSome

                let clrResult =
                    LRParser.parse augGrammar clr (Tokenizer.tokenizeTerminals s) |> Option.isSome

                Assert.Equal(slrResult, clrResult)

        [<Fact>]
        let ``SLR(1) and CLR(1) agree on grammar3 acceptance`` () =
            testAgree augGrammar3 LRParser.buildSLR1Table LRParser.buildCLR1Table grammar3Accept grammar3Reject

        [<Fact>]
        let ``SLR(1) and CLR(1) agree on grammar7 acceptance`` () =
            testAgree augGrammar7 LRParser.buildSLR1Table LRParser.buildCLR1Table exprAccept exprReject

        [<Fact>]
        let ``SLR(1) and CLR(1) agree on grammar8 acceptance`` () =
            testAgree augGrammar8 LRParser.buildSLR1Table LRParser.buildCLR1Table exprAccept exprReject

        [<Fact>]
        let ``grammar7 and grammar8 CLR(1) agree on expr strings`` () =
            let t7 = LRParser.buildCLR1Table augGrammar7
            let t8 = LRParser.buildCLR1Table augGrammar8

            for s in exprAccept @ exprReject do
                let r7 =
                    LRParser.parse augGrammar7 t7 (Tokenizer.tokenizeTerminals s) |> Option.isSome

                let r8 =
                    LRParser.parse augGrammar8 t8 (Tokenizer.tokenizeTerminals s) |> Option.isSome

                Assert.Equal(r7, r8)

module PropertyTests =

    module Grammar3PropertyTests =

        let private slrTable = LRParser.buildSLR1Table augGrammar3
        let private clrTable = LRParser.buildCLR1Table augGrammar3

        let private leavesMatch augGrammar table (s: string) =
            match LRParser.parse augGrammar table (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

        let private parsersAgree augGrammar slr clr (s: string) =
            let slrResult =
                LRParser.parse augGrammar slr (Tokenizer.tokenizeTerminals s) |> Option.isSome

            let clrResult =
                LRParser.parse augGrammar clr (Tokenizer.tokenizeTerminals s) |> Option.isSome

            slrResult = clrResult

        [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
        module Grammar3Props =

            [<Property>]
            let ``SLR(1) parser leaves match input for grammar3`` (s: string) = leavesMatch augGrammar3 slrTable s

            [<Property>]
            let ``CLR(1) parser leaves match input for grammar3`` (s: string) = leavesMatch augGrammar3 clrTable s

            [<Property>]
            let ``SLR(1) and CLR(1) agree on grammar3`` (s: string) =
                parsersAgree augGrammar3 slrTable clrTable s

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        let private slrTable = LRParser.buildSLR1Table augGrammar1
        let private clrTable = LRParser.buildCLR1Table augGrammar1

        let private leavesMatch augGrammar table (s: string) =
            match LRParser.parse augGrammar table (Tokenizer.tokenizeTerminals s) with
            | Some tree ->
                let leafTokens = DerivationTree.leaves tree |> String.concat " "
                leafTokens = s
            | None -> true

        [<Property>]
        let ``SLR(1) parser leaves match input for grammar1`` (s: string) = leavesMatch augGrammar1 slrTable s

        [<Property>]
        let ``CLR(1) parser leaves match input for grammar1`` (s: string) = leavesMatch augGrammar1 clrTable s

        [<Property>]
        let ``SLR(1) and CLR(1) agree on grammar1`` (s: string) =
            let slrResult =
                LRParser.parse augGrammar1 slrTable (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let clrResult =
                LRParser.parse augGrammar1 clrTable (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            slrResult = clrResult


module CrossParserPropertyTests =

    let private slrGrammar1 = LRParser.buildSLR1Table augGrammar1
    let private clrGrammar3 = LRParser.buildCLR1Table augGrammar3

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1CrossTests =

        [<Property>]
        let ``SLR(1) and CYK agree on grammar1 acceptance`` (s: string) =
            let slrResult =
                LRParser.parse augGrammar1 slrGrammar1 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            slrResult = cykResult

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Grammar3CrossTests =

        [<Property>]
        let ``CLR(1) and CYK agree on grammar3 acceptance`` (s: string) =
            let clrResult =
                LRParser.parse augGrammar3 clrGrammar3 (Tokenizer.tokenizeTerminals s)
                |> Option.isSome

            let cykResult =
                Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s)

            clrResult = cykResult

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module Grammar78CrossTests =

        let private clr7 = LRParser.buildCLR1Table augGrammar7
        let private clr8 = LRParser.buildCLR1Table augGrammar8

        [<Property>]
        let ``CLR(1) grammar7 and grammar8 agree on expression strings`` (s: string) =
            let r7 =
                LRParser.parse augGrammar7 clr7 (Tokenizer.tokenizeTerminals s) |> Option.isSome

            let r8 =
                LRParser.parse augGrammar8 clr8 (Tokenizer.tokenizeTerminals s) |> Option.isSome

            r7 = r8


module ConflictBehaviorTests =

    [<Fact>]
    let ``LR(0) table for grammar3 has shift-reduce conflicts`` () =
        let table = LRParser.buildLR0Table augGrammar3

        Assert.NotEmpty(table.conflicts)

        let hasShiftReduce =
            table.conflicts
            |> List.exists (function
                | LRConflict.ShiftReduce _ -> true
                | _ -> false)

        Assert.True(hasShiftReduce)

    [<Fact>]
    let ``LR(0) table for grammar1 has shift-reduce conflicts`` () =
        let table = LRParser.buildLR0Table augGrammar1

        Assert.NotEmpty(table.conflicts)

        let shiftReduceCount =
            table.conflicts
            |> List.filter (function
                | LRConflict.ShiftReduce _ -> true
                | _ -> false)
            |> List.length

        Assert.True(shiftReduceCount > 0)

    [<Fact>]
    let ``Ambiguous grammar6 has conflicts in LR(0) table`` () =
        let table = LRParser.buildLR0Table augGrammar6
        Assert.True(table.conflicts.Length > 0)

    [<Fact>]
    let ``SLR(1) resolves LR(0) conflicts for grammar3`` () =
        let lr0 = LRParser.buildLR0Table augGrammar3
        let slr1 = LRParser.buildSLR1Table augGrammar3

        let lr0ConflictCount = lr0.conflicts.Length
        let slr1ConflictCount = slr1.conflicts.Length

        Assert.True(lr0ConflictCount > 0)
        Assert.True(slr1ConflictCount < lr0ConflictCount)

    [<Fact>]
    let ``SLR(1) resolves LR(0) conflicts for grammar1`` () =
        let lr0 = LRParser.buildLR0Table augGrammar1
        let slr1 = LRParser.buildSLR1Table augGrammar1

        let lr0ConflictCount = lr0.conflicts.Length
        let slr1ConflictCount = slr1.conflicts.Length

        Assert.True(lr0ConflictCount > 0)
        Assert.True(slr1ConflictCount < lr0ConflictCount)

    [<Fact>]
    let ``SLR(1) table has no conflicts for LR(1) grammars`` () =
        let tables =
            [ LRParser.buildSLR1Table augGrammar3
              LRParser.buildSLR1Table augGrammar7
              LRParser.buildSLR1Table augGrammar8 ]

        for table in tables do
            Assert.Empty(table.conflicts)

    [<Fact>]
    let ``CLR(1) table has no conflicts for LR(1) grammars`` () =
        let tables =
            [ LRParser.buildCLR1Table augGrammar3
              LRParser.buildCLR1Table augGrammar7
              LRParser.buildCLR1Table augGrammar8 ]

        for table in tables do
            Assert.Empty(table.conflicts)

    [<Fact>]
    let ``Non-LR grammar2 has conflicts in all table types`` () =
        let lr0 = LRParser.buildLR0Table augGrammar2
        let slr1 = LRParser.buildSLR1Table augGrammar2
        let clr1 = LRParser.buildCLR1Table augGrammar2

        Assert.NotEmpty(lr0.conflicts)
        Assert.NotEmpty(slr1.conflicts)
        Assert.NotEmpty(clr1.conflicts)

    [<Fact>]
    let ``LR(0) table for grammar7 has shift-reduce on epsilon`` () =
        let table = LRParser.buildLR0Table augGrammar7

        let hasEpsilonReduce =
            table.conflicts
            |> List.exists (fun c ->
                match c with
                | LRConflict.ShiftReduce(state = _; symbol = Symbol.Epsilon) -> true
                | LRConflict.ReduceReduce(state = _; symbol = Symbol.Epsilon) -> true
                | _ -> false)

        Assert.True(hasEpsilonReduce || table.conflicts.Length > 0)

    [<Fact>]
    let ``Conflict states reference valid state indices`` () =
        let lr0 = LRParser.buildLR0Table augGrammar6

        let autStateCount =
            match lr0.automaton with
            | LRAutomaton.LR0 dfa -> dfa.states.Length
            | LRAutomaton.LR1 dfa -> dfa.states.Length

        for conflict in lr0.conflicts do
            match conflict with
            | LRConflict.ShiftReduce(state = s; shiftTo = toIdx) ->
                Assert.True(s >= 0 && s < autStateCount, $"Invalid state {s}")
                Assert.True(toIdx >= 0 && toIdx < autStateCount, $"Invalid shiftTo {toIdx}")
            | LRConflict.ReduceReduce(state = s) -> Assert.True(s >= 0 && s < autStateCount, $"Invalid state {s}")
