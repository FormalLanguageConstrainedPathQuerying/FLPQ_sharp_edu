module ValiantTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Core

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

open TestGrammars


module ValiantParseTests =

    [<Fact>]
    let ``Valiant and CYK agree on acceptance`` () =
        let testCases =
            [ (grammar1, "abab", true)
              (grammar1, "ab", true)
              (grammar1, "", true)
              (grammar1, "aabb", true)
              (grammar1, "aababb", true)
              (grammar1, "aa", false)
              (grammar1, "bb", false)
              (grammar1, "abb", false)
              (grammar3, "a", true)
              (grammar3, "aa", true)
              (grammar3, "aaaa", true)
              (grammar3, "aaaaa", true)
              (grammar3, "", false)
              (grammar4, "a", true)
              (grammar4, "aaa", true)
              (grammar5, "a", true)
              (grammar5, "aaaa", true)
              (grammar5, "b", false)
              (grammar5, "", false) ]

        for (g, input, expected) in testCases do
            let cykResult = Cyk.parse g input
            let valiantResult = Valiant.parse g input
            Assert.Equal(cykResult, valiantResult)

    [<Fact>]
    let ``Valiant parseWithTable returns n by n matrix`` () =
        let input = "ab"
        let table, accepted = Valiant.parseWithTable grammar1 input
        Assert.True(accepted)
        Assert.Equal(input.Length, table.rows)
        Assert.Equal(input.Length, table.cols)

    [<Fact>]
    let ``Valiant table matches CYK table for small example`` () =
        let input = "aa"
        let cykTable, cykAcc = Cyk.parseWithTable grammar3 input
        let valTable, valAcc = Valiant.parseWithTable grammar3 input

        Assert.Equal(cykAcc, valAcc)
        Assert.Equal(cykTable.rows, valTable.rows)
        Assert.Equal(cykTable.cols, valTable.cols)

        for i in 0 .. input.Length - 1 do
            for j in i .. input.Length - 1 do
                Assert.Equal<Set<Nonterminal<string>>>(cykTable.data.[i, j], valTable.data.[i, j])

    [<Fact>]
    let ``Valiant table matches CYK table for grammar 1 small example`` () =
        let input = "abab"
        let cykTable, cykAcc = Cyk.parseWithTable grammar1 input
        let valTable, valAcc = Valiant.parseWithTable grammar1 input

        Assert.Equal(cykAcc, valAcc)

        for i in 0 .. input.Length - 1 do
            for j in i .. input.Length - 1 do
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

                cykAcc = valAcc
                && [ for i in 0 .. s.Length - 1 do
                         for j in i .. s.Length - 1 do
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

                cykAcc = valAcc
                && [ for i in 0 .. s.Length - 1 do
                         for j in i .. s.Length - 1 do
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

                cykAcc = valAcc
                && [ for i in 0 .. s.Length - 1 do
                         for j in i .. s.Length - 1 do
                             if cykTable.data.[i, j] <> valTable.data.[i, j] then
                                 yield false ]
                   |> List.forall id
