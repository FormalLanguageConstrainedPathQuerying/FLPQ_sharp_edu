module BooleanDecompositionTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

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
    Assert.True(Matrix.get mat0 0 0)
    Assert.False(Matrix.get mat0 1 1)

    let mat1 = Map.find 1 decomp
    Assert.False(Matrix.get mat1 0 0)
    Assert.True(Matrix.get mat1 1 1)

[<Fact>]
let ``recompose restores original after decompose`` () =
    let m = Matrix.create 3 3 (fun i j -> set [ i + j ])

    let decomp = BooleanDecomposition.decompose m
    let restored = BooleanDecomposition.recompose decomp

    for i in 0..2 do
        for j in 0..2 do
            Assert.Equal<Set<int>>(Matrix.get m i j, Matrix.get restored i j)

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
        Assert.Equal(3, Matrix.rows kv.Value)
        Assert.Equal(4, Matrix.cols kv.Value)

[<Properties(Arbitrary = [| typeof<SetMatrixGenerators> |])>]
module PropertyTests =

    [<Property>]
    let ``decompose then recompose is identity`` (m: Matrix<Set<int>>) =
        let hasNonEmpty =
            [ for i in 0 .. Matrix.rows m - 1 do
                  for j in 0 .. Matrix.cols m - 1 do
                      if not (Set.isEmpty (Matrix.get m i j)) then
                          yield true ]
            |> List.contains true

        let decomp = BooleanDecomposition.decompose m

        if hasNonEmpty then
            not (Map.isEmpty decomp)
            && (let restored = BooleanDecomposition.recompose decomp

                Matrix.rows m = Matrix.rows restored
                && Matrix.cols m = Matrix.cols restored
                && [ for i in 0 .. Matrix.rows m - 1 do
                         for j in 0 .. Matrix.cols m - 1 do
                             if Matrix.get m i j <> Matrix.get restored i j then
                                 yield false ]
                   |> List.forall id)
        else
            true
