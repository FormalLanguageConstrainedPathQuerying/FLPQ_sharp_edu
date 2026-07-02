namespace FLPQ.Cli

open System.IO
open Argu
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

module Program =

    type Algorithm =
        | CYK
        | Valiant
        | LL
        | LR

    type Arguments =
        | [<AltCommandLine("-a")>] Algorithm of Algorithm
        | [<AltCommandLine("-g")>] Grammar of string
        | [<AltCommandLine("-i")>] Input of string
        | [<AltCommandLine("-o")>] Output of string
        | [<AltCommandLine("-k")>] Lookahead of int

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Algorithm _ -> "Parsing algorithm: CYK, Valiant, LL, or LR"
                | Grammar _ -> "Path to grammar file (.bnf format)"
                | Input _ -> "Path to input string file"
                | Output _ -> "Output directory for step-by-step visualization"
                | Lookahead _ -> "Lookahead k for LL parser (default: 1)"

    let private readFile path =
        if not (File.Exists path) then
            failwithf "File not found: %s" path

        File.ReadAllText(path).Trim()

    let private writeOutputFile (path: string) (content: string) =
        let dir = System.IO.Path.GetDirectoryName path

        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore

        System.IO.File.WriteAllText(path, content)

    let private writeStepsVisualization (outputDir: string) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].treeAndStack
            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].input

    let private symbolPrinter (sym: Symbol<string, string>) =
        match sym with
        | T(Terminal t) -> string t
        | N(Nonterminal n) -> string n
        | Epsilon -> "\\varepsilon"

    let private runCyk (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens
        let trace = Cyk.parseWithTrace Grammar.freshStringNonterminal grammar tokenList
        let inputSymbols = tokenList |> List.map (fun (Terminal t) -> T(Terminal t))

        writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow
                (fun sym ->
                    match sym with
                    | T(Terminal t) -> string t
                    | N n -> string n
                    | Epsilon -> "\\varepsilon")
                inputSymbols
                -1)

        writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)
        writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX cnf)

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                if step.highlights.IsEmpty then
                    CykTeX.tableToTeX step.table
                else
                    CykTeX.tableToTeXStyled step.table step.highlights

            writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        printfn "CYK trace: %d steps written to %s" trace.Length outputDir

    let private runValiant (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens
        let trace = Valiant.parseWithTrace Grammar.freshStringNonterminal grammar tokenList
        let inputSymbols = tokenList |> List.map (fun (Terminal t) -> T(Terminal t))

        writeOutputFile
            (Path.Combine(outputDir, "input.tex"))
            (TeXRenderer.inputRow
                (fun sym ->
                    match sym with
                    | T(Terminal t) -> string t
                    | N n -> string n
                    | Epsilon -> "\\varepsilon")
                inputSymbols
                -1)

        writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)
        writeOutputFile (Path.Combine(outputDir, "grammar_cnf.tex")) (GrammarTeX.grammarToTeX cnf)

        if trace.Length > 0 then
            let initialStepDir = Path.Combine(outputDir, "step_0")
            Directory.CreateDirectory initialStepDir |> ignore
        else
            ()

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            let tex = ValiantTeX.stepToTeX step
            writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

            if idx = trace.Length - 1 then
                let decomp = BooleanDecomposition.decompose step.table

                for (nt, mat) in decomp |> Map.toSeq |> Seq.sortBy (fun (nt, _) -> string nt) do
                    let ntName = string nt
                    let decompTex = ValiantTeX.boolDecompToTeX nt mat
                    writeOutputFile (Path.Combine(stepDir, sprintf "bool_decomp_%s.tex" ntName)) decompTex

        printfn "Valiant trace: %d steps written to %s" trace.Length outputDir

    let private runLL (grammarFile: string) (inputFile: string) (outputDir: string) (k: int) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let tokens = Tokenizer.tokenizeTerminals inputTokens
        let table = LLParser.buildTable grammar k

        let firstMap = FirstFollow.firstK grammar k
        let followMap = FirstFollow.followK grammar k

        writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)

        writeOutputFile
            (Path.Combine(outputDir, "ll_table.tex"))
            (LLTableTeX.tableToTeX symbolPrinter grammar k firstMap followMap table)

        let _, steps = LLParser.parseWithSteps grammar table k tokens
        let vizSteps = LLStepVisualizer.renderSteps string steps
        writeStepsVisualization outputDir vizSteps
        printfn "LL(%d) trace: %d steps written to %s" k vizSteps.Length outputDir

    let private runLR (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let tokens = Tokenizer.tokenizeTerminals inputTokens

        let freshStart = Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")

        let aug = LRAutomaton.augmentGrammar freshStart grammar
        let table = LRParser.buildSLR1Table aug
        let automaton = LRAutomaton.buildLR0 aug

        writeOutputFile (Path.Combine(outputDir, "grammar_original.tex")) (GrammarTeX.grammarToTeX grammar)
        writeOutputFile (Path.Combine(outputDir, "lr_table.tex")) (LRTableTeX.tableToTeX symbolPrinter aug table)

        writeOutputFile
            (Path.Combine(outputDir, "lr_automaton.dot"))
            (AutomatonDot.dfaToDot (fun idx _ -> sprintf "State %d" idx) automaton)

        let _, steps = LRParser.parseWithSteps aug table tokens
        let vizSteps = LRStepVisualizer.renderSteps string steps
        writeStepsVisualization outputDir vizSteps
        printfn "LR trace: %d steps written to %s" vizSteps.Length outputDir

    [<EntryPoint>]
    let main argv =
        let parser = ArgumentParser.Create<Arguments>(programName = "flpq")

        try
            let results = parser.ParseCommandLine argv
            let algorithm = results.GetResult Algorithm
            let grammar = results.GetResult Grammar
            let input = results.GetResult Input
            let output = results.GetResult(Output, defaultValue = "output")
            let k = results.GetResult(Lookahead, defaultValue = 1)

            match algorithm with
            | Algorithm.CYK -> runCyk grammar input output
            | Algorithm.Valiant -> runValiant grammar input output
            | Algorithm.LL -> runLL grammar input output k
            | Algorithm.LR -> runLR grammar input output

            0
        with ex ->
            eprintfn "%s" ex.Message
            eprintfn "%s" (parser.PrintUsage())
            1
