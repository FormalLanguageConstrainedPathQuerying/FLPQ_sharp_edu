// Shares the ConsoleCapture collection with the other runner tests so the modules'
// Console.SetOut calls never interleave (Console.Out is process-global).
[<Xunit.Collection("ConsoleCapture")>]
module BelyaninReachabilityRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.TestUtilities

let private baseDir = System.AppContext.BaseDirectory
let private exampleRegexp = Path.Combine(baseDir, "example_regexp.txt")
let private exampleGraph = Path.Combine(baseDir, "example_graph.txt")
let private bigCyclicGraph = Path.Combine(baseDir, "example_graph_demo_big.txt")
let private smallCyclicGraph = Path.Combine(baseDir, "example_graph_demo_small.txt")
let private cyclicRegexp = Path.Combine(baseDir, "example_regexp_demo.txt")

let private runBelyaninRunner (useDot: bool) : string * string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () ->
            BelyaninReachabilityRunner.runBelyanin exampleRegexp exampleGraph outDir useDot)

    outDir, output

let private cleanup (outDir: string) =
    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

/// Runs the runner on inline regexp/graph text (temp files), mirroring the other runner tests.
let private runBelyaninWithText (regexpText: string) (graphText: string) (useDot: bool) : string * string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(Path.Combine(tmpDir, "query_regexp.txt"), regexpText)
    File.WriteAllText(Path.Combine(tmpDir, "query_graph.txt"), graphText)
    let outDir = Path.Combine(tmpDir, "output")

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () ->
            BelyaninReachabilityRunner.runBelyanin
                (Path.Combine(tmpDir, "query_regexp.txt"))
                (Path.Combine(tmpDir, "query_graph.txt"))
                outDir
                useDot)

    outDir, output

let private cleanupTmpDir (outDir: string) =
    let tmpDir = Path.GetDirectoryName(outDir)

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

/// Runs the runner on the demo files (cyclic regression), returns (outDir, output).
let private runCyclic (graphFile: string) : string * string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () ->
            BelyaninReachabilityRunner.runBelyanin cyclicRegexp graphFile outDir false)

    outDir, output

// Two start vertices: v_0 reaches v_1 via `a`; v_2 has no outgoing edges and reaches nothing.
let private multiSourceGraph = "0 2\n0 a 1\n1 b 2\n"

[<Fact>]
let ``runBelyanin reachability produces root artifacts`` () =
    let (outDir, _) = runBelyaninRunner false

    for f in [ "regexp.tex"; "dfa.tikz.tex"; "graph.tikz.tex"; "result.tex" ] do
        let path = Path.Combine(outDir, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

[<Fact>]
let ``runBelyanin reachability produces one step dir per trace step in both modes`` () =
    let regexp = RpqInput.parseRegexpFile exampleRegexp
    let dfa = Regexp.toDfa regexp
    let graph = GraphReader.parseGraphFile exampleGraph
    let traceSteps, _ = BelyaninRPQ.evaluateReachabilityWithTrace dfa graph

    for useDot in [ false; true ] do
        let (outDir, _) = runBelyaninRunner useDot
        let ext = if useDot then "dot" else "tikz.tex"

        Assert.Equal(List.length traceSteps, Array.length (Directory.GetDirectories(outDir, "step_*")))

        for i in 0 .. List.length traceSteps - 1 do
            let step = traceSteps.[i]
            let stepDir = Path.Combine(outDir, sprintf "step_%d" i)

            let expectedFiles =
                [ "frontier_start.tex"
                  "visited_start.tex"
                  sprintf "automaton_start.%s" ext
                  sprintf "graph_start.%s" ext ]
                @ (if step.IsInit then
                       []
                   else
                       [ "frontier_end.tex"
                         "visited_end.tex"
                         sprintf "automaton_end.%s" ext
                         sprintf "graph_end.%s" ext ])

            for f in expectedFiles do
                let path = Path.Combine(stepDir, f)
                Assert.True(File.Exists path, sprintf "Missing: %s" f)
                Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

            for j in 0 .. List.length step.Labels - 1 do
                for f in
                    [ sprintf "label_%d_select.tex" j
                      sprintf "label_%d_extend.tex" j
                      sprintf "label_%d_automaton.%s" j ext
                      sprintf "label_%d_graph.%s" j ext ] do
                    let path = Path.Combine(stepDir, f)
                    Assert.True(File.Exists path, sprintf "Missing: %s" f)
                    Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

        cleanup outDir

[<Fact>]
let ``runBelyanin reachability result.tex reachable lines match BelyaninRPQ.evaluate`` () =
    let (outDir, _) = runBelyaninRunner false

    let lines = File.ReadAllText(Path.Combine(outDir, "result.tex")).Split('\n')

    let regexp = RpqInput.parseRegexpFile exampleRegexp
    let dfa = Regexp.toDfa regexp
    let graph = GraphReader.parseGraphFile exampleGraph
    let boolResult = BelyaninRPQ.evaluate dfa graph
    let vCount = Matrix.cols boolResult
    let sources = graph.StartStates |> Set.toArray

    for i in 0 .. sources.Length - 1 do
        let s = sources.[i]

        let expected =
            set
                [ for j in 0 .. vCount - 1 do
                      if boolResult.[i, j] then
                          j ]

        let prefix = sprintf "Source v_%d: " s
        let line = lines |> Array.find (fun l -> l.StartsWith prefix)

        let actual =
            if line.Contains("reachable (none)") then
                Set.empty
            else
                line.Substring(line.IndexOf("reachable ") + "reachable ".Length)
                |> fun rest -> rest.Split(',')
                |> Array.map (fun t -> int (t.Trim().Substring(2)))
                |> Set.ofArray

        Assert.Equal<Set<int>>(expected, actual)

    cleanup outDir

[<Fact>]
let ``runBelyanin reachability status line reports step count and reachable vertices`` () =
    let (outDir, output) = runBelyaninRunner false
    Assert.Contains("Belyanin RPQ (reachability): ", output)
    Assert.Contains("sources: [v_0] -> reachable: [v_4, v_5]", output)
    cleanup outDir

[<Fact>]
let ``runBelyanin reachability with multiple sources lists per-source reachable vertices`` () =
    let (outDir, _) = runBelyaninWithText "S -> a" multiSourceGraph false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))
    Assert.Contains("Source v_0: reachable v_1", content)
    Assert.Contains("Source v_2: reachable (none)", content)
    cleanupTmpDir outDir

[<Fact>]
let ``runBelyanin reachability with a regexp matching no edges reports no reachable vertices`` () =
    let (outDir, output) = runBelyaninWithText "S -> d" multiSourceGraph false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))
    Assert.Contains("Source v_0: reachable (none)", content)
    Assert.Contains("Source v_2: reachable (none)", content)
    Assert.Contains("reachable: []", output)
    cleanupTmpDir outDir

// --- Cyclic regressions (the reported bug) ---

[<Fact>]
let ``runBelyanin reachability reaches v_4 on the big cyclic graph`` () =
    let (outDir, output) = runCyclic bigCyclicGraph
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    Assert.Contains("Source v_0: reachable v_4", content)
    Assert.Contains("reachable: [v_4]", output)
    cleanup outDir

[<Fact>]
let ``runBelyanin reachability reaches v_3 on the small cyclic graph`` () =
    let (outDir, output) = runCyclic smallCyclicGraph
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    Assert.Contains("Source v_0: reachable v_3", content)
    Assert.Contains("reachable: [v_3]", output)
    cleanup outDir

[<Fact>]
let ``runBelyanin reachability dot mode produces dfa.dot and graph.dot`` () =
    let (outDir, _) = runBelyaninRunner true
    let dfaDot = Path.Combine(outDir, "dfa.dot")
    Assert.True(File.Exists dfaDot)
    Assert.Contains("q_0", File.ReadAllText dfaDot)

    let graphDot = Path.Combine(outDir, "graph.dot")
    Assert.True(File.Exists graphDot)
    Assert.True(FileInfo(graphDot).Length > 0L)
    cleanup outDir
