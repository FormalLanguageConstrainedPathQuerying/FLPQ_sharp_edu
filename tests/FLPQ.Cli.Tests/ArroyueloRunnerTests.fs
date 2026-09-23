// Shares the ConsoleCapture collection with GllRunnerTests/RnglrRunnerTests so the modules'
// Console.SetOut calls never interleave (Console.Out is process-global).
[<Xunit.Collection("ConsoleCapture")>]
module ArroyueloRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.LinearAlgebra
open FLPQ.RPQ
open FLPQ.TestUtilities

let private baseDir = System.AppContext.BaseDirectory
let private exampleRegexp = Path.Combine(baseDir, "example_regexp.txt")
let private exampleGraph = Path.Combine(baseDir, "example_graph.txt")

let private runArroyueloRunner (useDot: bool) : string * string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () ->
            ArroyueloRunner.runArroyuelo exampleRegexp exampleGraph outDir useDot)

    outDir, output

let private cleanup (outDir: string) =
    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

// Runs the runner on inline regexp/graph text (temp files), mirroring the GllRunnerTests pattern.
let private runArroyueloWithText (regexpText: string) (graphText: string) (useDot: bool) : string * string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(Path.Combine(tmpDir, "query_regexp.txt"), regexpText)
    File.WriteAllText(Path.Combine(tmpDir, "query_graph.txt"), graphText)
    let outDir = Path.Combine(tmpDir, "output")

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () ->
            ArroyueloRunner.runArroyuelo
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

// Two start vertices: v_0 reaches v_1 via `a`; v_2 has no outgoing edges and reaches nothing.
let private multiSourceGraph = "0 2\n0 a 1\n1 b 2\n"

[<Fact>]
let ``runArroyuelo produces regexp.tex`` () =
    let (outDir, _) = runArroyueloRunner false
    let f = Path.Combine(outDir, "regexp.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runArroyuelo tikz mode produces dfa.tikz.tex with q_i state labels`` () =
    let (outDir, _) = runArroyueloRunner false
    let f = Path.Combine(outDir, "dfa.tikz.tex")
    Assert.True(File.Exists f)
    let content = File.ReadAllText f
    Assert.Contains("$q_0$", content)
    cleanup outDir

[<Fact>]
let ``runArroyuelo tikz mode produces graph.tikz.tex`` () =
    let (outDir, _) = runArroyueloRunner false
    let f = Path.Combine(outDir, "graph.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runArroyuelo produces result.tex`` () =
    let (outDir, _) = runArroyueloRunner false
    let f = Path.Combine(outDir, "result.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

// a / (b | c)* / a parses as RSeq(a, RSeq(RStar(RAlt(b, c)), a)): 8 post-order tree nodes.
[<Fact>]
let ``runArroyuelo produces one step dir per regexp tree node`` () =
    let (outDir, _) = runArroyueloRunner false
    let stepDirs = Directory.GetDirectories(outDir, "step_*")
    Assert.Equal(8, stepDirs.Length)

    for stepDir in stepDirs do
        for f in [ "matrices.tex"; "tree.tikz.tex"; "graph.tikz.tex" ] do
            let path = Path.Combine(stepDir, f)
            Assert.True(File.Exists path, sprintf "Missing: %s" f)
            Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

[<Fact>]
let ``runArroyuelo result.tex lists the hand-computed reachable vertices`` () =
    let (outDir, _) = runArroyueloRunner false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    // Source 0: a-edge to 1, then (b|c)* walks 1->2->3 (cycle 2<->3), then a-edge to 4 or 5.
    Assert.Contains("Source v_0: reachable v_4, v_5", content)

    cleanup outDir

[<Fact>]
let ``runArroyuelo result.tex reachable lines match ArroyueloRPQ.evaluate`` () =
    let (outDir, _) = runArroyueloRunner false

    let lines =
        File.ReadAllText(Path.Combine(outDir, "result.tex")) |> fun c -> c.Split('\n')

    let regexp = RpqInput.parseRegexpFile exampleRegexp
    let graph = GraphReader.parseGraphFile exampleGraph
    let boolResult = ArroyueloRPQ.evaluate graph regexp
    let vCount = Matrix.cols boolResult
    let sources = graph.StartStates |> Set.toArray |> Array.sort

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
let ``runArroyuelo status line reports step count and reachable vertices`` () =
    let (outDir, output) = runArroyueloRunner false
    Assert.Contains("Arroyuelo RPQ: 8 steps, sources: [v_0] -> reachable: [v_4, v_5]", output)
    cleanup outDir

[<Fact>]
let ``runArroyuelo dot mode produces dfa.dot and graph.dot`` () =
    let (outDir, _) = runArroyueloRunner true
    let dfaDot = Path.Combine(outDir, "dfa.dot")
    Assert.True(File.Exists dfaDot)
    Assert.Contains("q_0", File.ReadAllText dfaDot)

    let graphDot = Path.Combine(outDir, "graph.dot")
    Assert.True(File.Exists graphDot)
    Assert.True(FileInfo(graphDot).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runArroyuelo dot mode step 0 produces tree.dot and graph.dot`` () =
    let (outDir, _) = runArroyueloRunner true
    let step0 = Path.Combine(outDir, "step_0")

    for f in [ "matrices.tex"; "tree.dot"; "graph.dot" ] do
        let path = Path.Combine(step0, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

[<Fact>]
let ``runArroyuelo with multiple sources lists per-source reachable vertices`` () =
    let (outDir, _) = runArroyueloWithText "S -> a" multiSourceGraph false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))
    Assert.Contains("Source v_0: reachable v_1", content)
    Assert.Contains("Source v_2: reachable (none)", content)
    cleanupTmpDir outDir

[<Fact>]
let ``runArroyuelo status line with multiple sources lists all sources and the union of reachable vertices`` () =
    let (outDir, output) = runArroyueloWithText "S -> a" multiSourceGraph false
    Assert.Contains("Arroyuelo RPQ: 1 steps, sources: [v_0, v_2] -> reachable: [v_1]", output)
    cleanupTmpDir outDir

[<Fact>]
let ``runArroyuelo with a regexp matching no edges reports no reachable vertices`` () =
    let (outDir, output) = runArroyueloWithText "S -> d" multiSourceGraph false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))
    Assert.Contains("Source v_0: reachable (none)", content)
    Assert.Contains("Source v_2: reachable (none)", content)
    Assert.Contains("Arroyuelo RPQ: 1 steps, sources: [v_0, v_2] -> reachable: []", output)
    cleanupTmpDir outDir
