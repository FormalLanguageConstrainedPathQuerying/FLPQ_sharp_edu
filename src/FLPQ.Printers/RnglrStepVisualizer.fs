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
          LrAutomatonDot: string }

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

    let private lrAutomatonToDot
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (lrTable: RnglrTable<'t, 'nt>)
        (lrStateCount: int)
        (currentLrState: int option)
        : string =
        let sb = System.Text.StringBuilder()
        sb.AppendLine("digraph LRAutomaton {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        for idx in 0 .. lrStateCount - 1 do
            let items = lrTable.Automaton.States.[idx]

            let itemLines =
                items
                |> Set.toList
                |> List.map (fun item ->
                    let (Nonterminal ntName) = item.BlockNonterminal
                    sprintf "%s / %d" (nonterminalPrinter ntName) item.RsmState)
                |> String.concat "\\n"

            let label = sprintf "State %d\\n%s" idx itemLines |> DerivationTreeDot.escapeLabel

            let fillColor =
                match currentLrState with
                | Some s when s = idx -> "style=filled, fillcolor=lightblue, "
                | _ -> ""

            let startAttr =
                if idx = lrTable.Automaton.StartState then
                    sprintf "%sstyle=filled, fillcolor=green, " fillColor
                else
                    fillColor

            let finalAttr =
                if Set.contains idx lrTable.Automaton.FinalStates then
                    "peripheries=2, "
                else
                    ""

            sb.AppendLine(sprintf "  s%d [%s%slabel=\"%s\"];" idx startAttr finalAttr label)
            |> ignore

        for fromIdx in 0 .. lrStateCount - 1 do
            for toIdx in 0 .. lrStateCount - 1 do
                match Matrix.get lrTable.Automaton.Transitions fromIdx toIdx with
                | Some labels ->
                    let termLabels =
                        labels
                        |> NonEmptySet.toSeq
                        |> Seq.choose (fun l ->
                            match l with
                            | AutomatonLabel.ATerm sym -> Some(symbolToDotLabel terminalPrinter nonterminalPrinter sym)
                            | AutomatonLabel.AEpsilon -> None)
                        |> List.ofSeq

                    if not (List.isEmpty termLabels) then
                        let label = termLabels |> String.concat ", " |> DerivationTreeDot.escapeLabel

                        sb.AppendLine(sprintf "  s%d -> s%d [label=\"%s\"];" fromIdx toIdx label)
                        |> ignore
                | None -> ()

        sb.AppendLine("}") |> ignore
        sb.ToString()

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
                    let fromLr = fromIdx / vertexCount
                    let fromV = fromIdx % vertexCount
                    let toLr = toIdx / vertexCount
                    let toV = toIdx % vertexCount
                    sprintf "%d,%d → %d,%d" fromLr fromV toLr toV)
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                currentGssIdx

        let lrAutomatonDot =
            lrAutomatonToDot terminals nonterminals lrTable lrStateCount step.CurrentLrState

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
          LrAutomatonDot = lrAutomatonDot }

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
