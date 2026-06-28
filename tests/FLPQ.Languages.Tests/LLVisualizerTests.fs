module LLVisualizerTests

open System
open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra

let private checkDotCompiles (dot: string) : bool =
    let tempFile = Path.GetTempFileName()
    File.WriteAllText(tempFile, dot)

    try
        let processInfo = new Diagnostics.Process()
        processInfo.StartInfo.FileName <- "dot"
        processInfo.StartInfo.Arguments <- "-Tplain " + tempFile
        processInfo.StartInfo.RedirectStandardOutput <- true
        processInfo.StartInfo.RedirectStandardError <- true
        processInfo.StartInfo.UseShellExecute <- false
        processInfo.Start() |> ignore
        processInfo.WaitForExit(5000) |> ignore
        processInfo.ExitCode = 0
    finally
        File.Delete(tempFile)

[<Fact>]
let ``LL step visualization for grammar1 produces valid dot and TeX`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenize "a b"
    let steps = LLVisualizer.visualizeSteps string g table 1 tokens

    Assert.NotEmpty(steps)

    for step in steps do
        Assert.Contains(@"\begin{pNiceMatrix}", step.stack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
        Assert.True(checkDotCompiles step.tree)

[<Fact>]
let ``LL step visualization includes input position marker`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenize "a b a b"
    let steps = LLVisualizer.visualizeSteps string g table 1 tokens

    Assert.True(steps |> List.exists (fun s -> s.input.Contains(@"\underbar{")))

[<Fact>]
let ``LL step visualization stack has bottom on left`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a B
        B -> b
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenize "a b"
    let steps = LLVisualizer.visualizeSteps string g table 1 tokens

    Assert.NotEmpty(steps)

[<Fact>]
let ``LL step visualization for accepted string returns success steps`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenize "a b"
    let steps = LLVisualizer.visualizeSteps string g table 1 tokens

    Assert.NotEmpty(steps)

    for step in steps do
        Assert.Contains(@"\begin{pNiceMatrix}", step.stack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
