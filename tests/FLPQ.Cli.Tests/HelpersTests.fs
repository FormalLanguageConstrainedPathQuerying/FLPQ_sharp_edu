module HelpersTests

open System.IO
open Xunit
open FLPQ.Cli.Helpers
open FLPQ.Printers

[<Fact>]
let ``readFile reads existing file`` () =
    let tmp = Path.GetTempFileName()
    File.WriteAllText(tmp, "hello world\n")
    let content = readFile tmp
    Assert.Equal("hello world", content)
    File.Delete tmp

[<Fact>]
let ``readFile throws for missing file`` () =
    Assert.Throws<System.Exception>(fun () -> readFile "nonexistent_file_xyz.txt" |> ignore)

[<Fact>]
let ``writeOutputFile creates directory and writes content`` () =
    let dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let file = Path.Combine(dir, "sub", "test.txt")

    writeOutputFile file "content"
    Assert.True(File.Exists file)
    Assert.Equal("content", File.ReadAllText file)

    Directory.Delete(dir, true)

[<Fact>]
let ``cleanOutputDir creates directory if not exists`` () =
    let dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    cleanOutputDir dir
    Assert.True(Directory.Exists dir)
    Directory.Delete dir

[<Fact>]
let ``cleanOutputDir clears existing directory`` () =
    let dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory dir |> ignore
    File.WriteAllText(Path.Combine(dir, "file.txt"), "data")
    cleanOutputDir dir
    Assert.True(Directory.Exists dir)
    Assert.Empty(Directory.GetFileSystemEntries dir)
    Directory.Delete dir

[<Fact>]
let ``readIfExists returns Some for existing file`` () =
    let tmp = Path.GetTempFileName()
    File.WriteAllText(tmp, "data")
    Assert.Equal(Some "data", SummaryTeX.readIfExists tmp)
    File.Delete tmp

[<Fact>]
let ``readIfExists returns None for missing file`` () =
    Assert.Equal(None, SummaryTeX.readIfExists "nonexistent_file_xyz.txt")

[<Fact>]
let ``naturalSortKey extracts step number`` () =
    Assert.Equal(5, naturalSortKey "step_5")
    Assert.Equal(0, naturalSortKey "step_0")
    Assert.Equal(42, naturalSortKey "step_42")

[<Fact>]
let ``naturalSortKey returns 0 for non-step directories`` () =
    Assert.Equal(0, naturalSortKey "something_else")
    Assert.Equal(0, naturalSortKey "")

[<Fact>]
let ``findSummaryTemplate returns existing file`` () =
    let templatePath = findSummaryTemplate ()
    Assert.True(File.Exists templatePath)
    let content = File.ReadAllText templatePath
    Assert.Contains("__CONTENT__", content)

[<Fact>]
let ``findTikzTemplate returns existing file`` () =
    let templatePath = findTikzTemplate ()
    Assert.True(File.Exists templatePath)
    let content = File.ReadAllText templatePath
    Assert.Contains("\\usepackage{tikz}", content)
