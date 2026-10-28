module InputGraphTikzTests

open Xunit
open FLPQ.GraphAnalysis
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

let private tikzTemplatePath =
    System.IO.Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")

[<Fact>]
let ``input graph tikz renders the linear path without bends`` () =
    let graph = GLL.stringToGraph [ "a"; "b" ]
    let tikz = InputGraphTikz.toTikz string graph None

    // The labels are registered in the measurement pre-pass; the edges carry no stock quote.
    Assert.Contains("v0 -> v1;", tikz)
    Assert.Contains("v1 -> v2;", tikz)
    Assert.Contains(@"\flpqdotmeasureedgelabel{v0->v1}{a}", tikz)
    Assert.DoesNotContain("bend", tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``input graph tikz renders reciprocal edges without a bend`` () =
    // The renderer is generic over Graph<int, Option<'t>>; a reciprocal pair is drawn as two
    // plain edges (separation is done natively by the flpqdot router, not the renderer).
    let edges = Matrix.init 2 2 None
    edges.[0, 1] <- Some "a"
    edges.[1, 0] <- Some "a"
    let graph = Graph.fromEdges [ 0; 1 ] edges

    let tikz = InputGraphTikz.toTikz string graph None

    Assert.Contains("v0 -> v1;", tikz)
    Assert.Contains("v1 -> v0;", tikz)
    Assert.DoesNotContain("bend", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)
