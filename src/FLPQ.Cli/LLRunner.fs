namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module LLRunner =

    let runLL (grammarFile: string) (inputFile: string) (outputDir: string) (k: int) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let tokens = Tokenizer.tokenizeTerminals inputTokens
        let table = LLParser.buildTable grammar k

        let firstMap = FirstFollow.firstK grammar k
        let followMap = FirstFollow.followK grammar k

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string grammar)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "ll_table.tex"))
            (LLTableTeX.tableToTeX (SymbolTeX.toLaTeX string string) grammar k firstMap followMap table)

        let _, steps = LLParser.parseWithSteps grammar table k tokens
        let vizSteps = LLStepVisualizer.renderSteps (SymbolTeX.toLaTeX string string) steps
        Helpers.writeStepsVisualization outputDir vizSteps
        printfn "LL(%d) trace: %d steps written to %s" k vizSteps.Length outputDir
