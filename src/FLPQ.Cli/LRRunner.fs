namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module LRRunner =

    let runLR (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let tokens = Tokenizer.tokenizeTerminals inputTokens

        let freshStart = Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")

        let aug = LRAutomaton.augmentGrammar freshStart grammar
        let table = LRParser.buildSLR1Table aug
        let automaton = LRAutomaton.buildLR0 aug

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "lr_table.tex"))
            (LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "lr_automaton.dot"))
            (AutomatonDot.dfaToDot SymbolTeX.toLaTeX (fun idx _ -> sprintf "State %d" idx) automaton)

        let _, steps = LRParser.parseWithSteps aug table tokens
        let vizSteps = LRStepVisualizer.renderSteps SymbolTeX.toLaTeX steps
        Helpers.writeStepsVisualization outputDir vizSteps
        printfn "LR trace: %d steps written to %s" vizSteps.Length outputDir
