module RpqGraphRendererTests

open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FlpqdotProbeHelpers

/// A two-vertex graph with one edge: 0 --a--> 1.
let private smallGraph: NFA<string, int> =
    Nfa.fromTransitions [ 0; 1 ] [ { From = 0; Label = "a"; To = 1 } ] Set.empty (set [ 0 ]) Set.empty

let private vertices = Set.ofList [ 0; 1 ]
let private edges = Set.singleton (0, 1)

// --- DOT ---

[<Fact>]
let ``RpqGraphDot: plain graph has no fills`` () =
    let actual =
        RpqGraphDot.toDot
            (sprintf "v_%d")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty

    let expected =
        "digraph GSS {\n  rankdir=LR;\n  compound=true;\n  v0 [label=\"v_0\", shape=ellipse];\n  v1 [label=\"v_1\", shape=ellipse];\n  v0 -> v1 [label=\"a\"];\n}\n"

    Assert.Equal(expected, actual)

[<Fact>]
let ``RpqGraphDot: fill precedence is highlighted over frontier over start`` () =
    let actual =
        RpqGraphDot.toDot
            (sprintf "v_%d")
            (fun _ -> "a")
            vertices
            edges
            (Set.singleton 1)
            (Set.singleton 0)
            (Set.singleton 0)
            Set.empty
            Set.empty

    Assert.Contains("v0 [label=\"v_0\", shape=ellipse, style=filled, fillcolor=lightblue];", actual)
    Assert.Contains("v1 [label=\"v_1\", shape=ellipse, style=filled, fillcolor=lightyellow];", actual)

[<Fact>]
let ``RpqGraphDot: current edges are red bold and path edges light red`` () =
    let current =
        RpqGraphDot.toDot
            (sprintf "v_%d")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            (Set.singleton (0, 1))
            Set.empty

    Assert.Contains("v0 -> v1 [label=\"a\", color=red, penwidth=2.0];", current)

    let path =
        RpqGraphDot.toDot
            (sprintf "v_%d")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            (Set.singleton (0, 1))

    Assert.Contains("v0 -> v1 [label=\"a\", color=\"#FF9999\"];", path)

// --- TikZ ---

[<Fact>]
let ``RpqGraphTikz: plain graph renders the dot-layout header and a bare edge`` () =
    let actual =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty

    let expected =
        "\\begin{tikzpicture}\n  \\flpqdotmeasureedgelabel{v0->v1}{a}\n  \\graph [dot layout, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {\n    v0 [as={$v_0$}];\n    v1 [as={$v_1$}];\n    v0 -> v1;\n  };\n\\end{tikzpicture}\n"

    Assert.Equal(expected, actual)

[<Fact>]
let ``RpqGraphTikz: fill precedence is highlighted over frontier over start`` () =
    let actual =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "a")
            vertices
            edges
            (Set.singleton 1)
            (Set.singleton 0)
            (Set.singleton 0)
            Set.empty
            Set.empty

    Assert.Contains("v0 [as={$v_0$}, fill=lightblue!20];", actual)
    Assert.Contains("v1 [as={$v_1$}, fill=yellow!20];", actual)

[<Fact>]
let ``RpqGraphTikz: current edges are red bold and path edges light red`` () =
    let current =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            (Set.singleton (0, 1))
            Set.empty

    Assert.Contains("v0 ->[red, thick] v1;", current)

    let path =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            (Set.singleton (0, 1))

    Assert.Contains("v0 ->[red!40] v1;", path)

[<Fact>]
let ``RpqGraphTikz: reciprocal edges are not bent`` () =
    let graph: NFA<string, int> =
        Nfa.fromTransitions
            [ 0; 1 ]
            [ { From = 0; Label = "a"; To = 1 }; { From = 1; Label = "b"; To = 0 } ]
            Set.empty
            (set [ 0 ])
            Set.empty

    let edgeLabel =
        function
        | (0, 1) -> "a"
        | (1, 0) -> "b"
        | _ -> ""

    let actual =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            edgeLabel
            (Set.ofList [ 0; 1 ])
            (Set.ofList [ (0, 1); (1, 0) ])
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty

    Assert.Contains("v0 -> v1;", actual)
    Assert.Contains("v1 -> v0;", actual)
    Assert.DoesNotContain("bend", actual)

[<Fact>]
let ``RpqGraphDot: start vertex is green when not highlighted or frontier`` () =
    let actual =
        RpqGraphDot.toDot
            (sprintf "v_%d")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            (Set.singleton 0)
            Set.empty
            Set.empty
            Set.empty

    Assert.Contains("v0 [label=\"v_0\", shape=ellipse, style=filled, fillcolor=green];", actual)

[<Fact>]
let ``RpqGraphDot: an edge endpoint absent from the active set is still rendered`` () =
    let actual =
        RpqGraphDot.toDot
            (sprintf "v_%d")
            (fun _ -> "a")
            (Set.singleton 0)
            edges
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty

    Assert.Contains("v1 [label=\"v_1\", shape=ellipse];", actual)

[<Fact>]
let ``RpqGraphTikz: start vertex is green when not highlighted or frontier`` () =
    let actual =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            (Set.singleton 0)
            Set.empty
            Set.empty
            Set.empty

    Assert.Contains("v0 [as={$v_0$}, fill=green!30];", actual)

[<Fact>]
let ``RpqGraphTikz: empty-label edges render bare and self edges as loops`` () =
    let loop =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "")
            (Set.singleton 0)
            (Set.singleton (0, 0))
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty

    Assert.Contains("v0 ->[loop above] v0;", loop)

    let bare =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            Set.empty
            Set.empty

    Assert.Contains("v0 -> v1;", bare)

[<Fact>]
let ``RpqGraphTikz: a highlighted edge also in the path set renders highlighted`` () =
    let actual =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            (fun _ -> "a")
            vertices
            edges
            Set.empty
            Set.empty
            Set.empty
            (Set.singleton (0, 1))
            (Set.singleton (0, 1))

    Assert.Contains("v0 ->[red, thick] v1;", actual)

[<Fact>]
let ``RpqGraphViz: renderGraph delegates to the RPQ renderers`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph
            id
            smallGraph
            (Set.singleton 1)
            (Set.singleton 0)
            Set.empty
            (Set.singleton (0, 1))
            Set.empty
            Set.empty
            Map.empty

    Assert.Contains("fillcolor=lightyellow", dot)
    Assert.Contains("color=red, penwidth=2.0", dot)
    Assert.Contains("fill=yellow!20", tikz)
    Assert.Contains("red, thick", tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``RpqGraphTikz: the Kronecker step-5 graph is label-clean`` () =
    // The real Kronecker-CFPQ summary step-5 graph (4 nodes, 8 edges, the v0->v3
    // edge highlighted): the reported overlap repro, now rendered through the
    // adopted measurement pre-pass.
    let edgeLabel =
        function
        | (0, 1) -> "a"
        | (0, 2) -> "S"
        | (0, 3) -> "S"
        | (1, 2) -> "a, S"
        | (1, 3) -> "S"
        | (2, 0) -> "a"
        | (2, 3) -> "b, S"
        | (3, 2) -> "b"
        | _ -> ""

    let step5 =
        RpqGraphTikz.toTikz
            (sprintf "$v_%d$")
            edgeLabel
            (Set.ofList [ 0; 1; 2; 3 ])
            (Set.ofList [ (0, 1); (0, 2); (0, 3); (1, 2); (1, 3); (2, 0); (2, 3); (3, 2) ])
            Set.empty
            Set.empty
            Set.empty
            (Set.singleton (0, 3))
            Set.empty

    let log = compileProbeLog step5

    // Every non-loop edge (all 8) is assigned a label position.
    let placed = probeInt "LABEL_PLACED_COUNT=" log
    Assert.Equal(8, placed)

    // No label overlaps another label, a node, or another edge's path.
    let labelOverlaps = probeInt "LABEL_OVERLAP_COUNT=" log
    let nodeColl = probeInt "LABEL_NODE_COLLISIONS=" log
    let edgeColl = probeInt "LABEL_EDGE_COLLISIONS=" log
    Assert.Equal(0, labelOverlaps)
    Assert.Equal(0, nodeColl)
    Assert.Equal(0, edgeColl)

    // No edge path is degenerate (self-intersecting).
    let selfInt = probeInt "SELF_INTERSECTION_COUNT=" log
    Assert.Equal(0, selfInt)
