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
        let query = results.GetResult AlgorithmTypes.Query
        let input = results.GetResult AlgorithmTypes.Input
        let k = results.GetResult(AlgorithmTypes.Lookahead, defaultValue = 1)
        let noSppfTable = results.Contains AlgorithmTypes.NoSppfTable

        match algorithm with
        | AlgorithmTypes.CYK -> CykRunner.runCyk query input output useDot noSppfTable
        | AlgorithmTypes.Valiant -> ValiantRunner.runValiant query input output useDot noSppfTable
        | AlgorithmTypes.ValiantModified -> ValiantRunner.runValiantModified query input output useDot noSppfTable
        | AlgorithmTypes.LL -> LLRunner.runLL query input output k useDot
        | AlgorithmTypes.LR0 -> LRRunner.runLR query input output algorithm useDot
        | AlgorithmTypes.SLR1 -> LRRunner.runLR query input output algorithm useDot
        | AlgorithmTypes.CLR1 -> LRRunner.runLR query input output algorithm useDot
        | AlgorithmTypes.GLL -> GllRunner.runGll query input output useDot
        | AlgorithmTypes.RNGLR -> RnglrRunner.runRnglr query input output useDot
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
            // RPQ algorithms take the same unified query/input pair: the query is a
            // regexp file and the input is a graph file.
            | AlgorithmTypes.ArroyueloRPQ ->
                let queryFile = results.GetResult AlgorithmTypes.Query
                let inputFile = results.GetResult AlgorithmTypes.Input
                ArroyueloRunner.runArroyuelo queryFile inputFile output useDot
            | AlgorithmTypes.BelyaninRPQ ->
                let queryFile = results.GetResult AlgorithmTypes.Query
                let inputFile = results.GetResult AlgorithmTypes.Input
                BelyaninRunner.runBelyanin queryFile inputFile output useDot
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
