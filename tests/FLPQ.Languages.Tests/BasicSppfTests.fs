module BasicSppfTests

open Xunit
open FLPQ.Languages
open FLPQ.Languages.BasicSppf
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Fact>]
let ``fromEdges constructs valid SPPF`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 3) ]

    let sppf = fromEdges vertices edges 0
    Assert.Equal(4, Graph.vertexCount sppf.Graph)
    Assert.Equal(0, sppf.RootIndex)

[<Fact>]
let ``extractDerivationTree simple S -> a b`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 3) ]

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
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Epsilon 0 ]

    let edges = [ (0, 1); (1, 2) ]

    let sppf = fromEdges vertices edges 0
    let tree = extractDerivationTree sppf

    match tree with
    | Node(Nonterminal nt, [ Leaf Symbol.Epsilon ]) -> Assert.Equal("S", nt)
    | _ -> Assert.Fail("Expected Node with Epsilon leaf")

[<Fact>]
let ``extractDerivationTree nested nonterminals`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Nonterminal(Nonterminal "A", 0, 1)
          BasicSppfNodeInfo.Production(1, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

    let edges = [ (0, 1); (1, 2); (1, 5); (2, 3); (3, 4) ]

    let sppf = fromEdges vertices edges 0
    let tree = extractDerivationTree sppf

    Assert.Equal<string list>([ "a"; "b" ], DerivationTree.leaves tree)

[<Fact>]
let ``enumerateTrees single production`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]

    let edges = [ (0, 1); (1, 2) ]

    let sppf = fromEdges vertices edges 0
    let trees = enumerateTrees sppf |> Seq.toList

    Assert.Single(trees) |> ignore
    Assert.Equal<string list>([ "a" ], DerivationTree.leaves trees.Head)

[<Fact>]
let ``enumerateTrees with packed alternatives`` () =
    let vertices =
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0)
          BasicSppfNodeInfo.Production(1, 0)
          BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
          BasicSppfNodeInfo.Terminal(Terminal "b", 0, 1) ]

    let edges = [ (0, 1); (0, 2); (1, 3); (2, 4) ]

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

[<Fact>]
let ``validateProductionChildren passes for SPPF from CYK`` () =
    let grammar = LanguageRegistry.SingleAB.Grammars.[0].Grammar

    let input = LanguageRegistry.SingleAB.AcceptStrings.[0]

    let table, accepted =
        Cyk.parseWithSppfTable Grammar.freshStringNonterminal grammar input

    Assert.True(accepted)

    let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
    let sppf = BasicSppf.fromParsingTable cnf table

    match validateProductionChildren sppf cnf with
    | Ok() -> ()
    | Error errors -> Assert.Fail(String.concat "\n" errors)

[<Fact>]
let ``validateProductionChildren passes for SPPF from Valiant`` () =
    let grammar = LanguageRegistry.SingleAB.Grammars.[0].Grammar

    let input = LanguageRegistry.SingleAB.AcceptStrings.[0]

    let table, accepted =
        Valiant.parseWithSppfTable Grammar.freshStringNonterminal grammar input

    Assert.True(accepted)

    let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
    let sppf = BasicSppf.fromParsingTable cnf table

    match validateProductionChildren sppf cnf with
    | Ok() -> ()
    | Error errors -> Assert.Fail(String.concat "\n" errors)

[<Fact>]
let ``validateProductionChildren passes for SPPF from Modified Valiant`` () =
    let grammar = LanguageRegistry.SingleAB.Grammars.[0].Grammar

    let input = LanguageRegistry.SingleAB.AcceptStrings.[0]

    let table, accepted =
        Valiant.parseModifiedWithSppfTable Grammar.freshStringNonterminal grammar input

    Assert.True(accepted)

    let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
    let sppf = BasicSppf.fromParsingTable cnf table

    match validateProductionChildren sppf cnf with
    | Ok() -> ()
    | Error errors -> Assert.Fail(String.concat "\n" errors)


/// NT(0) -> Production(1) -> NT(0): a cycle that must terminate via visited sets.
let private cyclicSppf: BasicSPPF<string, string> =
    fromEdges
        [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
          BasicSppfNodeInfo.Production(0, 0) ]
        [ (0, 1); (1, 0) ]
        0


module FromParsingTableTests =

    [<Fact>]
    let ``fromParsingTable empty table returns empty SPPF`` () =
        let cnf = LanguageRegistry.SingleA.Grammars.[0].Grammar
        let table = Matrix.init 0 0 (Set.empty: Set<SppfParsingEntry<string>>)
        let sppf = fromParsingTable cnf table
        Assert.Equal(0, Graph.vertexCount sppf.Graph)

    [<Fact>]
    let ``fromParsingTable without start entry returns empty SPPF`` () =
        // Cell (0, 1) holds an entry for a non-start nonterminal only.
        let cnf = Grammar.parseGrammar "S -> a b\nA -> a"
        let table = Matrix.init 2 2 (Set.empty: Set<SppfParsingEntry<string>>)

        table.[0, 1] <-
            Set.ofList
                [ { Nt = Nonterminal "A"
                    SplitPoint = 0
                    ProdIdx = 2 } ]

        let sppf = fromParsingTable cnf table
        Assert.Equal(0, Graph.vertexCount sppf.Graph)

    [<Fact>]
    let ``fromParsingTable epsilon production creates childless production node`` () =
        // Int-symbol variant: hits the int-specialized instantiation of fromParsingTable.
        let cnf: Grammar<int, int> =
            { Rules =
                [ { Lhs = Nonterminal 1
                    Rhs = EpsilonRhs } ]
              Start = Nonterminal 1 }

        let table = Matrix.init 1 1 (Set.empty: Set<SppfParsingEntry<int>>)

        table.[0, 0] <-
            Set.ofList
                [ { Nt = Nonterminal 1
                    SplitPoint = 0
                    ProdIdx = 1 } ]

        let sppf = fromParsingTable cnf table

        Assert.Equal(2, Graph.vertexCount sppf.Graph)

        match Graph.getVertex 1 sppf.Graph with
        | BasicSppfNodeInfo.Production(1, 0) -> ()
        | info -> Assert.Fail($"expected childless production node, got {info}")

        let n = Graph.vertexCount sppf.Graph

        let hasChildren =
            [ for j in 0 .. n - 1 do
                  if sppf.Graph.Edges.[1, j] then
                      yield true ]
            |> List.exists id

        Assert.False(hasChildren)


module ExtractDerivationTreeRootTests =

    [<Fact>]
    let ``extractDerivationTree terminal root returns leaf`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ] [] 0

        match extractDerivationTree sppf with
        | Leaf(Symbol.T(Terminal t)) -> Assert.Equal("a", t)
        | other -> Assert.Fail($"expected terminal leaf, got {other}")

    [<Fact>]
    let ``extractDerivationTree epsilon root returns epsilon leaf`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Epsilon 0 ] [] 0

        match extractDerivationTree sppf with
        | Leaf Symbol.Epsilon -> ()
        | other -> Assert.Fail($"expected epsilon leaf, got {other}")

    [<Fact>]
    let ``extractDerivationTree production root returns first child tree`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 0)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

        let edges = [ (0, 1); (0, 2) ]
        let sppf = fromEdges vertices edges 0

        match extractDerivationTree sppf with
        | Leaf(Symbol.T(Terminal t)) -> Assert.Equal("a", t)
        | other -> Assert.Fail($"expected first child leaf, got {other}")


module EnumerateTreesRootTests =

    [<Fact>]
    let ``enumerateTrees nonterminal root without productions yields nonterminal leaf`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 0) ] [] 0
        let trees = enumerateTrees sppf |> Seq.toList

        match List.head trees with
        | Leaf(Symbol.N(Nonterminal nt)) -> Assert.Equal("S", nt)
        | other -> Assert.Fail($"expected nonterminal leaf, got {other}")

    [<Fact>]
    let ``enumerateTrees terminal root yields single tree`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ] [] 0
        let trees = enumerateTrees sppf |> Seq.toList

        match List.head trees with
        | Leaf(Symbol.T(Terminal t)) -> Assert.Equal("a", t)
        | other -> Assert.Fail($"expected terminal leaf, got {other}")

    [<Fact>]
    let ``enumerateTrees epsilon root yields epsilon leaf`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Epsilon 0 ] [] 0
        let trees = enumerateTrees sppf |> Seq.toList

        match List.head trees with
        | Leaf Symbol.Epsilon -> ()
        | other -> Assert.Fail($"expected epsilon leaf, got {other}")

    [<Fact>]
    let ``enumerateTrees production root without children yields epsilon leaf`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Production(0, 0) ] [] 0
        let trees = enumerateTrees sppf |> Seq.toList

        match List.head trees with
        | Leaf Symbol.Epsilon -> ()
        | other -> Assert.Fail($"expected epsilon leaf, got {other}")

    [<Fact>]
    let ``enumerateTrees production root with two children combines them`` () =
        // Two production children exercise combineChildren first::rest and
        // the single-list branch of combineLists.
        let vertices =
            [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
              BasicSppfNodeInfo.Production(0, 0)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

        let edges = [ (0, 1); (1, 2); (1, 3) ]
        let sppf = fromEdges vertices edges 0
        let trees = enumerateTrees sppf |> Seq.toList

        Assert.Single(trees) |> ignore

        let tree = List.head trees

        match tree with
        | Node(Nonterminal nt, children) ->
            Assert.Equal("S", nt)
            Assert.Equal(2, List.length children)
            Assert.Equal<string list>([ "a"; "b" ], DerivationTree.leaves tree)
        | other -> Assert.Fail($"expected node, got {other}")

    [<Fact>]
    let ``enumerateTrees production root with three children yields head of combined forest`` () =
        // Three children force the recursive branch of combineLists. A production
        // root has no nonterminal label to wrap the forest in, so each combined
        // forest is truncated to its first tree (BasicSppf.enumerateTrees).
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 0)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2)
              BasicSppfNodeInfo.Terminal(Terminal "c", 2, 3) ]

        let edges = [ (0, 1); (0, 2); (0, 3) ]
        let sppf = fromEdges vertices edges 0
        let trees = enumerateTrees sppf |> Seq.toList

        Assert.Single(trees) |> ignore

        match List.head trees with
        | Leaf(Symbol.T(Terminal t)) -> Assert.Equal("a", t)
        | other -> Assert.Fail($"expected first child leaf, got {other}")


module CyclicSppfTests =

    [<Fact>]
    let ``extractDerivationTree terminates on cyclic SPPF`` () =
        let tree = extractDerivationTree cyclicSppf

        match tree with
        | Leaf(Symbol.N(Nonterminal nt)) -> Assert.Equal("S", nt)
        | other -> Assert.Fail($"expected nonterminal leaf, got {other}")

    [<Fact>]
    let ``enumerateTrees terminates on cyclic SPPF`` () =
        let trees = enumerateTrees cyclicSppf |> Seq.toList

        match List.head trees with
        | Leaf(Symbol.N(Nonterminal nt)) -> Assert.Equal("S", nt)
        | other -> Assert.Fail($"expected nonterminal leaf, got {other}")


module ValidateProductionChildrenTests =

    let private cnfUnary = LanguageRegistry.SingleA.Grammars.[0].Grammar
    let private cnfBinary = LanguageRegistry.SingleAB.Grammars.[0].Grammar

    [<Fact>]
    let ``validateProductionChildren flags production with zero children`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Production(1, 0) ] [] 0

        match validateProductionChildren sppf cnfUnary with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("has 0 children", err)
        | Ok() -> Assert.Fail("expected child count error")

    [<Fact>]
    let ``validateProductionChildren flags production with three children`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(1, 0)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2)
              BasicSppfNodeInfo.Terminal(Terminal "c", 2, 3) ]

        let edges = [ (0, 1); (0, 2); (0, 3) ]
        let sppf = fromEdges vertices edges 0

        match validateProductionChildren sppf cnfUnary with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("has 3 children", err)
        | Ok() -> Assert.Fail("expected child count error")

    [<Fact>]
    let ``validateProductionChildren flags unknown rule index`` () =
        let sppf =
            fromEdges
                [ BasicSppfNodeInfo.Production(5, 0)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]
                [ (0, 1) ]
                0

        match validateProductionChildren sppf cnfUnary with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("ruleIndex=5 not found", err)
        | Ok() -> Assert.Fail("expected unknown rule error")

    [<Fact>]
    let ``validateProductionChildren flags child count different from RHS length`` () =
        // Rule 1 is S -> a b (RHS length 2) but the production has one child.
        let sppf =
            fromEdges
                [ BasicSppfNodeInfo.Production(1, 0)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]
                [ (0, 1) ]
                0

        match validateProductionChildren sppf cnfBinary with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("RHS length is 2", err)
        | Ok() -> Assert.Fail("expected RHS length error")


module CountSccTests =

    [<Fact>]
    let ``countScc cyclic SPPF has single strongly connected component`` () = Assert.Equal(1, countScc cyclicSppf)

    [<Fact>]
    let ``countNonTrivialScc cyclic SPPF counts the two-node component`` () =
        Assert.Equal(1, countNonTrivialScc cyclicSppf)

    [<Fact>]
    let ``countNonTrivialScc acyclic SPPF counts nothing`` () =
        let sppf =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
                  BasicSppfNodeInfo.Production(0, 0)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]
                [ (0, 1); (1, 2) ]
                0

        Assert.Equal(0, countNonTrivialScc sppf)


module TraverseAndCompareTests =

    [<Fact>]
    let ``traverseAndCompare different vertex counts`` () =
        let a = fromEdges [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1) ] [] 0

        let b =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 1)
                  BasicSppfNodeInfo.Production(0, 0) ]
                []
                0

        Assert.False(traverseAndCompare a b)

    [<Fact>]
    let ``traverseAndCompare different node info`` () =
        let a = fromEdges [ BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ] [] 0
        let b = fromEdges [ BasicSppfNodeInfo.Terminal(Terminal "b", 0, 1) ] [] 0

        Assert.False(traverseAndCompare a b)

    [<Fact>]
    let ``traverseAndCompare different child counts`` () =
        let a =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
                  BasicSppfNodeInfo.Production(0, 0)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1) ]
                [ (0, 1); (1, 2) ]
                0

        let b =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
                  BasicSppfNodeInfo.Production(0, 0)
                  BasicSppfNodeInfo.Production(1, 0)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
                  BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]
                [ (0, 1); (0, 2); (1, 3); (2, 4) ]
                0

        Assert.False(traverseAndCompare a b)

    [<Fact>]
    let ``traverseAndCompare same child count but different child info`` () =
        let a =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
                  BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]
                [ (0, 1); (0, 2) ]
                0

        let b =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
                  BasicSppfNodeInfo.Terminal(Terminal "c", 1, 2) ]
                [ (0, 1); (0, 2) ]
                0

        Assert.False(traverseAndCompare a b)

    [<Fact>]
    let ``traverseAndCompare equal SPPFs`` () =
        let mk () =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
                  BasicSppfNodeInfo.Production(0, 0)
                  BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
                  BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]
                [ (0, 1); (1, 2); (1, 3) ]
                0

        Assert.True(traverseAndCompare (mk ()) (mk ()))


module ValidateProductionSplitConsistencyTests =

    [<Fact>]
    let ``validateProductionSplitConsistency passes for terminal children`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

        let edges = [ (0, 1); (0, 2) ]
        let sppf = fromEdges vertices edges 0

        match validateProductionSplitConsistency sppf with
        | Ok() -> ()
        | Error errors -> Assert.Fail(String.concat "\n" errors)

    [<Fact>]
    let ``validateProductionSplitConsistency passes for nonterminal children`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 1)
              BasicSppfNodeInfo.Nonterminal(Nonterminal "A", 0, 1)
              BasicSppfNodeInfo.Nonterminal(Nonterminal "B", 1, 2) ]

        let edges = [ (0, 1); (0, 2) ]
        let sppf = fromEdges vertices edges 0

        match validateProductionSplitConsistency sppf with
        | Ok() -> ()
        | Error errors -> Assert.Fail(String.concat "\n" errors)

    [<Fact>]
    let ``validateProductionSplitConsistency passes for epsilon children`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 1)
              BasicSppfNodeInfo.Epsilon 1
              BasicSppfNodeInfo.Epsilon 1 ]

        let edges = [ (0, 1); (0, 2) ]
        let sppf = fromEdges vertices edges 0

        match validateProductionSplitConsistency sppf with
        | Ok() -> ()
        | Error errors -> Assert.Fail(String.concat "\n" errors)

    [<Fact>]
    let ``validateProductionSplitConsistency flags left child ending after split point`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 2)
              BasicSppfNodeInfo.Terminal(Terminal "b", 1, 2) ]

        let edges = [ (0, 1); (0, 2) ]
        let sppf = fromEdges vertices edges 0

        match validateProductionSplitConsistency sppf with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("left child", err)
        | Ok() -> Assert.Fail("expected split consistency error")

    [<Fact>]
    let ``validateProductionSplitConsistency flags right child starting after split point`` () =
        let vertices =
            [ BasicSppfNodeInfo.Production(0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "a", 0, 1)
              BasicSppfNodeInfo.Terminal(Terminal "b", 2, 3) ]

        let edges = [ (0, 1); (0, 2) ]
        let sppf = fromEdges vertices edges 0

        match validateProductionSplitConsistency sppf with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("right child", err)
        | Ok() -> Assert.Fail("expected split consistency error")


module ValidateSingleRootTests =

    [<Fact>]
    let ``validateSingleRoot passes for correct root`` () =
        let vertices =
            [ BasicSppfNodeInfo.Nonterminal(Nonterminal "S", 0, 2)
              BasicSppfNodeInfo.Production(0, 0) ]

        let edges = [ (0, 1) ]
        let sppf = fromEdges vertices edges 0

        match validateSingleRoot sppf (Nonterminal "S") 2 with
        | Ok() -> ()
        | Error errors -> Assert.Fail(String.concat "\n" errors)

    [<Fact>]
    let ``validateSingleRoot flags root with wrong info`` () =
        let sppf = fromEdges [ BasicSppfNodeInfo.Nonterminal(Nonterminal "A", 0, 1) ] [] 0

        match validateSingleRoot sppf (Nonterminal "S") 2 with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("has info", err)
        | Ok() -> Assert.Fail("expected root info error")

    [<Fact>]
    let ``validateSingleRoot flags multiple roots without incoming edges`` () =
        let sppf =
            fromEdges
                [ BasicSppfNodeInfo.Nonterminal(Nonterminal "A", 0, 1)
                  BasicSppfNodeInfo.Nonterminal(Nonterminal "B", 1, 2) ]
                []
                0

        match validateSingleRoot sppf (Nonterminal "S") 2 with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("found 2", err)
        | Ok() -> Assert.Fail("expected multiple roots error")
