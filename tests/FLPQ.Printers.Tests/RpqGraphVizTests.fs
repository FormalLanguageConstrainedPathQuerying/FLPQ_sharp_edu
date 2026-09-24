module RpqGraphVizTests

open Xunit
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

let private exampleGraph: NFA<string, int> =
    TestHelpers.nfaFromEdges
        6
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "b"; To = 2 }
          { From = 2; Label = "c"; To = 3 }
          { From = 3; Label = "b"; To = 2 }
          { From = 3; Label = "a"; To = 4 }
          { From = 2; Label = "a"; To = 5 } ]
        [| 0 |]

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
let ``renderGraph: plain graph has no highlights in either format`` () =
    let dot, tikz = RpqGraphViz.renderGraph string exampleGraph Set.empty Set.empty

    Assert.Contains("digraph", dot)
    Assert.Contains("v_0", dot)
    Assert.Contains("a", dot)
    Assert.Contains("\\begin{tikzpicture}", tikz)
    Assert.DoesNotContain("lightblue", dot)
    Assert.DoesNotContain("yellow", dot)

[<Fact>]
let ``renderGraph: highlighted vertices and edges appear in both formats`` () =
    let dot, tikz =
        RpqGraphViz.renderGraph string exampleGraph (Set.ofList [ 1; 2 ]) (Set.ofList [ (1, 2) ])

    Assert.Contains("v_1", dot)
    Assert.Contains("v_2", dot)
    // The highlighted vertex style differs from the plain one in both formats.
    let plainDot, _ = RpqGraphViz.renderGraph string exampleGraph Set.empty Set.empty
    Assert.NotEqual<string>(plainDot, dot)
    let _, plainTikz = RpqGraphViz.renderGraph string exampleGraph Set.empty Set.empty
    Assert.NotEqual<string>(plainTikz, tikz)
