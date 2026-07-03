namespace FLPQ.Printers

open FLPQ.Languages

/// LL parser step-by-step visualization.
module LLStepVisualizer =

    /// Render a single LL parsing step to a visualization step (DOT + TeX).
    let renderStep (symbolVisualizer: Symbol<'t, 'nt> -> string) (step: LLParsingStep<'t, 'nt>) : VisualizationStep =
        let termPrinter (Terminal t) = symbolVisualizer (T(Terminal t))

        { treeAndStack = DerivationTreeDot.toDotWithLLStack symbolVisualizer step.stack
          input = TeXRenderer.inputRow termPrinter step.input.tokens step.input.position }

    /// Render a list of LL parsing steps to visualization steps.
    let renderSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (steps: LLParsingStep<'t, 'nt> list)
        : VisualizationStep list =
        steps |> List.map (renderStep symbolVisualizer)
