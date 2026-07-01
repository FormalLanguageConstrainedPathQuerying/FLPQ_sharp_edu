module LRVisualizerTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers


[<Fact>]
[<Trait("Category", "Graphviz")>]
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
    let tokens = Tokenizer.tokenizeTerminals "a a"
    let steps = LRStepVisualizer.visualizeSteps string aug table tokens

    Assert.NotEmpty(steps)

    for step in steps do
        Assert.Contains(@"\begin{pNiceMatrix}", step.stack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
        let info = TestUtils.checkDotCompilesWithInfo step.tree
        Assert.True(info.nodeCount > 0)

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
    let tokens = Tokenizer.tokenizeTerminals "a a b a b b"
    let steps = LRStepVisualizer.visualizeSteps string aug table tokens

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
    let tokens = Tokenizer.tokenizeTerminals "x + x"
    let steps = LRStepVisualizer.visualizeSteps string aug table tokens

    Assert.NotEmpty(steps)

    for step in steps do
        Assert.Contains(@"\begin{pNiceMatrix}", step.stack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
