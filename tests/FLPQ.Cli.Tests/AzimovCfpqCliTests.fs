module AzimovCfpqCliTests

open System.IO
open System.Text.RegularExpressions
open Xunit
open FLPQ.CFPQ
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

// The book's Chapter 12 worked example (data/cfpq_example): a-edges 0->1, 1->2, 2->0;
// b-edges 0->3, 3->0; grammar S -> AB | AS1, A -> a, B -> b, S1 -> SB. L(S) = {a^n b^n : n >= 1}.
let private baseDir = System.AppContext.BaseDirectory
let private exampleQuery = Path.Combine(baseDir, "cfpq_example", "query.cnf")
let private exampleGraphDir = Path.Combine(baseDir, "cfpq_example", "graph")

// The book's T13 start matrix: the six reachable pairs.
let private bookPairs: (int * int) list =
    [ (0, 0); (0, 3); (1, 0); (1, 3); (2, 0); (2, 3) ]

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
let ``runAzimov reachability produces the root artifacts`` () =
    let (outDir, _) = runRunner AzimovCfpqReachabilityRunner.runAzimov false

    for f in [ "grammar_original.tex"; "grammar_cnf.tex"; "graph.tikz.tex"; "result.tex" ] do
        let path = Path.Combine(outDir, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

[<Fact>]
let ``runAzimov reachability writes one step dir per productive iteration with highlighted cells`` () =
    let (outDir, _) = runRunner AzimovCfpqReachabilityRunner.runAzimov false

    // The book's worked example: 13 loop applications, 12 productive iterations.
    let stepDirs = Directory.GetDirectories(outDir, "step_*")
    Assert.Equal(12, stepDirs.Length)

    for stepDir in stepDirs do
        let tablePath = Path.Combine(stepDir, "table.tex")
        Assert.True(File.Exists tablePath, sprintf "Missing: %s" tablePath)

        let content = File.ReadAllText(tablePath)
        // Every productive iteration highlights its new cells.
        Assert.Contains(@"\cellcolor{yellow}", content)
        // One matrix per nonterminal of the CNF (S, S1, A, B).
        for nt in [ "A"; "B"; "S"; "S1" ] do
            Assert.Contains(sprintf @"\textbf{%s}" nt, content)

    cleanup outDir

[<Fact>]
let ``runAzimov reachability result.tex lists the book's six reachable pairs`` () =
    let (outDir, _) = runRunner AzimovCfpqReachabilityRunner.runAzimov false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    Assert.Contains("Reachable pairs: 6", content)

    for i, j in bookPairs do
        Assert.Contains(sprintf "(v_%d, v_%d)" i j, content)

    // The four non-reachable off-diagonal pairs are absent.
    for pair in [ "(v_1, v_2)"; "(v_3, v_0)"; "(v_3, v_1)"; "(v_3, v_2)" ] do
        Assert.DoesNotContain(pair, content)

    cleanup outDir

[<Fact>]
let ``runAzimov singlePath result.tex lists one extracted walk per reachable pair`` () =
    let (outDir, _) = runRunner AzimovCfpqSinglePathRunner.runAzimov false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    let lines =
        content.Split('\n') |> Array.filter (fun l -> not (l.StartsWith("    ")))

    Assert.Equal(6, List.length (List.ofArray lines))

    // The height-2 derivation S -> AB: 2-a-0-b-3 is extracted exactly.
    Assert.Contains("v_2, v_3: v_2 -a-> v_0 -b-> v_3", content)

    // Every extracted edge sequence is a walk of the graph whose label word is in L(S).
    let grammar = CfpqInput.parseCnfFile exampleQuery
    let graph = CfpqInput.parseGraphDir exampleGraphDir

    for line in lines do
        let colon = line.IndexOf(": ")
        let pathPart = line.Substring(colon + 2)

        // The edge endpoints: v_start, then one (label, to) per arrow.
        let startVertex = int (Regex.Match(pathPart, @"^v_(\d+)").Groups.[1].Value)

        let edges =
            Regex.Matches(pathPart, @"-([a-z]+)-> v_(\d+)")
            |> Seq.map (fun m -> m.Groups.[1].Value, int m.Groups.[2].Value)
            |> Seq.toList

        // Continuity: each edge starts where the previous one ended.
        let mutable current = startVertex

        for label, target in edges do
            let hasEdge =
                match Map.tryFind label graph.Labels with
                | Some m -> m.[current, target]
                | None -> false

            Assert.True(hasEdge, sprintf "edge (%d,%s,%d) not in graph: %s" current label target line)
            current <- target

        let word = edges |> List.map (fun (label, _) -> Terminal label)
        let accepted = Valiant.parse Grammar.freshStringNonterminal grammar word
        Assert.True(accepted, sprintf "word of %s is not in L(S)" line)

    cleanup outDir

[<Fact>]
let ``runAzimov allPaths result.tex lists exactly the simple paths accepted by the grammar`` () =
    let (outDir, output) = runRunner AzimovCfpqAllPathsRunner.runAzimov false
    let content = File.ReadAllText(Path.Combine(outDir, "result.tex"))

    // Only (2,3) has a simple path: the word of the only simple walk from v_1 to v_3 is "aab",
    // which is not in L(S) = {a^n b^n}; (1,3) is reachable only via non-simple walks.
    Assert.Contains("v_0, v_0: 0 path(s)", content)
    Assert.Contains("v_0, v_3: 0 path(s)", content)
    Assert.Contains("v_1, v_0: 0 path(s)", content)
    Assert.Contains("v_1, v_3: 0 path(s)", content)
    Assert.Contains("v_2, v_0: 0 path(s)", content)
    Assert.Contains("v_2, v_3: 1 path(s)", content)
    Assert.Contains(@"    v_2 -a-> v_0 -b-> v_3", content)

    Assert.Contains("Azimov CFPQ (allPaths): 12 productive iterations, 6 reachable pairs, 1 simple paths", output)

    cleanup outDir

[<Fact>]
let ``runAzimov dot mode produces graph.dot`` () =
    let (outDir, _) = runRunner AzimovCfpqReachabilityRunner.runAzimov true

    let dotPath = Path.Combine(outDir, "graph.dot")
    Assert.True(File.Exists dotPath, "Missing: graph.dot")
    Assert.True(FileInfo(dotPath).Length > 0L, "Empty: graph.dot")

    cleanup outDir

[<Fact>]
let ``Program dispatch runs AzimovCFPQ with the default reachability semantics`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"
           "AzimovCFPQ"
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
let ``Program dispatch rejects allPaths for ArroyueloRPQ`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let regexpFile = Path.Combine(baseDir, "example_regexp.txt")
    let graphFile = Path.Combine(baseDir, "example_graph.txt")

    let args =
        [| "-a"
           "ArroyueloRPQ"
           "-q"
           regexpFile
           "-i"
           graphFile
           "-o"
           outDir
           "--semantics"
           "allPaths" |]

    let code = Program.runCli args

    Assert.Equal(1, code)

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

[<Fact>]
let ``Program dispatch rejects allPaths for BelyaninRPQ`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let regexpFile = Path.Combine(baseDir, "example_regexp.txt")
    let graphFile = Path.Combine(baseDir, "example_graph.txt")

    let args =
        [| "-a"
           "BelyaninRPQ"
           "-q"
           regexpFile
           "-i"
           graphFile
           "-o"
           outDir
           "--semantics"
           "allPaths" |]

    let code = Program.runCli args

    Assert.Equal(1, code)

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

[<Fact>]
[<Trait("Category", "Summary")>]
let ``AzimovCFPQ summary merged TeX compiles with lualatex`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let args =
        [| "-a"
           "AzimovCFPQ"
           "-q"
           exampleQuery
           "-i"
           exampleGraphDir
           "-o"
           outDir
           "-s" |]

    let code = Program.runCli args
    Assert.Equal(0, code)

    let texPath = Path.Combine(outDir, "results", "azimovcfpq", "azimovcfpq_merged.tex")
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
