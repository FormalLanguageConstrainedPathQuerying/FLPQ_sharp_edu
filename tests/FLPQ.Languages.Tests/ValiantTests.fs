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
                Assert.Equal(Cyk.parse g s, Valiant.parse g s)

            for s in reject do
                Assert.Equal(Cyk.parse g s, Valiant.parse g s)

    [<Fact>]
    let ``Valiant parseWithTable returns n by n matrix`` () =
        let input = "a b"
        let table, accepted = Valiant.parseWithTable grammar1 input
        Assert.True(accepted)
        Assert.Equal(Tokenizer.tokenizeStrings input |> List.length, table.rows)
        Assert.Equal(Tokenizer.tokenizeStrings input |> List.length, table.cols)

    [<Fact>]
    let ``Valiant table matches CYK table for small example`` () =
        let input = "a a"
        let cykTable, cykAcc = Cyk.parseWithTable grammar3 input
        let valTable, valAcc = Valiant.parseWithTable grammar3 input

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
        let cykTable, cykAcc = Cyk.parseWithTable grammar1 input
        let valTable, valAcc = Valiant.parseWithTable grammar1 input

        Assert.Equal(cykAcc, valAcc)

        let n = cykTable.rows

        for i in 0 .. n - 1 do
            for j in i .. n - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(cykTable.data.[i, j], valTable.data.[i, j])


module PropertyTests =

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 1`` (s: string) =
            Cyk.parse grammar1 s = Valiant.parse grammar1 s

        [<Property>]
        let ``Valiant and CYK tables match for grammar 1`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc = Cyk.parseWithTable grammar1 s
                let valTable, valAcc = Valiant.parseWithTable grammar1 s
                let n = cykTable.rows

                cykAcc = valAcc
                && [ for i in 0 .. n - 1 do
                         for j in i .. n - 1 do
                             if cykTable.data.[i, j] <> valTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar2PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree on acceptance for grammar 2`` (s: string) =
            Cyk.parse grammar2 s = Valiant.parse grammar2 s

        [<Property>]
        let ``Valiant and CYK tables match for grammar 2`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc = Cyk.parseWithTable grammar2 s
                let valTable, valAcc = Valiant.parseWithTable grammar2 s
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
            Cyk.parse grammar3 s = Valiant.parse grammar3 s

        [<Property>]
        let ``Valiant and CYK tables match for grammar 3`` (s: string) =
            if s = "" then
                true
            else
                let cykTable, cykAcc = Cyk.parseWithTable grammar3 s
                let valTable, valAcc = Valiant.parseWithTable grammar3 s
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
            let cykTable, cykAcc = Cyk.parseWithTable grammar1 s
            let valTable, valAcc = Valiant.parseWithTable grammar1 s

            if cykAcc || valAcc then
                true
            else
                let n = cykTable.rows

                [ for i in 0 .. n - 1 do
                      for j in i .. n - 1 do
                          if cykTable.data.[i, j] <> valTable.data.[i, j] then
                              yield false ]
                |> List.forall id
