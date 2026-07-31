namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Printers

module RnglrRunner =

    let runRnglr (grammarFile: string) (inputFile: string) (outputDir: string) =
        let ebnfText = Helpers.readFile grammarFile
        let rsm = RsmBuilder.buildRSMFromText ebnfText

        let extRsm = ExtendedRSM.create (Nonterminal "S'") rsm
        let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm extRsm)
        let lrStateCount = Dfa.stateCount lrTable.Automaton

        let inputText = Helpers.readFile inputFile
        let inputTokens = Tokenizer.tokenizeTerminals inputText
        let rawTokens = inputTokens |> List.map (fun (Terminal t) -> t)
        let inputGraph = GLL.stringToGraph rawTokens
        let vertexCount = Graph.vertexCount inputGraph

        let pathIndex, steps, vertexInfoArr =
            Rnglr.buildPathIndexWithSteps (ExtendedRSM.freshStart extRsm) extRsm inputGraph

        let vertexInfo (idx: int) = vertexInfoArr.[idx]

        let accepted = PathIndex.isAccepted pathIndex extRsm vertexCount

        let sppf =
            Sppf.buildSppfFromExtendedRsm pathIndex (ExtendedRSM.extRsm extRsm) vertexCount

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_ebnf.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) inputTokens -1)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "rnglr_table.tex"))
            (RnglrTableTeX.tableToTeXTabularOnly string string lrTable)

        Helpers.writeOutputFile (Path.Combine(outputDir, "rsm_blocks.dot")) (RsmDot.toDot string string rsm)

        Helpers.writeOutputFile (Path.Combine(outputDir, "path_index.tex")) (PathIndexTeX.toTeX string string pathIndex)

        Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) (SppfDot.toDot string string sppf)

        let vizSteps =
            RnglrStepVisualizer.renderSteps
                string
                string
                lrTable
                lrStateCount
                vertexInfo
                steps
                pathIndex
                vertexCount
                inputGraph

        Helpers.writeRnglrStepsVisualization outputDir vizSteps

        let status = if accepted then "Accepted" else "Rejected"
        printfn "RNGLR: %s (%d tokens, %d steps) — %s" status inputTokens.Length steps.Length status
