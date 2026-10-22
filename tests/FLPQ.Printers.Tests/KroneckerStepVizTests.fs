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

/// The cells newly filled in `cur` versus `prev` (the true cells of `cur` not in `prev`).
let private newCells (prev: Set<int * int>) (cur: Matrix<bool>) : Set<int * int> = Set.difference (trueCells cur) prev

/// Render every step with the K new-cell diff computed against the previous step, exactly as the
/// CLI writer does. (The final front highlights its own FinalStateCols cells, so no diff is
/// passed for it.)
let private rendered =
    let rec go prevK acc (steps: KroneckerCfpq.CfpqKronStep<string, string> list) =
        match steps with
        | [] -> List.rev acc
        | step :: rest ->
            let kNew = newCells prevK step.K

            let renderedStep =
                KroneckerStepViz.renderStep id ntName bookGraph.VertexCount step kNew "" ""

            go (trueCells step.K) (renderedStep :: acc) rest

    go Set.empty [] result.Steps

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
    let ``kron product result has pair-based labels and highlights the newly filled cells`` () =
        for i, step in List.indexed rendered do
            // The result mask is a product-state matrix: row/column labels are the pairs
            // (q_i, v_j), matching the fronts' product-state labeling.
            Assert.Contains("q_0, v_0", step.Kron)
            Assert.Contains("q_0, v_3", step.Kron)
            // The highlighted cells are exactly the K cells that turned on for the first time
            // this iteration (the diff against the previous iteration's K). For the first step
            // that is all of step 1's K true cells; for later steps only the new ones.
            let k = result.Steps.[i].K

            let expected =
                if i = 0 then
                    trueCells k
                else
                    newCells (trueCells result.Steps.[i - 1].K) k

            Assert.Equal(expected |> Set.toList |> List.length, count @"\cellcolor{yellow}" step.Kron)

    [<Fact>]
    let ``kron product result draws block-splitting rules for the Kronecker structure`` () =
        let q = RSM.stateCount bookRsm
        let step = rendered.[0]

        // K is qn x qn = a q x q grid of n x n blocks. Thin rules separate the blocks: a
        // vertical rule at the block boundaries (after the last column of every block except the
        // last) and a horizontal rule after every n-th data row (q - 1 of each when q > 1; none
        // for a single block).
        let expectedRules = if q > 1 then q - 1 else 0

        Assert.Equal(expectedRules, count @"\vrule width 0.4pt" step.Kron)
        Assert.Equal(expectedRules, count @"\noalign{\hrule height 0.4pt}" step.Kron)

    [<Fact>]
    let ``kron product result header has an empty corner and aligned column labels`` () =
        let step = rendered.[0]
        // The K header row (the first pNiceMatrix row after \begin{pNiceMatrix}) must start with
        // an empty corner cell (the pNiceMatrix top-left), then the column labels in order — so
        // the first column is labeled (q_0, v_0) and the last is labeled, not shifted by one.
        // The K header row is the line of the K pNiceMatrix (the third, after the two operand
        // matrices). It carries the product-state pair labels (q_i, v_j) — find it by the first
        // pair label. It starts with an empty corner cell, then the column labels.
        let lines = step.Kron.Split('\n')

        let kHeaderLine = lines |> Array.tryFind (fun s -> s.Contains "q_0, v_0")

        Assert.NotNull(kHeaderLine)

        // cells[0] is the empty corner (the text before the first &), so it must be whitespace-only.
        let cells = kHeaderLine.Value.Split('&')
        let corner = cells.[0].Trim()
        Assert.Equal("", corner)
        let labeledCells = cells.[1..] |> Array.map (fun s -> s.Trim())
        // labeledCells[0] is the first column label; labeledCells[last] is the last.
        Assert.Equal("q_0, v_0", labeledCells.[0])

        Assert.True(
            labeledCells.[Array.length labeledCells - 1].StartsWith "q_",
            sprintf "last col unlabeled: %s" (labeledCells.[Array.length labeledCells - 1])
        )

    [<Fact>]
    let ``kron operands show set-of-symbols cells with dots for empty sets`` () =
        let step = rendered.[0]

        // M1 carries the terminal 'a', the nonterminal S (as \text{S}), and the terminal 'b'.
        Assert.Contains(@"\text{S}", step.Kron)
        Assert.Contains("a", step.Kron)

        // Empty set cells render as a dot (like empty cells in the other matrices), not
        // \varnothing. The book RSM's M1 is sparse, so its empty cells carry \cdot.
        Assert.DoesNotContain(@"\varnothing", step.Kron)
        Assert.True(count @"\cdot" step.Kron > 0, "expected \cdot for empty operand cells")

    [<Fact>]
    let ``fronts body has two pNiceMatrix blocks with product-state pair labels`` () =
        for step in rendered do
            Assert.Equal(2, count @"\begin{pNiceMatrix}" step.Fronts)
            // Rows and columns are product-state pairs (q_i, u_j); the row's nonterminal is
            // replaced by its start state q_0 (the book example has one block, S, start state 0).
            Assert.Contains("q_0, v_0", step.Fronts)
            Assert.DoesNotContain(@"\text{S}, v_", step.Fronts)

    [<Fact>]
    let ``final front highlights exactly the RSM final-state cells`` () =
        for i, step in List.indexed rendered do
            let rawStep = result.Steps.[i]
            // The highlighted cells are exactly the filled cells of the final front whose column
            // is a final-state column (the accept-reading of the MS-BFS result).
            let expected =
                [ for r in 0 .. (Matrix.rows rawStep.FinalFront - 1) do
                      for c in 0 .. (Matrix.cols rawStep.FinalFront - 1) do
                          if rawStep.FinalFront.[r, c] && Set.contains c rawStep.FinalStateCols then
                              (r, c) ]
                |> Set.ofList

            let yellow = count @"\cellcolor{yellow}" step.Fronts
            Assert.Equal(expected |> Set.toList |> List.length, yellow)
            // The book example's final state is 3 (last block), so the highlighted cells are in
            // the last n columns.
            for r, c in Set.toList expected do
                Assert.True(c >= (RSM.stateCount bookRsm - 1) * bookGraph.VertexCount)

    [<Fact>]
    let ``graph figure passes through`` () =
        let step0 = result.Steps.[0]

        let step =
            KroneckerStepViz.renderStep id ntName 4 step0 (trueCells step0.K) "DOTSRC" "TIKZSRC"

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

            let kNew =
                if i = 0 then
                    trueCells step.K
                else
                    newCells (trueCells result.Steps.[i - 1].K) step.K

            let renderedStep =
                KroneckerStepViz.renderStep id ntName bookGraph.VertexCount step kNew "" (graphFor step)

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
