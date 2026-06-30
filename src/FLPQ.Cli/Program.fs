namespace FLPQ.Cli

open System.IO
open Argu
open FLPQ.Languages
open FLPQ.LinearAlgebra

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

    let private writeOutputFile path content =
        let dir = Path.GetDirectoryName path

        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, content)

    let private writeStepsVisualization (outputDir: string) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "tree.dot")) steps.[idx].tree
            writeOutputFile (Path.Combine(stepDir, "stack.tex")) steps.[idx].stack
            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].input

    let private runCyk (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile

        let trace =
            Cyk.parseWithTrace Grammar.freshStringNonterminal grammar (Tokenizer.tokenize inputTokens)

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                if step.highlights.IsEmpty then
                    Cyk.tableToTeX string step.table
                else
                    Cyk.tableToTeXStyled string step.table step.highlights

            writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        printfn "CYK trace: %d steps written to %s" trace.Length outputDir

    let private runValiant (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile

        let trace =
            Valiant.parseWithTrace Grammar.freshStringNonterminal grammar (Tokenizer.tokenize inputTokens)

        for idx in 0 .. trace.Length - 1 do
            let step = trace.[idx]
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            Directory.CreateDirectory stepDir |> ignore

            let tex =
                Matrix.toTeX false false (fun s -> if Set.isEmpty s then @"\cdot" else string s) step.table

            writeOutputFile (Path.Combine(stepDir, "table.tex")) tex

        printfn "Valiant trace: %d steps written to %s" trace.Length outputDir

    let private runLL (grammarFile: string) (inputFile: string) (outputDir: string) (k: int) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let tokens = Tokenizer.tokenize inputTokens
        let table = LLParser.buildTable grammar k

        let steps = LLVisualizer.visualizeSteps string grammar table k tokens
        writeStepsVisualization outputDir steps
        printfn "LL(%d) trace: %d steps written to %s" k steps.Length outputDir

    let private runLR (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let tokens = Tokenizer.tokenize inputTokens

        let freshStart = Nonterminal(grammar.start |> fun (Nonterminal n) -> n + "'")

        let aug = LRAutomaton.augmentGrammar freshStart grammar
        let table = LRParser.buildSLR1Table aug

        let steps = LRVisualizer.visualizeSteps string aug table tokens
        writeStepsVisualization outputDir steps
        printfn "LR trace: %d steps written to %s" steps.Length outputDir

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
