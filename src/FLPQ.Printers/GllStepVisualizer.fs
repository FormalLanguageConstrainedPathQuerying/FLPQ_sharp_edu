namespace FLPQ.Printers

open FLPQ.Languages
open FLPQ.GraphAnalysis

/// GLL parser step-by-step visualization.
module GllStepVisualizer =

    /// GLL-specific visualization step containing rendered outputs for one parsing step.
    type GllVisualizationStep =
        {
            /// Descriptors queue rendered as TeX.
            Queue: string
            /// Descriptors table (to-handle and handled) rendered as TeX.
            DescriptorsTable: string
            /// Newly added descriptors rendered as TeX with coloring.
            NewDescriptors: string
            /// GSS graph rendered as DOT.
            GssDot: string
            /// GSS graph rendered as TikZ.
            GssTikz: string
            /// Path index matrix rendered as TeX.
            PathIndex: string
            /// Input tokens with position underline rendered as TeX.
            Input: string
            /// Input graph rendered as TikZ.
            InputTikz: string
            /// Extended RSM rendered as DOT with highlighted current state.
            RsmDot: string
            /// Extended RSM rendered as TikZ with highlighted current state.
            RsmTikz: string
        }

    /// Render the matched range component of a descriptor as TeX.
    let rangeDescriptorToTeX (range: RangeDescriptor) : string =
        match range with
        | RangeDescriptor.EmptyRange -> @"\emptyset"
        | RangeDescriptor.NonEmptyRange rk ->
            sprintf @"R^{%d,%d}_{%d,%d}" rk.FromState rk.FromVertex rk.ToState rk.ToVertex

    /// Render a single descriptor as a tuple: (rsmState, vertex, gssIdx, matchedRange).
    let descriptorToTeX (desc: Descriptor) : string =
        let rangeTex = rangeDescriptorToTeX desc.MatchedRange
        sprintf @"(%d, %d, %d, %s)" desc.RsmState desc.Vertex desc.GssIdx rangeTex

    /// Render a list of descriptors as a TeX list of tuples.
    let queueToTeX (descriptors: Descriptor list) : string =
        if List.isEmpty descriptors then
            @"\emptyset"
        else
            let items = descriptors |> List.map descriptorToTeX |> String.concat @" \\; "
            sprintf @"\begin{gathered} %s \end{gathered}" items

    /// Render descriptor components as TeX cells for table row.
    let private descriptorToRowCells (desc: Descriptor) : string * string * string * string =
        let rangeTex = rangeDescriptorToTeX desc.MatchedRange
        string desc.RsmState, string desc.Vertex, string desc.GssIdx, rangeTex

    /// Render a table of descriptors with to-handle and handled blocks,
    /// highlighting the current descriptor with yellow background.
    /// Table structure:
    ///   Header: q | i | g | \mathcal{MR}
    ///   \hline\hline
    ///   Block 1: descriptors to handle (queue)
    ///   \hline\hline
    ///   Block 2: handled descriptors
    let descriptorsTableToTeX
        (currentDescriptor: Descriptor option)
        (toHandle: Descriptor list)
        (handled: Set<Descriptor>)
        : string =
        let header = @"q & i & g & \mathcal{MR} \\ \hline\hline"

        let renderRow (desc: Descriptor) (isCurrent: bool) : string =
            let q, i, g, mr = descriptorToRowCells desc

            if isCurrent then
                sprintf @"\rowcolor{yellow!20} %s & %s & %s & %s \\" q i g mr
            else
                sprintf @"%s & %s & %s & %s \\" q i g mr

        let toHandleRows =
            if List.isEmpty toHandle then
                [ @"\emptyset & & & \\" ]
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
                [ @"\emptyset & & & \\" ]
            else
                handled |> Set.toList |> List.map (fun d -> renderRow d false)

        let rows = toHandleRows @ [ @"\hline\hline" ] @ handledRows |> String.concat "\n"

        sprintf @"\begin{array}{cccc} %s %s \end{array}" header rows

    /// Render newly created descriptors as a set of tuples with color-coded highlighting.
    /// Green background: genuinely new descriptors (not previously handled).
    /// Red background: already handled descriptors that were attempted again in this step.
    let newDescriptorsToTeX (newDescriptors: Set<Descriptor>) (attemptedDescriptors: Set<Descriptor>) : string =
        if Set.isEmpty attemptedDescriptors then
            @"\{ \emptyset \}"
        else
            let renderEntry (desc: Descriptor) (isReallyNew: bool) =
                let tex = descriptorToTeX desc

                if isReallyNew then
                    sprintf @"\colorbox{green!20}{$%s$}" tex
                else
                    sprintf @"\colorbox{red!20}{$%s$}" tex

            attemptedDescriptors
            |> Set.toList
            |> List.map (fun d -> renderEntry d (Set.contains d newDescriptors))
            |> String.concat @",\; "
            |> sprintf @"\{ %s \}"

    /// Render a single GLL parsing step to visualization output.
    let renderStep
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (ersm: ExtendedRSM<'t, 'nt>)
        (step: GLLParsingStep<'t, 'nt>)
        (pathIndex: PathIndex<'t, 'nt>)
        (vertexCount: int)
        (inputGraph: Graph<int, Option<'t>>)
        : GllVisualizationStep =

        let gssDot =
            GssDot.toDotFromSets
                (fun idx ->
                    let state = idx / vertexCount
                    let vertex = idx % vertexCount
                    sprintf "%d: (%d,%d)" idx state vertex)
                (fun (from, to_) ->
                    let s1 = from / vertexCount
                    let v1 = from % vertexCount
                    let s2 = to_ / vertexCount
                    let v2 = to_ % vertexCount
                    sprintf "%d,%d → %d,%d" s1 v1 s2 v2)
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                step.CurrentGssIdx

        let gssTikz =
            GssTikz.toTikzFromSets
                (fun idx ->
                    let state = idx / vertexCount
                    let vertex = idx % vertexCount
                    sprintf "%d: (%d,%d)" idx state vertex)
                (fun (from, to_) ->
                    let s1 = from / vertexCount
                    let v1 = from % vertexCount
                    let s2 = to_ / vertexCount
                    let v2 = to_ % vertexCount
                    sprintf "%d,%d \\to %d,%d" s1 v1 s2 v2)
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                step.CurrentGssIdx

        let rsmDot =
            let currentState = step.CurrentDescriptor |> Option.map (fun d -> d.RsmState)
            RsmDot.extendedRsmToDot terminalPrinter nonterminalPrinter ersm currentState

        let rsmTikz =
            let currentState = step.CurrentDescriptor |> Option.map (fun d -> d.RsmState)
            RsmTikz.extendedRsmToTikz terminalPrinter nonterminalPrinter ersm currentState

        // Create a temporary PathIndex from the step's matrix snapshot for rendering
        let stepPathIndex =
            { Matrix = step.PathIndexMatrix
              StateCount = pathIndex.StateCount
              VertexCount = pathIndex.VertexCount }

        { Queue = queueToTeX step.Queue
          DescriptorsTable = descriptorsTableToTeX step.CurrentDescriptor step.Queue step.HandledDescriptors
          NewDescriptors = newDescriptorsToTeX step.NewDescriptors step.AttemptedDescriptors
          GssDot = gssDot
          GssTikz = gssTikz
          PathIndex = PathIndexTeX.toTeXWithHighlights string string stepPathIndex step.ChangedCells
          Input = InputGraphDot.toDot terminalPrinter inputGraph (Some step.InputPosition)
          InputTikz = InputGraphTikz.toTikz terminalPrinter inputGraph (Some step.InputPosition)
          RsmDot = rsmDot
          RsmTikz = rsmTikz }

    /// Render the initialization step with no highlights.
    /// The initial descriptor is present in the table but not highlighted;
    /// no GSS/RSM/input vertices are highlighted; no new descriptor coloring;
    /// path index has no cell highlights.
    let renderInit
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (ersm: ExtendedRSM<'t, 'nt>)
        (step: GLLParsingStep<'t, 'nt>)
        (pathIndex: PathIndex<'t, 'nt>)
        (vertexCount: int)
        (inputGraph: Graph<int, Option<'t>>)
        : GllVisualizationStep =

        let gssDot =
            GssDot.toDotFromSets
                (fun idx ->
                    let state = idx / vertexCount
                    let vertex = idx % vertexCount
                    sprintf "%d: (%d,%d)" idx state vertex)
                (fun (from, to_) ->
                    let s1 = from / vertexCount
                    let v1 = from % vertexCount
                    let s2 = to_ / vertexCount
                    let v2 = to_ % vertexCount
                    sprintf "%d,%d → %d,%d" s1 v1 s2 v2)
                step.ActiveGssVertices
                step.ActiveGssEdges
                Set.empty
                Set.empty
                None

        let gssTikz =
            GssTikz.toTikzFromSets
                (fun idx ->
                    let state = idx / vertexCount
                    let vertex = idx % vertexCount
                    sprintf "%d: (%d,%d)" idx state vertex)
                (fun (from, to_) ->
                    let s1 = from / vertexCount
                    let v1 = from % vertexCount
                    let s2 = to_ / vertexCount
                    let v2 = to_ % vertexCount
                    sprintf "%d,%d \\to %d,%d" s1 v1 s2 v2)
                step.ActiveGssVertices
                step.ActiveGssEdges
                Set.empty
                Set.empty
                None

        let rsmDot = RsmDot.extendedRsmToDot terminalPrinter nonterminalPrinter ersm None

        let rsmTikz = RsmTikz.extendedRsmToTikz terminalPrinter nonterminalPrinter ersm None

        let stepPathIndex =
            { Matrix = step.PathIndexMatrix
              StateCount = pathIndex.StateCount
              VertexCount = pathIndex.VertexCount }

        { Queue = queueToTeX step.Queue
          DescriptorsTable = descriptorsTableToTeX step.CurrentDescriptor step.Queue step.HandledDescriptors
          NewDescriptors = @"\{ \emptyset \}"
          GssDot = gssDot
          GssTikz = gssTikz
          PathIndex = PathIndexTeX.toTeX string string stepPathIndex
          Input = InputGraphDot.toDot terminalPrinter inputGraph None
          InputTikz = InputGraphTikz.toTikz terminalPrinter inputGraph None
          RsmDot = rsmDot
          RsmTikz = rsmTikz }

    /// Render a list of GLL parsing steps to visualization steps.
    let renderSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (ersm: ExtendedRSM<'t, 'nt>)
        (steps: GLLParsingStep<'t, 'nt> list)
        (pathIndex: PathIndex<'t, 'nt>)
        (vertexCount: int)
        (inputGraph: Graph<int, Option<'t>>)
        : GllVisualizationStep list =
        steps
        |> List.map (fun step ->
            match step.CurrentDescriptor with
            | None ->
                renderInit
                    symbolVisualizer
                    terminalPrinter
                    nonterminalPrinter
                    ersm
                    step
                    pathIndex
                    vertexCount
                    inputGraph
            | Some _ ->
                renderStep
                    symbolVisualizer
                    terminalPrinter
                    nonterminalPrinter
                    ersm
                    step
                    pathIndex
                    vertexCount
                    inputGraph)
