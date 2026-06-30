module ValiantTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars


module ValiantParseTests =

    [<Fact>]
    let ``Valiant and CYK agree on all test strings`` () =
        let cases =
            [ (grammar1, grammar1Accept, grammar1Reject)
              (grammar3, grammar3Accept, grammar3Reject)
              (grammar4, grammar3Accept, grammar3Reject)
              (grammar5, grammar3Accept, grammar3Reject) ]

        for (g, accept, reject) in cases do
            for s in accept do
                Assert.Equal(Cyk.parse g (Tokenizer.tokenize s), Valiant.parse g (Tokenizer.tokenizeStrings s))

            for s in reject do
                Assert.Equal(Cyk.parse g (Tokenizer.tokenize s), Valiant.parse g (Tokenizer.tokenizeStrings s))

    [<Fact>]
    let ``Valiant parseWithTable returns n by n matrix`` () =
        let input = "a b"

        let table, accepted =
            Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings input)

        Assert.True(accepted)
        Assert.Equal(Tokenizer.tokenizeStrings input |> List.length, table.rows)
        Assert.Equal(Tokenizer.tokenizeStrings input |> List.length, table.cols)

    [<Fact>]
    let ``Valiant table matches CYK table for small example`` () =
        let input = "a a"
        let cykTable, cykAcc = Cyk.parseWithTable grammar3 (Tokenizer.tokenize input)

        let valTable, valAcc =
            Valiant.parseWithTable grammar3 (Tokenizer.tokenizeStrings input)

        Assert.Equal(cykAcc, valAcc)
        Assert.Equal(cykTable.rows, valTable.rows)
        Assert.Equal(cykTable.cols, valTable.cols)

        let n = cykTable.rows

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(cykTable.data.[i, j], valTable.data.[i, j])

    [<Fact>]
    let ``Valiant table matches CYK table for grammar 1 small example`` () =
        let input = "a b a b"
        let cykTable, cykAcc = Cyk.parseWithTable grammar1 (Tokenizer.tokenize input)

        let valTable, valAcc =
            Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings input)

        Assert.Equal(cykAcc, valAcc)

        let n = cykTable.rows

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(cykTable.data.[i, j], valTable.data.[i, j])


module ModifiedValiantTests =

    [<Fact>]
    let ``Modified Valiant and standard Valiant agree on all test strings`` () =
        let cases =
            [ (grammar1, grammar1Accept, grammar1Reject)
              (grammar3, grammar3Accept, grammar3Reject)
              (grammar6, exprAccept, exprReject) ]

        for (g, accept, reject) in cases do
            for s in accept do
                Assert.Equal(
                    Valiant.parse g (Tokenizer.tokenizeStrings s),
                    Valiant.parseModified g (Tokenizer.tokenizeStrings s)
                )

            for s in reject do
                Assert.Equal(
                    Valiant.parse g (Tokenizer.tokenizeStrings s),
                    Valiant.parseModified g (Tokenizer.tokenizeStrings s)
                )

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for grammar 1`` () =
        let input = "a b a b"

        let valTable, valAcc =
            Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable grammar1 (Tokenizer.tokenizeStrings input)

        Assert.Equal(valAcc, modAcc)

        let n = valTable.rows

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(valTable.data.[i, j], modTable.data.[i, j])

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for grammar 3`` () =
        let input = "a a a a"

        let valTable, valAcc =
            Valiant.parseWithTable grammar3 (Tokenizer.tokenizeStrings input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable grammar3 (Tokenizer.tokenizeStrings input)

        Assert.Equal(valAcc, modAcc)

        let n = valTable.rows

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(valTable.data.[i, j], modTable.data.[i, j])

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for expression grammar`` () =
        let input = "x + x * x"

        let valTable, valAcc =
            Valiant.parseWithTable grammar6 (Tokenizer.tokenizeStrings input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable grammar6 (Tokenizer.tokenizeStrings input)

        Assert.Equal(valAcc, modAcc)

        let n = valTable.rows

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(valTable.data.[i, j], modTable.data.[i, j])

    [<Fact>]
    let ``Modified Valiant trace produces steps for grammar 1`` () =
        let input = "a b"

        let trace =
            Valiant.parseModifiedWithTrace grammar1 (Tokenizer.tokenizeStrings input)

        Assert.NotEmpty(trace)

        let lastStep = trace |> List.last
        Assert.True(lastStep.submatrices.Length >= 1)

    [<Fact>]
    let ``Modified Valiant trace submatrices are disjoint within each layer`` () =
        let input = "a b a b"

        let trace =
            Valiant.parseModifiedWithTrace grammar1 (Tokenizer.tokenizeStrings input)

        Assert.NotEmpty(trace)

        for step in trace do
            let cells =
                step.submatrices
                |> List.collect (fun m ->
                    [ for i in m.A - m.Size + 1 .. m.A do
                          for j in m.B .. m.B + m.Size - 1 do
                              yield (i, j) ])

            let uniqueCells = Set.ofList cells
            Assert.Equal(List.length cells, Set.count uniqueCells)


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 1`` (s: string) =
            Cyk.parse grammar1 (Tokenizer.tokenize s) = Valiant.parse grammar1 (Tokenizer.tokenizeStrings s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc = Cyk.parseWithTable grammar1 (Tokenizer.tokenize s)
                let valTable, valAcc = Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings s)
                let n = cykTable.rows

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.data.[i, j] <> valTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 1`` (s: string) =
            Valiant.parse grammar1 (Tokenizer.tokenizeStrings s) = Valiant.parseModified
                grammar1
                (Tokenizer.tokenizeStrings s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc = Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable grammar1 (Tokenizer.tokenizeStrings s)

                let n = valTable.rows

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if valTable.data.[i, j] <> modTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar2PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 2`` (s: string) =
            Cyk.parse grammar2 (Tokenizer.tokenize s) = Valiant.parse grammar2 (Tokenizer.tokenizeStrings s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 2`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc = Cyk.parseWithTable grammar2 (Tokenizer.tokenize s)
                let valTable, valAcc = Valiant.parseWithTable grammar2 (Tokenizer.tokenizeStrings s)
                let n = cykTable.rows

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.data.[i, j] <> valTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Grammar3PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 3`` (s: string) =
            Cyk.parse grammar3 (Tokenizer.tokenize s) = Valiant.parse grammar3 (Tokenizer.tokenizeStrings s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 3`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc = Cyk.parseWithTable grammar3 (Tokenizer.tokenize s)
                let valTable, valAcc = Valiant.parseWithTable grammar3 (Tokenizer.tokenizeStrings s)
                let n = cykTable.rows

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.data.[i, j] <> valTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module RejectTableTests =

        [<Property>]
        let ``CYK and Valiant reject tables are identical for grammar 1`` (s: string) =
            let cykTable, cykAcc = Cyk.parseWithTable grammar1 (Tokenizer.tokenize s)
            let valTable, valAcc = Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings s)

            if cykAcc || valAcc then
                true
            else
                let n = cykTable.rows

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if cykTable.data.[i, j] <> valTable.data.[i, j] then
                              yield false ]
                |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module ModifiedValiantPropertyTests =

        [<Property>]
        let ``Modified Valiant and standard Valiant reject tables are identical for grammar 1`` (s: string) =
            let valTable, valAcc = Valiant.parseWithTable grammar1 (Tokenizer.tokenizeStrings s)

            let modTable, modAcc =
                Valiant.parseModifiedWithTable grammar1 (Tokenizer.tokenizeStrings s)

            if valAcc || modAcc then
                true
            else
                let n = valTable.rows

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if valTable.data.[i, j] <> modTable.data.[i, j] then
                              yield false ]
                |> List.forall id

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module ModifiedValiantExprPropertyTests =

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 6`` (s: string) =
            Valiant.parse grammar6 (Tokenizer.tokenizeStrings s) = Valiant.parseModified
                grammar6
                (Tokenizer.tokenizeStrings s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 6`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc = Valiant.parseWithTable grammar6 (Tokenizer.tokenizeStrings s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable grammar6 (Tokenizer.tokenizeStrings s)

                let n = valTable.rows

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if valTable.data.[i, j] <> modTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id
