namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.Printers

module ValiantRunner =

    let runValiant (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens
        let trace = Valiant.parseWithTrace Grammar.freshStringNonterminal grammar tokenList

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokenList -1)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string grammar)

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX string string cnf)

        if trace.Length > 0 then
            let initialStepDir = Path.Combine(outputDir, "step_0")
            Directory.CreateDirectory initialStepDir |> ignore
        else
            ()

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex = ValiantTeX.stepToTeX string step
            Helpers.writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        if not (List.isEmpty tokenList) then
            let sppfTable =
                Valiant.parseWithSppfInfo Grammar.freshStringNonterminal grammar tokenList

            let sppf = BasicSppf.fromParsingTable cnf sppfTable
            let dot = BasicSppfDot.toDot id id sppf
            Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) dot

        printfn "Valiant trace: %d steps written to %s" trace.Length outputDir

    let runValiantModified (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = Helpers.readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens

        let trace =
            Valiant.parseModifiedWithTrace Grammar.freshStringNonterminal grammar tokenList

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow (SymbolTeX.terminalContent string) tokenList -1)

        Helpers.writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeX string string grammar)

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX string string cnf)

        if trace.Length > 0 then
            let initialStepDir = Path.Combine(outputDir, "step_0")
            Directory.CreateDirectory initialStepDir |> ignore
        else
            ()

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex = ValiantTeX.modifiedStepToTeX string step
            Helpers.writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        if not (List.isEmpty tokenList) then
            let sppfTable =
                Valiant.parseModifiedWithSppfInfo Grammar.freshStringNonterminal grammar tokenList

            let sppf = BasicSppf.fromParsingTable cnf sppfTable
            let dot = BasicSppfDot.toDot id id sppf
            Helpers.writeOutputFile (Path.Combine(outputDir, "sppf.dot")) dot

        printfn "Modified Valiant trace: %d steps written to %s" trace.Length outputDir
