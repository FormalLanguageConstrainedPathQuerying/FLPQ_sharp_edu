module ProgramDispatchTests

open System.IO
open Xunit
open FLPQ.Cli
open FLPQ.Cli.Tests

let private baseDir = System.AppContext.BaseDirectory

let private exampleInput = Path.Combine(baseDir, "example_input.txt")
let private exampleLRInput = Path.Combine(baseDir, "example_lr_input.txt")
let private exampleRegexp = Path.Combine(baseDir, "example_regexp.txt")
let private exampleGraph = Path.Combine(baseDir, "example_graph.txt")

let private runAlgorithm (algorithm: string) (queryFile: string) (inputFile: string) : int =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args = [| "-a"; algorithm; "-q"; queryFile; "-i"; inputFile; "-o"; outDir |]

    let code = Program.runCli args
    let mutable cleanup = true

    try
        Directory.Delete(outDir, true)
    with _ ->
        cleanup <- false

    code

[<Fact>]
let ``ValiantModified runs successfully`` () =
    let code =
        runAlgorithm "ValiantModified" (TestGrammarFiles.exampleGrammar ()) exampleInput

    Assert.Equal(0, code)

[<Fact>]
let ``CLR1 runs successfully`` () =
    let code = runAlgorithm "CLR1" (TestGrammarFiles.exampleLRGrammar ()) exampleLRInput
    Assert.Equal(0, code)

[<Fact>]
let ``GLL runs successfully with EBNF grammar`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, TestGrammarFiles.anbnEbnf)
    File.WriteAllText(inputFile, TestGrammarFiles.anbnInput)

    let args = [| "-a"; "GLL"; "-q"; grammarFile; "-i"; inputFile; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.Equal(0, code)

[<Fact>]
let ``RNGLR runs successfully with EBNF grammar`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, TestGrammarFiles.anbnEbnf)
    File.WriteAllText(inputFile, TestGrammarFiles.anbnInput)

    let args = [| "-a"; "RNGLR"; "-q"; grammarFile; "-i"; inputFile; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.Equal(0, code)

[<Fact>]
let ``Valiant runs successfully`` () =
    let code = runAlgorithm "Valiant" (TestGrammarFiles.exampleGrammar ()) exampleInput
    Assert.Equal(0, code)

[<Fact>]
let ``LR0 runs successfully`` () =
    let code = runAlgorithm "LR0" (TestGrammarFiles.exampleLRGrammar ()) exampleLRInput
    Assert.Equal(0, code)

[<Fact>]
let ``SLR1 runs successfully`` () =
    let code = runAlgorithm "SLR1" (TestGrammarFiles.exampleLRGrammar ()) exampleLRInput
    Assert.Equal(0, code)

[<Fact>]
let ``CYK runs successfully`` () =
    let code = runAlgorithm "CYK" (TestGrammarFiles.exampleGrammar ()) exampleInput
    Assert.Equal(0, code)

[<Fact>]
let ``ArroyueloRPQ runs successfully with query and input files`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"
           "ArroyueloRPQ"
           "-q"
           exampleRegexp
           "-i"
           exampleGraph
           "-o"
           outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.Equal(0, code)

[<Fact>]
let ``ArroyueloRPQ without query file exits non-zero`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args = [| "-a"; "ArroyueloRPQ"; "-i"; exampleGraph; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``ArroyueloRPQ without input file exits non-zero`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args = [| "-a"; "ArroyueloRPQ"; "-q"; exampleRegexp; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``BelyaninRPQ runs successfully with query and input files`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"; "BelyaninRPQ"; "-q"; exampleRegexp; "-i"; exampleGraph; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.Equal(0, code)

[<Fact>]
let ``BelyaninRPQ without query file exits non-zero`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args = [| "-a"; "BelyaninRPQ"; "-i"; exampleGraph; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``BelyaninRPQ without input file exits non-zero`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args = [| "-a"; "BelyaninRPQ"; "-q"; exampleRegexp; "-o"; outDir |]

    let code = Program.runCli args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.NotEqual(0, code)

[<Fact>]
let ``main delegates to runCli`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"
           "ValiantModified"
           "-q"
           TestGrammarFiles.exampleGrammar ()
           "-i"
           exampleInput
           "-o"
           outDir |]

    let code = Program.main args

    try
        Directory.Delete(outDir, true)
    with _ ->
        ()

    Assert.Equal(0, code)
