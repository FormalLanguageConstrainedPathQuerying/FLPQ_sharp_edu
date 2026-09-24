// The runner prints its status line to the process-global Console.Out; all capturing
// test modules share this collection, and the assembly disables xUnit parallelization
// (xunit.runner.json) so a Console.SetOut capture never interleaves with console writes
// from tests in other collections.
[<Xunit.Collection("ConsoleCapture")>]
module GllRunnerTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests

let private baseDir = System.AppContext.BaseDirectory

let private runGllRunner (grammarText: string) (inputText: string) : string * string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () -> GllRunner.runGll grammarFile inputFile outDir true)

    outDir, output

let private cleanup (outDir: string) =
    let parent = Path.GetDirectoryName(outDir)

    try
        Directory.Delete(parent, true)
    with _ ->
        ()

[<Fact>]
let ``runGll produces grammar_original.tex`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "grammar_original.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces grammar_ebnf.tex`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "grammar_ebnf.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces input.dot`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "input.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces rsm_blocks.dot`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "rsm_blocks.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces path_index.tex`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "path_index.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces sppf.dot`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll handles ambiguous grammar with S -> S S production`` () =
    let (outDir, _) = runGllRunner "S -> a S b | S S | eps" "a b"
    let f = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces step visualization with descriptors table`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"
    let step0Dir = Path.Combine(outDir, "step_0")

    Assert.True(File.Exists(Path.Combine(step0Dir, "queue.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "descriptors_table.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "new_descriptors.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "gss.dot")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "path_index.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "input.dot")))

    Assert.True(FileInfo(Path.Combine(step0Dir, "descriptors_table.tex")).Length > 0L)
    Assert.True(FileInfo(Path.Combine(step0Dir, "new_descriptors.tex")).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll step 0 GSS dot has no highlighted vertex`` () =
    let (outDir, _) = runGllRunner "S -> a | S S | S S S" "a a a"
    let gssDot = Path.Combine(outDir, "step_0", "gss.dot")

    if File.Exists gssDot then
        let content = File.ReadAllText gssDot
        Assert.DoesNotContain("fillcolor=lightblue", content)
        Assert.DoesNotContain("fillcolor=lightyellow", content)

    cleanup outDir

[<Fact>]
let ``runGll step 0 descriptors table has no highlighted row`` () =
    let (outDir, _) = runGllRunner "S -> a | S S | S S S" "a a a"
    let table = Path.Combine(outDir, "step_0", "descriptors_table.tex")

    if File.Exists table then
        let content = File.ReadAllText table
        Assert.DoesNotContain(@"\rowcolor{yellow!20}", content)

    cleanup outDir

[<Fact>]
let ``runGll step 0 input DOT has no highlighted vertex`` () =
    let (outDir, _) = runGllRunner "S -> a | S S | S S S" "a a a"
    let inputDot = Path.Combine(outDir, "step_0", "input.dot")

    if File.Exists inputDot then
        let content = File.ReadAllText inputDot
        Assert.DoesNotContain("fillcolor=lightgreen", content)

    cleanup outDir

[<Fact>]
let ``runGll non-init step GSS dot has current vertex highlighted`` () =
    let (outDir, _) = runGllRunner "S -> a | S S | S S S" "a a a"

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success then
            let num = int stepNum.Groups.[1].Value
            let gssDot = Path.Combine(stepDir, "gss.dot")

            if File.Exists gssDot then
                let content = File.ReadAllText gssDot

                if num = 0 then
                    Assert.DoesNotContain("fillcolor=lightblue", content)
                else
                    Assert.Contains("fillcolor=lightblue", content)

    cleanup outDir

[<Fact>]
let ``runGll non-init step descriptors table has current descriptor highlighted`` () =
    let (outDir, _) = runGllRunner "S -> a | S S | S S S" "a a a"

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success then
            let num = int stepNum.Groups.[1].Value
            let table = Path.Combine(stepDir, "descriptors_table.tex")

            if File.Exists table then
                let content = File.ReadAllText table

                if num = 0 then
                    Assert.DoesNotContain(@"\rowcolor{yellow!20}", content)
                else
                    Assert.Contains(@"\rowcolor{yellow!20}", content)

    cleanup outDir

[<Fact>]
let ``runGll non-init step input DOT has current vertex highlighted`` () =
    let (outDir, _) = runGllRunner "S -> a | S S | S S S" "a a a"

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success then
            let num = int stepNum.Groups.[1].Value
            let inputDot = Path.Combine(stepDir, "input.dot")

            if File.Exists inputDot then
                let content = File.ReadAllText inputDot

                if num = 0 then
                    Assert.DoesNotContain("fillcolor=lightgreen", content)
                else
                    Assert.Contains("fillcolor=lightgreen", content)

    cleanup outDir

/// Grammar where nonterminal A is called at position 0 by two different callers, the second
/// only after A has already been popped — this triggers stored-pops handling at (A_start, 0).
let private reentryGrammar = "S -> A | B\nA -> a\nB -> C\nC -> A"

[<Fact>]
let ``runGll stored-pops step highlights GSS vertex orange (dot mode)`` () =
    let (outDir, _) = runGllRunner reentryGrammar "a"

    let mutable foundOrange = false

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success && int stepNum.Groups.[1].Value > 0 then
            let gssDot = Path.Combine(stepDir, "gss.dot")

            if File.Exists gssDot then
                let content = File.ReadAllText gssDot
                foundOrange <- foundOrange || content.Contains("fillcolor=orange")

    Assert.True(foundOrange, "Expected at least one non-init step to highlight a stored-pops GSS vertex orange")

    cleanup outDir

[<Fact>]
let ``runGll stored-pops step 0 has no orange highlight (dot mode)`` () =
    let (outDir, _) = runGllRunner reentryGrammar "a"
    let gssDot = Path.Combine(outDir, "step_0", "gss.dot")

    if File.Exists gssDot then
        let content = File.ReadAllText gssDot
        Assert.DoesNotContain("fillcolor=orange", content)

    cleanup outDir

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``runGll input DOT compiles with graphviz`` () =
    let (outDir, _) = runGllRunner "S -> a S b | eps" "a a b b"

    let checkInputDot path =
        let content = File.ReadAllText path
        Assert.True(FLPQ.Printers.ExternalTools.compileDotString content)

    checkInputDot (Path.Combine(outDir, "input.dot"))

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let inputDot = Path.Combine(stepDir, "input.dot")

        if File.Exists inputDot then
            checkInputDot inputDot

    cleanup outDir

let private runGllRunnerTikz (grammarText: string) (inputText: string) : string * string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)

    let output =
        RunnerTestHelpers.withCapturedOutput (fun () -> GllRunner.runGll grammarFile inputFile outDir false)

    outDir, output

[<Fact>]
let ``runGll stored-pops step highlights GSS vertex orange (tikz mode)`` () =
    let (outDir, _) = runGllRunnerTikz reentryGrammar "a"

    let mutable foundOrange = false

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success && int stepNum.Groups.[1].Value > 0 then
            let gssTikz = Path.Combine(stepDir, "gss.tikz.tex")

            if File.Exists gssTikz then
                let content = File.ReadAllText gssTikz
                foundOrange <- foundOrange || content.Contains("fill=orange!30")

    Assert.True(foundOrange, "Expected at least one non-init step to highlight a stored-pops GSS vertex orange")

    cleanup outDir

[<Fact>]
let ``runGll tikz mode produces input.tikz.tex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "input.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll tikz mode produces ext_rsm.tikz.tex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "ext_rsm.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll tikz mode produces sppf.tikz.tex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "sppf.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll tikz mode step 0 produces gss.tikz.tex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "step_0", "gss.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll tikz mode step 0 produces input.tikz.tex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "step_0", "input.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll tikz mode step 0 produces rsm.tikz.tex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "step_0", "rsm.tikz.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll tikz mode step 0 gss tikz has no highlighted vertex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a | S S | S S S" "a a a"
    let gssTikz = Path.Combine(outDir, "step_0", "gss.tikz.tex")

    if File.Exists gssTikz then
        let content = File.ReadAllText gssTikz
        Assert.DoesNotContain("fill=lightblue!20", content)
        Assert.DoesNotContain("fill=yellow!20", content)

    cleanup outDir

[<Fact>]
let ``runGll tikz mode step 0 input tikz has no highlighted vertex`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a | S S | S S S" "a a a"
    let inputTikz = Path.Combine(outDir, "step_0", "input.tikz.tex")

    if File.Exists inputTikz then
        let content = File.ReadAllText inputTikz
        Assert.DoesNotContain("fill=lightgreen!20", content)

    cleanup outDir

[<Fact>]
let ``runGll tikz mode non-init step gss tikz has current vertex highlighted`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a | S S | S S S" "a a a"

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success then
            let num = int stepNum.Groups.[1].Value
            let gssTikz = Path.Combine(stepDir, "gss.tikz.tex")

            if File.Exists gssTikz then
                let content = File.ReadAllText gssTikz

                if num = 0 then
                    Assert.DoesNotContain("fill=lightblue!20", content)
                else
                    Assert.Contains("fill=lightblue!20", content)

    cleanup outDir

[<Fact>]
let ``runGll tikz mode non-init step input tikz has current vertex highlighted`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a | S S | S S S" "a a a"

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success then
            let num = int stepNum.Groups.[1].Value
            let inputTikz = Path.Combine(stepDir, "input.tikz.tex")

            if File.Exists inputTikz then
                let content = File.ReadAllText inputTikz

                if num = 0 then
                    Assert.DoesNotContain("fill=lightgreen!20", content)
                else
                    Assert.Contains("fill=lightgreen!20", content)

    cleanup outDir

[<Fact>]
let ``runGll tikz mode gss uses rounded rectangle shape`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a | S S | S S S" "a a a"

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let gssTikz = Path.Combine(stepDir, "gss.tikz.tex")

        if File.Exists gssTikz then
            let content = File.ReadAllText gssTikz
            Assert.Contains("rectangle, rounded corners", content)

    cleanup outDir

[<Fact>]
let ``runGll tikz mode gss edges use R-based range notation`` () =
    let (outDir, _) = runGllRunnerTikz "S -> a S b | eps" "a a b b"

    let mutable foundEdges = false

    for stepDir in Directory.GetDirectories(outDir, "step_*") do
        let stepName = System.IO.Path.GetFileName(stepDir)
        let stepNum = System.Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")

        if stepNum.Success && int stepNum.Groups.[1].Value > 0 then
            let gssTikz = Path.Combine(stepDir, "gss.tikz.tex")

            if File.Exists gssTikz then
                let content = File.ReadAllText gssTikz

                if content.Contains("$R^") then
                    foundEdges <- true

    Assert.True(foundEdges, "Expected R-based range notation R^{...}_{...} on GSS edges")

    cleanup outDir

[<Fact>]
let ``runGll reports Rejected status for unbalanced input`` () =
    let (outDir, output) = runGllRunner "S -> a S b | eps" "a a a"

    Assert.Contains("GLL: Rejected", output)

    cleanup outDir
