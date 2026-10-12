namespace FLPQ.Cli

open System.IO
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

/// Azimov CFPQ single-path runner (LAGraph PR #8): the fixed point runs over the path-index
/// semiring (minimum derivation-tree height), and for every reachable pair one path is
/// extracted from the middle/height indices. The result file lists one edge sequence per
/// reachable pair; the step tables show the v_k^h cell values.
module AzimovCfpqSinglePathRunner =

    let private vertexName (i: int) : string = sprintf "v_%d" i

    /// A path-index cell as text: the middle vertex with the derivation height as a superscript.
    let private elementToTeX (pi: CfpqSemiring.PathIndex) : string =
        sprintf @"v_{%d}^{%d}" pi.Middle pi.Height

    /// One extracted edge sequence per reachable pair of T[Start], in row-major order.
    let private extractedPaths
        (rules: AzimovCfpq.WcnfRule<string, string> list)
        (graph: CfpqInput.CfpqGraph<string>)
        (matrices: Map<Nonterminal<string>, Matrix<CfpqSemiring.PathIndex option>>)
        (start: Nonterminal<string>)
        : string list =
        let m = Map.find start matrices

        [ for i in 0 .. Matrix.rows m - 1 do
              for j in 0 .. Matrix.cols m - 1 do
                  match AzimovCfpq.extractSinglePath rules graph matrices start i j with
                  | None -> ()
                  | Some path -> sprintf @"%s, %s: %s" (vertexName i) (vertexName j) (CfpqMatrixTeX.pathToText id path) ]

    /// The query is the .cnf grammar file and the input is the graph directory (unified CLI `-q`/`-i`).
    let runAzimov (queryFile: string) (inputDir: string) (outputDir: string) (useDot: bool) : unit =
        let grammar = CfpqInput.parseCnfFile queryFile
        let graph = CfpqInput.parseGraphDir inputDir

        // The same CNF conversion evaluate applies internally; extraction walks its rules.
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
        let rules = AzimovCfpq.classifyRules cnf

        let result =
            AzimovCfpq.evaluate CfpqSemiring.SinglePathSemiring Grammar.freshStringNonterminal grammar graph

        Helpers.writeCfpqRootArtifacts outputDir useDot grammar cnf graph
        Helpers.writeCfpqSteps outputDir result Helpers.ntName elementToTeX

        let paths = extractedPaths rules graph result.Matrices cnf.Start
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (String.concat "\n" paths)

        printfn
            "Azimov CFPQ (singlePath): %d productive iterations, %d reachable pairs"
            (List.length result.Steps)
            (List.length paths)
