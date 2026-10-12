module CfpqSemiringTests

open System
open FSharpPlus.Data
open Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra

[<Fact>]
let ``reachability semiring operations`` () =
    let s = CfpqSemiring.ReachabilitySemiring
    Assert.True(s.Add true false)
    Assert.False(s.Add false false)
    Assert.True(s.Mult 0 true true)
    Assert.False(s.Mult 0 true false)
    Assert.True(s.InitPath 0 1)
    Assert.Equal(None, CfpqSemiring.addCell s None None)
    Assert.Equal(Some true, CfpqSemiring.addCell s None (Some true))
    Assert.Equal(Some true, CfpqSemiring.addCell s (Some false) (Some true))
    Assert.Equal(None, CfpqSemiring.multCell s 0 None (Some true))
    Assert.Equal(Some false, CfpqSemiring.multCell s 0 (Some true) (Some false))

[<Fact>]
let ``singlePath semiring keeps minimum height with middle tie-break`` () =
    let s = CfpqSemiring.SinglePathSemiring
    let low: CfpqSemiring.PathIndex = { Middle = 5; Height = 2 }
    let high: CfpqSemiring.PathIndex = { Middle = 1; Height = 3 }
    Assert.Equal(low, s.Add low high)
    let tieA: CfpqSemiring.PathIndex = { Middle = 2; Height = 3 }
    let tieB: CfpqSemiring.PathIndex = { Middle = 7; Height = 3 }
    Assert.Equal(tieA, s.Add tieA tieB)
    let expectedMult: CfpqSemiring.PathIndex = { Middle = 4; Height = 4 }
    Assert.Equal(expectedMult, s.Mult 4 low high)
    let expectedInit: CfpqSemiring.PathIndex = { Middle = 2; Height = 1 }
    Assert.Equal(expectedInit, s.InitPath 2 9)
    let cell = CfpqSemiring.addCell s (Some high) (Some low)
    Assert.Equal(Some low, cell)
    Assert.Equal(None, CfpqSemiring.multCell s 0 None (Some low))

[<Fact>]
let ``allPaths semiring unions splits and records the middle vertex`` () =
    let s = CfpqSemiring.AllPathsSemiring
    let sentinel = CfpqSemiring.allPathsSentinel
    Assert.Equal(CfpqSemiring.Base, s.Add CfpqSemiring.Base CfpqSemiring.Base)

    Assert.Equal(
        CfpqSemiring.Splits(NonEmptyList.ofList [ 1; sentinel ]),
        s.Add CfpqSemiring.Base (CfpqSemiring.Splits(NonEmptyList.ofList [ 1 ]))
    )

    Assert.Equal(
        CfpqSemiring.Splits(NonEmptyList.ofList [ 1; 3 ]),
        s.Add (CfpqSemiring.Splits(NonEmptyList.ofList [ 3 ])) (CfpqSemiring.Splits(NonEmptyList.ofList [ 1 ]))
    )

    Assert.Equal(CfpqSemiring.Splits(NonEmptyList.ofList [ 2 ]), s.Mult 2 CfpqSemiring.Base CfpqSemiring.Base)
    Assert.Equal(CfpqSemiring.Base, s.InitPath 0 1)

    let cell =
        CfpqSemiring.addCell s (Some CfpqSemiring.Base) (Some(CfpqSemiring.Splits(NonEmptyList.ofList [ 2 ])))

    Assert.Equal(Some(CfpqSemiring.Splits(NonEmptyList.ofList [ 2; sentinel ])), cell)

[<Fact>]
let ``classifyRules classifies epsilon terminal and binary rules`` () =
    let g: Grammar<string, string> =
        { Rules =
            [ { Lhs = Nonterminal "A"
                Rhs = Rhs.EpsilonRhs }
              { Lhs = Nonterminal "B"
                Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "a") ]) }
              { Lhs = Nonterminal "C"
                Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "A"); Symbol.N(Nonterminal "B") ]) } ]
          Start = Nonterminal "C" }

    let rules = AzimovCfpq.classifyRules g
    Assert.Equal(3, List.length rules)

    let hasEpsilon =
        List.exists
            (function
            | AzimovCfpq.WcnfRule.Epsilon x -> x = Nonterminal "A"
            | _ -> false)
            rules

    let hasTerminal =
        List.exists
            (function
            | AzimovCfpq.WcnfRule.Terminal(x, t) -> x = Nonterminal "B" && t = "a"
            | _ -> false)
            rules

    let hasBinary =
        List.exists
            (function
            | AzimovCfpq.WcnfRule.Binary(x, b, c) -> x = Nonterminal "C" && b = Nonterminal "A" && c = Nonterminal "B"
            | _ -> false)
            rules

    Assert.True(hasEpsilon, "missing epsilon rule")
    Assert.True(hasTerminal, "missing terminal rule")
    Assert.True(hasBinary, "missing binary rule")

[<Fact>]
let ``classifyRules rejects non-wcnf right-hand sides`` () =
    let mixed: Grammar<string, string> =
        { Rules =
            [ { Lhs = Nonterminal "A"
                Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "B"); Symbol.T(Terminal "a") ]) } ]
          Start = Nonterminal "A" }

    Assert.Throws<ArgumentException>(fun () -> AzimovCfpq.classifyRules mixed |> ignore)
    |> ignore

    let twoTerminals: Grammar<string, string> =
        { Rules =
            [ { Lhs = Nonterminal "A"
                Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "a"); Symbol.T(Terminal "b") ]) } ]
          Start = Nonterminal "A" }

    Assert.Throws<ArgumentException>(fun () -> AzimovCfpq.classifyRules twoTerminals |> ignore)

[<Fact>]
let ``extractSinglePath on the book example returns the two-edge derivation`` () =
    let result =
        AzimovCfpq.evaluate
            CfpqSemiring.SinglePathSemiring
            Grammar.freshStringNonterminal
            CfpqTestHelpers.bookGrammar
            CfpqTestHelpers.bookGraph

    let rules = AzimovCfpq.classifyRules CfpqTestHelpers.bookGrammar
    Assert.Equal(Some({ Middle = 0; Height = 2 }: CfpqSemiring.PathIndex), result.Matrices.[Nonterminal "S"].[2, 3])

    let path =
        AzimovCfpq.extractSinglePath rules CfpqTestHelpers.bookGraph result.Matrices (Nonterminal "S") 2 3

    let expected =
        [ { From = 2; Label = "a"; To = 0 }; { From = 0; Label = "b"; To = 3 } ]

    Assert.Equal(Some expected, path)

[<Fact>]
let ``extractSinglePath returns epsilon empty list and None for missing cells`` () =
    let graph: CfpqInput.CfpqGraph<string> =
        { VertexCount = 2
          Labels = Map.ofList [ "a", Matrix.create 2 2 (fun i j -> (i, j) = (0, 1)) ] }

    let rules =
        [ AzimovCfpq.WcnfRule.Epsilon(Nonterminal "A")
          AzimovCfpq.WcnfRule.Terminal(Nonterminal "A", "a") ]

    let m =
        Matrix.create 2 2 (fun i j ->
            if (i, j) = (0, 0) || (i, j) = (0, 1) then
                Some({ Middle = i; Height = 1 }: CfpqSemiring.PathIndex)
            else
                None)

    let matrices = Map.add (Nonterminal "A") m Map.empty

    Assert.Equal(Some [], AzimovCfpq.extractSinglePath rules graph matrices (Nonterminal "A") 0 0)
    let edge = [ { From = 0; Label = "a"; To = 1 } ]
    Assert.Equal(Some edge, AzimovCfpq.extractSinglePath rules graph matrices (Nonterminal "A") 0 1)
    Assert.Equal(None, AzimovCfpq.extractSinglePath rules graph matrices (Nonterminal "A") 1 0)

[<Fact>]
let ``enumerateAllPaths equals brute-force simple paths on a cyclic graph`` () =
    // Cyclic graph: a-edges 0->1, 1->2, 2->0; b-edge 0->2. Grammar S -> SS | a derives a+.
    let graph: CfpqInput.CfpqGraph<string> =
        { VertexCount = 3
          Labels =
            Map.ofList
                [ "a", Matrix.create 3 3 (fun i j -> (i, j) = (0, 1) || (i, j) = (1, 2) || (i, j) = (2, 0))
                  "b", Matrix.create 3 3 (fun i j -> (i, j) = (0, 2)) ] }

    let grammar: Grammar<string, string> =
        { Rules =
            [ { Lhs = Nonterminal "S"
                Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "S"); Symbol.N(Nonterminal "S") ]) }
              { Lhs = Nonterminal "S"
                Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "a") ]) } ]
          Start = Nonterminal "S" }

    let result =
        AzimovCfpq.evaluate CfpqSemiring.AllPathsSemiring Grammar.freshStringNonterminal grammar graph

    let rules = CfpqTestHelpers.wcnfRules grammar

    for i in 0..2 do
        for j in 0..2 do
            let enumerated =
                AzimovCfpq.enumerateAllPaths rules graph result.Matrices (Nonterminal "S") i j
                |> Set.ofList

            let expected =
                CfpqTestHelpers.bruteForceSimplePaths graph i j
                |> List.filter (fun p ->
                    Valiant.parse Grammar.freshStringNonterminal grammar (p |> List.map (fun e -> Terminal e.Label)))
                |> Set.ofList

            let matches = enumerated = expected
            Assert.True(matches, $"pair ({i},{j}): enumerated {enumerated} but expected {expected}")
