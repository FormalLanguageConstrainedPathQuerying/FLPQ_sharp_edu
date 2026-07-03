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
let ``LR summary produces merged TeX`` () =
    let outDir = runWithSummary "LR"
    assertMergedTexExists outDir "LR"
