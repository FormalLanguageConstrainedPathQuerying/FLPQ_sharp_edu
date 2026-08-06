module StressTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private nfaWithSources (vCount: int) (edges: Trans<string> list) (sources: int list) : NFA<string, int> =
    TestHelpers.nfaFromEdges vCount edges (Array.ofList sources)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``RPQ Belyanin and Kronecker agree on 100-vertex line graph`` () =
    let n = 100
    let edges = [ for i in 0 .. n - 2 -> { From = i; Label = "a"; To = i + 1 } ]
    let nfa = nfaWithSources n edges [ 0 ]
    let dfa = TestHelpers.buildDfa [ { From = 0; Label = "a"; To = 0 } ] 0 [ 0 ]

    let belyResult = BelyaninRPQ.evaluate dfa nfa
    let kronResult = KroneckerRPQ.evaluate dfa nfa
    let regexp = Regexp.RStar(RTerm(Terminal "a"))
    let arroResult = ArroyueloRPQ.evaluate nfa regexp

    for i in 0 .. n - 1 do
        Assert.True(belyResult.[0, i] = kronResult.[0, i], sprintf "Mismatch at vertex %d" i)
        Assert.True(belyResult.[0, i] = arroResult.[0, i], sprintf "Arroyuelo mismatch at vertex %d" i)

[<Fact>]
[<Trait("Category", "Stress")>]
let ``RPQ Belyanin and Kronecker agree on 80-vertex random-like graph`` () =
    let n = 80

    let dfa =
        TestHelpers.buildDfa [ { From = 0; Label = "a"; To = 1 }; { From = 1; Label = "b"; To = 2 } ] 0 [ 2 ]

    let edges =
        [ for i in 0 .. n - 2 do
              yield { From = i; Label = "a"; To = i + 1 }

              if i % 3 = 0 then
                  yield { From = i; Label = "b"; To = i + 2 } ]
        |> List.filter (fun t -> t.To < n)

    let nfa = nfaWithSources n edges [ 0 ]

    let belyResult = BelyaninRPQ.evaluate dfa nfa
    let kronResult = KroneckerRPQ.evaluate dfa nfa

    for i in 0 .. n - 1 do
        Assert.True(belyResult.[0, i] = kronResult.[0, i])

[<Properties(Arbitrary = [| typeof<StressRpqGenerators> |], MaxTest = 5)>]
module StressRpqProperties =

    [<Property>]
    [<Trait("Category", "Stress")>]
    let ``Belyanin and Kronecker agree on large random graphs`` (d: RPQTestData) =
        if d.Sources.Length = 0 || d.VertexCount = 0 then
            true
        else
            let source = min d.Sources.[0] (d.VertexCount - 1)
            let nfa = TestHelpers.nfaFromEdges d.VertexCount d.Edges [| source |]
            let dfa = TestHelpers.buildDfa [ { From = 0; Label = "a"; To = 1 } ] 0 [ 1 ]

            let belyResult = BelyaninRPQ.evaluate dfa nfa
            let kronResult = KroneckerRPQ.evaluate dfa nfa

            let mutable ok = true

            for j in 0 .. d.VertexCount - 1 do
                if belyResult.[0, j] <> kronResult.[0, j] then
                    ok <- false

            ok
