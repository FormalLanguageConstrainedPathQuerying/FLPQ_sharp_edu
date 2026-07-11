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

        let freshStart = Nonterminal("S'")
        let extRsm = RSM.extendWithStart freshStart rsm
        let lrTable = RnglrLR.buildLR0Table extRsm
        let lrStateCount = Dfa.stateCount lrTable.Automaton

        let inputText = Helpers.readFile inputFile
        let inputTokens = Tokenizer.tokenizeTerminals inputText
        let rawTokens = inputTokens |> List.map (fun (Terminal t) -> t)
        let inputGraph = GLL.stringToGraph rawTokens
        let vertexCount = Graph.vertexCount inputGraph

        let pathIndex = Rnglr.buildPathIndex freshStart rsm inputGraph

        let accepted = Rnglr.isAccepted pathIndex vertexCount

        let k = pathIndex.StateCount * pathIndex.VertexCount

        let mutable rootRanges = []

        for i in 0 .. k - 1 do
            let fromVertex = i % vertexCount

            if fromVertex = 0 then
                for j in 0 .. k - 1 do
                    let toVertex = j % vertexCount

                    if toVertex = vertexCount - 1 then
                        let entries = Matrix.get pathIndex.Matrix i j

                        if not (Set.isEmpty entries) then
                            let fromState = i / vertexCount
                            let toState = j / vertexCount

                            rootRanges <-
                                { FromState = fromState
                                  FromVertex = fromVertex
                                  ToState = toState
                                  ToVertex = toVertex }
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
