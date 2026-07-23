module GssDotVisualizationTests

open System.Text.RegularExpressions
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers

let private stripQuotes (s: string) =
    if s.Length >= 2 && s.[0] = '"' && s.[s.Length - 1] = '"' then
        s.Substring(1, s.Length - 2)
    else
        s

let private vertexLabelRegex = Regex(@"^\d+: \(\d+,\d+\)$")

let private edgeLabelRegex = Regex(@"^\d+,\d+ → \d+,\d+$")

let private renderGssDots (ebnfText: string) (input: string list) : string list =
    let rsm = RsmBuilder.buildRSMFromText ebnfText
    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph input
    let vertexCount = Graph.vertexCount graph
    let ersm = ExtendedRSM.create freshStart rsm
    let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm graph

    let vizSteps =
        GllStepVisualizer.renderSteps
            (SymbolTeX.toLaTeX string string)
            string
            string
            ersm
            steps
            pathIndex
            vertexCount
            graph

    vizSteps |> List.map (fun s -> s.GssDot)

let private verifyGssDots (dots: string list) =
    Assert.NotEmpty dots

    for dot in dots do
        let info = ExternalTools.compileDotStringToInfo dot

        for label in info.NodeLabels do
            Assert.Matches(vertexLabelRegex, stripQuotes label)

        for label in info.EdgeLabels do
            Assert.Matches(edgeLabelRegex, stripQuotes label)

        let blueCount =
            info.NodeFillColors
            |> List.filter (fun c -> c.Contains "lightblue")
            |> List.length

        if info.NodeCount > 0 then
            Assert.Equal(1, blueCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS DOT invariants hold for S->a a with input a a`` () =
    renderGssDots "S -> a a" [ "a"; "a" ] |> verifyGssDots

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS DOT invariants hold for S->a S b|eps with input a a b b`` () =
    renderGssDots "S -> a S b | eps" [ "a"; "a"; "b"; "b" ] |> verifyGssDots

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS DOT invariants hold for S->a S|a with input a a`` () =
    renderGssDots "S -> a S | a" [ "a"; "a" ] |> verifyGssDots

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS DOT invariants hold for S->a with input a`` () =
    renderGssDots "S -> a" [ "a" ] |> verifyGssDots

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``GSS DOT invariants hold for S->(a b)* with input a b a b`` () =
    renderGssDots "S -> (a b)*" [ "a"; "b"; "a"; "b" ] |> verifyGssDots
