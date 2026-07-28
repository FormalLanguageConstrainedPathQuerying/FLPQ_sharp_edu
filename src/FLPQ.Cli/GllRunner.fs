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

        let sppf = Sppf.buildSppfFromExtendedRsm pathIndex ersm.ExtendedRsm vertexCount

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_ebnf.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile (Path.Combine(outputDir, "input.dot")) (InputGraphDot.toDot string inputGraph None)

        Helpers.writeOutputFile (Path.Combine(outputDir, "rsm_blocks.dot")) (RsmDot.toDot string string rsm)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "ext_rsm.dot"))
            (RsmDot.extendedRsmToDot string string ersm None)

        Helpers.writeOutputFile (Path.Combine(outputDir, "path_index.tex")) (PathIndexTeX.toTeX string string pathIndex)

        Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) (SppfDot.toDot string string sppf)

        // Write step-by-step visualization
        let vizSteps =
            GllStepVisualizer.renderSteps
                (SymbolTeX.toLaTeX string string)
                string
                string
                ersm
                steps
                pathIndex
                vertexCount
                inputGraph

        Helpers.writeGllStepsVisualization outputDir vizSteps

        let status = if accepted then "Accepted" else "Rejected"
        printfn "GLL: %s (%d tokens, %d steps) — %s" status inputTokens.Length steps.Length status
