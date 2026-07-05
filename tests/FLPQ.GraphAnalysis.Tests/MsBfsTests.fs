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
    Matrix.set a 0 0 true
    let b = Matrix.init 2 2 false
    Matrix.set b 0 1 true
    let r = MsBfs.boolAdd a b
    Assert.True(Matrix.get r 0 0)
    Assert.True(Matrix.get r 0 1)
    Assert.False(Matrix.get r 1 0)
    Assert.False(Matrix.get r 1 1)

[<Fact>]
let ``boolMul: matrix product in Boolean semiring`` () =
    let a = Matrix.init 2 2 false
    Matrix.set a 0 0 true
    Matrix.set a 1 1 true
    let b = Matrix.init 2 2 false
    Matrix.set b 0 1 true
    Matrix.set b 1 0 true
    let r = MsBfs.boolMul a b
    Assert.True(Matrix.get r 0 1)
    Assert.True(Matrix.get r 1 0)
    Assert.False(Matrix.get r 0 0)
    Assert.False(Matrix.get r 1 1)

[<Fact>]
let ``maskFilter: keeps values from first only where second is false`` () =
    let newFront = Matrix.init 1 2 false
    Matrix.set newFront 0 0 true
    let visited = Matrix.init 1 2 false
    Matrix.set visited 0 1 true
    let r = MsBfs.maskFilter newFront visited
    Assert.True(Matrix.get r 0 0)
    Assert.False(Matrix.get r 0 1)

[<Fact>]
let ``maskFilter: [1,1] +_M [0,1] = [1,0]`` () =
    let newFront = Matrix.init 1 2 false
    Matrix.set newFront 0 0 true
    Matrix.set newFront 0 1 true
    let visited = Matrix.init 1 2 false
    Matrix.set visited 0 1 true
    let r = MsBfs.maskFilter newFront visited
    Assert.True(Matrix.get r 0 0)
    Assert.False(Matrix.get r 0 1)

[<Fact>]
let ``msBfs: simple path graph v0->v1->v2, sources [v0, v1]`` () =
    let n = 3
    let m = Matrix.init n n false
    Matrix.set m 0 1 true
    Matrix.set m 1 2 true
    let sources = [| 0; 1 |]
    let result = MsBfs.msBfs sources m
    Assert.True(Matrix.get result 0 0)
    Assert.True(Matrix.get result 0 1)
    Assert.True(Matrix.get result 0 2)
    Assert.False(Matrix.get result 1 0)
    Assert.True(Matrix.get result 1 1)
    Assert.True(Matrix.get result 1 2)

[<Fact>]
let ``msBfs: disconnected graph, one source in each component`` () =
    let n = 4
    let m = Matrix.init n n false
    Matrix.set m 0 1 true
    Matrix.set m 2 3 true
    let sources = [| 0; 2 |]
    let result = MsBfs.msBfs sources m
    Assert.True(Matrix.get result 0 0)
    Assert.True(Matrix.get result 0 1)
    Assert.False(Matrix.get result 0 2)
    Assert.False(Matrix.get result 0 3)
    Assert.False(Matrix.get result 1 0)
    Assert.False(Matrix.get result 1 1)
    Assert.True(Matrix.get result 1 2)
    Assert.True(Matrix.get result 1 3)

[<Fact>]
let ``msBfs: complete graph, single source v0`` () =
    let n = 4
    let m = Matrix.init n n false

    for i in 0 .. n - 1 do
        for j in 0 .. n - 1 do
            if i <> j then
                Matrix.set m i j true

    let sources = [| 0 |]
    let result = MsBfs.msBfs sources m

    for j in 0 .. n - 1 do
        Assert.True(Matrix.get result 0 j)

[<Fact>]
let ``msBfs: no sources, result is zero matrix`` () =
    let n = 3
    let m = Matrix.init n n false
    Matrix.set m 0 1 true
    let sources: int[] = Array.empty
    let result = MsBfs.msBfs sources m
    Assert.Equal(0, Matrix.rows result)

[<Fact>]
let ``msBfs: self-loop source, reachable only to self`` () =
    let n = 2
    let m = Matrix.init n n false
    Matrix.set m 0 0 true
    let sources = [| 0 |]
    let result = MsBfs.msBfs sources m
    Assert.True(Matrix.get result 0 0)
    Assert.False(Matrix.get result 0 1)

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
                              if Matrix.get m v u && not (Set.contains u visited) then
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
                      if Matrix.get msResult i j <> expected.[j] then
                          yield false ]
            |> List.forall id
