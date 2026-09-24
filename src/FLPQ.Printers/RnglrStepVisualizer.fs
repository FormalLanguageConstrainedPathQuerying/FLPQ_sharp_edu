namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra

module RnglrStepVisualizer =

    /// Rendered artifacts of one RNGLR action substep: GSS figure (DOT and TikZ), path index,
    /// input graph (DOT and TikZ), and the LR table with the substep's action cell(s)
    /// highlighted.
    type RnglrVisualizationStep =
        { GssDot: string
          GssTikz: string
          PathIndex: string
          Input: string
          InputTikz: string
          LrTable: string }

    let private symbolToDotLabel
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (sym: Symbol<'t, 'nt>)
        : string =
        match sym with
        | Symbol.T(Terminal t) -> sprintf "\"%s\"" (terminalPrinter t)
        | Symbol.N(Nonterminal nt) -> nonterminalPrinter nt
        | Symbol.Epsilon -> "ε"

    /// Renders a single RNGLR action substep into its visualization artifacts. The LR table
    /// highlights exactly the cell(s) of the substep's action: shift → the Action cell
    /// (lrState, terminal) in green; reduce → the Goto cell (gotoLrState, nonterminal) and the
    /// Action cell (triggerLrState, $) in red; initial state (Action = None) → no highlight.
    let renderStep
        (terminals: 't -> string)
        (nonterminals: 'nt -> string)
        (lrTable: RnglrTable<'t, 'nt>)
        (vertexInfo: int -> int * int)
        (step: RnglrParsingStep<'t, 'nt>)
        (pathIndex: PathIndex<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : RnglrVisualizationStep =
        let gssDot =
            GssDot.toDotFromSets
                (fun idx ->
                    let lrState, inputVertex = vertexInfo idx
                    sprintf "%d: (%d,%d)" idx lrState inputVertex)
                (fun (fromIdx, toIdx) ->
                    match Map.tryFind (fromIdx, toIdx) step.ActiveGssEdgeSymbols with
                    | Some symbols ->
                        symbols
                        |> NonEmptySet.toSeq
                        |> Seq.map (symbolToDotLabel terminals nonterminals)
                        |> String.concat ", "
                    | None -> "")
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                step.PassingReductionVertices
                None
                (Some(fun idx -> snd (vertexInfo idx)))

        let gssTikz =
            GssTikz.toTikzFromSets
                (fun idx ->
                    let lrState, inputVertex = vertexInfo idx
                    sprintf "%d: (%d,%d)" idx lrState inputVertex)
                (fun (fromIdx, toIdx) ->
                    match Map.tryFind (fromIdx, toIdx) step.ActiveGssEdgeSymbols with
                    | Some symbols ->
                        symbols
                        |> NonEmptySet.toSeq
                        // Escape only the printer-produced names; the epsilon label
                        // is math-mode TeX and must not be escaped (as in RsmTikz).
                        |> Seq.map (fun sym ->
                            match sym with
                            | Symbol.T(Terminal t) -> AutomatonTikz.escapeLatex (terminals t)
                            | Symbol.N(Nonterminal nt) -> AutomatonTikz.escapeLatex (nonterminals nt)
                            | Symbol.Epsilon -> "$\\varepsilon$")
                        |> String.concat ", "
                    | None -> "")
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                step.PassingReductionVertices
                None
                "circle"
                true
                (Some(fun idx -> snd (vertexInfo idx)))
                false

        let lrTable =
            match step.Action with
            | None -> RnglrTableTeX.tableToTeXTabularOnly terminals nonterminals lrTable
            | Some action ->
                RnglrTableTeX.tableToTeXWithActionHighlightTabularOnly terminals nonterminals lrTable action

        let stepPi =
            { Matrix = step.PathIndexMatrix
              StateCount = pathIndex.StateCount
              VertexCount = pathIndex.VertexCount }

        { GssDot = gssDot
          GssTikz = gssTikz
          PathIndex = PathIndexTeX.toTeXWithHighlights terminals nonterminals stepPi step.ChangedCells
          Input = InputGraphDot.toDot terminals inputGraph (Some step.InputVertex)
          InputTikz = InputGraphTikz.toTikz terminals inputGraph (Some step.InputVertex)
          LrTable = lrTable }

    /// Renders every RNGLR action substep into its visualization artifacts.
    let renderSteps
        (terminals: 't -> string)
        (nonterminals: 'nt -> string)
        (lrTable: RnglrTable<'t, 'nt>)
        (vertexInfo: int -> int * int)
        (steps: RnglrParsingStep<'t, 'nt> list)
        (pathIndex: PathIndex<'t, 'nt>)
        (inputGraph: Graph<int, Option<'t>>)
        : RnglrVisualizationStep list =
        steps
        |> List.map (fun step -> renderStep terminals nonterminals lrTable vertexInfo step pathIndex inputGraph)
