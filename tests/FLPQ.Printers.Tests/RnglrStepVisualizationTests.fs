module RnglrStepVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers
open FLPQ.TestUtilities

[<Struct>]
type private RnglrVizData =
    { GssDots: string list
      GssTikzs: string list
      PathIndices: string list
      Inputs: string list
      LrTables: string list
      PositionOf: int -> int }

let private renderRnglr (rsm: RSM<string, string>) (input: string list) : RnglrVizData =
    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph input
    let ersm = ExtendedRSM.create freshStart rsm
    let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)

    let rnglrResult = Rnglr.buildPathIndexWithSteps freshStart ersm graph

    let vertexInfoArr = rnglrResult.VertexInfo
    let vertexInfo (idx: int) = vertexInfoArr.[idx]
    let positionOf (idx: int) = snd vertexInfoArr.[idx]

    let viz =
        RnglrStepVisualizer.renderSteps string string lrTable vertexInfo rnglrResult.Steps rnglrResult.PathIndex graph

    { GssDots = viz |> List.map (fun s -> s.GssDot)
      GssTikzs = viz |> List.map (fun s -> s.GssTikz)
      PathIndices = viz |> List.map (fun s -> s.PathIndex)
      Inputs = viz |> List.map (fun s -> s.Input)
      LrTables = viz |> List.map (fun s -> s.LrTable)
      PositionOf = positionOf }

let private renderViz (input: string list) : RnglrVizData =
    renderRnglr (LanguageRegistry.findGrammar LanguageRegistry.DoubleA "singleRule").Rsm input

[<Fact>]
let ``RNGLR golden for S->a a input a a — gss step 0`` () =
    let data = renderViz [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_gss_step0.dot" data.GssDots.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — path_index step 0`` () =
    let data = renderViz [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_path_index_step0.tex" data.PathIndices.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — input step 0`` () =
    let data = renderViz [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_input_step0.dot" data.Inputs.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — lr_table step 0`` () =
    let data = renderViz [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_lr_table_step0.tex" data.LrTables.[0]

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR GSS DOT vertex/edge label format for S->a a`` () =
    let data = renderViz [ "a"; "a" ]

    Assert.NotEmpty data.GssDots

    for dot in data.GssDots do
        let info = ExternalTools.compileDotStringToInfo dot

        for label in info.NodeLabels do
            Assert.Matches(GoldenHelpers.vertexLabelRegex, GoldenHelpers.stripQuotes label)

        for label in info.EdgeLabels do
            Assert.Matches(GoldenHelpers.rnglrEdgeLabelRegex, GoldenHelpers.stripQuotes label)

        let blueCount =
            info.NodeFillColors |> List.filter (fun c -> c.Contains "blue") |> List.length

        if info.NodeCount > 0 then
            Assert.Equal(0, blueCount)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``RNGLR LR table TeX compiles for S->a a`` () =
    let data = renderViz [ "a"; "a" ]

    Assert.NotEmpty data.LrTables

    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_table_color_template.tex")

    for lrTable in data.LrTables do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath lrTable)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR input DOT compiles for S->a a`` () =
    let data = renderViz [ "a"; "a" ]

    Assert.NotEmpty data.Inputs

    for inputDot in data.Inputs do
        Assert.True(ExternalTools.compileDotString inputDot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR GSS DOT compiles for S->a a`` () =
    let data = renderViz [ "a"; "a" ]

    Assert.NotEmpty data.GssDots

    for gssDot in data.GssDots do
        Assert.True(ExternalTools.compileDotString gssDot)

/// Grammar with multiple reduction paths per input position (S -> a | S S | S S S), so GSS
/// steps contain several nodes at the same input position — the case the layering must handle.
let private tripleA =
    LanguageRegistry.findGrammar LanguageRegistry.APlus "ambiguousWithSingleRule"

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR GSS DOT places same-input-position nodes on one layer with position 0 rightmost`` () =
    let data = renderRnglr tripleA.Rsm [ "a"; "a"; "a" ]

    let positions = data.PositionOf
    let checkedCount = ref 0

    let checkStep (dot: string) : unit =
        let nodePos = ExternalTools.compileDotStringToNodePositions dot

        if not (Map.isEmpty nodePos) then
            // Group rendered nodes by input position; each position maps to its x-coordinate(s).
            let columns =
                nodePos
                |> Map.toSeq
                |> Seq.map (fun (name, (x, _y)) -> (positions (int (name.Substring(1))), x))
                |> Seq.groupBy fst
                |> Seq.map (fun (p, xs) -> (p, xs |> Seq.map snd))
                |> Map.ofSeq

            // Invariant 1: every position is a single column (all its nodes share one x).
            for pos in Seq.sort columns.Keys do
                let xs = columns.[pos] |> Seq.distinct |> List.ofSeq
                Assert.True(List.length xs <= 1, sprintf "position %d spans multiple layers: %A" pos xs)

            // Invariant 2: columns are strictly ordered left-to-right by decreasing position.
            let sorted =
                columns
                |> Map.toSeq
                |> Seq.map (fun (p, xs) -> (p, xs |> Seq.max))
                |> List.ofSeq
                |> List.sortBy fst

            for i in 0 .. sorted.Length - 2 do
                let p1: int = fst sorted.[i]
                let x1: float = snd sorted.[i]
                let p2: int = fst sorted.[i + 1]
                let x2: float = snd sorted.[i + 1]
                Assert.True(x1 > x2, sprintf "position %d (x=%f) must be right of position %d (x=%f)" p1 x1 p2 x2)

            // Invariant 3: the rightmost column is input position 0.
            let rightmost: int = sorted |> List.maxBy snd |> fst
            Assert.Equal(0, rightmost)

            checkedCount := !checkedCount + 1

    for dot in data.GssDots do
        checkStep dot

    Assert.True(!checkedCount > 0, "no non-empty GSS step was checked")

[<Fact>]
let ``RNGLR GSS TikZ grows left and groups each input position into one same-layer collection`` () =
    let data = renderRnglr tripleA.Rsm [ "a"; "a"; "a" ]

    let positions = data.PositionOf
    let checkedCount = ref 0

    let checkStep (tikz: string) : unit =
        if tikz.Contains("[same layer]") then
            Assert.Contains("grow=left", tikz)
            Assert.DoesNotContain("grow'=right", tikz)

            // Parse each "{ [same layer] vA, vB };" collection into a Set<int>.
            let marker = "[same layer]"

            let actualGroups =
                tikz.Split('\n')
                |> Seq.filter (fun l -> l.Contains(marker))
                |> Seq.map (fun l ->
                    let inner = l.Substring(l.IndexOf(marker) + marker.Length)

                    inner.Split(',')
                    |> Array.map (fun t ->
                        let tok = t.Trim().Replace("}", "").Replace(";", "").Trim()
                        int (tok.Substring(1)))
                    |> Set.ofArray)
                |> List.ofSeq

            // Declared nodes come from the vertex declarations, independently of the collections.
            let declared =
                tikz.Split('\n')
                |> Seq.filter (fun l -> l.Contains("[as="))
                |> Seq.map (fun l ->
                    let t = l.TrimStart()
                    int (t.Substring(0, t.IndexOf(' ')).Substring(1)))
                |> Set.ofSeq

            // Expected: group the declared nodes by input position.
            let expectedGroups =
                declared |> Seq.groupBy positions |> Seq.map (snd >> Set.ofSeq) |> List.ofSeq

            let minOf (s: Set<int>) = s |> Set.toSeq |> Seq.min
            let actualSorted = actualGroups |> List.sortBy minOf
            let expectedSorted = expectedGroups |> List.sortBy minOf

            let groupsMatch =
                List.length actualSorted = List.length expectedSorted
                && List.forall2 (=) actualSorted expectedSorted

            Assert.True(
                groupsMatch,
                sprintf "same-layer collections %A do not match per-position groups %A" actualSorted expectedSorted
            )

            checkedCount := !checkedCount + 1

    for tikz in data.GssTikzs do
        checkStep tikz

    Assert.True(!checkedCount > 0, "no GSS TikZ step with layers was checked")

[<Fact>]
let ``RNGLR golden for S->a|SS|SSS input a a a — gss last step`` () =
    let data = renderRnglr tripleA.Rsm [ "a"; "a"; "a" ]

    GoldenHelpers.verifyGolden "rnglr_gss_aaa_last.dot" data.GssDots.[data.GssDots.Length - 1]
