module LRVisualizerTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

let private symbolPrinter = SymbolTeX.toLaTeX string string

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LR step visualization for SLR(1) grammar3 produces valid combined DOT and TeX`` () =
    let g = LanguageRegistry.APlus.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "a a"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.TreeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.Input)
        let info = ExternalTools.compileDotStringToInfo step.TreeAndStack
        Assert.True(info.NodeCount > 0)

    Assert.True(vizSteps |> List.exists (fun s -> s.TreeAndStack.Contains("{rank=same")))

[<Fact>]
let ``LR step visualization includes input position marker`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "a a b a b b"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.True(vizSteps |> List.exists (fun s -> s.Input.Contains(@"\underbar{")))

[<Fact>]
let ``LR step visualization for accepted string returns success steps`` () =
    let g = LanguageRegistry.ArithExpr.Grammars.[1].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "x add x"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.TreeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.Input)

[<Fact>]
let ``LR step visualization includes state frames with sN labels`` () =
    let g = LanguageRegistry.APlus.Grammars.[0].Grammar

    let freshStart = Nonterminal(g.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart g
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol
    let tokens = Tokenizer.tokenizeTerminals "a a"
    let _, steps = LRParser.parseWithSteps aug table tokens
    let vizSteps = LRStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)
    let firstStep = vizSteps.[0]
    Assert.Contains("s0", firstStep.TreeAndStack)
    Assert.Contains("{rank=same", firstStep.TreeAndStack)
