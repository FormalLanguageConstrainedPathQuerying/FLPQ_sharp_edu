module MsBfsTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.LinearAlgebra

module MyGen = FsCheck.FSharp.Gen
module MyArb = FsCheck.FSharp.Arb

[<Fact>]
let ``boolAdd: element-wise OR`` () =
    let a = Matrix.init 2 2 false
    a.data.[0, 0] <- true
    let b = Matrix.init 2 2 false
    b.data.[0, 1] <- true
    let r = MsBfs.boolAdd a b
    Assert.True(r.data.[0, 0])
    Assert.True(r.data.[0, 1])
    Assert.False(r.data.[1, 0])
    Assert.False(r.data.[1, 1])

[<Fact>]
let ``boolMul: matrix product in Boolean semiring`` () =
    let a = Matrix.init 2 2 false
    a.data.[0, 0] <- true
    a.data.[1, 1] <- true
    let b = Matrix.init 2 2 false
    b.data.[0, 1] <- true
    b.data.[1, 0] <- true
    let r = MsBfs.boolMul a b
    Assert.True(r.data.[0, 1])
    Assert.True(r.data.[1, 0])
    Assert.False(r.data.[0, 0])
    Assert.False(r.data.[1, 1])

[<Fact>]
let ``maskFilter: keeps values from first only where second is false`` () =
    let newFront = Matrix.init 1 2 false
    newFront.data.[0, 0] <- true
    let visited = Matrix.init 1 2 false
    visited.data.[0, 1] <- true
    let r = MsBfs.maskFilter newFront visited
    Assert.True(r.data.[0, 0])
    Assert.False(r.data.[0, 1])

[<Fact>]
let ``maskFilter: [1,1] +_M [0,1] = [1,0]`` () =
    let newFront = Matrix.init 1 2 false
    newFront.data.[0, 0] <- true
    newFront.data.[0, 1] <- true
    let visited = Matrix.init 1 2 false
    visited.data.[0, 1] <- true
    let r = MsBfs.maskFilter newFront visited
    Assert.True(r.data.[0, 0])
    Assert.False(r.data.[0, 1])

[<Fact>]
let ``msBfs: simple path graph v0->v1->v2, sources [v0, v1]`` () =
    let n = 3
    let m = Matrix.init n n false
    m.data.[0, 1] <- true
    m.data.[1, 2] <- true
    let sources = [| 0; 1 |]
    let result = MsBfs.msBfs sources m
    Assert.True(result.data.[0, 0])
    Assert.True(result.data.[0, 1])
    Assert.True(result.data.[0, 2])
    Assert.False(result.data.[1, 0])
    Assert.True(result.data.[1, 1])
    Assert.True(result.data.[1, 2])

[<Fact>]
let ``msBfs: disconnected graph, one source in each component`` () =
    let n = 4
    let m = Matrix.init n n false
    m.data.[0, 1] <- true
    m.data.[2, 3] <- true
    let sources = [| 0; 2 |]
    let result = MsBfs.msBfs sources m
    Assert.True(result.data.[0, 0])
    Assert.True(result.data.[0, 1])
    Assert.False(result.data.[0, 2])
    Assert.False(result.data.[0, 3])
    Assert.False(result.data.[1, 0])
    Assert.False(result.data.[1, 1])
    Assert.True(result.data.[1, 2])
    Assert.True(result.data.[1, 3])

[<Fact>]
let ``msBfs: complete graph, single source v0`` () =
    let n = 4
    let m = Matrix.init n n false

    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            if i <> j then
                m.data.[i, j] <- true

    let sources = [| 0 |]
    let result = MsBfs.msBfs sources m

    for j in 0 .. n - 1 do
        Assert.True(result.data.[0, j])

[<Fact>]
let ``msBfs: no sources, result is zero matrix`` () =
    let n = 3
    let m = Matrix.init n n false
    m.data.[0, 1] <- true
    let sources: int[] = Array.empty
    let result = MsBfs.msBfs sources m
    Assert.Equal(0, result.rows)

[<Fact>]
let ``msBfs: self-loop source, reachable only to self`` () =
    let n = 2
    let m = Matrix.init n n false
    m.data.[0, 0] <- true
    let sources = [| 0 |]
    let result = MsBfs.msBfs sources m
    Assert.True(result.data.[0, 0])
    Assert.False(result.data.[0, 1])

[<Property>]
let ``msBfs equals independent single-source BFS`` () =
    let mutable succeeded = true

    for _ in 0..9 do
        let k = 1 + System.Random.Shared.Next(4)
        let n = 1 + System.Random.Shared.Next(7)

        let edges =
            let ec = System.Random.Shared.Next(n * 2)

            [ for _ in 1..ec do
                  let f = System.Random.Shared.Next(n)
                  let t = System.Random.Shared.Next(n)

                  if f <> t then
                      (f, t) ]

        let sources = [| for _ in 1..k -> System.Random.Shared.Next(n) |]

        let m = Matrix.init n n false

        for (fromV, toV) in edges do
            m.data.[fromV, toV] <- true

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
                              if m.data.[v, u] && not (Set.contains u visited) then
                                  yield u ])
                    |> Set.ofList

                if not (Set.isEmpty newFront) then
                    changed <- true
                    front <- newFront
                    visited <- Set.union visited newFront

            [| for j in 0 .. n - 1 -> Set.contains j visited |]

        for i in 0 .. sources.Length - 1 do
            let expected = single sources.[i]

            for j in 0 .. n - 1 do
                if msResult.data.[i, j] <> expected.[j] then
                    succeeded <- false

    succeeded
