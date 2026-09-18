module CykRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.TestUtilities

let private baseDir = System.AppContext.BaseDirectory

let private exampleInput = Path.Combine(baseDir, "example_input.txt")

// Dyck1 reject string "a a b" from the registry; its final CYK step has no highlights.
let private dyck1RejectAab =
    LanguageRegistry.Dyck1.RejectStrings
    |> List.find (fun tokens ->
        tokens = [ FLPQ.Languages.Terminal "a"
                   FLPQ.Languages.Terminal "a"
                   FLPQ.Languages.Terminal "b" ])
    |> List.map (fun (FLPQ.Languages.Terminal t) -> t)
    |> String.concat " "

let private runRunner () : string =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    CykRunner.runCyk (TestGrammarFiles.exampleGrammar ()) exampleInput outDir false false
    outDir

[<Fact>]
let ``runCyk produces input.tex`` () =
    let outDir = runRunner ()
    let inputTex = Path.Combine(outDir, "input.tex")
    Assert.True(File.Exists inputTex)
    Assert.True(FileInfo(inputTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces grammar_original.tex`` () =
    let outDir = runRunner ()
    let grammarTex = Path.Combine(outDir, "grammar_original.tex")
    Assert.True(File.Exists grammarTex)
    Assert.True(FileInfo(grammarTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces grammar_cnf.tex`` () =
    let outDir = runRunner ()
    let cnfTex = Path.Combine(outDir, "grammar_cnf.tex")
    Assert.True(File.Exists cnfTex)
    Assert.True(FileInfo(cnfTex).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces step directories with table.tex`` () =
    let outDir = runRunner ()

    let stepDirs =
        Directory.GetDirectories outDir
        |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))

    Assert.NotEmpty(stepDirs)

    for stepDir in stepDirs do
        let tableTex = Path.Combine(stepDir, "table.tex")
        Assert.True(File.Exists tableTex, sprintf "table.tex missing in %s" stepDir)
        Assert.True(FileInfo(tableTex).Length > 0L)

    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk produces sppf.tikz.tex by default`` () =
    let outDir = runRunner ()
    let sppfTikz = Path.Combine(outDir, "sppf.tikz.tex")
    Assert.True(File.Exists sppfTikz, "sppf.tikz.tex missing")
    Assert.True(FileInfo(sppfTikz).Length > 0L)
    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk with noSppfTable=true renders nonterminal names only`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    CykRunner.runCyk (TestGrammarFiles.exampleGrammar ()) exampleInput outDir false true

    let step0Dir = Path.Combine(outDir, "step_0")

    if Directory.Exists step0Dir then
        let tableTex = File.ReadAllText(Path.Combine(step0Dir, "table.tex"))
        Assert.DoesNotContain("(", tableTex)

    Directory.Delete(outDir, true)

[<Fact>]
let ``runCyk with useDot=true produces sppf.dot`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    CykRunner.runCyk (TestGrammarFiles.exampleGrammar ()) exampleInput outDir true false
    let sppfDot = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists sppfDot, "sppf.dot missing")
    Assert.True(FileInfo(sppfDot).Length > 0L)
    Directory.Delete(outDir, true)

[<Theory>]
[<InlineData(true)>]
[<InlineData(false)>]
let ``runCyk renders step tables for empty-highlight steps`` (noSppfTable: bool) =
    // Input "a a b" is rejected by Dyck1; its final CYK step has no highlights,
    // exercising the empty-highlight arm of both table renderers.
    let outDir =
        RunnerTestHelpers.runWithInput CykRunner.runCyk dyck1RejectAab noSppfTable

    let stepDirs =
        Directory.GetDirectories outDir
        |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))

    Assert.NotEmpty(stepDirs)

    if noSppfTable then
        // AsNt cells render nonterminal names only.
        for stepDir in stepDirs do
            let tableTex = File.ReadAllText(Path.Combine(stepDir, "table.tex"))
            Assert.DoesNotContain("(", tableTex)
    else
        // Plain cells render (Nt, splitPoint, prodIdx) tuples; span-1 cells are
        // filled for this input, so at least one step shows tuples.
        let hasTuples =
            stepDirs
            |> Array.exists (fun d -> File.ReadAllText(Path.Combine(d, "table.tex")).Contains("("))

        Assert.True(hasTuples, "expected (Nt, splitPoint, prodIdx) tuples in plain tables")

    Directory.Delete(outDir, true)
