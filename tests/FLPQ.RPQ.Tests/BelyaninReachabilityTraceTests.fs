module BelyaninReachabilityTraceTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.TestUtilities

/// The regexp (a / a)* / b = (aa)* b (sequence rendered as `/`).
let private aaStarB: Regexp<string, string> =
    Regexp.RSeq(
        Regexp.RStar(Regexp.RSeq(Regexp.RTerm(Terminal "a"), Regexp.RTerm(Terminal "a"))),
        Regexp.RTerm(Terminal "b")
    )

/// The user's cyclic regression graph: 0 -a-> 1 -a-> 2 -a-> 0, 2 -a-> 3 -b-> 4. The only
/// accepting walks to v_4 revisit v_0, so they are non-simple.
let private bigCyclicGraph: NFA<string, int> =
    TestHelpers.nfaFromEdges
        5
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "a"; To = 2 }
          { From = 2; Label = "a"; To = 0 }
          { From = 2; Label = "a"; To = 3 }
          { From = 3; Label = "b"; To = 4 } ]
        [| 0 |]

/// 0 -a-> 1 -a-> 2 -a-> 0, 1 -b-> 3. v_3 is reached by the non-simple walk aaaab.
let private smallCyclicGraph: NFA<string, int> =
    TestHelpers.nfaFromEdges
        4
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "a"; To = 2 }
          { From = 1; Label = "b"; To = 3 }
          { From = 2; Label = "a"; To = 0 } ]
        [| 0 |]

/// Vertices reachable from a source as witnessed by the trace's final P at a final state.
let private traceReachable (dfa: DFA<string, int>) (p: Matrix<bool>) (vCount: int) : bool list =
    [ for v in 0 .. vCount - 1 -> dfa.FinalStates |> Set.exists (fun q -> p.[q, v]) ]

/// Whether any cell of a Boolean matrix is true.
let private anyTrue (m: Matrix<bool>) : bool = Matrix.fold (||) false m

[<Fact>]
let ``reachability trace: initialization step holds the start cell`` () =
    let dfa = Regexp.toDfa aaStarB
    let steps, _ = BelyaninRPQ.evaluateReachabilityWithTrace dfa bigCyclicGraph
    let init = steps.[0]

    Assert.True(init.IsInit)
    Assert.True(init.M.[dfa.StartState, 0])
    Assert.False(anyTrue init.P)
    Assert.Empty(init.Labels)
    Assert.Equal(init.M, init.NewM)
    Assert.True(List.length steps > 1, "expected at least one non-init step")

[<Fact>]
let ``reachability trace: M is a subset of P on every non-init step`` () =
    let dfa = Regexp.toDfa aaStarB
    let steps, p = BelyaninRPQ.evaluateReachabilityWithTrace dfa bigCyclicGraph

    let mutable subsetHolds = true

    for i in 1 .. steps.Length - 1 do
        for q in 0 .. Matrix.rows steps.[i].M - 1 do
            for v in 0 .. Matrix.cols steps.[i].M - 1 do
                if steps.[i].M.[q, v] && not steps.[i].P.[q, v] then
                    subsetHolds <- false

    Assert.True(subsetHolds, "M is not a subset of P on some step")

    // The final step's NewM is empty (the loop's termination condition).
    let last = List.last steps
    Assert.False(anyTrue last.NewM)
    Assert.Equal(p, last.P)

[<Fact>]
let ``reachability trace: (aa)*b on the big cyclic graph reaches v_4 only`` () =
    let dfa = Regexp.toDfa aaStarB
    let _, p = BelyaninRPQ.evaluateReachabilityWithTrace dfa bigCyclicGraph

    Assert.Equal<bool list>([ false; false; false; false; true ], traceReachable dfa p 5)

[<Fact>]
let ``reachability trace: (aa)*b on the small cyclic graph reaches v_3`` () =
    let dfa = Regexp.toDfa aaStarB
    let _, p = BelyaninRPQ.evaluateReachabilityWithTrace dfa smallCyclicGraph

    Assert.Equal<bool list>([ false; false; false; true ], traceReachable dfa p 4)

[<Fact>]
let ``reachability trace: empty start-state set yields no steps`` () =
    let dfa = Regexp.toDfa aaStarB
    let nfa = TestHelpers.nfaFromEdges 2 [ { From = 0; Label = "a"; To = 1 } ] [||]
    let steps, p = BelyaninRPQ.evaluateReachabilityWithTrace dfa nfa

    Assert.Empty steps
    Assert.Equal(2, Matrix.cols p)
    Assert.False(anyTrue p)

[<Fact>]
let ``reachability trace: projection equals evaluate on the shared acyclic example`` () =
    let dfa = Regexp.toDfa TestHelpers.rpqExampleRegexp
    let vCount = Nfa.stateCount TestHelpers.rpqExampleGraph
    let _, p = BelyaninRPQ.evaluateReachabilityWithTrace dfa TestHelpers.rpqExampleGraph
    let boolResult = BelyaninRPQ.evaluate dfa TestHelpers.rpqExampleGraph

    let fromTrace = traceReachable dfa p vCount
    let fromEvaluate = [ for v in 0 .. vCount - 1 -> boolResult.[0, v] ]
    Assert.Equal<bool list>(fromEvaluate, fromTrace)

[<Properties(Arbitrary = [| typeof<RegexAndGraphGenerators> |])>]
module BelyaninReachabilityTracePropertyTests =

    /// The trace runs the same Boolean traversal as `evaluate`, so for the traced source
    /// (the first start vertex, sorted) the final-state projection of P must match
    /// `evaluate`'s first row — on cyclic and acyclic graphs alike.
    [<Property>]
    let ``reachability trace projects to evaluate for the first source`` (d: RegexAndGraph) =
        if d.Sources.Length = 0 then
            true
        else
            let v = d.VertexCount

            if v = 0 then
                true
            else
                let safeSources =
                    d.Sources |> Array.map (fun s -> min s (v - 1)) |> Set.ofArray |> Set.toArray

                let nfa = TestHelpers.nfaFromEdges v d.Edges safeSources
                let dfa = Regexp.toDfa d.Regex
                let _, p = BelyaninRPQ.evaluateReachabilityWithTrace dfa nfa
                let boolResult = BelyaninRPQ.evaluate dfa nfa

                [ for j in 0 .. v - 1 do
                      if boolResult.[0, j] <> (dfa.FinalStates |> Set.exists (fun q -> p.[q, j])) then
                          false ]
                |> List.forall id
