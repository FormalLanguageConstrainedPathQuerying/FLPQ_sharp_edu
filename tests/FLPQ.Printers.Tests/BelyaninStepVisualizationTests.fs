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

let private rendered = BelyaninStepVisualizer.renderSteps id testDfa testGraph steps

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
    // Per label: N^a^T, F, Select, G^a, Extend — five explicit matrices.
    let expectedBlocks (step: BelyaninRPQ.BelyaninTraceStep<string>) =
        if step.IsInit then
            2
        else
            2 + 5 * List.length step.Labels + 1

    for i in 0 .. rendered.Length - 1 do
        let blocks = countOccurrences rendered.[i].Matrices @"\begin{pNiceMatrix}"
        Assert.Equal(expectedBlocks steps.[i], blocks)

[<Fact>]
let ``matrices title the frontier F and the visited V, never M or P`` () =
    for i in 0 .. rendered.Length - 1 do
        let m = rendered.[i].Matrices

        Assert.Contains(@"$\text{F}$", m)
        Assert.Contains(@"$\text{V}$", m)
        Assert.DoesNotContain(@"$\text{M}$", m)
        Assert.DoesNotContain(@"$\text{P}$", m)

    for i in 1 .. rendered.Length - 1 do
        Assert.Contains(@"$\text{New } F =$", rendered.[i].Matrices)

[<Fact>]
let ``each label block writes every matrix of the product explicitly`` () =
    for i in 1 .. rendered.Length - 1 do
        let m = rendered.[i].Matrices

        // One explicit chain per label: (N^a)^T ⊗ F = ... × G^a = ...
        Assert.Equal(List.length steps.[i].Labels, countOccurrences m @"\otimes F =")
        Assert.Equal(List.length steps.[i].Labels, countOccurrences m @"\times G^{")

        for ls in steps.[i].Labels do
            let l =
                match ls.Label with
                | ATerm t -> t
                | AEpsilon -> failwith "epsilon label in trace"

            Assert.Contains(sprintf @"$(N^{%s})^T \otimes F =$" l, m)
            Assert.Contains(sprintf @"$\times G^{%s} =$" l, m)

[<Fact>]
let ``matrices use q_i row headers for |Q|x|V| blocks and write N^a^T/G^a explicitly`` () =
    let m1 = rendered.[1].Matrices

    // F block: the frontier path (v_0) sits at state q_0, vertex column v_0.
    Assert.Contains(@"q_0 & \{(v_0)\}", m1)
    // The old mislabeling (v_i row headers on |Q|x|V| matrices) is gone.
    Assert.DoesNotContain(@"v_0 & \{(v_0)\}", m1)

    // Explicit (N^a)^T for label a: q0 -a-> q1 and q1 -a-> q2 transpose to T[1,0], T[2,1].
    Assert.Contains(@"q_1 & \bullet & \cdot & \cdot \\", m1)
    Assert.Contains(@"q_2 & \cdot & \bullet & \cdot \\", m1)

    // Explicit G^a: the a-edges 0->1, 2->5, 3->4.
    Assert.Contains(@"v_0 & \cdot & \bullet & \cdot & \cdot & \cdot & \cdot \\", m1)
    Assert.Contains(@"v_2 & \cdot & \cdot & \cdot & \cdot & \cdot & \bullet \\", m1)
    Assert.Contains(@"v_3 & \cdot & \cdot & \cdot & \cdot & \bullet & \cdot \\", m1)

[<Fact>]
let ``step 4 matrices show the I_simple drop as an empty Extend`` () =
    // Label b at step 4: Select holds (v_0, v_1, v_2, v_3), but the only b-edge from v_3
    // revisits v_2, so the Extend matrix is entirely empty cells.
    let matrices = rendered.[4].Matrices

    Assert.Contains(@"(v_0, v_1, v_2, v_3)", matrices)
    Assert.True(countOccurrences matrices @"\cdot" > 0)

[<Fact>]
let ``graph DOT highlights exactly the current-step edges in red bold`` () =
    for i in 0 .. rendered.Length - 1 do
        let expectedEdges = RpqGraphViz.pathLastEdges steps.[i].NewM
        let dot = rendered.[i].GraphDot

        for fromV, toV in expectedEdges do
            let pattern = sprintf "v%d -> v%d [label=" fromV toV

            let redLines =
                dot.Split('\n')
                |> Array.filter (fun l -> l.TrimStart().StartsWith(pattern) && l.Contains("color=red, penwidth=2.0"))

            Assert.True(redLines.Length >= 1, sprintf "step %d: edge (%d,%d) not highlighted red bold" i fromV toV)

        let redCount = countOccurrences dot "color=red, penwidth=2.0"
        Assert.Equal(Set.count expectedEdges, redCount)

[<Fact>]
let ``graph DOT highlights exactly the path edges in light red`` () =
    for i in 0 .. rendered.Length - 1 do
        // Edges that are also current-step edges render red bold instead of light red.
        let expectedEdges =
            Set.difference (RpqGraphViz.pathEdges steps.[i].M) (RpqGraphViz.pathLastEdges steps.[i].NewM)

        let dot = rendered.[i].GraphDot

        for fromV, toV in expectedEdges do
            let pattern = sprintf "v%d -> v%d [label=" fromV toV

            let lightRedLines =
                dot.Split('\n')
                |> Array.filter (fun l -> l.TrimStart().StartsWith(pattern) && l.Contains("color=\"#FF9999\""))

            Assert.True(
                lightRedLines.Length >= 1,
                sprintf "step %d: edge (%d,%d) not highlighted light red" i fromV toV
            )

        let lightRedCount = countOccurrences dot "color=\"#FF9999\""
        Assert.Equal(Set.count expectedEdges, lightRedCount)

[<Fact>]
let ``graph DOT highlights exactly the current-step endpoint vertices`` () =
    for i in 0 .. rendered.Length - 1 do
        let expectedVerts =
            RpqGraphViz.edgeEndpoints (RpqGraphViz.pathLastEdges steps.[i].NewM)

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
