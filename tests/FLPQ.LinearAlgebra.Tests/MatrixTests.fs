module MatrixTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Properties(Arbitrary = [| typeof<MatrixGenerators> |])>]
module PropertyTests =

    [<Property>]
    let ``map2 with commutative operation is commutative`` ((a, b): Matrix<int> * Matrix<int>) =
        Matrix.map2 (+) a b = Matrix.map2 (+) b a

    [<Property>]
    let ``repeated transpose is identity`` (m: Matrix<int>) =
        Matrix.transpose (Matrix.transpose m) = m

    [<Property>]
    let ``sequence of maps is a single map with composition`` (m: Matrix<int>) (f: int) (g: int) =
        let f = (+) f
        let g = (+) g
        Matrix.map f (Matrix.map g m) = Matrix.map (f << g) m

    [<Property>]
    let ``map preserves dimensions`` (m: Matrix<int>) (f: int) =
        let f = (+) f
        let result = Matrix.map f m
        Matrix.rows result = Matrix.rows m && Matrix.cols result = Matrix.cols m

    [<Property>]
    let ``transpose swaps dimensions`` (m: Matrix<int>) =
        let result = Matrix.transpose m
        Matrix.rows result = Matrix.cols m && Matrix.cols result = Matrix.rows m

module FactTests =

    [<Fact>]
    let ``map2 throws when dimensions differ`` () =
        let a = Matrix.init 2 3 1
        let b = Matrix.init 3 2 2
        Assert.Throws<System.ArgumentException>(fun () -> Matrix.map2 (+) a b |> ignore)

    [<Fact>]
    let ``create yields correct element values`` () =
        let rows = 3
        let cols = 2
        let m = Matrix.create rows cols (fun i j -> i * 1000 + j)

        Assert.Equal(0 * 1000 + 0, Matrix.get m 0 0)
        Assert.Equal(0 * 1000 + 1, Matrix.get m 0 1)
        Assert.Equal(1 * 1000 + 0, Matrix.get m 1 0)
        Assert.Equal(1 * 1000 + 1, Matrix.get m 1 1)
        Assert.Equal(2 * 1000 + 0, Matrix.get m 2 0)
        Assert.Equal(2 * 1000 + 1, Matrix.get m 2 1)

    [<Fact>]
    let ``init fills all cells with the same value`` () =
        let m = Matrix.init 3 2 42

        for i in 0..2 do
            for j in 0..1 do
                Assert.Equal(42, Matrix.get m i j)

    [<Fact>]
    let ``transpose of 2x3 produces 3x2`` () =
        let m = Matrix.create 2 3 (fun i j -> i * 10 + j)
        let t = Matrix.transpose m
        Assert.Equal(3, Matrix.rows t)
        Assert.Equal(2, Matrix.cols t)
        Assert.Equal(Matrix.get m 0 0, Matrix.get t 0 0)
        Assert.Equal(Matrix.get m 0 1, Matrix.get t 1 0)
        Assert.Equal(Matrix.get m 0 2, Matrix.get t 2 0)
        Assert.Equal(Matrix.get m 1 0, Matrix.get t 0 1)
        Assert.Equal(Matrix.get m 1 1, Matrix.get t 1 1)
        Assert.Equal(Matrix.get m 1 2, Matrix.get t 2 1)

    [<Fact>]
    let ``copy produces independent matrix`` () =
        let m = Matrix.create 2 2 (fun i j -> i * 10 + j)
        let c = Matrix.copy m
        Assert.Equal(2, Matrix.rows c)
        Assert.Equal(2, Matrix.cols c)
        Assert.Equal(0, Matrix.get c 0 0)
        Assert.Equal(11, Matrix.get c 1 1)
        Matrix.set c 0 0 999
        Assert.Equal(0, Matrix.get m 0 0)

    [<Fact>]
    let ``ofArray2D creates matrix from 2D array`` () =
        let arr = Array2D.init 3 2 (fun i j -> i * 100 + j)
        let m = Matrix.ofArray2D arr
        Assert.Equal(3, Matrix.rows m)
        Assert.Equal(2, Matrix.cols m)
        Assert.Equal(0, Matrix.get m 0 0)
        Assert.Equal(101, Matrix.get m 1 1)

    [<Fact>]
    let ``reduceByColumn reduces each column`` () =
        let m = Matrix.create 3 2 (+)
        let result = Matrix.reduceByColumn (+) 0 m
        Assert.Equal(2, Array.length result)
        Assert.Equal(3, result.[0])
        Assert.Equal(6, result.[1])

    [<Fact>]
    let ``reduceByColumn with non-trivial init`` () =
        let m = Matrix.create 2 2 (fun i j -> 1)
        let result = Matrix.reduceByColumn (*) 1 m
        Assert.Equal(2, Array.length result)
        Assert.Equal(1, result.[0])
        Assert.Equal(1, result.[1])

[<Fact>]
let ``diagonal matrix has ones on diagonal for selected indices`` () =
    let d = Matrix.diagonal 5 (set [ 0; 2; 4 ]) 1 0
    Assert.Equal(1, Matrix.get d 0 0)
    Assert.Equal(0, Matrix.get d 1 1)
    Assert.Equal(1, Matrix.get d 2 2)
    Assert.Equal(0, Matrix.get d 3 3)
    Assert.Equal(1, Matrix.get d 4 4)
    Assert.Equal(0, Matrix.get d 0 1)

[<Fact>]
let ``diagonal matrix with empty set is zero matrix`` () =
    let d = Matrix.diagonal 3 Set.empty 1 0
    Assert.Equal(0, Matrix.get d 0 0)
    Assert.Equal(0, Matrix.get d 1 1)
    Assert.Equal(0, Matrix.get d 2 2)

[<Fact>]
let ``diagonal matrix with all indices is identity`` () =
    let d = Matrix.diagonal 3 (set [ 0; 1; 2 ]) 1 0
    Assert.Equal(1, Matrix.get d 0 0)
    Assert.Equal(1, Matrix.get d 1 1)
    Assert.Equal(1, Matrix.get d 2 2)
    Assert.Equal(0, Matrix.get d 0 1)
