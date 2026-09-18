module MatrixTeXTests

open System.IO
open Xunit
open FLPQ.LinearAlgebra
open FLPQ.Printers

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

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

type ``Matrix TeX golden tests``() =

    [<Fact>]
    member _.``matrix 3x3 numeric``() =
        let m = Matrix.create 3 3 (fun i j -> i * 3 + j + 1)
        let tex = MatrixTeX.toTeX false false string m
        verifyGolden "matrix_3x3_numeric.tex" (wrapInTemplate templatePath tex)

    [<Fact>]
    member _.``matrix 2x2 with row and col numbers``() =
        let m = Matrix.create 2 2 (fun i j -> sprintf "x_{%d%d}" i j)
        let tex = MatrixTeX.toTeX true true string m
        verifyGolden "matrix_2x2_labeled.tex" (wrapInTemplate templatePath tex)

    [<Fact>]
    member _.``matrix 4x4 identity pattern``() =
        let m =
            Matrix.create 4 4 (fun i j ->
                if i = j then "1"
                elif i < j then ">"
                else "<")

        let tex = MatrixTeX.toTeX false false string m
        verifyGolden "matrix_4x4_identity_pattern.tex" (wrapInTemplate templatePath tex)


module MatrixBlockHighlightTests =

    [<Fact>]
    let ``toTeXStyled renders Submatrix blocks with per-index colors via \Block`` () =
        let m = Matrix.create 3 3 (fun i j -> sprintf "c%d%d" i j)

        let blocks: Matrix.SubmatrixBlock list =
            [ { StartRow = 0
                StartCol = 0
                RowCount = 2
                ColCount = 2
                Label = Matrix.Submatrix 0 }
              { StartRow = 1
                StartCol = 1
                RowCount = 2
                ColCount = 2
                Label = Matrix.Submatrix 1 }
              // index 12 wraps around the 10-color palette to green
              { StartRow = 0
                StartCol = 1
                RowCount = 1
                ColCount = 1
                Label = Matrix.Submatrix 12 } ]

        let tex = MatrixTeX.toTeXStyled false false string m [] blocks None None false false

        Assert.Contains(@"\Block[draw=red,fill=red!20]{2-2}", tex)
        Assert.Contains(@"\Block[draw=blue,fill=blue!20]{2-2}", tex)
        Assert.Contains(@"\Block[draw=green,fill=green!20]{1-1}", tex)

    [<Fact>]
    let ``toTeXStyled renders CurrentStepSubmatrix block with red outline`` () =
        let m = Matrix.create 2 2 (fun i j -> sprintf "c%d%d" i j)

        let blocks: Matrix.SubmatrixBlock list =
            [ { StartRow = 0
                StartCol = 0
                RowCount = 2
                ColCount = 2
                Label = Matrix.CurrentStepSubmatrix } ]

        let tex = MatrixTeX.toTeXStyled false false string m [] blocks None None false false

        Assert.Contains(@"\Block[draw=red,fill=red!10]{2-2}", tex)

    [<Fact>]
    let ``toTeXStyled block overlapping a highlighted cell nests \cellcolor inside \Block`` () =
        let m = Matrix.create 3 3 (fun i j -> sprintf "c%d%d" i j)

        let blocks: Matrix.SubmatrixBlock list =
            [ { StartRow = 0
                StartCol = 0
                RowCount = 2
                ColCount = 2
                Label = Matrix.Submatrix 0 } ]

        let highlights: Matrix.Highlight list =
            [ { Row = 0
                Col = 0
                Label = Matrix.CurrentCell } ]

        let tex =
            MatrixTeX.toTeXStyled false false string m highlights blocks None None false false

        Assert.Contains(@"\Block[draw=red,fill=red!20]{2-2}{\cellcolor{yellow}{c00}}", tex)
