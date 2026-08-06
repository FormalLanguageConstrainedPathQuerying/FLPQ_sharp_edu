module LRRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests

let private baseDir = System.AppContext.BaseDirectory

let private exampleInput = Path.Combine(baseDir, "example_lr_input.txt")

let private runRunner (algo: AlgorithmTypes.Algorithm) (useDot: bool) : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    LRRunner.runLR (TestGrammarFiles.exampleLRGrammar ()) exampleInput outDir algo useDot
    outDir

[<Fact>]
let ``runLR with LR0 produces grammar_original.tex`` () =
    let outDir = runRunner AlgorithmTypes.LR0 false
    let grammarTex = Path.Combine(outDir, "grammar_original.tex")
    Assert.True(File.Exists grammarTex)
    Assert.True(FileInfo(grammarTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLR with LR0 produces lr_table.tex`` () =
    let outDir = runRunner AlgorithmTypes.LR0 false
    let tableTex = Path.Combine(outDir, "lr_table.tex")
    Assert.True(File.Exists tableTex)
    Assert.True(FileInfo(tableTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLR with LR0 produces lr_automaton.tikz.tex in tikz mode`` () =
    let outDir = runRunner AlgorithmTypes.LR0 false
    let tikzTex = Path.Combine(outDir, "lr_automaton.tikz.tex")
    Assert.True(File.Exists tikzTex, sprintf "lr_automaton.tikz.tex missing in %s" outDir)
    Assert.True(FileInfo(tikzTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLR with LR0 produces lr_automaton.dot in dot mode`` () =
    let outDir = runRunner AlgorithmTypes.LR0 true
    let dotFile = Path.Combine(outDir, "lr_automaton.dot")
    Assert.True(File.Exists dotFile, sprintf "lr_automaton.dot missing in %s" outDir)
    Assert.True(FileInfo(dotFile).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLR with LR0 produces step directories with tree_and_stack.dot`` () =
    let outDir = runRunner AlgorithmTypes.LR0 false

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

[<Fact>]
let ``runLR with SLR1 succeeds`` () =
    let outDir = runRunner AlgorithmTypes.SLR1 false

    let stepDirs =
        Directory.GetDirectories outDir
        |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))

    Assert.NotEmpty(stepDirs)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runLR with CLR1 succeeds`` () =
    let outDir = runRunner AlgorithmTypes.CLR1 false

    let stepDirs =
        Directory.GetDirectories outDir
        |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))

    Assert.NotEmpty(stepDirs)
    Directory.Delete(outDir, true)
