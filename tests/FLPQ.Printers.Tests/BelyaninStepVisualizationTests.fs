module BelyaninStepVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.RPQ
open FLPQ.TestUtilities

open GoldenHelpers

/// The example from the detailed plan: regexp a / (b | c)* / a on the graph
/// 0 a 1, 1 b 2, 2 c 3, 3 b 2, 3 a 4, 2 a 5 with source v_0.
let private testGraph: NFA<string, int> =
    TestHelpers.nfaFromEdges
        6
        [ { From = 0; Label = "a"; To = 1 }
          { From = 1; Label = "b"; To = 2 }
          { From = 2; Label = "c"; To = 3 }
          { From = 3; Label = "b"; To = 2 }
          { From = 3; Label = "a"; To = 4 }
          { From = 2; Label = "a"; To = 5 } ]
        [| 0 |]

let private testRegexp: Regexp<string, string> =
    RSeq(RTerm(Terminal "a"), RSeq(RStar(RAlt(RTerm(Terminal "b"), RTerm(Terminal "c"))), RTerm(Terminal "a")))

let private testDfa: DFA<string, int> = Regexp.toDfa testRegexp

let private steps, _ = BelyaninRPQ.evaluateWithTrace testDfa testGraph

let private rendered =
    BelyaninStepVisualizer.renderSteps string testDfa testGraph steps

/// The automaton states of the step's frontier: q with a non-empty M[q, *] cell.
let private frontierStates (m: Matrix<Set<int list>>) : Set<int> =
    let rows = Matrix.rows m
    let cols = Matrix.cols m

    Set.ofList
        [ for q in 0 .. rows - 1 do
              if
                  [ for v in 0 .. cols - 1 do
                        not (PathSemiring.isZero m.[q, v]) ]
                  |> List.exists id
              then
                  q ]

// --- Golden tests (TikZ mode) ---

module BelyaninStepGoldenTests =

    [<Fact>]
    let ``all steps: automaton tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_automaton.tikz" i) rendered.[i].AutomatonTikz

    [<Fact>]
    let ``all steps: matrices goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_matrices.tex" i) rendered.[i].Matrices

    [<Fact>]
    let ``all steps: graph tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_graph.tikz" i) rendered.[i].GraphTikz

// --- Structural facts ---

[<Fact>]
let ``automaton DOT of each step highlights exactly the frontier states`` () =
    for i in 0 .. rendered.Length - 1 do
        let expected = frontierStates steps.[i].M
        let dot = rendered.[i].AutomatonDot
        let lightblueCount = countOccurrences dot "fillcolor=lightblue"

        Assert.Equal(Set.count expected, lightblueCount)

        for q in expected do
            // DOT node ids are s<idx>; the state visualizer sets the label attribute.
            let lines =
                dot.Split('\n') |> Array.filter (fun l -> l.StartsWith(sprintf "  s%d [" q))

            ignore (Assert.Single lines)
            Assert.Contains("fillcolor=lightblue", lines.[0])

[<Fact>]
let ``initialization step highlights only the start state`` () =
    let dot = rendered.[0].AutomatonDot

    let lightblueLines =
        dot.Split('\n') |> Array.filter (fun l -> l.Contains("fillcolor=lightblue"))

    Assert.Equal(1, lightblueLines.Length)
    Assert.Contains(sprintf "s%d [" testDfa.StartState, lightblueLines.[0])

[<Fact>]
let ``matrices TeX has the right number of pNiceMatrix blocks per step`` () =
    let expectedBlocks (step: BelyaninRPQ.BelyaninTraceStep<string>) =
        if step.IsInit then
            2
        else
            2 + 2 * List.length step.Labels + 1

    for i in 0 .. rendered.Length - 1 do
        let blocks = countOccurrences rendered.[i].Matrices @"\begin{pNiceMatrix}"
        Assert.Equal(expectedBlocks steps.[i], blocks)

[<Fact>]
let ``step 4 matrices show the I_simple drop as an empty Extend`` () =
    // Label b at step 4: Select holds (v_0, v_1, v_2, v_3), but the only b-edge from v_3
    // revisits v_2, so the Extend matrix is entirely empty cells.
    let matrices = rendered.[4].Matrices

    Assert.Contains(@"(v_0, v_1, v_2, v_3)", matrices)
    Assert.True(countOccurrences matrices @"\cdot" > 0)

[<Fact>]
let ``graph DOT highlights exactly the edges of the step's frontier paths`` () =
    for i in 0 .. rendered.Length - 1 do
        let expectedEdges =
            PathSemiring.allPaths steps.[i].M
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
let ``graph DOT highlights exactly the vertices of the step's frontier paths`` () =
    for i in 0 .. rendered.Length - 1 do
        let expectedVerts =
            PathSemiring.allPaths steps.[i].M
            |> Set.fold (fun acc p -> p |> List.fold (fun a v -> Set.add v a) acc) Set.empty

        let dot = rendered.[i].GraphDot
        let yellowCount = countOccurrences dot "fillcolor=lightyellow"
        Assert.Equal(Set.count expectedVerts, yellowCount)

// --- Template fill ---

let private summaryTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

/// Mirrors the real pipeline (SummaryTeX.belyaninStepSection): figures are wrapped in the
/// column-width adjustbox before being inserted into the template.
let private fillStepTemplate (template: string) (step: BelyaninStepVisualizer.BelyaninVisualizationStep) : string =
    template
        .Replace("__STEP_AUTOMATON_TIKZ__", SummaryTeX.wrapTikzAdjustboxColumn step.AutomatonTikz)
        .Replace("__MATRICES__", step.Matrices)
        .Replace("__STEP_GRAPH_TIKZ__", SummaryTeX.wrapTikzAdjustboxColumn step.GraphTikz)

[<Fact>]
let ``filled tikz step template has no leftover placeholders and wraps matrices in adjustbox`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Belyanin_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let filled = fillStepTemplate template rendered.[0]

    Assert.DoesNotContain("__", filled)
    Assert.Contains("adjustbox", filled)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``filled tikz step template compiles with lualatex`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Belyanin_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    for i in 0 .. rendered.Length - 1 do
        let step = rendered.[i]

        let filled = fillStepTemplate template step

        let document =
            summaryTemplate.Replace("__ALGORITHM__", "Belyanin RPQ").Replace("__CONTENT__", filled)

        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory tempDir |> ignore
        let texFile = Path.Combine(tempDir, sprintf "belyanin_step_%d.tex" i)
        File.WriteAllText(texFile, document)

        try
            Assert.True(ExternalTools.compileTexFile texFile tempDir)
        finally
            Directory.Delete(tempDir, true)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``every filled step wrapped in the step adjustbox compiles without Overfull boxes`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "Belyanin_step_tikz_template.tex")

    let template = File.ReadAllText templatePath
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    for i in 0 .. rendered.Length - 1 do
        // The whole-step adjustbox (at most \textwidth and 0.9\textheight) is what the
        // summary section builder applies around the filled template.
        let filled = SummaryTeX.wrapStepAdjustbox (fillStepTemplate template rendered.[i])

        let document =
            summaryTemplate.Replace("__ALGORITHM__", "Belyanin RPQ").Replace("__CONTENT__", filled)

        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory tempDir |> ignore
        let texFile = Path.Combine(tempDir, sprintf "belyanin_step_%d.tex" i)
        File.WriteAllText(texFile, document)

        try
            Assert.True(ExternalTools.compileTexFile texFile tempDir, sprintf "step %d: lualatex failed" i)

            let log = File.ReadAllText(texFile.Replace(".tex", ".log"))
            Assert.False(log.Contains "Overfull", sprintf "step %d: Overfull box in the log" i)
        finally
            Directory.Delete(tempDir, true)
