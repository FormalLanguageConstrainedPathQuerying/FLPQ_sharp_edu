namespace FLPQ.Cli

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
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
            (TeXRenderer.inputRow SymbolTeX.terminalContent tokenList -1)

        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)
        Helpers.writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX cnf)

        if trace.Length > 0 then
            let initialStepDir = Path.Combine(outputDir, "step_0")
            Directory.CreateDirectory initialStepDir |> ignore
        else
            ()

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            let tex = ValiantTeX.stepToTeX step
            Helpers.writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

            if idx = trace.Length - 1 then
                let decomp = BooleanDecomposition.decompose step.table

                for (nt, mat) in decomp |> Map.toSeq |> Seq.sortBy (fun (nt, _) -> string nt) do
                    let ntName = string nt
                    let decompTex = ValiantTeX.boolDecompToTeX nt mat
                    Helpers.writeOutputFile (Path.Combine(stepDir, sprintf "bool_decomp_%s.tex" ntName)) decompTex

        printfn "Valiant trace: %d steps written to %s" trace.Length outputDir
