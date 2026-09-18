namespace FLPQ.Cli.Tests

open System.IO

module RunnerTestHelpers =

    /// Write inputText as input.txt in a fresh temp output directory, run runner
    /// with the example Dyck1 grammar (useDot = false), and return the output dir.
    let runWithInput
        (runner: string -> string -> string -> bool -> bool -> unit)
        (inputText: string)
        (noSppfTable: bool)
        : string =
        let outDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        let inputFile = Path.Combine(outDir, "input.txt")
        Directory.CreateDirectory outDir |> ignore
        File.WriteAllText(inputFile, inputText)
        runner (TestGrammarFiles.exampleGrammar ()) inputFile outDir false noSppfTable
        outDir
