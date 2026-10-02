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
    /// algorithms that do not use a step template. Belyanin uses four templates: the
    /// start-of-step line (Belyanin/BelyaninTikz), the end-of-step line (BelyaninRowEnd/
    /// BelyaninRowEndTikz), and one per-label propagation row (BelyaninLabelRow/
    /// BelyaninLabelRowTikz).
    type StepTemplates =
        { Gll: string
          GllTikz: string
          Rnglr: string
          RnglrTikz: string
          Arroyuelo: string
          ArroyueloTikz: string
          Belyanin: string
          BelyaninTikz: string
          BelyaninRowEnd: string
          BelyaninRowEndTikz: string
          BelyaninLabelRow: string
          BelyaninLabelRowTikz: string }

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

    /// Wraps a TikZ figure in a centered adjustbox with the given options, shrinking it to at
    /// most the current column width without upscaling smaller figures.
    let private wrapTikzAdjustboxColumnWith (options: string) (tikz: string) : string =
        [ @"\begin{center}"
          sprintf @"\begin{adjustbox}{%s}" options
          tikz
          @"\end{adjustbox}"
          @"\end{center}" ]
        |> String.concat "\n"

    /// Wraps a TikZ figure in a centered adjustbox that shrinks it to at most \linewidth
    /// (the current column width) without upscaling smaller figures. Used for RPQ step
    /// figures, which sit inside minipage columns where \textwidth is still the full page
    /// width and would let an over-wide figure overflow its column.
    let wrapTikzAdjustboxColumn (tikz: string) : string =
        wrapTikzAdjustboxColumnWith @"max width=\linewidth" tikz

    /// Like `wrapTikzAdjustboxColumn` but top-aligned (`valign=T`). Used by the Belyanin and
    /// Arroyuelo RPQ step figures so each figure lines up with the matrix/formula beside it.
    let wrapTikzAdjustboxColumnTop (tikz: string) : string =
        wrapTikzAdjustboxColumnWith @"max width=\linewidth, valign=T" tikz

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

    /// Generates the color legend for RNGLR summary visualization.
    let private rnglrColorLegend () : string =
        let rows =
            [ colorBox "yellow", "Modified path index cells"
              colorBox "green!30", "Current input position"
              colorBox "yellow!30", "Newly added GSS vertices"
              coloredEdge "red", "Newly added GSS edges"
              colorBox "orange!30", "Passing reductions handling triggered at GSS vertex" ]

        legendTable rows

    /// Generates the color legend for Arroyuelo RPQ summary visualization. Covers both
    /// semantics: simplePath highlights result paths (light red edges, yellow vertices);
    /// reachability draws computed M(E) edges red bold.
    let private arroyueloColorLegend () : string =
        let rows =
            [ colorBox "lightblue!20", "Current regexp tree node"
              colorBox "yellow!20", "Vertices of the step's result paths (simplePath)"
              coloredEdge "red!40", "Result-path edges (simplePath)"
              coloredEdge "red", "Computed M(E) edges (reachability)"
              @"$\cdot$", "Empty matrix cell" ]

        legendTable rows

    /// Generates the color legend for Belyanin RPQ summary visualization.
    let private belyaninColorLegend () : string =
        let rows =
            [ colorBox "green!30", "Start vertex / initial automaton state"
              colorBox "lightblue!20", "Frontier automaton states and frontier (initial) vertices"
              colorBox "yellow!20", "Target vertices and automaton states (newly reached on the step)"
              coloredEdge "red", "Current transition edges (used on the step)"
              coloredEdge "red!40", "Frontier path edges"
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
    /// layout (left: regexp tree figure; right: the matrix equation line and, below it, the
    /// graph figure). In TikZ mode the step's tree and graph figures are wrapped in a
    /// top-aligned adjustbox (shrink-only, at most \linewidth) via `wrapTikzAdjustboxColumnTop`;
    /// DOT mode includes the dot-compiled PDFs. The filled template is NOT wrapped whole (unlike
    /// the legacy layout) so a tall step can span pages; each component keeps its own
    /// width-limited adjustbox.
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
                    .Replace("__STEP_TREE_TIKZ__", wrapTikzAdjustboxColumnTop treeTikz)
                    .Replace("__MATRICES__", matrices)
                    .Replace("__STEP_GRAPH_TIKZ__", wrapTikzAdjustboxColumnTop graphTikz)
            else
                template
                    .Replace("__STEP_TREE_PDF__", treePdf)
                    .Replace("__MATRICES__", matrices)
                    .Replace("__STEP_GRAPH_PDF__", graphPdf)

        [ header; filledTemplate; "" ]

    /// Applies placeholder replacements to a template in order.
    let private fill (template: string) (replacements: (string * string) list) : string =
        replacements
        |> List.fold (fun t (placeholder, value) -> t.Replace(placeholder, value)) template

    /// Builds the content lines for a single Belyanin RPQ step using the line-per-substep
    /// layout: the start-of-step state (frontier/visited matrices, automaton with frontier
    /// states highlighted, graph with green sources and lightblue frontier vertices), one row
    /// per active label with the explicit propagation products and the used transitions /
    /// followed edges highlighted red bold, and — when the step has end artifacts (non-init) —
    /// the end-of-step state. In TikZ mode the figures are wrapped in a top-aligned adjustbox
    /// (shrink-only, at most \linewidth) via `wrapTikzAdjustboxColumnTop`; DOT mode includes the
    /// dot-compiled PDFs. The filled template is NOT wrapped whole (unlike Arroyuelo), so a tall
    /// step can break across pages; each component keeps its own width-limited adjustbox and the
    /// step heading stays outside any box.
    let belyaninStepSection (stepDir: string) (stepNum: int) (templates: StepTemplates) (useTikz: bool) : string list =
        let header = section (sprintf "Step %d" stepNum)

        let stepName = Path.GetFileName(stepDir)

        let readArtifact (file: string) : string =
            match readIfExists (Path.Combine(stepDir, file)) with
            | Some tex -> tex
            | None -> ""

        // Per-label rows in numeric label-index order; the index is the position of the
        // label in the trace's Labels list (the visualizer and this builder must agree on
        // the index-based naming).
        let labelRows =
            Directory.GetFiles(stepDir, "label_*_select.tex")
            |> Array.map Path.GetFileName
            |> Array.filter (fun f -> Regex.IsMatch(f, @"^label_\d+_select\.tex$"))
            |> Array.map (fun f -> Int32.Parse(Regex.Match(f, @"^label_(\d+)_select\.tex$").Groups.[1].Value))
            |> Array.sort
            |> Array.map (fun i ->
                let selectFormula = readArtifact (sprintf "label_%d_select.tex" i)
                let extendFormula = readArtifact (sprintf "label_%d_extend.tex" i)

                if useTikz then
                    fill
                        templates.BelyaninLabelRowTikz
                        [ ("__SELECT_FORMULA__", selectFormula)
                          ("__LABEL_AUTOMATON_TIKZ__",
                           wrapTikzAdjustboxColumnTop (readArtifact (sprintf "label_%d_automaton.tikz.tex" i)))
                          ("__EXTEND_FORMULA__", extendFormula)
                          ("__LABEL_GRAPH_TIKZ__",
                           wrapTikzAdjustboxColumnTop (readArtifact (sprintf "label_%d_graph.tikz.tex" i))) ]
                else
                    fill
                        templates.BelyaninLabelRow
                        [ ("__SELECT_FORMULA__", selectFormula)
                          ("__LABEL_AUTOMATON_PDF__", sprintf "dot_pdfs/%s_label_%d_automaton.pdf" stepName i)
                          ("__EXTEND_FORMULA__", extendFormula)
                          ("__LABEL_GRAPH_PDF__", sprintf "dot_pdfs/%s_label_%d_graph.pdf" stepName i) ])
            |> Array.toList
            |> String.concat "\n"

        // The end-of-step line is present only when the step has end artifacts (non-init).
        let rowEnd =
            if File.Exists(Path.Combine(stepDir, "frontier_end.tex")) then
                let frontierEnd = readArtifact "frontier_end.tex"
                let visitedEnd = readArtifact "visited_end.tex"

                if useTikz then
                    fill
                        templates.BelyaninRowEndTikz
                        [ ("__FRONTIER_END__", frontierEnd)
                          ("__VISITED_END__", visitedEnd)
                          ("__AUTOMATON_END_TIKZ__", wrapTikzAdjustboxColumnTop (readArtifact "automaton_end.tikz.tex"))
                          ("__GRAPH_END_TIKZ__", wrapTikzAdjustboxColumnTop (readArtifact "graph_end.tikz.tex")) ]
                else
                    fill
                        templates.BelyaninRowEnd
                        [ ("__FRONTIER_END__", frontierEnd)
                          ("__VISITED_END__", visitedEnd)
                          ("__AUTOMATON_END_PDF__", sprintf "dot_pdfs/%s_automaton_end.pdf" stepName)
                          ("__GRAPH_END_PDF__", sprintf "dot_pdfs/%s_graph_end.pdf" stepName) ]
            else
                ""

        let filledTemplate =
            if useTikz then
                fill
                    templates.BelyaninTikz
                    [ ("__FRONTIER_START__", readArtifact "frontier_start.tex")
                      ("__VISITED_START__", readArtifact "visited_start.tex")
                      ("__AUTOMATON_START_TIKZ__", wrapTikzAdjustboxColumnTop (readArtifact "automaton_start.tikz.tex"))
                      ("__GRAPH_START_TIKZ__", wrapTikzAdjustboxColumnTop (readArtifact "graph_start.tikz.tex"))
                      ("__LABEL_ROWS__", labelRows)
                      ("__ROW_END__", rowEnd) ]
            else
                fill
                    templates.Belyanin
                    [ ("__FRONTIER_START__", readArtifact "frontier_start.tex")
                      ("__VISITED_START__", readArtifact "visited_start.tex")
                      ("__AUTOMATON_START_PDF__", sprintf "dot_pdfs/%s_automaton_start.pdf" stepName)
                      ("__GRAPH_START_PDF__", sprintf "dot_pdfs/%s_graph_start.pdf" stepName)
                      ("__LABEL_ROWS__", labelRows)
                      ("__ROW_END__", rowEnd) ]

        [ header; filledTemplate; "" ]

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
                    belyaninStepSection stepDir stepNum templates useTikz |> List.toArray
                else
                    stackStepSection stepDir stepNum stepName useTikz |> List.toArray)
            |> Array.toList

        prefix @ headerLines @ stepLines @ (sppfSection vizDir useTikz)
