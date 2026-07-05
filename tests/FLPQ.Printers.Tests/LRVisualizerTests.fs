module LRVisualizerTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

let private symbolPrinter = SymbolTeX.toLaTeX string string

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LR step visualization for SLR(1) grammar3 produces valid combined DOT and TeX`` () =
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
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.treeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
        let info = ExternalTools.compileDotStringToInfo step.treeAndStack
        Assert.True(info.nodeCount > 0)

    Assert.True(vizSteps |> List.exists (fun s -> s.treeAndStack.Contains("{rank=same")))

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
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.True(vizSteps |> List.exists (fun s -> s.input.Contains(@"\underbar{")))

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
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.treeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)

[<Fact>]
let ``LR step visualization includes state frames with sN labels`` () =
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
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)
    let firstStep = vizSteps.[0]
    Assert.Contains("s0", firstStep.treeAndStack)
    Assert.Contains("{rank=same", firstStep.treeAndStack)
