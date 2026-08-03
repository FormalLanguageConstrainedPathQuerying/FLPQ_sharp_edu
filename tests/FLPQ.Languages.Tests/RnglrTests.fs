module RnglrTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private accepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted

let private checkReject =
    TestHelpers.checkReject Rnglr.buildPathIndex PathIndex.isAccepted

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private astar = LanguageRegistry.AStar
let private anbn = LanguageRegistry.ANBN
let private astarBStar = LanguageRegistry.AStarBStar
let private epsilonOnly = LanguageRegistry.EpsilonOnly

module RnglrSharedAcceptance =
    let private rnglrCases =
        ParsingTestCases.AcceptanceCases.allCases
        |> List.filter (fun c ->
            c.GrammarName <> "grammar_aSa_eps"
            && c.LanguageName <> "ArithExpr (arithmetic expressions)"
            && c.LanguageName <> "TwoTrackDyck (ab/c, ax/y)"
            && c.LanguageName <> "AltAB ({a, b})"
            && c.LanguageName <> "LL2Test ({abc, aad})"
            && c.LanguageName <> "LL3Test ({abcx, abdy})")

    [<Fact>]
    let ``All registered accept/reject strings handled correctly`` () =
        for case in rnglrCases do
            if case.ExpectedAccepted then
                Assert.True(accepts case.Rsm case.Input, $"{case.LanguageName}/{case.GrammarName}: accept {case.Input}")
            else
                Assert.True(
                    checkReject case.Rsm case.Input,
                    $"{case.LanguageName}/{case.GrammarName}: reject {case.Input}"
                )

module RnglrTreeYield =
    [<Fact>]
    let ``S -> a S b S | eps tree yield matches inputs`` () =
        for input in ParsingTestCases.TreeYieldCases.grammar1Inputs do
            let desc = (input |> List.map (fun (Terminal x) -> x) |> String.concat " ")
            Assert.True(accepts (TestHelpers.grammarToRsm dyck1.Grammars[0].Grammar) input, $"tree yield: {desc}")

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches inputs`` () =
        for input in ParsingTestCases.TreeYieldCases.grammarSaSb_epsInputs do
            let desc = (input |> List.map (fun (Terminal x) -> x) |> String.concat " ")
            Assert.True(accepts (TestHelpers.grammarToRsm dyck1.Grammars[2].Grammar) input, $"tree yield: {desc}")

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches inputs`` () =
        for input in ParsingTestCases.TreeYieldCases.grammar2Inputs do
            let desc = (input |> List.map (fun (Terminal x) -> x) |> String.concat " ")
            Assert.True(accepts (TestHelpers.grammarToRsm dyck1.Grammars[1].Grammar) input, $"tree yield: {desc}")

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = dyck1.AcceptStrings[6]
        Assert.True(accepts dyck1.Grammars[3].Rsm input)

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts dual dyck 10`` () =
        let input = LanguageRegistry.DualDyck.AcceptStrings[0]
        let rsm = LanguageRegistry.DualDyck.Grammars[0].Rsm
        Assert.True(accepts rsm input, "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts dual dyck 16`` () =
        let input = LanguageRegistry.DualDyck.AcceptStrings[1]
        let rsm = LanguageRegistry.DualDyck.Grammars[0].Rsm
        Assert.True(accepts rsm input, "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts dual dyck 12`` () =
        let input = LanguageRegistry.DualDyck.AcceptStrings[2]
        let rsm = LanguageRegistry.DualDyck.Grammars[0].Rsm
        Assert.True(accepts rsm input, "Should produce a tree")

module RnglrRightNullable =
    let private g = astarBStar.Grammars[0].Grammar
    let private lang = astarBStar

    [<Fact>]
    let ``S -> A B accepts all accept strings`` () =
        for s in lang.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> A B rejects all reject strings`` () =
        for s in lang.RejectStrings do
            Assert.True(checkReject (TestHelpers.grammarToRsm g) s)

module RnglrReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        let g = epsilonOnly.Grammars |> List.find (fun g -> g.Name = "grammarCascade")
        Assert.True(accepts (TestHelpers.grammarToRsm g.Grammar) [], "Should accept and produce tree")

module RnglrPropertyTreeYield =
    let private propertyRunner =
        ParsingTestCases.Runners.runPropertyTreeYieldTest accepts

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Ab =
        [<Property>]
        let ``S -> a S b S | eps tree yield (Dyck1 grammar1)`` (s: string) =
            propertyRunner dyck1.Grammars[0].Grammar "S -> a S b S | eps" s

        [<Property>]
        let ``S -> S S | a S b | eps tree yield (Dyck1 grammar2)`` (s: string) =
            propertyRunner dyck1.Grammars[1].Grammar "S -> S S | a S b | eps" s

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module A =
        [<Property>]
        let ``S -> a S | a tree yield (APlus grammar3)`` (s: string) =
            propertyRunner aplus.Grammars[0].Grammar "S -> a S | a" s

        [<Property>]
        let ``S -> S a | a tree yield (APlus grammar4)`` (s: string) =
            propertyRunner aplus.Grammars[1].Grammar "S -> S a | a" s

        [<Property>]
        let ``S -> N a* tree yield (APlus grammar11)`` (s: string) =
            propertyRunner aplus.Grammars[3].Grammar "S -> N a*" s

        [<Property>]
        let ``S -> a* N tree yield (APlus grammar12)`` (s: string) =
            propertyRunner aplus.Grammars[4].Grammar "S -> a* N" s

        [<Property>]
        let ``S -> N* tree yield (AStar grammar13)`` (s: string) =
            propertyRunner astar.Grammars[0].Grammar "S -> N*" s

        [<Property>]
        let ``S -> a | S S | S S S tree yield (APlus grammar14)`` (s: string) =
            propertyRunner aplus.Grammars[5].Grammar "S -> a | S S | S S S" s

module SppfDotTests =

    let private buildSppf (grammarText: string) (input: Terminal<string> list) : SPPF<string, string> =
        let rsm = RsmBuilder.buildRSMFromText grammarText
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let freshStart = Nonterminal("S'")
        let graph = TestHelpers.terminalsToGraph input
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart ersm graph
        let vc = Graph.vertexCount graph

        let flatExt = ersm.ExtendedRsm

        let startGlobal =
            match flatExt.BlockStart.TryGetValue(flatExt.StartBlock) with
            | true, gs -> gs
            | false, _ -> 0

        let finalGlobal = startGlobal + 1

        let rootRanges =
            let entries = PathIndex.get pathIndex startGlobal 0 finalGlobal (vc - 1)

            if not (Set.isEmpty entries) then
                [ { FromState = startGlobal
                    FromVertex = 0
                    ToState = finalGlobal
                    ToVertex = vc - 1 } ]
            else
                []

        Sppf.buildSppfFromIndex
            pathIndex
            rootRanges
            (Some(flatExt.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
            (Some(RSM.blockFinalsMap flatExt))

    [<Fact>]
    let ``RNGLR SPPF contains all terminals for S->aSb|SS|eps with aababb`` () =
        let grammarText = dyck1.Grammars[1].Text
        let input = dyck1.AcceptStrings[4]

        let sppf = buildSppf grammarText input

        TestHelpers.assertSppfInvariant sppf

        let terminalPositions =
            Graph.vertices sppf.Graph
            |> List.choose (fun (_, v) ->
                match v with
                | SppfNodeInfo.SppfTerminal(Terminal t, l, r) -> Some(t, l, r)
                | _ -> None)
            |> Set.ofList

        let expected: Set<string * int * int> =
            set [ ("a", 0, 1); ("a", 1, 2); ("b", 2, 3); ("a", 3, 4); ("b", 4, 5); ("b", 5, 6) ]

        Assert.Equal<Set<string * int * int>>(expected, terminalPositions)

    [<Fact>]
    let ``RNGLR SPPF has root nodes for S->aSb|SS|eps with aababb`` () =
        let grammarText = dyck1.Grammars[1].Text
        let input = dyck1.AcceptStrings[4]

        let sppf = buildSppf grammarText input

        TestHelpers.assertSppfInvariant sppf

        Assert.NotEmpty(sppf.RootIndices)

module RnglrEpsilonGrammars =
    [<Fact>]
    let ``all epsilon grammars accept empty and reject non-empty`` () =
        ParsingTestCases.Runners.runEpsilonTests accepts checkReject
