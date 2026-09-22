namespace FLPQ.Printers

open System.Text
open FLPQ.Languages

/// Tikz visualization for the RNGLR LR automaton — a DFA over RSM symbols whose states hold
/// sets of RnglrItem (a block nonterminal and an RSM state within its block).
/// Special style: rectangle nodes with aligned item lists, mirroring LRAutomatonTikz.
/// Book reference: sec:CFPQ_RNGLR.
module RnglrAutomatonTikz =

    /// Render one RNGLR item as "<nonterminal> : <rsm state>".
    let renderRnglrItem (nonterminalPrinter: 'nt -> string) (item: RnglrItem<'nt>) : string =
        let (Nonterminal nt) = item.BlockNonterminal

        sprintf "%s : %d" (nonterminalPrinter nt) item.RsmState

    /// Aligned state content: a "State N" header line followed by one line per item.
    let renderRnglrStateContent
        (nonterminalPrinter: 'nt -> string)
        (stateIdx: int)
        (items: Set<RnglrItem<'nt>>)
        : string =
        let sb = StringBuilder()
        sb.AppendLine(sprintf "\\text{State %d}\\\\" stateIdx) |> ignore

        for item in Set.toSeq items do
            sb.AppendLine(sprintf "%s \\\\" (renderRnglrItem nonterminalPrinter item))
            |> ignore

        sb.ToString().TrimEnd('\n')

    /// Render the RNGLR LR automaton as a Tikz tikzpicture.
    /// States are rectangles with aligned item lists and state numbers.
    /// Delegates to AutomatonTikz.dfaToTikz with shape "rectangle" (layered layout).
    let rnglrAutomatonToTikz
        (labelPrinter: Symbol<'t, 'nt> -> string)
        (nonterminalPrinter: 'nt -> string)
        (dfa: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>>)
        : string =
        AutomatonTikz.dfaToTikz
            labelPrinter
            (fun idx items ->
                LRAutomatonTikz.stateContentToTikzAs (renderRnglrStateContent nonterminalPrinter idx items))
            "rectangle"
            dfa
