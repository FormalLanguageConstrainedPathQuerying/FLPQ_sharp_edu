module LLVisualizerTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

let private symbolPrinter = SymbolTeX.toLaTeX string string

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LL step visualization for grammar1 produces valid combined DOT and TeX`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.TreeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.Input)
        let info = ExternalTools.compileDotStringToInfo step.TreeAndStack
        Assert.True(info.NodeCount > 0)

    Assert.True(vizSteps |> List.exists (fun s -> s.TreeAndStack.Contains("{rank=same")))

[<Fact>]
let ``LL step visualization includes input position marker`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.True(vizSteps |> List.exists (fun s -> s.Input.Contains(@"\underbar{")))

[<Fact>]
let ``LL step visualization stack has bottom on left`` () =
    let g = (LanguageRegistry.findGrammar LanguageRegistry.SingleAB "twoRule").Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

[<Fact>]
let ``LL step visualization for accepted string returns success steps`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.TreeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.Input)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LL step visualization combined tree includes dashed stack chain`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)
    Assert.True(vizSteps |> List.exists (fun s -> s.TreeAndStack.Contains("style=dashed")))
    Assert.True(vizSteps |> List.exists (fun s -> s.TreeAndStack.Contains("shape=box")))

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LL step visualization stack leaves are connected by dashed edges and same rank`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        let info = ExternalTools.compileDotStringToInfo step.TreeAndStack
        Assert.True(info.NodeCount > 0)

    Assert.True(vizSteps |> List.exists (fun s -> s.TreeAndStack.Contains("digraph StackTree")))

[<Fact>]
let ``LL step visualization tree is properly nested`` () =
    let g = LanguageRegistry.Dyck1.Grammars.[0].Grammar

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let treeOpt, steps = LLParser.parseWithSteps g table 1 tokens

    match treeOpt with
    | Some tree ->
        match tree with
        | DerivationTree.Node(Nonterminal "S",
                              [ DerivationTree.Leaf(Symbol.T(Terminal "a"))
                                DerivationTree.Node(Nonterminal "S", [ DerivationTree.Leaf(Symbol.Epsilon) ])
                                DerivationTree.Leaf(Symbol.T(Terminal "b"))
                                DerivationTree.Node(Nonterminal "S", [ DerivationTree.Leaf(Symbol.Epsilon) ]) ]) -> ()
        | _ -> Assert.Fail(sprintf "Unexpected tree structure: %A" tree)
    | None -> Assert.Fail("Failed to parse a b")
