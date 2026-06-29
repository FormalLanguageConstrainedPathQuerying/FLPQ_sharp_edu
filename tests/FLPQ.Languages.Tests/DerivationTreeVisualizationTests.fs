module DerivationTreeVisualizationTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra


[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``leaf tree dot compiles`` () =
    let tree = Leaf(T(Terminal "x"))
    let dot = DerivationTreeVisualizer.toDot string tree
    Assert.Contains("digraph DerivationTree", dot)
    Assert.Contains("shape=box", dot)

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.Equal(1, info.nodeCount)
    Assert.Equal(0, info.edgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``node with children dot compiles`` () =
    let tree =
        Node(Nonterminal "S", [ Leaf(T(Terminal "a")); Node(Nonterminal "B", [ Leaf(T(Terminal "b")) ]) ])

    let dot = DerivationTreeVisualizer.toDot string tree
    Assert.Contains("digraph DerivationTree", dot)

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.Equal(4, info.nodeCount)
    Assert.Equal(3, info.edgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``epsilon leaf dot compiles`` () =
    let tree = Node(Nonterminal "S", [ Leaf(Epsilon) ])
    let dot = DerivationTreeVisualizer.toDot string tree

    let info = TestUtils.checkDotCompilesWithInfo dot
    Assert.Equal(2, info.nodeCount)
    Assert.Equal(1, info.edgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LR parser tree dot compiles`` () =
    let grammar = Grammar.parseGrammar "S -> a S\nS -> a"
    let freshStart = Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart grammar
    let table = LRParser.buildSLR1Table aug

    match LRParser.parse aug table (Tokenizer.tokenize "a a") with
    | Some tree ->
        let dot = DerivationTreeVisualizer.toDot string tree

        let info = TestUtils.checkDotCompilesWithInfo dot
        Assert.True(info.nodeCount > 0)
        Assert.True(info.edgeCount > 0)
    | None -> Assert.Fail("Failed to parse")
