module StressTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Fact>]
[<Trait("Category", "Stress")>]
let ``mxm of 200x200 integer matrices succeeds`` () =
    let n = 200

    let a = Array2D.init n n (fun i j -> (i * j) % 100) |> Matrix.ofArray2D

    let b = Array2D.init n n (fun i j -> (i + j) % 100) |> Matrix.ofArray2D

    let c = LinearAlgebra.mxm a b (*) (+) 0
    Assert.Equal(n, c.rows)
    Assert.Equal(n, c.cols)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``kron of 50x50 matrices produces 2500x2500 result`` () =
    let n = 50

    let a = Array2D.init n n (fun i j -> i + j) |> Matrix.ofArray2D

    let b = Array2D.init n n (fun i j -> i * j) |> Matrix.ofArray2D

    let c = LinearAlgebra.kron a b (*) 0
    Assert.Equal(n * n, c.rows)
    Assert.Equal(n * n, c.cols)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``map2 of 200x200 matrices succeeds`` () =
    let n = 200

    let a = Array2D.init n n (fun i j -> i + j) |> Matrix.ofArray2D

    let b = Array2D.init n n (fun i j -> i * j) |> Matrix.ofArray2D

    let c = Matrix.map2 (+) a b
    Assert.Equal(n, c.rows)
    Assert.Equal(n, c.cols)
    Assert.Equal(0 + 0 + 0 * 0, c.data.[0, 0])

[<Fact>]
[<Trait("Category", "Stress")>]
let ``mxm associativity holds for 100x100 random matrices`` () =
    let n = 100

    let a = Array2D.init n n (fun i j -> i * n + j) |> Matrix.ofArray2D

    let b = Array2D.init n n (fun i j -> (i + j) % 50) |> Matrix.ofArray2D

    let c = Array2D.init n n (fun i j -> (i - j + n) % 30) |> Matrix.ofArray2D

    let ab = LinearAlgebra.mxm a b (*) (+) 0
    let bc = LinearAlgebra.mxm b c (*) (+) 0
    let ab_c = LinearAlgebra.mxm ab c (*) (+) 0
    let a_bc = LinearAlgebra.mxm a bc (*) (+) 0

    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            Assert.Equal(ab_c.data.[i, j], a_bc.data.[i, j])

[<Properties(Arbitrary = [| typeof<StressMatrixGenerators> |], MaxTest = 5)>]
module StressMatrixProperties =

    [<Property>]
    [<Trait("Category", "Stress")>]
    let ``mxm terminates for large random square matrices`` (m: Matrix<int>) =
        let result = LinearAlgebra.mxm m m (*) (+) 0
        result.rows = m.rows && result.cols = m.cols

    [<Property>]
    [<Trait("Category", "Stress")>]
    let ``kron terminates for large random square matrices`` (m: Matrix<int>) =
        let result = LinearAlgebra.kron m m (*) 0
        result.rows = m.rows * m.rows && result.cols = m.cols * m.cols

    [<Property>]
    [<Trait("Category", "Stress")>]
    let ``map2 terminates for large random square matrices`` (m: Matrix<int>) =
        let result = Matrix.map2 (+) m m
        result.rows = m.rows && result.cols = m.cols
