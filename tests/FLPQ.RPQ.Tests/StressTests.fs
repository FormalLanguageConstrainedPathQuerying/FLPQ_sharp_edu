module StressTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private nfaWithSources (vCount: int) (edges: (int * string * int) list) (sources: int list) : NFA<string, int> =
    TestHelpers.nfaFromEdges vCount edges (Array.ofList sources)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``RPQ Belyanin and Kronecker agree on 100-vertex line graph`` () =
    let n = 100
    let edges = [ for i in 0 .. n - 2 -> (i, "a", i + 1) ]
    let nfa = nfaWithSources n edges [ 0 ]
    let dfa = TestHelpers.buildDfa [ (0, "a", 0) ] 0 [ 0 ]

    let belyResult = BelyaninRPQ.evaluate dfa nfa
    let kronResult = KroneckerRPQ.evaluate dfa nfa
    let regexp = Regexp.RStar(RTerm(Terminal "a"))
    let arroResult = ArroyueloRPQ.evaluate nfa regexp

    for i in 0 .. n - 1 do
        Assert.True(Matrix.get belyResult 0 i = Matrix.get kronResult 0 i, sprintf "Mismatch at vertex %d" i)
        Assert.True(Matrix.get belyResult 0 i = Matrix.get arroResult 0 i, sprintf "Arroyuelo mismatch at vertex %d" i)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``RPQ Belyanin and Kronecker agree on 80-vertex random-like graph`` () =
    let n = 80
    let dfa = TestHelpers.buildDfa [ (0, "a", 1); (1, "b", 2) ] 0 [ 2 ]

    let edges =
        [ for i in 0 .. n - 2 do
              yield (i, "a", i + 1)

              if i % 3 = 0 then
                  yield (i, "b", i + 2) ]
        |> List.filter (fun (_, _, t) -> t < n)

    let nfa = nfaWithSources n edges [ 0 ]

    let belyResult = BelyaninRPQ.evaluate dfa nfa
    let kronResult = KroneckerRPQ.evaluate dfa nfa

    for i in 0 .. n - 1 do
        Assert.True(Matrix.get belyResult 0 i = Matrix.get kronResult 0 i)

[<Properties(Arbitrary = [| typeof<StressRpqGenerators> |], MaxTest = 5)>]
module StressRpqProperties =

    [<Property>]
    [<Trait("Category", "Stress")>]
    let ``Belyanin and Kronecker agree on large random graphs`` (d: RPQTestData) =
        if d.sources.Length = 0 || d.vertexCount = 0 then
            true
        else
            let source = min d.sources.[0] (d.vertexCount - 1)
            let nfa = TestHelpers.nfaFromEdges d.vertexCount d.edges [| source |]
            let dfa = TestHelpers.buildDfa [ (0, "a", 1) ] 0 [ 1 ]

            let belyResult = BelyaninRPQ.evaluate dfa nfa
            let kronResult = KroneckerRPQ.evaluate dfa nfa

            let mutable ok = true

            for j in 0 .. d.vertexCount - 1 do
                if Matrix.get belyResult 0 j <> Matrix.get kronResult 0 j then
                    ok <- false

            ok
