module DerivationTreeVisualizationTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers


[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``leaf tree dot compiles`` () =
    let tree = Leaf(Symbol.T(Terminal "x"))
    let dot = DerivationTreeDot.toDot string tree
    Assert.Contains("digraph DerivationTree", dot)
    Assert.Contains("shape=box", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(1, info.NodeCount)
    Assert.Equal(0, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``node with children dot compiles`` () =
    let tree =
        Node(
            Nonterminal "S",
            [ Leaf(Symbol.T(Terminal "a"))
              Node(Nonterminal "B", [ Leaf(Symbol.T(Terminal "b")) ]) ]
        )

    let dot = DerivationTreeDot.toDot string tree
    Assert.Contains("digraph DerivationTree", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(4, info.NodeCount)
    Assert.Equal(3, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``epsilon leaf dot compiles`` () =
    let tree = Node(Nonterminal "S", [ Leaf(Symbol.Epsilon) ])
    let dot = DerivationTreeDot.toDot string tree

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(2, info.NodeCount)
    Assert.Equal(1, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``LR parser tree dot compiles`` () =
    let grammar = Grammar.parseGrammar "S -> a S\nS -> a"
    let freshStart = Nonterminal(grammar.Start |> fun (Nonterminal n) -> n + "'")
    let aug = LRAutomaton.augmentGrammar freshStart grammar
    let table = LRParser.buildSLR1Table aug Grammar.eoiSymbol

    match LRParser.parse aug table (Tokenizer.tokenizeTerminals "a a") with
    | Some tree ->
        let dot = DerivationTreeDot.toDot string tree

        let info = ExternalTools.compileDotStringToInfo dot
        Assert.True(info.NodeCount > 0)
        Assert.True(info.EdgeCount > 0)
    | None -> Assert.Fail("Failed to parse")


module DerivationTreeGoldenTests =

    open GoldenHelpers

    [<Fact>]
    let ``simple tree dot golden`` () =
        let tree = Leaf(Symbol.T(Terminal "x"))
        let dot = DerivationTreeDot.toDot string tree
        verifyGolden "tree_leaf_x.dot" dot

    [<Fact>]
    let ``nested tree dot golden`` () =
        let tree =
            Node(
                Nonterminal "S",
                [ Leaf(Symbol.T(Terminal "a"))
                  Node(Nonterminal "B", [ Leaf(Symbol.T(Terminal "b")) ]) ]
            )

        let dot = DerivationTreeDot.toDot string tree
        verifyGolden "tree_nested_ab.dot" dot

    [<Fact>]
    let ``epsilon tree dot golden`` () =
        let tree = Node(Nonterminal "S", [ Leaf(Symbol.Epsilon) ])
        let dot = DerivationTreeDot.toDot string tree
        verifyGolden "tree_epsilon.dot" dot
