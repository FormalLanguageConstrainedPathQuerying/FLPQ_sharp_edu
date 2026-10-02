module ArroyueloBooleanTraceTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.TestUtilities

/// The regexp (a / a)* / b = (aa)* b.
let private aaStarB: Regexp<string, string> =
    Regexp.RSeq(
        Regexp.RStar(Regexp.RSeq(Regexp.RTerm(Terminal "a"), Regexp.RTerm(Terminal "a"))),
        Regexp.RTerm(Terminal "b")
    )

/// 0 -a-> 1 -a-> 2 -a-> 0, 2 -a-> 3 -b-> 4. The only accepting walks to v_4 revisit v_0,
/// so they are non-simple.
let private bigCyclicGraph: NFA<string, int> =
    TestHelpers.nfaFromEdges
        5
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "a"; To = 2 }
          { From = 2; Label = "a"; To = 0 }
          { From = 2; Label = "a"; To = 3 }
          { From = 3; Label = "b"; To = 4 } ]
        [| 0 |]

/// Restrict a full |V| x |V| matrix to the graph's start states.
let private projectSources (sources: int array) (full: Matrix<bool>) : Matrix<bool> =
    let vCount = Matrix.cols full
    Matrix.create sources.Length vCount (fun i j -> full.[sources.[i], j])

[<Fact>]
let ``boolean trace emits one step per AST node, root last`` () =
    let graph = TestHelpers.rpqExampleGraph
    let regexp = TestHelpers.rpqExampleRegexp
    let steps, _ = ArroyueloRPQ.evaluateBooleanWithTrace graph regexp

    // a / (b | c)* / a has eight post-order AST nodes.
    Assert.Equal(8, steps.Length)

    for i in 0 .. steps.Length - 1 do
        Assert.Equal(i, steps.[i].NodeIndex)

    // The last step is the root (a sequence); leaves have Base operation and no operands.
    Assert.Equal(ArroyueloRPQ.Seq, steps.[steps.Length - 1].Operation)

    Assert.All(
        steps |> List.filter (fun s -> s.Children.IsEmpty),
        (fun s -> Assert.Equal(ArroyueloRPQ.Base, s.Operation))
    )

[<Fact>]
let ``boolean reachability finds non-simple walks that simple paths miss`` () =
    let _, full = ArroyueloRPQ.evaluateBooleanWithTrace bigCyclicGraph aaStarB
    Assert.True(full.[0, 4], "the Boolean reachability matrix must reach v_4")

    let _, pathMatrix = ArroyueloRPQ.evaluateWithTrace bigCyclicGraph aaStarB
    let pathBool = PathSemiring.booleanProjection pathMatrix
    Assert.False(pathBool.[0, 4], "the simple-path projection must not reach v_4")

[<Fact>]
let ``boolean trace projection equals evaluate on the sources`` () =
    let graph = TestHelpers.rpqExampleGraph
    let regexp = TestHelpers.rpqExampleRegexp
    let _, full = ArroyueloRPQ.evaluateBooleanWithTrace graph regexp
    let sources = graph.StartStates |> Set.toArray |> Array.sort
    let expected = ArroyueloRPQ.evaluate graph regexp
    let actual = projectSources sources full

    for i in 0 .. Matrix.rows expected - 1 do
        for j in 0 .. Matrix.cols expected - 1 do
            Assert.Equal(expected.[i, j], actual.[i, j])

// --- Cross-algorithm property tests ---

[<Properties(Arbitrary = [| typeof<RegexAndGraphGenerators> |])>]
module ArroyueloBooleanTracePropertyTests =

    [<Property>]
    let ``boolean trace projection equals Kronecker reachability`` (d: RegexAndGraph) =
        if d.Sources.Length = 0 || d.VertexCount = 0 then
            true
        else
            let v = d.VertexCount

            let safeSources =
                d.Sources
                |> Array.map (fun s -> min s (v - 1))
                |> Set.ofArray
                |> Set.toArray
                |> Array.sort

            let nfa = TestHelpers.nfaFromEdges v d.Edges safeSources
            let dfa = Regexp.toDfa d.Regex
            let _, full = ArroyueloRPQ.evaluateBooleanWithTrace nfa d.Regex
            let actual = projectSources safeSources full
            let expected = KroneckerRPQ.evaluate dfa nfa

            let mutable ok = true

            for i in 0 .. Matrix.rows expected - 1 do
                for j in 0 .. Matrix.cols expected - 1 do
                    if expected.[i, j] <> actual.[i, j] then
                        ok <- false

            ok
