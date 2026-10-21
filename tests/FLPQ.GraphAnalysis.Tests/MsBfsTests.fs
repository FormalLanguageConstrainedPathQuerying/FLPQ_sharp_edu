module MsBfsTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

[<Fact>]
let ``boolAdd: element-wise OR`` () =
    let a = Matrix.init 2 2 false
    a.[0, 0] <- true
    let b = Matrix.init 2 2 false
    b.[0, 1] <- true
    let r = MsBfs.boolAdd a b
    Assert.True(r.[0, 0])
    Assert.True(r.[0, 1])
    Assert.False(r.[1, 0])
    Assert.False(r.[1, 1])

[<Fact>]
let ``boolMul: matrix product in Boolean semiring`` () =
    let a = Matrix.init 2 2 false
    a.[0, 0] <- true
    a.[1, 1] <- true
    let b = Matrix.init 2 2 false
    b.[0, 1] <- true
    b.[1, 0] <- true
    let r = MsBfs.boolMul a b
    Assert.True(r.[0, 1])
    Assert.True(r.[1, 0])
    Assert.False(r.[0, 0])
    Assert.False(r.[1, 1])

[<Fact>]
let ``maskFilter: keeps values from first only where second is false`` () =
    let newFront = Matrix.init 1 2 false
    newFront.[0, 0] <- true
    let visited = Matrix.init 1 2 false
    visited.[0, 1] <- true
    let r = MsBfs.maskFilter newFront visited
    Assert.True(r.[0, 0])
    Assert.False(r.[0, 1])

[<Fact>]
let ``maskFilter: [1,1] +_M [0,1] = [1,0]`` () =
    let newFront = Matrix.init 1 2 false
    newFront.[0, 0] <- true
    newFront.[0, 1] <- true
    let visited = Matrix.init 1 2 false
    visited.[0, 1] <- true
    let r = MsBfs.maskFilter newFront visited
    Assert.True(r.[0, 0])
    Assert.False(r.[0, 1])

[<Fact>]
let ``msBfs: simple path graph v0->v1->v2, sources [v0, v1]`` () =
    let n = 3
    let m = Matrix.init n n false
    m.[0, 1] <- true
    m.[1, 2] <- true
    let sources = [| 0; 1 |]
    let result = MsBfs.msBfs sources m
    Assert.True(result.[0, 0])
    Assert.True(result.[0, 1])
    Assert.True(result.[0, 2])
    Assert.False(result.[1, 0])
    Assert.True(result.[1, 1])
    Assert.True(result.[1, 2])

[<Fact>]
let ``msBfs: disconnected graph, one source in each component`` () =
    let n = 4
    let m = Matrix.init n n false
    m.[0, 1] <- true
    m.[2, 3] <- true
    let sources = [| 0; 2 |]
    let result = MsBfs.msBfs sources m
    Assert.True(result.[0, 0])
    Assert.True(result.[0, 1])
    Assert.False(result.[0, 2])
    Assert.False(result.[0, 3])
    Assert.False(result.[1, 0])
    Assert.False(result.[1, 1])
    Assert.True(result.[1, 2])
    Assert.True(result.[1, 3])

[<Fact>]
let ``msBfs: complete graph, single source v0`` () =
    let n = 4
    let m = Matrix.init n n false

    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            if i <> j then
                m.[i, j] <- true

    let sources = [| 0 |]
    let result = MsBfs.msBfs sources m

    for j in 0 .. n - 1 do
        Assert.True(result.[0, j])

[<Fact>]
let ``msBfs: no sources, result is zero matrix`` () =
    let n = 3
    let m = Matrix.init n n false
    m.[0, 1] <- true
    let sources: int[] = Array.empty
    let result = MsBfs.msBfs sources m
    Assert.Equal(0, Matrix.rows result)

[<Fact>]
let ``msBfs: self-loop source, reachable only to self`` () =
    let n = 2
    let m = Matrix.init n n false
    m.[0, 0] <- true
    let sources = [| 0 |]
    let result = MsBfs.msBfs sources m
    Assert.True(result.[0, 0])
    Assert.False(result.[0, 1])

[<Fact>]
let ``msBfsWithTrace: final visited equals msBfs`` () =
    let n = 3
    let m = Matrix.init n n false
    m.[0, 1] <- true
    m.[1, 2] <- true
    let sources = [| 0; 1 |]
    let trace, finalVisited = MsBfs.msBfsWithTrace sources m
    let direct = MsBfs.msBfs sources m

    for i in 0 .. sources.Length - 1 do
        for j in 0 .. n - 1 do
            Assert.Equal(direct.[i, j], finalVisited.[i, j])

    // The last round's visited equals the final visited.
    let lastVisited = List.last trace |> snd

    for i in 0 .. sources.Length - 1 do
        for j in 0 .. n - 1 do
            Assert.Equal(finalVisited.[i, j], lastVisited.[i, j])

[<Fact>]
let ``msBfsWithTrace: initial front seeds exactly the source cells`` () =
    let n = 3
    let m = Matrix.init n n false
    m.[0, 1] <- true
    m.[1, 2] <- true
    let sources = [| 0; 1 |]
    let trace, _ = MsBfs.msBfsWithTrace sources m
    let initialFront = List.head trace |> fst

    for i in 0 .. sources.Length - 1 do
        Assert.True(initialFront.[i, sources.[i]], sprintf "source %d not seeded" i)

    // No other cell of the initial front is true.
    for i in 0 .. sources.Length - 1 do
        for j in 0 .. n - 1 do
            if j <> sources.[i] then
                Assert.False(initialFront.[i, j], sprintf "unexpected initial front cell (%d,%d)" i j)

[<Fact>]
let ``msBfsWithTrace: fronts are subset of visited and visited is monotone`` () =
    let n = 4
    let m = Matrix.init n n false
    m.[0, 1] <- true
    m.[1, 2] <- true
    m.[2, 3] <- true
    let sources = [| 0 |]
    let trace, _ = MsBfs.msBfsWithTrace sources m

    for pair in trace do
        let front, visited = pair

        // Every front cell is also visited in the same round.
        for i in 0 .. sources.Length - 1 do
            for j in 0 .. n - 1 do
                if front.[i, j] then
                    Assert.True(visited.[i, j], sprintf "front cell (%d,%d) not in visited" i j)

    // Visited is monotone non-decreasing across rounds.
    let rounds = trace |> List.map snd

    for i in 1 .. List.length rounds - 1 do
        let prev, curr = rounds.[i - 1], rounds.[i]

        for a in 0 .. sources.Length - 1 do
            for b in 0 .. n - 1 do
                if prev.[a, b] then
                    Assert.True(curr.[a, b], sprintf "visited regressed at (%d,%d)" a b)

[<Fact>]
let ``msBfsWithTrace: no sources yields empty trace and zero matrix`` () =
    let n = 3
    let m = Matrix.init n n false
    m.[0, 1] <- true
    let sources: int[] = Array.empty
    let trace, finalVisited = MsBfs.msBfsWithTrace sources m
    Assert.Equal(0, List.length trace)
    Assert.Equal(0, Matrix.rows finalVisited)

[<Properties(Arbitrary = [| typeof<RandomGraphGenerators> |])>]
module PropertyTests =

    [<Property>]
    let ``msBfs equals independent single-source BFS`` ((m: Matrix<bool>, sources: int[])) =
        let n = Matrix.rows m
        let msResult = MsBfs.msBfs sources m

        let single (source: int) : bool[] =
            let mutable front = Set.ofList [ source ]
            let mutable visited = Set.ofList [ source ]
            let mutable changed = true

            while changed do
                changed <- false

                let newFront =
                    front
                    |> Set.toList
                    |> List.collect (fun v ->
                        [ for u in 0 .. n - 1 do
                              if m.[v, u] && not (Set.contains u visited) then
                                  yield u ])
                    |> Set.ofList

                if not (Set.isEmpty newFront) then
                    changed <- true
                    front <- newFront
                    visited <- Set.union visited newFront

            [| for j in 0 .. n - 1 -> Set.contains j visited |]

        if n = 0 then
            true
        else
            [ for i in 0 .. sources.Length - 1 do
                  let expected = single sources.[i]

                  for j in 0 .. n - 1 do
                      if msResult.[i, j] <> expected.[j] then
                          yield false ]
            |> List.forall id
