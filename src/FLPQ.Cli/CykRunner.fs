namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module CykRunner =

    let runCyk (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens
        let trace = Cyk.parseWithTrace Grammar.freshStringNonterminal grammar tokenList

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow SymbolTeX.terminalContent tokenList -1)

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)
        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX cnf)

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                if step.highlights.IsEmpty then
                    CykTeX.tableToTeX step.table
                else
                    CykTeX.tableToTeXStyled step.table step.highlights

            Helpers.writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        printfn "CYK trace: %d steps written to %s" trace.Length outputDir
