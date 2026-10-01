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

    Assert.Contains("v0 ->[\"a\"] v1;", tikz)
    Assert.Contains("v1 ->[\"b\"] v2;", tikz)
    Assert.DoesNotContain("bend", tikz)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``input graph tikz bends reciprocal edges`` () =
    // The renderer is generic over Graph<int, Option<'t>>; a reciprocal pair must not draw as
    // two fully overlapping straight lines (task 289).
    let edges = Matrix.init 2 2 None
    edges.[0, 1] <- Some "a"
    edges.[1, 0] <- Some "a"
    let graph = Graph.fromEdges [ 0; 1 ] edges

    let tikz = InputGraphTikz.toTikz string graph None

    Assert.Contains("v0 ->[\"a\", bend left=15] v1;", tikz)
    Assert.Contains("v1 ->[\"a\", bend left=15] v0;", tikz)
    Assert.True(ExternalTools.compileTexStringWithTemplate tikzTemplatePath tikz)
