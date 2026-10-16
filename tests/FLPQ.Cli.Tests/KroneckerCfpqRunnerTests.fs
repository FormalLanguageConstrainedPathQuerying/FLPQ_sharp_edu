module KroneckerCfpqRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.Printers

// The TensorProduct.tex example (data/cfpq_kron_example): a-edges 0->1, 1->2, 2->0; b-edges 2->3,
// 3->2; query S -> (a S b) | (a b), so L(S) = {a^n b^n : n >= 1}.
let private baseDir = System.AppContext.BaseDirectory
let private exampleQuery = Path.Combine(baseDir, "cfpq_kron_example", "query.ebnf")
let private exampleGraphDir = Path.Combine(baseDir, "cfpq_kron_example", "graph")

// The six reachable pairs: the a-phase must land on vertex 2 (the only vertex of {0,1,2} with a
// b-edge out), so i + n ≡ 2 (mod 3); the b-phase then alternates 2<->3, ending at 2 (n even) or 3
// (n odd). Vertex 3 has no a-edge out, so no accepted path starts there.
let private kronPairs: (int * int) list =
    [ (0, 2); (0, 3); (1, 2); (1, 3); (2, 2); (2, 3) ]

let private runRunner (runner: (string -> string -> string -> bool -> unit)) (useDot: bool) : string * string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () -> runner exampleQuery exampleGraphDir outDir useDot)

    outDir, output

let private cleanup (outDir: string) =
    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

[<Fact>]
let ``runKronecker reachability produces the root artifacts`` () =
    let (outDir, _) = runRunner KroneckerCfpqRunner.runKronecker false

    for f in [ "rsm.tikz.tex"; "graph.tikz.tex"; "result.tex" ] do
        let path = Path.Combine(outDir, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

[<Fact>]
let ``runKronecker reachability writes one step dir per productive iteration with highlighted cells`` () =
    let (outDir, _) = runRunner KroneckerCfpqRunner.runKronecker false

    let stepDirs = Directory.GetDirectories(outDir, "step_*")
    Assert.True(stepDirs.Length >= 1, "Expected at least one productive iteration")

    for stepDir in stepDirs do
        let tablePath = Path.Combine(stepDir, "table.tex")
        Assert.True(File.Exists tablePath, sprintf "Missing: %s" tablePath)

        let content = File.ReadAllText(tablePath)
        // Every productive iteration highlights its new cells.
        Assert.Contains(@"\cellcolor{yellow}", content)
        // One matrix for the single nonterminal S.
        Assert.Contains(@"\textbf{S}", content)

    cleanup outDir

[<Fact>]
let ``runKronecker reachability result.tex lists the six reachable pairs`` () =
    let (outDir, _) = runRunner KroneckerCfpqRunner.runKronecker false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    Assert.Contains("Reachable pairs: 6", content)

    for i, j in kronPairs do
        Assert.Contains(sprintf "(v_%d, v_%d)" i j, content)

    // Vertex 3 has no a-edge out (no accepted path starts there); (0,1) and (1,1) are absent.
    for pair in [ "(v_3, v_0)"; "(v_3, v_3)"; "(v_0, v_1)"; "(v_1, v_1)" ] do
        Assert.DoesNotContain(pair, content)

    cleanup outDir

[<Fact>]
let ``runKronecker dot mode produces graph.dot and rsm.dot`` () =
    let (outDir, _) = runRunner KroneckerCfpqRunner.runKronecker true

    for f in [ "graph.dot"; "rsm.dot" ] do
        let path = Path.Combine(outDir, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

[<Fact>]
let ``Program dispatch runs KroneckerCFPQ with the default reachability semantics`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"
           "KroneckerCFPQ"
           "-q"
           exampleQuery
           "-i"
           exampleGraphDir
           "-o"
           outDir |]

    let code = Program.runCli args

    Assert.Equal(0, code)
    Assert.True(File.Exists(Path.Combine(outDir, "result.tex")), "Missing: result.tex")

    cleanup outDir

[<Fact>]
let ``Program dispatch rejects simplePath for KroneckerCFPQ`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"
           "KroneckerCFPQ"
           "-q"
           exampleQuery
           "-i"
           exampleGraphDir
           "-o"
           outDir
           "--semantics"
           "simplePath" |]

    let code = Program.runCli args

    Assert.Equal(1, code)

    cleanup outDir

[<Fact>]
let ``Program dispatch rejects allPaths for KroneckerCFPQ`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"
           "KroneckerCFPQ"
           "-q"
           exampleQuery
           "-i"
           exampleGraphDir
           "-o"
           outDir
           "--semantics"
           "allPaths" |]

    let code = Program.runCli args

    Assert.Equal(1, code)

    cleanup outDir

[<Fact>]
[<Trait("Category", "Summary")>]
let ``KroneckerCFPQ summary merged TeX compiles with lualatex`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let args =
        [| "-a"
           "KroneckerCFPQ"
           "-q"
           exampleQuery
           "-i"
           exampleGraphDir
           "-o"
           outDir
           "-s" |]

    let code = Program.runCli args
    Assert.Equal(0, code)

    let texPath =
        Path.Combine(outDir, "results", "kroneckercfpq", "kroneckercfpq_merged.tex")

    Assert.True(File.Exists texPath, sprintf "Expected merged TeX not found: %s" texPath)

    try
        Assert.True(
            ExternalTools.compileTexFile texPath (Path.GetDirectoryName texPath),
            sprintf "Merged TeX failed to compile with lualatex: %s" texPath
        )
    finally
        try
            Directory.Delete(outDir, true)
        with _ ->
            ()
