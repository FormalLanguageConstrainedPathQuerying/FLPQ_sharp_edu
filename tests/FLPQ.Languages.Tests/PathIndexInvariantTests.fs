module PathIndexInvariantTests

open System
open Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.TestUtilities

let private ntA = Nonterminal "A"
let private ntB = Nonterminal "B"


module AddGetTests =

    [<Fact>]
    let ``add and get round-trip all four entry kinds`` () =
        let pi = PathIndexFixtures.mk 2 3
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PTerminal(Terminal "a"))
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PNonterminal ntA)
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PEpsilonNonterminal ntB)
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PIntermediate(1, 0))

        let entries = PathIndex.get pi 0 0 1 2
        Assert.Equal(4, Set.count entries)
        Assert.True(Set.contains (PathIndexEntry.PTerminal(Terminal "a")) entries)
        Assert.True(Set.contains (PathIndexEntry.PNonterminal ntA) entries)
        Assert.True(Set.contains (PathIndexEntry.PEpsilonNonterminal ntB) entries)
        Assert.True(Set.contains (PathIndexEntry.PIntermediate(1, 0)) entries)

    [<Fact>]
    let ``addWithTracking tracks only newly changed cells`` () =
        let pi = PathIndexFixtures.mk 2 3
        let changed = ref Set.empty
        let entry = PathIndexEntry.PNonterminal ntA

        PathIndex.addWithTracking pi 0 0 1 2 entry changed
        PathIndex.addWithTracking pi 0 0 1 2 entry changed
        PathIndex.addWithTracking pi 0 1 1 2 (PathIndexEntry.PTerminal(Terminal "a")) changed

        Assert.Equal(2, Set.count changed.Value)


module CheckCalleeReachabilityTests =

    [<Fact>]
    let ``checkCalleeReachabilityInvariant flags empty callee cells`` () =
        // PNonterminal(A) in cell (1,0)->(2,2); A's block is states 0..1, so the
        // callee cell (0,0)->(1,2) must be non-empty.
        let pi = PathIndexFixtures.mk 3 3
        PathIndex.add pi 1 0 2 2 (PathIndexEntry.PNonterminal ntA)

        let blockStart = Map.ofList [ (ntA, 0) ]
        let blockFinals = Map.ofList [ (ntA, Set.ofList [ 1 ]) ]

        match PathIndex.checkCalleeReachabilityInvariant pi blockStart blockFinals with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("all callee cells", err)
        | Ok() -> Assert.Fail("expected callee reachability error")

    [<Fact>]
    let ``checkCalleeReachabilityInvariant flags nonterminal missing from blockFinals`` () =
        let pi = PathIndexFixtures.mk 2 3
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PNonterminal ntA)

        let blockStart = Map.ofList [ (ntA, 0) ]
        let blockFinals = Map.empty

        match PathIndex.checkCalleeReachabilityInvariant pi blockStart blockFinals with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("block metadata not found", err)
        | Ok() -> Assert.Fail("expected block metadata error")

    [<Fact>]
    let ``checkCalleeReachabilityInvariant flags nonterminal missing from blockStart`` () =
        let pi = PathIndexFixtures.mk 2 3
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PNonterminal ntA)

        let blockStart = Map.empty
        let blockFinals = Map.ofList [ (ntA, Set.ofList [ 1 ]) ]

        match PathIndex.checkCalleeReachabilityInvariant pi blockStart blockFinals with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("block metadata not found", err)
        | Ok() -> Assert.Fail("expected block metadata error")


module CheckNoEpsilonTests =

    [<Fact>]
    let ``checkNoEpsilonInvariant flags epsilon entry without start-final overlap`` () =
        let pi = PathIndexFixtures.mk 2 3
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PEpsilonNonterminal ntA)

        let blockStart = Map.ofList [ (ntA, 0) ]
        let finalStates = Set.ofList [ 1 ]

        match PathIndex.checkNoEpsilonInvariant id pi blockStart finalStates with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("no start=final overlap exists", err)
        | Ok() -> Assert.Fail("expected no-epsilon error")

    [<Fact>]
    let ``checkNoEpsilonInvariant passes with start-final overlap`` () =
        let pi = PathIndexFixtures.mk 2 3
        PathIndex.add pi 0 0 1 2 (PathIndexEntry.PEpsilonNonterminal ntA)

        let blockStart = Map.ofList [ (ntA, 0) ]
        let finalStates = Set.ofList [ 0 ]

        match PathIndex.checkNoEpsilonInvariant id pi blockStart finalStates with
        | Ok() -> ()
        | Error errors -> Assert.Fail(String.concat "\n" errors)


module AcceptanceTests =

    let private ersm =
        ExtendedRSM.create (Nonterminal "S'") LanguageRegistry.Dyck1.Grammars[0].Rsm

    let private vertexCount = 3

    let private startGlobalState =
        let flat = ersm.ExtendedRsm

        match flat.BlockStart.TryGetValue(flat.StartBlock) with
        | true, gs -> gs
        | false, _ -> 0

    [<Fact>]
    let ``isAccepted false for empty acceptance cell`` () =
        let pi = PathIndexFixtures.mk (ExtendedRSM.stateCount ersm) vertexCount
        Assert.False(PathIndex.isAccepted pi ersm vertexCount)

    [<Fact>]
    let ``isAccepted true when acceptance cell holds the fresh start nonterminal`` () =
        let pi = PathIndexFixtures.mk (ExtendedRSM.stateCount ersm) vertexCount

        PathIndex.add
            pi
            startGlobalState
            0
            (startGlobalState + 1)
            (vertexCount - 1)
            (PathIndexEntry.PNonterminal(ExtendedRSM.freshStart ersm))

        Assert.True(PathIndex.isAccepted pi ersm vertexCount)

    [<Fact>]
    let ``checkAcceptanceInvariant passes when not accepted`` () =
        let pi = PathIndexFixtures.mk (ExtendedRSM.stateCount ersm) vertexCount

        match PathIndex.checkAcceptanceInvariant id pi ersm vertexCount with
        | Ok() -> ()
        | Error errors -> Assert.Fail(String.concat "\n" errors)

    [<Fact>]
    let ``checkAcceptanceInvariant flags acceptance cell without start nonterminal`` () =
        let pi = PathIndexFixtures.mk (ExtendedRSM.stateCount ersm) vertexCount

        PathIndex.add
            pi
            startGlobalState
            0
            (startGlobalState + 1)
            (vertexCount - 1)
            (PathIndexEntry.PTerminal(Terminal "a"))

        match PathIndex.checkAcceptanceInvariant id pi ersm vertexCount with
        | Error errors ->
            let err = Assert.Single(errors)
            Assert.Contains("does not contain PNonterminal", err)
        | Ok() -> Assert.Fail("expected acceptance invariant error")


module RsmTransitionArrayTests =

    [<Fact>]
    let ``ExtendedRSM transition arrays match the flat transition matrix`` () =
        let ersm =
            ExtendedRSM.create (Nonterminal "S'") LanguageRegistry.Dyck1.Grammars[0].Rsm

        let flat = ersm.ExtendedRsm

        let countLabels (pred: AutomatonLabel<RsmSymbol<string, string>> -> bool) : int =
            let mutable count = 0

            for i in 0 .. flat.StateCount - 1 do
                for j in 0 .. flat.StateCount - 1 do
                    match flat.Transitions.[i, j] with
                    | Some labels ->
                        for label in NonEmptySet.toSeq labels do
                            if pred label then
                                count <- count + 1
                    | None -> ()

            count

        let expectedTerms =
            countLabels (function
                | AutomatonLabel.ATerm(RsmSymbol.RTerm _) -> true
                | _ -> false)

        let expectedNonterms =
            countLabels (function
                | AutomatonLabel.ATerm(RsmSymbol.RNonterm _) -> true
                | _ -> false)

        let actualTerms =
            ExtendedRSM.termTransitions ersm |> Array.sumBy (fun arr -> arr.Count)

        let actualNonterms =
            ExtendedRSM.nontermTransitions ersm |> Array.sumBy (fun arr -> arr.Count)

        Assert.True(expectedTerms > 0, "expected at least one term transition")
        Assert.True(expectedNonterms > 0, "expected at least one nonterm transition")
        Assert.Equal(expectedTerms, actualTerms)
        Assert.Equal(expectedNonterms, actualNonterms)


module GllTypesEqualityTests =

    let private mkDescriptor (state: int) (vertex: int) : Descriptor =
        { RsmState = state
          Vertex = vertex
          GssIdx = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    [<Fact>]
    let ``Descriptor Equals returns false for wrong-type object`` () =
        let d = mkDescriptor 0 0
        Assert.False(d.Equals(box "not a descriptor"))

    [<Fact>]
    let ``Descriptor CompareTo throws invalidArg for wrong-type object`` () =
        let d = mkDescriptor 0 0
        let comparable: System.IComparable = d
        Assert.Throws<ArgumentException>(fun () -> comparable.CompareTo(box "not a descriptor") |> ignore)

    [<Fact>]
    let ``Descriptor Equals and CompareTo work for same-type objects`` () =
        let d1 = mkDescriptor 0 0
        let d2 = mkDescriptor 0 0
        let d3 = mkDescriptor 1 0
        let c1: System.IComparable = d1

        Assert.True(d1.Equals(box d2))
        Assert.Equal(0, c1.CompareTo(box d2))
        Assert.True(c1.CompareTo(box d3) < 0)
