namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra

module RnglrStepVisualizer =

    /// Rendered artifacts of one RNGLR level step: GSS figure (DOT and TikZ), path index,
    /// input graph (DOT and TikZ), and the highlighted LR table.
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

    /// Renders a single RNGLR level step into its visualization artifacts.
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
                        |> Seq.map (fun sym ->
                            match sym with
                            | Symbol.T(Terminal t) -> terminals t
                            | Symbol.N(Nonterminal nt) -> nonterminals nt
                            | Symbol.Epsilon -> "\\varepsilon")
                        |> String.concat ", "
                    | None -> "")
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                step.PassingReductionVertices
                None
                "circle"
                false

        let activeActions =
            let shifts =
                step.ActiveShiftTerminals |> Set.map (fun (Terminal t) -> Symbol.T(Terminal t))

            let reduces = step.ActiveReduceNonterminals |> Set.map Symbol.N

            Set.union shifts reduces

        let lrTable =
            RnglrTableTeX.tableToTeXWithHighlightsTabularOnly
                terminals
                nonterminals
                lrTable
                None
                activeActions
                step.LevelReductions

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

    /// Renders every RNGLR level step into its visualization artifacts.
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
