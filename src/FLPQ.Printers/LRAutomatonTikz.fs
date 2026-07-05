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
            item.rhs
            |> List.take item.dot
            |> List.map (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter)

        let afterDot =
            item.rhs
            |> List.skip item.dot
            |> List.map (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter)

        let lhs = SymbolTeX.nonterminalContent nonterminalPrinter item.lhs
        let rhsParts = (beforeDot @ [ @"\cdot" ] @ afterDot) |> String.concat "\\ "
        sprintf "%s &\\to %s" lhs rhsParts

    let private renderLR0StateContent
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

    let private renderLR1StateContent
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
                    { lhs = item.lhs
                      rhs = item.rhs
                      dot = item.dot }

            let lookahead = SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter item.lookahead
            sb.AppendLine(sprintf "%s,\\ %s \\\\" baseLine lookahead) |> ignore

        sb.ToString().TrimEnd('\n')

    let private stateContentToTikzAs (content: string) : string =
        sprintf "$\\begin{aligned}\n%s\n\\end{aligned}$" content

    /// Render an LR(0) automaton as a Tikz tikzpicture.
    /// States are rectangles with aligned LR items and state numbers.
    let lr0AutomatontoTikz
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (aug: Grammar<'t, 'nt>)
        (dfa: DFA<Symbol<'t, 'nt>, Set<LR0Item<'t, 'nt>>>)
        : string =
        let stateVisualizer (stateIdx: int) (items: Set<LR0Item<'t, 'nt>>) =
            stateContentToTikzAs (renderLR0StateContent terminalPrinter nonterminalPrinter stateIdx items)

        AutomatonTikz.dfaToTikz (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter) stateVisualizer "rectangle" dfa

    /// Render an LR(1) automaton as a Tikz tikzpicture.
    let lr1AutomatontoTikz
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (aug: Grammar<'t, 'nt>)
        (dfa: DFA<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>>)
        : string =
        let stateVisualizer (stateIdx: int) (items: Set<LR1Item<'t, 'nt>>) =
            stateContentToTikzAs (renderLR1StateContent terminalPrinter nonterminalPrinter stateIdx items)

        AutomatonTikz.dfaToTikz (SymbolTeX.toLaTeX terminalPrinter nonterminalPrinter) stateVisualizer "rectangle" dfa
