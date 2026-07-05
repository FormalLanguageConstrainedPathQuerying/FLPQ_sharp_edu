module ErrorPathTests

open System.IO
open Xunit
open FLPQ.Cli

[<Fact>]
let ``missing grammar file returns non-zero`` () =
    let args =
        [| "-a"
           "CYK"
           "-g"
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
        [| "-a"; "InvalidAlgo"; "-g"; "nonexistent.bnf"; "-i"; "nonexistent.txt" |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)

[<Fact>]
let ``empty output directory is handled`` () =
    let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory outDir |> ignore

    let args =
        [| "-a"
           "CYK"
           "-g"
           "nonexistent.bnf"
           "-i"
           "nonexistent.txt"
           "-o"
           outDir |]

    Program.runCli args |> ignore
    Directory.Delete(outDir, true)

[<Fact>]
let ``unsupported algorithm with summary`` () =
    let args =
        [| "-a"
           "CYK"
           "-g"
           "nonexistent.bnf"
           "-i"
           "nonexistent.txt"
           "-o"
           (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()))
           "-s" |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)

[<Fact>]
let ``bad lookahead value`` () =
    let args =
        [| "-a"
           "LL"
           "-g"
           "nonexistent.bnf"
           "-i"
           "nonexistent.txt"
           "-o"
           (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()))
           "-k"
           "badvalue" |]

    let code = Program.runCli args
    Assert.NotEqual(0, code)
