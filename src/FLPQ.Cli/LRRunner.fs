namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module LRRunner =

    let runLR
        (grammarFile: string)
        (inputFile: string)
        (outputDir: string)
        (algo: AlgorithmTypes.Algorithm)
        (useDot: bool)
        =
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

        if useDot then
            match algo with
            | AlgorithmTypes.LR0
            | AlgorithmTypes.SLR1 ->
                Helpers.writeOutputFile
                    (Path.Combine(outputDir, "lr_automaton.dot"))
                    (AutomatonDot.dfaToDot
                        (SymbolTeX.toLaTeX string string)
                        (fun idx _ -> sprintf "State %d" idx)
                        (LRAutomaton.buildLR0 aug))
            | AlgorithmTypes.CLR1 ->
                Helpers.writeOutputFile
                    (Path.Combine(outputDir, "lr_automaton.dot"))
                    (AutomatonDot.dfaToDot
                        (SymbolTeX.toLaTeX string string)
                        (fun idx _ -> sprintf "State %d" idx)
                        (LRAutomaton.buildLR1 aug))
            | _ -> ()
        else
            match algo with
            | AlgorithmTypes.LR0
            | AlgorithmTypes.SLR1 ->
                let tikzContent =
                    LRAutomatonTikz.lr0AutomatontoTikz string string aug (LRAutomaton.buildLR0 aug)

                Helpers.writeOutputFile (Path.Combine(outputDir, "lr_automaton.tikz.tex")) tikzContent
            | AlgorithmTypes.CLR1 ->
                let tikzContent =
                    LRAutomatonTikz.lr1AutomatontoTikz string string aug (LRAutomaton.buildLR1 aug)

                Helpers.writeOutputFile (Path.Combine(outputDir, "lr_automaton.tikz.tex")) tikzContent
            | _ -> ()

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string grammar)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "lr_table.tex"))
            (LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table)

        let _, steps = LRParser.parseWithSteps aug table tokens
        let vizSteps = LRStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
        Helpers.writeStepsVisualization outputDir vizSteps
        printfn "%s trace: %d steps written to %s" (AlgorithmTypes.displayName algo) vizSteps.Length outputDir
