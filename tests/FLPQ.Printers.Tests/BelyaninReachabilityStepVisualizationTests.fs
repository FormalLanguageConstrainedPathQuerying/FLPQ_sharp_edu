module BelyaninReachabilityStepVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.RPQ
open FLPQ.TestUtilities

open GoldenHelpers

/// The shared RPQ example (see TestHelpers.rpqExampleGraph / rpqExampleRegexp).
let private testGraph: NFA<string, int> = TestHelpers.rpqExampleGraph

let private testRegexp: Regexp<string, string> = TestHelpers.rpqExampleRegexp

let private testDfa: DFA<string, int> = Regexp.toDfa testRegexp

let private steps, _ = BelyaninRPQ.evaluateReachabilityWithTrace testDfa testGraph

let private rendered =
    BelyaninReachabilityStepVisualizer.renderSteps id testDfa testGraph steps

/// Whether a Boolean cell is the additive zero.
let private isZero (b: bool) : bool = not b

let private frontierStates (m: Matrix<bool>) : Set<int> =
    BelyaninStepCommon.frontierStates isZero m

let private usedAutoEdges (m: Matrix<bool>) (n: Matrix<bool>) : Set<int * int> =
    BelyaninStepCommon.usedAutoEdges isZero m n

let private followedEdges (g: Matrix<bool>) (select: Matrix<bool>) : Set<int * int> =
    BelyaninStepCommon.followedEdges isZero g select

let private frontierVertices (m: Matrix<bool>) : Set<int> =
    Set.ofList
        [ for v in 0 .. Matrix.cols m - 1 do
              if BelyaninStepCommon.colNonEmpty isZero m v then
                  v ]

/// The terminal of a trace label; the example trace only has terminal label rows.
let private labelTerminal (label: AutomatonLabel<string>) : string =
    match label with
    | ATerm t -> t
    | AEpsilon -> failwith "example trace has an epsilon label row"

// --- Golden tests (TikZ mode) ---

module BelyaninReachabilityStepGoldenTests =

    [<Fact>]
    let ``all steps: frontier start goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_frontier_start.tex" i) rendered.[i].Start.Frontier

    [<Fact>]
    let ``all steps: visited start goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_visited_start.tex" i) rendered.[i].Start.Visited

    [<Fact>]
    let ``non-init steps: frontier end goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_frontier_end.tex" i) rendered.[i].End.Value.Frontier

    [<Fact>]
    let ``non-init steps: visited end goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_visited_end.tex" i) rendered.[i].End.Value.Visited

    [<Fact>]
    let ``all steps: automaton start tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_automaton_start.tikz" i) rendered.[i].Start.AutomatonTikz

    [<Fact>]
    let ``all steps: graph start tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_graph_start.tikz" i) rendered.[i].Start.GraphTikz

    [<Fact>]
    let ``all labels: select formula goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden
                    (sprintf "belyanin_reach_step_%d_label_%d_select.tex" i j)
                    rendered.[i].Labels.[j].SelectFormula

    [<Fact>]
    let ``all labels: extend formula goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden
                    (sprintf "belyanin_reach_step_%d_label_%d_extend.tex" i j)
                    rendered.[i].Labels.[j].ExtendFormula

    [<Fact>]
    let ``all labels: automaton tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden
                    (sprintf "belyanin_reach_step_%d_label_%d_automaton.tikz" i j)
                    rendered.[i].Labels.[j].AutomatonTikz

    [<Fact>]
    let ``all labels: graph tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden
                    (sprintf "belyanin_reach_step_%d_label_%d_graph.tikz" i j)
                    rendered.[i].Labels.[j].GraphTikz

    [<Fact>]
    let ``non-init steps: automaton end tikz goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_automaton_end.tikz" i) rendered.[i].End.Value.AutomatonTikz

    [<Fact>]
    let ``non-init steps: graph end tikz goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_reach_step_%d_graph_end.tikz" i) rendered.[i].End.Value.GraphTikz

// --- Structural facts ---

[<Fact>]
let ``init step has no end state and no label rows`` () =
    let init = rendered.[0]

    Assert.Null(init.End)
    Assert.True(List.isEmpty init.Labels)

[<Fact>]
let ``no reachability figure draws path edges`` () =
    for i in 0 .. rendered.Length - 1 do
        Assert.DoesNotContain("color=\"#FF9999\"", rendered.[i].Start.GraphDot)

        for label in rendered.[i].Labels do
            Assert.DoesNotContain("color=\"#FF9999\"", label.GraphDot)

        match rendered.[i].End with
        | Some end_ -> Assert.DoesNotContain("color=\"#FF9999\"", end_.GraphDot)
        | None -> ()

[<Fact>]
let ``all matrices are Boolean (bullets and dots, no path tuples)`` () =
    for i in 0 .. rendered.Length - 1 do
        Assert.DoesNotContain("(v_", rendered.[i].Start.Frontier)
        Assert.DoesNotContain("(v_", rendered.[i].Start.Visited)

        for label in rendered.[i].Labels do
            Assert.DoesNotContain("(v_", label.SelectFormula)
            Assert.DoesNotContain("(v_", label.ExtendFormula)

[<Fact>]
let ``start automaton of each step highlights exactly the frontier states of M`` () =
    for i in 0 .. rendered.Length - 1 do
        let expected = frontierStates steps.[i].M
        let dot = rendered.[i].Start.AutomatonDot

        Assert.Equal(Set.count expected, countOccurrences dot "fillcolor=lightblue")

        for q in expected do
            let lines =
                dot.Split('\n') |> Array.filter (fun l -> l.StartsWith(sprintf "  s%d [" q))

            ignore (Assert.Single lines)
            Assert.Contains("fillcolor=lightblue", lines.[0])

[<Fact>]
let ``end automaton of each non-init step highlights exactly the frontier states of NewM`` () =
    for i in 1 .. rendered.Length - 1 do
        let expected = frontierStates steps.[i].NewM
        let dot = rendered.[i].End.Value.AutomatonDot

        Assert.Equal(Set.count expected, countOccurrences dot "fillcolor=lightblue")

        for q in expected do
            let lines =
                dot.Split('\n') |> Array.filter (fun l -> l.StartsWith(sprintf "  s%d [" q))

            ignore (Assert.Single lines)
            Assert.Contains("fillcolor=lightblue", lines.[0])

[<Fact>]
let ``per-label automaton DOT highlights the used transitions red bold and the targets yellow`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let ls = steps.[i].Labels.[j]
            let expected = usedAutoEdges steps.[i].M ls.N
            let dot = rendered.[i].Labels.[j].AutomatonDot

            for q, qp in expected do
                let pattern = sprintf "s%d -> s%d [label=" q qp

                let redLines =
                    dot.Split('\n')
                    |> Array.filter (fun l ->
                        l.TrimStart().StartsWith(pattern) && l.Contains("color=red, penwidth=2.0"))

                Assert.True(
                    redLines.Length >= 1,
                    sprintf "step %d label %d: transition (%d,%d) not highlighted red bold" i j q qp
                )

            Assert.Equal(Set.count expected, countOccurrences dot "color=red, penwidth=2.0")

            // Target states = states with a non-empty F^a row; start > frontier > target
            // precedence means a state that is also on the current frontier stays lightblue.
            let targets =
                Set.difference (Set.remove testDfa.StartState (frontierStates ls.Select)) (frontierStates steps.[i].M)

            Assert.Equal(Set.count targets, countOccurrences dot "fillcolor=lightyellow")

[<Fact>]
let ``per-label graph DOT highlights followed edges red bold with endpoint tiers`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let ls = steps.[i].Labels.[j]
            let followed = followedEdges ls.G ls.Select
            let fromEndpoints = followed |> Set.fold (fun acc (u, _) -> Set.add u acc) Set.empty
            let targets = frontierVertices ls.Extend
            let dot = rendered.[i].Labels.[j].GraphDot

            for u, v in followed do
                let pattern = sprintf "v%d -> v%d [label=" u v

                let redLines =
                    dot.Split('\n')
                    |> Array.filter (fun l ->
                        l.TrimStart().StartsWith(pattern) && l.Contains("color=red, penwidth=2.0"))

                Assert.True(
                    redLines.Length >= 1,
                    sprintf "step %d label %d: edge (%d,%d) not highlighted red bold" i j u v
                )

            Assert.Equal(Set.count followed, countOccurrences dot "color=red, penwidth=2.0")

            let starts = testGraph.StartStates

            let lightblue = Set.difference fromEndpoints starts
            let lightyellow = Set.difference (Set.difference targets starts) fromEndpoints

            Assert.Equal(Set.count starts, countOccurrences dot "fillcolor=green")
            Assert.Equal(Set.count lightblue, countOccurrences dot "fillcolor=lightblue")
            Assert.Equal(Set.count lightyellow, countOccurrences dot "fillcolor=lightyellow")

[<Fact>]
let ``select formulas name the product and write three Boolean matrices`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let l = labelTerminal steps.[i].Labels.[j].Label
            let tex = rendered.[i].Labels.[j].SelectFormula

            Assert.Contains(sprintf @"$(N^{%s})^T \otimes F = " l, tex)
            Assert.Equal(3, countOccurrences tex @"\begin{pNiceMatrix}")
            Assert.DoesNotContain(@"\times", tex)
            Assert.Contains(@"\bullet", tex)

[<Fact>]
let ``extend formulas name the product and write three Boolean matrices`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let l = labelTerminal steps.[i].Labels.[j].Label
            let tex = rendered.[i].Labels.[j].ExtendFormula

            Assert.Contains(sprintf @"$F^{%s} \otimes G^{%s} = " l l, tex)
            Assert.Equal(3, countOccurrences tex @"\begin{pNiceMatrix}")
            Assert.DoesNotContain(@"\times", tex)

[<Fact>]
let ``visited start block is P minus M and frontier start has no valign`` () =
    for i in 0 .. rendered.Length - 1 do
        let frontier = rendered.[i].Start.Frontier
        Assert.Contains(@"\text{F} =", frontier)
        Assert.DoesNotContain(@"\text{V}", frontier)
        Assert.DoesNotContain("valign=T", frontier)

        let visited = rendered.[i].Start.Visited
        Assert.Contains(@"\text{V} =", visited)
        Assert.DoesNotContain(@"\text{F}", visited)
        Assert.Contains("valign=T", visited)

[<Fact>]
let ``frontier end tile is titled F and top-aligned`` () =
    for i in 1 .. rendered.Length - 1 do
        let m = rendered.[i].End.Value.Frontier
        Assert.Contains(@"\text{F} =", m)
        Assert.DoesNotContain(@"\text{New}", m)
        Assert.Contains("valign=T", m)

// --- Template fill (real pipeline) ---

let private summaryTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

/// The real step templates as the CLI loads them (TikZ mode).
let private realTemplates: SummaryTeX.StepTemplates =
    let read (name: string) =
        File.ReadAllText(Path.Combine(System.AppContext.BaseDirectory, name))

    { Gll = ""
      GllTikz = ""
      Rnglr = ""
      RnglrTikz = ""
      Arroyuelo = ""
      ArroyueloTikz = ""
      Belyanin = read "Belyanin_step_template.tex"
      BelyaninTikz = read "Belyanin_step_tikz_template.tex"
      BelyaninRowEnd = read "Belyanin_row_end_template.tex"
      BelyaninRowEndTikz = read "Belyanin_row_end_tikz_template.tex"
      BelyaninLabelRow = read "Belyanin_label_row_template.tex"
      BelyaninLabelRowTikz = read "Belyanin_label_row_tikz_template.tex" }

/// Writes one rendered step to a step dir exactly as the runner does (TikZ mode).
let private writeStepDir (stepDir: string) (step: BelyaninStepCommon.BelyaninVisualizationStep) : unit =
    File.WriteAllText(Path.Combine(stepDir, "frontier_start.tex"), step.Start.Frontier)
    File.WriteAllText(Path.Combine(stepDir, "visited_start.tex"), step.Start.Visited)
    File.WriteAllText(Path.Combine(stepDir, "automaton_start.tikz.tex"), step.Start.AutomatonTikz)
    File.WriteAllText(Path.Combine(stepDir, "graph_start.tikz.tex"), step.Start.GraphTikz)

    for j in 0 .. step.Labels.Length - 1 do
        let label = step.Labels.[j]

        File.WriteAllText(Path.Combine(stepDir, sprintf "label_%d_select.tex" j), label.SelectFormula)
        File.WriteAllText(Path.Combine(stepDir, sprintf "label_%d_extend.tex" j), label.ExtendFormula)
        File.WriteAllText(Path.Combine(stepDir, sprintf "label_%d_automaton.tikz.tex" j), label.AutomatonTikz)
        File.WriteAllText(Path.Combine(stepDir, sprintf "label_%d_graph.tikz.tex" j), label.GraphTikz)

    match step.End with
    | Some end_ ->
        File.WriteAllText(Path.Combine(stepDir, "frontier_end.tex"), end_.Frontier)
        File.WriteAllText(Path.Combine(stepDir, "visited_end.tex"), end_.Visited)
        File.WriteAllText(Path.Combine(stepDir, "automaton_end.tikz.tex"), end_.AutomatonTikz)
        File.WriteAllText(Path.Combine(stepDir, "graph_end.tikz.tex"), end_.GraphTikz)
    | None -> ()

let private filledStep (stepDir: string) (i: int) : string =
    SummaryTeX.belyaninStepSection stepDir i realTemplates true |> List.item 1

[<Fact>]
let ``filled tikz step sections have no leftover placeholders`` () =
    TestHelpers.withTempDir (fun dir ->
        for i in 0 .. rendered.Length - 1 do
            let stepDir = Path.Combine(dir, sprintf "step_%d" i)
            Directory.CreateDirectory stepDir |> ignore
            writeStepDir stepDir rendered.[i]

            let filled = filledStep stepDir i

            Assert.DoesNotContain("__", filled)
            Assert.Contains("adjustbox", filled))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``every filled reachability step section compiles with lualatex`` () =
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    TestHelpers.withTempDir (fun dir ->
        for i in 0 .. rendered.Length - 1 do
            let stepDir = Path.Combine(dir, sprintf "step_%d" i)
            Directory.CreateDirectory stepDir |> ignore
            writeStepDir stepDir rendered.[i]

            let document =
                summaryTemplate
                    .Replace("__ALGORITHM__", "Belyanin RPQ (reachability)")
                    .Replace("__CONTENT__", filledStep stepDir i)

            let texFile = Path.Combine(dir, sprintf "belyanin_reach_step_%d.tex" i)
            File.WriteAllText(texFile, document)

            Assert.True(ExternalTools.compileTexFile texFile dir, sprintf "step %d: lualatex failed" i))
