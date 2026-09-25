module BelyaninTraceTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.TestUtilities

/// The example from the detailed plan: regexp a / (b | c)* / a on the graph
/// 0 a 1, 1 b 2, 2 c 3, 3 b 2, 3 a 4, 2 a 5 with source v_0.
let private exampleNfa: NFA<string, int> =
    TestHelpers.nfaFromEdges
        6
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "b"; To = 2 }
          { From = 2; Label = "c"; To = 3 }
          { From = 3; Label = "b"; To = 2 }
          { From = 3; Label = "a"; To = 4 }
          { From = 2; Label = "a"; To = 5 } ]
        [| 0 |]

let private exampleRegexp: Regexp<string, string> =
    RSeq(RTerm(Terminal "a"), RSeq(RStar(RAlt(RTerm(Terminal "b"), RTerm(Terminal "c"))), RTerm(Terminal "a")))

/// The query DFA for the example regexp: q0 (start) -a-> q1, q1 -b,c-> q1, q1 -a-> q2 (final).
let private exampleDfa: DFA<string, int> = Regexp.toDfa exampleRegexp

let private labelOf (s: BelyaninRPQ.BelyaninLabelStep<string>) : string =
    match s.Label with
    | ATerm t -> t
    | AEpsilon -> failwith "epsilon label in trace"

let private labelStep
    (step: BelyaninRPQ.BelyaninTraceStep<string>)
    (t: string)
    : BelyaninRPQ.BelyaninLabelStep<string> =
    step.Labels |> List.find (fun s -> labelOf s = t)

[<Fact>]
let ``trace: six steps, initialization first`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa

    Assert.Equal(6, List.length steps)
    Assert.True(steps.[0].IsInit)
    Assert.False(steps.[1].IsInit)

    for i in 1..5 do
        Assert.Equal(i, steps.[i].Index)

[<Fact>]
let ``trace: initialization step holds the trivial path at the start state`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let init = steps.[0]

    Assert.Equal<Set<int list>>(Set.singleton [ 0 ], init.M.[exampleDfa.StartState, 0])
    Assert.True(PathSemiring.isEmpty init.P)
    Assert.Empty(init.Labels)
    Assert.Equal(init.M, init.NewM)

[<Fact>]
let ``trace: step 1 propagates label a to v_1`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let step = steps.[1]

    Assert.Equal<int>(1, List.length step.Labels)
    let a = List.head step.Labels
    Assert.Equal("a", labelOf a)
    Assert.Equal<Set<int list>>(Set.singleton [ 0 ], a.Select.[1, 0])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1 ], a.Extend.[1, 1])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1 ], step.NewM.[1, 1])

[<Fact>]
let ``trace: step 2 shows all three labels, only b extends`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let step = steps.[2]

    Assert.Equal<string list>([ "a"; "b"; "c" ], step.Labels |> List.map labelOf)
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1 ], (labelStep step "a").Select.[2, 1])
    Assert.True(PathSemiring.isEmpty (labelStep step "a").Extend)
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2 ], (labelStep step "b").Extend.[1, 2])
    Assert.True(PathSemiring.isEmpty (labelStep step "c").Extend)
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2 ], step.NewM.[1, 2])

[<Fact>]
let ``trace: label steps store the per-label N and G matrices`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa

    // DFA a(b|c)*a: q0 -a-> q1, q1 -b,c-> q1, q1 -a-> q2. Graph a-edges: 0->1, 2->5, 3->4.
    let nA = Matrix.init 3 3 false
    nA.[0, 1] <- true
    nA.[1, 2] <- true
    let gA = Matrix.init 6 6 false
    gA.[0, 1] <- true
    gA.[2, 5] <- true
    gA.[3, 4] <- true

    // Step 1: only label a propagates.
    let step1 = steps.[1]
    Assert.Equal(1, List.length step1.Labels)
    let a1 = List.head step1.Labels
    Assert.Equal(nA, a1.N)
    Assert.Equal(gA, a1.G)

    // Step 2: all three labels; N/G are the DFA/graph per-label matrices.
    let step2 = steps.[2]
    let nBC = Matrix.init 3 3 false
    nBC.[1, 1] <- true
    let gB = Matrix.init 6 6 false
    gB.[1, 2] <- true
    gB.[3, 2] <- true
    let gC = Matrix.init 6 6 false
    gC.[2, 3] <- true
    Assert.Equal(nA, (labelStep step2 "a").N)
    Assert.Equal(gA, (labelStep step2 "a").G)
    Assert.Equal(nBC, (labelStep step2 "b").N)
    Assert.Equal(gB, (labelStep step2 "b").G)
    Assert.Equal(nBC, (labelStep step2 "c").N)
    Assert.Equal(gC, (labelStep step2 "c").G)

[<Fact>]
let ``trace: step 3 forks to v_5 via a and v_3 via c`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let step = steps.[3]

    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 5 ], (labelStep step "a").Extend.[2, 5])
    Assert.True(PathSemiring.isEmpty (labelStep step "b").Extend)
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3 ], (labelStep step "c").Extend.[1, 3])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 5 ], step.NewM.[2, 5])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3 ], step.NewM.[1, 3])

[<Fact>]
let ``trace: step 4 label b is dropped by I_simple`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let step = steps.[4]

    // The automaton side matches (q1 -b-> q1 at v_3), but the only b-edge from v_3 goes
    // back to v_2, which is already in the path: [0; 1; 2; 3; 2] is not simple.
    let b = labelStep step "b"
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3 ], b.Select.[1, 3])
    Assert.True(PathSemiring.isEmpty b.Extend)

    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3; 4 ], (labelStep step "a").Extend.[2, 4])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3; 4 ], step.NewM.[2, 4])

[<Fact>]
let ``trace: final step has an unpropagatable frontier`` () =
    let steps, _ = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let last = steps.[5]

    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3; 4 ], last.M.[2, 4])
    Assert.Empty(last.Labels)
    Assert.True(PathSemiring.isEmpty last.NewM)

[<Fact>]
let ``trace: P accumulates only simple paths from sources`` () =
    let steps, p = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let all = PathSemiring.allPaths p

    Assert.True(all |> Set.forall PathSemiring.isSimple)
    // Every path starts at a source and ends at its column vertex.
    let rows = Matrix.rows p
    let cols = Matrix.cols p

    let wellFormed =
        [ for i in 0 .. rows - 1 do
              for j in 0 .. cols - 1 do
                  p.[i, j] |> Set.forall (fun path -> List.head path = 0 && List.last path = j) ]
        |> List.forall id

    Assert.True(wellFormed)
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 5 ], p.[2, 5])
    Assert.Equal<Set<int list>>(Set.singleton [ 0; 1; 2; 3; 4 ], p.[2, 4])

[<Fact>]
let ``reachableFromPaths: v_0 reaches v_4 and v_5`` () =
    let _, p = BelyaninRPQ.evaluateWithTrace exampleDfa exampleNfa
    let reachable = BelyaninRPQ.reachableFromPaths exampleDfa p [| 0 |]

    Assert.Equal<(int * int list) list>([ (0, [ 4; 5 ]) ], reachable)

[<Fact>]
let ``reachableFromPaths: multi-source keeps per-source paths separate`` () =
    // Two sources: v_0 reaches {v_4, v_5}; v_2 reaches nothing — its only a-edge goes to
    // the dead end v_5, and a(b|c)*a needs a second a-edge.
    let nfa =
        TestHelpers.nfaFromEdges
            6
            [ { From = 0; Label = "a"; To = 1 }
              { From = 1; Label = "b"; To = 2 }
              { From = 2; Label = "c"; To = 3 }
              { From = 3; Label = "b"; To = 2 }
              { From = 3; Label = "a"; To = 4 }
              { From = 2; Label = "a"; To = 5 } ]
            [| 0; 2 |]

    let _, p = BelyaninRPQ.evaluateWithTrace exampleDfa nfa
    let reachable = BelyaninRPQ.reachableFromPaths exampleDfa p [| 0; 2 |]

    Assert.Equal<(int * int list) list>([ (0, [ 4; 5 ]); (2, []) ], reachable)

[<Properties(Arbitrary = [| typeof<AcyclicRpqGenerators> |])>]
module BelyaninTracePropertyTests =

    /// On acyclic graphs every walk is simple, so per-source reachability derived from the
    /// path trace's P equals the Boolean evaluate for every source row.
    [<Property>]
    let ``path trace projects to Boolean evaluate on acyclic graphs`` (d: RegexAndGraph) =
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
                let _, p = BelyaninRPQ.evaluateWithTrace dfa nfa
                let reachable = BelyaninRPQ.reachableFromPaths dfa p safeSources
                let boolResult = BelyaninRPQ.evaluate dfa nfa

                [ for i in 0 .. safeSources.Length - 1 do
                      let s = safeSources.[i]
                      let fromTrace = reachable |> List.tryFind (fun (s', _) -> s' = s) |> Option.map snd

                      match fromTrace with
                      | None -> false
                      | Some vs ->
                          [ for j in 0 .. v - 1 do
                                if boolResult.[i, j] <> (List.contains j vs) then
                                    false ]
                          |> List.forall id ]
                |> List.forall id
