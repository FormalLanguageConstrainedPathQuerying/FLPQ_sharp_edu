module LinearAlgebraTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Properties(Arbitrary = [| typeof<LinearAlgebraGenerators> |])>]
module PropertyTests =

    [<Property>]
    let ``Kronecker product with 1x1 matrix is equivalent to map`` (singleValue: int) (b: Matrix<int>) =
        let a = Matrix.init 1 1 singleValue
        let kronResult = LinearAlgebra.kron a b (*) 0
        let mapResult = Matrix.map (fun x -> singleValue * x) b
        kronResult = mapResult

    [<Property>]
    let ``mxm with identity matrix returns original matrix`` (a: Matrix<int>) =
        let n = a.rows
        let identity = Matrix.create n n (fun i j -> if i = j then 1 else 0)

        let leftResult = LinearAlgebra.mxm identity a (*) (+) 0

        let rightResult = LinearAlgebra.mxm a identity (*) (+) 0

        leftResult = a && rightResult = a

    [<Property>]
    let ``transpose of product equals product of transposes in reverse order`` ((a, b): Matrix<int> * Matrix<int>) =
        let product = LinearAlgebra.mxm a b (*) (+) 0
        let transposedProduct = Matrix.transpose product

        let productOfTransposes =
            LinearAlgebra.mxm (Matrix.transpose b) (Matrix.transpose a) (*) (+) 0

        transposedProduct = productOfTransposes


module FactTests =

    [<Fact>]
    let ``mxm throws when dimensions are incompatible`` () =
        let a = Matrix.init 2 3 1
        let b = Matrix.init 2 2 2
        Assert.Throws<System.ArgumentException>(fun () -> LinearAlgebra.mxm a b (*) (+) 0 |> ignore)

    [<Fact>]
    let ``mxm produces correct result dimensions`` () =
        let a = Matrix.init 3 4 1
        let b = Matrix.init 4 2 2
        let result = LinearAlgebra.mxm a b (*) (+) 0
        Assert.Equal(3, result.rows)
        Assert.Equal(2, result.cols)

    [<Fact>]
    let ``kron produces correct result dimensions`` () =
        let a = Matrix.init 2 3 1
        let b = Matrix.init 4 5 2
        let result = LinearAlgebra.kron a b (*) 0
        Assert.Equal(8, result.rows)
        Assert.Equal(15, result.cols)

    [<Fact>]
    let ``kron with 1x1 matrix produces correct values`` () =
        let a = Matrix.init 1 1 7
        let b = Matrix.create 2 3 (fun i j -> i * 3 + j)
        let result = LinearAlgebra.kron a b (*) 0

        Assert.Equal(2, result.rows)
        Assert.Equal(3, result.cols)

        for i in 0..1 do
            for j in 0..2 do
                Assert.Equal(7 * (i * 3 + j), result.data.[i, j])

    [<Fact>]
    let ``mxm with identity matrix preserves values`` () =
        let a = Matrix.create 3 3 (fun i j -> i * 3 + j)
        let identity = Matrix.create 3 3 (fun i j -> if i = j then 1 else 0)
        let result = LinearAlgebra.mxm a identity (*) (+) 0

        for i in 0..2 do
            for j in 0..2 do
                Assert.Equal(a.data.[i, j], result.data.[i, j])

    [<Fact>]
    let ``mxm computes known product correctly`` () =
        let a = Matrix.ofArray2D (array2D [ [ 1; 2; 3 ]; [ 4; 5; 6 ] ])
        let b = Matrix.ofArray2D (array2D [ [ 7; 8 ]; [ 9; 10 ]; [ 11; 12 ] ])
        let result = LinearAlgebra.mxm a b (*) (+) 0
        Assert.Equal(2, result.rows)
        Assert.Equal(2, result.cols)
        Assert.Equal(1 * 7 + 2 * 9 + 3 * 11, result.data.[0, 0])
        Assert.Equal(1 * 8 + 2 * 10 + 3 * 12, result.data.[0, 1])
        Assert.Equal(4 * 7 + 5 * 9 + 6 * 11, result.data.[1, 0])
        Assert.Equal(4 * 8 + 5 * 10 + 6 * 12, result.data.[1, 1])

    [<Fact>]
    let ``kron result cell equals product of corresponding elements`` () =
        let a = Matrix.create 2 2 (fun i j -> i * 2 + j + 1)
        let b = Matrix.create 3 2 (fun i j -> i * 2 + j + 10)
        let result = LinearAlgebra.kron a b (*) 0

        Assert.Equal(6, result.rows)
        Assert.Equal(4, result.cols)

        Assert.Equal(a.data.[0, 0] * b.data.[0, 0], result.data.[0, 0])
        Assert.Equal(a.data.[0, 1] * b.data.[2, 1], result.data.[2, 3])
        Assert.Equal(a.data.[1, 1] * b.data.[1, 0], result.data.[4, 2])
