namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module CykRunner =

    let runCyk (grammarFile: string) (inputFile: string) (outputDir: string) (useDot: bool) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens
        let trace = Cyk.parseWithSppfTrace Grammar.freshStringNonterminal grammar tokenList

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokenList -1)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string grammar)

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX string string cnf)

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                if step.Highlights.IsEmpty then
                    CykTeX.sppfTableToTeX string step.Table
                else
                    CykTeX.sppfTableToTeXStyled string step.Table step.Highlights

            Helpers.writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        if not (List.isEmpty tokenList) then
            let sppfTable =
                Cyk.parseWithSppfInfo Grammar.freshStringNonterminal grammar tokenList

            let sppf = BasicSppf.fromParsingTable cnf sppfTable

            if useDot then
                let dot = BasicSppfDot.toDot id id sppf
                Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) dot
            else
                let tikz = BasicSppfTikz.toTikz id id sppf
                Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.tikz.tex")) tikz

        printfn "CYK trace: %d steps written to %s" trace.Length outputDir
