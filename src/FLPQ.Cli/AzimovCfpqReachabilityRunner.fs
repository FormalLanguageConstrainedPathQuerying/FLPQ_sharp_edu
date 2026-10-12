namespace FLPQ.Cli

open System.IO
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

/// Azimov CFPQ reachability runner: parses the .cnf grammar and the graph directory, runs the
/// matrix-based algorithm over the Boolean semiring (book Chapter 12, listing alg:graphParse),
/// and writes the root artifacts plus one directory per productive fixed-point iteration. The
/// answer is T[Start]: the pair (i, j) is reachable iff some path from i to j is labeled by a
/// word of L(Start).
module AzimovCfpqReachabilityRunner =

    let private vertexName (i: int) : string = sprintf "v_%d" i

    /// The reachable pairs of the answer matrix, in row-major order.
    let private reachablePairs (answer: Matrix<bool>) : (int * int) list =
        [ for i in 0 .. Matrix.rows answer - 1 do
              for j in 0 .. Matrix.cols answer - 1 do
                  if answer.[i, j] then
                      (i, j) ]

    /// The result file: T[Start] as a Boolean matrix plus the list of reachable pairs.
    let private renderResult (answer: Matrix<bool>) : string =
        let matrixTex = PathSemiringTeX.boolMatrixToTeX vertexName vertexName answer
        let pairs = reachablePairs answer

        let pairLines =
            pairs
            |> List.map (fun (i, j) -> sprintf "(%s, %s)" (vertexName i) (vertexName j))

        matrixTex
        + "\n\n"
        + sprintf "Reachable pairs: %d\n%s" (List.length pairs) (String.concat ", " pairLines)

    /// The query is the .cnf grammar file and the input is the graph directory (unified CLI `-q`/`-i`).
    let runAzimov (queryFile: string) (inputDir: string) (outputDir: string) (useDot: bool) : unit =
        let grammar = CfpqInput.parseCnfFile queryFile
        let graph = CfpqInput.parseGraphDir inputDir

        // The same CNF conversion evaluate applies internally, so the answer is queried at the CNF start.
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let result =
            AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal grammar graph

        Helpers.writeCfpqRootArtifacts outputDir useDot grammar cnf graph
        Helpers.writeCfpqSteps outputDir result Helpers.ntName (fun _ -> @"\bullet")

        let answer = AzimovCfpq.answer result cnf.Start
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (renderResult answer)

        printfn
            "Azimov CFPQ (reachability): %d productive iterations, %d reachable pairs"
            (List.length result.Steps)
            (List.length (reachablePairs answer))
