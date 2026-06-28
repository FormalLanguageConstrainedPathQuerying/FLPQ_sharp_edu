module LRVisualizerTests

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
let ``LR step visualization for SLR(1) grammar3 produces valid dot and TeX`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S
        S -> a
        "

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug
    let tokens = Tokenizer.tokenize "a a"
    let steps = LRVisualizer.visualizeSteps string aug table tokens

    Assert.NotEmpty(steps)

    for step in steps do
        Assert.Contains(@"\begin{pNiceMatrix}", step.stack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
        Assert.True(checkDotCompiles step.tree)

[<Fact>]
let ``LR step visualization includes input position marker`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug
    let tokens = Tokenizer.tokenize "a a b a b b"
    let steps = LRVisualizer.visualizeSteps string aug table tokens

    Assert.True(steps |> List.exists (fun s -> s.input.Contains(@"\underbar{")))

[<Fact>]
let ``LR step visualization for accepted string returns success steps`` () =
    let g =
        Grammar.parseGrammar
            "
        E -> E + T
        E -> T
        T -> T * F
        T -> F
        F -> ( E )
        F -> x
        "

    let freshStart = Nonterminal(g.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug
    let tokens = Tokenizer.tokenize "x + x"
    let steps = LRVisualizer.visualizeSteps string aug table tokens

    Assert.NotEmpty(steps)

    for step in steps do
        Assert.Contains(@"\begin{pNiceMatrix}", step.stack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
