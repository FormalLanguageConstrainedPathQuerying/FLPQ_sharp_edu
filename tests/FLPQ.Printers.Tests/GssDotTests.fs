module GssDotTests

open Xunit
open FLPQ.Languages
open FLPQ.Printers

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``empty GSS dot compiles`` () =
    let gss = GSS.init 3 4

    let dot =
        GssDot.toDot (fun idx -> sprintf "%d" idx) (fun _ -> "") Set.empty Set.empty None gss

    Assert.Contains("digraph GSS", dot)
    Assert.Contains("rankdir=LR", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(0, info.NodeCount)
    Assert.Equal(0, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS with edges dot compiles`` () =
    let gss = GSS.init 3 4

    let edgeInfo: GssEdgeInfo =
        { ReturnState = 1
          PreCallState = 0
          PreCallVertex = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    GSS.addEdge gss 5 2 edgeInfo |> ignore

    let dot =
        GssDot.toDot
            (fun idx ->
                let state = idx / 4
                let vertex = idx % 4
                sprintf "(%d,%d)" state vertex)
            (fun (from, _) -> sprintf "e%d" from)
            Set.empty
            Set.empty
            None
            gss

    Assert.Contains("digraph GSS", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(2, info.NodeCount)
    Assert.Equal(1, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS with highlighted vertices and edges`` () =
    let gss = GSS.init 3 4

    let edgeInfo1: GssEdgeInfo =
        { ReturnState = 1
          PreCallState = 0
          PreCallVertex = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    let edgeInfo2: GssEdgeInfo =
        { ReturnState = 2
          PreCallState = 1
          PreCallVertex = 1
          MatchedRange = RangeDescriptor.EmptyRange }

    GSS.addEdge gss 5 2 edgeInfo1 |> ignore
    GSS.addEdge gss 9 6 edgeInfo2 |> ignore

    let dot =
        GssDot.toDot (fun idx -> sprintf "%d" idx) (fun _ -> "call") (set [ 9 ]) (set [ (5, 2) ]) None gss

    Assert.Contains("fillcolor=lightyellow", dot)
    Assert.Contains("color=red", dot)
    Assert.Contains("penwidth=2.0", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(4, info.NodeCount)
    Assert.Equal(2, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS current vertex gets lightblue fill`` () =
    let gss = GSS.init 3 4

    let edgeInfo: GssEdgeInfo =
        { ReturnState = 1
          PreCallState = 0
          PreCallVertex = 0
          MatchedRange = RangeDescriptor.EmptyRange }

    GSS.addEdge gss 5 2 edgeInfo |> ignore
    GSS.addEdge gss 5 7 edgeInfo |> ignore

    let dot =
        GssDot.toDot (fun idx -> sprintf "%d" idx) (fun _ -> "call") (set [ 7 ]) Set.empty (Some 5) gss

    Assert.Contains("fillcolor=lightblue", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(3, info.NodeCount)
    Assert.Equal(2, info.EdgeCount)

[<Fact>]
let ``toDotFromSets stored-pop vertex gets orange fill`` () =
    let dot =
        GssDot.toDotFromSets
            (fun idx -> sprintf "%d" idx)
            (fun _ -> "e")
            (set [ 0; 2; 5 ])
            (set [ (0, 2); (5, 7) ])
            (set [ 5 ])
            Set.empty
            (set [ 2 ])
            (Some 0)

    Assert.Contains("fillcolor=orange", dot)
    Assert.Contains("fillcolor=lightblue", dot)
    Assert.Contains("fillcolor=lightyellow", dot)

    let info = ExternalTools.compileDotStringToInfo dot
    // vertex 2 is the stored-pop vertex -> orange
    let orangeNodes =
        info.NodeFillColors |> List.filter (fun c -> c.Contains "orange") |> List.length

    Assert.Equal(1, orangeNodes)

[<Fact>]
let ``toDotFromSets current vertex takes priority over stored-pop`` () =
    // Vertex 0 is both current and a stored-pop vertex; it must render lightblue, not orange.
    let dot =
        GssDot.toDotFromSets
            (fun idx -> sprintf "%d" idx)
            (fun _ -> "e")
            (set [ 0 ])
            Set.empty
            Set.empty
            Set.empty
            (set [ 0 ])
            (Some 0)

    Assert.Contains("fillcolor=lightblue", dot)
    Assert.DoesNotContain("fillcolor=orange", dot)
