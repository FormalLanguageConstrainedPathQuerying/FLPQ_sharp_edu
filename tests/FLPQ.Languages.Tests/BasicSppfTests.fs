module BasicSppfTests

open Xunit
open FLPQ.Languages
open FLPQ.Languages.BasicSppf
open FLPQ.GraphAnalysis

[<Fact>]
let ``fromEdges constructs valid SPPF`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0, 2)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges =
        [ (0, BasicSppfEdgeLabel.Derives, 1)
          (1, BasicSppfEdgeLabel.ChildOf 0, 2)
          (1, BasicSppfEdgeLabel.ChildOf 1, 3) ]

    let sppf = fromEdges vertices edges 0
    Assert.Equal(4, Graph.vertexCount sppf.Graph)
    Assert.Equal(0, sppf.RootIndex)

[<Fact>]
let ``extractDerivationTree simple S -> a b`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0, 2)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges =
        [ (0, BasicSppfEdgeLabel.Derives, 1)
          (1, BasicSppfEdgeLabel.ChildOf 0, 2)
          (1, BasicSppfEdgeLabel.ChildOf 1, 3) ]

    let sppf = fromEdges vertices edges 0
    let tree = extractDerivationTree sppf

    match tree with
    | Node(Nonterminal nt, children) ->
        Assert.Equal("S", nt)
        Assert.Equal(2, List.length children)
        Assert.Equal<string list>([ "a"; "b" ], DerivationTree.leaves tree)
    | _ -> Assert.Fail("Expected Node")

[<Fact>]
let ``extractDerivationTree with epsilon`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 0)
          BasicSppfNodeInfo.Production(0, 0, 0)
          BasicSppfNodeInfo.Epsilon 0 ]

    let edges =
        [ (0, BasicSppfEdgeLabel.Derives, 1); (1, BasicSppfEdgeLabel.ChildOf 0, 2) ]

    let sppf = fromEdges vertices edges 0
    let tree = extractDerivationTree sppf

    match tree with
    | Node(Nonterminal nt, [ Leaf Symbol.Epsilon ]) -> Assert.Equal("S", nt)
    | _ -> Assert.Fail("Expected Node with Epsilon leaf")

[<Fact>]
let ``extractDerivationTree nested nonterminals`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0, 2)
          BasicSppfNodeInfo.Nonterminal(Nonterminal "A", 0, 1)
          BasicSppfNodeInfo.Production(1, 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges =
        [ (0, BasicSppfEdgeLabel.Derives, 1)
          (1, BasicSppfEdgeLabel.ChildOf 0, 2)
          (1, BasicSppfEdgeLabel.ChildOf 1, 5)
          (2, BasicSppfEdgeLabel.Derives, 3)
          (3, BasicSppfEdgeLabel.ChildOf 0, 4) ]

    let sppf = fromEdges vertices edges 0
    let tree = extractDerivationTree sppf

    Assert.Equal<string list>([ "a"; "b" ], DerivationTree.leaves tree)

[<Fact>]
let ``enumerateTrees single production`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]

    let edges =
        [ (0, BasicSppfEdgeLabel.Derives, 1); (1, BasicSppfEdgeLabel.ChildOf 0, 2) ]

    let sppf = fromEdges vertices edges 0
    let trees = enumerateTrees sppf |> Seq.toList

    Assert.Single(trees) |> ignore
    Assert.Equal<string list>([ "a" ], DerivationTree.leaves trees.Head)

[<Fact>]
let ``enumerateTrees with packed alternatives`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0, 1)
          BasicSppfNodeInfo.Production(1, 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 0, 1) ]

    let edges =
        [ (0, BasicSppfEdgeLabel.Derives, 1)
          (0, BasicSppfEdgeLabel.Derives, 2)
          (1, BasicSppfEdgeLabel.ChildOf 0, 3)
          (2, BasicSppfEdgeLabel.ChildOf 0, 4) ]

    let sppf = fromEdges vertices edges 0
    let trees = enumerateTrees sppf |> Seq.toList

    Assert.Equal(2, List.length trees)
    let leaves = trees |> List.map DerivationTree.leaves |> List.sort
    Assert.Equal<string list list>([ [ "a" ]; [ "b" ] ], leaves)

[<Fact>]
let ``leaf nonterminal returns leaf`` () =
    let vertices = [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 0) ]

    let sppf = fromEdges vertices [] 0
    let tree = extractDerivationTree sppf

    match tree with
    | Leaf(Symbol.N(Nonterminal nt)) -> Assert.Equal("S", nt)
    | _ -> Assert.Fail("Expected Leaf with Nonterminal")
