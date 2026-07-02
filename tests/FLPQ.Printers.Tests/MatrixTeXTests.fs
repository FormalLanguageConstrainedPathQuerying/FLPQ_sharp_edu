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
    Assert.Contains("first-col", tex)
    Assert.Contains(@"\begin{pNiceMatrix}", tex)

[<Fact>]
let ``toTeX with column numbers produces extra row`` () =
    let m = Matrix.init 2 2 42
    let tex = MatrixTeX.toTeX false true string m
    Assert.Contains("first-row", tex)
    Assert.Contains("code-for-first-row", tex)
    Assert.Contains(@"\begin{pNiceMatrix}", tex)

[<Fact>]
let ``toTeX produces correct cell content`` () =
    let m = Matrix.create 2 2 (fun i j -> i * 10 + j)
    let tex = MatrixTeX.toTeX false false string m
    Assert.Contains("0 & 1", tex.ReplaceLineEndings().Trim())
    Assert.Contains("10 & 11", tex.ReplaceLineEndings().Trim())
