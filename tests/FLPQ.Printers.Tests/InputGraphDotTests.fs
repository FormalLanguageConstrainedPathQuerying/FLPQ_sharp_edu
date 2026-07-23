module InputGraphDotTests

open System
open System.Text.RegularExpressions
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers

let private verticesPattern = Regex(@"v(\d+) \[label=""(\d+)""")

let private parseDotVertices (dot: string) : (int * int) list =
    [ for m in verticesPattern.Matches(dot) do
          let idx = Int32.Parse(m.Groups.[1].Value)
          let label = Int32.Parse(m.Groups.[2].Value)
          yield (idx, label) ]

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``input graph DOT compiles for simple string`` () =
    let input = [ "a"; "b"; "c" ]
    let graph = GLL.stringToGraph input
    let dot = InputGraphDot.toDot string graph None
    Assert.True(ExternalTools.compileDotString dot)

[<Fact>]
let ``input graph DOT has correct vertex count`` () =
    let input = [ "a"; "b"; "c"; "d" ]
    let graph = GLL.stringToGraph input
    let dot = InputGraphDot.toDot string graph None
    let vertices = parseDotVertices dot
    Assert.Equal(5, vertices.Length)

[<Fact>]
let ``input graph DOT has correct vertex labels`` () =
    let input = [ "a"; "b"; "c" ]
    let graph = GLL.stringToGraph input
    let dot = InputGraphDot.toDot string graph None
    let vertices = parseDotVertices dot |> List.sortBy fst
    let expected = [ (0, 0); (1, 1); (2, 2); (3, 3) ]
    Assert.Equal<int * int>(expected, vertices)

[<Fact>]
let ``input graph DOT highlights current vertex`` () =
    let input = [ "a"; "b"; "c" ]
    let graph = GLL.stringToGraph input
    let dot = InputGraphDot.toDot string graph (Some 1)
    Assert.Contains("fillcolor=\"green!30\"", dot)
    Assert.Contains("v1 [label=\"1\", shape=circle, style=filled, fillcolor=\"green!30\"]", dot)

[<Fact>]
let ``input graph DOT has edge labels for all terminals`` () =
    let input = [ "x"; "y"; "z" ]
    let graph = GLL.stringToGraph input
    let dot = InputGraphDot.toDot string graph None

    Assert.Contains("v0 -> v1 [label=\"x\"]", dot)
    Assert.Contains("v1 -> v2 [label=\"y\"]", dot)
    Assert.Contains("v2 -> v3 [label=\"z\"]", dot)

[<Fact>]
let ``input graph DOT for empty input has only one vertex`` () =
    let input: string list = []
    let graph = GLL.stringToGraph input
    let dot = InputGraphDot.toDot string graph None
    let vertices = parseDotVertices dot
    Assert.Equal(1, vertices.Length)
    Assert.Equal(0, fst vertices.[0])
