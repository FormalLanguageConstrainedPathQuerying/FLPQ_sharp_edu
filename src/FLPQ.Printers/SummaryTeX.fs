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
        | ArroyueloRPQ
        | BelyaninRPQ

        override this.ToString() =
            match this with
            | TablePerStep -> "table"
            | LL -> "ll"
            | LR -> "lr"
            | GLL -> "gll"
            | RNGLR -> "rnglr"
            | ArroyueloRPQ -> "arroyuelorpq"
            | BelyaninRPQ -> "belyaninrpq"

    /// Per-algorithm step templates (DOT and TikZ variants). Fields are empty strings for
    /// algorithms that do not use a step template.
    type StepTemplates =
        { Gll: string
          GllTikz: string
          Rnglr: string
          RnglrTikz: string
          Arroyuelo: string
          ArroyueloTikz: string
          Belyanin: string
          BelyaninTikz: string }

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

    /// Wraps a TikZ picture in a centered adjustbox that shrinks the figure to at most
    /// \textwidth without upscaling smaller figures (font metrics are preserved). When
    /// `limitHeight` is true the figure is additionally limited to at most \textheight.
    let wrapTikzAdjustbox (limitHeight: bool) (tikz: string) : string =
        let heightOpt = if limitHeight then @", max totalheight=\textheight" else ""

        [ @"\begin{center}"
          sprintf @"\begin{adjustbox}{max width=\textwidth%s}" heightOpt
          tikz
          @"\end{adjustbox}"
          @"\end{center}" ]
        |> String.concat "\n"

    /// Wraps a TikZ figure in a centered adjustbox that shrinks it to at most \linewidth
    /// (the current column width) without upscaling smaller figures. Used for RPQ step
    /// figures, which sit inside minipage columns where \textwidth is still the full page
    /// width and would let an over-wide figure overflow its column.
    let wrapTikzAdjustboxColumn (tikz: string) : string =
        [ @"\begin{center}"
          @"\begin{adjustbox}{max width=\linewidth}"
          tikz
          @"\end{adjustbox}"
          @"\end{center}" ]
        |> String.concat "\n"

    /// Wraps a filled RPQ step template in an adjustbox limited to at most \textwidth and
    /// 0.9\textheight (shrink-only). The 10% height headroom accommodates the step's
    /// \subsection* heading, which must stay outside the box: sectioning commands cannot
    /// run inside an adjustbox. Short steps are never scaled.
    let wrapStepAdjustbox (tex: string) : string =
        [ @"\begin{adjustbox}{max width=\textwidth, max totalheight=0.9\textheight}"
          tex
          @"\end{adjustbox}" ]
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

    /// A filled color swatch for a legend row.
    let private colorBox (color: string) : string =
        sprintf @"\colorbox{%s}{\rule{0pt}{2ex}\rule{1.2em}{0pt}}" color

    /// A colored edge sample for a legend row.
    let private coloredEdge (color: string) : string =
        sprintf @"\textcolor{%s}{\rule{2em}{0.4pt}}" color

    /// Renders legend rows as a centered two-column tabular.
    let private legendTable (rows: (string * string) list) : string =
        let rowLines =
            rows
            |> List.map (fun (colorSample, desc) -> sprintf @"%s & %s \\" colorSample desc)
            |> String.concat "\n"

        sprintf @"\begin{center}\begin{tabular}{cl} %s \end{tabular}\end{center}" rowLines

    /// Generates the color legend for GLL summary visualization.
    let private gllColorLegend () : string =
        let rows =
            [ colorBox "yellow!20", "Current descriptor in descriptors table"
              colorBox "yellow", "Modified path index cells"
              colorBox "blue!20", "Current GSS node"
              colorBox "green!30", "Current input position"
              colorBox "yellow!30", "Newly added GSS vertices"
              coloredEdge "red", "Newly added GSS edges"
              colorBox "orange!30", "Stored pops handling triggered at GSS vertex"
              colorBox "green!20", "Genuinely new descriptors"
              colorBox "red!20", "Already-handled descriptors attempted again" ]

        legendTable rows

    let private rnglrColorLegend () : string =
        let rows =
            [ colorBox "yellow", "Modified path index cells"
              colorBox "green!30", "Current input position"
              colorBox "yellow!30", "Newly added GSS vertices"
              coloredEdge "red", "Newly added GSS edges"
              colorBox "orange!30", "Passing reductions handling triggered at GSS vertex" ]

        legendTable rows

    let private arroyueloColorLegend () : string =
        let rows =
            [ colorBox "lightblue!20", "Current regexp tree node"
              colorBox "yellow!20", "Vertices of the step's result paths"
              coloredEdge "red", "Edges of the step's result paths"
              @"$\cdot$", "Empty matrix cell" ]

        legendTable rows

    let private belyaninColorLegend () : string =
        let rows =
            [ colorBox "lightblue!20", "Frontier automaton states (non-empty M[q,\\*])"
              colorBox "yellow!20", "Vertices of the current frontier paths"
              coloredEdge "red", "Edges of the current frontier paths"
              @"$\cdot$", "Empty matrix cell" ]

        legendTable rows

    /// Shared RPQ header section (Arroyuelo and Belyanin): color legend, query regexp,
    /// query DFA, and input graph. Mode-aware: TikZ embeds the .tikz.tex source wrapped in a
    /// width-only adjustbox; DOT includes the dot-compiled PDF (present when the dot source
    /// exists).
    let private rpqHeaderSection (vizDir: string) (legend: string) (useTikz: bool) : string list =
        let maybe (file: string) (label: string) (wrap: string -> string) =
            match readIfExists (Path.Combine(vizDir, file)) with
            | Some tex -> [ section label; wrap tex; "" ]
            | None -> []

        // DOT-mode figure head: present when the dot source exists, referencing the PDF
        // compiled by the CLI.
        let dotFigure (file: string) (label: string) (pdfName: string) : string list =
            match readIfExists (Path.Combine(vizDir, file)) with
            | Some _ -> [ section label; includePdf pdfName; "" ]
            | None -> []

        let colorLegend = [ section "Color Legend"; legend; "" ]

        let regexpLines = maybe "regexp.tex" "Query Regular Expression" wrapMath

        let dfaLines =
            if useTikz then
                maybe "dfa.tikz.tex" "Query DFA" (fun t -> wrapTikzAdjustbox false t)
            else
                dotFigure "dfa.dot" "Query DFA" "dot_pdfs/dfa.pdf"

        let graphLines =
            if useTikz then
                maybe "graph.tikz.tex" "Input Graph" (fun t -> wrapTikzAdjustbox false t)
            else
                dotFigure "graph.dot" "Input Graph" "dot_pdfs/graph.pdf"

        colorLegend @ regexpLines @ dfaLines @ graphLines

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
        (rsmPdfs: (string * string) list)
        (useTikz: bool)
        : string list =
        let maybe (file: string) (label: string) (wrap: string -> string) =
            match readIfExists (Path.Combine(vizDir, file)) with
            | Some tex -> [ section label; wrap tex; "" ]
            | None -> []

        let grammar = maybe "grammar_original.tex" "Original Grammar" wrapCenter

        // Extended RSM figure at the head in TikZ mode (blocks stacked top-to-bottom).
        // Skipped gracefully when ext_rsm.tikz.tex is absent so older viz dirs still build.
        let extRsmTikzSection =
            if useTikz then
                maybe "ext_rsm.tikz.tex" "Extended RSM" (fun t -> wrapTikzAdjustbox false t)
            else
                []

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

                let rsmFigureLines =
                    rsmPdfs
                    |> List.collect (fun (title, rel) -> [ section title; includePdf rel; "" ])

                let pathIndexLines = maybe "path_index.tex" "Path Index" wrapMathResized

                colorLegend @ inputSection @ extRsmTikzSection @ rsmFigureLines @ pathIndexLines

            | SummaryKind.ArroyueloRPQ -> rpqHeaderSection vizDir (arroyueloColorLegend ()) useTikz
            | SummaryKind.BelyaninRPQ -> rpqHeaderSection vizDir (belyaninColorLegend ()) useTikz

            | SummaryKind.RNGLR ->
                let colorLegend = [ section "Color Legend"; rnglrColorLegend (); "" ]

                // Mode-aware extended RSM head: TikZ embeds ext_rsm.tikz.tex, DOT includes
                // the compiled PDF. The plain RSM figure is never part of the RNGLR summary.
                let extRsmLines =
                    if useTikz then
                        extRsmTikzSection
                    else
                        rsmPdfs
                        |> List.collect (fun (title, rel) -> [ section title; includePdf rel; "" ])

                // LR automaton head following the SPPF pattern: TikZ wrapped in an adjustbox
                // limited to \textwidth and \textheight, DOT includes the compiled PDF.
                let lrAutomatonLines =
                    match lrAutomatonTikz with
                    | Some tikz when useTikz -> [ section "LR Automaton"; wrapTikzAdjustbox true tikz; "" ]
                    | _ ->
                        match lrAutomatonPdf with
                        | Some rel -> [ section "LR Automaton"; includePdf rel; "" ]
                        | None -> []

                let tableLines = maybe "rnglr_table.tex" "RNGLR Parsing Table" wrapTabularResized

                let pathIndexLines = maybe "path_index.tex" "Path Index" wrapMathResized

                colorLegend @ extRsmLines @ lrAutomatonLines @ tableLines @ pathIndexLines

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
    /// Includes the step header, the stack-tree picture (inline TikZ in TikZ mode,
    /// dot-compiled PDF in DOT mode), and the input state.
    let stackStepSection (stepDir: string) (stepNum: int) (stepName: string) (useTikz: bool) : string list =
        let header = [ section (sprintf "Step %d" stepNum) ]

        let pictureLines =
            if useTikz then
                match readIfExists (Path.Combine(stepDir, "tree_and_stack.tikz.tex")) with
                | Some tikz -> [ wrapTikzAdjustbox false tikz; "" ]
                | None -> []
            else
                [ includePdf (sprintf "dot_pdfs/%s_tree_and_stack.pdf" stepName); "" ]

        let inputLines =
            match readIfExists (Path.Combine(stepDir, "input.tex")) with
            | Some tex -> [ wrapMath tex; "" ]
            | None -> []

        header @ pictureLines @ inputLines

    /// Builds the content lines for a single GLL step using the side-by-side template layout.
    /// In TikZ mode the step's GSS and RSM figures are wrapped in adjustbox (shrink-only,
    /// at most \textwidth) via `wrapTikzAdjustbox`; DOT mode includes the dot-compiled PDFs.
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
                    .Replace("__STEP_GSS_TIKZ__", wrapTikzAdjustbox false gssTikz)
                    .Replace("__STEP_RSM_TIKZ__", wrapTikzAdjustbox false rsmTikz)
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

    /// Builds the content lines for a single RNGLR step using the two-column template layout
    /// (left: GSS figure + LR table; right: input figure + path index).
    /// In TikZ mode the step's GSS figure is wrapped in adjustbox (shrink-only, at most
    /// \textwidth) via `wrapTikzAdjustbox`; DOT mode includes the dot-compiled PDF.
    let rnglrStepSection
        (stepDir: string)
        (stepNum: int)
        (template: string)
        (tikzTemplate: string)
        (useTikz: bool)
        : string list =
        let header = section (sprintf "Step %d" stepNum)

        let stepName = Path.GetFileName(stepDir)

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
                    .Replace("__STEP_GSS_TIKZ__", wrapTikzAdjustbox false gssTikz)
                    .Replace("__LR_TABLE__", lrTable)
                    .Replace("__STEP_INPUT_TIKZ__", inputTikz)
                    .Replace("__PATH_INDEX__", pathIndex)
            else
                template
                    .Replace("__STEP_GSS_PDF__", gssPdf)
                    .Replace("__LR_TABLE__", lrTable)
                    .Replace("__STEP_INPUT_PDF__", inputPdf)
                    .Replace("__PATH_INDEX__", pathIndex)

        [ header; filledTemplate; "" ]

    /// Builds the content lines for a single Arroyuelo RPQ step using the two-column template
    /// layout (left: regexp tree figure + matrix equation; right: graph with the step's result
    /// path vertices/edges highlighted). In TikZ mode the step's tree and graph figures are
    /// wrapped in adjustbox (shrink-only, at most \linewidth) via `wrapTikzAdjustboxColumn`;
    /// DOT mode includes the dot-compiled PDFs. The filled template is wrapped whole in
    /// `wrapStepAdjustbox` (at most \textwidth and 0.9\textheight) so the step fits one page;
    /// the step heading stays outside the box.
    let arroyueloStepSection
        (stepDir: string)
        (stepNum: int)
        (template: string)
        (tikzTemplate: string)
        (useTikz: bool)
        : string list =
        let header = section (sprintf "Step %d" stepNum)

        let stepName = Path.GetFileName(stepDir)

        let matrices =
            match readIfExists (Path.Combine(stepDir, "matrices.tex")) with
            | Some tex -> tex
            | None -> ""

        let treePdf = sprintf "dot_pdfs/%s_tree.pdf" stepName
        let graphPdf = sprintf "dot_pdfs/%s_graph.pdf" stepName

        let filledTemplate =
            if useTikz then
                let treeTikz =
                    match readIfExists (Path.Combine(stepDir, "tree.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                let graphTikz =
                    match readIfExists (Path.Combine(stepDir, "graph.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                tikzTemplate
                    .Replace("__STEP_TREE_TIKZ__", wrapTikzAdjustboxColumn treeTikz)
                    .Replace("__MATRICES__", matrices)
                    .Replace("__STEP_GRAPH_TIKZ__", wrapTikzAdjustboxColumn graphTikz)
            else
                template
                    .Replace("__STEP_TREE_PDF__", treePdf)
                    .Replace("__MATRICES__", matrices)
                    .Replace("__STEP_GRAPH_PDF__", graphPdf)

        [ header; wrapStepAdjustbox filledTemplate; "" ]

    /// Builds the content lines for a single Belyanin RPQ step using the two-column template
    /// layout (left: query DFA figure with frontier states highlighted + matrix stack; right:
    /// graph with the current frontier path vertices/edges highlighted). In TikZ mode the
    /// step's automaton and graph figures are wrapped in adjustbox (shrink-only, at most
    /// \linewidth) via `wrapTikzAdjustboxColumn`; DOT mode includes the dot-compiled PDFs.
    /// The filled template is wrapped whole in `wrapStepAdjustbox` (at most \textwidth and
    /// 0.9\textheight) so the step fits one page; the step heading stays outside the box.
    let belyaninStepSection
        (stepDir: string)
        (stepNum: int)
        (template: string)
        (tikzTemplate: string)
        (useTikz: bool)
        : string list =
        let header = section (sprintf "Step %d" stepNum)

        let stepName = Path.GetFileName(stepDir)

        let matrices =
            match readIfExists (Path.Combine(stepDir, "matrices.tex")) with
            | Some tex -> tex
            | None -> ""

        let automatonPdf = sprintf "dot_pdfs/%s_automaton.pdf" stepName
        let graphPdf = sprintf "dot_pdfs/%s_graph.pdf" stepName

        let filledTemplate =
            if useTikz then
                let automatonTikz =
                    match readIfExists (Path.Combine(stepDir, "automaton.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                let graphTikz =
                    match readIfExists (Path.Combine(stepDir, "graph.tikz.tex")) with
                    | Some tikz -> tikz
                    | None -> ""

                tikzTemplate
                    .Replace("__STEP_AUTOMATON_TIKZ__", wrapTikzAdjustboxColumn automatonTikz)
                    .Replace("__MATRICES__", matrices)
                    .Replace("__STEP_GRAPH_TIKZ__", wrapTikzAdjustboxColumn graphTikz)
            else
                template
                    .Replace("__STEP_AUTOMATON_PDF__", automatonPdf)
                    .Replace("__MATRICES__", matrices)
                    .Replace("__STEP_GRAPH_PDF__", graphPdf)

        [ header; wrapStepAdjustbox filledTemplate; "" ]

    /// Builds the SPPF section using TikZ if available, falling back to DOT PDF.
    /// In TikZ mode the figure is wrapped in an adjustbox limited to at most \textwidth
    /// and \textheight (shrink-only); DOT mode includes the dot-compiled PDF.
    let sppfSection (vizDir: string) (useTikz: bool) : string list =
        let tikzPath = Path.Combine(vizDir, "sppf.tikz.tex")
        let dotPath = Path.Combine(vizDir, "sppf.dot")

        if useTikz then
            match readIfExists tikzPath with
            | Some tikz -> [ section "SPPF (Shared Packed Parse Forest)"; wrapTikzAdjustbox true tikz; "" ]
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
        (rsmPdfs: (string * string) list)
        (templates: StepTemplates)
        (useTikz: bool)
        : string list =
        let prefix =
            [ section ("Algorithm: " + algo)
              sprintf "\\textit{Total steps: %d}\\\\" stepCount
              "" ]

        let headerLines =
            headerSection vizDir algoKind lrAutomatonPdf lrAutomatonTikz rsmPdfs useTikz

        let isTableBased = algoKind = SummaryKind.TablePerStep
        let isGll = algoKind = SummaryKind.GLL
        let isRnglr = algoKind = SummaryKind.RNGLR
        let isArroyuelo = algoKind = SummaryKind.ArroyueloRPQ
        let isBelyanin = algoKind = SummaryKind.BelyaninRPQ

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
                    gllStepSection stepDir stepNum templates.Gll templates.GllTikz useTikz
                    |> List.toArray
                elif isRnglr then
                    rnglrStepSection stepDir stepNum templates.Rnglr templates.RnglrTikz useTikz
                    |> List.toArray
                elif isArroyuelo then
                    arroyueloStepSection stepDir stepNum templates.Arroyuelo templates.ArroyueloTikz useTikz
                    |> List.toArray
                elif isBelyanin then
                    belyaninStepSection stepDir stepNum templates.Belyanin templates.BelyaninTikz useTikz
                    |> List.toArray
                else
                    stackStepSection stepDir stepNum stepName useTikz |> List.toArray)
            |> Array.toList

        prefix @ headerLines @ stepLines @ (sppfSection vizDir useTikz)
