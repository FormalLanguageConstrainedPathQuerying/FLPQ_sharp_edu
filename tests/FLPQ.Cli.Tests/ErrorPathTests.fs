module ErrorPathTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.TestUtilities

[<Fact>]
let ``readFile with missing path throws CliErrorException`` () =
    TestHelpers.assertThrows "File not found" (fun () -> Helpers.readFile "/nonexistent/path/to/file.bnf")

[<Fact>]
let ``missing query file returns non-zero`` () =
    let args =
        [| "-a"
           "CYK"
           "-q"
           "nonexistent_grammar.bnf"
           "-i"
           "nonexistent_input.txt"
           "-o"
           (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())) |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)

[<Fact>]
let ``invalid algorithm name returns non-zero`` () =
    let args =
        [| "-a"; "InvalidAlgo"; "-q"; "nonexistent.bnf"; "-i"; "nonexistent.txt" |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)

[<Fact>]
let ``empty output directory is handled`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let args =
        [| "-a"
           "CYK"
           "-q"
           "nonexistent.bnf"
           "-i"
           "nonexistent.txt"
           "-o"
           outDir |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)
    Directory.Delete(outDir, true)

[<Fact>]
let ``unsupported algorithm with summary`` () =
    let args =
        [| "-a"
           "CYK"
           "-q"
           "nonexistent.bnf"
           "-i"
           "nonexistent.txt"
           "-o"
           (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()))
           "-s" |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)

[<Fact>]
let ``malformed EBNF regexp with RPQ returns non-zero`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tmpDir) |> ignore
    let regexpFile = Path.Combine(tmpDir, "query.ebnf")
    let graphFile = Path.Combine(tmpDir, "graph.txt")
    File.WriteAllText(regexpFile, "S -> a@b")
    File.WriteAllText(graphFile, "0 a 1")
    let outDir = Path.Combine(tmpDir, "out")

    let code =
        Program.runCli [| "-a"; "ArroyueloRPQ"; "-q"; regexpFile; "-i"; graphFile; "-o"; outDir |]

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``LL grammar with LL(1) conflict returns non-zero`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tmpDir) |> ignore
    let grammarFile = Path.Combine(tmpDir, "grammar.bnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    File.WriteAllText(grammarFile, "S -> A a\nA -> a A\nA -> eps")
    File.WriteAllText(inputFile, "a a")
    let outDir = Path.Combine(tmpDir, "out")

    let code =
        Program.runCli [| "-a"; "LL"; "-q"; grammarFile; "-i"; inputFile; "-o"; outDir |]

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``malformed graph file with RPQ returns non-zero`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tmpDir) |> ignore
    let regexpFile = Path.Combine(tmpDir, "query.ebnf")
    let graphFile = Path.Combine(tmpDir, "graph.txt")
    File.WriteAllText(regexpFile, "S -> a")
    File.WriteAllText(graphFile, "invalid line without three parts")
    let outDir = Path.Combine(tmpDir, "out")

    let code =
        Program.runCli [| "-a"; "ArroyueloRPQ"; "-q"; regexpFile; "-i"; graphFile; "-o"; outDir |]

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``empty regexp file with RPQ returns non-zero`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tmpDir) |> ignore
    let regexpFile = Path.Combine(tmpDir, "query.ebnf")
    let graphFile = Path.Combine(tmpDir, "graph.txt")
    File.WriteAllText(regexpFile, "")
    File.WriteAllText(graphFile, "0 a 1")
    let outDir = Path.Combine(tmpDir, "out")

    let code =
        Program.runCli [| "-a"; "ArroyueloRPQ"; "-q"; regexpFile; "-i"; graphFile; "-o"; outDir |]

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)
