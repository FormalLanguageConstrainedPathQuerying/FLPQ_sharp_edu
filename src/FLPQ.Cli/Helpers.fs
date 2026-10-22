namespace FLPQ.Cli

open System
open System.IO
open System.Text.RegularExpressions
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers

module Helpers =

    /// Reads a file, throwing CliErrorException if it does not exist. Returns the content trimmed.
    let readFile path =
        if not (File.Exists path) then
            raise (CliErrorException(sprintf "File not found: %s" path))

        File.ReadAllText(path).Trim()

    /// Creates the parent directory if needed and writes the content to the file.
    let writeOutputFile (path: string) (content: string) =
        let dir = Path.GetDirectoryName path

        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, content)

    /// Removes and recreates the output directory (ensuring it is empty before a run).
    let cleanOutputDir (dir: string) =
        if Directory.Exists dir then
            if Directory.GetFileSystemEntries(dir).Length > 0 then
                Directory.Delete(dir, true)
                Directory.CreateDirectory dir |> ignore
        else
            Directory.CreateDirectory dir |> ignore

    /// Writes one directory per parsing step (step_<n>) with the tree/stack visualization and input.
    let writeStepsVisualization (outputDir: string) (useDot: bool) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            if useDot then
                writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].TreeAndStack
            else
                writeOutputFile (Path.Combine(stepDir, "tree_and_stack.tikz.tex")) steps.[idx].TreeAndStackTikz

            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].Input

    /// Writes one directory per GLL step (step_<n>) with queue, descriptor tables, path index, and GSS/RSM figures.
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

    /// Writes one directory per RNGLR step (step_<n>) with path index, LR table, and GSS/input figures.
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

    /// Writes one directory per Arroyuelo RPQ step (step_<n>) with matrix and tree/graph figures.
    let writeArroyueloStepsVisualization
        (outputDir: string)
        (useDot: bool)
        (steps: ArroyueloStepCommon.ArroyueloVisualizationStep list)
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
        (steps: BelyaninStepCommon.BelyaninVisualizationStep list)
        =
        // One step-boundary group: the frontier/visited matrix files plus the automaton/graph
        // figures in the selected format.
        let writeBoundary
            (stepDir: string)
            (suffix: string)
            (boundary: BelyaninStepCommon.BelyaninBoundaryVisual)
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
            RpqGraphViz.renderGraph
                id
                graph
                Set.empty
                graph.StartStates
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                Map.empty

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

    /// The CFPQ input-graph artifact: all vertex pairs are queried, so there are no sources to
    /// highlight. Written as graph.tikz.tex (default) or graph.dot (--use-dot). Shared by the
    /// Azimov and Kronecker root-artifact writers.
    let private writeCfpqGraphArtifact (outputDir: string) (useDot: bool) (graph: CfpqInput.CfpqGraph<string>) : unit =
        let graphDot, graphTikz =
            RpqGraphViz.renderGraph
                id
                (CfpqInput.toNfa graph)
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                Set.empty
                Map.empty

        if useDot then
            writeOutputFile (Path.Combine(outputDir, "graph.dot")) graphDot
        else
            writeOutputFile (Path.Combine(outputDir, "graph.tikz.tex")) graphTikz

    /// Shared Azimov CFPQ root artifacts: the original grammar, the CNF passed to the
    /// algorithm, and the input graph — DOT source or inline TikZ depending on useDot.
    let writeCfpqRootArtifacts
        (outputDir: string)
        (useDot: bool)
        (grammar: Grammar<string, string>)
        (cnf: Grammar<string, string>)
        (graph: CfpqInput.CfpqGraph<string>)
        : unit =
        writeOutputFile
            (Path.Combine(outputDir, "grammar_original.tex"))
            (GrammarTeX.grammarToTeXWithNumbers string string grammar)

        writeOutputFile
            (Path.Combine(outputDir, "grammar_cnf.tex"))
            (GrammarTeX.grammarToTeXWithNumbers string string cnf)

        writeCfpqGraphArtifact outputDir useDot graph

    /// Shared Kronecker CFPQ root artifacts: the query RSM figure and the input graph — DOT
    /// source or inline TikZ depending on useDot. The RSM is rendered in global-state appearance
    /// order (no fresh start block, no highlighted state).
    let writeKroneckerCfpqRootArtifacts
        (outputDir: string)
        (useDot: bool)
        (rsm: RSM<string, string>)
        (graph: CfpqInput.CfpqGraph<string>)
        : unit =
        if useDot then
            writeOutputFile (Path.Combine(outputDir, "rsm.dot")) (RsmDot.toDot string string rsm)
        else
            writeOutputFile (Path.Combine(outputDir, "rsm.tikz.tex")) (RsmTikz.rsmToTikz string string rsm)

        writeCfpqGraphArtifact outputDir useDot graph

    /// The name of a string nonterminal (its bare symbol).
    let ntName (nt: Nonterminal<string>) : string =
        match nt with
        | Nonterminal s -> s

    /// The name of a graph vertex in result output.
    let vertexName (i: int) : string = sprintf "v_%d" i

    /// The reachable pairs of the answer matrix, in row-major order.
    let reachablePairs (answer: Matrix<bool>) : (int * int) list =
        [ for i in 0 .. Matrix.rows answer - 1 do
              for j in 0 .. Matrix.cols answer - 1 do
                  if answer.[i, j] then
                      (i, j) ]

    /// The result file: the answer matrix as a Boolean matrix plus the list of reachable pairs.
    let renderResult (answer: Matrix<bool>) : string =
        let matrixTex = PathSemiringTeX.boolMatrixToTeX vertexName vertexName answer
        let pairs = reachablePairs answer

        let pairLines =
            pairs
            |> List.map (fun (i, j) -> sprintf "(%s, %s)" (vertexName i) (vertexName j))

        matrixTex
        + "\n\n"
        + sprintf "Reachable pairs: %d\n%s" (List.length pairs) (String.concat ", " pairLines)

    /// One directory per productive fixed-point iteration (step_<iteration>): the
    /// per-nonterminal matrices of that iteration with the newly changed cells highlighted.
    let writeCfpqSteps
        (outputDir: string)
        (steps: AzimovCfpq.CfpqStep<'nt, 'elem> list)
        (ntName: Nonterminal<'nt> -> string)
        (elementPrinter: 'elem -> string)
        : unit =
        for step in steps do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" step.Iteration)

            writeOutputFile
                (Path.Combine(stepDir, "table.tex"))
                (CfpqMatrixTeX.stepToTeX ntName elementPrinter step.Matrices step.ChangedCells)

    /// One directory per productive Kronecker iteration (step_<iteration>) with the two-line
    /// step visualization: `kron.tex` (line 1 — the full Kronecker product), `fronts.tex`
    /// (line 2 — the MS-BFS initial/final fronts), and the input graph with the newly added
    /// nonterminal edges of this iteration highlighted red bold (`graph.dot` / `graph.tikz.tex`).
    /// String-labeled (the graph figure needs `CfpqInput.toNfa`, which is string-only).
    let writeKroneckerStepsVisualization
        (outputDir: string)
        (useDot: bool)
        (steps: KroneckerCfpq.CfpqKronStep<string, string> list)
        (terminal: string -> string)
        (ntName: Nonterminal<string> -> string)
        (rsm: RSM<string, string>)
        (graph: CfpqInput.CfpqGraph<string>)
        : unit =
        let n = graph.VertexCount

        /// Nonterminal edges derived by every earlier iteration, preserved (black) in each
        /// later step's graph so the reader can see how the derived graph grows; only the
        /// current iteration's edges are highlighted red bold.
        let mutable seenEdges: Map<int * int, string> = Map.empty

        /// The true cells of the previous iteration's K mask, so each step can highlight only the
        /// K cells that turned on for the first time this iteration (the diff). K grows
        /// monotonically across iterations.
        let mutable prevK: Set<int * int> = Set.empty

        /// The cells (row, col) of a Boolean matrix whose value is true.
        let trueCells (m: Matrix<bool>) : Set<int * int> =
            [ for i in 0 .. (Matrix.rows m - 1) do
                  for j in 0 .. (Matrix.cols m - 1) do
                      if m.[i, j] then
                          (i, j) ]
            |> Set.ofList

        for step in steps do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" step.Iteration)

            // The K cells that turned on for the first time this iteration (the diff against the
            // previous iteration's K).
            let kNewCells = Set.difference (trueCells step.K) prevK

            // The newly derived nonterminal edges of this iteration: (u, v) vertex pairs
            // labeled by the nonterminal, drawn red bold on top of the input graph.
            let newNtEdges =
                step.ChangedCells
                |> List.map (fun c -> ((c.Row, c.Col), ntName c.Nt))
                |> Map.ofList

            let newNtEdgeSet = newNtEdges |> Map.toList |> List.map fst |> Set.ofList

            let allNtEdges =
                Map.toList newNtEdges @ Map.toList seenEdges
                |> List.distinctBy fst
                |> Map.ofList

            let allNtEdgeSet = Map.toList allNtEdges |> List.map fst |> Set.ofList

            let graphDot, graphTikz =
                RpqGraphViz.renderGraph
                    terminal
                    (CfpqInput.toNfa graph)
                    Set.empty
                    Set.empty
                    Set.empty
                    newNtEdgeSet
                    Set.empty
                    allNtEdgeSet
                    allNtEdges

            seenEdges <- allNtEdges

            let rendered =
                KroneckerStepViz.renderStep terminal ntName n step kNewCells graphDot graphTikz

            prevK <- trueCells step.K

            writeOutputFile (Path.Combine(stepDir, "kron.tex")) rendered.Kron
            writeOutputFile (Path.Combine(stepDir, "fronts.tex")) rendered.Fronts

            if useDot then
                writeOutputFile (Path.Combine(stepDir, "graph.dot")) rendered.GraphDot
            else
                writeOutputFile (Path.Combine(stepDir, "graph.tikz.tex")) rendered.GraphTikz

    /// Extracts the step number from a step directory name (step_N); returns 0 if not a step dir.
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
        | None -> raise (CliErrorException(sprintf "Could not locate %s. Tried: %A" fileName candidates))

    /// Path to the merged-summary TeX template.
    let findSummaryTemplate () : string =
        findTemplateFile "tex_summary_template.tex"

    /// Path to the TikZ document wrapper template.
    let findTikzTemplate () : string =
        findTemplateFile "tex_tikz_template.tex"

    /// Path to the GLL step TeX template.
    let findGllStepTemplate () : string =
        findTemplateFile "GLL_step_template.tex"

    /// Path to the RNGLR step TeX template.
    let findRnglrStepTemplate () : string =
        findTemplateFile "RNGLR_step_template.tex"

    /// Path to the Arroyuelo step TeX template.
    let findArroyueloStepTemplate () : string =
        findTemplateFile "Arroyuelo_step_template.tex"

    /// Path to the Arroyuelo step TikZ template.
    let findArroyueloStepTikzTemplate () : string =
        findTemplateFile "Arroyuelo_step_tikz_template.tex"

    /// Path to the Belyanin step TeX template.
    let findBelyaninStepTemplate () : string =
        findTemplateFile "Belyanin_step_template.tex"

    /// Path to the Belyanin step TikZ template.
    let findBelyaninStepTikzTemplate () : string =
        findTemplateFile "Belyanin_step_tikz_template.tex"

    /// Path to the Belyanin row-end TeX template.
    let findBelyaninRowEndTemplate () : string =
        findTemplateFile "Belyanin_row_end_template.tex"

    /// Path to the Belyanin row-end TikZ template.
    let findBelyaninRowEndTikzTemplate () : string =
        findTemplateFile "Belyanin_row_end_tikz_template.tex"

    /// Path to the Belyanin label-row TeX template.
    let findBelyaninLabelRowTemplate () : string =
        findTemplateFile "Belyanin_label_row_template.tex"

    /// Path to the Belyanin label-row TikZ template.
    let findBelyaninLabelRowTikzTemplate () : string =
        findTemplateFile "Belyanin_label_row_tikz_template.tex"

    /// Path to the GLL step TikZ template.
    let findGllStepTikzTemplate () : string =
        findTemplateFile "GLL_step_tikz_template.tex"

    /// Path to the RNGLR step TikZ template.
    let findRnglrStepTikzTemplate () : string =
        findTemplateFile "RNGLR_step_tikz_template.tex"

    /// Path to the Kronecker CFPQ step TeX template.
    let findKroneckerStepTemplate () : string =
        findTemplateFile "Kronecker_step_template.tex"

    /// Path to the Kronecker CFPQ step TikZ template.
    let findKroneckerStepTikzTemplate () : string =
        findTemplateFile "Kronecker_step_tikz_template.tex"
