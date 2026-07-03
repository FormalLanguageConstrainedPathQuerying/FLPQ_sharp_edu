namespace FLPQ.Cli

open System
open System.IO
open System.Text.RegularExpressions
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
        | [<AltCommandLine("-s")>] Summary

        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | Algorithm _ -> "Parsing algorithm: CYK, Valiant, LL, or LR"
                | Grammar _ -> "Path to grammar file (.bnf format)"
                | Input _ -> "Path to input string file"
                | Output _ -> "Output directory for step-by-step visualization"
                | Lookahead _ -> "Lookahead k for LL parser (default: 1)"
                | Summary -> "Build summary PDF (compiles Dot and TeX via Graphviz and pdflatex)"

    let private readFile path =
        if not (File.Exists path) then
            failwithf "File not found: %s" path

        File.ReadAllText(path).Trim()

    let private writeOutputFile (path: string) (content: string) =
        let dir = Path.GetDirectoryName path

        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, content)

    let private writeStepsVisualization (outputDir: string) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].treeAndStack
            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].input

    let private runCyk (grammarFile: string) (inputFile: string) (outputDir: string) =
        let grammar = Grammar.parseGrammarFromFile grammarFile
        let inputTokens = readFile inputFile
        let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

        let tokenList = Tokenizer.tokenizeTerminals inputTokens
        let trace = Cyk.parseWithTrace Grammar.freshStringNonterminal grammar tokenList

        let termPrinter (Terminal t) = t

        writeOutputFile (Path.Combine(outputDir, "input.tex")) (TeXRenderer.inputRow termPrinter tokenList -1)

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

        let termPrinter (Terminal t) = t

        writeOutputFile (Path.Combine(outputDir, "input.tex")) (TeXRenderer.inputRow termPrinter tokenList -1)

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
            (LLTableTeX.tableToTeX SymbolTeX.toLaTeX grammar k firstMap followMap table)

        let _, steps = LLParser.parseWithSteps grammar table k tokens
        let vizSteps = LLStepVisualizer.renderSteps SymbolTeX.toLaTeX steps
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
        writeOutputFile (Path.Combine(outputDir, "lr_table.tex")) (LRTableTeX.tableToTeX SymbolTeX.toLaTeX aug table)

        writeOutputFile
            (Path.Combine(outputDir, "lr_automaton.dot"))
            (AutomatonDot.dfaToDot (fun idx _ -> sprintf "State %d" idx) automaton)

        let _, steps = LRParser.parseWithSteps aug table tokens
        let vizSteps = LRStepVisualizer.renderSteps SymbolTeX.toLaTeX steps
        writeStepsVisualization outputDir vizSteps
        printfn "LR trace: %d steps written to %s" vizSteps.Length outputDir

    // ---------------------------------------------------------------------------
    // Summary generation (replaces run_viz.py)
    // ---------------------------------------------------------------------------

    /// Algorithm kinds for summary assembly: which artifacts exist per step.
    type private SummaryKind =
        | TablePerStep
        | StackPerStep

    let private algorithmKind (algo: Algorithm) : SummaryKind =
        match algo with
        | CYK
        | Valiant -> TablePerStep
        | LL
        | LR -> StackPerStep

    let private algorithmLower (algo: Algorithm) : string = (algo.ToString()).ToLower()

    let private naturalSortKey (dirName: string) : int =
        let m = Regex.Match(dirName, "step_(\d+)")

        if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

    let private collectSteps (vizDir: string) : string[] =
        if not (Directory.Exists vizDir) then
            [||]
        else
            Directory.GetDirectories vizDir
            |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))
            |> Array.sortBy (fun d -> naturalSortKey (Path.GetFileName d))

    let private readIfExists (path: string) : string option =
        if File.Exists path then
            Some(File.ReadAllText(path).Trim())
        else
            None

    let private wrapMath (tex: string) : string =
        [ @"\begin{center}"; @"\["; tex; @"\]"; @"\end{center}" ] |> String.concat "\n"

    let private wrapCenter (tex: string) : string =
        [ @"\begin{center}"; tex; @"\end{center}" ] |> String.concat "\n"

    /// Include a PDF (path relative to the merged TeX build directory).
    let private includePdf (relPath: string) : string =
        sprintf @"\begin{center}\includegraphics[width=0.9\textwidth,keepaspectratio]{{%s}}\end{center}" relPath

    /// Render a section header.
    let private section (title: string) : string = sprintf "\\subsection*{%s}" title

    /// Compile every Dot file inside `vizDir` (per-step + optional root) into
    /// PDFs under `dotPdfDir`. Returns (ok, list of (dotName, pdfRelPath)).
    let private compileDotArtifacts (vizDir: string) (dotPdfDir: string) : bool * (string * string) list =
        if not (Directory.Exists dotPdfDir) then
            Directory.CreateDirectory dotPdfDir |> ignore

        let mutable ok = true
        let mutable produced = []

        // Root-level dot files (e.g. lr_automaton.dot)
        for dotFile in Directory.GetFiles(vizDir, "*.dot") do
            let name = Path.GetFileNameWithoutExtension(dotFile)
            let pdfPath = Path.Combine(dotPdfDir, sprintf "%s.pdf" name)
            let rel = sprintf "../dot_pdfs/%s.pdf" name

            if ExternalTools.compileDotFileToPdf dotFile pdfPath then
                produced <- (name, rel) :: produced
            else
                ok <- false

        // Per-step dot files
        for stepDir in collectSteps vizDir do
            let stepName = Path.GetFileName(stepDir)

            for dotFile in Directory.GetFiles(stepDir, "*.dot") do
                let dotName = Path.GetFileNameWithoutExtension(dotFile)
                let pdfName = sprintf "%s_%s.pdf" stepName dotName
                let pdfPath = Path.Combine(dotPdfDir, pdfName)
                let rel = sprintf "../dot_pdfs/%s" pdfName

                if ExternalTools.compileDotFileToPdf dotFile pdfPath then
                    produced <- (sprintf "%s/%s" stepName dotName, rel) :: produced
                else
                    ok <- false

        (ok, List.rev produced)

    /// Build the header section: original grammar, CNF (if present), input,
    /// LL/LR table (if present). LR automaton PDF is included via `lrAutomatonPdf`.
    let private headerSection (vizDir: string) (algo: Algorithm) (lrAutomatonPdf: string option) : string list =
        let mutable lines = []

        match readIfExists (Path.Combine(vizDir, "grammar_original.tex")) with
        | Some tex -> lines <- lines @ [ section "Original Grammar"; wrapCenter tex; "" ]
        | None -> ()

        match algo with
        | CYK
        | Valiant ->
            match readIfExists (Path.Combine(vizDir, "grammar_cnf.tex")) with
            | Some tex -> lines <- lines @ [ section "CNF Grammar (passed to algorithm)"; wrapCenter tex; "" ]
            | None -> ()

            match readIfExists (Path.Combine(vizDir, "input.tex")) with
            | Some tex -> lines <- lines @ [ section "Input String"; wrapMath tex; "" ]
            | None -> ()
        | LL ->
            match readIfExists (Path.Combine(vizDir, "ll_table.tex")) with
            | Some tex -> lines <- lines @ [ section "LL Parsing Table"; wrapCenter tex; "" ]
            | None -> ()
        | LR ->
            match readIfExists (Path.Combine(vizDir, "lr_table.tex")) with
            | Some tex -> lines <- lines @ [ section "LR Parsing Table"; wrapCenter tex; "" ]
            | None -> ()

            match lrAutomatonPdf with
            | Some rel -> lines <- lines @ [ section "LR Automaton"; includePdf rel; "" ]
            | None -> ()

        lines

    /// Build the per-step section for an algorithm with `TablePerStep` (CYK, Valiant).
    let private tableStepSection (stepDir: string) (stepNum: int) : string list =
        let mutable lines = [ section (sprintf "Step %d" stepNum) ]

        // table.tex first
        match readIfExists (Path.Combine(stepDir, "table.tex")) with
        | Some tex -> lines <- lines @ [ wrapMath tex; "" ]
        | None -> ()

        // bool_decomp_*.tex files (Valiant last step)
        let decompFiles =
            Directory.GetFiles(stepDir, "bool_decomp_*.tex")
            |> Array.sortBy (fun f -> Path.GetFileName(f))

        for f in decompFiles do
            lines <- lines @ [ wrapMath (File.ReadAllText(f).Trim()); "" ]

        lines

    /// Build the per-step section for an algorithm with `StackPerStep` (LL, LR).
    let private stackStepSection (stepDir: string) (stepNum: int) (stepName: string) : string list =
        let mutable lines = [ section (sprintf "Step %d" stepNum) ]

        // tree_and_stack.pdf (already compiled by compileDotArtifacts)
        let rel = sprintf "../dot_pdfs/%s_tree_and_stack.pdf" stepName
        lines <- lines @ [ includePdf rel; "" ]

        match readIfExists (Path.Combine(stepDir, "input.tex")) with
        | Some tex -> lines <- lines @ [ wrapMath tex; "" ]
        | None -> ()

        lines

    /// Build the merged TeX content for the given algorithm's `vizDir`.
    /// `dotArtifacts` is the list produced by `compileDotArtifacts`.
    let private buildContent (algo: Algorithm) (vizDir: string) (stepCount: int) : string list =
        let mutable lines =
            [ section ("Algorithm: " + algo.ToString())
              sprintf "\\textit{Total steps: %d}\\\\" stepCount
              "" ]

        // lr_automaton.pdf if it was compiled
        let lrAutomatonPdf =
            match algo with
            | LR ->
                let autoDot = Path.Combine(vizDir, "lr_automaton.dot")

                if File.Exists autoDot then
                    Some "../dot_pdfs/lr_automaton.pdf"
                else
                    None
            | _ -> None

        lines <- lines @ headerSection vizDir algo lrAutomatonPdf

        let kind = algorithmKind algo

        for stepDir in collectSteps vizDir do
            let stepName = Path.GetFileName(stepDir)
            let stepNum = naturalSortKey stepName

            let stepLines =
                match kind with
                | TablePerStep -> tableStepSection stepDir stepNum
                | StackPerStep -> stackStepSection stepDir stepNum stepName

            lines <- lines @ stepLines

        lines

    /// Build the summary PDF for one algorithm.
    /// `vizDir` is the directory where step artifacts live.
    /// `resultDir` is the parent of the per-algorithm summary directory.
    /// Returns true iff all Dot and TeX compilations succeed and the final PDF exists.
    let private buildSummary (templatePath: string) (algo: Algorithm) (vizDir: string) (resultDir: string) : bool =
        let algoLower = algorithmLower algo
        let algoDir = Path.Combine(resultDir, algoLower)

        if not (Directory.Exists algoDir) then
            Directory.CreateDirectory algoDir |> ignore

        let dotPdfDir = Path.Combine(algoDir, "dot_pdfs")

        // Compile all Dot artifacts first.
        let (dotOk, _) = compileDotArtifacts vizDir dotPdfDir

        if not dotOk then
            eprintfn "Summary: Dot compilation failed for %s" (algo.ToString())
            false
        else
            let steps = collectSteps vizDir
            let content = buildContent algo vizDir steps.Length |> String.concat "\n"
            let template = File.ReadAllText templatePath

            let fullTex =
                template.Replace("__ALGORITHM__", algo.ToString()).Replace("__CONTENT__", content)

            let mergedTexPath = Path.Combine(algoDir, sprintf "%s_merged.tex" algoLower)
            writeOutputFile mergedTexPath fullTex

            let buildDir = Path.Combine(algoDir, "merged_tex_build")

            if not (Directory.Exists buildDir) then
                Directory.CreateDirectory buildDir |> ignore

            let buildTexPath = Path.Combine(buildDir, sprintf "%s_merged.tex" algoLower)
            File.Copy(mergedTexPath, buildTexPath, true)

            printfn "Summary: compiling %s_merged.tex (two passes)" algoLower

            let texOk = ExternalTools.compileTexFileTwice buildTexPath buildDir

            if not texOk then
                eprintfn "Summary: TeX compilation failed for %s" (algo.ToString())
                false
            else
                let srcPdf = Path.Combine(buildDir, sprintf "%s_merged.pdf" algoLower)
                let dstPdf = Path.Combine(algoDir, sprintf "%s_visualization.pdf" algoLower)

                if File.Exists srcPdf then
                    File.Copy(srcPdf, dstPdf, true)
                    printfn "Summary: %s visualization PDF -> %s" (algo.ToString()) dstPdf
                    true
                else
                    eprintfn "Summary: PDF not produced for %s" (algo.ToString())
                    false

    /// Locate the summary TeX template.
    /// Looks for `data/tex_summary_template.tex` relative to the current directory
    /// and the application base directory.
    let private findSummaryTemplate () : string =
        let candidates =
            [ Path.Combine("data", "tex_summary_template.tex")
              Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")
              Path.Combine(
                  System.AppContext.BaseDirectory,
                  "..",
                  "..",
                  "..",
                  "..",
                  "..",
                  "data",
                  "tex_summary_template.tex"
              ) ]

        match candidates |> List.tryFind File.Exists with
        | Some p -> p
        | None -> failwithf "Could not locate tex_summary_template.tex. Tried: %A" candidates

    /// Testable entry point: parses arguments, runs the algorithm, optionally
    /// builds the summary PDF. Returns the process exit code (0 = success).
    /// Does not call `exit` so it can be invoked from tests.
    let runCli (argv: string[]) : int =
        let parser = ArgumentParser.Create<Arguments>(programName = "flpq")

        try
            let results = parser.ParseCommandLine argv
            let algorithm = results.GetResult Algorithm
            let grammar = results.GetResult Grammar
            let input = results.GetResult Input
            let output = results.GetResult(Output, defaultValue = "output")
            let k = results.GetResult(Lookahead, defaultValue = 1)
            let summary = results.Contains Summary

            match algorithm with
            | Algorithm.CYK -> runCyk grammar input output
            | Algorithm.Valiant -> runValiant grammar input output
            | Algorithm.LL -> runLL grammar input output k
            | Algorithm.LR -> runLR grammar input output

            if summary then
                let templatePath = findSummaryTemplate ()
                let resultDir = Path.Combine(output, "results")

                if not (buildSummary templatePath algorithm output resultDir) then
                    1
                else
                    0
            else
                0
        with ex ->
            eprintfn "%s" ex.Message
            eprintfn "%s" (parser.PrintUsage())
            1

    [<EntryPoint>]
    let main argv = runCli argv
