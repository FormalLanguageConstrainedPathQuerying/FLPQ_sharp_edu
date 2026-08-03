module RnglrStepVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers

let private renderViz
    (ebnfText: string)
    (input: string list)
    : string list * string list * string list * string list * string list * string list =
    let rsm = RsmBuilder.buildRSMFromText ebnfText
    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph input
    let vertexCount = Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)
    let lrStateCount = Dfa.stateCount lrTable.Automaton

    let pathIndex, steps, vertexInfoArr =
        Rnglr.buildPathIndexWithSteps freshStart ersm graph

    let vertexInfo (idx: int) = vertexInfoArr.[idx]

    let viz =
        RnglrStepVisualizer.renderSteps string string lrTable lrStateCount vertexInfo steps pathIndex vertexCount graph

    let descriptorsTables = viz |> List.map (fun s -> s.DescriptorsTable)
    let newDescriptors = viz |> List.map (fun s -> s.NewDescriptors)
    let gssDots = viz |> List.map (fun s -> s.GssDot)
    let pathIndices = viz |> List.map (fun s -> s.PathIndex)
    let inputs = viz |> List.map (fun s -> s.Input)
    let lrTables = viz |> List.map (fun s -> s.LrTable)

    descriptorsTables, newDescriptors, gssDots, pathIndices, inputs, lrTables

[<Fact>]
let ``RNGLR golden for S->a a input a a — descriptors_table step 0`` () =
    let tables, _, _, _, _, _ = renderViz "S -> a a" [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_descriptors_table_step0.tex" tables.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — new_descriptors step 0`` () =
    let _, newDescs, _, _, _, _ = renderViz "S -> a a" [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_new_descriptors_step0.tex" newDescs.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — gss step 0`` () =
    let _, _, gssDots, _, _, _ = renderViz "S -> a a" [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_gss_step0.dot" gssDots.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — path_index step 0`` () =
    let _, _, _, pi, _, _ = renderViz "S -> a a" [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_path_index_step0.tex" pi.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — input step 0`` () =
    let _, _, _, _, inputs, _ = renderViz "S -> a a" [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_input_step0.dot" inputs.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — lr_table step 0`` () =
    let _, _, _, _, _, tables = renderViz "S -> a a" [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_lr_table_step0.tex" tables.[0]

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR GSS DOT vertex/edge label format for S->a a`` () =
    let _, _, gssDots, _, _, _ = renderViz "S -> a a" [ "a"; "a" ]

    Assert.NotEmpty gssDots

    for dot in gssDots do
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
    let _, _, _, _, _, tables = renderViz "S -> a a" [ "a"; "a" ]

    Assert.NotEmpty tables

    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_table_color_template.tex")

    for lrTable in tables do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath lrTable)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR input DOT compiles for S->a a`` () =
    let _, _, _, _, inputs, _ = renderViz "S -> a a" [ "a"; "a" ]

    Assert.NotEmpty inputs

    for inputDot in inputs do
        Assert.True(ExternalTools.compileDotString inputDot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``RNGLR GSS DOT compiles for S->a a`` () =
    let _, _, gssDots, _, _, _ = renderViz "S -> a a" [ "a"; "a" ]

    Assert.NotEmpty gssDots

    for gssDot in gssDots do
        Assert.True(ExternalTools.compileDotString gssDot)
