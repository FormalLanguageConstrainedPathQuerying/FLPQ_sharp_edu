module RnglrRunnerTests

open System.IO
open Xunit
open FLPQ.Cli

let private runRnglrRunner (grammarText: string) (inputText: string) : string =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, grammarText)
    File.WriteAllText(inputFile, inputText)
    RnglrRunner.runRnglr grammarFile inputFile outDir
    outDir

let private cleanup (outDir: string) =
    let parent = Path.GetDirectoryName(outDir)

    try
        Directory.Delete(parent, true)
    with _ ->
        ()

[<Fact>]
let ``runRnglr produces grammar_ebnf.tex`` () =
    let outDir = runRnglrRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "grammar_ebnf.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces input.tex`` () =
    let outDir = runRnglrRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "input.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces rnglr_table.tex`` () =
    let outDir = runRnglrRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "rnglr_table.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces rsm_blocks.dot`` () =
    let outDir = runRnglrRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "rsm_blocks.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces path_index.tex`` () =
    let outDir = runRnglrRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "path_index.tex")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces sppf.dot`` () =
    let outDir = runRnglrRunner "S -> a S b | eps" "a a b b"
    let f = Path.Combine(outDir, "sppf.dot")
    Assert.True(File.Exists f)
    Assert.True(FileInfo(f).Length > 0L)
    cleanup outDir

[<Fact>]
let ``runRnglr produces step visualization files`` () =
    let outDir = runRnglrRunner "S -> a a" "a a"
    let step0Dir = Path.Combine(outDir, "step_0")

    let expected =
        [ "descriptors_table.tex"
          "new_descriptors.tex"
          "gss.dot"
          "path_index.tex"
          "input.dot"
          "lr_automaton.dot" ]

    for f in expected do
        let path = Path.Combine(step0Dir, f)
        Assert.True(File.Exists path, sprintf "Missing: %s" f)
        Assert.True(FileInfo(path).Length > 0L, sprintf "Empty: %s" f)

    cleanup outDir
