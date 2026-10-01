namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.RPQ

/// Belyanin RPQ reachability runner: parses the query regexp and the graph, runs the
/// Boolean BFS-like traversal (the classical RPQ reachability semantics), and writes the
/// root artifacts (regexp, DFA, graph, result) plus one directory per trace step. The
/// reported answer comes from `BelyaninRPQ.evaluate` (all sources); the trace and step
/// visualization cover a single source (the first start vertex) because Boolean cells do
/// not identify their origin.
module BelyaninReachabilityRunner =

    let private vertexName (i: int) : string = sprintf "v_%d" i

    /// The result file: the final accumulated Boolean reachability matrix for the traced
    /// source (`q_i`/`v_i` labels, no title) plus one line per source listing its reachable
    /// vertices (derived from `BelyaninRPQ.evaluate`).
    let private renderResult (boolResult: Matrix<bool>) (sources: int array) (finalP: Matrix<bool>) : string =
        let matrixTex =
            PathSemiringTeX.boolMatrixToTeX BelyaninStepCommon.stateLabel BelyaninStepCommon.vertexLabel finalP

        let vCount = Matrix.cols boolResult

        let sourceLines =
            sources
            |> Array.mapi (fun i s ->
                let reachable =
                    [ for j in 0 .. vCount - 1 do
                          if boolResult.[i, j] then
                              j ]

                if List.isEmpty reachable then
                    sprintf "Source %s: reachable (none)" (vertexName s)
                else
                    sprintf
                        "Source %s: reachable %s"
                        (vertexName s)
                        (reachable |> List.map vertexName |> String.concat ", "))
            |> Array.toList

        matrixTex + "\n\n" + String.concat "\n" sourceLines

    /// The query is the regexp file and the input is the graph file (unified CLI `-q`/`-i`).
    let runBelyanin (queryFile: string) (inputFile: string) (outputDir: string) (useDot: bool) : unit =
        let regexp = RpqInput.parseRegexpFile queryFile
        let dfa = Regexp.toDfa regexp
        let graph = GraphReader.parseGraphFile inputFile

        let steps, finalP = BelyaninRPQ.evaluateReachabilityWithTrace dfa graph
        let boolResult = BelyaninRPQ.evaluate dfa graph

        Helpers.writeRpqRootArtifacts outputDir useDot regexp dfa graph

        let sources = graph.StartStates |> Set.toArray |> Array.sort
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (renderResult boolResult sources finalP)

        let vizSteps = BelyaninReachabilityStepVisualizer.renderSteps id dfa graph steps
        Helpers.writeBelyaninStepsVisualization outputDir useDot vizSteps

        let sourceStrs =
            sources |> Array.map vertexName |> Array.toList |> String.concat ", "

        let reachableStrs =
            [ for i in 0 .. sources.Length - 1 do
                  for j in 0 .. Matrix.cols boolResult - 1 do
                      if boolResult.[i, j] then
                          vertexName j ]
            |> List.distinct
            |> List.sort
            |> String.concat ", "

        printfn
            "Belyanin RPQ (reachability): %d steps, sources: [%s] -> reachable: [%s]"
            steps.Length
            sourceStrs
            reachableStrs
