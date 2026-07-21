namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers

module GllRunner =

    let runGll (grammarFile: string) (inputFile: string) (outputDir: string) =
        let ebnfText = Helpers.readFile grammarFile
        let rsm = RsmBuilder.buildRSMFromText ebnfText

        let inputText = Helpers.readFile inputFile
        let inputTokens = Tokenizer.tokenizeTerminals inputText
        let rawTokens = inputTokens |> List.map (fun (Terminal t) -> t)
        let inputGraph = GLL.stringToGraph rawTokens
        let vertexCount = Graph.vertexCount inputGraph

        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart rsm
        let pathIndex, steps = GLL.buildPathIndexWithSteps freshStart ersm inputGraph

        let accepted = PathIndex.isAccepted pathIndex ersm vertexCount

        let flatExt = ersm.ExtendedRsm

        let startGlobalState =
            match flatExt.BlockStart.TryGetValue(flatExt.StartBlock) with
            | true, gs -> gs
            | false, _ -> failwith "Start block not found in extended RSM"

        let finalGlobalState = startGlobalState + 1

        let rootRanges =
            let entries =
                PathIndex.get pathIndex startGlobalState 0 finalGlobalState (vertexCount - 1)

            if not (Set.isEmpty entries) then
                [ { FromState = startGlobalState
                    FromVertex = 0
                    ToState = finalGlobalState
                    ToVertex = vertexCount - 1 } ]
            else
                []

        let sppf =
            Sppf.buildSppfFromIndex
                pathIndex
                rootRanges
                (Some(flatExt.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
                (Some(RSM.blockFinalsMap flatExt))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_ebnf.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) inputTokens -1)

        Helpers.writeOutputFile (Path.Combine(outputDir, "rsm_blocks.dot")) (RsmDot.toDot string string rsm)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "ext_rsm.dot"))
            (RsmDot.extendedRsmToDot string string ersm None)

        Helpers.writeOutputFile (Path.Combine(outputDir, "path_index.tex")) (PathIndexTeX.toTeX string string pathIndex)

        Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) (SppfDot.toDot string string sppf)

        // Write step-by-step visualization
        let stateLabel (state: int) : string =
            let info = flatExt.StateInfo.[state]
            let (Nonterminal ntName) = info.BlockNonterminal
            sprintf "<%s>" ntName

        let vizSteps =
            GllStepVisualizer.renderSteps
                (SymbolTeX.toLaTeX string string)
                stateLabel
                string
                string
                ersm
                steps
                pathIndex
                inputTokens
                vertexCount

        Helpers.writeGllStepsVisualization outputDir vizSteps

        let status = if accepted then "Accepted" else "Rejected"
        printfn "GLL: %s (%d tokens, %d steps) — %s" status inputTokens.Length steps.Length status
