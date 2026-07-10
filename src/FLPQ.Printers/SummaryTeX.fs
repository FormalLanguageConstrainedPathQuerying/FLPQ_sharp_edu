namespace FLPQ.Printers

open System
open System.IO
open System.Text.RegularExpressions

module SummaryTeX =

    /// Classifies the visualization type for summary generation.
    type SummaryKind =
        | TablePerStep
        | LL
        | LR
        | GLL
        | RNGLR

        override this.ToString() =
            match this with
            | TablePerStep -> "table"
            | LL -> "ll"
            | LR -> "lr"
            | GLL -> "gll"
            | RNGLR -> "rnglr"

    /// Wraps TeX content in a centered display math environment.
    let wrapMath (tex: string) : string =
        [ @"\begin{center}"; @"\["; tex; @"\]"; @"\end{center}" ] |> String.concat "\n"

    /// Wraps TeX content in a centered display math environment with resizing.
    let wrapMathResized (tex: string) : string =
        [ @"\begin{center}"
          @"\resizebox{0.9\textwidth}{!}{%%"
          @"\["
          tex
          @"\]"
          "}"
          @"\end{center}" ]
        |> String.concat "\n"

    /// Wraps TeX content in a centered environment.
    let wrapCenter (tex: string) : string =
        [ @"\begin{center}"; tex; @"\end{center}" ] |> String.concat "\n"

    /// Wraps a TikZ picture in a centered, resizable box.
    let wrapTikzCenter (tikz: string) : string =
        [ @"\begin{center}"
          @"\resizebox{0.98\textwidth}{!}{%%"
          tikz
          "}"
          @"\end{center}" ]
        |> String.concat "\n"

    /// Generates LaTeX code to include a PDF file as a centered figure.
    let includePdf (relPath: string) : string =
        sprintf @"\begin{center}\includegraphics[width=0.9\textwidth,keepaspectratio]{{%s}}\end{center}" relPath

    /// Generates a LaTeX subsection header with the given title.
    let section (title: string) : string = sprintf "\\subsection*{%s}" title

    /// Reads a file and returns its trimmed content, or None if the file does not exist.
    let readIfExists (path: string) : string option =
        if File.Exists path then
            Some(File.ReadAllText(path).Trim())
        else
            None

    /// Enumerates step directories in the given visualization directory,
    /// sorted by step number. Returns an empty array if the directory does not exist.
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

    /// Builds the header section of the summary, including grammar, input, and parsing table.
    let headerSection
        (vizDir: string)
        (algoKind: SummaryKind)
        (lrAutomatonPdf: string option)
        (lrAutomatonTikz: string option)
        (rsmSppfPdfs: (string * string) list)
        : string list =
        let maybe (file: string) (label: string) (wrap: string -> string) =
            match readIfExists (Path.Combine(vizDir, file)) with
            | Some tex -> [ section label; wrap tex; "" ]
            | None -> []

        let grammar = maybe "grammar_original.tex" "Original Grammar" wrapCenter

        let algoLines =
            match algoKind with
            | SummaryKind.TablePerStep ->
                maybe "grammar_cnf.tex" "CNF Grammar (passed to algorithm)" wrapCenter
                @ maybe "input.tex" "Input String" wrapMath

            | SummaryKind.LL -> maybe "ll_table.tex" "LL Parsing Table" wrapCenter

            | SummaryKind.LR ->
                maybe "lr_table.tex" "LR Parsing Table" wrapCenter
                @ (match lrAutomatonPdf with
                   | Some rel -> [ section "LR Automaton"; includePdf rel; "" ]
                   | None -> [])
                @ (match lrAutomatonTikz with
                   | Some tikz -> [ section "LR Automaton"; wrapTikzCenter tikz; "" ]
                   | None -> [])

            | SummaryKind.GLL ->
                maybe "input.tex" "Input String" wrapMath
                @ (rsmSppfPdfs
                   |> List.collect (fun (title, rel) -> [ section title; includePdf rel; "" ]))
                @ maybe "path_index.tex" "Path Index" wrapMathResized

            | SummaryKind.RNGLR ->
                maybe "input.tex" "Input String" wrapMath
                @ maybe "rnglr_table.tex" "RNGLR Parsing Table" wrapCenter
                @ (rsmSppfPdfs
                   |> List.collect (fun (title, rel) -> [ section title; includePdf rel; "" ]))
                @ maybe "path_index.tex" "Path Index" wrapMathResized

        grammar @ algoLines

    /// Builds the content lines for a single table-based algorithm step.
    let tableStepSection (stepDir: string) (stepNum: int) : string list =
        let header = [ section (sprintf "Step %d" stepNum) ]

        let tableLines =
            match readIfExists (Path.Combine(stepDir, "table.tex")) with
            | Some tex -> [ wrapMath tex; "" ]
            | None -> []

        header @ tableLines

    /// Builds the content lines for a single stack-based algorithm step (LL or LR).
    /// Includes the step header, stack-tree PDF, and input state.
    let stackStepSection (stepDir: string) (stepNum: int) (stepName: string) : string list =
        let header = [ section (sprintf "Step %d" stepNum) ]
        let pdfLine = [ includePdf (sprintf "dot_pdfs/%s_tree_and_stack.pdf" stepName); "" ]

        let inputLines =
            match readIfExists (Path.Combine(stepDir, "input.tex")) with
            | Some tex -> [ wrapMath tex; "" ]
            | None -> []

        header @ pdfLine @ inputLines

    /// Builds the complete summary content as a list of LaTeX lines.
    /// Combines the header section with all step sections into a single document.
    let buildContent
        (algo: string)
        (algoKind: SummaryKind)
        (vizDir: string)
        (stepCount: int)
        (lrAutomatonPdf: string option)
        (lrAutomatonTikz: string option)
        (rsmSppfPdfs: (string * string) list)
        : string list =
        let prefix =
            [ section ("Algorithm: " + algo)
              sprintf "\\textit{Total steps: %d}\\\\" stepCount
              "" ]

        let headerLines =
            headerSection vizDir algoKind lrAutomatonPdf lrAutomatonTikz rsmSppfPdfs

        let isTableBased = algoKind = SummaryKind.TablePerStep

        let stepLines =
            collectSteps vizDir
            |> Array.collect (fun stepDir ->
                let stepName = Path.GetFileName(stepDir)

                let stepNum =
                    let m = Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")
                    if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

                if isTableBased then
                    tableStepSection stepDir stepNum |> List.toArray
                else
                    stackStepSection stepDir stepNum stepName |> List.toArray)
            |> Array.toList

        prefix @ headerLines @ stepLines
