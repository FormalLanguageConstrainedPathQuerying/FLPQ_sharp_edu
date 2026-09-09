module CliSummaryTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests

let private baseDir = System.AppContext.BaseDirectory

let private exampleInput = Path.Combine(baseDir, "example_input.txt")

let private runWithSummary (algorithm: string) (useDot: bool) : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let dotFlag = if useDot then [ "--use-dot" ] else []

    let args =
        Array.append
            [| "-a"
               algorithm
               "-g"
               TestGrammarFiles.exampleGrammar ()
               "-i"
               exampleInput
               "-o"
               outDir
               "-s" |]
            (Array.ofList dotFlag)

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
    let outDir = runWithSummary "CYK" false
    assertMergedTexExists outDir "CYK"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``Valiant summary produces merged TeX`` () =
    let outDir = runWithSummary "Valiant" false
    assertMergedTexExists outDir "Valiant"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary produces merged TeX`` () =
    let outDir = runWithSummary "LL" false
    assertMergedTexExists outDir "LL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary default mode embeds step stack-trees as inline TikZ`` () =
    let outDir = runWithSummary "LL" false
    let texPath = Path.Combine(outDir, "results", "ll", "ll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains(@"\graph [layered layout", content)
    Assert.Contains("{ [same layer]", content)
    Assert.DoesNotContain("dot_pdfs/step_", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary useDot mode includes step stack-tree PDFs`` () =
    let outDir = runWithSummary "LL" true
    let texPath = Path.Combine(outDir, "results", "ll", "ll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("dot_pdfs/step_0_tree_and_stack.pdf", content)
    Assert.DoesNotContain(@"\graph [layered layout", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``SLR(1) summary produces merged TeX`` () =
    let outDir = runWithSummary "SLR1" false
    assertMergedTexExists outDir "SLR1"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LR(0) summary produces merged TeX`` () =
    let outDir = runWithSummary "LR0" false
    assertMergedTexExists outDir "LR0"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CLR(1) summary produces merged TeX`` () =
    let outDir = runWithSummary "CLR1" false
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
let ``GLL summary color legend includes stored-pops orange row`` () =
    let outDir = runWithSummaryEBNF "GLL" "S -> a S b | eps" "a a b b"
    let texPath = Path.Combine(outDir, "results", "gll", "gll_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("Stored pops handling triggered at GSS vertex", content)
    Assert.Contains(@"\colorbox{orange!30}", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary produces merged TeX`` () =
    let outDir = runWithSummaryEBNF "RNGLR" "S -> a S b | eps" "a a b b"
    assertMergedTexExists outDir "RNGLR"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``RNGLR summary color legend includes passing-reductions orange row`` () =
    let outDir = runWithSummaryEBNF "RNGLR" "S -> a S b | eps" "a a b b"
    let texPath = Path.Combine(outDir, "results", "rnglr", "rnglr_merged.tex")
    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    let content = File.ReadAllText texPath
    Assert.Contains("Passing reductions handling triggered at GSS vertex", content)
    Assert.Contains(@"\colorbox{orange!30}", content)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``ValiantModified summary produces merged TeX`` () =
    let outDir = runWithSummary "ValiantModified" false
    assertMergedTexExists outDir "ValiantModified"
