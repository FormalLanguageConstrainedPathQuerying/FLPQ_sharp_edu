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
            Set.empty
            (set [ 2 ])
            (Some 0)
            None

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
            Set.empty
            (set [ 0 ])
            (Some 0)
            None

    Assert.Contains("fillcolor=lightblue", dot)
    Assert.DoesNotContain("fillcolor=orange", dot)

[<Fact>]
let ``toDotFromSets with positionOf emits one rank=same subgraph per position`` () =
    // Positions via idx/2: {v0,v1} at position 0, {v2,v3} at position 1.
    let dot =
        GssDot.toDotFromSets
            (fun idx -> sprintf "%d" idx)
            (fun _ -> "e")
            (set [ 0; 1; 2; 3 ])
            (set [ (1, 0); (3, 2) ])
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            None
            (Some(fun idx -> idx / 2))

    Assert.Contains("{rank=same; v0; v1;}", dot)
    Assert.Contains("{rank=same; v2; v3;}", dot)

[<Fact>]
let ``toDotFromSets without positionOf emits no rank=same subgraph`` () =
    let dot =
        GssDot.toDotFromSets
            (fun idx -> sprintf "%d" idx)
            (fun _ -> "e")
            (set [ 0; 1 ])
            (set [ (1, 0) ])
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            None
            None

    Assert.DoesNotContain("rank=same", dot)

[<Fact>]
let ``toDotFromSets renders the current vertex even when absent from all active sets`` () =
    let dot =
        GssDot.toDotFromSets
            (fun idx -> sprintf "%d" idx)
            (fun _ -> "e")
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            (Some 5)
            None

    Assert.Contains("v5 [", dot)
    Assert.Contains("fillcolor=lightblue", dot)

[<Fact>]
let ``toDotFromSets renders path edges light red and highlighted edges red bold`` () =
    let dot =
        GssDot.toDotFromSets
            (fun idx -> sprintf "%d" idx)
            (fun _ -> "e")
            (set [ 0; 1; 2 ])
            (set [ (0, 1); (1, 2); (0, 2) ])
            Set.empty
            (set [ (1, 2); (0, 2) ])
            (set [ (0, 1); (0, 2) ])
            Set.empty
            None
            None

    // The hex color is quoted: an unquoted # would start a DOT comment.
    Assert.Contains("v0 -> v1 [label=\"e\", color=\"#FF9999\"];", dot)
    Assert.Contains("v1 -> v2 [label=\"e\", color=red, penwidth=2.0];", dot)
    // An edge in both sets renders as highlighted.
    Assert.Contains("v0 -> v2 [label=\"e\", color=red, penwidth=2.0];", dot)


module GssTikzTests =

    [<Fact>]
    let ``toTikzFromSets renders the current vertex even when absent from all active sets`` () =
        let tikz =
            GssTikz.toTikzFromSets
                (fun idx -> sprintf "%d" idx)
                (fun _ -> "e")
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                (Some 3)
                "circle"
                false
                None
                false

        Assert.Contains("v3 [as={3}, fill=lightblue!20];", tikz)

    [<Fact>]
    let ``toTikzFromSets edge with empty label renders a bare edge and a bare loop`` () =
        let tikz =
            GssTikz.toTikzFromSets
                (fun idx -> sprintf "%d" idx)
                (fun _ -> "")
                (set [ 0; 1 ])
                (set [ (0, 1); (0, 0) ])
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                None
                "circle"
                false
                None
                false

        Assert.Contains("v0 -> v1;", tikz)
        Assert.Contains("v0 ->[loop above] v0;", tikz)

    [<Fact>]
    let ``toTikzFromSets bends both edges of a reciprocal pair when requested`` () =
        let tikz =
            GssTikz.toTikzFromSets
                (fun idx -> sprintf "%d" idx)
                (fun _ -> "")
                (set [ 0; 1 ])
                (set [ (0, 1); (1, 0) ])
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                None
                "circle"
                false
                None
                true

        Assert.Contains("v0 ->[bend left=15] v1;", tikz)
        Assert.Contains("v1 ->[bend left=15] v0;", tikz)

    [<Fact>]
    let ``toTikzFromSets leaves reciprocal pairs straight when not requested`` () =
        let tikz =
            GssTikz.toTikzFromSets
                (fun idx -> sprintf "%d" idx)
                (fun _ -> "")
                (set [ 0; 1 ])
                (set [ (0, 1); (1, 0) ])
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                None
                "circle"
                false
                None
                false

        Assert.Contains("v0 -> v1;", tikz)
        Assert.Contains("v1 -> v0;", tikz)
        Assert.DoesNotContain("bend", tikz)

    [<Fact>]
    let ``toTikzFromSets bends labeled and highlighted reciprocal edges but not one-way edges`` () =
        let tikz =
            GssTikz.toTikzFromSets
                (fun idx -> sprintf "%d" idx)
                (fun _ -> "e")
                (set [ 0; 1; 2 ])
                (set [ (0, 1); (1, 0); (0, 2) ])
                Set.empty
                (set [ (1, 0) ])
                Set.empty
                Set.empty
                None
                "circle"
                false
                None
                true

        Assert.Contains("v0 ->[\"e\", bend left=15] v1;", tikz)
        Assert.Contains("v1 ->[\"e\", red, thick, bend left=15] v0;", tikz)
        Assert.Contains("v0 ->[\"e\"] v2;", tikz)

    [<Fact>]
    let ``toTikzFromSets renders path edges light red and highlighted edges red bold`` () =
        let tikz =
            GssTikz.toTikzFromSets
                (fun idx -> sprintf "%d" idx)
                (fun _ -> "e")
                (set [ 0; 1; 2 ])
                (set [ (0, 1); (1, 2); (0, 2) ])
                Set.empty
                (set [ (1, 2); (0, 2) ])
                (set [ (0, 1); (0, 2) ])
                Set.empty
                None
                "circle"
                false
                None
                false

        Assert.Contains("v0 ->[\"e\", red!40] v1;", tikz)
        Assert.Contains("v1 ->[\"e\", red, thick] v2;", tikz)
        // An edge in both sets renders as highlighted.
        Assert.Contains("v0 ->[\"e\", red, thick] v2;", tikz)
