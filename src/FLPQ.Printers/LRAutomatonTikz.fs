namespace FLPQ.Printers

open System.Text
open FLPQ.Languages

/// Tikz visualization for LR automata using graphdrawing with layered layout.
/// Special style: rectangle nodes, aligned LR items with state numbers.
module LRAutomatonTikz =

    let private renderRhsWithDot (item: LR0Item<'t, 'nt>) : string =
        let beforeDot = item.rhs |> List.take item.dot |> List.map SymbolTeX.toLaTeX
        let afterDot = item.rhs |> List.skip item.dot |> List.map SymbolTeX.toLaTeX
        let lhs = SymbolTeX.nonterminalContent item.lhs
        let rhsParts = (beforeDot @ [ @"\cdot" ] @ afterDot) |> String.concat "\\ "
        sprintf "%s &\\to %s" lhs rhsParts

    let private renderLR0StateContent (stateIdx: int) (items: Set<LR0Item<'t, 'nt>>) : string =
        let sb = StringBuilder()
        sb.AppendLine(sprintf "\\text{State %d}\\\\" stateIdx) |> ignore

        for item in Set.toSeq items do
            sb.AppendLine(sprintf "%s \\\\" (renderRhsWithDot item)) |> ignore

        sb.ToString().TrimEnd('\n')

    let private renderLR1StateContent (stateIdx: int) (items: Set<LR1Item<'t, 'nt>>) : string =
        let sb = StringBuilder()
        sb.AppendLine(sprintf "\\text{State %d}\\\\" stateIdx) |> ignore

        for item in Set.toSeq items do
            let baseLine =
                renderRhsWithDot
                    { lhs = item.lhs
                      rhs = item.rhs
                      dot = item.dot }

            let lookahead = SymbolTeX.toLaTeX item.lookahead
            sb.AppendLine(sprintf "%s,\\ %s \\\\" baseLine lookahead) |> ignore

        sb.ToString().TrimEnd('\n')

    let private stateContentToTikzAs (content: string) : string =
        sprintf "$\\begin{aligned}\n%s\n\\end{aligned}$" content

    /// Render an LR(0) automaton as a Tikz tikzpicture.
    /// States are rectangles with aligned LR items and state numbers.
    let lr0AutomatontoTikz (aug: Grammar<'t, 'nt>) (dfa: DFA<Symbol<'t, 'nt>, Set<LR0Item<'t, 'nt>>>) : string =
        let stateVisualizer (stateIdx: int) (items: Set<LR0Item<'t, 'nt>>) =
            stateContentToTikzAs (renderLR0StateContent stateIdx items)

        AutomatonTikz.dfaToTikz SymbolTeX.toLaTeX stateVisualizer "rectangle" dfa

    /// Render an LR(1) automaton as a Tikz tikzpicture.
    let lr1AutomatontoTikz (aug: Grammar<'t, 'nt>) (dfa: DFA<Symbol<'t, 'nt>, Set<LR1Item<'t, 'nt>>>) : string =
        let stateVisualizer (stateIdx: int) (items: Set<LR1Item<'t, 'nt>>) =
            stateContentToTikzAs (renderLR1StateContent stateIdx items)

        AutomatonTikz.dfaToTikz SymbolTeX.toLaTeX stateVisualizer "rectangle" dfa
