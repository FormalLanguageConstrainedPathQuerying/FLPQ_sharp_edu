module RpqGraphVizTests

open Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

/// The shared RPQ example (see TestHelpers.rpqExampleGraph).
let private exampleGraph: NFA<string, int> = TestHelpers.rpqExampleGraph

[<Fact>]
let ``pathHighlights: two paths yield their vertices and edges`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if i = 0 && j = 2 then
                Set.singleton [ 0; 1; 2 ]
            else
                Set.empty)

    let verts, edges = RpqGraphViz.pathHighlights m

    Assert.True(Set.ofList [ 0; 1; 2 ] = verts)
    Assert.True(Set.ofList [ (0, 1); (1, 2) ] = edges)

[<Fact>]
let ``pathHighlights: multiple paths in one cell are unioned`` () =
    let m =
        Matrix.create 2 2 (fun i j ->
            if i = 0 && j = 1 then
                Set.ofList [ [ 0; 1 ]; [ 0; 5 ] ]
            else
                Set.empty)

    let verts, edges = RpqGraphViz.pathHighlights m

    Assert.True(Set.ofList [ 0; 1; 5 ] = verts)
    Assert.True(Set.ofList [ (0, 1); (0, 5) ] = edges)

[<Fact>]
let ``pathHighlights: empty matrix yields empty sets`` () =
    let m = Matrix.init 2 2 Set.empty
    let verts, edges = RpqGraphViz.pathHighlights m

    Assert.True(Set.isEmpty verts)
    Assert.True(Set.isEmpty edges)

[<Fact>]
let ``pathEdges: only the consecutive pairs of stored paths`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if i = 0 && j = 2 then
                Set.ofList [ [ 0; 1; 2 ]; [ 5 ] ]
            else
                Set.empty)

    Assert.True(Set.ofList [ (0, 1); (1, 2) ] = RpqGraphViz.pathEdges m)

[<Fact>]
let ``pathLastEdges: the final edge of every non-trivial path`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if i = 0 && j = 2 then
                Set.ofList [ [ 0; 1; 2 ]; [ 5 ] ]
            else
                Set.empty)

    // The trivial path [5] contributes no edge.
    Assert.True(Set.singleton (1, 2) = RpqGraphViz.pathLastEdges m)

[<Fact>]
let ``edgeEndpoints: both endpoints of every edge`` () =
    Assert.True(Set.ofList [ 0; 1; 2 ] = RpqGraphViz.edgeEndpoints (Set.ofList [ (0, 1); (1, 2) ]))
    Assert.True(Set.isEmpty (RpqGraphViz.edgeEndpoints Set.empty))

[<Fact>]
let ``frontierVertices: the last vertex of every non-empty path`` () =
    let m =
        Matrix.create 3 3 (fun i j ->
            if i = 0 && j = 2 then
                Set.ofList [ [ 0; 1; 2 ] ]
            elif i = 1 && j = 1 then
                // A trivial path [5] marks vertex 5 as a frontier position.
                Set.singleton [ 5 ]
            else
                Set.empty)

    Assert.True(Set.ofList [ 2; 5 ] = RpqGraphViz.frontierVertices m)

[<Fact>]
let ``frontierVertices: empty matrix yields the empty set`` () =
    let m = Matrix.init 2 2 Set.empty

    Assert.True(Set.isEmpty (RpqGraphViz.frontierVertices m))

[<Fact>]
let ``renderGraph: start vertices render green and frontier vertices lightblue in both formats`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph id exampleGraph Set.empty (Set.ofList [ 0 ]) (Set.ofList [ 1; 2 ]) Set.empty Set.empty

    Assert.Contains("fillcolor=green", dot)
    Assert.Contains("fillcolor=lightblue", dot)
    Assert.Contains("fill=green!30", tikz)
    Assert.Contains("fill=lightblue!20", tikz)

[<Fact>]
let ``renderGraph: empty start and frontier sets leave the plain graph unchanged`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph id exampleGraph Set.empty Set.empty Set.empty Set.empty Set.empty

    Assert.DoesNotContain("green", dot)
    Assert.DoesNotContain("lightblue", dot)
    Assert.DoesNotContain("green", tikz)
    Assert.DoesNotContain("lightblue", tikz)

[<Fact>]
let ``renderGraph: plain graph has no highlights in either format`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph id exampleGraph Set.empty Set.empty Set.empty Set.empty Set.empty

    Assert.Contains("digraph", dot)
    Assert.Contains("v_0", dot)
    Assert.Contains("a", dot)
    Assert.Contains("\\begin{tikzpicture}", tikz)
    Assert.DoesNotContain("lightblue", dot)
    Assert.DoesNotContain("yellow", dot)

[<Fact>]
let ``renderGraph: highlighted vertices and edges appear in both formats`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph
            id
            exampleGraph
            (Set.ofList [ 1; 2 ])
            Set.empty
            Set.empty
            (Set.ofList [ (1, 2) ])
            Set.empty

    Assert.Contains("v_1", dot)
    Assert.Contains("v_2", dot)
    // The highlighted vertex style differs from the plain one in both formats.
    let plainDot, _ =
        RpqGraphViz.renderGraph id exampleGraph Set.empty Set.empty Set.empty Set.empty Set.empty

    Assert.NotEqual<string>(plainDot, dot)

    let _, plainTikz =
        RpqGraphViz.renderGraph id exampleGraph Set.empty Set.empty Set.empty Set.empty Set.empty

    Assert.NotEqual<string>(plainTikz, tikz)

[<Fact>]
let ``renderGraph: path edges render light red and current edges red bold in both formats`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph
            id
            exampleGraph
            Set.empty
            Set.empty
            Set.empty
            (Set.ofList [ (1, 2) ])
            (Set.ofList [ (0, 1) ])

    // The hex color is quoted: an unquoted # would start a DOT comment.
    Assert.Contains("v0 -> v1 [label=\"a\", color=\"#FF9999\"];", dot)
    Assert.Contains("v1 -> v2 [label=\"b\", color=red, penwidth=2.0];", dot)
    Assert.Contains("v0 ->[\"a\", red!40] v1;", tikz)
    Assert.Contains("v1 ->[\"b\", red, thick] v2;", tikz)

[<Fact>]
let ``renderGraph: reciprocal edges are bent in TikZ but not in DOT`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph id exampleGraph Set.empty Set.empty Set.empty Set.empty Set.empty

    // The example graph carries both 2->3 ("c") and 3->2 ("b").
    Assert.Contains("v2 ->[\"c\", bend left=15] v3;", tikz)
    Assert.Contains("v3 ->[\"b\", bend left=15] v2;", tikz)
    // One-way edges stay straight.
    Assert.Contains("v0 ->[\"a\"] v1;", tikz)
    // Graphviz separates reciprocal pairs on its own; no bend attribute in DOT.
    Assert.DoesNotContain("bend", dot)

[<Fact>]
let ``renderGraph: a highlighted reciprocal edge keeps its bend`` () =
    let _, tikz =
        RpqGraphViz.renderGraph id exampleGraph Set.empty Set.empty Set.empty (Set.ofList [ (2, 3) ]) Set.empty

    Assert.Contains("v2 ->[\"c\", red, thick, bend left=15] v3;", tikz)
    Assert.Contains("v3 ->[\"b\", bend left=15] v2;", tikz)
