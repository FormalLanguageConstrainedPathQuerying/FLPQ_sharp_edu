namespace FLPQ.Printers

/// Generic TeX helpers shared by the RPQ step visualizers.
module StepTeX =

    /// Wrap `content` in a single shrink-only adjustbox with the given options.
    let wrapAdjustbox (options: string) (content: string) : string =
        @"\begin{adjustbox}{"
        + options
        + "}"
        + "\n"
        + content
        + "\n"
        + @"\end{adjustbox}"

    /// Wrap a one-line math expression in a single shrink-only adjustbox so side-by-side
    /// matrices fit the formula column. Top-aligned (`valign=T`) so the formula and the
    /// figure beside it line up.
    let wrapFormula (expression: string) : string =
        wrapAdjustbox @"max width=\textwidth, valign=T" expression
