namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.RPQ

/// Arroyuelo RPQ runner: parses the query regexp and the graph, evaluates the regexp over
/// the path semiring with a trace, and writes the root artifacts (regexp, DFA, graph, result)
/// plus one directory per trace step.
module ArroyueloRunner =

    let private vertexName (i: int) : string = sprintf "v_%d" i

    /// The result file: the final path semiring matrix plus one line per source listing its
    /// reachable vertices (the boolean projection from ArroyueloRPQ.evaluate).
    let private renderResult
        (boolResult: Matrix<bool>)
        (sources: int array)
        (finalMatrix: Matrix<Set<int list>>)
        : string =
        let matrixTex = PathSemiringTeX.matrixToTeX vertexName vertexName finalMatrix
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

    let runArroyuelo (regexpFile: string) (graphFile: string) (outputDir: string) (useDot: bool) : unit =
        let regexp = RpqInput.parseRegexpFile regexpFile
        let dfa = Regexp.toDfa regexp
        let graph = GraphReader.parseGraphFile graphFile

        let steps, finalMatrix = ArroyueloRPQ.evaluateWithTrace graph regexp

        Helpers.writeOutputFile (Path.Combine(outputDir, "regexp.tex")) (RegexpTeX.toTeX regexp)

        if useDot then
            Helpers.writeOutputFile
                (Path.Combine(outputDir, "dfa.dot"))
                (AutomatonDot.dfaToDot string (fun idx _ -> sprintf "q_%d" idx) dfa)

            let graphDot, _ =
                ArroyueloStepVisualizer.renderGraph string graph Set.empty Set.empty

            Helpers.writeOutputFile (Path.Combine(outputDir, "graph.dot")) graphDot
        else
            Helpers.writeOutputFile
                (Path.Combine(outputDir, "dfa.tikz.tex"))
                (AutomatonTikz.dfaToTikz string (fun idx _ -> sprintf "$q_%d$" idx) "circle" dfa)

            let _, graphTikz =
                ArroyueloStepVisualizer.renderGraph string graph Set.empty Set.empty

            Helpers.writeOutputFile (Path.Combine(outputDir, "graph.tikz.tex")) graphTikz

        let boolResult = ArroyueloRPQ.evaluate graph regexp
        let sources = graph.StartStates |> Set.toArray |> Array.sort

        Helpers.writeOutputFile (Path.Combine(outputDir, "result.tex")) (renderResult boolResult sources finalMatrix)

        let vizSteps = ArroyueloStepVisualizer.renderSteps string graph steps
        Helpers.writeArroyueloStepsVisualization outputDir useDot vizSteps

        let vCount = Nfa.stateCount graph

        let sourceStrs =
            sources |> Array.map vertexName |> Array.toList |> String.concat ", "

        let reachableStrs =
            [ for i in 0 .. sources.Length - 1 do
                  for j in 0 .. vCount - 1 do
                      if boolResult.[i, j] then
                          vertexName j ]
            |> List.distinct
            |> List.sort
            |> String.concat ", "

        printfn "Arroyuelo RPQ: %d steps, sources: [%s] -> reachable: [%s]" steps.Length sourceStrs reachableStrs
