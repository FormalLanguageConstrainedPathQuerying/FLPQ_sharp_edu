module GllTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private accepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted

let private checkReject =
    TestHelpers.checkReject GLL.buildPathIndex PathIndex.isAccepted

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private astar = LanguageRegistry.AStar
let private astarBStar = LanguageRegistry.AStarBStar
let private epsilonOnly = LanguageRegistry.EpsilonOnly

module GllSharedAcceptance =
    [<Fact>]
    let ``All registered accept/reject strings handled correctly`` () =
        for case in ParsingTestCases.AcceptanceCases.allCases do
            if case.ExpectedAccepted then
                Assert.True(accepts case.Rsm case.Input, $"{case.LanguageName}/{case.GrammarName}: accept {case.Input}")
            else
                Assert.True(
                    checkReject case.Rsm case.Input,
                    $"{case.LanguageName}/{case.GrammarName}: reject {case.Input}"
                )

module GllTreeYield =
    [<Fact>]
    let ``S -> a S b S | eps tree yield matches inputs`` () =
        for input in ParsingTestCases.TreeYieldCases.grammar1Inputs do
            let desc = (input |> List.map (fun (Terminal x) -> x) |> String.concat " ")
            Assert.True(accepts dyck1.Grammars[0].Rsm input, $"tree yield: {desc}")

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches inputs`` () =
        for input in ParsingTestCases.TreeYieldCases.grammarSaSb_epsInputs do
            let desc = (input |> List.map (fun (Terminal x) -> x) |> String.concat " ")
            Assert.True(accepts dyck1.Grammars[2].Rsm input, $"tree yield: {desc}")

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches inputs`` () =
        for input in ParsingTestCases.TreeYieldCases.grammar2Inputs do
            let desc = (input |> List.map (fun (Terminal x) -> x) |> String.concat " ")
            Assert.True(accepts dyck1.Grammars[1].Rsm input, $"tree yield: {desc}")

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = dyck1.AcceptStrings[6]
        Assert.True(accepts dyck1.Grammars[3].Rsm input)

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = dyck1.AcceptStrings[6]
        Assert.True(accepts dyck1.Grammars[2].Rsm input)

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = dyck1.AcceptStrings[5]
        Assert.True(accepts dyck1.Grammars[2].Rsm input)

    [<Fact>]
    let ``Tree extraction for S->aSbS|eps on abab produces tree with correct yield`` () =
        let s = dyck1.AcceptStrings[0]
        Assert.True(accepts dyck1.Grammars[0].Rsm s)

    [<Fact>]
    let ``Tree extraction for S->aSb|eps on aabb produces tree with correct yield`` () =
        let s = LanguageRegistry.ANBN.AcceptStrings[2]
        Assert.True(accepts LanguageRegistry.ANBN.Grammars[0].Rsm s)

    [<Fact>]
    let ``Tree extraction for S->a|b on a produces tree with leaf`` () =
        let lang = LanguageRegistry.AltAB

        for s in lang.AcceptStrings do
            Assert.True(accepts lang.Grammars[0].Rsm s)

module GllPropertyTreeYield =
    let private propertyRunner =
        ParsingTestCases.Runners.runPropertyTreeYieldTest accepts

    [<Property>]
    let ``S -> a S b S | eps tree yield (Dyck1 grammar1)`` (s: string) =
        propertyRunner dyck1.Grammars[0].Rsm "S -> a S b S | eps" s

    [<Property>]
    let ``S -> S S | a S b | eps tree yield (Dyck1 grammar2)`` (s: string) =
        propertyRunner dyck1.Grammars[1].Rsm "S -> S S | a S b | eps" s

    [<Property>]
    let ``S -> a S | a tree yield (APlus grammar3)`` (s: string) =
        propertyRunner aplus.Grammars[0].Rsm "S -> a S | a" s

    [<Property>]
    let ``S -> S a | a tree yield (APlus grammar4)`` (s: string) =
        propertyRunner aplus.Grammars[1].Rsm "S -> S a | a" s

    [<Property>]
    let ``S -> N a* tree yield (APlus grammar11)`` (s: string) =
        propertyRunner aplus.Grammars[3].Rsm "S -> N a*" s

    [<Property>]
    let ``S -> a* N tree yield (APlus grammar12)`` (s: string) =
        propertyRunner aplus.Grammars[4].Rsm "S -> a* N" s

    [<Property>]
    let ``S -> N* tree yield (AStar grammar13)`` (s: string) =
        propertyRunner astar.Grammars[0].Rsm "S -> N*" s

    [<Property>]
    let ``S -> a | S S | S S S tree yield (APlus grammar14)`` (s: string) =
        propertyRunner aplus.Grammars[5].Rsm "S -> a | S S | S S S" s

module GllRightNullable =
    let private lang = astarBStar

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts accept strings`` () =
        for s in lang.AcceptStrings do
            Assert.True(accepts astarBStar.Grammars[0].Rsm s)

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps rejects reject strings`` () =
        for s in lang.RejectStrings do
            Assert.False(accepts astarBStar.Grammars[0].Rsm s)

module GllReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        let g = epsilonOnly.Grammars |> List.find (fun g -> g.Name = "viaCascade")
        Assert.True(accepts g.Rsm [])

module GllEpsilonGrammars =
    [<Fact>]
    let ``all epsilon grammars accept empty and reject non-empty`` () =
        ParsingTestCases.Runners.runEpsilonTests accepts (fun g input -> not (accepts g input))

module GllMalformedRsm =

    let private run (rsm: RSM<string, string>) (input: string list) : bool =
        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart rsm
        let graph = GLL.stringToGraph input
        let pi = GLL.buildPathIndex freshStart ersm graph
        PathIndex.isAccepted pi ersm (Graph.vertexCount graph)

    [<Fact>]
    let ``GLL skips dangling nonterminal call without a block`` () =
        // S: 0 --N(A)--> 1 --N(GHOST)--> 2f; A: 3 --a--> 4f. GHOST has no block, so the
        // call at S state 1 hits the `blockStart.TryGetValue -> false` branch (Gll.fs:208).
        let ntS = Nonterminal "S"
        let ntA = Nonterminal "A"
        let ntGhost = Nonterminal "GHOST"

        let rsm =
            RsmFixtures.mkRsm
                [| { BlockNonterminal = ntS
                     LocalState = 0
                     IsFinal = false }
                   { BlockNonterminal = ntS
                     LocalState = 1
                     IsFinal = false }
                   { BlockNonterminal = ntS
                     LocalState = 2
                     IsFinal = true }
                   { BlockNonterminal = ntA
                     LocalState = 0
                     IsFinal = false }
                   { BlockNonterminal = ntA
                     LocalState = 1
                     IsFinal = true } |]
                [ { From = 0
                    Label = AutomatonLabel.ATerm(RsmSymbol.RNonterm ntA)
                    To = 1 }
                  { From = 1
                    Label = AutomatonLabel.ATerm(RsmSymbol.RNonterm ntGhost)
                    To = 2 }
                  { From = 3
                    Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "a"))
                    To = 4 } ]
                [ (ntS, 0); (ntA, 3) ]
                [ 2; 4 ]
                ntS

        Assert.False(run rsm [ "a" ])

    [<Fact>]
    let ``GLL tolerates return state whose block is missing from BlockStart`` () =
        // S: 0 --N(A)--> 1f but StateInfo.[1].BlockNonterminal = GHOST, which has no
        // BlockStart entry. The return handling hits the `blockStart.TryGetValue -> false`
        // branch (Gll.fs:370) and the final-state handler hits the NonEmptyRange fallback
        // (Gll.fs:309). Range bookkeeping still completes, so the input is accepted.
        let ntS = Nonterminal "S"
        let ntA = Nonterminal "A"
        let ntGhost = Nonterminal "GHOST"

        let rsm =
            RsmFixtures.mkRsm
                [| { BlockNonterminal = ntS
                     LocalState = 0
                     IsFinal = false }
                   { BlockNonterminal = ntGhost
                     LocalState = 1
                     IsFinal = true }
                   { BlockNonterminal = ntA
                     LocalState = 0
                     IsFinal = false }
                   { BlockNonterminal = ntA
                     LocalState = 1
                     IsFinal = true } |]
                [ { From = 0
                    Label = AutomatonLabel.ATerm(RsmSymbol.RNonterm ntA)
                    To = 1 }
                  { From = 2
                    Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "a"))
                    To = 3 } ]
                [ (ntS, 0); (ntA, 2) ]
                [ 1; 3 ]
                ntS

        Assert.True(run rsm [ "a" ])
