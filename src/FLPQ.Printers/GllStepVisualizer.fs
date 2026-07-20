namespace FLPQ.Printers

open FLPQ.Languages

/// GLL parser step-by-step visualization.
module GllStepVisualizer =

    /// GLL-specific visualization step containing rendered outputs for one parsing step.
    type GllVisualizationStep =
        {
            /// Descriptors queue rendered as TeX.
            Queue: string
            /// GSS graph rendered as DOT.
            GssDot: string
            /// Path index matrix rendered as TeX.
            PathIndex: string
            /// Input tokens with position underline rendered as TeX.
            Input: string
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
            descriptors |> List.map descriptorToTeX |> String.concat @" \\; "

    /// Render a single GLL parsing step to visualization output.
    let renderStep
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (step: GLLParsingStep<'t, 'nt>)
        (pathIndex: PathIndex<'t, 'nt>)
        (inputTokens: Terminal<'t> list)
        (vertexCount: int)
        : GllVisualizationStep =
        let termPrinter = TeXRenderer.termPrinterFromSymbolVisualizer symbolVisualizer

        let gssDot =
            GssDot.toDotFromSets
                (fun idx ->
                    let state = idx / vertexCount
                    let vertex = idx % vertexCount
                    sprintf "(%d,%d)" state vertex)
                (fun (from, to_) ->
                    let s1 = from / vertexCount
                    let v1 = from % vertexCount
                    let s2 = to_ / vertexCount
                    let v2 = to_ % vertexCount
                    sprintf "%d,%d→%d,%d" s1 v1 s2 v2)
                step.ActiveGssVertices
                step.ActiveGssEdges
                step.NewGssVertices
                step.NewGssEdges
                step.CurrentGssIdx

        // Create a temporary PathIndex from the step's matrix snapshot for rendering
        let stepPathIndex =
            { Matrix = step.PathIndexMatrix
              StateCount = pathIndex.StateCount
              VertexCount = pathIndex.VertexCount }

        { Queue = queueToTeX step.Queue
          GssDot = gssDot
          PathIndex = PathIndexTeX.toTeXWithHighlights string string stepPathIndex step.ChangedCells
          Input = TeXRenderer.inputRow termPrinter inputTokens step.InputPosition }

    /// Render a list of GLL parsing steps to visualization steps.
    let renderSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (steps: GLLParsingStep<'t, 'nt> list)
        (pathIndex: PathIndex<'t, 'nt>)
        (inputTokens: Terminal<'t> list)
        (vertexCount: int)
        : GllVisualizationStep list =
        steps
        |> List.map (fun step -> renderStep symbolVisualizer step pathIndex inputTokens vertexCount)
