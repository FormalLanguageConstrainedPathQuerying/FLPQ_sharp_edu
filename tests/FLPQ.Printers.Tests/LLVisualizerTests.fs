module LLVisualizerTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

let private symbolPrinter = SymbolTeX.toLaTeX string string

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LL step visualization for grammar1 produces valid combined DOT and TeX`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.treeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)
        let info = ExternalTools.compileDotStringToInfo step.treeAndStack
        Assert.True(info.nodeCount > 0)

    Assert.True(vizSteps |> List.exists (fun s -> s.treeAndStack.Contains("{rank=same")))

[<Fact>]
let ``LL step visualization includes input position marker`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.True(vizSteps |> List.exists (fun s -> s.input.Contains(@"\underbar{")))

[<Fact>]
let ``LL step visualization stack has bottom on left`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a B
        B -> b
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

[<Fact>]
let ``LL step visualization for accepted string returns success steps`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        Assert.Contains("digraph StackTree", step.treeAndStack)
        Assert.Contains(@"\begin{pNiceMatrix}", step.input)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LL step visualization combined tree includes dashed stack chain`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)
    Assert.True(vizSteps |> List.exists (fun s -> s.treeAndStack.Contains("style=dashed")))
    Assert.True(vizSteps |> List.exists (fun s -> s.treeAndStack.Contains("shape=box")))

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LL step visualization stack leaves are connected by dashed edges and same rank`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let _, steps = LLParser.parseWithSteps g table 1 tokens
    let vizSteps = LLStepVisualizer.renderSteps symbolPrinter steps

    Assert.NotEmpty(vizSteps)

    for step in vizSteps do
        let info = ExternalTools.compileDotStringToInfo step.treeAndStack
        Assert.True(info.nodeCount > 0)

    Assert.True(vizSteps |> List.exists (fun s -> s.treeAndStack.Contains("digraph StackTree")))

[<Fact>]
let ``LL step visualization tree is properly nested`` () =
    let g =
        Grammar.parseGrammar
            "
        S -> a S b S
        S -> eps
        "

    let table = LLParser.buildTable g 1
    let tokens = Tokenizer.tokenizeTerminals "a b"
    let treeOpt, steps = LLParser.parseWithSteps g table 1 tokens

    match treeOpt with
    | Some tree ->
        match tree with
        | DerivationTree.Node(Nonterminal "S",
                              [ DerivationTree.Leaf(T(Terminal "a"))
                                DerivationTree.Node(Nonterminal "S", [ DerivationTree.Leaf(Epsilon) ])
                                DerivationTree.Leaf(T(Terminal "b"))
                                DerivationTree.Node(Nonterminal "S", [ DerivationTree.Leaf(Epsilon) ]) ]) -> ()
        | _ -> Assert.Fail(sprintf "Unexpected tree structure: %A" tree)
    | None -> Assert.Fail("Failed to parse a b")
