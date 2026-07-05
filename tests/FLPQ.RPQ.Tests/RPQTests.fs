module RPQTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private smallGraphNfa (edges: (int * string * int) list) : NFA<string, int> =
    let allVerts = edges |> List.collect (fun (f, _, t) -> [ f; t ]) |> List.distinct
    let vCount = if List.isEmpty allVerts then 0 else (List.max allVerts) + 1
    let states = [ 0 .. vCount - 1 ]
    Nfa.fromTransitions states edges Set.empty Set.empty Set.empty

let private vc (nfa: NFA<string, int>) = Nfa.stateCount nfa

let private buildDfa (transitions: (int * string * int) list) (startState: int) (finalStates: int list) =
    let allStates =
        transitions
        |> List.collect (fun (f, _, t) -> [ f; t ])
        |> List.append (startState :: finalStates)
        |> List.distinct
        |> List.sort

    Dfa.fromTransitions (List.map id allStates) transitions startState (Set.ofList finalStates)

let private nfaWithSources (edges: (int * string * int) list) (sources: int list) : NFA<string, int> =
    let allVerts = edges |> List.collect (fun (f, _, t) -> [ f; t ]) |> List.distinct

    let allWithSources = allVerts @ sources |> List.distinct

    let vCount =
        if List.isEmpty allWithSources then
            0
        else
            (List.max allWithSources) + 1

    let states = [ 0 .. vCount - 1 ]
    Nfa.fromTransitions states edges Set.empty (Set.ofList sources) Set.empty

// --- GraphReader tests (task 62, updated for task 64) ---

[<Fact>]
let ``GraphReader: no start vertices specified, all vertices as sources`` () =
    let text = "0 a 1\n1 b 2"
    let g = GraphReader.parseGraph text
    Assert.Equal(3, Nfa.stateCount g)
    Assert.True(Set.ofList [ 0; 1; 2 ] = g.startStates)
    Assert.True(g.transitions.data.[0, 1].IsSome)
    Assert.True(g.transitions.data.[1, 2].IsSome)

[<Fact>]
let ``GraphReader: explicit start vertices`` () =
    let text = "0 2\n0 a 1\n1 b 2"
    let g = GraphReader.parseGraph text
    Assert.Equal(3, Nfa.stateCount g)
    Assert.True(Set.ofList [ 0; 2 ] = g.startStates)

[<Fact>]
let ``GraphReader: per-label adjacency`` () =
    let text = "0 a 1\n0 b 2"
    let g = GraphReader.parseGraph text
    Assert.True(Set.ofList [ "a"; "b" ] = Nfa.alphabet g)
    Assert.True(g.transitions.data.[0, 1].IsSome)
    Assert.True(g.transitions.data.[0, 2].IsSome)

// --- Belyanin tests (task 59, updated for task 64) ---

[<Fact>]
let ``Belyanin: single edge v0-[a]->v1, query a, v1 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1) ] [ 0 ]
    let dfa = buildDfa [ (0, "a", 1) ] 0 [ 1 ]
    let result = BelyaninRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 1])

[<Fact>]
let ``Belyanin: v0-[a]->v1-[b]->v2, query a*b, v2 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (1, "b", 2) ] [ 0 ]
    let dfa = buildDfa [ (0, "a", 0); (0, "b", 1) ] 0 [ 1 ]
    let result = BelyaninRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 2])

[<Fact>]
let ``Belyanin: v0-[a]->v1-[a]->v2, query a+, v1 and v2 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (1, "a", 2) ] [ 0 ]
    let dfa = buildDfa [ (0, "a", 1); (1, "a", 1) ] 0 [ 1 ]
    let result = BelyaninRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 1])
    Assert.True(result.data.[0, 2])

[<Fact>]
let ``Belyanin: cycle v0-[a]->v1-[a]->v0, query a*, both reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (1, "a", 0) ] [ 0 ]
    let dfa = buildDfa [ (0, "a", 0) ] 0 [ 0 ]
    let result = BelyaninRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 0])
    Assert.True(result.data.[0, 1])

// --- Arroyuelo tests (task 60, updated for task 64) ---

[<Fact>]
let ``Arroyuelo: single edge v0-[a]->v1, query a, v1 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1) ] [ 0 ]
    let regexp = Regexp.RTerm(Terminal "a")
    let result = ArroyueloRPQ.evaluate nfa regexp
    Assert.True(result.data.[0, 1])

[<Fact>]
let ``Arroyuelo: v0-[a]->v1-[b]->v2, query a b, v2 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (1, "b", 2) ] [ 0 ]
    let regexp = Regexp.RSeq(Regexp.RTerm(Terminal "a"), Regexp.RTerm(Terminal "b"))
    let result = ArroyueloRPQ.evaluate nfa regexp
    Assert.True(result.data.[0, 2])

[<Fact>]
let ``Arroyuelo: alternation a|b, both branches`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (0, "b", 2) ] [ 0 ]
    let regexp = Regexp.RAlt(Regexp.RTerm(Terminal "a"), Regexp.RTerm(Terminal "b"))
    let result = ArroyueloRPQ.evaluate nfa regexp
    Assert.True(result.data.[0, 1])
    Assert.True(result.data.[0, 2])

[<Fact>]
let ``Arroyuelo: Kleene star a* on path, all pairs`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (1, "a", 2) ] [ 0 ]
    let regexp = Regexp.RStar(Regexp.RTerm(Terminal "a"))
    let result = ArroyueloRPQ.evaluate nfa regexp
    Assert.True(result.data.[0, 0])
    Assert.True(result.data.[0, 1])
    Assert.True(result.data.[0, 2])

[<Fact>]
let ``Arroyuelo: epsilon query returns identity`` () =
    let nfa = nfaWithSources [ (0, "a", 1) ] [ 0; 1 ]
    let regexp = Regexp.REps
    let result = ArroyueloRPQ.evaluate nfa regexp
    Assert.True(result.data.[0, 0])
    Assert.False(result.data.[0, 1])
    Assert.True(result.data.[1, 1])
    Assert.False(result.data.[1, 0])

// --- Kronecker tests (task 61, updated for task 64) ---

[<Fact>]
let ``Kronecker: single edge v0-[a]->v1, query a, v1 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1) ] [ 0 ]
    let dfa = buildDfa [ (0, "a", 1) ] 0 [ 1 ]
    let result = KroneckerRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 1])

[<Fact>]
let ``Kronecker: v0-[a]->v1-[b]->v2, query a*b, v2 reachable`` () =
    let nfa = nfaWithSources [ (0, "a", 1); (1, "b", 2) ] [ 0 ]
    let dfa = buildDfa [ (0, "a", 0); (0, "b", 1) ] 0 [ 1 ]
    let result = KroneckerRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 2])

[<Fact>]
let ``Kronecker: multiple sources, only one connects`` () =
    let nfa = nfaWithSources [ (0, "a", 2); (1, "b", 2) ] [ 0; 1 ]
    let dfa = buildDfa [ (0, "a", 1) ] 0 [ 1 ]
    let result = KroneckerRPQ.evaluate dfa nfa
    Assert.True(result.data.[0, 2])
    Assert.False(result.data.[1, 2])

// --- Cross-algorithm property-based tests (task 63, updated for task 64) ---

[<Properties(Arbitrary = [| typeof<RPQGenerators> |])>]
module PropertyTests =

    let private smallGraphNfaFromEdges (vCount: int) (edges: (int * string * int) list) : NFA<string, int> =
        let states = [ 0 .. vCount - 1 ]
        Nfa.fromTransitions states edges Set.empty Set.empty Set.empty

    let private nfaWithSourcesProp
        (vCount: int)
        (edges: (int * string * int) list)
        (sources: int[])
        : NFA<string, int> =
        let states = [ 0 .. vCount - 1 ]
        Nfa.fromTransitions states edges Set.empty (Set.ofArray sources) Set.empty

    [<Property>]
    let ``Belyanin and Arroyuelo produce identical results for single source with single-label regex``
        (d: RPQTestData)
        =
        if d.sources.Length = 0 then
            true
        else
            let v = d.vertexCount

            if v = 0 then
                true
            else
                let source = min d.sources.[0] (v - 1)
                let nfaBely = nfaWithSourcesProp v d.edges [| source |]
                let dfa = buildDfa [ (0, "a", 1) ] 0 [ 1 ]
                let belyResult = BelyaninRPQ.evaluate dfa nfaBely

                let nfaArro = nfaWithSourcesProp v d.edges [| source |]
                let regexp = Regexp.RTerm(Terminal "a")
                let arroResult = ArroyueloRPQ.evaluate nfaArro regexp

                let mutable ok = true

                for j in 0 .. v - 1 do
                    if belyResult.data.[0, j] <> arroResult.data.[0, j] then
                        ok <- false

                ok

    [<Property>]
    let ``Belyanin and Kronecker produce identical results for single source`` (d: RPQTestData) =
        if d.sources.Length = 0 then
            true
        else
            let v = d.vertexCount

            if v = 0 then
                true
            else
                let source = min d.sources.[0] (v - 1)
                let nfa = nfaWithSourcesProp v d.edges [| source |]
                let dfa = buildDfa [ (0, "a", 1) ] 0 [ 1 ]
                let belyResult = BelyaninRPQ.evaluate dfa nfa
                let kronResult = KroneckerRPQ.evaluate dfa nfa

                let mutable ok = true

                for j in 0 .. v - 1 do
                    if belyResult.data.[0, j] <> kronResult.data.[0, j] then
                        ok <- false

                ok

    [<Property>]
    let ``Arroyuelo and Kronecker produce identical results`` (d: RPQTestData) =
        if d.sources.Length = 0 then
            true
        else
            let v = d.vertexCount

            if v = 0 then
                true
            else
                let safeSources =
                    d.sources |> Array.map (fun s -> min s (v - 1)) |> Set.ofArray |> Set.toArray

                let nfa = nfaWithSourcesProp v d.edges safeSources
                let dfa = buildDfa [ (0, "a", 1) ] 0 [ 1 ]
                let regexp = Regexp.RTerm(Terminal "a")
                let arroResult = ArroyueloRPQ.evaluate nfa regexp
                let kronResult = KroneckerRPQ.evaluate dfa nfa

                let mutable ok = true
                let rows = arroResult.rows

                for i in 0 .. rows - 1 do
                    for j in 0 .. v - 1 do
                        if arroResult.data.[i, j] <> kronResult.data.[i, j] then
                            ok <- false

                ok
