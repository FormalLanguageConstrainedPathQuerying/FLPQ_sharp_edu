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
        let tokensWithEoi = tokens @ [ Grammar.eoiTerminal ]

        let freshStart = Nonterminal(grammar.Start |> fun (Nonterminal n) -> n + "'")

        let extGram = ExtendedGrammar.create freshStart grammar
        let aug = ExtendedGrammar.extGrammar extGram

        let table =
            match algo with
            | AlgorithmTypes.LR0 -> LRParser.buildLR0Table aug
            | AlgorithmTypes.SLR1 -> LRParser.buildSLR1Table aug
            | AlgorithmTypes.CLR1 -> LRParser.buildCLR1Table aug
            | _ -> failwithf "Unexpected algorithm in LR runner: %A" algo

        if useDot then
            let dotContent =
                match table.Automaton with
                | LR0 dfa ->
                    AutomatonDot.dfaToDot (SymbolTeX.toLaTeX string string) (fun idx _ -> sprintf "State %d" idx) dfa
                | LR1 dfa ->
                    AutomatonDot.dfaToDot (SymbolTeX.toLaTeX string string) (fun idx _ -> sprintf "State %d" idx) dfa

            Helpers.writeOutputFile (Path.Combine(outputDir, "lr_automaton.dot")) dotContent
        else
            let tikzContent =
                match table.Automaton with
                | LR0 dfa ->
                    let labelPrinter = SymbolTeX.toLaTeX string string

                    let stateVisualizer stateIdx items =
                        LRAutomatonTikz.stateContentToTikzAs (
                            LRAutomatonTikz.renderLR0StateContent string string stateIdx items
                        )

                    LRAutomatonTikz.lr0AutomatonToTikz labelPrinter stateVisualizer "rectangle" dfa
                | LR1 dfa ->
                    let labelPrinter = SymbolTeX.toLaTeX string string

                    let stateVisualizer stateIdx items =
                        LRAutomatonTikz.stateContentToTikzAs (
                            LRAutomatonTikz.renderLR1StateContent string string stateIdx items
                        )

                    LRAutomatonTikz.lr1AutomatonToTikz labelPrinter stateVisualizer "rectangle" dfa

            Helpers.writeOutputFile (Path.Combine(outputDir, "lr_automaton.tikz.tex")) tikzContent

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string (ExtendedGrammar.originalGrammar extGram))

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "lr_table.tex"))
            (LRTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) aug table)

        let _, steps = LRParser.parseWithSteps aug table tokensWithEoi
        let vizSteps = LRStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
        Helpers.writeStepsVisualization outputDir vizSteps
        printfn "%s trace: %d steps written to %s" (AlgorithmTypes.displayName algo) vizSteps.Length outputDir
