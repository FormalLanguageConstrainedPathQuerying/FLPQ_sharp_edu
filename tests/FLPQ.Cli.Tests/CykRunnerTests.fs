module CykRunnerTests

open System.IO
open Xunit
open FLPQ.Cli

let private baseDir = System.AppContext.BaseDirectory

let private exampleGrammar = Path.Combine(baseDir, "example_grammar.bnf")
let private exampleInput = Path.Combine(baseDir, "example_input.txt")

let private runRunner () : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    CykRunner.runCyk exampleGrammar exampleInput outDir
    outDir

[<Fact>]
let ``runCyk produces input.tex`` () =
    let outDir = runRunner ()
    let inputTex = Path.Combine(outDir, "input.tex")
    Assert.True(File.Exists inputTex)
    Assert.True(FileInfo(inputTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces grammar_original.tex`` () =
    let outDir = runRunner ()
    let grammarTex = Path.Combine(outDir, "grammar_original.tex")
    Assert.True(File.Exists grammarTex)
    Assert.True(FileInfo(grammarTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces grammar_cnf.tex`` () =
    let outDir = runRunner ()
    let cnfTex = Path.Combine(outDir, "grammar_cnf.tex")
    Assert.True(File.Exists cnfTex)
    Assert.True(FileInfo(cnfTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces step directories with table.tex`` () =
    let outDir = runRunner ()

    let stepDirs =
        Directory.GetDirectories outDir
        |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))

    Assert.NotEmpty(stepDirs)

    for stepDir in stepDirs do
        let tableTex = Path.Combine(stepDir, "table.tex")
        Assert.True(File.Exists tableTex, sprintf "table.tex missing in %s" stepDir)
        Assert.True(FileInfo(tableTex).Length > 0L)

    Directory.Delete(outDir, true)
