namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.Printers

module GllRunner =

    let runGll (grammarFile: string) (inputFile: string) (outputDir: string) =
        let ebnfText = Helpers.readFile grammarFile
        let rsm = RsmBuilder.buildRSMFromText ebnfText
        let flat = RSM.flattenRsm rsm
        let startBlock = RSM.startBlock rsm
        let (Nonterminal startNtName) = startBlock.Nonterminal

        let startGlobalState =
            match flat.BlockStart.TryGetValue(Nonterminal startNtName) with
            | true, gs -> gs
            | false, _ -> failwithf "Start block %s not found in RSM" startNtName

        let startBlockOffset =
            rsm.Blocks
            |> List.takeWhile (fun b -> b.Nonterminal <> startBlock.Nonterminal)
            |> List.sumBy (fun b -> b.Dfa.States.Length)

        let startBlockFinalStates =
            startBlock.Dfa.FinalStates
            |> Set.map (fun localFinal -> startBlockOffset + localFinal)

        let inputText = Helpers.readFile inputFile
        let inputTokens = Tokenizer.tokenizeTerminals inputText
        let rawTokens = inputTokens |> List.map (fun (Terminal t) -> t)
        let inputGraph = GLL.stringToGraph rawTokens
        let vertexCount = Graph.vertexCount inputGraph

        let pathIndex = GLL.buildPathIndex rsm inputGraph (set [ 0 ])

        let accepted =
            GLL.isAccepted pathIndex startGlobalState 0 startBlockFinalStates vertexCount

        let startVertex = 0

        let mutable sppfRootRanges = []

        for finalLocal in startBlock.Dfa.FinalStates do
            let finalGlobalState = startBlockOffset + finalLocal

            let entries =
                PathIndex.get pathIndex startGlobalState startVertex finalGlobalState (vertexCount - 1)

            if not (Set.isEmpty entries) then
                sppfRootRanges <-
                    { FromState = startGlobalState
                      FromVertex = startVertex
                      ToState = finalGlobalState
                      ToVertex = vertexCount - 1 }
                    :: sppfRootRanges

        let sppf = Sppf.buildSppfFromIndex pathIndex (List.rev sppfRootRanges)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_ebnf.tex"))
            (GrammarTeX.grammarToTeX string string (RsmToGrammar.convert rsm))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) inputTokens -1)

        Helpers.writeOutputFile (Path.Combine(outputDir, "rsm_blocks.dot")) (RsmDot.toDot string string rsm)

        Helpers.writeOutputFile (Path.Combine(outputDir, "path_index.tex")) (PathIndexTeX.toTeX string string pathIndex)

        Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) (SppfDot.toDot string string sppf)

        let status = if accepted then "Accepted" else "Rejected"
        printfn "GLL: %s (%d tokens) — %s" status inputTokens.Length status
