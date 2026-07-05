namespace FLPQ.Printers

open System
open System.IO
open System.Text.RegularExpressions

module SummaryTeX =

    type SummaryKind =
        | TablePerStep
        | LL
        | LR

        member this.toString =
            match this with
            | TablePerStep -> "table"
            | LL -> "ll"
            | LR -> "lr"

    let wrapMath (tex: string) : string =
        [ @"\begin{center}"; @"\["; tex; @"\]"; @"\end{center}" ] |> String.concat "\n"

    let wrapCenter (tex: string) : string =
        [ @"\begin{center}"; tex; @"\end{center}" ] |> String.concat "\n"

    let wrapTikzCenter (tikz: string) : string =
        [ @"\begin{center}"
          @"\resizebox{0.98\textwidth}{!}{%%"
          tikz
          "}"
          @"\end{center}" ]
        |> String.concat "\n"

    let includePdf (relPath: string) : string =
        sprintf @"\begin{center}\includegraphics[width=0.9\textwidth,keepaspectratio]{{%s}}\end{center}" relPath

    let section (title: string) : string = sprintf "\\subsection*{%s}" title

    let readIfExists (path: string) : string option =
        if File.Exists path then
            Some(File.ReadAllText(path).Trim())
        else
            None

    let collectSteps (vizDir: string) : string[] =
        if not (Directory.Exists vizDir) then
            [||]
        else
            Directory.GetDirectories vizDir
            |> Array.filter (fun d ->
                let name = Path.GetFileName(d)
                name.StartsWith("step_"))
            |> Array.sortBy (fun d ->
                let name = Path.GetFileName(d)
                let m = Text.RegularExpressions.Regex.Match(name, "step_(\d+)")
                if m.Success then Int32.Parse(m.Groups.[1].Value) else 0)

    let headerSection
        (vizDir: string)
        (algoKind: SummaryKind)
        (lrAutomatonPdf: string option)
        (lrAutomatonTikz: string option)
        : string list =
        let mutable lines = []

        match readIfExists (Path.Combine(vizDir, "grammar_original.tex")) with
        | Some tex -> lines <- lines @ [ section "Original Grammar"; wrapCenter tex; "" ]
        | None -> ()

        match algoKind with
        | SummaryKind.TablePerStep ->
            match readIfExists (Path.Combine(vizDir, "grammar_cnf.tex")) with
            | Some tex -> lines <- lines @ [ section "CNF Grammar (passed to algorithm)"; wrapCenter tex; "" ]
            | None -> ()

            match readIfExists (Path.Combine(vizDir, "input.tex")) with
            | Some tex -> lines <- lines @ [ section "Input String"; wrapMath tex; "" ]
            | None -> ()
        | SummaryKind.LL ->
            match readIfExists (Path.Combine(vizDir, "ll_table.tex")) with
            | Some tex -> lines <- lines @ [ section "LL Parsing Table"; wrapCenter tex; "" ]
            | None -> ()
        | SummaryKind.LR ->
            match readIfExists (Path.Combine(vizDir, "lr_table.tex")) with
            | Some tex -> lines <- lines @ [ section "LR Parsing Table"; wrapCenter tex; "" ]
            | None -> ()

            match lrAutomatonPdf with
            | Some rel -> lines <- lines @ [ section "LR Automaton"; includePdf rel; "" ]
            | None -> ()

            match lrAutomatonTikz with
            | Some tikz -> lines <- lines @ [ section "LR Automaton"; wrapTikzCenter tikz; "" ]
            | None -> ()

        lines

    let tableStepSection (stepDir: string) (stepNum: int) : string list =
        let mutable lines = [ section (sprintf "Step %d" stepNum) ]

        match readIfExists (Path.Combine(stepDir, "table.tex")) with
        | Some tex -> lines <- lines @ [ wrapMath tex; "" ]
        | None -> ()

        let decompFiles =
            Directory.GetFiles(stepDir, "bool_decomp_*.tex")
            |> Array.sortBy (fun f -> Path.GetFileName(f))

        for f in decompFiles do
            lines <- lines @ [ wrapMath (File.ReadAllText(f).Trim()); "" ]

        lines

    let stackStepSection (stepDir: string) (stepNum: int) (stepName: string) : string list =
        let mutable lines = [ section (sprintf "Step %d" stepNum) ]

        let rel = sprintf "dot_pdfs/%s_tree_and_stack.pdf" stepName
        lines <- lines @ [ includePdf rel; "" ]

        match readIfExists (Path.Combine(stepDir, "input.tex")) with
        | Some tex -> lines <- lines @ [ wrapMath tex; "" ]
        | None -> ()

        lines

    let buildContent
        (algo: string)
        (algoKind: SummaryKind)
        (vizDir: string)
        (stepCount: int)
        (lrAutomatonPdf: string option)
        (lrAutomatonTikz: string option)
        : string list =
        let mutable lines =
            [ section ("Algorithm: " + algo)
              sprintf "\\textit{Total steps: %d}\\\\" stepCount
              "" ]

        lines <- lines @ headerSection vizDir algoKind lrAutomatonPdf lrAutomatonTikz

        let stepDirs = collectSteps vizDir

        let isTableBased = algoKind = SummaryKind.TablePerStep

        for stepDir in stepDirs do
            let stepName = Path.GetFileName(stepDir)

            let stepNum =
                let m = Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")
                if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

            let stepLines =
                if isTableBased then
                    tableStepSection stepDir stepNum
                else
                    stackStepSection stepDir stepNum stepName

            lines <- lines @ stepLines

        lines
