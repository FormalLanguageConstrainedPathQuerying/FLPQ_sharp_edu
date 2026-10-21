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

            // allPaths is CFPQ-only: the split-vertex semiring lives in Azimov's matrix core.
            let rejectAllPaths (algo: AlgorithmTypes.Algorithm) (semantics: AlgorithmTypes.PathQuerySemantics) =
                if semantics = AlgorithmTypes.PathQuerySemantics.AllPaths then
                    let name = AlgorithmTypes.displayName algo

                    raise (
                        CliErrorException(sprintf "allPaths semantics is only supported for AzimovCFPQ, not %s" name)
                    )

            match algorithm with
            // RPQ algorithms take the same unified query/input pair: the query is a
            // regexp file and the input is a graph file.
            | AlgorithmTypes.ArroyueloRPQ ->
                let queryFile = results.GetResult AlgorithmTypes.Query
                let inputFile = results.GetResult AlgorithmTypes.Input

                let semantics =
                    results.GetResult(
                        AlgorithmTypes.Semantics,
                        defaultValue = AlgorithmTypes.PathQuerySemantics.Reachability
                    )

                rejectAllPaths algorithm semantics

                match semantics with
                | AlgorithmTypes.PathQuerySemantics.Reachability ->
                    ArroyueloReachabilityRunner.runArroyuelo queryFile inputFile output useDot
                | AlgorithmTypes.PathQuerySemantics.SimplePath ->
                    ArroyueloSimplePathRunner.runArroyuelo queryFile inputFile output useDot
                | _ -> failwithf "Unexpected semantics in ArroyueloRPQ dispatch: %A" semantics
            | AlgorithmTypes.BelyaninRPQ ->
                let queryFile = results.GetResult AlgorithmTypes.Query
                let inputFile = results.GetResult AlgorithmTypes.Input

                let semantics =
                    results.GetResult(
                        AlgorithmTypes.Semantics,
                        defaultValue = AlgorithmTypes.PathQuerySemantics.Reachability
                    )

                rejectAllPaths algorithm semantics

                match semantics with
                | AlgorithmTypes.PathQuerySemantics.Reachability ->
                    BelyaninReachabilityRunner.runBelyanin queryFile inputFile output useDot
                | AlgorithmTypes.PathQuerySemantics.SimplePath ->
                    BelyaninSimplePathRunner.runBelyanin queryFile inputFile output useDot
                | _ -> failwithf "Unexpected semantics in BelyaninRPQ dispatch: %A" semantics
            // Azimov CFPQ takes the .cnf grammar file and the graph directory.
            | AlgorithmTypes.AzimovCFPQ ->
                let queryFile = results.GetResult AlgorithmTypes.Query
                let inputDir = results.GetResult AlgorithmTypes.Input

                let semantics =
                    results.GetResult(
                        AlgorithmTypes.Semantics,
                        defaultValue = AlgorithmTypes.PathQuerySemantics.Reachability
                    )

                match semantics with
                | AlgorithmTypes.PathQuerySemantics.Reachability ->
                    AzimovCfpqReachabilityRunner.runAzimov queryFile inputDir output useDot
                | AlgorithmTypes.PathQuerySemantics.SimplePath ->
                    AzimovCfpqSinglePathRunner.runAzimov queryFile inputDir output useDot
                | AlgorithmTypes.PathQuerySemantics.AllPaths ->
                    AzimovCfpqAllPathsRunner.runAzimov queryFile inputDir output useDot
            // Kronecker CFPQ takes the EBNF query file and the graph directory; reachability only.
            | AlgorithmTypes.KroneckerCFPQ ->
                let queryFile = results.GetResult AlgorithmTypes.Query
                let inputDir = results.GetResult AlgorithmTypes.Input

                let semantics =
                    results.GetResult(
                        AlgorithmTypes.Semantics,
                        defaultValue = AlgorithmTypes.PathQuerySemantics.Reachability
                    )

                match semantics with
                | AlgorithmTypes.PathQuerySemantics.Reachability ->
                    KroneckerCfpqRunner.runKronecker queryFile inputDir output useDot
                | _ ->
                    raise (
                        CliErrorException(
                            sprintf "KroneckerCFPQ supports only reachability semantics, not %A" semantics
                        )
                    )
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
        with
        | CliErrorException msg
        | FLPQ.Languages.EbnfParseErrorException msg
        | FLPQ.Languages.LlConflictErrorException msg
        | FLPQ.RPQ.GraphParseErrorException msg
        | FLPQ.RPQ.RpqInputErrorException msg
        | FLPQ.Printers.ExternalToolErrorException msg ->
            eprintfn "Error: %s" msg
            1
        | ex ->
            eprintfn "%s" ex.Message
            eprintfn "%s" (parser.PrintUsage())
            1

    [<EntryPoint>]
    let main argv = runCli argv
