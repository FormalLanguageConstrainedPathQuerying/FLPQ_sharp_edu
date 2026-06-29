namespace FLPQ.Languages

/// Visualization step shared by LL and LR parser visualizers.
[<Struct>]
type VisualizationStep =
    { tree: string
      stack: string
      input: string }

/// Input state for LL/LR parser step visualization.
[<Struct>]
type StepInput<'t, 'nt> =
    { tokens: Symbol<'t, 'nt> list
      position: int }

/// Data for a single LL parser visualization step.
[<Struct>]
type LLParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: Symbol<'t, 'nt> list
      input: StepInput<'t, 'nt> }

/// Frame on the unified LR parser stack.
[<Struct>]
type LRStackFrame<'t, 'nt> =
    | LRState of int
    | LRSymbol of Symbol<'t, 'nt> * DerivationTree<'t, 'nt>

/// Data for a single LR parser visualization step.
[<Struct>]
type LRParsingStep<'t, 'nt> =
    { tree: DerivationTree<'t, 'nt>
      stack: LRStackFrame<'t, 'nt> list
      input: StepInput<'t, 'nt> }

/// Shared TeX rendering helpers for parser visualization.
module TeXRenderer =

    /// Render a list of items as a one-row pNiceMatrix (used for parser stacks).
    let oneRowMatrix (itemPrinter: 'a -> string) (items: 'a list) : string =
        if List.isEmpty items then
            @"\begin{pNiceMatrix}[margin=2pt] \varepsilon \end{pNiceMatrix}"
        else
            let cells = items |> List.map itemPrinter |> String.concat " & "
            @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"

    /// Render input tokens with the current position underlined.
    let inputRow (symbolPrinter: Symbol<'t, 'nt> -> string) (tokens: Symbol<'t, 'nt> list) (position: int) : string =
        if List.isEmpty tokens then
            @"\begin{pNiceMatrix}[margin=2pt] \varepsilon \end{pNiceMatrix}"
        else
            let cells =
                tokens
                |> List.mapi (fun i sym ->
                    let s =
                        match sym with
                        | T(Terminal _) -> symbolPrinter sym
                        | N _ -> symbolPrinter sym
                        | Epsilon -> "\\varepsilon"

                    if i = position then @"\underbar{" + s + "}" else s)
                |> String.concat " & "

            @"\begin{pNiceMatrix}[margin=2pt] " + cells + @" \end{pNiceMatrix}"
