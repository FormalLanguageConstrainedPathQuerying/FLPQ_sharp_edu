namespace FLPQ.Cli

open System
open System.IO
open Argu

module Program =

    let private runParsingAlgorithm
        (results: ParseResults<AlgorithmTypes.Arguments>)
        (algorithm: AlgorithmTypes.Algorithm)
        (output: string)
        (useDot: bool)
        =
        let grammar = results.GetResult AlgorithmTypes.Grammar
        let input = results.GetResult AlgorithmTypes.Input
        let k = results.GetResult(AlgorithmTypes.Lookahead, defaultValue = 1)
        let noSppfTable = results.Contains AlgorithmTypes.NoSppfTable

        match algorithm with
        | AlgorithmTypes.CYK -> CykRunner.runCyk grammar input output useDot noSppfTable
        | AlgorithmTypes.Valiant -> ValiantRunner.runValiant grammar input output useDot noSppfTable
        | AlgorithmTypes.ValiantModified -> ValiantRunner.runValiantModified grammar input output useDot noSppfTable
        | AlgorithmTypes.LL -> LLRunner.runLL grammar input output k useDot
        | AlgorithmTypes.LR0 -> LRRunner.runLR grammar input output algorithm useDot
        | AlgorithmTypes.SLR1 -> LRRunner.runLR grammar input output algorithm useDot
        | AlgorithmTypes.CLR1 -> LRRunner.runLR grammar input output algorithm useDot
        | AlgorithmTypes.GLL -> GllRunner.runGll grammar input output useDot
        | AlgorithmTypes.RNGLR -> RnglrRunner.runRnglr grammar input output useDot
        | _ -> failwithf "Unexpected algorithm in runParsingAlgorithm: %A" algorithm

    let runCli (argv: string[]) : int =
        let parser = ArgumentParser.Create<AlgorithmTypes.Arguments>(programName = "flpq")

        try
            let results = parser.ParseCommandLine argv
            let algorithm = results.GetResult AlgorithmTypes.Algorithm
            let output = results.GetResult(AlgorithmTypes.Output, defaultValue = "output")
            let summary = results.Contains AlgorithmTypes.Summary
            let useDot = results.Contains AlgorithmTypes.UseDot

            Helpers.cleanOutputDir output

            match algorithm with
            // RPQ algorithms take a regexp file and a graph file instead of grammar/input.
            | AlgorithmTypes.ArroyueloRPQ ->
                let regexpFile = results.GetResult AlgorithmTypes.Regexp
                let graphFile = results.GetResult AlgorithmTypes.GraphFile
                ArroyueloRunner.runArroyuelo regexpFile graphFile output useDot
            | AlgorithmTypes.BelyaninRPQ ->
                let regexpFile = results.GetResult AlgorithmTypes.Regexp
                let graphFile = results.GetResult AlgorithmTypes.GraphFile
                BelyaninRunner.runBelyanin regexpFile graphFile output useDot
            | _ -> runParsingAlgorithm results algorithm output useDot

            if summary then
                let templatePath = Helpers.findSummaryTemplate ()
                let resultDir = Path.Combine(output, "results")

                if not (Summary.buildSummary templatePath algorithm output resultDir useDot) then
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
