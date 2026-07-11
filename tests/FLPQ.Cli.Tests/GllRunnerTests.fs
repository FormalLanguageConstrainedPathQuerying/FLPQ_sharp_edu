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
