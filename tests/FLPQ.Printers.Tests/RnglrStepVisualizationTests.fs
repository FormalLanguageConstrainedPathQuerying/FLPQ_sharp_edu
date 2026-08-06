module RnglrStepVisualizationTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers
open FLPQ.TestUtilities

[<Struct>]
type private RnglrVizData =
    { DescriptorsTables: string list
      NewDescriptors: string list
      GssDots: string list
      PathIndices: string list
      Inputs: string list
      LrTables: string list }

let private renderViz (input: string list) : RnglrVizData =
    let rsm =
        (LanguageRegistry.findGrammar LanguageRegistry.MiscTestGrammars "grammar_ebnf_aa").Rsm

    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph input
    let vertexCount = Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)
    let lrStateCount = Dfa.stateCount lrTable.Automaton

    let rnglrResult = Rnglr.buildPathIndexWithSteps freshStart ersm graph

    let vertexInfoArr = rnglrResult.VertexInfo
    let vertexInfo (idx: int) = vertexInfoArr.[idx]

    let viz =
        RnglrStepVisualizer.renderSteps
            string
            string
            lrTable
            lrStateCount
            vertexInfo
            rnglrResult.Steps
            rnglrResult.PathIndex
            vertexCount
            graph

    { DescriptorsTables = viz |> List.map (fun s -> s.DescriptorsTable)
      NewDescriptors = viz |> List.map (fun s -> s.NewDescriptors)
      GssDots = viz |> List.map (fun s -> s.GssDot)
      PathIndices = viz |> List.map (fun s -> s.PathIndex)
      Inputs = viz |> List.map (fun s -> s.Input)
      LrTables = viz |> List.map (fun s -> s.LrTable) }

[<Fact>]
let ``RNGLR golden for S->a a input a a — descriptors_table step 0`` () =
    let data = renderViz [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_descriptors_table_step0.tex" data.DescriptorsTables.[0]

[<Fact>]
let ``RNGLR golden for S->a a input a a — new_descriptors step 0`` () =
    let data = renderViz [ "a"; "a" ]
    GoldenHelpers.verifyGolden "rnglr_new_descriptors_step0.tex" data.NewDescriptors.[0]

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
