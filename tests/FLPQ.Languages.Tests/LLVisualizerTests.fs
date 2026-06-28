module LLVisualizerTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra


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
        Assert.True(TestUtils.checkDotCompiles step.tree)

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
