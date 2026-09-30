module BelyaninStepVisualizationTests

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

let private steps, _ = BelyaninRPQ.evaluateWithTrace testDfa testGraph

let private rendered = BelyaninStepVisualizer.renderSteps id testDfa testGraph steps

/// The automaton states of the path matrix's frontier: q with a non-empty M[q, *] cell.
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

/// True when the row of a path semiring matrix holds at least one path.
let private rowNonEmpty (m: Matrix<Set<int list>>) (i: int) : bool =
    [ for j in 0 .. Matrix.cols m - 1 do
          not (PathSemiring.isZero m.[i, j]) ]
    |> List.exists id

/// True when the column of a path semiring matrix holds at least one path.
let private colNonEmpty (m: Matrix<Set<int list>>) (j: int) : bool =
    [ for i in 0 .. Matrix.rows m - 1 do
          not (PathSemiring.isZero m.[i, j]) ]
    |> List.exists id

/// The DFA transitions used by the label's backward step: q -> q' when N^a[q, q'] and the
/// frontier holds a path at q.
let private usedAutoEdges (m: Matrix<Set<int list>>) (n: Matrix<bool>) : Set<int * int> =
    Set.ofList
        [ for q in 0 .. Matrix.rows n - 1 do
              if rowNonEmpty m q then
                  for qp in 0 .. Matrix.cols n - 1 do
                      if n.[q, qp] then
                          (q, qp) ]

/// The graph edges followed by the label's forward step: u -> v when G^a[u, v] and the
/// selected matrix holds a path at u.
let private followedEdges (g: Matrix<bool>) (select: Matrix<Set<int list>>) : Set<int * int> =
    Set.ofList
        [ for u in 0 .. Matrix.rows g - 1 do
              if colNonEmpty select u then
                  for v in 0 .. Matrix.cols g - 1 do
                      if g.[u, v] then
                          (u, v) ]

/// The number of cells of a path semiring matrix that hold at least one path.
let private nonEmptyCells (m: Matrix<Set<int list>>) : int =
    [ for i in 0 .. Matrix.rows m - 1 do
          for j in 0 .. Matrix.cols m - 1 do
              if not (PathSemiring.isZero m.[i, j]) then 1 else 0 ]
    |> List.sum

/// The terminal of a trace label; the example trace only has terminal label rows.
let private labelTerminal (label: AutomatonLabel<string>) : string =
    match label with
    | ATerm t -> t
    | AEpsilon -> failwith "example trace has an epsilon label row"

// --- Golden tests (TikZ mode) ---

module BelyaninStepGoldenTests =

    [<Fact>]
    let ``all steps: frontier start goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_frontier_start.tex" i) rendered.[i].Start.Frontier

    [<Fact>]
    let ``all steps: visited start goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_visited_start.tex" i) rendered.[i].Start.Visited

    [<Fact>]
    let ``non-init steps: frontier end goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_frontier_end.tex" i) rendered.[i].End.Value.Frontier

    [<Fact>]
    let ``non-init steps: visited end goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_visited_end.tex" i) rendered.[i].End.Value.Visited

    [<Fact>]
    let ``all steps: automaton start tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_automaton_start.tikz" i) rendered.[i].Start.AutomatonTikz

    [<Fact>]
    let ``all steps: graph start tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_graph_start.tikz" i) rendered.[i].Start.GraphTikz

    [<Fact>]
    let ``all labels: select formula goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden (sprintf "belyanin_step_%d_label_%d_select.tex" i j) rendered.[i].Labels.[j].SelectFormula

    [<Fact>]
    let ``all labels: extend formula goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden (sprintf "belyanin_step_%d_label_%d_extend.tex" i j) rendered.[i].Labels.[j].ExtendFormula

    [<Fact>]
    let ``all labels: automaton tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden
                    (sprintf "belyanin_step_%d_label_%d_automaton.tikz" i j)
                    rendered.[i].Labels.[j].AutomatonTikz

    [<Fact>]
    let ``all labels: graph tikz goldens`` () =
        for i in 0 .. rendered.Length - 1 do
            for j in 0 .. rendered.[i].Labels.Length - 1 do
                verifyGolden (sprintf "belyanin_step_%d_label_%d_graph.tikz" i j) rendered.[i].Labels.[j].GraphTikz

    [<Fact>]
    let ``non-init steps: automaton end tikz goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_automaton_end.tikz" i) rendered.[i].End.Value.AutomatonTikz

    [<Fact>]
    let ``non-init steps: graph end tikz goldens`` () =
        for i in 1 .. rendered.Length - 1 do
            verifyGolden (sprintf "belyanin_step_%d_graph_end.tikz" i) rendered.[i].End.Value.GraphTikz

// --- Structural facts ---

[<Fact>]
let ``init step has no end state and no label rows`` () =
    let init = rendered.[0]

    Assert.Null(init.End)
    Assert.True(List.isEmpty init.Labels)

[<Fact>]
let ``start automaton DOT of each step highlights exactly the frontier states of M`` () =
    for i in 0 .. rendered.Length - 1 do
        let expected = frontierStates steps.[i].M
        let dot = rendered.[i].Start.AutomatonDot

        Assert.Equal(Set.count expected, countOccurrences dot "fillcolor=lightblue")

        for q in expected do
            // DOT node ids are s<idx>; the state visualizer sets the label attribute.
            let lines =
                dot.Split('\n') |> Array.filter (fun l -> l.StartsWith(sprintf "  s%d [" q))

            ignore (Assert.Single lines)
            Assert.Contains("fillcolor=lightblue", lines.[0])

[<Fact>]
let ``end automaton DOT of each non-init step highlights exactly the frontier states of NewM`` () =
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
let ``per-label automaton DOT highlights exactly the used a-transitions`` () =
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

[<Fact>]
let ``per-label graph DOT highlights followed edges red bold and tiers the endpoint vertices`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let ls = steps.[i].Labels.[j]
            let followed = followedEdges ls.G ls.Select
            let fromEndpoints = followed |> Set.fold (fun acc (u, _) -> Set.add u acc) Set.empty
            let targets = RpqGraphViz.frontierVertices ls.Extend
            let dot = rendered.[i].Labels.[j].GraphDot

            // Red bold: exactly the followed a-edges.
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

            // Vertex tiers (precedence start > frontier > highlighted): green sources,
            // lightblue from-endpoints, lightyellow targets.
            let starts = testGraph.StartStates
            let lightblue = Set.difference fromEndpoints starts
            let lightyellow = Set.difference (Set.difference targets starts) fromEndpoints

            Assert.Equal(Set.count starts, countOccurrences dot "fillcolor=green")
            Assert.Equal(Set.count lightblue, countOccurrences dot "fillcolor=lightblue")
            Assert.Equal(Set.count lightyellow, countOccurrences dot "fillcolor=lightyellow")

            for v in lightblue do
                let line =
                    dot.Split('\n') |> Array.find (fun l -> l.StartsWith(sprintf "  v%d [" v))

                Assert.Contains("fillcolor=lightblue", line)

            for v in lightyellow do
                let line =
                    dot.Split('\n') |> Array.find (fun l -> l.StartsWith(sprintf "  v%d [" v))

                Assert.Contains("fillcolor=lightyellow", line)

            // Light red: F's path edges minus the followed ones.
            let pathEdges = Set.difference (RpqGraphViz.pathEdges steps.[i].M) followed
            Assert.Equal(Set.count pathEdges, countOccurrences dot "color=\"#FF9999\"")

[<Fact>]
let ``start and end graphs show green sources, lightblue frontier vertices, and lightred path edges only`` () =
    let check (m: Matrix<Set<int list>>) (dot: string) =
        Assert.Equal(Set.count testGraph.StartStates, countOccurrences dot "fillcolor=green")

        Assert.Equal(
            Set.count (Set.difference (RpqGraphViz.frontierVertices m) testGraph.StartStates),
            countOccurrences dot "fillcolor=lightblue"
        )

        Assert.Equal(0, countOccurrences dot "color=red, penwidth=2.0")
        Assert.Equal(Set.count (RpqGraphViz.pathEdges m), countOccurrences dot "color=\"#FF9999\"")

    for i in 0 .. rendered.Length - 1 do
        check steps.[i].M rendered.[i].Start.GraphDot

    for i in 1 .. rendered.Length - 1 do
        check steps.[i].NewM rendered.[i].End.Value.GraphDot

[<Fact>]
let ``select formulas name the product with otimes and write three explicit matrices`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let ls = steps.[i].Labels.[j]
            let l = labelTerminal ls.Label
            let tex = rendered.[i].Labels.[j].SelectFormula

            Assert.Contains(sprintf @"$(N^{%s})^T \otimes F = " l, tex)
            Assert.Equal(3, countOccurrences tex @"\begin{pNiceMatrix}")
            Assert.DoesNotContain(@"\times", tex)

[<Fact>]
let ``extend formulas name the product with otimes and write three explicit matrices`` () =
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let ls = steps.[i].Labels.[j]
            let l = labelTerminal ls.Label
            let tex = rendered.[i].Labels.[j].ExtendFormula

            Assert.Contains(sprintf @"$F^{%s} \otimes G^{%s} = " l l, tex)
            Assert.Equal(3, countOccurrences tex @"\begin{pNiceMatrix}")
            Assert.DoesNotContain(@"\times", tex)

[<Fact>]
let ``extend formulas whose Extend is empty keep only the F^a path tuples`` () =
    let emptyExtendLabels =
        [ for i in 0 .. rendered.Length - 1 do
              for j in 0 .. rendered.[i].Labels.Length - 1 do
                  if PathSemiring.isEmpty steps.[i].Labels.[j].Extend then
                      (i, j) ]

    // The example trace has I_simple drops (e.g. step 4 label b); the fact must not pass
    // vacuously if a trace change removes them all.
    Assert.NotEmpty(emptyExtendLabels)

    for i, j in emptyExtendLabels do
        let ls = steps.[i].Labels.[j]

        // Only the F^a block (first) holds path tuples; G^a is boolean and the
        // Extend result is entirely empty cells.
        Assert.Equal(nonEmptyCells ls.Select, countOccurrences rendered.[i].Labels.[j].ExtendFormula @"\{(v_")

[<Fact>]
let ``frontier start has exactly the F block`` () =
    for i in 0 .. rendered.Length - 1 do
        let m = rendered.[i].Start.Frontier

        Assert.Equal(1, countOccurrences m @"\begin{pNiceMatrix}")
        Assert.Contains(@"$\text{F}$", m)
        Assert.DoesNotContain(@"$\text{V}$", m)

[<Fact>]
let ``visited start has exactly the V block`` () =
    for i in 0 .. rendered.Length - 1 do
        let m = rendered.[i].Start.Visited

        Assert.Equal(1, countOccurrences m @"\begin{pNiceMatrix}")
        Assert.Contains(@"$\text{V}$", m)
        Assert.DoesNotContain(@"$\text{F}$", m)

[<Fact>]
let ``frontier end has exactly the New F block`` () =
    for i in 1 .. rendered.Length - 1 do
        let m = rendered.[i].End.Value.Frontier

        Assert.Equal(1, countOccurrences m @"\begin{pNiceMatrix}")
        Assert.Contains(@"$\text{New } F =$", m)
        Assert.DoesNotContain(@"$\text{V}$", m)

[<Fact>]
let ``visited end has exactly the V block`` () =
    for i in 1 .. rendered.Length - 1 do
        let m = rendered.[i].End.Value.Visited

        Assert.Equal(1, countOccurrences m @"\begin{pNiceMatrix}")
        Assert.Contains(@"$\text{V}$", m)

[<Fact>]
let ``visited start block holds the visited paths P minus the frontier M`` () =
    // Step 1: nothing visited before the first iteration — the V block is empty.
    Assert.DoesNotContain(@"(v_0)", rendered.[1].Start.Visited)

    // Step 2: V holds exactly (v_0) at q_0.
    Assert.Contains(@"q_0 & \{(v_0)\}", rendered.[2].Start.Visited)

    // Step 5: V holds every earlier frontier path, including both of step 4's M.
    let v5 = rendered.[5].Start.Visited
    Assert.Contains(@"(v_0, v_1, v_2, v_3)", v5)
    Assert.Contains(@"(v_0, v_1, v_2, v_5)", v5)

[<Fact>]
let ``frontier and visited use q_i row headers for |Q|x|V| blocks`` () =
    let frontier = rendered.[1].Start.Frontier

    // F block: the frontier path (v_0) sits at state q_0, vertex column v_0.
    Assert.Contains(@"q_0 & \{(v_0)\}", frontier)
    // The old mislabeling (v_i row headers on |Q|x|V| matrices) is gone.
    Assert.DoesNotContain(@"v_0 & \{(v_0)\}", frontier)

    let visited = rendered.[2].Start.Visited
    Assert.Contains(@"q_0 & \{(v_0)\}", visited)
    Assert.DoesNotContain(@"v_0 & \{(v_0)\}", visited)

[<Fact>]
let ``step 1 label a writes N^a^T and G^a explicitly`` () =
    let select = rendered.[1].Labels.[0].SelectFormula

    // Explicit (N^a)^T: q0 -a-> q1 and q1 -a-> q2 transpose to T[1,0], T[2,1].
    Assert.Contains(@"q_1 & \bullet & \cdot & \cdot \\", select)
    Assert.Contains(@"q_2 & \cdot & \bullet & \cdot \\", select)

    let extend = rendered.[1].Labels.[0].ExtendFormula

    // Explicit G^a: the a-edges 0->1, 2->5, 3->4.
    Assert.Contains(@"v_0 & \cdot & \bullet & \cdot & \cdot & \cdot & \cdot \\", extend)
    Assert.Contains(@"v_2 & \cdot & \cdot & \cdot & \cdot & \cdot & \bullet \\", extend)
    Assert.Contains(@"v_3 & \cdot & \cdot & \cdot & \cdot & \bullet & \cdot \\", extend)

[<Fact>]
let ``all labels: formulas are single horizontal lines`` () =
    // Every select/extend formula is one math expression holding the three matrices side by
    // side: one adjustbox wrap, one `$...$` pair, three matrices, two `\otimes`, and no blank
    // line between the matrices (which would stack them).
    for i in 0 .. rendered.Length - 1 do
        for j in 0 .. rendered.[i].Labels.Length - 1 do
            let label = rendered.[i].Labels.[j]

            for formula in [ label.SelectFormula; label.ExtendFormula ] do
                Assert.StartsWith(@"\begin{adjustbox}{max width=\textwidth}", formula)
                Assert.EndsWith(@"\end{adjustbox}", formula)
                Assert.Equal(3, countOccurrences formula @"\begin{pNiceMatrix}")
                Assert.Equal(2, countOccurrences formula @"\otimes")
                Assert.Equal(2, countOccurrences formula "$")
                Assert.DoesNotContain("\n\n", formula)

[<Fact>]
let ``step 4 label b shows the I_simple drop as an empty Extend`` () =
    // Label b at step 4: Select holds (v_0, v_1, v_2, v_3), but the only b-edge from v_3
    // revisits v_2, so the Extend matrix is entirely empty cells.
    let label = rendered.[4].Labels.[1]

    Assert.Contains(@"(v_0, v_1, v_2, v_3)", label.SelectFormula)
    Assert.True(countOccurrences label.ExtendFormula @"\cdot" > 0)

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
let private writeStepDir (stepDir: string) (step: BelyaninStepVisualizer.BelyaninVisualizationStep) : unit =
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

/// The filled step template (no whole-step adjustbox) as produced by the real section builder
/// (SummaryTeX.belyaninStepSection), which scans the step dir for label rows.
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
let ``filled tikz step sections compile with lualatex`` () =
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    TestHelpers.withTempDir (fun dir ->
        for i in 0 .. rendered.Length - 1 do
            let stepDir = Path.Combine(dir, sprintf "step_%d" i)
            Directory.CreateDirectory stepDir |> ignore
            writeStepDir stepDir rendered.[i]

            let document =
                summaryTemplate.Replace("__ALGORITHM__", "Belyanin RPQ").Replace("__CONTENT__", filledStep stepDir i)

            let texFile = Path.Combine(dir, sprintf "belyanin_step_%d.tex" i)
            File.WriteAllText(texFile, document)

            Assert.True(ExternalTools.compileTexFile texFile dir, sprintf "step %d: lualatex failed" i))

[<Fact>]
[<Trait("Category", "TeX")>]
let ``every filled step section compiles without Overfull boxes`` () =
    let summaryTemplate = File.ReadAllText summaryTemplatePath

    TestHelpers.withTempDir (fun dir ->
        for i in 0 .. rendered.Length - 1 do
            // The section builder wraps the whole filled template in the step adjustbox
            // (at most \textwidth and 0.9\textheight).
            let stepDir = Path.Combine(dir, sprintf "step_%d" i)
            Directory.CreateDirectory stepDir |> ignore
            writeStepDir stepDir rendered.[i]

            let document =
                summaryTemplate.Replace("__ALGORITHM__", "Belyanin RPQ").Replace("__CONTENT__", filledStep stepDir i)

            let texFile = Path.Combine(dir, sprintf "belyanin_step_%d.tex" i)
            File.WriteAllText(texFile, document)

            Assert.True(ExternalTools.compileTexFile texFile dir, sprintf "step %d: lualatex failed" i)

            let log = File.ReadAllText(texFile.Replace(".tex", ".log"))
            Assert.False(log.Contains "Overfull", sprintf "step %d: Overfull box in the log" i))
