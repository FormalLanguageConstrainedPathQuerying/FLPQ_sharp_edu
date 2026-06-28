module BooleanDecompositionTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

type SetMatrixGenerators =

    static member SetMatrix() : Arbitrary<Matrix<Set<int>>> =
        MyGen.choose (0, 5)
        |> MyGen.bind (fun rows ->
            MyGen.choose (0, 5)
            |> MyGen.bind (fun cols ->
                MyGen.choose (0, 4)
                |> MyGen.listOfLength (rows * cols)
                |> MyGen.map (fun values ->
                    let array = Array2D.init rows cols (fun i j -> Set.empty)

                    for k in 0 .. min (values.Length - 1) (rows * cols - 1) do
                        let i = k / cols
                        let j = k % cols
                        array.[i, j] <- set [ values.[k] ]

                    { rows = rows
                      cols = cols
                      data = array })))
        |> MyArb.fromGen

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

[<Properties(Arbitrary = [| typeof<SetMatrixGenerators> |])>]
module PropertyTests =

    [<Property>]
    let ``decompose then recompose is identity`` (m: Matrix<Set<int>>) =
        let decomp = BooleanDecomposition.decompose m

        if Map.isEmpty decomp then
            true
        else
            let restored = BooleanDecomposition.recompose decomp

            m.rows = restored.rows
            && m.cols = restored.cols
            && [ for i in 0 .. m.rows - 1 do
                     for j in 0 .. m.cols - 1 do
                         if m.data.[i, j] <> restored.data.[i, j] then
                             yield false ]
               |> List.forall id
