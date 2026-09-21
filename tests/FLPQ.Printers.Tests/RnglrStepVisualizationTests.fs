module RnglrStepVisualizationTests

open System.IO
open FSharpPlus.Data
open Xunit
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities

[<Struct>]
type private RnglrVizData =
    { GssDots: string list
      GssTikzs: string list
      PathIndices: string list
      Inputs: string list
      LrTables: string list
      Actions: RnglrAction<string, string> option list
      PositionOf: int -> int }

[<Struct>]
type private ReduceCells =
    { Nt: string
      TriggerLrState: int
      GotoLrState: int }

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
      Actions = rnglrResult.Steps |> List.map (fun s -> s.Action)
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

/// Splits the rendered tabular row of the given LR state into cells. Rows are located by their
/// leading `\hline <state>` marker; cells are separated by " & ".
let private rowCells (tex: string) (state: int) : string list =
    let row =
        tex.Split('\n')
        |> Seq.tryFind (fun l -> l.StartsWith(sprintf @"\hline %d " state))
        |> function
            | Some l -> l
            | None -> failwithf "no tabular row for state %d" state

    row.Split([| " & " |], System.StringSplitOptions.None) |> List.ofArray

/// The header row cells (state name, terminals, $, nonterminals) of a rendered tabular.
let private headerCells (tex: string) : string list =
    let headerLine = tex.Split('\n') |> Seq.find (fun l -> l.EndsWith(@"\\ \hline"))
    let withoutTerminator = headerLine.Substring(0, headerLine.Length - 9)

    withoutTerminator.Split([| " & " |], System.StringSplitOptions.None)
    |> Array.map (fun c -> c.Trim())
    |> List.ofArray

[<Fact>]
let ``initial substep table has no cell highlights`` () =
    let data = renderViz [ "a"; "a" ]
    Assert.Equal((None: RnglrAction<string, string> option), data.Actions.[0])
    Assert.DoesNotContain(@"\cellcolor", data.LrTables.[0])

[<Fact>]
let ``shift substep table highlights exactly one green cell on the action row and column`` () =
    let data = renderViz [ "a"; "a" ]

    let shiftIdx =
        data.Actions
        |> List.findIndex (function
            | Some(RnglrAction.Shift _) -> true
            | _ -> false)

    let (t, lrState) =
        match data.Actions.[shiftIdx].Value with
        | RnglrAction.Shift(Terminal t, s) -> (t, s)
        | _ -> failwith "expected a shift action"

    let table = data.LrTables.[shiftIdx]

    Assert.Equal(1, GoldenHelpers.countOccurrences table @"\cellcolor{green!20}")
    Assert.Equal(0, GoldenHelpers.countOccurrences table @"\cellcolor{red!20}")

    let cells = rowCells table lrState
    let greenCol = cells |> List.findIndex (fun c -> c.Contains @"\cellcolor{green!20}")
    let expectedCol = headerCells table |> List.findIndex (fun c -> c = string t)
    Assert.Equal(expectedCol, greenCol)

[<Fact>]
let ``reduce substep table highlights exactly two red cells: goto row and trigger row $ column`` () =
    let data = renderViz [ "a"; "a" ]

    let reduceIdx =
        data.Actions
        |> List.findIndex (function
            | Some(RnglrAction.Reduce _) -> true
            | _ -> false)

    let cells =
        match data.Actions.[reduceIdx].Value with
        | RnglrAction.Reduce(Nonterminal nt, triggerLrState, gotoLrState) ->
            { ReduceCells.Nt = nt
              TriggerLrState = triggerLrState
              GotoLrState = gotoLrState }
        | _ -> failwith "expected a reduce action"

    let table = data.LrTables.[reduceIdx]

    Assert.Equal(2, GoldenHelpers.countOccurrences table @"\cellcolor{red!20}")
    Assert.Equal(0, GoldenHelpers.countOccurrences table @"\cellcolor{green!20}")

    // The $ column is the last action-part column, right before the goto part; in TeX source
    // the dollar sign is escaped as \$.
    let header = headerCells table
    let dollarCol = header |> List.findIndex (fun c -> c = @"\$")
    let ntCol = header |> List.findIndex (fun c -> c = cells.Nt)

    let gotoRow = rowCells table cells.GotoLrState

    Assert.True(
        gotoRow.[ntCol].Contains @"\cellcolor{red!20}",
        "the red goto cell must sit in the nonterminal column of the goto row"
    )

    let triggerRow = rowCells table cells.TriggerLrState

    Assert.True(
        triggerRow.[dollarCol].Contains @"\cellcolor{red!20}",
        "the red action cell must sit in the $ column of the trigger row"
    )

[<Fact>]
let ``RNGLR golden for S->a a input a a — lr_table first shift substep`` () =
    let data = renderViz [ "a"; "a" ]

    let shiftIdx =
        data.Actions
        |> List.findIndex (function
            | Some(RnglrAction.Shift _) -> true
            | _ -> false)

    GoldenHelpers.verifyGolden "rnglr_lr_table_substep_shift.tex" data.LrTables.[shiftIdx]

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


/// Hand-built step: edge (0,1) carries Epsilon + terminal 'a'; edge (1,2) is active but
/// missing from the edge-symbol map — exercises both arms of the label printers.
[<Fact>]
let ``renderStep labels epsilon edges and leaves missing edge symbols empty`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.DoubleA "singleRule").Rsm
    let freshStart = Nonterminal "S'"
    let graph = GLL.stringToGraph [ "a" ]
    let ersm = ExtendedRSM.create freshStart rsm
    let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)

    let step: RnglrParsingStep<string, string> =
        { ActiveGssVertices = set [ 0; 1 ]
          ActiveGssEdges = set [ (0, 1); (1, 2) ]
          ActiveGssEdgeSymbols = Map.ofList [ ((0, 1), NonEmptySet.ofList [ Symbol.Epsilon; Symbol.T(Terminal "a") ]) ]
          NewGssVertices = set [ 1 ]
          NewGssEdges = set [ (0, 1) ]
          PathIndexMatrix = Matrix.create 4 4 (fun _ _ -> Set.empty: Set<PathIndexEntry<string, string>>)
          ChangedCells = set [ (0, 1) ]
          InputVertex = 0
          Action = Some(RnglrAction.Shift(Terminal "a", 0))
          PassingReductionVertices = Set.empty }

    let pathIndex: PathIndex<string, string> =
        { Matrix = step.PathIndexMatrix
          StateCount = 2
          VertexCount = 2 }

    let vertexInfo (idx: int) = (0, idx % 2)

    let viz =
        RnglrStepVisualizer.renderStep string string lrTable vertexInfo step pathIndex graph

    // Epsilon symbol appears in both the DOT and TikZ edge labels of edge (0,1);
    // the TikZ label is math-mode TeX, not escaped literal source (as in RsmTikz).
    Assert.Contains("ε", viz.GssDot)
    Assert.Contains("$\\varepsilon$", viz.GssTikz)
    Assert.DoesNotContain(@"\textbackslash varepsilon", viz.GssTikz)

    // Edge (1,2) has no entry in ActiveGssEdgeSymbols -> empty label.
    Assert.Contains("v1 -> v2 [label=\"\"];", viz.GssDot)
