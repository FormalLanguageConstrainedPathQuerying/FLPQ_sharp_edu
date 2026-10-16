namespace FLPQ.Cli

open System.IO
open FLPQ.CFPQ
open FLPQ.Languages

/// Kronecker CFPQ reachability runner: parses the EBNF query file into an RSM and the graph
/// directory, runs the Kronecker-product matrix algorithm over the Boolean semiring (book
/// Chapter 12, listing lst:algo1 with the transitive closure replaced by MS-BFS), and writes the
/// root artifacts plus one directory per productive fixed-point iteration. The answer is the
/// start block's matrix: the pair (i, j) is reachable iff some path from i to j is labeled by a
/// word of L(start).
module KroneckerCfpqRunner =

    /// The query is the EBNF file and the input is the graph directory (unified CLI `-q`/`-i`).
    let runKronecker (queryFile: string) (inputDir: string) (outputDir: string) (useDot: bool) : unit =
        let rsm = RsmBuilder.buildRSMFromFile queryFile
        let graph = CfpqInput.parseGraphDir inputDir

        let result = KroneckerCfpq.evaluate rsm graph

        Helpers.writeKroneckerCfpqRootArtifacts outputDir useDot rsm graph
        Helpers.writeCfpqSteps outputDir result.Steps Helpers.ntName (fun _ -> @"\bullet")

        let answer = KroneckerCfpq.answer result rsm
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (Helpers.renderResult answer)

        printfn
            "Kronecker CFPQ: %d productive iterations, %d reachable pairs"
            (List.length result.Steps)
            (List.length (Helpers.reachablePairs answer))
