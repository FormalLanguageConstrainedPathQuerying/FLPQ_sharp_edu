module ArroyueloStepVisualizationTests

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

let private steps, finalMatrix = ArroyueloRPQ.evaluateWithTrace testGraph testRegexp

let private rendered = ArroyueloStepVisualizer.renderSteps string testGraph steps

// --- Golden tests (TikZ mode) ---

module ArroyueloStepGoldenTests =

    [<Fact>]
    let ``all steps: tree tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "arroyuelo_step_%d_tree.tikz" i) rendered.[i].TreeTikz

    [<Fact>]
    let ``all steps: matrices goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "arroyuelo_step_%d_matrices.tex" i) rendered.[i].Matrices

    [<Fact>]
    let ``all steps: graph tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "arroyuelo_step_%d_graph.tikz" i) rendered.[i].GraphTikz

// --- Structural facts ---

[<Fact>]
let ``tree DOT of each step highlights exactly the current node`` () =
    for i in 0 .. rendered.Length - 1 do
        let lightblueCount = countOccurrences rendered.[i].TreeDot "fillcolor=lightblue"
        let isSingle = lightblueCount = 1
        Assert.True(isSingle, sprintf "step %d: expected 1 lightblue node, got %d" i lightblueCount)

[<Fact>]
let ``tree DOT of step k marks node k as current`` () =
    for i in 0 .. rendered.Length - 1 do
        let dot = rendered.[i].TreeDot

        let currentLine =
            dot.Split('\n') |> Array.filter (fun l -> l.StartsWith(sprintf "  v%d [" i))

        let _line = Assert.Single currentLine
        Assert.Contains("fillcolor=lightblue", currentLine.[0])

[<Fact>]
let ``matrices TeX has the right number of pNiceMatrix blocks per operation`` () =
    let expectedBlocks (op: ArroyueloRPQ.ArroyueloOperation) =
        match op with
        | ArroyueloRPQ.Base -> 1
        | ArroyueloRPQ.Alt -> 3
        | ArroyueloRPQ.Seq -> 3
        | ArroyueloRPQ.Star -> 2

    for i in 0 .. rendered.Length - 1 do
        let blocks = countOccurrences rendered.[i].Matrices @"\begin{pNiceMatrix}"
        Assert.Equal(expectedBlocks steps.[i].Operation, blocks)

[<Fact>]
let ``graph DOT highlights exactly the edges of the step's result paths`` () =
    for i in 0 .. rendered.Length - 1 do
        let expectedEdges =
            PathSemiring.allPaths steps.[i].Result
            |> Set.fold
                (fun acc p ->
                    p
                    |> List.windowed 2
                    |> List.map (fun w -> (w.[0], w.[1]))
                    |> List.fold (fun a e -> Set.add e a) acc)
                Set.empty

        let dot = rendered.[i].GraphDot

        for fromV, toV in expectedEdges do
            let pattern = sprintf "v%d -> v%d [label=" fromV toV

            let redLines =
                dot.Split('\n')
                |> Array.filter (fun l -> l.TrimStart().StartsWith(pattern) && l.Contains("color=red"))

            let hasRed = redLines.Length >= 1
            Assert.True(hasRed, sprintf "step %d: edge (%d,%d) not highlighted red" i fromV toV)

        let redCount = countOccurrences dot "color=red"
        Assert.Equal(Set.count expectedEdges, redCount)

[<Fact>]
let ``graph DOT highlights exactly the vertices of the step's result paths`` () =
    for i in 0 .. rendered.Length - 1 do
        let expectedVerts =
            PathSemiring.allPaths steps.[i].Result
            |> Set.fold (fun acc p -> p |> List.fold (fun a v -> Set.add v a) acc) Set.empty

        let dot = rendered.[i].GraphDot
        let yellowCount = countOccurrences dot "fillcolor=lightyellow"
        Assert.Equal(Set.count expectedVerts, yellowCount)

// --- Template fill ---

let private summaryTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

let private fillStepTemplate (template: string) (step: ArroyueloStepVisualizer.ArroyueloVisualizationStep) : string =
    template
        .Replace("__STEP_TREE_TIKZ__", step.TreeTikz)
        .Replace("__MATRICES__", step.Matrices)
        .Replace("__STEP_GRAPH_TIKZ__", step.GraphTikz)

[<Fact>]
let ``filled tikz step template has no leftover placeholders and wraps matrices in adjustbox`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Arroyuelo_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let filled = fillStepTemplate template rendered.[0]

    Assert.DoesNotContain("__", filled)
    Assert.Contains("adjustbox", filled)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``filled tikz step template compiles with lualatex`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Arroyuelo_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    for i in 0 .. rendered.Length - 1 do
        let step = rendered.[i]

        let filled = fillStepTemplate template step

        let document =
            summaryTemplate.Replace("__ALGORITHM__", "Arroyuelo RPQ").Replace("__CONTENT__", filled)

        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory tempDir |> ignore
        let texFile = Path.Combine(tempDir, sprintf "arroyuelo_step_%d.tex" i)
        File.WriteAllText(texFile, document)

        try
            Assert.True(ExternalTools.compileTexFile texFile tempDir)
        finally
            Directory.Delete(tempDir, true)
