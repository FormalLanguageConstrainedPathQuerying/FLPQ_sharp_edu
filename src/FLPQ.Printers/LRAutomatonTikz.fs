namespace FLPQ.Printers

open System.Text
open FLPQ.Languages

/// Tikz visualization for LR automata using graphdrawing with layered layout.
/// Special style: rectangle nodes, aligned LR items with state numbers.
module LRAutomatonTikz =

    let private renderRhsWithDot
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (item: LR0Item<'t, 'nt>)
        : string =
        let beforeDot =
            item.Rhs
            |> List.take item.Dot
            |> List.map (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter)

        let afterDot =
            item.Rhs
            |> List.skip item.Dot
            |> List.map (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter)

        let lhs = SymbolTeX.nonterminalContent nonterminalPrinter item.Lhs
        let rhsParts = (beforeDot @ [ @"\cdot" ] @ afterDot) |> String.concat "\\ "
        sprintf "%s &\\to %s" lhs rhsParts

    let renderLR0StateContent
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (stateIdx: int)
        (items: Set<LR0Item<'t, 'nt>>)
        : string =
        let sb = StringBuilder()
        sb.AppendLine(sprintf "\\text{State %d}\\\\" stateIdx) |> ignore

        for item in Set.toSeq items do
            sb.AppendLine(sprintf "%s \\\\" (renderRhsWithDot terminalPrinter nonterminalPrinter item))
            |> ignore

        sb.ToString().TrimEnd('\n')

    let renderLR1StateContent
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (stateIdx: int)
        (items: Set<LR1Item<'t, 'nt>>)
        : string =
        let sb = StringBuilder()
        sb.AppendLine(sprintf "\\text{State %d}\\\\" stateIdx) |> ignore

        for item in Set.toSeq items do
            let baseLine =
                renderRhsWithDot
                    terminalPrinter
                    nonterminalPrinter
                    { Lhs = item.Lhs
                      Rhs = item.Rhs
                      Dot = item.Dot }

            let lookahead = SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter item.Lookahead
            sb.AppendLine(sprintf "%s,\\ %s \\\\" baseLine lookahead) |> ignore

        sb.ToString().TrimEnd('\n')

    let stateContentToTikzAs (content: string) : string =
        sprintf "$\\begin{aligned}\n%s\n\\end{aligned}$" content

    /// Render an LR(0) automaton as a Tikz tikzpicture.
    /// States are rectangles with aligned LR items and state numbers.
    /// Accepts labelPrinter, stateVisualizer, and shape consistent with AutomatonTikz.dfaToTikz.
    let lr0AutomatonToTikz
        (labelPrinter: Symbol<'t, 'nt> -> string)
        (stateVisualizer: int -> Set<LR0Item<'t, 'nt>> -> string)
        (shape: string)
        (dfa: DFA<Symbol<'t, 'nt>, Set<LR0Item<'t, 'nt>>>)
        : string =
        AutomatonTikz.dfaToTikz labelPrinter stateVisualizer shape dfa

    /// Render an LR(1) automaton as a Tikz tikzpicture.
    /// Accepts labelPrinter, stateVisualizer, and shape consistent with AutomatonTikz.dfaToTikz.
    let lr1AutomatonToTikz
        (labelPrinter: Symbol<'t, 'nt> -> string)
        (stateVisualizer: int -> Set<LR1Item<'t, 'nt>> -> string)
        (shape: string)
        (dfa: DFA<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>>)
        : string =
        AutomatonTikz.dfaToTikz labelPrinter stateVisualizer shape dfa
