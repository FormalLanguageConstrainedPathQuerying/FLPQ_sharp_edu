module MatrixTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.Core

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

type MatrixGenerators =

    static member Matrix() : Arbitrary<Matrix<int>> =
        MyGen.choose (1, 5)
        |> MyGen.bind (fun rows ->
            MyGen.choose (1, 5)
            |> MyGen.bind (fun cols ->
                MyGen.choose (-100, 100)
                |> MyGen.listOfLength (rows * cols)
                |> MyGen.map (fun values ->
                    let arr = Array2D.init rows cols (fun i j -> values.[i * cols + j])

                    Matrix.ofArray2D arr)))
        |> MyArb.fromGen

    static member SameDimMatrixPair() : Arbitrary<Matrix<int> * Matrix<int>> =
        MyGen.choose (1, 5)
        |> MyGen.bind (fun rows ->
            MyGen.choose (1, 5)
            |> MyGen.bind (fun cols ->
                MyGen.choose (-100, 100)
                |> MyGen.listOfLength (rows * cols)
                |> MyGen.bind (fun valuesA ->
                    let arrA = Array2D.init rows cols (fun i j -> valuesA.[i * cols + j])

                    MyGen.choose (-100, 100)
                    |> MyGen.listOfLength (rows * cols)
                    |> MyGen.map (fun valuesB ->
                        let arrB = Array2D.init rows cols (fun i j -> valuesB.[i * cols + j])

                        (Matrix.ofArray2D arrA, Matrix.ofArray2D arrB)))))

        |> MyArb.fromGen

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
        result.rows = m.rows && result.cols = m.cols

    [<Property>]
    let ``transpose swaps dimensions`` (m: Matrix<int>) =
        let result = Matrix.transpose m
        result.rows = m.cols && result.cols = m.rows

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

        Assert.Equal(0 * 1000 + 0, m.data.[0, 0])
        Assert.Equal(0 * 1000 + 1, m.data.[0, 1])
        Assert.Equal(1 * 1000 + 0, m.data.[1, 0])
        Assert.Equal(1 * 1000 + 1, m.data.[1, 1])
        Assert.Equal(2 * 1000 + 0, m.data.[2, 0])
        Assert.Equal(2 * 1000 + 1, m.data.[2, 1])

    [<Fact>]
    let ``init fills all cells with the same value`` () =
        let m = Matrix.init 3 2 42

        for i in 0..2 do
            for j in 0..1 do
                Assert.Equal(42, m.data.[i, j])

    [<Fact>]
    let ``toTeX wraps in pNiceMatrix environment`` () =
        let m = Matrix.init 2 2 1
        let tex = Matrix.toTeX false false string m
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.Contains(@"\end{pNiceMatrix}", tex)

    [<Fact>]
    let ``toTeX with row numbers produces extra column`` () =
        let m = Matrix.init 2 2 1
        let tex = Matrix.toTeX true false string m
        let firstLine = tex.Split('\n').[1]
        Assert.Contains("1 &", firstLine)

    [<Fact>]
    let ``toTeX with column numbers produces extra row`` () =
        let m = Matrix.init 2 2 42
        let tex = Matrix.toTeX false true string m
        let lines = tex.Split('\n') |> Array.filter (fun l -> l.TrimEnd().EndsWith(@"\\"))
        Assert.Equal(3, lines.Length)
        Assert.Contains("1 & 2", tex.ReplaceLineEndings().Trim())

    [<Fact>]
    let ``toTeX produces correct cell content`` () =
        let m = Matrix.create 2 2 (fun i j -> i * 10 + j)
        let tex = Matrix.toTeX false false string m
        Assert.Contains("0 & 1", tex.ReplaceLineEndings().Trim())
        Assert.Contains("10 & 11", tex.ReplaceLineEndings().Trim())

    [<Fact>]
    let ``transpose of 2x3 produces 3x2`` () =
        let m = Matrix.create 2 3 (fun i j -> i * 10 + j)
        let t = Matrix.transpose m
        Assert.Equal(3, t.rows)
        Assert.Equal(2, t.cols)
        Assert.Equal(m.data.[0, 0], t.data.[0, 0])
        Assert.Equal(m.data.[0, 1], t.data.[1, 0])
        Assert.Equal(m.data.[0, 2], t.data.[2, 0])
        Assert.Equal(m.data.[1, 0], t.data.[0, 1])
        Assert.Equal(m.data.[1, 1], t.data.[1, 1])
        Assert.Equal(m.data.[1, 2], t.data.[2, 1])
