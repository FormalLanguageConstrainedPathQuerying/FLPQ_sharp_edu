module ValiantTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Core

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb


module BooleanDecompositionTests =

    [<Fact>]
    let ``decompose produces correct number of matrices`` () =
        let m =
            Matrix.create 2 2 (fun i j ->
                if i = 0 && j = 0 then set [ "a"; "b" ]
                elif i = 1 && j = 1 then set [ "a" ]
                else Set.empty)

        let decomp = BooleanDecomposition.decompose m
        Assert.Equal(2, Map.count decomp)

    [<Fact>]
    let ``decompose cells match original sets`` () =
        let m = Matrix.create 2 2 (fun i j -> if i = j then set [ i ] else Set.empty)

        let decomp = BooleanDecomposition.decompose m

        Assert.True(Map.containsKey 0 decomp)
        Assert.True(Map.containsKey 1 decomp)

        let mat0 = Map.find 0 decomp
        Assert.True(mat0.data.[0, 0])
        Assert.False(mat0.data.[1, 1])

        let mat1 = Map.find 1 decomp
        Assert.False(mat1.data.[0, 0])
        Assert.True(mat1.data.[1, 1])

    [<Fact>]
    let ``recompose restores original after decompose`` () =
        let m = Matrix.create 3 3 (fun i j -> set [ i + j ])

        let decomp = BooleanDecomposition.decompose m
        let restored = BooleanDecomposition.recompose decomp

        for i in 0..2 do
            for j in 0..2 do
                Assert.Equal<Set<int>>(m.data.[i, j], restored.data.[i, j])

    [<Fact>]
    let ``decompose handles empty matrix`` () =
        let m = Matrix.create 2 2 (fun _ _ -> Set.empty: Set<int>)

        let decomp = BooleanDecomposition.decompose m
        Assert.Equal(0, Map.count decomp)

    [<Fact>]
    let ``recompose of empty decomposition throws`` () =
        let empty: Map<int, Matrix<bool>> = Map.empty
        Assert.Throws<System.ArgumentException>(fun () -> BooleanDecomposition.recompose empty |> ignore)

    [<Fact>]
    let ``decompose preserves matrix dimensions`` () =
        let m = Matrix.create 3 4 (fun i j -> if i < j then set [ i ] else Set.empty)

        let decomp = BooleanDecomposition.decompose m

        for kv in decomp do
            Assert.Equal(3, kv.Value.rows)
            Assert.Equal(4, kv.Value.cols)


module ValiantParseTests =

    let testGrammarsAndStrings =
        [ ("abab", "S -> a S b S\nS -> eps", true)
          ("ab", "S -> a S b S\nS -> eps", true)
          ("", "S -> a S b S\nS -> eps", true)
          ("aabb", "S -> a S b S\nS -> eps", true)
          ("aababb", "S -> a S b S\nS -> eps", true)
          ("aa", "S -> a S b S\nS -> eps", false)
          ("bb", "S -> a S b S\nS -> eps", false)
          ("abb", "S -> a S b S\nS -> eps", false)
          ("a", "S -> a S\nS -> a", true)
          ("aa", "S -> a S\nS -> a", true)
          ("aaaa", "S -> a S\nS -> a", true)
          ("aaaaa", "S -> a S\nS -> a", true)
          ("", "S -> a S\nS -> a", false)
          ("a", "S -> S a\nS -> a", true)
          ("aaa", "S -> S a\nS -> a", true)
          ("a", "S -> S S\nS -> S S S\nS -> a", true)
          ("aaaa", "S -> S S\nS -> S S S\nS -> a", true)
          ("b", "S -> S S\nS -> S S S\nS -> a", false)
          ("", "S -> S S\nS -> S S S\nS -> a", false) ]

    [<Fact>]
    let ``Valiant and CYK agree on specific strings`` () =
        for (input, grammarText, expected) in testGrammarsAndStrings do
            let g = Grammar.parseGrammar grammarText
            let cykResult = Cyk.parse g input
            let valiantResult = Valiant.parse g input
            Assert.Equal(cykResult, valiantResult)

    [<Fact>]
    let ``Valiant parseWithTable produces correct table dimensions`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S b S
        S -> eps
        "

        let input = "ab"
        let table, accepted = Valiant.parseWithTable g input
        Assert.True(accepted)

        let n = input.Length

        for kv in table do
            let mat = kv.Value
            Assert.True(mat.rows >= n + 1)
            Assert.True(mat.cols >= n + 1)

    [<Fact>]
    let ``Valiant table matches CYK table for small example`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> a S
        S -> a
        "

        let input = "aa"
        let cykTable, cykAcc = Cyk.parseWithTable g input
        let valTable, valAcc = Valiant.parseWithTable g input

        Assert.Equal(cykAcc, valAcc)

        let nonterms = Map.keys cykTable |> Set.ofSeq

        let n = input.Length

        for nt in nonterms do
            let cykMat = Map.find nt cykTable
            let valMat = Map.find nt valTable

            for i in 0 .. n - 1 do
                for j in i .. n - 1 do
                    Assert.Equal(cykMat.data.[i, j], valMat.data.[i, j + 1])


module PropertyTests =

    let private grammar1 = Grammar.parseGrammar "S -> a S b S\nS -> eps"
    let private grammar2 = Grammar.parseGrammar "S -> a S b\nS -> eps\nS -> S S"
    let private grammar3 = Grammar.parseGrammar "S -> a S\nS -> a"

    type AbStringGenerators =

        static member AbString() : Arbitrary<string> =
            MyGen.choose (0, 10)
            |> MyGen.bind (fun len ->
                MyGen.choose (0, 1)
                |> MyGen.listOfLength len
                |> MyGen.map (fun bits ->
                    bits |> List.map (fun b -> if b = 0 then 'a' else 'b') |> System.String.Concat))
            |> MyArb.fromGen

    type AStringGenerators =

        static member AString() : Arbitrary<string> =
            MyGen.choose (0, 12)
            |> MyGen.map (fun len -> System.String('a', len))
            |> MyArb.fromGen

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar1PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree for grammar 1`` (s: string) =
            Cyk.parse grammar1 s = Valiant.parse grammar1 s

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Grammar2PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree for grammar 2`` (s: string) =
            Cyk.parse grammar2 s = Valiant.parse grammar2 s

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module Grammar3PropertyTests =

        [<Property>]
        let ``Valiant and CYK agree for grammar 3`` (s: string) =
            Cyk.parse grammar3 s = Valiant.parse grammar3 s
