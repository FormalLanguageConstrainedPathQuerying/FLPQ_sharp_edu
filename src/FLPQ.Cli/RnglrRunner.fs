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

        let pathIndex = Rnglr.buildPathIndex (ExtendedRSM.freshStart extRsm) rsm inputGraph

        let accepted = Rnglr.isAccepted pathIndex vertexCount

        let flatExt = RSM.flattenRsm (ExtendedRSM.extRsm extRsm)
        let originalStartBlock = ExtendedRSM.originalStartBlock extRsm

        let startGlobalState =
            match flatExt.BlockStart.TryGetValue(originalStartBlock.Nonterminal) with
            | true, gs -> gs
            | false, _ -> failwithf "Start block %A not found in extended RSM" originalStartBlock.Nonterminal

        let mutable rootRanges = []

        for finalLocal in originalStartBlock.Dfa.FinalStates do
            let startBlockOffset =
                ExtendedRSM.extBlocks extRsm
                |> List.takeWhile (fun b -> b.Nonterminal <> originalStartBlock.Nonterminal)
                |> List.sumBy (fun b -> b.Dfa.States.Length)

            let finalGlobalState = startBlockOffset + finalLocal

            let entries =
                PathIndex.get pathIndex startGlobalState 0 finalGlobalState (vertexCount - 1)

            if not (Set.isEmpty entries) then
                rootRanges <-
                    { FromState = startGlobalState
                      FromVertex = 0
                      ToState = finalGlobalState
                      ToVertex = vertexCount - 1 }
                    :: rootRanges

        let sppf = Sppf.buildSppfFromIndex pathIndex (List.rev rootRanges)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_ebnf.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) inputTokens -1)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "rnglr_table.tex"))
            (RnglrTableTeX.tableToTeX string string lrTable)

        Helpers.writeOutputFile (Path.Combine(outputDir, "rsm_blocks.dot")) (RsmDot.toDot string string rsm)

        Helpers.writeOutputFile (Path.Combine(outputDir, "path_index.tex")) (PathIndexTeX.toTeX string string pathIndex)

        Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) (SppfDot.toDot string string sppf)

        let status = if accepted then "Accepted" else "Rejected"
        printfn "RNGLR: %s (%d tokens) — %s" status inputTokens.Length status
