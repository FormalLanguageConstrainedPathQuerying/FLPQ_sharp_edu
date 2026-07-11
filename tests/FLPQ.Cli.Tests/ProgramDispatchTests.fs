module ProgramDispatchTests

open System.IO
open Xunit
open FLPQ.Cli

let private baseDir = System.AppContext.BaseDirectory

let private exampleGrammar = Path.Combine(baseDir, "example_grammar.bnf")
let private exampleInput = Path.Combine(baseDir, "example_input.txt")
let private exampleLRGrammar = Path.Combine(baseDir, "example_lr_grammar.bnf")
let private exampleLRInput = Path.Combine(baseDir, "example_lr_input.txt")

let private runAlgorithm (algorithm: string) (grammarFile: string) (inputFile: string) : int =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())

    let args =
        [| "-a"; algorithm; "-g"; grammarFile; "-i"; inputFile; "-o"; outDir |]

    let code = Program.runCli args
    let mutable cleanup = true

    try
        Directory.Delete(outDir, true)
    with _ ->
        cleanup <- false

    code

[<Fact>]
let ``ValiantModified runs successfully`` () =
    let code = runAlgorithm "ValiantModified" exampleGrammar exampleInput
    Assert.Equal(0, code)

[<Fact>]
let ``CLR1 runs successfully`` () =
    let code = runAlgorithm "CLR1" exampleLRGrammar exampleLRInput
    Assert.Equal(0, code)

[<Fact>]
let ``GLL runs successfully with EBNF grammar`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    let grammarFile = Path.Combine(tmpDir, "grammar.ebnf")
    let inputFile = Path.Combine(tmpDir, "input.txt")
    let outDir = Path.Combine(tmpDir, "output")
    Directory.CreateDirectory(tmpDir) |> ignore
    File.WriteAllText(grammarFile, "S -> a S b | eps")
    File.WriteAllText(inputFile, "a a b b")

    let args =
        [| "-a"; "GLL"; "-g"; grammarFile; "-i"; inputFile; "-o"; outDir |]

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
    File.WriteAllText(grammarFile, "S -> a S b | eps")
    File.WriteAllText(inputFile, "a a b b")

    let args =
        [| "-a"; "RNGLR"; "-g"; grammarFile; "-i"; inputFile; "-o"; outDir |]

    let code = Program.runCli args
    try
        Directory.Delete(tmpDir, true)
    with _ ->
        ()

    Assert.Equal(0, code)
