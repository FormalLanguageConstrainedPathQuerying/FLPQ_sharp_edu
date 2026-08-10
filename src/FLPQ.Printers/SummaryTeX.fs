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

    /// Wraps TeX content in a centered math environment with resizing.
    let wrapMathResized (tex: string) : string =
        [ @"\begin{center}"
          @"\resizebox{0.9\textwidth}{!}{%%"
          "$"
          tex
          "$"
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

    /// Wraps a raw TeX tabular in a centered, resizable box (no math mode, no inner center).
    let wrapTabularResized (tabular: string) : string =
        [ @"\begin{center}"
          @"\resizebox{0.3\textwidth}{!}{%%"
          tabular
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

    /// Generates the color legend for GLL summary visualization.
    let private gllColorLegend () : string =
        let colorBox (color: string) =
            sprintf @"\colorbox{%s}{\rule{0pt}{2ex}\rule{1.2em}{0pt}}" color

        let coloredEdge (color: string) =
            sprintf @"\textcolor{%s}{\rule{2em}{0.4pt}}" color

        let rows =
            [ colorBox "yellow!20", "Current descriptor in descriptors table"
              colorBox "yellow", "Modified path index cells"
              colorBox "blue!20", "Current GSS node"
              colorBox "green!30", "Current input position"
              colorBox "yellow!30", "Newly added GSS vertices"
              coloredEdge "red", "Newly added GSS edges"
              colorBox "green!20", "Genuinely new descriptors"
              colorBox "red!20", "Already-handled descriptors attempted again" ]

        let rowLines =
            rows
            |> List.map (fun (colorSample, desc) -> sprintf @"%s & %s \\" colorSample desc)
            |> String.concat "\n"

        sprintf @"\begin{center}\begin{tabular}{cl} %s \end{tabular}\end{center}" rowLines

    let private rnglrColorLegend () : string =
        let colorBox (color: string) =
            sprintf @"\colorbox{%s}{\rule{0pt}{2ex}\rule{1.2em}{0pt}}" color

        let coloredEdge (color: string) =
            sprintf @"\textcolor{%s}{\rule{2em}{0.4pt}}" color

        let rows =
            [ colorBox "yellow!20", "Current descriptor in descriptors table"
              colorBox "yellow", "Modified path index cells"
              colorBox "lightblue!20", "Current GSS node"
              colorBox "green!30", "Current input position"
              colorBox "yellow!30", "Newly added GSS vertices"
              coloredEdge "red", "Newly added GSS edges"
              colorBox "green!20", "Genuinely new descriptors"
              colorBox "red!20", "Already-handled descriptors attempted again" ]

        let rowLines =
            rows
            |> List.map (fun (colorSample, desc) -> sprintf @"%s & %s \\" colorSample desc)
            |> String.concat "\n"

        sprintf @"\begin{center}\begin{tabular}{cl} %s \end{tabular}\end{center}" rowLines

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
        (useTikz: bool)
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
                let colorLegend = [ section "Color Legend"; gllColorLegend (); "" ]

                let inputSection =
                    if useTikz then
                        match readIfExists (Path.Combine(vizDir, "input.tikz.tex")) with
                        | Some tikz -> [ section "Input String"; wrapTikzCenter tikz; "" ]
                        | None -> []
                    else
                        [ section "Input String"; includePdf "dot_pdfs/input.pdf"; "" ]

                let rsmSppfLines =
                    rsmSppfPdfs
                    |> List.collect (fun (title, rel) -> [ section title; includePdf rel; "" ])

                let pathIndexLines = maybe "path_index.tex" "Path Index" wrapMathResized

                colorLegend @ inputSection @ rsmSppfLines @ pathIndexLines

            | SummaryKind.RNGLR ->
                let colorLegend = [ section "Color Legend"; rnglrColorLegend (); "" ]

                let tableLines = maybe "rnglr_table.tex" "RNGLR Parsing Table" wrapTabularResized

                let rsmSppfLines =
                    rsmSppfPdfs
                    |> List.collect (fun (title, rel) -> [ section title; includePdf rel; "" ])

                let pathIndexLines = maybe "path_index.tex" "Path Index" wrapMathResized

                colorLegend @ tableLines @ rsmSppfLines @ pathIndexLines

        grammar @ algoLines

    /// Builds the content lines for a single table-based algorithm step.
    let tableStepSection (stepDir: string) (stepNum: int) (wrap: string -> string) : string list =
        let header = [ section (sprintf "Step %d" stepNum) ]

        let tableLines =
            match readIfExists (Path.Combine(stepDir, "table.tex")) with
            | Some tex -> [ wrap tex; "" ]
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

    /// Builds the content lines for a single GLL step using the side-by-side template layout.
    let gllStepSection
        (stepDir: string)
        (stepNum: int)
        (template: string)
        (tikzTemplate: string)
        (useTikz: bool)
        : string list =
        let title =
            if stepNum = 0 then
                "Initialization"
            else
                sprintf "Step %d" stepNum

        let header = section title

        let stepName = Path.GetFileName(stepDir)

        let descriptorsTable =
            match readIfExists (Path.Combine(stepDir, "descriptors_table.tex")) with
            | Some tex -> tex
            | None -> ""

        let newDescriptors =
            match readIfExists (Path.Combine(stepDir, "new_descriptors.tex")) with
            | Some tex -> tex
            | None -> ""

        let pathIndex =
            match readIfExists (Path.Combine(stepDir, "path_index.tex")) with
            | Some tex -> tex
            | None -> ""

        let gssPdf = sprintf "dot_pdfs/%s_gss.pdf" stepName
        let rsmPdf = sprintf "dot_pdfs/%s_rsm.pdf" stepName
        let inputPdf = sprintf "dot_pdfs/%s_input.pdf" stepName

        let filledTemplate =
            if useTikz then
                let gssTikz =
                    match readIfExists (Path.Combine(stepDir, "gss.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                let rsmTikz =
                    match readIfExists (Path.Combine(stepDir, "rsm.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                let inputTikz =
                    match readIfExists (Path.Combine(stepDir, "input.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                tikzTemplate
                    .Replace("__DESCRIPTORS_TABLE__", descriptorsTable)
                    .Replace("__STEP_GSS_TIKZ__", gssTikz)
                    .Replace("__STEP_RSM_TIKZ__", rsmTikz)
                    .Replace("__STEP_INPUT_TIKZ__", inputTikz)
                    .Replace("__PATH_INDEX__", pathIndex)
                    .Replace("__NEW_DESCRIPTORS__", newDescriptors)
            else
                template
                    .Replace("__DESCRIPTORS_TABLE__", descriptorsTable)
                    .Replace("__STEP_GSS_PDF__", gssPdf)
                    .Replace("__STEP_RSM_PDF__", rsmPdf)
                    .Replace("__STEP_INPUT_PDF__", inputPdf)
                    .Replace("__PATH_INDEX__", pathIndex)
                    .Replace("__NEW_DESCRIPTORS__", newDescriptors)

        [ header; filledTemplate; "" ]

    let rnglrStepSection
        (stepDir: string)
        (stepNum: int)
        (template: string)
        (tikzTemplate: string)
        (useTikz: bool)
        : string list =
        let header = section (sprintf "Step %d" stepNum)

        let stepName = Path.GetFileName(stepDir)

        let descriptorsTable =
            match readIfExists (Path.Combine(stepDir, "descriptors_table.tex")) with
            | Some tex -> tex
            | None -> ""

        let newDescriptors =
            match readIfExists (Path.Combine(stepDir, "new_descriptors.tex")) with
            | Some tex -> tex
            | None -> ""

        let pathIndex =
            match readIfExists (Path.Combine(stepDir, "path_index.tex")) with
            | Some tex -> tex
            | None -> ""

        let lrTable =
            match readIfExists (Path.Combine(stepDir, "lr_table.tex")) with
            | Some tex -> tex
            | None -> ""

        let gssPdf = sprintf "dot_pdfs/%s_gss.pdf" stepName
        let inputPdf = sprintf "dot_pdfs/%s_input.pdf" stepName

        let filledTemplate =
            if useTikz then
                let gssTikz =
                    match readIfExists (Path.Combine(stepDir, "gss.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                let inputTikz =
                    match readIfExists (Path.Combine(stepDir, "input.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                tikzTemplate
                    .Replace("__DESCRIPTORS_TABLE__", descriptorsTable)
                    .Replace("__STEP_GSS_TIKZ__", gssTikz)
                    .Replace("__LR_TABLE__", lrTable)
                    .Replace("__STEP_INPUT_TIKZ__", inputTikz)
                    .Replace("__PATH_INDEX__", pathIndex)
                    .Replace("__NEW_DESCRIPTORS__", newDescriptors)
            else
                template
                    .Replace("__DESCRIPTORS_TABLE__", descriptorsTable)
                    .Replace("__STEP_GSS_PDF__", gssPdf)
                    .Replace("__LR_TABLE__", lrTable)
                    .Replace("__STEP_INPUT_PDF__", inputPdf)
                    .Replace("__PATH_INDEX__", pathIndex)
                    .Replace("__NEW_DESCRIPTORS__", newDescriptors)

        [ header; filledTemplate; "" ]

    /// Builds the SPPF section using TikZ if available, falling back to DOT PDF.
    let sppfSection (vizDir: string) (useTikz: bool) : string list =
        let tikzPath = Path.Combine(vizDir, "sppf.tikz.tex")
        let dotPath = Path.Combine(vizDir, "sppf.dot")

        if useTikz then
            match readIfExists tikzPath with
            | Some tikz -> [ section "SPPF (Shared Packed Parse Forest)"; wrapTikzCenter tikz; "" ]
            | None ->
                match readIfExists dotPath with
                | Some _ ->
                    [ section "SPPF (Shared Packed Parse Forest)"
                      includePdf "dot_pdfs/sppf.pdf"
                      "" ]
                | None -> []
        else
            match readIfExists dotPath with
            | Some _ ->
                [ section "SPPF (Shared Packed Parse Forest)"
                  includePdf "dot_pdfs/sppf.pdf"
                  "" ]
            | None -> []

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
        (gllStepTemplate: string)
        (rnglrStepTemplate: string)
        (gllStepTikzTemplate: string)
        (rnglrStepTikzTemplate: string)
        (useTikz: bool)
        : string list =
        let prefix =
            [ section ("Algorithm: " + algo)
              sprintf "\\textit{Total steps: %d}\\\\" stepCount
              "" ]

        let headerLines =
            headerSection vizDir algoKind lrAutomatonPdf lrAutomatonTikz rsmSppfPdfs useTikz

        let isTableBased = algoKind = SummaryKind.TablePerStep
        let isGll = algoKind = SummaryKind.GLL
        let isRnglr = algoKind = SummaryKind.RNGLR

        let stepLines =
            collectSteps vizDir
            |> Array.collect (fun stepDir ->
                let stepName = Path.GetFileName(stepDir)

                let stepNum =
                    let m = Text.RegularExpressions.Regex.Match(stepName, "step_(\d+)")
                    if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

                if isTableBased then
                    let wrap =
                        match readIfExists (Path.Combine(stepDir, "table.tex")) with
                        | Some tex when tex.Contains(@"\begin{adjustbox}") -> wrapCenter
                        | _ -> wrapMath

                    tableStepSection stepDir stepNum wrap |> List.toArray
                elif isGll then
                    gllStepSection stepDir stepNum gllStepTemplate gllStepTikzTemplate useTikz
                    |> List.toArray
                elif isRnglr then
                    rnglrStepSection stepDir stepNum rnglrStepTemplate rnglrStepTikzTemplate useTikz
                    |> List.toArray
                else
                    stackStepSection stepDir stepNum stepName |> List.toArray)
            |> Array.toList

        prefix @ headerLines @ stepLines @ (sppfSection vizDir useTikz)
