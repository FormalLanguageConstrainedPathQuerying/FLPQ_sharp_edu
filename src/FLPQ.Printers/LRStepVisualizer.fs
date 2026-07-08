namespace FLPQ.Printers

open FLPQ.Languages

/// LR parser step-by-step visualization.
module LRStepVisualizer =

    /// Render a single LR parsing step to a visualization step (DOT + TeX).
    let renderStep (symbolVisualizer: Symbol<'t, 'nt> -> string) (step: LRParsingStep<'t, 'nt>) : VisualizationStep =
        let termPrinter = TeXRenderer.termPrinterFromSymbolVisualizer symbolVisualizer

        { treeAndStack = DerivationTreeDot.toDotWithLRStack symbolVisualizer step.Stack
          input = TeXRenderer.inputRow termPrinter step.Input.Tokens step.Input.Position }

    /// Render a list of LR parsing steps to visualization steps.
    let renderSteps
        (symbolVisualizer: Symbol<'t, 'nt> -> string)
        (steps: LRParsingStep<'t, 'nt> list)
        : VisualizationStep list =
        steps |> List.map (renderStep symbolVisualizer)
