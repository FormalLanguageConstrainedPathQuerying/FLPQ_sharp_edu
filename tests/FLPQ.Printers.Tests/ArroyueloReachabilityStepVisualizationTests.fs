module ArroyueloReachabilityStepVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.RPQ
open FLPQ.TestUtilities

open GoldenHelpers

let private testGraph: NFA<string, int> =
    Nfa.fromTransitions
        [ 0; 1; 2; 3 ]
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "b"; To = 2 }
          { From = 2; Label = "a"; To = 3 }
          { From = 1; Label = "a"; To = 3 } ]
        Set.empty
        (set [ 0 ])
        Set.empty

let private testRegexp: Regexp<string, string> =
    RSeq(RTerm(Terminal "a"), RStar(RAlt(RTerm(Terminal "b"), RTerm(Terminal "a"))))

let private steps, fullMatrix =
    ArroyueloRPQ.evaluateBooleanWithTrace testGraph testRegexp

let private intermediate =
    steps |> List.filter (fun s -> s.Operation <> ArroyueloRPQ.Base)

let private rendered =
    ArroyueloReachabilityStepVisualizer.renderSteps id id testGraph steps

/// Every true cell (u, v) of a Boolean result matrix.
let private derivedEdges (m: Matrix<bool>) : Set<int * int> =
    Set.ofList
        [ for u in 0 .. Matrix.rows m - 1 do
              for v in 0 .. Matrix.cols m - 1 do
                  if m.[u, v] then
                      (u, v) ]

// --- Structural facts ---

[<Fact>]
let ``only intermediate nodes are emitted as reachability steps`` () =
    Assert.Equal(3, rendered.Length)
    Assert.All(intermediate, (fun s -> Assert.NotEqual(ArroyueloRPQ.Base, s.Operation)))

[<Fact>]
let ``tree is vertical and includes every node`` () =
    for i in 0 .. rendered.Length - 1 do
        Assert.Contains("rankdir=TB;", rendered.[i].TreeDot)
        Assert.Contains("grow'=down", rendered.[i].TreeTikz)

        for node in 0 .. steps.Length - 1 do
            Assert.Contains(sprintf "v%d [" node, rendered.[i].TreeTikz)

[<Fact>]
let ``matrices are Boolean (bullet and cdot), not path tuples`` () =
    for i in 0 .. rendered.Length - 1 do
        Assert.Contains(@"\bullet", rendered.[i].Matrices)
        Assert.DoesNotContain("v_0, v_1", rendered.[i].Matrices)

[<Fact>]
let ``graph adds one red derived edge per true result cell and no path edges`` () =
    for i in 0 .. rendered.Length - 1 do
        let expected = derivedEdges intermediate.[i].Result
        let dot = rendered.[i].GraphDot

        Assert.Equal(Set.count expected, countOccurrences dot "color=red")
        Assert.Equal(Set.count expected, countOccurrences rendered.[i].GraphTikz "red, thick")

        // No path (light red) highlighting in the reachability semantics.
        Assert.DoesNotContain("red!40", rendered.[i].GraphTikz)
        Assert.DoesNotContain("#FF9999", dot)

[<Fact>]
let ``derived edges are labeled with the handled node's subexpression`` () =
    // Step 0 is the Alt(b, a) node; its DOT label for the derived relation carries the
    // plain-text subexpression.
    Assert.Contains("b | a", rendered.[0].GraphDot)

// --- Template fill ---

let private summaryTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

let private fillStepTemplate (template: string) (step: ArroyueloStepCommon.ArroyueloVisualizationStep) : string =
    template
        .Replace("__STEP_TREE_TIKZ__", SummaryTeX.wrapTikzAdjustboxColumnTop step.TreeTikz)
        .Replace("__MATRICES__", step.Matrices)
        .Replace("__STEP_GRAPH_TIKZ__", SummaryTeX.wrapTikzAdjustboxColumnTop step.GraphTikz)

[<Fact>]
let ``filled tikz reachability step template has no leftover placeholders`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Arroyuelo_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let filled = fillStepTemplate template rendered.[0]

    Assert.DoesNotContain("__", filled)
    Assert.Contains("adjustbox", filled)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``filled tikz reachability step template compiles with lualatex`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Arroyuelo_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    for i in 0 .. rendered.Length - 1 do
        let filled = fillStepTemplate template rendered.[i]

        let document =
            summaryTemplate.Replace("__ALGORITHM__", "Arroyuelo RPQ").Replace("__CONTENT__", filled)

        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory tempDir |> ignore
        let texFile = Path.Combine(tempDir, sprintf "arroyuelo_reach_step_%d.tex" i)
        File.WriteAllText(texFile, document)

        try
            Assert.True(ExternalTools.compileTexFile texFile tempDir, sprintf "step %d: lualatex failed" i)
        finally
            Directory.Delete(tempDir, true)
