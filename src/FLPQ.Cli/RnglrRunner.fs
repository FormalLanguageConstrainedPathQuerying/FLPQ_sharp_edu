namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Printers

module RnglrRunner =

    let runRnglr (grammarFile: string) (inputFile: string) (outputDir: string) (useDot: bool) =
        let ebnfText = Helpers.readFile grammarFile
        let rsm = RsmBuilder.buildRSMFromText ebnfText

        let extRsm = ExtendedRSM.create (Nonterminal "S'") rsm
        let lrTable = RnglrLR.buildLR0Table (ExtendedRSM.extRsm extRsm)

        let inputText = Helpers.readFile inputFile
        let inputTokens = Tokenizer.tokenizeTerminals inputText
        let rawTokens = inputTokens |> List.map (fun (Terminal t) -> t)
        let inputGraph = GLL.stringToGraph rawTokens
        let vertexCount = Graph.vertexCount inputGraph

        let result =
            Rnglr.buildPathIndexWithSteps (ExtendedRSM.freshStart extRsm) extRsm inputGraph

        let pathIndex = result.PathIndex
        let steps = result.Steps
        let vertexInfoArr = result.VertexInfo

        let vertexInfo (idx: int) = vertexInfoArr.[idx]

        let accepted = PathIndex.isAccepted pathIndex extRsm vertexCount

        let sppf =
            Sppf.buildSppfFromExtendedRsm pathIndex (ExtendedRSM.extRsm extRsm) vertexCount

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_ebnf.tex"))
            (GrammarTeX.grammarToTeXWithNumbers string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) inputTokens -1)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "rnglr_table.tex"))
            (RnglrTableTeX.tableToTeXTabularOnly string string lrTable)

        Helpers.writeOutputFile (Path.Combine(outputDir, "path_index.tex")) (PathIndexTeX.toTeX string string pathIndex)

        Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) (SppfDot.toDot string string sppf)

        if useDot then
            Helpers.writeOutputFile
                (Path.Combine(outputDir, "ext_rsm.dot"))
                (RsmDot.extendedRsmToDot string string extRsm None)

            // DOT state labels carry the same item lines as the TikZ rendering: a "State N"
            // header plus one "<nt> : <rsmState>" line per item.
            let automatonDot =
                AutomatonDot.dfaToDot
                    (SymbolTeX.toLaTeX string string)
                    (fun idx items ->
                        let lines =
                            (sprintf "State %d" idx)
                            :: (items |> Set.toList |> List.map (RnglrAutomatonTikz.renderRnglrItem string))

                        String.concat "\n" lines)
                    lrTable.Automaton

            Helpers.writeOutputFile (Path.Combine(outputDir, "lr_automaton.dot")) automatonDot
        else
            Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.tikz.tex")) (SppfTikz.toTikz string string sppf)

            Helpers.writeOutputFile
                (Path.Combine(outputDir, "input.tikz.tex"))
                (InputGraphTikz.toTikz string inputGraph None)

            Helpers.writeOutputFile
                (Path.Combine(outputDir, "ext_rsm.tikz.tex"))
                (RsmTikz.extendedRsmToTikz string string extRsm None)

            Helpers.writeOutputFile
                (Path.Combine(outputDir, "lr_automaton.tikz.tex"))
                (RnglrAutomatonTikz.rnglrAutomatonToTikz (SymbolTeX.toLaTeX string string) string lrTable.Automaton)

        let vizSteps =
            RnglrStepVisualizer.renderSteps string string lrTable vertexInfo steps pathIndex inputGraph

        Helpers.writeRnglrStepsVisualization outputDir useDot vizSteps

        let status = if accepted then "Accepted" else "Rejected"
        printfn "RNGLR: %s (%d tokens, %d steps) — %s" status inputTokens.Length steps.Length status
