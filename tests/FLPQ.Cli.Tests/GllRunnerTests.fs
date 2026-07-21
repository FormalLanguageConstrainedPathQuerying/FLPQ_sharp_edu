module GllRunnerTests

open System.IO
open Xunit
open FLPQ.Cli

let private baseDir = System.AppContext.BaseDirectory

let private runGllRunner (grammarText: string) (inputText: string) : string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)
    GllRunner.runGll grammarFile inputFile outDir
    outDir

let private cleanup (outDir: string) =
    let parent = Path.GetDirectoryName(outDir)

    try
        Directory.Delete(parent, true)
    with _ ->
        ()

[<Fact>]
let ``runGll produces grammar_original.tex`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "grammar_original.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces grammar_ebnf.tex`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "grammar_ebnf.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces input.tex`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "input.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces rsm_blocks.dot`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "rsm_blocks.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces path_index.tex`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "path_index.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces sppf.dot`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll handles ambiguous grammar with S -> S S production`` () =
    let outDir = runGllRunner "S -> a S b | S S | eps" "a b"
    let f = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll produces step visualization with descriptors table`` () =
    let outDir = runGllRunner "S -> a S b | eps" "a a b b"
    let step0Dir = Path.Combine(outDir, "step_0")

    Assert.True(File.Exists(Path.Combine(step0Dir, "queue.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "descriptors_table.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "new_descriptors.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "gss.dot")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "path_index.tex")))
    Assert.True(File.Exists(Path.Combine(step0Dir, "input.tex")))

    Assert.True(FileInfo(Path.Combine(step0Dir, "descriptors_table.tex")).Length > 0L)
    Assert.True(FileInfo(Path.Combine(step0Dir, "new_descriptors.tex")).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runGll step GSS dot has current vertex highlighted`` () =
    let outDir = runGllRunner "S -> a | S S | S S S" "a a a"

    let checkStep stepNum =
        let gssDot = Path.Combine(outDir, sprintf "step_%d" stepNum, "gss.dot")

        if File.Exists gssDot then
            let content = File.ReadAllText gssDot
            Assert.Contains("fillcolor=lightblue", content)

    checkStep 5
    checkStep 12
    checkStep 19
    cleanup outDir

[<Fact>]
let ``runGll step descriptors table has current descriptor highlighted`` () =
    let outDir = runGllRunner "S -> a | S S | S S S" "a a a"

    let checkStep stepNum =
        let table = Path.Combine(outDir, sprintf "step_%d" stepNum, "descriptors_table.tex")

        if File.Exists table then
            let content = File.ReadAllText table
            Assert.Contains(@"\rowcolor{yellow!20}", content)

    checkStep 5
    checkStep 12
    checkStep 19
    cleanup outDir
