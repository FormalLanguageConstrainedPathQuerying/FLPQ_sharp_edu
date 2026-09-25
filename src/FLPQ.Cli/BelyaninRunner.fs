namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.RPQ

/// Belyanin RPQ runner: parses the query regexp and the graph, runs the BFS-like traversal
/// over the path semiring with a trace, and writes the root artifacts (regexp, DFA, graph,
/// result) plus one directory per trace step.
module BelyaninRunner =

    let private vertexName (i: int) : string = sprintf "v_%d" i

    /// The result file: the final accumulated path semiring matrix (q_i/v_i labels, no
    /// title) plus one line per source listing its reachable vertices (derived from the
    /// matrix via path heads at final states — the trace is the single source of truth).
    let private renderResult (dfa: DFA<string, int>) (p: Matrix<Set<int list>>) (sources: int array) : string =
        let matrixTex = PathSemiringTeX.matrixWithStateVertexLabels p
        let reachable = BelyaninRPQ.reachableFromPaths dfa p sources

        let sourceLines =
            reachable
            |> List.map (fun (s, vs) ->
                if List.isEmpty vs then
                    sprintf "Source %s: reachable (none)" (vertexName s)
                else
                    sprintf "Source %s: reachable %s" (vertexName s) (vs |> List.map vertexName |> String.concat ", "))

        matrixTex + "\n\n" + String.concat "\n" sourceLines

    /// The query is the regexp file and the input is the graph file (unified CLI `-q`/`-i`).
    let runBelyanin (queryFile: string) (inputFile: string) (outputDir: string) (useDot: bool) : unit =
        let regexp = RpqInput.parseRegexpFile queryFile
        let dfa = Regexp.toDfa regexp
        let graph = GraphReader.parseGraphFile inputFile

        let steps, finalP = BelyaninRPQ.evaluateWithTrace dfa graph

        Helpers.writeRpqRootArtifacts outputDir useDot regexp dfa graph

        let sources = graph.StartStates |> Set.toArray |> Array.sort
        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (renderResult dfa finalP sources)

        let vizSteps = BelyaninStepVisualizer.renderSteps id dfa graph steps
        Helpers.writeBelyaninStepsVisualization outputDir useDot vizSteps

        let sourceStrs =
            sources |> Array.map vertexName |> Array.toList |> String.concat ", "

        let reachableStrs =
            BelyaninRPQ.reachableFromPaths dfa finalP sources
            |> List.collect snd
            |> List.distinct
            |> List.sort
            |> List.map vertexName
            |> String.concat ", "

        printfn "Belyanin RPQ: %d steps, sources: [%s] -> reachable: [%s]" steps.Length sourceStrs reachableStrs
