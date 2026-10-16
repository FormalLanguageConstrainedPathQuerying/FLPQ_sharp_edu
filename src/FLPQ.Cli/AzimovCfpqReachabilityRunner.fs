namespace FLPQ.Cli

open System.IO
open FLPQ.CFPQ
open FLPQ.Languages

/// Azimov CFPQ reachability runner: parses the .cnf grammar and the graph directory, runs the
/// matrix-based algorithm over the Boolean semiring (book Chapter 12, listing alg:graphParse),
/// and writes the root artifacts plus one directory per productive fixed-point iteration. The
/// answer is T[Start]: the pair (i, j) is reachable iff some path from i to j is labeled by a
/// word of L(Start).
module AzimovCfpqReachabilityRunner =

    /// The query is the .cnf grammar file and the input is the graph directory (unified CLI `-q`/`-i`).
    let runAzimov (queryFile: string) (inputDir: string) (outputDir: string) (useDot: bool) : unit =
        let grammar = CfpqInput.parseCnfFile queryFile
        let graph = CfpqInput.parseGraphDir inputDir

        // The same CNF conversion evaluate applies internally, so the answer is queried at the CNF start.
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let result =
            AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal grammar graph

        Helpers.writeCfpqRootArtifacts outputDir useDot grammar cnf graph
        Helpers.writeCfpqSteps outputDir result.Steps Helpers.ntName (fun _ -> @"\bullet")

        let answer = AzimovCfpq.answer result cnf.Start
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (Helpers.renderResult answer)

        printfn
            "Azimov CFPQ (reachability): %d productive iterations, %d reachable pairs"
            (List.length result.Steps)
            (List.length (Helpers.reachablePairs answer))
