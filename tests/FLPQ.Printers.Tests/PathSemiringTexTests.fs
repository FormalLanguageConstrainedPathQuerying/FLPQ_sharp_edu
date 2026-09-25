module PathSemiringTexTests

open System.IO
open Xunit
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

let private adjustboxTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_adjustbox_template.tex")

// --- pathToTeX / pathSetCellToTeX ---

[<Fact>]
let ``pathToTeX renders a vertex tuple`` () =
    Assert.Equal("(v_0, v_2, v_3)", PathSemiringTeX.pathToTeX [ 0; 2; 3 ])
    Assert.Equal("(v_1)", PathSemiringTeX.pathToTeX [ 1 ])

[<Fact>]
let ``pathSetCellToTeX: empty set renders as \cdot`` () =
    Assert.Equal(@"\cdot", PathSemiringTeX.pathSetCellToTeX Set.empty)

[<Fact>]
let ``pathSetCellToTeX: single path`` () =
    Assert.Equal(@"\{(v_0, v_1)\}", PathSemiringTeX.pathSetCellToTeX (Set.singleton [ 0; 1 ]))

[<Fact>]
let ``pathSetCellToTeX: multiple paths in set order`` () =
    let cell = Set.ofList [ [ 0; 2 ]; [ 0; 1 ] ]
    Assert.Equal(@"\{(v_0, v_1), (v_0, v_2)\}", PathSemiringTeX.pathSetCellToTeX cell)

// --- matrixToTeX ---

[<Fact>]
let ``matrixToTeX contains pNiceMatrix, adjustbox, vertex headers and \cdot`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if (i, j) = (0, 1) then Set.ofList [ [ 0; 1 ]; [ 0; 2; 1 ] ]
            elif (i, j) = (2, 0) then Set.singleton [ 2; 0 ]
            else Set.empty)

    let tex =
        PathSemiringTeX.matrixToTeX (fun i -> sprintf "v_%d" i) (fun j -> sprintf "v_%d" j) m

    Assert.Contains("pNiceMatrix", tex)
    Assert.Contains("adjustbox", tex)
    Assert.Contains(@"\cdot", tex)
    Assert.Contains("(v_0, v_1)", tex)
    Assert.Contains("(v_0, v_2, v_1)", tex)
    Assert.Contains("v_0", tex)
    Assert.Contains("v_2", tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``matrixToTeX of a 3x3 path matrix compiles with lualatex`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if (i, j) = (0, 1) then Set.ofList [ [ 0; 1 ]; [ 0; 2; 1 ] ]
            elif (i, j) = (1, 2) then Set.singleton [ 1; 2 ]
            elif (i, j) = (2, 0) then Set.singleton [ 2; 0 ]
            else Set.empty)

    let tex =
        PathSemiringTeX.matrixToTeX (fun i -> sprintf "v_%d" i) (fun j -> sprintf "v_%d" j) m

    Assert.True(ExternalTools.compileTexStringWithTemplate adjustboxTemplatePath tex)

// --- matrixWithStateVertexLabels / boolMatrixToTeX ---

[<Fact>]
let ``matrixWithStateVertexLabels uses q_i row and v_i column headers`` () =
    let m =
        Matrix.create 2 3 (fun i j ->
            if (i, j) = (0, 1) then
                Set.singleton [ 0; 1 ]
            else
                Set.empty)

    let tex = PathSemiringTeX.matrixWithStateVertexLabels m
    let lines = tex.Split('\n') |> Array.map (fun l -> l.TrimEnd())

    Assert.Contains(@" & v_0 & v_1 & v_2 \\", lines)
    Assert.Contains(@"q_0 & \cdot & \{(v_0, v_1)\} & \cdot \\", lines)
    Assert.Contains(@"q_1 & \cdot & \cdot & \cdot \\", lines)

[<Fact>]
let ``boolMatrixToTeX renders true as \bullet and false as \cdot with labels`` () =
    // Off-diagonal matrix: true exactly where i <> j.
    let m = Matrix.init 2 2 false
    m.[0, 1] <- true
    m.[1, 0] <- true

    let tex =
        PathSemiringTeX.boolMatrixToTeX (fun i -> sprintf "q_%d" i) (fun j -> sprintf "v_%d" j) m

    let lines = tex.Split('\n') |> Array.map (fun l -> l.TrimEnd())

    Assert.Contains("pNiceMatrix", tex)
    Assert.Contains("adjustbox", tex)
    Assert.Contains(@" & v_0 & v_1 \\", lines)
    Assert.Contains(@"q_0 & \cdot & \bullet \\", lines)
    Assert.Contains(@"q_1 & \bullet & \cdot \\", lines)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``boolMatrixToTeX of a 3x3 boolean matrix compiles with lualatex`` () =
    let m = Matrix.create 3 3 (fun i j -> i + j < 3)

    let tex =
        PathSemiringTeX.boolMatrixToTeX (fun i -> sprintf "q_%d" i) (fun j -> sprintf "q_%d" j) m

    Assert.True(ExternalTools.compileTexStringWithTemplate adjustboxTemplatePath tex)
