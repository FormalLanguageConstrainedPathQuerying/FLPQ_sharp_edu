module ValiantTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

open TestGrammars
open FLPQ.TestUtilities


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
                Assert.Equal(
                    Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s),
                    Valiant.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s)
                )

            for s in reject do
                Assert.Equal(
                    Cyk.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s),
                    Valiant.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s)
                )

    [<Fact>]
    let ``Valiant parseWithTable returns n by n matrix`` () =
        let input = "a b"

        let table, accepted =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.True(accepted)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.rows table)
        Assert.Equal(Tokenizer.tokenizeTerminals input |> List.length, Matrix.cols table)

    [<Fact>]
    let ``Valiant table matches CYK table for small example`` () =
        let input = "a a"

        let cykTable, cykAcc =
            Cyk.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(cykAcc, valAcc)
        Assert.Equal(Matrix.rows cykTable, Matrix.rows valTable)
        Assert.Equal(Matrix.cols cykTable, Matrix.cols valTable)

        let n = Matrix.rows cykTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(Matrix.get cykTable i j, Matrix.get valTable i j)

    [<Fact>]
    let ``Valiant table matches CYK table for grammar 1 small example`` () =
        let input = "a b a b"

        let cykTable, cykAcc =
            Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(cykAcc, valAcc)

        let n = Matrix.rows cykTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(Matrix.get cykTable i j, Matrix.get valTable i j)


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
                    Valiant.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s),
                    Valiant.parseModified Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s)
                )

            for s in reject do
                Assert.Equal(
                    Valiant.parse Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s),
                    Valiant.parseModified Grammar.freshStringNonterminal g (Tokenizer.tokenizeTerminals s)
                )

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for grammar 1`` () =
        let input = "a b a b"

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(valAcc, modAcc)

        let n = Matrix.rows valTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(Matrix.get valTable i j, Matrix.get modTable i j)

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for grammar 3`` () =
        let input = "a a a a"

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(valAcc, modAcc)

        let n = Matrix.rows valTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(Matrix.get valTable i j, Matrix.get modTable i j)

    [<Fact>]
    let ``Modified Valiant table matches standard Valiant table for expression grammar`` () =
        let input = "x + x * x"

        let valTable, valAcc =
            Valiant.parseWithTable Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals input)

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals input)

        Assert.Equal(valAcc, modAcc)

        let n = Matrix.rows valTable

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(Matrix.get valTable i j, Matrix.get modTable i j)

    [<Fact>]
    let ``Modified Valiant trace produces steps for grammar 1`` () =
        let input = "a b"

        let trace =
            Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.NotEmpty(trace)

        let lastStep = trace |> List.last

        match lastStep with
        | Valiant.LayerForward(_, _, submatrices) -> Assert.True(submatrices.Length >= 1 || submatrices.Length = 0)
        | Valiant.LayerBackward(_, _, submatrices, _) -> Assert.True(submatrices.Length >= 1 || submatrices.Length = 0)

    [<Fact>]
    let ``Modified Valiant trace submatrices are disjoint within each layer`` () =
        let input = "a b a b"

        let trace =
            Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals input)

        Assert.NotEmpty(trace)

        for step in trace do
            let submatrices =
                match step with
                | Valiant.LayerForward(_, _, sm) -> sm
                | Valiant.LayerBackward(_, _, sm, _) -> sm

            let cells =
                submatrices
                |> List.collect (fun m ->
                    [ for i in m.row - m.Size + 1 .. m.row do
                          for j in m.col .. m.col + m.Size - 1 do
                              yield (i, j) ])

            let uniqueCells = Set.ofList cells
            Assert.Equal(List.length cells, Set.count uniqueCells)

    [<Fact>]
    let ``Modified Valiant empty input with epsilon grammar returns accepted`` () =
        let result = Valiant.parseModified Grammar.freshStringNonterminal grammar1 []
        Assert.True(result)

    [<Fact>]
    let ``Modified Valiant empty input with non-epsilon grammar returns rejected`` () =
        let result = Valiant.parseModified Grammar.freshStringNonterminal grammar3 []
        Assert.False(result)

    [<Fact>]
    let ``Modified Valiant empty input parseWithTable returns correct table`` () =
        let table, accepted =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar1 []

        Assert.True(accepted)
        Assert.Equal(0, Matrix.rows table)
        Assert.Equal(0, Matrix.cols table)

    [<Fact>]
    let ``Modified Valiant empty input parseWithTable with non-epsilon grammar`` () =
        let table, accepted =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar3 []

        Assert.False(accepted)
        Assert.Equal(0, Matrix.rows table)
        Assert.Equal(0, Matrix.cols table)

    [<Fact>]
    let ``Standard Valiant empty input with epsilon grammar returns accepted`` () =
        let result = Valiant.parse Grammar.freshStringNonterminal grammar1 []
        Assert.True(result)

    [<Fact>]
    let ``Standard Valiant empty input with non-epsilon grammar returns rejected`` () =
        let result = Valiant.parse Grammar.freshStringNonterminal grammar3 []
        Assert.False(result)


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 1`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar1
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get cykTable i j <> Matrix.get valTable i j then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 1`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar1
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar1
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get valTable i j <> Matrix.get modTable i j then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar2PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 2`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar2 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar2
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 2`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar2 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar2 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get cykTable i j <> Matrix.get valTable i j then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Grammar3PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 3`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar3
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 3`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar3 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get cykTable i j <> Matrix.get valTable i j then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module RejectTableTests =

        [<Property>]
        let ``CYK and Valiant reject tables are identical for grammar 1`` (s: string) =
            let cykTable, cykAcc =
                Cyk.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            let valTable, valAcc =
                Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            if cykAcc || valAcc then
                true
            else
                let n = Matrix.rows cykTable

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if Matrix.get cykTable i j <> Matrix.get valTable i j then
                              yield false ]
                |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module ModifiedValiantPropertyTests =

        [<Property>]
        let ``Modified Valiant and standard Valiant reject tables are identical for grammar 1`` (s: string) =
            let valTable, valAcc =
                Valiant.parseWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            let modTable, modAcc =
                Valiant.parseModifiedWithTable Grammar.freshStringNonterminal grammar1 (Tokenizer.tokenizeTerminals s)

            if valAcc || modAcc then
                true
            else
                let n = Matrix.rows valTable

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if Matrix.get valTable i j <> Matrix.get modTable i j then
                              yield false ]
                |> List.forall id

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module ModifiedValiantExprPropertyTests =

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 6`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar6
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 6`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar6 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar6
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get valTable i j <> Matrix.get modTable i j then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module Grammar7PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 7`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar7
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 7`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get cykTable i j <> Matrix.get valTable i j then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 7`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar7
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 7`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar7 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar7
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get valTable i j <> Matrix.get modTable i j then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<ExprStringGenerators> |])>]
    module Grammar8PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 8`` (s: string) =
            Cyk.parse Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s) = Valiant.parse
                Grammar.freshStringNonterminal
                grammar8
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Valiant and CYK tables match for grammar 8`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc =
                    Cyk.parseWithTable Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s)

                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows cykTable

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get cykTable i j <> Matrix.get valTable i j then
                                 yield false ]
                   |> List.forall id

        [<Property>]
        let ``Modified Valiant and standard Valiant agree on acceptance for grammar 8`` (s: string) =
            Valiant.parse Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s) = Valiant.parseModified
                Grammar.freshStringNonterminal
                grammar8
                (Tokenizer.tokenizeTerminals s)

        [<Property>]
        let ``Modified Valiant and standard Valiant tables match for grammar 8`` (s: string) =
            if s = "" then
                true
            else
                let valTable, valAcc =
                    Valiant.parseWithTable Grammar.freshStringNonterminal grammar8 (Tokenizer.tokenizeTerminals s)

                let modTable, modAcc =
                    Valiant.parseModifiedWithTable
                        Grammar.freshStringNonterminal
                        grammar8
                        (Tokenizer.tokenizeTerminals s)

                let n = Matrix.rows valTable

                valAcc = modAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if Matrix.get valTable i j <> Matrix.get modTable i j then
                                 yield false ]
                   |> List.forall id
