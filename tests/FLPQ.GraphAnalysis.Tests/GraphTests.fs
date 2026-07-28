module GraphTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra
open FLPQ.TestUtilities

[<Fact>]
let ``Graph fromEdges has correct vertex count`` () =
    let g = Graph.fromEdges [ "A"; "B"; "C" ] (Matrix.init 3 3 None)
    Assert.Equal(3, Graph.vertexCount g)

[<Fact>]
let ``Graph fromEdges has correct vertex access`` () =
    let g = Graph.fromEdges [ "A"; "B"; "C" ] (Matrix.init 3 3 None)
    Assert.Equal(Some "A", Graph.tryGetVertex 0 g)
    Assert.Equal(Some "B", Graph.tryGetVertex 1 g)
    Assert.Equal(Some "C", Graph.tryGetVertex 2 g)
    Assert.Equal(None, Graph.tryGetVertex 3 g)

[<Fact>]
let ``Graph vertices returns sorted list`` () =
    let g = Graph.fromEdges [ "C"; "A"; "B" ] (Matrix.init 3 3 None)
    let verts = Graph.vertices g
    Assert.Equal<string list>([ "C"; "A"; "B" ], verts |> List.map snd)

[<Fact>]
let ``Graph edge access works`` () =
    let m = Matrix.create 3 3 (fun i j -> Some(i + j))
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m
    Assert.Equal(Some 0, Graph.edge g 0 0)
    Assert.Equal(Some 2, Graph.edge g 1 1)

[<Fact>]
let ``Graph mapVertices transforms vertex labels`` () =
    let g = Graph.fromEdges [ "A"; "B" ] (Matrix.init 2 2 None)
    let g2 = Graph.mapVertices (fun s -> s + s) g
    Assert.Equal(Some "AA", Graph.tryGetVertex 0 g2)
    Assert.Equal(Some "BB", Graph.tryGetVertex 1 g2)

[<Fact>]
let ``Graph mapEdges transforms edge values`` () =
    let g = Graph.fromEdges [ "A"; "B" ] (Matrix.init 2 2 (Some 1))
    let g2 = Graph.mapEdges (Option.map (fun x -> x * 2)) g
    Assert.Equal(Some 2, Graph.edge g2 0 0)

[<Fact>]
let ``filterOutgoing keeps only edges from selected vertices`` () =
    let m = Matrix.create 3 3 (<>)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m
    let filtered = Graph.filterOutgoing (set [ 0 ]) g
    Assert.True(Graph.edge filtered 0 1)
    Assert.True(Graph.edge filtered 0 2)
    Assert.False(Graph.edge filtered 1 2)
    Assert.False(Graph.edge filtered 1 0)

[<Fact>]
let ``filterIncoming keeps only edges to selected vertices`` () =
    let m = Matrix.create 3 3 (<>)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m
    let filtered = Graph.filterIncoming (set [ 2 ]) g
    Assert.True(Graph.edge filtered 0 2)
    Assert.True(Graph.edge filtered 1 2)
    Assert.False(Graph.edge filtered 0 1)
    Assert.False(Graph.edge filtered 1 0)

[<Fact>]
let ``filterOutgoing all vertices is identity`` () =
    let m = Matrix.create 3 3 (<>)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m
    let filtered = Graph.filterOutgoing (set [ 0; 1; 2 ]) g
    Assert.Equal(g.Edges, filtered.Edges)

[<Fact>]
let ``filterOutgoing empty set yields no edges`` () =
    let filterFilter name filter =
        let m = Matrix.create 3 3 (fun i j -> true)
        let g = Graph.fromEdges [ "A"; "B"; "C" ] m
        let filtered = filter Set.empty g

        for i in 0..2 do
            for j in 0..2 do
                Assert.False(Graph.edge filtered i j)

    filterFilter "outgoing" Graph.filterOutgoing
    filterFilter "incoming" Graph.filterIncoming

[<Fact>]
let ``getVertex returns correct value`` () =
    let g = Graph.fromEdges [ "A"; "B"; "C" ] (Matrix.init 3 3 None)
    Assert.Equal("A", Graph.getVertex 0 g)
    Assert.Equal("B", Graph.getVertex 1 g)

[<Fact>]
let ``getVertex throws for missing index`` () =
    let g = Graph.fromEdges [ "A"; "B" ] (Matrix.init 2 2 None)
    Assert.Throws<System.Collections.Generic.KeyNotFoundException>(fun () -> Graph.getVertex 5 g |> ignore)

[<Fact>]
let ``keepVertices keeps selected vertices and remaps indices`` () =
    let m = Matrix.create 4 4 (fun i j -> Some(i * 4 + j))
    let g = Graph.fromEdges [ "A"; "B"; "C"; "D" ] m
    let kept = Graph.keepVertices (set [ 1; 3 ]) g
    Assert.Equal(2, Graph.vertexCount kept)
    Assert.Equal(Some "B", Graph.tryGetVertex 0 kept)
    Assert.Equal(Some "D", Graph.tryGetVertex 1 kept)
    Assert.Equal(Some 7, Graph.edge kept 0 1)

[<Fact>]
let ``filterOutgoingGeneric filters with custom operations`` () =
    let m = Matrix.create 3 3 (fun i j -> if i <> j then Some 1 else None)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m

    let filtered =
        Graph.filterOutgoingGeneric None (fun keep edge -> if keep then edge else None) Option.orElse (set [ 0; 1 ]) g

    Assert.True((Graph.edge filtered 0 1).IsSome)
    Assert.True((Graph.edge filtered 0 2).IsSome)
    Assert.True((Graph.edge filtered 1 0).IsSome)
    Assert.True((Graph.edge filtered 1 2).IsSome)
    Assert.False((Graph.edge filtered 2 0).IsSome)
    Assert.False((Graph.edge filtered 2 1).IsSome)

[<Fact>]
let ``filterIncomingGeneric filters with custom operations`` () =
    let m = Matrix.create 3 3 (fun i j -> if i <> j then Some 1 else None)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m

    let filtered =
        Graph.filterIncomingGeneric None (fun edge keep -> if keep then edge else None) Option.orElse (set [ 0; 2 ]) g

    Assert.True((Graph.edge filtered 1 0).IsSome)
    Assert.True((Graph.edge filtered 1 2).IsSome)
    Assert.True((Graph.edge filtered 0 2).IsSome)
    Assert.True((Graph.edge filtered 2 0).IsSome)
    Assert.False((Graph.edge filtered 0 1).IsSome)
    Assert.False((Graph.edge filtered 2 1).IsSome)

[<Properties(Arbitrary = [| typeof<RandomGraphGenerators> |])>]
module PropertyGraphTests =

    let private matricesEqual (a: Matrix<bool>) (b: Matrix<bool>) : bool =
        if Matrix.rows a <> Matrix.rows b || Matrix.cols a <> Matrix.cols b then
            false
        else
            let mutable ok = true

            for i in 0 .. Matrix.rows a - 1 do
                for j in 0 .. Matrix.cols a - 1 do
                    if Matrix.get a i j <> Matrix.get b i j then
                        ok <- false

            ok

    [<Property>]
    let ``filterOutgoing with all vertices is identity`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let allVerts = Set.ofSeq [ 0 .. n - 1 ]
            let filtered = Graph.filterOutgoing allVerts g
            matricesEqual g.Edges filtered.Edges

    [<Property>]
    let ``filterIncoming with all vertices is identity`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let allVerts = Set.ofSeq [ 0 .. n - 1 ]
            let filtered = Graph.filterIncoming allVerts g
            matricesEqual g.Edges filtered.Edges

    [<Property>]
    let ``filterOutgoing empty set clears all edges`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let filtered = Graph.filterOutgoing Set.empty g

            let mutable ok = true

            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    if Matrix.get filtered.Edges i j then
                        ok <- false

            ok

    [<Property>]
    let ``filterIncoming empty set clears all edges`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let filtered = Graph.filterIncoming Set.empty g

            let mutable ok = true

            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    if Matrix.get filtered.Edges i j then
                        ok <- false

            ok

    [<Property>]
    let ``keepVertices preserves vertex count`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let keep = Set.ofSeq [ 0 .. min 2 (n - 1) ]
            let kept = Graph.keepVertices keep g
            Graph.vertexCount kept = Set.count keep

    [<Property>]
    let ``mapVertices preserves vertex count and edges`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let g2 = Graph.mapVertices (fun i -> i * 2) g
            Graph.vertexCount g2 = n && matricesEqual g.Edges g2.Edges

    [<Property>]
    let ``mapEdges preserves vertex count`` (m: Matrix<bool>, sources: int[]) =
        let n = Matrix.rows m

        if n <= 0 then
            true
        else
            let g = Graph.fromEdges [ 0 .. n - 1 ] m
            let g2 = Graph.mapEdges not g
            Graph.vertexCount g2 = n

    [<Property>]
    let ``fromEdges produces correct dimensions`` () =
        let states = [ "A"; "B"; "C"; "D" ]
        let m = Matrix.init 4 4 false
        let g = Graph.fromEdges states m

        Graph.vertexCount g = List.length states
        && Matrix.rows g.Edges = List.length states
        && Matrix.cols g.Edges = List.length states
