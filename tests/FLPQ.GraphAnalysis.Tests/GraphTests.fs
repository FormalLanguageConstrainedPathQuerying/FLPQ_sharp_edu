module GraphTests

open Xunit
open FsCheck.Xunit
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra

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
    let m = Matrix.create 3 3 (fun i j -> i <> j)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m
    let filtered = Graph.filterOutgoing (set [ 0 ]) g
    Assert.True(Graph.edge filtered 0 1)
    Assert.True(Graph.edge filtered 0 2)
    Assert.False(Graph.edge filtered 1 2)
    Assert.False(Graph.edge filtered 1 0)

[<Fact>]
let ``filterIncoming keeps only edges to selected vertices`` () =
    let m = Matrix.create 3 3 (fun i j -> i <> j)
    let g = Graph.fromEdges [ "A"; "B"; "C" ] m
    let filtered = Graph.filterIncoming (set [ 2 ]) g
    Assert.True(Graph.edge filtered 0 2)
    Assert.True(Graph.edge filtered 1 2)
    Assert.False(Graph.edge filtered 0 1)
    Assert.False(Graph.edge filtered 1 0)

[<Fact>]
let ``filterOutgoing all vertices is identity`` () =
    let m = Matrix.create 3 3 (fun i j -> i <> j)
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
