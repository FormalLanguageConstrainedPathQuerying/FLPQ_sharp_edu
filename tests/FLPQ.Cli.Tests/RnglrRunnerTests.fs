// Shares the ConsoleCapture collection with GllRunnerTests so the two modules'
// Console.SetOut calls never interleave (Console.Out is process-global).
[<Xunit.Collection("ConsoleCapture")>]
module RnglrRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests
open FLPQ.TestUtilities

let private runRnglrRunner (grammarText: string) (inputText: string) : string * string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () -> RnglrRunner.runRnglr grammarFile inputFile outDir true)

    outDir, output

let private cleanup (outDir: string) =
    let parent = Path.GetDirectoryName(outDir)

    try
        Directory.Delete(parent, true)
    with _ ->
        ()

[<Fact>]
let ``runRnglr produces grammar_ebnf.tex`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "grammar_ebnf.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces input.tex`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "input.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces rnglr_table.tex`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "rnglr_table.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr dot mode produces ext_rsm.dot`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "ext_rsm.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr dot mode produces lr_automaton.dot`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "lr_automaton.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr no longer produces rsm_blocks.dot`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    Assert.False(File.Exists(Path.Combine(outDir, "rsm_blocks.dot")))
    cleanup outDir

[<Fact>]
let ``runRnglr produces path_index.tex`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "path_index.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces sppf.dot`` () =
    let (outDir, _) =
        runRnglrRunner TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces step visualization files`` () =
    let (outDir, _) = runRnglrRunner "S -> a a" "a a"
    let step0Dir = Path.Combine(outDir, "step_0")

    let expected = [ "gss.dot"; "path_index.tex"; "input.dot"; "lr_table.tex" ]

    for f in expected do
        let path = Path.Combine(step0Dir, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir

let private runRnglrRunnerTikz (grammarText: string) (inputText: string) : string * string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () -> RnglrRunner.runRnglr grammarFile inputFile outDir false)

    outDir, output

[<Fact>]
let ``runRnglr tikz mode produces input.tikz.tex`` () =
    let (outDir, _) =
        runRnglrRunnerTikz TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "input.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr tikz mode produces ext_rsm.tikz.tex`` () =
    let (outDir, _) =
        runRnglrRunnerTikz TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "ext_rsm.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr tikz mode produces lr_automaton.tikz.tex`` () =
    let (outDir, _) =
        runRnglrRunnerTikz TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "lr_automaton.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr tikz mode produces sppf.tikz.tex`` () =
    let (outDir, _) =
        runRnglrRunnerTikz TestGrammarFiles.anbnEbnf TestGrammarFiles.anbnInput

    let f = Path.Combine(outDir, "sppf.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr tikz mode step 0 produces gss.tikz.tex`` () =
    let (outDir, _) = runRnglrRunnerTikz "S -> a a" "a a"
    let f = Path.Combine(outDir, "step_0", "gss.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr tikz mode step 0 produces input.tikz.tex`` () =
    let (outDir, _) = runRnglrRunnerTikz "S -> a a" "a a"
    let f = Path.Combine(outDir, "step_0", "input.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

/// Grammar where the concatenation rule S -> S S creates multiple reduction paths at the same
/// input position; a product BFS deposits stored states at an intermediate GSS vertex that a
/// later reduction in the same level consumes — this triggers passing-reduction handling.
let private passingReductionGrammar = LanguageRegistry.Dyck1.Grammars.[1].Text

/// Minimal input that triggers passing-reduction handling (step 3).
let private passingReductionInput = "a b"

[<Fact>]
let ``runRnglr passing-reductions step highlights GSS vertex orange (dot mode)`` () =
    let (outDir, _) = runRnglrRunner passingReductionGrammar passingReductionInput

    let mutable foundOrange = false

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success && int stepNum.Groups.[1].Value > 0 then
            let gssDot = Path.Combine(stepDir, "gss.dot")

            if File.Exists gssDot then
                let content = File.ReadAllText gssDot
                foundOrange <- foundOrange || content.Contains("fillcolor=orange")

    Assert.True(foundOrange, "Expected at least one non-init step to highlight a passing-reduction GSS vertex orange")

    cleanup outDir

[<Fact>]
let ``runRnglr passing-reductions step 0 has no orange highlight (dot mode)`` () =
    let (outDir, _) = runRnglrRunner passingReductionGrammar passingReductionInput
    let gssDot = Path.Combine(outDir, "step_0", "gss.dot")

    if File.Exists gssDot then
        let content = File.ReadAllText gssDot
        Assert.DoesNotContain("fillcolor=orange", content)

    cleanup outDir

[<Fact>]
let ``runRnglr passing-reductions step highlights GSS vertex orange (tikz mode)`` () =
    let (outDir, _) = runRnglrRunnerTikz passingReductionGrammar passingReductionInput

    let mutable foundOrange = false

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success && int stepNum.Groups.[1].Value > 0 then
            let gssTikz = Path.Combine(stepDir, "gss.tikz.tex")

            if File.Exists gssTikz then
                let content = File.ReadAllText gssTikz
                foundOrange <- foundOrange || content.Contains("fill=orange!30")

    Assert.True(foundOrange, "Expected at least one non-init step to highlight a passing-reduction GSS vertex orange")

    cleanup outDir

[<Fact>]
let ``runRnglr reports Rejected status for unbalanced input`` () =
    let (outDir, output) = runRnglrRunner TestGrammarFiles.anbnEbnf "a a a"

    Assert.Contains("RNGLR: Rejected", output)

    cleanup outDir
