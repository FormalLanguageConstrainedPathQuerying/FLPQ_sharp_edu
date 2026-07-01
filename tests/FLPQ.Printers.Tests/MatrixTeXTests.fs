module MatrixTeXTests

open Xunit
open FLPQ.LinearAlgebra
open FLPQ.Printers


[<Fact>]
let ``toTeX wraps in pNiceMatrix environment`` () =
    let m = Matrix.init 2 2 1
    let tex = MatrixTeX.toTeX false false string m
    Assert.Contains(@"\begin{pNiceMatrix}", tex)
    Assert.Contains(@"\end{pNiceMatrix}", tex)

[<Fact>]
let ``toTeX with row numbers produces extra column`` () =
    let m = Matrix.init 2 2 1
    let tex = MatrixTeX.toTeX true false string m
    let firstLine = tex.Split('\n').[1]
    Assert.Contains("1 &", firstLine)

[<Fact>]
let ``toTeX with column numbers produces extra row`` () =
    let m = Matrix.init 2 2 42
    let tex = MatrixTeX.toTeX false true string m
    let lines = tex.Split('\n') |> Array.filter (fun l -> l.TrimEnd().EndsWith(@"\\"))
    Assert.Equal(3, lines.Length)
    Assert.Contains("1 & 2", tex.ReplaceLineEndings().Trim())

[<Fact>]
let ``toTeX produces correct cell content`` () =
    let m = Matrix.create 2 2 (fun i j -> i * 10 + j)
    let tex = MatrixTeX.toTeX false false string m
    Assert.Contains("0 & 1", tex.ReplaceLineEndings().Trim())
    Assert.Contains("10 & 11", tex.ReplaceLineEndings().Trim())
