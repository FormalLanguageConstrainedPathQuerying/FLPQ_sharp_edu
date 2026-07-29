namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.GraphAnalysis
open FLPQ.LinearAlgebra

module RnglrStepVisualizer =

    type RnglrVisualizationStep =
        { DescriptorsTable: string
          NewDescriptors: string
          GssDot: string
          PathIndex: string
          Input: string
          LrTable: string }

    let private rnglrDescriptorToTeX (desc: RnglrDescriptor) : string =
        sprintf @"(%d, %d)" desc.LrState desc.Vertex

    let private descriptorsTableToTeX
        (currentDescriptor: RnglrDescriptor option)
        (toHandle: RnglrDescriptor list)
        (handled: Set<RnglrDescriptor>)
        : string =
        let header = @"\text{lrState} & \text{input} \\ \hline\hline"

        let renderRow (desc: RnglrDescriptor) (isCurrent: bool) : string =
            if isCurrent then
                sprintf @"\rowcolor{yellow!20} %d & %d \\" desc.LrState desc.Vertex
            else
                sprintf @"%d & %d \\" desc.LrState desc.Vertex

        let toHandleRows =
            if List.isEmpty toHandle then
                [ @"\emptyset & \\" ]
            else
                toHandle
                |> List.map (fun d ->
                    let isCurrent =
                        match currentDescriptor with
                        | Some cd -> cd.Equals(d)
                        | None -> false

                    renderRow d isCurrent)

        let handledRows =
            if Set.isEmpty handled then
                [ @"\emptyset & \\" ]
            else
                handled |> Set.toList |> List.map (fun d -> renderRow d false)

        let rows = toHandleRows @ [ @"\hline\hline" ] @ handledRows |> String.concat "\n"

        sprintf @"\begin{array}{cc} %s %s \end{array}" header rows

    let private newDescriptorsToTeX
        (newDescriptors: Set<RnglrDescriptor>)
        (attemptedDescriptors: Set<RnglrDescriptor>)
        : string =
        if Set.isEmpty attemptedDescriptors then
            @"\{ \emptyset \}"
        else
            let renderEntry (desc: RnglrDescriptor) (isReallyNew: bool) =
                let tex = rnglrDescriptorToTeX desc

                if isReallyNew then
                    sprintf @"\colorbox{green!20}{$%s$}" tex
                else
                    sprintf @"\colorbox{red!20}{$%s$}" tex

            attemptedDescriptors
            |> Set.toList
            |> List.map (fun d -> renderEntry d (Set.contains d newDescriptors))
            |> String.concat @",\; "
            |> sprintf @"\{ %s \}"

    let private symbolToDotLabel
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (sym: Symbol<'t, 'nt>)
        : string =
        match sym with
        | Symbol.T(Terminal t) -> sprintf "\"%s\"" (terminalPrinter t)
        | Symbol.N(Nonterminal nt) -> nonterminalPrinter nt
        | Symbol.Epsilon -> "ε"

    let renderStep
        (terminals: 't -> string)
        (nonterminals: 'nt -> string)
        (lrTable: RnglrTable<'t, 'nt>)
        (lrStateCount: int)
        (step: RnglrParsingStep<'t, 'nt>)
        (pathIndex: PathIndex<'t, 'nt>)
        (vertexCount: int)
        (inputGraph: Graph<int, Option<'t>>)
        : RnglrVisualizationStep =
        let currentGssIdx =
            match step.CurrentLrState with
            | Some lrState -> Some(lrState * vertexCount + step.InputVertex)
            | None -> None

        let gssDot =
            GssDot.toDotFromSets
                (fun idx ->
                    let lrState = idx / vertexCount
                    let inputVertex = idx % vertexCount
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
                currentGssIdx

        let activeActions =
            let shifts =
                step.ActiveShiftTerminals |> Set.map (fun (Terminal t) -> Symbol.T(Terminal t))

            let reduces = step.ActiveReduceNonterminals |> Set.map (fun nt -> Symbol.N nt)

            Set.union shifts reduces

        let lrTable =
            RnglrTableTeX.tableToTeXWithHighlights
                terminals
                nonterminals
                lrTable
                step.CurrentLrState
                activeActions
                step.LevelReductions

        let stepPi =
            { Matrix = step.PathIndexMatrix
              StateCount = pathIndex.StateCount
              VertexCount = pathIndex.VertexCount }

        { DescriptorsTable =
            descriptorsTableToTeX
                step.CurrentDescriptor
                (step.PendingQueues |> Array.toList |> List.concat)
                step.HandledDescriptors
          NewDescriptors = newDescriptorsToTeX step.NewDescriptors step.AttemptedDescriptors
          GssDot = gssDot
          PathIndex = PathIndexTeX.toTeXWithHighlights terminals nonterminals stepPi step.ChangedCells
          Input = InputGraphDot.toDot terminals inputGraph (Some step.InputVertex)
          LrTable = lrTable }

    let renderSteps
        (terminals: 't -> string)
        (nonterminals: 'nt -> string)
        (lrTable: RnglrTable<'t, 'nt>)
        (lrStateCount: int)
        (steps: RnglrParsingStep<'t, 'nt> list)
        (pathIndex: PathIndex<'t, 'nt>)
        (vertexCount: int)
        (inputGraph: Graph<int, Option<'t>>)
        : RnglrVisualizationStep list =
        steps
        |> List.map (fun step ->
            renderStep terminals nonterminals lrTable lrStateCount step pathIndex vertexCount inputGraph)
