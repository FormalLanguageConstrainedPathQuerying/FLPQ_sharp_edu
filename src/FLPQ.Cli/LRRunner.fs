namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module LRRunner =

    let runLR (grammarFile: string) (inputFile: string) (outputDir: string) (algo: AlgorithmTypes.Algorithm) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let tokens = Tokenizer.tokenizeTerminals inputTokens

        let freshStart = Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")

        let aug = LRAutomaton.augmentGrammar freshStart grammar

        let table =
            match algo with
            | AlgorithmTypes.LR0 -> LRParser.buildLR0Table aug
            | AlgorithmTypes.SLR1 -> LRParser.buildSLR1Table aug
            | AlgorithmTypes.CLR1 -> LRParser.buildCLR1Table aug
            | _ -> failwithf "Unexpected algorithm in LR runner: %A" algo

        match algo with
        | AlgorithmTypes.LR0
        | AlgorithmTypes.SLR1 ->
            Helpers.writeOutputFile
                (Path.Combine(outputDir, "lr_automaton.dot"))
                (AutomatonDot.dfaToDot
                    SymbolTeX.toLaTeX
                    (fun idx _ -> sprintf "State %d" idx)
                    (LRAutomaton.buildLR0 aug))
        | AlgorithmTypes.CLR1 ->
            Helpers.writeOutputFile
                (Path.Combine(outputDir, "lr_automaton.dot"))
                (AutomatonDot.dfaToDot
                    SymbolTeX.toLaTeX
                    (fun idx _ -> sprintf "State %d" idx)
                    (LRAutomaton.buildLR1 aug))
        | _ -> ()

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "lr_table.tex"))
            (LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table)

        let _, steps = LRParser.parseWithSteps aug table tokens
        let vizSteps = LRStepVisualizer.renderSteps SymbolTeX.toLaTeX steps
        Helpers.writeStepsVisualization outputDir vizSteps
        printfn "%s trace: %d steps written to %s" (AlgorithmTypes.displayName algo) vizSteps.Length outputDir
