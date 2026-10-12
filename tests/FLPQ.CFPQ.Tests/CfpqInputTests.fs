module CfpqInputTests

open System
open System.IO
open FSharpPlus.Data
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.CFPQ
open FLPQ.TestUtilities

let private mtxHeader = "%%MatrixMarket matrix coordinate pattern general\n"

[<Fact>]
let ``parses a flpq-data style MatrixMarket file with comment lines`` () =
    let text =
        mtxHeader
        + "%%GraphBLAS type bool\n"
        + "4 4 5\n"
        + "0 1\n"
        + "1 2\n"
        + "2 0\n"
        + "0 3\n"
        + "3 0\n"

    let m = CfpqInput.parseMatrixMarketText text
    Assert.Equal(4, Matrix.rows m)
    Assert.Equal(4, Matrix.cols m)
    let expected = Set.ofList [ (0, 1); (1, 2); (2, 0); (0, 3); (3, 0) ]
    let matches = CfpqTestHelpers.trueCells m = expected
    Assert.True(matches, $"expected {expected}, got {CfpqTestHelpers.trueCells m}")

[<Fact>]
let ``parses an empty matrix`` () =
    let text = mtxHeader + "%%GraphBLAS type bool\n" + "129 129 0\n"

    let m = CfpqInput.parseMatrixMarketText text
    Assert.Equal(129, Matrix.rows m)
    Assert.Equal(129, Matrix.cols m)
    let isEmpty = CfpqTestHelpers.trueCellCount m = 0
    Assert.True(isEmpty)

[<Fact>]
let ``duplicate entries collapse in the cell set`` () =
    let text = mtxHeader + "2 2 3\n" + "0 1\n" + "0 1\n" + "1 0\n"

    let m = CfpqInput.parseMatrixMarketText text
    let count = CfpqTestHelpers.trueCellCount m
    Assert.Equal(2, count)

[<Fact>]
let ``missing MatrixMarket header throws`` () =
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseMatrixMarketText "4 4 0\n" |> ignore)

[<Fact>]
let ``unsupported MatrixMarket header throws`` () =
    let text =
        "%%MatrixMarket matrix array dense general\n" + "2 2\n" + "1 0\n" + "0 1\n"

    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseMatrixMarketText text |> ignore)

[<Fact>]
let ``missing dimensions line throws`` () =
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseMatrixMarketText mtxHeader |> ignore)

[<Fact>]
let ``declared nnz mismatch throws`` () =
    let text = mtxHeader + "2 2 3\n" + "0 1\n" + "1 0\n"
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseMatrixMarketText text |> ignore)

[<Fact>]
let ``out-of-bounds entry throws`` () =
    let text = mtxHeader + "2 2 1\n" + "0 5\n"
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseMatrixMarketText text |> ignore)

[<Fact>]
let ``parses a graph directory with one matrix per label`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(
            Path.Combine(dir, "a.mtx"),
            mtxHeader + "%%GraphBLAS type bool\n" + "4 4 3\n" + "0 1\n" + "1 2\n" + "2 0\n"
        )
        |> ignore

        File.WriteAllText(
            Path.Combine(dir, "b.mtx"),
            mtxHeader + "%%GraphBLAS type bool\n" + "4 4 2\n" + "0 3\n" + "3 0\n"
        )
        |> ignore

        let g = CfpqInput.parseGraphDir dir
        Assert.Equal(4, g.VertexCount)
        let labels = Map.keys g.Labels |> Seq.toList |> List.sort
        Assert.Equal<string list>([ "a"; "b" ], labels)
        let a = Map.find "a" g.Labels
        let b = Map.find "b" g.Labels
        let aOk = a.[0, 1] && a.[1, 2] && a.[2, 0] && not a.[0, 0] && not a.[3, 0]
        let bOk = b.[0, 3] && b.[3, 0] && not b.[0, 1]
        Assert.True(aOk, "label a matrix is wrong")
        Assert.True(bOk, "label b matrix is wrong"))

[<Fact>]
let ``graph directory with disagreeing dimensions throws`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "a.mtx"), mtxHeader + "4 4 0\n") |> ignore
        File.WriteAllText(Path.Combine(dir, "b.mtx"), mtxHeader + "3 3 0\n") |> ignore
        Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseGraphDir dir |> ignore))

[<Fact>]
let ``missing graph directory throws`` () =
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseGraphDir "/nonexistent/cfpq-graph" |> ignore)

[<Fact>]
let ``graph directory without mtx files throws`` () =
    TestHelpers.withTempDir (fun dir ->
        File.WriteAllText(Path.Combine(dir, "readme.txt"), "no matrices here")
        Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseGraphDir dir |> ignore))

[<Fact>]
let ``parses the book grammar in cnf format`` () =
    // Book Chapter 12, 02_MatrixBased.tex worked example: S -> AB | AS1, A -> a, B -> b, S1 -> SB
    let text =
        "S\tA\tB\n"
        + "S\tA\tN1\n"
        + "A\ta\n"
        + "B\tb\n"
        + "N1\tS\tB\n"
        + "\n"
        + "Count:\n"
        + "S\n"

    let g = CfpqInput.parseCnfText text
    Assert.Equal(Nonterminal "S", g.Start)
    Assert.Equal(5, List.length g.Rules)

    let hasSAB =
        g.Rules
        |> List.exists (fun r ->
            r.Lhs = Nonterminal "S"
            && match r.Rhs with
               | Rhs.Symbols nel ->
                   let syms = NonEmptyList.toList nel
                   syms = [ Symbol.N(Nonterminal "A"); Symbol.N(Nonterminal "B") ]
               | _ -> false)

    let hasATerminal =
        g.Rules
        |> List.exists (fun r ->
            r.Lhs = Nonterminal "A"
            && match r.Rhs with
               | Rhs.Symbols nel ->
                   let syms = NonEmptyList.toList nel
                   syms = [ Symbol.T(Terminal "a") ]
               | _ -> false)

    Assert.True(hasSAB, "missing S -> A B")
    Assert.True(hasATerminal, "missing A -> a")

[<Fact>]
let ``bare lhs line is an epsilon rule`` () =
    let text = "S\tA\n" + "A\n" + "\n" + "Count:\n" + "S\n"

    let g = CfpqInput.parseCnfText text

    let hasEpsilon =
        g.Rules |> List.exists (fun r -> r.Lhs = Nonterminal "A" && Rhs.isEpsilon r.Rhs)

    Assert.True(hasEpsilon, "missing A -> eps")

[<Fact>]
let ``cnf without Count trailer throws`` () =
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseCnfText "S\ta\n" |> ignore)

[<Fact>]
let ``cnf without start symbol throws`` () =
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseCnfText "S\ta\n\nCount:\n" |> ignore)

[<Fact>]
let ``cnf with indexed placeholder throws`` () =
    let text = "S\tload_i\tN1\n" + "N1\tstore_i\n" + "\n" + "Count:\n" + "S\n"
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseCnfText text |> ignore)

[<Fact>]
let ``cnf with non-nonterminal start symbol throws`` () =
    let text = "S\ta\n" + "\n" + "Count:\n" + "a\n"
    Assert.Throws<ArgumentException>(fun () -> CfpqInput.parseCnfText text |> ignore)

[<Fact>]
let ``cnf accepts whitespace-separated symbols`` () =
    let text = "S A B\nA a\nB b\n\nCount:\nS\n"

    let g = CfpqInput.parseCnfText text
    Assert.Equal(3, List.length g.Rules)

[<Fact>]
let ``parses a reference results file`` () =
    TestHelpers.withTempDir (fun dir ->
        let path = Path.Combine(dir, "results.mtx")

        File.WriteAllText(
            path,
            mtxHeader
            + "%%GraphBLAS type bool\n"
            + "129 129 12\n"
            + "9 9\n"
            + "13 13\n"
            + "21 21\n"
            + "64 64\n"
            + "64 112\n"
            + "64 128\n"
            + "76 76\n"
            + "88 88\n"
            + "112 64\n"
            + "112 112\n"
            + "128 64\n"
            + "128 128\n"
        )

        let m = CfpqInput.parseResultsFile path
        Assert.Equal(129, Matrix.rows m)
        Assert.Equal(129, Matrix.cols m)

        let trueCells =
            [ for i in 0..128 do
                  for j in 0..128 do
                      if m.[i, j] then
                          (i, j) ]

        let expected =
            [ (9, 9)
              (13, 13)
              (21, 21)
              (64, 64)
              (64, 112)
              (64, 128)
              (76, 76)
              (88, 88)
              (112, 64)
              (112, 112)
              (128, 64)
              (128, 128) ]
            |> Set.ofList

        let matches = trueCells |> Set.ofList = expected
        Assert.True(matches, $"expected {expected}, got {Set.ofList trueCells}"))
