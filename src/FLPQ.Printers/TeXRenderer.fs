namespace FLPQ.Printers

open FLPQ.Languages

/// Shared TeX rendering helpers for parser visualization.
module TeXRenderer =

    /// Create a terminal printer function from a symbol visualizer.
    let termPrinterFromSymbolVisualizer (symbolVisualizer: Symbol<'t, 'nt> -> string) : Terminal<'t> -> string =
        fun (Terminal t) -> symbolVisualizer (T(Terminal t))

    /// Render input tokens with the current position underlined.
    let inputRow (symbolPrinter: Terminal<'t> -> string) (tokens: Terminal<'t> list) (position: int) : string =
        if List.isEmpty tokens then
            @"\begin{pNiceMatrix}[margin=2pt] \varepsilon \end{pNiceMatrix}"
        else
            let cells =
                tokens
                |> List.mapi (fun i (Terminal t) ->
                    let s = symbolPrinter (Terminal t)
                    if i = position then @"\underbar{" + s + "}" else s)
                |> String.concat " & "

            @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"
