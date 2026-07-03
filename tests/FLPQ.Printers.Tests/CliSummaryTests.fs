module CliSummaryTests

open System.IO
open Xunit
open FLPQ.Cli

/// End-to-end tests for the CLI summary generation (`--summary` flag).
/// These tests invoke `Program.runCli` directly with a temp output directory
/// and assert that the final visualization PDF is produced and non-empty.
/// They require both `dot` (Graphviz) and `pdflatex` to be installed.

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

let private assertPdfExists (outDir: string) (algorithm: string) =
    let algoLower = algorithm.ToLower()

    let pdfPath =
        Path.Combine(outDir, "results", algoLower, sprintf "%s_visualization.pdf" algoLower)

    Assert.True(File.Exists pdfPath, sprintf "Expected PDF not found: %s" pdfPath)
    Assert.True(FileInfo(pdfPath).Length > 0L, sprintf "PDF is empty: %s" pdfPath)

[<Fact>]
[<Trait("Category", "Summary")>]
let ``CYK summary produces visualization PDF`` () =
    let outDir = runWithSummary "CYK"
    assertPdfExists outDir "CYK"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``Valiant summary produces visualization PDF`` () =
    let outDir = runWithSummary "Valiant"
    assertPdfExists outDir "Valiant"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LL summary produces visualization PDF`` () =
    let outDir = runWithSummary "LL"
    assertPdfExists outDir "LL"

[<Fact>]
[<Trait("Category", "Summary")>]
let ``LR summary produces visualization PDF`` () =
    let outDir = runWithSummary "LR"
    assertPdfExists outDir "LR"
