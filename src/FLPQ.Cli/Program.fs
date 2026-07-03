namespace FLPQ.Cli

open System
open System.IO
open Argu

module Program =

    let runCli (argv: string[]) : int =
        let parser = ArgumentParser.Create<AlgorithmTypes.Arguments>(programName = "flpq")

        try
            let results = parser.ParseCommandLine argv
            let algorithm = results.GetResult AlgorithmTypes.Algorithm
            let grammar = results.GetResult AlgorithmTypes.Grammar
            let input = results.GetResult AlgorithmTypes.Input
            let output = results.GetResult(AlgorithmTypes.Output, defaultValue = "output")
            let k = results.GetResult(AlgorithmTypes.Lookahead, defaultValue = 1)
            let summary = results.Contains AlgorithmTypes.Summary

            match algorithm with
            | AlgorithmTypes.CYK -> CykRunner.runCyk grammar input output
            | AlgorithmTypes.Valiant -> ValiantRunner.runValiant grammar input output
            | AlgorithmTypes.LL -> LLRunner.runLL grammar input output k
            | AlgorithmTypes.LR -> LRRunner.runLR grammar input output

            if summary then
                let templatePath = Helpers.findSummaryTemplate ()
                let resultDir = Path.Combine(output, "results")

                if not (Summary.buildSummary templatePath algorithm output resultDir) then
                    1
                else
                    0
            else
                0
        with ex ->
            eprintfn "%s" ex.Message
            eprintfn "%s" (parser.PrintUsage())
            1

    [<EntryPoint>]
    let main argv = runCli argv
