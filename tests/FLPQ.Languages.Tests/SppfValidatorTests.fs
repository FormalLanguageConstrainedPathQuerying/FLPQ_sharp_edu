module SppfValidatorTests

open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

/// Builds an SPPF graph from explicit vertices and labeled edges.
/// Vertices are indexed 0..n-1 in list order (Graph.fromEdges convention).
let private mkSppf
    (vertices: SppfNodeInfo<string, string> list)
    (edges: Trans<SppfEdgeLabel> list)
    : SPPF<string, string> =
    let n = vertices.Length
    let m = Matrix.init n n (None: Option<SppfEdgeLabel>)

    for e in edges do
        m.[e.From, e.To] <- Some e.Label

    { Graph = Graph.fromEdges vertices m
      RootIndices = [ 0 ] }

let private ntA = Nonterminal "A"
let private ntB = Nonterminal "B"

module CoverageInvariantTests =

    let private emptyPi = PathIndexFixtures.mk 2 2

    [<Fact>]
    let ``checkSppfCoverageInvariant reports nonterminal overflow`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfNonterminal(ntA, 0, 1, 0, 1) ] []
        let result = Sppf.checkSppfCoverageInvariant emptyPi sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("PNonterminal entries but SPPF has", err)
        | Ok() -> failwith "expected nonterminal overflow error"

    [<Fact>]
    let ``checkSppfCoverageInvariant reports terminal overflow`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfTerminal(Terminal "a", 0, 1) ] []
        let result = Sppf.checkSppfCoverageInvariant emptyPi sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("PTerminal entries but SPPF has", err)
        | Ok() -> failwith "expected terminal overflow error"

    [<Fact>]
    let ``checkSppfCoverageInvariant reports epsilon overflow`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfEpsilon(ntA, 0) ] []
        let result = Sppf.checkSppfCoverageInvariant emptyPi sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("PEpsilonNonterminal entries but SPPF has", err)
        | Ok() -> failwith "expected epsilon overflow error"

    [<Fact>]
    let ``checkSppfCoverageInvariant reports intermediate overflow`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfIntermediate(1, 2, 0, 0, 3, 4) ] []
        let result = Sppf.checkSppfCoverageInvariant emptyPi sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("PIntermediate entries but SPPF has", err)
        | Ok() -> failwith "expected intermediate overflow error"

    [<Fact>]
    let ``checkSppfCoverageInvariant reports all four overflows at once`` () =
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfNonterminal(ntA, 0, 1, 0, 1)
                  SppfNodeInfo.SppfTerminal(Terminal "a", 0, 1)
                  SppfNodeInfo.SppfEpsilon(ntB, 0)
                  SppfNodeInfo.SppfIntermediate(1, 2, 0, 0, 3, 4) ]
                []

        let result = Sppf.checkSppfCoverageInvariant emptyPi sppf

        match result with
        | Error errors -> Assert.Equal(4, List.length errors)
        | Ok() -> failwith "expected four overflow errors"

    [<Fact>]
    let ``checkSppfCoverageInvariant passes when counts are balanced`` () =
        // Range nodes are excluded from the count, so a range-only SPPF is balanced
        // against an empty path index.
        let sppf = mkSppf [ SppfNodeInfo.SppfRange(0, 0, 1, 1) ] []
        let result = Sppf.checkSppfCoverageInvariant emptyPi sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)


module RangeChildrenTests =

    [<Fact>]
    let ``validateRangeNodesHaveChildren flags childless range`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfRange(0, 0, 1, 1) ] []
        let result = Sppf.validateRangeNodesHaveChildren sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("has no children", err)
        | Ok() -> failwith "expected childless range error"

    [<Fact>]
    let ``validateRangeNodesHaveChildren passes for range with packed alternative`` () =
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfRange(0, 0, 1, 1)
                  SppfNodeInfo.SppfTerminal(Terminal "a", 0, 1) ]
                [ { From = 0
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 1 } ]

        let result = Sppf.validateRangeNodesHaveChildren sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)


module IntermediateChildrenTests =

    let private inter = SppfNodeInfo.SppfIntermediate(1, 2, 0, 0, 3, 4)

    [<Fact>]
    let ``validateIntermediateChildren flags missing left and right`` () =
        let sppf = mkSppf [ inter ] []
        let result = Sppf.validateIntermediateChildren sppf

        match result with
        | Error errors ->
            let all = String.concat " " errors
            Assert.Equal(2, List.length errors)
            Assert.Contains("has no LeftChild edge", all)
            Assert.Contains("has no RightChild edge", all)
        | Ok() -> failwith "expected missing left and right child errors"

    [<Fact>]
    let ``validateIntermediateChildren flags missing right only`` () =
        let sppf =
            mkSppf
                [ inter; SppfNodeInfo.SppfRange(0, 0, 1, 2) ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 } ]

        let result = Sppf.validateIntermediateChildren sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("has no RightChild edge", err)
        | Ok() -> failwith "expected missing right child edge error"

    [<Fact>]
    let ``validateIntermediateChildren passes when both children present`` () =
        let sppf =
            mkSppf
                [ inter
                  SppfNodeInfo.SppfRange(0, 0, 1, 2)
                  SppfNodeInfo.SppfRange(1, 2, 3, 4) ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateChildren sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)


module NonterminalChildrenTests =

    [<Fact>]
    let ``validateNonterminalChildren flags zero single-child edges`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfNonterminal(ntA, 0, 1, 0, 1) ] []
        let result = Sppf.validateNonterminalChildren id sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("has 0 SingleChild edge(s)", err)
        | Ok() -> failwith "expected zero single-child edges error"

    [<Fact>]
    let ``validateNonterminalChildren flags child that is not range or epsilon`` () =
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfNonterminal(ntA, 0, 1, 0, 1)
                  SppfNodeInfo.SppfTerminal(Terminal "a", 0, 1) ]
                [ { From = 0
                    Label = SppfEdgeLabel.SingleChild
                    To = 1 } ]

        let result = Sppf.validateNonterminalChildren id sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("is not a range or epsilon node", err)
        | Ok() -> failwith "expected non-range child error"

    [<Fact>]
    let ``validateNonterminalChildren passes for range child`` () =
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfNonterminal(ntA, 0, 1, 0, 1)
                  SppfNodeInfo.SppfRange(0, 0, 1, 1) ]
                [ { From = 0
                    Label = SppfEdgeLabel.SingleChild
                    To = 1 } ]

        let result = Sppf.validateNonterminalChildren id sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)


module RangePositionTests =

    [<Fact>]
    let ``validateRangePositions flags inverted range`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfRange(0, 5, 1, 2) ] []
        let result = Sppf.validateRangePositions sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("fromPos (5) > toPos (2)", err)
        | Ok() -> failwith "expected inverted range error"

    [<Fact>]
    let ``validateRangePositions passes for ordered range`` () =
        let sppf = mkSppf [ SppfNodeInfo.SppfRange(0, 0, 1, 2) ] []
        let result = Sppf.validateRangePositions sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)


module IntermediateConnectednessTests =

    /// Intermediate I(1,2; 0,0 -> 3,4): valid left child is Range(0,0,1,2),
    /// valid right child is Range(1,2,3,4).
    let private inter = SppfNodeInfo.SppfIntermediate(1, 2, 0, 0, 3, 4)
    let private validLeft = SppfNodeInfo.SppfRange(0, 0, 1, 2)
    let private validRight = SppfNodeInfo.SppfRange(1, 2, 3, 4)

    [<Fact>]
    let ``validateIntermediateConnectedness passes for consistent children`` () =
        let sppf =
            mkSppf
                [ inter; validLeft; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)

    [<Fact>]
    let ``validateIntermediateConnectedness flags left child starting at wrong coords`` () =
        let badLeft = SppfNodeInfo.SppfRange(5, 0, 1, 2)

        let sppf =
            mkSppf
                [ inter; badLeft; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("left child 1 starts at [s5,v0], expected [s0,v0]", err)
        | Ok() -> failwith "expected wrong left start coordinates error"

    [<Fact>]
    let ``validateIntermediateConnectedness flags left child ending at wrong coords`` () =
        let badLeft = SppfNodeInfo.SppfRange(0, 0, 9, 9)

        let sppf =
            mkSppf
                [ inter; badLeft; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("left child 1 ends at [s9,v9], expected [s1,v2]", err)
        | Ok() -> failwith "expected wrong left end coordinates error"

    [<Fact>]
    let ``validateIntermediateConnectedness flags right child starting at wrong coords`` () =
        let badRight = SppfNodeInfo.SppfRange(5, 5, 3, 4)

        let sppf =
            mkSppf
                [ inter; validLeft; badRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("right child 2 starts at [s5,v5], expected [s1,v2]", err)
        | Ok() -> failwith "expected wrong right start coordinates error"

    [<Fact>]
    let ``validateIntermediateConnectedness flags right child ending at wrong coords`` () =
        let badRight = SppfNodeInfo.SppfRange(1, 2, 9, 9)

        let sppf =
            mkSppf
                [ inter; validLeft; badRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("right child 2 ends at [s9,v9], expected [s3,v4]", err)
        | Ok() -> failwith "expected wrong right end coordinates error"

    [<Fact>]
    let ``validateIntermediateConnectedness flags missing left child`` () =
        let sppf =
            mkSppf
                [ inter; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 1 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("missing LeftChild", err)
        | Ok() -> failwith "expected missing LeftChild edge error"

    [<Fact>]
    let ``validateIntermediateConnectedness flags missing right child`` () =
        let sppf =
            mkSppf
                [ inter; validLeft ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("missing RightChild", err)
        | Ok() -> failwith "expected missing RightChild edge error"

    [<Fact>]
    let ``validateIntermediateConnectedness checks nonterminal child coordinates`` () =
        // getCoords maps Nonterminal(nt, l, r, fs, ts) to (fs, l, ts, r);
        // fromState=5 mismatches the intermediate's fromState=0.
        let ntChild = SppfNodeInfo.SppfNonterminal(ntB, 0, 2, 5, 1)

        let sppf =
            mkSppf
                [ inter; ntChild; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf

        match result with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("left child 1 starts at [s5,v0], expected [s0,v0]", err)
        | Ok() -> failwith "expected nonterminal child coordinates error"

    [<Fact>]
    let ``validateIntermediateConnectedness skips terminal children`` () =
        let termChild = SppfNodeInfo.SppfTerminal(Terminal "a", 0, 2)

        let sppf =
            mkSppf
                [ inter; termChild; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)

    [<Fact>]
    let ``validateIntermediateConnectedness skips epsilon children`` () =
        let epsChild = SppfNodeInfo.SppfEpsilon(ntB, 0)

        let sppf =
            mkSppf
                [ inter; epsChild; validRight ]
                [ { From = 0
                    Label = SppfEdgeLabel.LeftChild
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.RightChild
                    To = 2 } ]

        let result = Sppf.validateIntermediateConnectedness sppf
        Assert.Equal<Result<unit, string list>>(Ok(), result)


module BuildSppfFromIndexTests =

    /// Path index with PNonterminal A and PTerminal "a" in cell (0,0)->(1,1).
    let private mkPi () : PathIndex<string, string> =
        let pi = PathIndexFixtures.mk 2 2
        PathIndex.add pi 0 0 1 1 (PathIndexEntry.PNonterminal ntA)
        PathIndex.add pi 0 0 1 1 (PathIndexEntry.PTerminal(Terminal "a"))
        pi

    let private rootRange =
        { FromState = 0
          FromVertex = 0
          ToState = 1
          ToVertex = 1 }

    let private hasSingleChildEdge (sppf: SPPF<string, string>) : bool =
        let n = Graph.vertexCount sppf.Graph

        [ for i in 0 .. n - 1 do
              for j in 0 .. n - 1 do
                  if sppf.Graph.Edges.[i, j] = Some SppfEdgeLabel.SingleChild then
                      yield true ]
        |> List.exists id

    [<Fact>]
    let ``buildSppfFromIndex with None block maps skips callee edges`` () =
        let sppf = Sppf.buildSppfFromIndex (mkPi ()) [ rootRange ] None None
        Assert.False(hasSingleChildEdge sppf)

    [<Fact>]
    let ``buildSppfFromIndex with nonterminal missing from blockStart skips callee edges`` () =
        let blockStart = Map.empty
        let blockFinals = Map.ofList [ (ntA, Set.ofList [ 1 ]) ]

        let sppf =
            Sppf.buildSppfFromIndex (mkPi ()) [ rootRange ] (Some blockStart) (Some blockFinals)

        Assert.False(hasSingleChildEdge sppf)

    [<Fact>]
    let ``buildSppfFromIndex with nonterminal missing from blockFinals skips callee edges`` () =
        let blockStart = Map.ofList [ (ntA, 0) ]
        let blockFinals = Map.empty

        let sppf =
            Sppf.buildSppfFromIndex (mkPi ()) [ rootRange ] (Some blockStart) (Some blockFinals)

        Assert.False(hasSingleChildEdge sppf)

    [<Fact>]
    let ``buildSppfFromIndex links callee range via single-child edge`` () =
        // The callee cell (0,0)->(1,1) is non-empty (PTerminal "a"), so the
        // nonterminal node must gain a SingleChild edge to the callee range.
        let blockStart = Map.ofList [ (ntA, 0) ]
        let blockFinals = Map.ofList [ (ntA, Set.ofList [ 1 ]) ]

        let sppf =
            Sppf.buildSppfFromIndex (mkPi ()) [ rootRange ] (Some blockStart) (Some blockFinals)

        Assert.True(hasSingleChildEdge sppf)


module EnumerateTreesTests =

    /// In all tests the root range (vertex 0) points via PackedAlternative to an
    /// intermediate whose left/right children are ranges holding terminals "a"
    /// and "b". The intermediate yields the two-tree list [Leaf a; Leaf b] at
    /// depth 2, so Seq.take 1 always terminates after the first yield.
    let private goodIntermediate = SppfNodeInfo.SppfIntermediate(1, 1, 0, 0, 1, 2)
    let private leftRange = SppfNodeInfo.SppfRange(0, 0, 1, 1)
    let private rightRange = SppfNodeInfo.SppfRange(1, 1, 1, 2)
    let private leftTerm = SppfNodeInfo.SppfTerminal(Terminal "a", 0, 1)
    let private rightTerm = SppfNodeInfo.SppfTerminal(Terminal "b", 1, 2)

    let private expectedTree =
        [ Node(Nonterminal "S", [ Leaf(Symbol.T(Terminal "a")); Leaf(Symbol.T(Terminal "b")) ]) ]

    [<Fact>]
    let ``enumerateTrees wraps multi-tree child list in root node`` () =
        // Vertices: 0 range, 1 intermediate, 2 leftRange, 3 rightRange,
        // 4 leftTerm, 5 rightTerm.
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfRange(0, 0, 1, 2)
                  goodIntermediate
                  leftRange
                  rightRange
                  leftTerm
                  rightTerm ]
                [ { From = 0
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 1 }
                  { From = 1
                    Label = SppfEdgeLabel.LeftChild
                    To = 2 }
                  { From = 1
                    Label = SppfEdgeLabel.RightChild
                    To = 3 }
                  { From = 2
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 4 }
                  { From = 3
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 5 } ]

        let trees = Sppf.enumerateTrees (Nonterminal "S") sppf 0 |> Seq.take 1 |> Seq.toList
        Assert.Equal<DerivationTree<string, string>>(expectedTree, trees)

    [<Fact>]
    let ``enumerateTrees visits nonterminal without single-child edge`` () =
        // Vertex 2 is a dead nonterminal (no SingleChild edge): at depth > 1 it
        // yields nothing. The good intermediate still yields at depth 2 so
        // Seq.take 1 terminates.
        // Vertices: 0 range, 1 intermediate, 2 deadNt, 3 leftRange, 4 rightRange,
        // 5 leftTerm, 6 rightTerm.
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfRange(0, 0, 1, 2)
                  goodIntermediate
                  SppfNodeInfo.SppfNonterminal(ntA, 0, 2, 0, 1)
                  leftRange
                  rightRange
                  leftTerm
                  rightTerm ]
                [ { From = 0
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 2 }
                  { From = 1
                    Label = SppfEdgeLabel.LeftChild
                    To = 3 }
                  { From = 1
                    Label = SppfEdgeLabel.RightChild
                    To = 4 }
                  { From = 3
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 5 }
                  { From = 4
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 6 } ]

        let trees = Sppf.enumerateTrees (Nonterminal "S") sppf 0 |> Seq.take 1 |> Seq.toList
        Assert.Equal<DerivationTree<string, string>>(expectedTree, trees)

    [<Fact>]
    let ``enumerateTrees visits intermediate missing a child edge`` () =
        // Vertex 2 is an intermediate with only a LeftChild: at depth > 1 it
        // yields nothing. The good intermediate still yields at depth 2 so
        // Seq.take 1 terminates.
        // Vertices: 0 range, 1 good intermediate, 2 broken intermediate,
        // 3 leftRange, 4 rightRange, 5 leftTerm, 6 rightTerm.
        let sppf =
            mkSppf
                [ SppfNodeInfo.SppfRange(0, 0, 1, 2)
                  goodIntermediate
                  SppfNodeInfo.SppfIntermediate(1, 1, 0, 0, 1, 2)
                  leftRange
                  rightRange
                  leftTerm
                  rightTerm ]
                [ { From = 0
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 1 }
                  { From = 0
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 2 }
                  { From = 1
                    Label = SppfEdgeLabel.LeftChild
                    To = 3 }
                  { From = 1
                    Label = SppfEdgeLabel.RightChild
                    To = 4 }
                  { From = 2
                    Label = SppfEdgeLabel.LeftChild
                    To = 3 }
                  { From = 3
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 5 }
                  { From = 4
                    Label = SppfEdgeLabel.PackedAlternative
                    To = 6 } ]

        let trees = Sppf.enumerateTrees (Nonterminal "S") sppf 0 |> Seq.take 1 |> Seq.toList
        Assert.Equal<DerivationTree<string, string>>(expectedTree, trees)
