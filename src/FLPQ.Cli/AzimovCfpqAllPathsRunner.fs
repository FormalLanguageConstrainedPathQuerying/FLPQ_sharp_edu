namespace FLPQ.Cli

open System.IO
open FSharpPlus.Data
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

/// Azimov CFPQ all-paths runner (LAGraph PR #9): the fixed point runs over the split-vertex
/// semiring, so every cell records all middle vertices of all derivations; enumeration with a
/// visited set then yields exactly all simple paths. The result file lists all simple paths per
/// reachable pair; the step tables show the split sets (• marks a direct derivation).
module AzimovCfpqAllPathsRunner =

    let private vertexName (i: int) : string = sprintf "v_%d" i

    /// A split-set cell as text: • for a direct derivation, otherwise the sorted split vertices
    /// (the sentinel renders as d for "direct").
    let private elementToTeX (e: CfpqSemiring.AllPathsElem) : string =
        match e with
        | CfpqSemiring.Base -> @"\bullet"
        | CfpqSemiring.Splits ks ->
            NonEmptyList.toList ks
            |> List.map (fun k ->
                if k = CfpqSemiring.allPathsSentinel then
                    "d"
                else
                    sprintf "v_%d" k)
            |> String.concat ", "

    /// All simple paths per reachable pair of T[Start], in row-major order.
    let private allPathsPerPair
        (rules: AzimovCfpq.WcnfRule<string, string> list)
        (graph: CfpqInput.CfpqGraph<string>)
        (matrices: Map<Nonterminal<string>, Matrix<CfpqSemiring.AllPathsElem option>>)
        (start: Nonterminal<string>)
        : string list =
        let m = Map.find start matrices

        [ for i in 0 .. Matrix.rows m - 1 do
              for j in 0 .. Matrix.cols m - 1 do
                  if m.[i, j].IsSome then
                      let paths = AzimovCfpq.enumerateAllPaths rules graph matrices start i j

                      yield sprintf @"%s, %s: %d path(s)" (vertexName i) (vertexName j) (List.length paths)
                      yield! (paths |> List.map (fun p -> "    " + CfpqMatrixTeX.pathToText id p)) ]

    /// The query is the .cnf grammar file and the input is the graph directory (unified CLI `-q`/`-i`).
    let runAzimov (queryFile: string) (inputDir: string) (outputDir: string) (useDot: bool) : unit =
        let grammar = CfpqInput.parseCnfFile queryFile
        let graph = CfpqInput.parseGraphDir inputDir

        // The same CNF conversion evaluate applies internally; enumeration walks its rules.
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar
        let rules = AzimovCfpq.classifyRules cnf

        let result =
            AzimovCfpq.evaluate CfpqSemiring.AllPathsSemiring Grammar.freshStringNonterminal grammar graph

        Helpers.writeCfpqRootArtifacts outputDir useDot grammar cnf graph
        Helpers.writeCfpqSteps outputDir result.Steps Helpers.ntName elementToTeX

        let lines = allPathsPerPair rules graph result.Matrices cnf.Start
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (String.concat "\n" lines)

        let pairCount =
            lines |> List.filter (fun l -> not (l.StartsWith("    "))) |> List.length

        let pathCount = lines |> List.filter (fun l -> l.StartsWith("    ")) |> List.length

        printfn
            "Azimov CFPQ (allPaths): %d productive iterations, %d reachable pairs, %d simple paths"
            (List.length result.Steps)
            pairCount
            pathCount
