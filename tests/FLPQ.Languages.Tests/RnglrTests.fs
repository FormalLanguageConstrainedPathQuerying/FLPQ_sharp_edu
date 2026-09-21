module RnglrTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

[<Struct>]
type private TerminalPosition =
    { Terminal: string
      Left: int
      Right: int }

let private accepts = TestHelpers.accepts Rnglr.buildPathIndex PathIndex.isAccepted

let private checkReject =
    TestHelpers.checkReject Rnglr.buildPathIndex PathIndex.isAccepted

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private astar = LanguageRegistry.AStar
let private anbn = LanguageRegistry.ANBN
let private astarBStar = LanguageRegistry.AStarBStar
let private epsilonOnly = LanguageRegistry.EpsilonOnly

/// The outcome of running RNGLR with step collection: the result (steps + path index),
/// the extended RSM (for building the LR table), and the input graph.
[<Struct>]
type private RnglrRun =
    { Result: RnglrResult<string, string>
      Ers: ExtendedRSM<string, string>
      Graph: Graph<int, Option<string>> }

/// Runs RNGLR with step collection on a registry RSM and a terminal list.
let private runWithSteps (rsm: RSM<string, string>) (input: Terminal<string> list) : RnglrRun =
    let startNt = (RSM.startBlock rsm).Nonterminal
    let rsmFixed = { rsm with StartBlock = startNt }
    let freshStart = Nonterminal "S'"
    let graph = TestHelpers.terminalsToGraph input
    let ersm = ExtendedRSM.create freshStart rsmFixed

    { Result = Rnglr.buildPathIndexWithSteps freshStart ersm graph
      Ers = ersm
      Graph = graph }

module RnglrSharedAcceptance =
    let private rnglrCases = ParsingTestCases.AcceptanceCases.allCases

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
    let private lang = astarBStar

    [<Fact>]
    let ``S -> A B accepts all accept strings`` () =
        for s in lang.AcceptStrings do
            Assert.True(accepts astarBStar.Grammars[0].Rsm s, "Should accept and produce tree")

    [<Fact>]
    let ``S -> A B rejects all reject strings`` () =
        for s in lang.RejectStrings do
            Assert.True(checkReject astarBStar.Grammars[0].Rsm s)

module RnglrReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        let g = epsilonOnly.Grammars |> List.find (fun g -> g.Name = "viaCascade")
        Assert.True(accepts g.Rsm [], "Should accept and produce tree")

module RnglrPropertyTreeYield =
    let private propertyRunner =
        ParsingTestCases.Runners.runPropertyTreeYieldTest accepts

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AbString> |])>]
    module Ab =
        [<Property>]
        let ``S -> a S b S | eps tree yield (Dyck1 grammar1)`` (s: string) =
            propertyRunner dyck1.Grammars[0].Rsm "S -> a S b S | eps" s

        [<Property>]
        let ``S -> S S | a S b | eps tree yield (Dyck1 grammar2)`` (s: string) =
            propertyRunner dyck1.Grammars[1].Rsm "S -> S S | a S b | eps" s

    [<Properties(Arbitrary = [| typeof<GenToArbitrary.AString> |])>]
    module A =
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

module SppfDotTests =

    let private buildSppf (rsm: RSM<string, string>) (input: Terminal<string> list) : SPPF<string, string> =
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
        let rsm = dyck1.Grammars[1].Rsm
        let input = dyck1.AcceptStrings[4]

        let sppf = buildSppf rsm input

        TestHelpers.assertSppfInvariant sppf

        let terminalPositions =
            Graph.vertices sppf.Graph
            |> List.choose (fun (_, v) ->
                match v with
                | SppfNodeInfo.SppfTerminal(Terminal t, l, r) -> Some { Terminal = t; Left = l; Right = r }
                | _ -> None)
            |> Set.ofList

        let expected: Set<TerminalPosition> =
            set
                [ { Terminal = "a"; Left = 0; Right = 1 }
                  { Terminal = "a"; Left = 1; Right = 2 }
                  { Terminal = "b"; Left = 2; Right = 3 }
                  { Terminal = "a"; Left = 3; Right = 4 }
                  { Terminal = "b"; Left = 4; Right = 5 }
                  { Terminal = "b"; Left = 5; Right = 6 } ]

        Assert.Equal<Set<TerminalPosition>>(expected, terminalPositions)

    [<Fact>]
    let ``RNGLR SPPF has root nodes for S->aSb|SS|eps with aababb`` () =
        let rsm = dyck1.Grammars[1].Rsm
        let input = dyck1.AcceptStrings[4]

        let sppf = buildSppf rsm input

        TestHelpers.assertSppfInvariant sppf

        Assert.NotEmpty(sppf.RootIndices)

module RnglrPassingReductions =
    let private stepsOf
        (rsm: RSM<string, string>)
        (input: Terminal<string> list)
        : RnglrParsingStep<string, string> list =
        runWithSteps rsm input |> fun run -> run.Result.Steps

    [<Fact>]
    let ``passing-reduction vertices tracked for S -> a S b | eps | S S`` () =
        // The concatenation rule S -> S S creates multiple reduction paths at the same
        // input position; a product BFS deposits stored states at an intermediate GSS
        // vertex that a later reduction in the same level consumes.
        let steps = stepsOf dyck1.Grammars[1].Rsm [ Terminal "a"; Terminal "b" ]

        Assert.True(
            steps |> List.exists (fun s -> not (Set.isEmpty s.PassingReductionVertices)),
            "Expected at least one step with non-empty PassingReductionVertices"
        )

    [<Fact>]
    let ``passing-reduction vertices empty at initial step`` () =
        let steps = stepsOf dyck1.Grammars[1].Rsm [ Terminal "a"; Terminal "b" ]
        Assert.True(Set.isEmpty steps.[0].PassingReductionVertices)

    [<Fact>]
    let ``shift consumes stored states on self-loop input graph`` () =
        // S -> A B | A S; A -> a E; B -> b E; E -> eps. The LR state reached after A
        // (S mid-rule plus B, S and A items) shifts 'a' into the in-progress-A state, so a
        // self-loop on 'a' at vertex 1 re-targets the GSS vertex where the A product BFS
        // deposited a stored state; the level's shift pass consumes it (Rnglr.fs:327-343).
        // A path graph can never trigger this: stored states are deposited at earlier
        // vertices whose shifts already ran, and the driver skips shifts at the last vertex.
        let rsm = RsmBuilder.buildRSMFromText "S -> A B | A S\nA -> a E\nB -> b E\nE -> eps"

        let graph =
            let m = Matrix.init 3 3 None
            m.[0, 1] <- Some "a"
            m.[1, 2] <- Some "a"
            m.[1, 1] <- Some "a"
            Graph.fromEdges [ 0; 1; 2 ] m

        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart rsm

        let steps = Rnglr.buildPathIndexWithSteps freshStart ersm graph |> fun r -> r.Steps

        Assert.True(
            steps |> List.exists (fun s -> not (Set.isEmpty s.PassingReductionVertices)),
            "Expected a step with non-empty PassingReductionVertices"
        )

        // RNGLR and GLL agree on the same input graph; both reject because the input has
        // no 'b' and S -> A S has no base case.
        let vc = Graph.vertexCount graph

        let rnglrAccepted =
            PathIndex.isAccepted (Rnglr.buildPathIndex freshStart ersm graph) ersm vc

        let gllAccepted =
            PathIndex.isAccepted (GLL.buildPathIndex freshStart ersm graph) ersm vc

        Assert.Equal(gllAccepted, rnglrAccepted)
        Assert.False(rnglrAccepted)

module RnglrLrTableEdgeCases =

    [<Fact>]
    let ``buildLR0Table skips dangling nonterminal without a block`` () =
        // Hand-built RSM: the S block has an N(GHOST) transition but GHOST has no block.
        // Closure hits the `blockStartStates.TryGetValue -> false` branch (RnglrLR.fs:65).
        let ntS = Nonterminal "S"
        let ntGhost = Nonterminal "GHOST"

        let rsm =
            RsmFixtures.mkRsm
                [| { BlockNonterminal = ntS
                     LocalState = 0
                     IsFinal = false }
                   { BlockNonterminal = ntS
                     LocalState = 1
                     IsFinal = true } |]
                [ { From = 0
                    Label = AutomatonLabel.ATerm(RsmSymbol.RNonterm ntGhost)
                    To = 1 } ]
                [ (ntS, 0) ]
                [ 1 ]
                ntS

        let table = RnglrLR.buildLR0Table rsm

        Assert.Equal(2, Dfa.stateCount table.Automaton)
        Assert.Equal(1, Map.find (0, ntGhost) table.Goto)
        Assert.Equal(LRAction.Accept, Map.find (1, Symbol.Epsilon) table.Action)

module RnglrEpsilonGrammars =
    [<Fact>]
    let ``all epsilon grammars accept empty and reject non-empty`` () =
        ParsingTestCases.Runners.runEpsilonTests accepts checkReject

module RnglrSubsteps =
    let private doubleA = LanguageRegistry.DoubleA

    let private aplusAmbiguous =
        LanguageRegistry.findGrammar LanguageRegistry.APlus "ambiguousWithSingleRule"

    /// One substep test case: a name for failure messages, a registry RSM, and an input.
    [<Struct>]
    type private SubstepCase =
        { Name: string
          Rsm: RSM<string, string>
          Input: Terminal<string> list }

    /// Cases covering a simple input, an ambiguous one (cascade/passing reductions), and a
    /// nested one.
    let private cases: SubstepCase list =
        [ { Name = "DoubleA/aa"
            Rsm = doubleA.Grammars[0].Rsm
            Input = [ Terminal "a"; Terminal "a" ] }
          { Name = "APlus/aaa"
            Rsm = aplusAmbiguous.Rsm
            Input = [ Terminal "a"; Terminal "a"; Terminal "a" ] }
          { Name = "Dyck1/ab"
            Rsm = dyck1.Grammars[1].Rsm
            Input = [ Terminal "a"; Terminal "b" ] } ]

    let private lrTableOf (ersm: ExtendedRSM<string, string>) : RnglrTable<string, string> =
        RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)

    [<Fact>]
    let ``initial substep has no action, vertex -1, and empty GSS`` () =
        for case in cases do
            let initial = runWithSteps case.Rsm case.Input |> fun run -> run.Result.Steps.[0]
            let noAction = initial.Action = None
            Assert.True(noAction, $"{case.Name}: the initial substep must have no action")
            Assert.Equal(-1, initial.InputVertex)
            Assert.True(Set.isEmpty initial.ActiveGssVertices, case.Name)
            Assert.True(Set.isEmpty initial.ActiveGssEdges, case.Name)
            Assert.True(Set.isEmpty initial.NewGssVertices, case.Name)
            Assert.True(Set.isEmpty initial.NewGssEdges, case.Name)
            Assert.True(Set.isEmpty initial.ChangedCells, case.Name)

    [<Fact>]
    let ``every non-initial substep carries an action at an input vertex in range`` () =
        for case in cases do
            let result = runWithSteps case.Rsm case.Input |> fun run -> run.Result
            let maxVertex = result.PathIndex.VertexCount - 1

            for step in List.skip 1 result.Steps do
                Assert.True(step.Action.IsSome, $"{case.Name}: substep without an action")
                let inRange = step.InputVertex >= 0 && step.InputVertex <= maxVertex
                Assert.True(inRange, $"{case.Name}: input vertex {step.InputVertex} out of range [0..{maxVertex}]")

    [<Fact>]
    let ``within each level all reduce substeps precede shift substeps`` () =
        for case in cases do
            let result = runWithSteps case.Rsm case.Input |> fun run -> run.Result
            let byLevel = List.skip 1 result.Steps |> List.groupBy (fun s -> s.InputVertex)

            for level, steps in byLevel do
                let actions = steps |> List.map (fun s -> s.Action.Value)

                let firstShiftIdx =
                    actions
                    |> List.tryFindIndex (function
                        | RnglrAction.Shift _ -> true
                        | _ -> false)

                match firstShiftIdx with
                | Some i ->
                    let beforeFirstShift = actions |> List.take i

                    let allReducesBeforeShifts =
                        List.forall
                            (function
                            | RnglrAction.Reduce _ -> true
                            | _ -> false)
                            beforeFirstShift

                    Assert.True(
                        allReducesBeforeShifts,
                        sprintf
                            "%s: level %d has a non-reduce substep before the first shift: %A"
                            case.Name
                            level
                            actions
                    )
                | None -> ()

    [<Fact>]
    let ``shift substep adds exactly one new GSS edge labeled with the shifted terminal`` () =
        for case in cases do
            let result = runWithSteps case.Rsm case.Input |> fun run -> run.Result

            for step in result.Steps do
                match step.Action with
                | Some(RnglrAction.Shift(Terminal t, _)) ->
                    let edgeCount = Set.count step.NewGssEdges
                    let exactlyOneEdge = edgeCount = 1

                    Assert.True(
                        exactlyOneEdge,
                        $"{case.Name}: shift substep must add exactly one new GSS edge, got {edgeCount}"
                    )

                    let (fromIdx, toIdx) = step.NewGssEdges |> Set.minElement
                    let symbols = step.ActiveGssEdgeSymbols.[fromIdx, toIdx]
                    Assert.True(NonEmptySet.contains (Symbol.T(Terminal t)) symbols, case.Name)
                | _ -> ()

    [<Fact>]
    let ``reduce substep adds at most one new GSS edge labeled with the reduced nonterminal`` () =
        for case in cases do
            let run = runWithSteps case.Rsm case.Input
            let table = lrTableOf run.Ers

            for step in run.Result.Steps do
                match step.Action with
                | Some(RnglrAction.Reduce(nt, _, gotoLrState)) ->
                    Assert.True(Set.count step.NewGssEdges <= 1, case.Name)

                    for (fromIdx, toIdx) in Set.toSeq step.NewGssEdges do
                        let symbols = step.ActiveGssEdgeSymbols.[fromIdx, toIdx]
                        Assert.True(NonEmptySet.contains (Symbol.N nt) symbols, case.Name)
                        Assert.True(Map.containsKey (gotoLrState, nt) table.Goto, case.Name)
                | _ -> ()

    [<Fact>]
    let ``exact substep action sequence for S -> a a on input a a`` () =
        // LR states of the golden table: 0 = start, 1 = after the first a, 2 = after S (acc),
        // 3 = reduce S. The reduce's trigger state is 3 (holds the complete item); its goto
        // cell is (0, S).
        let result =
            runWithSteps doubleA.Grammars[0].Rsm [ Terminal "a"; Terminal "a" ]
            |> fun run -> run.Result

        let actions = result.Steps |> List.map (fun s -> s.Action)

        let expected =
            [ None
              Some(RnglrAction.Shift(Terminal "a", 0))
              Some(RnglrAction.Shift(Terminal "a", 1))
              Some(RnglrAction.Reduce(Nonterminal "S", 3, 0)) ]

        let matches = actions = expected
        Assert.True(matches, sprintf "expected action sequence %A, got %A" expected actions)

    let private samePathIndex (a: PathIndex<string, string>) (b: PathIndex<string, string>) : bool =
        a.StateCount = b.StateCount
        && a.VertexCount = b.VertexCount
        && Matrix.map2i (fun _ _ ca cb -> ca = cb) a.Matrix b.Matrix
           |> Matrix.fold ((&&)) true

    [<Fact>]
    let ``buildPathIndexWithSteps produces the same path index as buildPathIndex`` () =
        for case in cases do
            let run = runWithSteps case.Rsm case.Input
            let plain = Rnglr.buildPathIndex (Nonterminal "S'") run.Ers run.Graph
            Assert.True(samePathIndex plain run.Result.PathIndex, $"{case.Name}: path index differs")
