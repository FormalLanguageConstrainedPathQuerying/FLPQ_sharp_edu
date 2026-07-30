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
let private anbn = LanguageRegistry.ANBN

module RnglrAcceptance =
    let private singleA = LanguageRegistry.SingleA
    let private singleAB = LanguageRegistry.SingleAB
    let private anb = LanguageRegistry.ANB

    [<Fact>]
    let ``S -> a accepts all accept strings`` () =
        for s in singleA.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarS2a) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> a rejects all reject strings`` () =
        for s in singleA.RejectStrings do
            Assert.True(checkReject TestGrammars.grammarS2a s)

    [<Fact>]
    let ``S -> a b accepts all accept strings`` () =
        for s in singleAB.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarAB) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> a S | b accepts all accept strings`` () =
        for s in anb.AcceptStrings do
            Assert.True(
                accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aS_b) s,
                "Should accept and produce tree"
            )

    [<Fact>]
    let ``S -> a S | b rejects all reject strings`` () =
        for s in anb.RejectStrings do
            Assert.True(checkReject TestGrammars.grammar_aS_b s)

    [<Fact>]
    let ``S -> a S b S | eps accepts Dyck1 accept strings`` () =
        let g = dyck1.Grammars[0].Grammar

        for s in dyck1.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> a S b | eps accepts ANBN accept strings`` () =
        let g = anbn.Grammars[0].Grammar

        for s in anbn.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> a S b | eps rejects ANBN reject strings`` () =
        for s in anbn.RejectStrings do
            Assert.True(checkReject anbn.Grammars[0].Grammar s)

    [<Fact>]
    let ``S -> a S b | eps | S S accepts Dyck1 accept strings`` () =
        let g = dyck1.Grammars[1].Grammar

        for s in dyck1.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> a S b | eps | S S accepts a b (no infinite loop)`` () =
        let g = dyck1.Grammars[1].Grammar
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "b" ], "Should accept and produce tree")

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts APlus accept strings`` () =
        let g = aplus.Grammars[0].Grammar

        for s in aplus.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts APlus accept strings`` () =
        let g = aplus.Grammars[1].Grammar

        for s in aplus.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

module RnglrEquivalence =
    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Ab =
        [<Property>]
        let ``RNGLR and CYK agree on Dyck1 grammar1`` (s: string) =
            let g = dyck1.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``RNGLR and GLL agree on Dyck1 grammar1`` (s: string) =
            let g = dyck1.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            accepts (TestHelpers.grammarToRsm g) input = accepts (TestHelpers.grammarToRsm g) input

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module A =
        [<Property>]
        let ``RNGLR and CYK agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``RNGLR and GLL agree on APlus grammar3`` (s: string) =
            let g = aplus.Grammars[0].Grammar
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            accepts (TestHelpers.grammarToRsm g) input = accepts (TestHelpers.grammarToRsm g) input

module RnglrRightNullable =
    let private g = TestGrammars.grammarRightNullable
    let private lang = LanguageRegistry.AStarBStar

    [<Fact>]
    let ``S -> A B accepts all accept strings`` () =
        for s in lang.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> A B rejects all reject strings`` () =
        for s in lang.RejectStrings do
            Assert.True(checkReject g s)

module RnglrReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarCascade) [], "Should accept and produce tree")

module RnglrRegexEquivalence =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module A =
        [<Property(MaxTest = 50)>]
        let ``S -> a* matches DFA for a*`` (s: string) =
            SharedParsingTests.Runners.runRegexEquivalenceTests accepts (fun c -> c = "a") "a *" s

        [<Property(MaxTest = 50)>]
        let ``S -> a* a* matches DFA for a* a*`` (s: string) =
            SharedParsingTests.Runners.runRegexEquivalenceTests accepts (fun c -> c = "a") "a * a *" s

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Ab =
        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
            SharedParsingTests.Runners.runRegexEquivalenceTests accepts (fun c -> c = "a" || c = "b") "( a | b ) *" s

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
            SharedParsingTests.Runners.runRegexEquivalenceTests
                accepts
                (fun c -> c = "a" || c = "b" || c = "c")
                "( a | b ) * ( a | c ) *"
                s

module RnglrGrammarAcceptanceAndTree =
    let private grammar1 = TestGrammars.grammar11
    let private grammar2 = TestGrammars.grammar12
    let private grammar3 = TestGrammars.grammar13
    let private grammar4 = TestGrammars.grammar14

    module Grammar1 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG1 do
                Assert.True(
                    accepts (TestHelpers.grammarToRsm grammar1) input,
                    $"Should accept and produce tree: {desc}"
                )

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG1 do
                Assert.True(checkReject grammar1 input, $"Should reject: {desc}")

    module Grammar2 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG2 do
                Assert.True(
                    accepts (TestHelpers.grammarToRsm grammar2) input,
                    $"Should accept and produce tree: {desc}"
                )

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG2 do
                Assert.True(checkReject grammar2 input, $"Should reject: {desc}")

    module Grammar3 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG3 do
                Assert.True(
                    accepts (TestHelpers.grammarToRsm grammar3) input,
                    $"Should accept and produce tree: {desc}"
                )

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG3 do
                Assert.True(checkReject grammar3 input, $"Should reject: {desc}")

    module Grammar4 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG4 do
                Assert.True(
                    accepts (TestHelpers.grammarToRsm grammar4) input,
                    $"Should accept and produce tree: {desc}"
                )

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG4 do
                Assert.True(checkReject grammar4 input, $"Should reject: {desc}")

module RnglrGrammar159A =
    let private grammar = dyck1.Grammars[0].Grammar

    [<Fact>]
    let ``S -> a S b S | eps accepts and yields trees`` () =
        for input in SharedParsingTests.Grammar159Cases.grammar1Inputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"Should produce a tree: {desc}")

module RnglrGrammar159B =
    let private grammar = TestGrammars.grammarSaSb_eps

    [<Fact>]
    let ``S -> S a S b | eps accepts and yields trees`` () =
        for input in SharedParsingTests.Grammar159Cases.grammarSaSb_epsInputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"Should produce a tree: {desc}")

module RnglrGrammar159C =
    let private grammar = dyck1.Grammars[1].Grammar

    [<Fact>]
    let ``S -> S S | a S b | eps accepts and yields trees`` () =
        for input in SharedParsingTests.Grammar159Cases.grammar2Inputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"Should produce a tree: {desc}")

module RnglrGrammar159D =
    let private rsm = TestHelpers.buildRegexRsm "(a S b)*"

    [<Fact>]
    let ``S -> (a S b)* accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(accepts rsm [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ], "Should produce a tree")

    let private rsm2 =
        let r = RsmBuilder.buildRSMFromText "S -> S1 S2\nS1 -> (a S1 b)*\nS2 -> (c S2 d)*"
        { r with StartBlock = Nonterminal "S" }

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(accepts rsm2 [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ], "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts and yields tree: a a a b a b b a b b c c d c d d`` () =
        let input =
            [ "a"
              "a"
              "a"
              "b"
              "a"
              "b"
              "b"
              "a"
              "b"
              "b"
              "c"
              "c"
              "d"
              "c"
              "d"
              "d" ]

        Assert.True(accepts rsm2 input, "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts and yields tree: a a a b a b b a b b c d`` () =
        Assert.True(
            accepts rsm2 [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b"; "c"; "d" ],
            "Should produce a tree"
        )

module RnglrPropertyTreeYield =
    let private propertyRunner =
        SharedParsingTests.Runners.runPropertyTreeYieldTest accepts

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Ab =
        [<Property>]
        let ``S -> a S b S | eps tree yield (Dyck1 grammar1)`` (s: string) =
            propertyRunner dyck1.Grammars[0].Grammar "S -> a S b S | eps" s

        [<Property>]
        let ``S -> S S | a S b | eps tree yield (Dyck1 grammar2)`` (s: string) =
            propertyRunner dyck1.Grammars[1].Grammar "S -> S S | a S b | eps" s

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
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
            propertyRunner TestGrammars.grammar13 "S -> N*" s

        [<Property>]
        let ``S -> a | S S | S S S tree yield (APlus grammar14)`` (s: string) =
            propertyRunner aplus.Grammars[5].Grammar "S -> a | S S | S S S" s

module SppfDotTests =

    let private buildSppf (grammarText: string) (input: string list) : SPPF<string, string> =
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
        let grammarText = "S -> a S b\nS -> S S\nS -> eps\n"
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

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
        let grammarText = "S -> a S b\nS -> S S\nS -> eps\n"
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

        let sppf = buildSppf grammarText input

        TestHelpers.assertSppfInvariant sppf

        Assert.NotEmpty(sppf.RootIndices)

module RnglrEpsilonGrammars =
    [<Fact>]
    let ``all epsilon grammars accept empty and reject non-empty`` () =
        SharedParsingTests.Runners.runEpsilonTests accepts checkReject

module RnglrGrammarTests =

    let private checkRsmAccepts (ebnfText: string) (input: string list) : unit =
        let rsm = RsmBuilder.buildRSMFromText ebnfText
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let freshStart = Nonterminal("S'")
        let graph = TestHelpers.terminalsToGraph input
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart ersm graph
        let vc = Graph.vertexCount graph
        Assert.True(PathIndex.isAccepted pathIndex ersm vc, $"Should accept {input}: {ebnfText}")

    let private checkRsmRejects (ebnfText: string) (input: string list) : unit =
        let rsm = RsmBuilder.buildRSMFromText ebnfText
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let freshStart = Nonterminal("S'")
        let graph = TestHelpers.terminalsToGraph input
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart ersm graph
        let vc = Graph.vertexCount graph
        Assert.False(PathIndex.isAccepted pathIndex ersm vc, $"Should reject {input}: {ebnfText}")

    module Grammar1 =
        let private g = "S -> N a*\nN -> (a a) | a"
        let private lang = LanguageRegistry.APlus

        [<Fact>]
        let ``S -> N a* accepts APlus strings`` () =
            for input in lang.AcceptStrings do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> N a* rejects APlus reject strings`` () =
            for input in lang.RejectStrings do
                checkRsmRejects g input

    module Grammar2 =
        let private g = "S -> a* N\nN -> a | (a a)"
        let private lang = LanguageRegistry.APlus

        [<Fact>]
        let ``S -> a* N accepts APlus strings`` () =
            for input in lang.AcceptStrings do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> a* N rejects APlus reject strings`` () =
            for input in lang.RejectStrings do
                checkRsmRejects g input

    module Grammar3 =
        let private g = "S -> N*\nN -> a | (a a)"
        let private lang = LanguageRegistry.AStar

        [<Fact>]
        let ``S -> N* accepts AStar strings`` () =
            for input in lang.AcceptStrings do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> N* rejects AStar reject strings`` () =
            for input in lang.RejectStrings do
                checkRsmRejects g input

    module Grammar4 =
        let private g = "S -> a\nS -> S S\nS -> S S S"
        let private lang = LanguageRegistry.APlus

        [<Fact>]
        let ``S -> a | S S | S S S accepts APlus strings`` () =
            for input in lang.AcceptStrings do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> a | S S | S S S rejects APlus reject strings`` () =
            for input in lang.RejectStrings do
                checkRsmRejects g input
