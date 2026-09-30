namespace FLPQ.Cli

open System
open System.IO
open System.Text.RegularExpressions
open FLPQ.Languages
open FLPQ.Printers

module Helpers =

    let readFile path =
        if not (File.Exists path) then
            failwithf "File not found: %s" path

        File.ReadAllText(path).Trim()

    let writeOutputFile (path: string) (content: string) =
        let dir = Path.GetDirectoryName path

        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, content)

    let cleanOutputDir (dir: string) =
        if Directory.Exists dir then
            if Directory.GetFileSystemEntries(dir).Length > 0 then
                Directory.Delete(dir, true)
                Directory.CreateDirectory dir |> ignore
        else
            Directory.CreateDirectory dir |> ignore

    let writeStepsVisualization (outputDir: string) (useDot: bool) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            if useDot then
                writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].TreeAndStack
            else
                writeOutputFile (Path.Combine(stepDir, "tree_and_stack.tikz.tex")) steps.[idx].TreeAndStackTikz

            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].Input

    let writeGllStepsVisualization
        (outputDir: string)
        (useDot: bool)
        (steps: GllStepVisualizer.GllVisualizationStep list)
        =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "queue.tex")) steps.[idx].Queue
            writeOutputFile (Path.Combine(stepDir, "descriptors_table.tex")) steps.[idx].DescriptorsTable
            writeOutputFile (Path.Combine(stepDir, "new_descriptors.tex")) steps.[idx].NewDescriptors
            writeOutputFile (Path.Combine(stepDir, "path_index.tex")) steps.[idx].PathIndex

            if useDot then
                writeOutputFile (Path.Combine(stepDir, "gss.dot")) steps.[idx].GssDot
                writeOutputFile (Path.Combine(stepDir, "input.dot")) steps.[idx].Input
                writeOutputFile (Path.Combine(stepDir, "rsm.dot")) steps.[idx].RsmDot
            else
                writeOutputFile (Path.Combine(stepDir, "gss.tikz.tex")) steps.[idx].GssTikz
                writeOutputFile (Path.Combine(stepDir, "input.tikz.tex")) steps.[idx].InputTikz
                writeOutputFile (Path.Combine(stepDir, "rsm.tikz.tex")) steps.[idx].RsmTikz

    let writeRnglrStepsVisualization
        (outputDir: string)
        (useDot: bool)
        (steps: RnglrStepVisualizer.RnglrVisualizationStep list)
        =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "path_index.tex")) steps.[idx].PathIndex
            writeOutputFile (Path.Combine(stepDir, "lr_table.tex")) steps.[idx].LrTable

            if useDot then
                writeOutputFile (Path.Combine(stepDir, "gss.dot")) steps.[idx].GssDot
                writeOutputFile (Path.Combine(stepDir, "input.dot")) steps.[idx].Input
            else
                writeOutputFile (Path.Combine(stepDir, "gss.tikz.tex")) steps.[idx].GssTikz
                writeOutputFile (Path.Combine(stepDir, "input.tikz.tex")) steps.[idx].InputTikz

    let writeArroyueloStepsVisualization
        (outputDir: string)
        (useDot: bool)
        (steps: ArroyueloStepVisualizer.ArroyueloVisualizationStep list)
        =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "matrices.tex")) steps.[idx].Matrices

            if useDot then
                writeOutputFile (Path.Combine(stepDir, "tree.dot")) steps.[idx].TreeDot
                writeOutputFile (Path.Combine(stepDir, "graph.dot")) steps.[idx].GraphDot
            else
                writeOutputFile (Path.Combine(stepDir, "tree.tikz.tex")) steps.[idx].TreeTikz
                writeOutputFile (Path.Combine(stepDir, "graph.tikz.tex")) steps.[idx].GraphTikz

    /// Writes one directory per trace step with the line-per-substep layout: the start-of-step
    /// state (frontier/visited matrices + automaton/graph figures), one quad of files per active
    /// label (label_i_select/extend formulas + automaton/graph figures), and — for non-init
    /// steps — the end-of-step state (frontier/visited matrices + automaton/graph figures). The
    /// init step has no end artifacts and no label quads.
    let writeBelyaninStepsVisualization
        (outputDir: string)
        (useDot: bool)
        (steps: BelyaninStepVisualizer.BelyaninVisualizationStep list)
        =
        // One step-boundary group: the frontier/visited matrix files plus the automaton/graph
        // figures in the selected format.
        let writeBoundary
            (stepDir: string)
            (suffix: string)
            (boundary: BelyaninStepVisualizer.BelyaninBoundaryVisual)
            : unit =
            writeOutputFile (Path.Combine(stepDir, sprintf "frontier_%s.tex" suffix)) boundary.Frontier
            writeOutputFile (Path.Combine(stepDir, sprintf "visited_%s.tex" suffix)) boundary.Visited

            if useDot then
                writeOutputFile (Path.Combine(stepDir, sprintf "automaton_%s.dot" suffix)) boundary.AutomatonDot
                writeOutputFile (Path.Combine(stepDir, sprintf "graph_%s.dot" suffix)) boundary.GraphDot
            else
                writeOutputFile (Path.Combine(stepDir, sprintf "automaton_%s.tikz.tex" suffix)) boundary.AutomatonTikz
                writeOutputFile (Path.Combine(stepDir, sprintf "graph_%s.tikz.tex" suffix)) boundary.GraphTikz

        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)
            let step = steps.[idx]

            writeBoundary stepDir "start" step.Start

            for i in 0 .. step.Labels.Length - 1 do
                let label = step.Labels.[i]

                writeOutputFile (Path.Combine(stepDir, sprintf "label_%d_select.tex" i)) label.SelectFormula
                writeOutputFile (Path.Combine(stepDir, sprintf "label_%d_extend.tex" i)) label.ExtendFormula

                if useDot then
                    writeOutputFile (Path.Combine(stepDir, sprintf "label_%d_automaton.dot" i)) label.AutomatonDot
                    writeOutputFile (Path.Combine(stepDir, sprintf "label_%d_graph.dot" i)) label.GraphDot
                else
                    writeOutputFile (Path.Combine(stepDir, sprintf "label_%d_automaton.tikz.tex" i)) label.AutomatonTikz
                    writeOutputFile (Path.Combine(stepDir, sprintf "label_%d_graph.tikz.tex" i)) label.GraphTikz

            match step.End with
            | Some end_ -> writeBoundary stepDir "end" end_
            | None -> ()

    /// Shared RPQ root artifacts (Arroyuelo and Belyanin): the query regexp in TeX, the
    /// query DFA, and the input graph — DOT sources or inline TikZ depending on useDot.
    let writeRpqRootArtifacts
        (outputDir: string)
        (useDot: bool)
        (regexp: Regexp<string, string>)
        (dfa: DFA<string, int>)
        (graph: NFA<string, int>)
        : unit =
        writeOutputFile (Path.Combine(outputDir, "regexp.tex")) (RegexpTeX.toTeX id id regexp)

        // The input graph's sources render green (matching the automaton's initial state
        // fill) — shared with the Belyanin step figures.
        let graphDot, graphTikz =
            RpqGraphViz.renderGraph id graph Set.empty graph.StartStates Set.empty Set.empty Set.empty

        if useDot then
            writeOutputFile
                (Path.Combine(outputDir, "dfa.dot"))
                (AutomatonDot.dfaToDot string (fun idx _ -> sprintf "q_%d" idx) dfa)

            writeOutputFile (Path.Combine(outputDir, "graph.dot")) graphDot
        else
            writeOutputFile
                (Path.Combine(outputDir, "dfa.tikz.tex"))
                (AutomatonTikz.dfaToTikz string (fun idx _ -> sprintf "$q_%d$" idx) "circle" dfa)

            writeOutputFile (Path.Combine(outputDir, "graph.tikz.tex")) graphTikz

    let naturalSortKey (dirName: string) : int =
        let m = Regex.Match(dirName, "step_(\d+)")

        if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

    /// Locate a template file in the data directory (CWD-relative), the test output
    /// directory, or the repository's data directory. Fails with the tried paths when
    /// none exists.
    let findTemplateFile (fileName: string) : string =
        let candidates =
            [ Path.Combine("data", fileName)
              Path.Combine(System.AppContext.BaseDirectory, fileName)
              Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "..", "data", fileName) ]

        match candidates |> List.tryFind File.Exists with
        | Some p -> p
        | None -> failwithf "Could not locate %s. Tried: %A" fileName candidates

    let findSummaryTemplate () : string =
        findTemplateFile "tex_summary_template.tex"

    let findTikzTemplate () : string =
        findTemplateFile "tex_tikz_template.tex"

    let findGllStepTemplate () : string =
        findTemplateFile "GLL_step_template.tex"

    let findRnglrStepTemplate () : string =
        findTemplateFile "RNGLR_step_template.tex"

    let findArroyueloStepTemplate () : string =
        findTemplateFile "Arroyuelo_step_template.tex"

    let findArroyueloStepTikzTemplate () : string =
        findTemplateFile "Arroyuelo_step_tikz_template.tex"

    let findBelyaninStepTemplate () : string =
        findTemplateFile "Belyanin_step_template.tex"

    let findBelyaninStepTikzTemplate () : string =
        findTemplateFile "Belyanin_step_tikz_template.tex"

    let findBelyaninRowEndTemplate () : string =
        findTemplateFile "Belyanin_row_end_template.tex"

    let findBelyaninRowEndTikzTemplate () : string =
        findTemplateFile "Belyanin_row_end_tikz_template.tex"

    let findBelyaninLabelRowTemplate () : string =
        findTemplateFile "Belyanin_label_row_template.tex"

    let findBelyaninLabelRowTikzTemplate () : string =
        findTemplateFile "Belyanin_label_row_tikz_template.tex"

    let findGllStepTikzTemplate () : string =
        findTemplateFile "GLL_step_tikz_template.tex"

    let findRnglrStepTikzTemplate () : string =
        findTemplateFile "RNGLR_step_tikz_template.tex"
