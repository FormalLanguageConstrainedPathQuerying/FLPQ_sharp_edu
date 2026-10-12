module CfpqMatrixTexTests

open Xunit
open FLPQ.CFPQ.AzimovCfpq
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

// --- cellToTeX ---

[<Fact>]
let ``cellToTeX renders None as \varnothing and Some through the element printer`` () =
    Assert.Equal(@"\varnothing", CfpqMatrixTeX.cellToTeX (fun _ -> "x") None)
    Assert.Equal(@"\bullet", CfpqMatrixTeX.cellToTeX (fun _ -> @"\bullet") (Some true))

// --- matrixToTeX ---

[<Fact>]
let ``matrixToTeX renders a pNiceMatrix with v_i labels and the element text`` () =
    let m = Matrix.create 2 2 (fun i j -> if (i, j) = (0, 1) then Some true else None)

    let tex = CfpqMatrixTeX.matrixToTeX (fun _ -> @"\bullet") m

    Assert.Contains("pNiceMatrix", tex)
    Assert.Contains("adjustbox", tex)
    Assert.Contains(@"\bullet", tex)
    Assert.Contains(@"\varnothing", tex)
    Assert.Contains("v_0", tex)
    Assert.Contains("v_1", tex)

// --- pathToText ---

[<Fact>]
let ``pathToText renders the empty sequence as \varepsilon`` () =
    Assert.Equal(@"\varepsilon", CfpqMatrixTeX.pathToText id [])

[<Fact>]
let ``pathToText renders an edge sequence with vertex names and labels`` () =
    let path = [ { From = 2; Label = "a"; To = 0 }; { From = 0; Label = "b"; To = 3 } ]
    Assert.Equal("v_2 -a-> v_0 -b-> v_3", CfpqMatrixTeX.pathToText id path)

[<Fact>]
let ``pathToText uses the label printer for edge labels`` () =
    let path = [ { From = 0; Label = "a"; To = 1 } ]
    Assert.Equal("v_0 -{a}-> v_1", CfpqMatrixTeX.pathToText (fun l -> sprintf "{%s}" l) path)

// --- stepToTeX ---

[<Fact>]
let ``stepToTeX stacks one bold-named matrix per nonterminal with changed cells highlighted`` () =
    let matrices =
        Map.ofList
            [ Nonterminal "A", Matrix.create 2 2 (fun i j -> if (i, j) = (0, 1) then Some true else None)
              Nonterminal "B", Matrix.create 2 2 (fun i j -> if (i, j) = (1, 0) then Some true else None) ]

    let changed =
        [ { Nt = Nonterminal "A"
            Row = 0
            Col = 1 } ]

    let tex =
        CfpqMatrixTeX.stepToTeX (fun (Nonterminal s) -> s) (fun _ -> @"\bullet") matrices changed

    Assert.Contains(@"\textbf{A}", tex)
    Assert.Contains(@"\textbf{B}", tex)
    Assert.Equal(2, tex.Split("\cellcolor{yellow}").Length)

[<Fact>]
let ``stepToTeX without changed cells renders no highlights`` () =
    let matrices =
        Map.ofList [ Nonterminal "A", Matrix.create 1 1 (fun _i _j -> Some true) ]

    let tex =
        CfpqMatrixTeX.stepToTeX (fun (Nonterminal s) -> s) (fun _ -> @"\bullet") matrices []

    Assert.Contains(@"\textbf{A}", tex)
    Assert.DoesNotContain("\cellcolor{yellow}", tex)
