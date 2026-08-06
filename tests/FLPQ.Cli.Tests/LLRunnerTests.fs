module LLRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests

let private baseDir = System.AppContext.BaseDirectory

let private exampleInput = Path.Combine(baseDir, "example_input.txt")

let private runRunner () : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    LLRunner.runLL (TestGrammarFiles.exampleGrammar ()) exampleInput outDir 1
    outDir

[<Fact>]
let ``runLL produces grammar_original.tex`` () =
    let outDir = runRunner ()
    let grammarTex = Path.Combine(outDir, "grammar_original.tex")
    Assert.True(File.Exists grammarTex)
    Assert.True(FileInfo(grammarTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLL produces ll_table.tex`` () =
    let outDir = runRunner ()
    let tableTex = Path.Combine(outDir, "ll_table.tex")
    Assert.True(File.Exists tableTex)
    Assert.True(FileInfo(tableTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLL produces step directories with tree_and_stack.dot and input.tex`` () =
    let outDir = runRunner ()

    let stepDirs =
        Directory.GetDirectories outDir
        |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))

    Assert.NotEmpty(stepDirs)

    for stepDir in stepDirs do
        let treeDot = Path.Combine(stepDir, "tree_and_stack.dot")
        Assert.True(File.Exists treeDot, sprintf "tree_and_stack.dot missing in %s" stepDir)
        Assert.True(FileInfo(treeDot).Length > 0L)

        let inputTex = Path.Combine(stepDir, "input.tex")
        Assert.True(File.Exists inputTex, sprintf "input.tex missing in %s" stepDir)
        Assert.True(FileInfo(inputTex).Length > 0L)

    Directory.Delete(outDir, true)
