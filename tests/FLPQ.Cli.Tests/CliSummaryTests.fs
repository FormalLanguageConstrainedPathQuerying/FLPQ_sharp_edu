module CliSummaryTests

open System.IO
open Xunit
open FLPQ.Cli

let private baseDir = System.AppContext.BaseDirectory

let private exampleGrammar = Path.Combine(baseDir, "example_grammar.bnf")
let private exampleInput = Path.Combine(baseDir, "example_input.txt")

let private runWithSummary (algorithm: string) : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let args =
        [| "-a"
           algorithm
           "-g"
           exampleGrammar
           "-i"
           exampleInput
           "-o"
           outDir
           "-s" |]

    let code = Program.runCli args
    Assert.Equal(0, code)
    outDir

let private assertMergedTexExists (outDir: string) (algorithm: string) =
    let algoLower = algorithm.ToLower()

    let texPath =
        Path.Combine(outDir, "results", algoLower, sprintf "%s_merged.tex" algoLower)

    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)
    Assert.True(FileInfo(texPath).Length > 0L, sprintf "Merged TeX is empty: %s" texPath)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CYK summary produces merged TeX`` () =
    let outDir = runWithSummary "CYK"
    assertMergedTexExists outDir "CYK"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``Valiant summary produces merged TeX`` () =
    let outDir = runWithSummary "Valiant"
    assertMergedTexExists outDir "Valiant"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary produces merged TeX`` () =
    let outDir = runWithSummary "LL"
    assertMergedTexExists outDir "LL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``SLR(1) summary produces merged TeX`` () =
    let outDir = runWithSummary "SLR1"
    assertMergedTexExists outDir "SLR1"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LR(0) summary produces merged TeX`` () =
    let outDir = runWithSummary "LR0"
    assertMergedTexExists outDir "LR0"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CLR(1) summary produces merged TeX`` () =
    let outDir = runWithSummary "CLR1"
    assertMergedTexExists outDir "CLR1"

let private runWithSummaryEBNF (algorithm: string) (grammarText: string) (inputText: string) : string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)

    let args =
        [| "-a"; algorithm; "-g"; grammarFile; "-i"; inputFile; "-o"; outDir; "-s" |]

    let code = Program.runCli args
    Assert.Equal(0, code)
    outDir

[<Fact>]
[<Trait("Category", "Summary")>]
let ``GLL summary produces merged TeX`` () =
    let outDir = runWithSummaryEBNF "GLL" "S -> a S b | eps" "a a b b"
    assertMergedTexExists outDir "GLL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary produces merged TeX`` () =
    let outDir = runWithSummaryEBNF "RNGLR" "S -> a S b | eps" "a a b b"
    assertMergedTexExists outDir "RNGLR"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ValiantModified summary produces merged TeX`` () =
    let outDir = runWithSummary "ValiantModified"
    assertMergedTexExists outDir "ValiantModified"
