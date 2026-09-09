namespace FLPQ.Printers

open FLPQ.Languages

/// Shared TeX rendering helpers for parser visualization.
module TeXRenderer =

    /// Create a terminal printer function from a symbol visualizer.
    let termPrinterFromSymbolVisualizer (symbolVisualizer: Symbol<'t, 'nt> -> string) : Terminal<'t> -> string =
        fun (Terminal t) -> symbolVisualizer (Symbol.T(Terminal t))

    /// Escape TeX special characters for use in math mode.
    /// The output is safe to embed inside `$...$`, `pNiceMatrix`, or TikZ `as={$...$}` content.
    let escapeMath (s: string) : string =
        s
            .Replace(@"\", @"\textbackslash ")
            .Replace("&", @"\&")
            .Replace("%", @"\%")
            .Replace("$", @"\$")
            .Replace("#", @"\#")
            .Replace("_", @"\_")
            .Replace("{", @"\{")
            .Replace("}", @"\}")
            .Replace("^", @"\text{\^{}}")

    /// Render input tokens with the current position underlined.
    /// Each token is escaped for math mode (the row is a pNiceMatrix), so terminals
    /// containing TeX special characters (e.g. the `$` end-of-input marker) compile correctly.
    let inputRow (symbolPrinter: Terminal<'t> -> string) (tokens: Terminal<'t> list) (position: int) : string =
        if List.isEmpty tokens then
            @"\begin{pNiceMatrix}[margin=2pt] \varepsilon \end{pNiceMatrix}"
        else
            let cells =
                tokens
                |> List.mapi (fun i (Terminal t) ->
                    let s = symbolPrinter (Terminal t) |> escapeMath
                    if i = position then @"\underbar{" + s + "}" else s)
                |> String.concat " & "

            @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"
