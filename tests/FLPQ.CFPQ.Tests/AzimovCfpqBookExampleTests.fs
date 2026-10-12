module AzimovCfpqBookExampleTests

open Xunit
open FLPQ.CFPQ
open FLPQ.Languages

// The book's T13 (Chapter 12 worked example): the final matrix per nonterminal.
let private bookT13: Map<string, Set<int * int>> =
    Map.ofList
        [ "A", Set.ofList [ (0, 1); (1, 2); (2, 0) ]
          "B", Set.ofList [ (0, 3); (3, 0) ]
          "S", Set.ofList [ (0, 0); (0, 3); (1, 0); (1, 3); (2, 0); (2, 3) ]
          "S1", Set.ofList [ (0, 0); (0, 3); (1, 0); (1, 3); (2, 0); (2, 3) ] ]

// The book's productive-step sequence: one new cell per iteration, T1..T12.
let private bookSteps: AzimovCfpq.CfpqCell<string> list =
    [ { Nt = Nonterminal "S"
        Row = 2
        Col = 3 }
      { Nt = Nonterminal "S1"
        Row = 2
        Col = 0 }
      { Nt = Nonterminal "S"
        Row = 1
        Col = 0 }
      { Nt = Nonterminal "S1"
        Row = 1
        Col = 3 }
      { Nt = Nonterminal "S"
        Row = 0
        Col = 3 }
      { Nt = Nonterminal "S1"
        Row = 0
        Col = 0 }
      { Nt = Nonterminal "S"
        Row = 2
        Col = 0 }
      { Nt = Nonterminal "S1"
        Row = 2
        Col = 3 }
      { Nt = Nonterminal "S"
        Row = 1
        Col = 3 }
      { Nt = Nonterminal "S1"
        Row = 1
        Col = 0 }
      { Nt = Nonterminal "S"
        Row = 0
        Col = 0 }
      { Nt = Nonterminal "S1"
        Row = 0
        Col = 3 } ]

[<Fact>]
let ``book example reachability fixed point matches the book T0..T13 trace`` () =
    let result =
        AzimovCfpq.evaluate
            CfpqSemiring.ReachabilitySemiring
            Grammar.freshStringNonterminal
            CfpqTestHelpers.bookGrammar
            CfpqTestHelpers.bookGraph

    Assert.Equal(13, result.Iterations)
    Assert.Equal(12, List.length result.Steps)

    // Exactly the four nonterminals of the book.
    let nts =
        result.Matrices
        |> Map.toList
        |> List.map (fun (nt, _) ->
            match nt with
            | Nonterminal s -> s)
        |> Set.ofList

    Assert.Equal<Set<string>>(Set.ofList [ "A"; "B"; "S"; "S1" ], nts)

    // Final matrices equal the book's T13 cell by cell.
    for name, expected in Map.toList bookT13 do
        let actual =
            AzimovCfpq.answer result (Nonterminal name) |> CfpqTestHelpers.trueCells

        let matches = actual = expected
        Assert.True(matches, $"nonterminal {name}: expected {expected}, got {actual}")

    // Per-step changed cells equal the book sequence; iteration numbers are 1..12.
    let actualSteps = result.Steps |> List.map (fun s -> s.ChangedCells)

    Assert.Equal<AzimovCfpq.CfpqCell<string> list list>(List.map (fun c -> [ c ]) bookSteps, actualSteps)

    Assert.Equal<int list>(
        [ for k in 1..12 do
              k ],
        result.Steps |> List.map (fun s -> s.Iteration)
    )

[<Fact>]
let ``book example singlePath extraction yields a Valiant-accepted walk for every reachable pair`` () =
    let result =
        AzimovCfpq.evaluate
            CfpqSemiring.SinglePathSemiring
            Grammar.freshStringNonterminal
            CfpqTestHelpers.bookGrammar
            CfpqTestHelpers.bookGraph

    // The same rule set evaluate used internally (toCnf with the deterministic fresh names).
    let rules = CfpqTestHelpers.wcnfRules CfpqTestHelpers.bookGrammar
    let graph = CfpqTestHelpers.bookGraph
    let reachable = bookT13.["S"]

    for i in 0..3 do
        for j in 0..3 do
            let extracted =
                AzimovCfpq.extractSinglePath rules graph result.Matrices (Nonterminal "S") i j

            if Set.contains (i, j) reachable then
                match extracted with
                | None -> Assert.True(false, $"pair ({i},{j}): expected a path, got None")
                | Some path ->
                    // Endpoints and continuity of the walk.
                    let verts =
                        match path with
                        | [] -> []
                        | e :: _ -> e.From :: List.map (fun e -> e.To) path

                    if List.isEmpty path then
                        Assert.Equal(i, j)
                    else
                        Assert.Equal(i, List.head verts)
                        Assert.Equal(j, List.last verts)

                        for k in 0 .. List.length path - 2 do
                            let v = path.[k].To
                            let u2 = path.[k + 1].From
                            Assert.Equal(v, u2)

                    // Every edge exists in the graph.
                    for e in path do
                        let has =
                            Map.tryFind e.Label graph.Labels |> Option.exists (fun m -> m.[e.From, e.To])

                        Assert.True(has, $"edge ({e.From},{e.Label},{e.To}) is not in the graph")

                    // The label word is accepted by Valiant from S.
                    let word = path |> List.map (fun e -> Terminal e.Label)

                    let accepted =
                        Valiant.parse Grammar.freshStringNonterminal CfpqTestHelpers.bookGrammar word

                    Assert.True(accepted, $"word {word} is not accepted from S")
            else
                Assert.Equal(None, extracted)

[<Fact>]
let ``book example allPaths enumeration equals Valiant-filtered simple paths`` () =
    let result =
        AzimovCfpq.evaluate
            CfpqSemiring.AllPathsSemiring
            Grammar.freshStringNonterminal
            CfpqTestHelpers.bookGrammar
            CfpqTestHelpers.bookGraph

    let rules = CfpqTestHelpers.wcnfRules CfpqTestHelpers.bookGrammar
    let graph = CfpqTestHelpers.bookGraph

    for i in 0..3 do
        for j in 0..3 do
            let enumerated =
                AzimovCfpq.enumerateAllPaths rules graph result.Matrices (Nonterminal "S") i j
                |> Set.ofList

            let expected =
                CfpqTestHelpers.bruteForceSimplePaths graph i j
                |> List.filter (fun p ->
                    Valiant.parse
                        Grammar.freshStringNonterminal
                        CfpqTestHelpers.bookGrammar
                        (p |> List.map (fun e -> Terminal e.Label)))
                |> Set.ofList

            let matches = enumerated = expected
            Assert.True(matches, $"pair ({i},{j}): enumerated {enumerated} but expected {expected}")

    // Two-edge derivation: S derives "ab" and the only simple 2->3 path is 2-a-0-b-3.
    let twoEdge =
        AzimovCfpq.enumerateAllPaths rules graph result.Matrices (Nonterminal "S") 2 3

    let expectedTwoEdge =
        Set.ofList [ [ { From = 2; Label = "a"; To = 0 }; { From = 0; Label = "b"; To = 3 } ] ]

    Assert.Equal<Set<Trans<string> list>>(expectedTwoEdge, Set.ofList twoEdge)

    // Direct b-edge: the only simple 0->3 path is the single b-edge, whose word "b" is not in L(S).
    let directB =
        AzimovCfpq.enumerateAllPaths rules graph result.Matrices (Nonterminal "S") 0 3

    Assert.Empty(directB)
