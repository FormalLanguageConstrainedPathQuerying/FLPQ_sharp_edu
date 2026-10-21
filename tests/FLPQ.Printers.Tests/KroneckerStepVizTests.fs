module KroneckerStepVizTests

open System.IO
open Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

open GoldenHelpers

/// The book Chapter 12 worked example (S -> aSb | ab over a 4-vertex graph), hand-built so the
/// trace is small and deterministic. Mirrors CfpqTestHelpers.tensorBookRsm/tensorBookGraph.
let private bookRsm: RSM<string, string> =
    let s = Nonterminal "S"

    RsmFixtures.mkRsm
        [| for i in 0..3 ->
               { BlockNonterminal = s
                 LocalState = i
                 IsFinal = (i = 3) } |]
        [ { From = 0
            Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "a"))
            To = 1 }
          { From = 1
            Label = AutomatonLabel.ATerm(RsmSymbol.RNonterm s)
            To = 2 }
          { From = 1
            Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "b"))
            To = 3 }
          { From = 2
            Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "b"))
            To = 3 } ]
        [ (s, 0) ]
        [ 3 ]
        s

let private bookGraph: CfpqInput.CfpqGraph<string> =
    { VertexCount = 4
      Labels =
        Map.ofList
            [ "a", Matrix.create 4 4 (fun i j -> (i, j) = (0, 1) || (i, j) = (1, 2) || (i, j) = (2, 0))
              "b", Matrix.create 4 4 (fun i j -> (i, j) = (2, 3) || (i, j) = (3, 2)) ] }

let private ntName (nt: Nonterminal<string>) : string =
    match nt with
    | Nonterminal s -> s

let private result = KroneckerCfpq.evaluate bookRsm bookGraph

let private rendered =
    result.Steps
    |> List.map (fun step -> KroneckerStepViz.renderStep id ntName bookGraph.VertexCount step "" "")

let private count (needle: string) (haystack: string) : int = countOccurrences haystack needle

module KroneckerStepVizStructural =

    [<Fact>]
    let ``kron body has three pNiceMatrix blocks and the product operators`` () =
        for step in rendered do
            Assert.Equal(3, count @"\begin{pNiceMatrix}" step.Kron)
            Assert.Contains(@"\otimes", step.Kron)
            Assert.Contains(@"=", step.Kron)
            // The operands are labeled by q_/v_; the result mask is a Boolean matrix.
            Assert.Contains("q_0", step.Kron)
            Assert.Contains("v_0", step.Kron)

    [<Fact>]
    let ``kron product result highlights the step's newly derived cells`` () =
        for step in rendered do
            // Every productive iteration derives cells, so the result mask carries highlights.
            Assert.True(count @"\cellcolor{yellow}" step.Kron >= 1, "no highlighted cell in the product result")

    [<Fact>]
    let ``kron operands show set-of-symbols cells`` () =
        let step = rendered.[0]

        // M1 carries the terminal 'a', the nonterminal S (as \text{S}), and the terminal 'b'.
        Assert.Contains(@"\text{S}", step.Kron)
        Assert.Contains("a", step.Kron)

    [<Fact>]
    let ``fronts body has two pNiceMatrix blocks with product-state pair labels`` () =
        for step in rendered do
            Assert.Equal(2, count @"\begin{pNiceMatrix}" step.Fronts)
            // Rows and columns are product-state pairs (q_i, u_j); the row's nonterminal is
            // replaced by its start state q_0 (the book example has one block, S, start state 0).
            Assert.Contains("q_0, v_0", step.Fronts)
            Assert.DoesNotContain(@"\text{S}, v_", step.Fronts)

    [<Fact>]
    let ``final front highlights exactly the final-state columns`` () =
        for step in rendered do
            let yellow = count @"\cellcolor{yellow}" step.Fronts
            // 4 rows (one per source) x 4 final-state columns (final state 3, vertices 0..3).
            Assert.Equal(4 * 4, yellow)

    [<Fact>]
    let ``graph figure passes through`` () =
        let step =
            KroneckerStepViz.renderStep id ntName 4 result.Steps.[0] "DOTSRC" "TIKZSRC"

        Assert.Equal("DOTSRC", step.GraphDot)
        Assert.Equal("TIKZSRC", step.GraphTikz)

module KroneckerStepVizCompile =

    /// Build the graph figure for a step (full input graph, new NT edges drawn and highlighted red).
    let private graphFor (step: KroneckerCfpq.CfpqKronStep<string, string>) : string =
        let newNtEdges =
            step.ChangedCells
            |> List.map (fun c -> ((c.Row, c.Col), ntName c.Nt))
            |> Map.ofList

        let newNtEdgeSet = newNtEdges |> Map.toList |> List.map fst |> Set.ofList

        let _, tikz =
            RpqGraphViz.renderGraph
                id
                (CfpqInput.toNfa bookGraph)
                Set.empty
                Set.empty
                Set.empty
                newNtEdgeSet
                Set.empty
                newNtEdgeSet
                newNtEdges

        tikz

    let private fill (template: string) (step: KroneckerStepViz.KroneckerVisualizationStep) : string =
        template
            .Replace("__KRON__", step.Kron)
            .Replace("__FRONTS__", step.Fronts)
            .Replace("__GRAPH_TIKZ__", SummaryTeX.wrapTikzAdjustboxColumnTop step.GraphTikz)

    [<Fact>]
    [<Trait("Category", "TeX")>]
    let ``filled tikz step template has no leftover placeholders`` () =
        let templatePath =
            Path.Combine(System.AppContext.BaseDirectory, "Kronecker_step_tikz_template.tex")

        let template = File.ReadAllText templatePath

        for step in rendered do
            let filled = fill template step
            Assert.DoesNotContain("__", filled)

    [<Fact>]
    [<Trait("Category", "TeX")>]
    let ``every filled step compiles with lualatex without Overfull`` () =
        let templatePath =
            Path.Combine(System.AppContext.BaseDirectory, "Kronecker_step_tikz_template.tex")

        let summaryTemplatePath =
            Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")

        let template = File.ReadAllText templatePath
        let summaryTemplate = File.ReadAllText summaryTemplatePath

        for i in 0 .. result.Steps.Length - 1 do
            let step = result.Steps.[i]

            let renderedStep =
                KroneckerStepViz.renderStep id ntName bookGraph.VertexCount step "" (graphFor step)

            let filled = fill template renderedStep

            let document =
                summaryTemplate.Replace("__ALGORITHM__", "Kronecker CFPQ").Replace("__CONTENT__", filled)

            let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory tempDir |> ignore
            let texFile = Path.Combine(tempDir, sprintf "kron_step_%d.tex" i)
            File.WriteAllText(texFile, document)

            try
                Assert.True(ExternalTools.compileTexFile texFile tempDir, sprintf "step %d: lualatex failed" i)

                let log = File.ReadAllText(texFile.Replace(".tex", ".log"))
                Assert.False(log.Contains "Overfull", sprintf "step %d: Overfull box in the log" i)
            finally
                Directory.Delete(tempDir, true)
