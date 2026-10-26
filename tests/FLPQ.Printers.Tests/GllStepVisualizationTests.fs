module GllStepVisualizationTests

open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers
open FLPQ.TestUtilities

[<Struct>]
type private GllVizData =
    { GssDots: string list
      GssTikzs: string list
      Steps: GLLParsingStep<string, string> list }

let private renderGll (rsm: RSM<string, string>) (input: string list) : GllVizData =
    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph input
    let ersm = ExtendedRSM.create freshStart rsm
    let (pi, steps) = GLL.buildPathIndexWithSteps freshStart ersm graph

    let viz =
        GllStepVisualizer.renderSteps string string ersm steps pi (Graph.vertexCount graph) graph

    { GssDots = viz |> List.map (fun s -> s.GssDot)
      GssTikzs = viz |> List.map (fun s -> s.GssTikz)
      Steps = steps }

let private visibleStoredPopIndex = TestHelpers.gllVisibleStoredPopIndex

[<Fact>]
let ``GLL golden for S -> a S | S S | eps stored-pop step — gss dot`` () =
    let data =
        renderGll (LanguageRegistry.findGrammar LanguageRegistry.AStar "aSSWithEps").Rsm [ "a"; "a" ]

    let idx = visibleStoredPopIndex data.Steps
    Assert.True(idx >= 0, "Expected a visible stored-pop step")
    GoldenHelpers.verifyGolden "gll_gss_stored_pop_a_s_ss_eps.dot" data.GssDots.[idx]

[<Fact>]
let ``GLL golden for S -> a S | S S | eps stored-pop step — gss tikz`` () =
    let data =
        renderGll (LanguageRegistry.findGrammar LanguageRegistry.AStar "aSSWithEps").Rsm [ "a"; "a" ]

    let idx = visibleStoredPopIndex data.Steps
    Assert.True(idx >= 0, "Expected a visible stored-pop step")
    GoldenHelpers.verifyGolden "gll_gss_stored_pop_a_s_ss_eps.tikz" data.GssTikzs.[idx]

[<Fact>]
let ``GLL golden for S -> C C, C -> a | eps stored-pop step — gss dot`` () =
    let data =
        renderGll (LanguageRegistry.findGrammar LanguageRegistry.AStar "twoBlockConcat").Rsm [ "a" ]

    let idx = visibleStoredPopIndex data.Steps
    Assert.True(idx >= 0, "Expected a visible stored-pop step")
    GoldenHelpers.verifyGolden "gll_gss_stored_pop_two_block_concat.dot" data.GssDots.[idx]

[<Fact>]
let ``GLL golden for S -> C C, C -> a | eps stored-pop step — gss tikz`` () =
    let data =
        renderGll (LanguageRegistry.findGrammar LanguageRegistry.AStar "twoBlockConcat").Rsm [ "a" ]

    let idx = visibleStoredPopIndex data.Steps
    Assert.True(idx >= 0, "Expected a visible stored-pop step")
    GoldenHelpers.verifyGolden "gll_gss_stored_pop_two_block_concat.tikz" data.GssTikzs.[idx]

[<Fact>]
let ``GLL stored-pop step renders orange fill for S -> a S | S S | eps`` () =
    let data =
        renderGll (LanguageRegistry.findGrammar LanguageRegistry.AStar "aSSWithEps").Rsm [ "a"; "a" ]

    let idx = visibleStoredPopIndex data.Steps
    Assert.True(idx >= 0, "Expected a visible stored-pop step")
    Assert.Contains("fillcolor=orange", data.GssDots.[idx])

[<Fact>]
let ``GLL stored-pop step renders orange fill for S -> C C, C -> a | eps`` () =
    let data =
        renderGll (LanguageRegistry.findGrammar LanguageRegistry.AStar "twoBlockConcat").Rsm [ "a" ]

    let idx = visibleStoredPopIndex data.Steps
    Assert.True(idx >= 0, "Expected a visible stored-pop step")
    Assert.Contains("fillcolor=orange", data.GssDots.[idx])
