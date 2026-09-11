namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TikZ visualization for Recursive State Machines (RSM).
/// Renders all blocks in a single flat graph with global state numbering.
/// Blocks are the graph's connected components (no inter-block edges exist):
/// pgf-gd component packing (`components go down left aligned`) stacks them
/// top-to-bottom with left edges aligned, while the layered layout inside each
/// block grows left-to-right. The start block S' is declared first, so it is
/// packed on top; remaining blocks follow in global-state appearance order.
/// Each block's start state carries a plain nonterminal label to its left.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module RsmTikz =

    /// Renders an extended RSM as a TikZ tikzpicture using global state numbering.
    /// State labels use the format "NtName_globalIdx".
    /// Blocks are stacked top-to-bottom (S' on top); intra-block layout is layered, left-to-right.
    /// Block start states get fill=green!30 and a plain nonterminal label to the left;
    /// final states get double + fill=red!30.
    /// If highlightedState is specified, that state gets fill=lightblue!20.
    let extendedRsmToTikz
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (ersm: ExtendedRSM<'t, 'nt>)
        (highlightedState: int option)
        : string =
        let rsm = ersm.ExtendedRsm
        let freshStart = ersm.FreshStart

        let sb = StringBuilder()

        // Blocks are the graph's connected components; pgf-gd packs them top-to-bottom
        // (PGF manual §28.7) while the layered layout inside each block grows left-to-right.
        AutomatonTikz.tikzHeaderWithOptions
            (sprintf
                "%s, components go down left aligned, component sep=1.5cm"
                (AutomatonTikz.layeredGraphOptions "circle"))
            sb

        let stateInfo = rsm.StateInfo
        let stateCount = rsm.StateCount

        // Block declaration order drives pgf-gd `component order`
        // (default: by first specified node): S' block on top,
        // remaining blocks in global-state appearance order.
        let blockOrder =
            [ freshStart ]
            @ (RSM.nonterminals rsm |> List.filter (fun nt -> nt <> freshStart))

        // Global state indices of a block: start state first, then the rest in local-index order.
        let blockStates (nt: Nonterminal<'nt>) : int list =
            let startGlobal = rsm.BlockStart.[nt]

            let rest =
                [ for g in 0 .. stateCount - 1 do
                      if stateInfo.[g].BlockNonterminal = nt && g <> startGlobal then
                          g ]

            [ startGlobal ] @ rest

        for nt in blockOrder do
            for globalIdx in blockStates nt do
                let info = stateInfo.[globalIdx]
                let isFreshStart = info.BlockNonterminal = freshStart
                let isStartState = globalIdx = rsm.BlockStart.[nt]
                let isFinal = info.IsFinal
                let isHighlighted = highlightedState |> Option.exists (fun hs -> hs = globalIdx)

                let (Nonterminal ntName) = nt
                let ntLabel = nonterminalPrinter ntName
                let ntEscaped = AutomatonTikz.escapeLatex ntLabel

                let label =
                    if isFreshStart then
                        sprintf "%s'\\_%d" ntEscaped globalIdx
                    else
                        sprintf "%s\\_%d" ntEscaped globalIdx

                let opts =
                    if isHighlighted && isStartState then
                        sprintf "as={%s}, label=left:%s, fill=lightblue!20" label ntEscaped
                    elif isHighlighted then
                        sprintf "as={%s}, fill=lightblue!20" label
                    elif isStartState then
                        sprintf "as={%s}, label=above:Start, label=left:%s, fill=green!30" label ntEscaped
                    elif isFinal then
                        sprintf "as={%s}, double, double distance=1.5pt, fill=red!30" label
                    else
                        sprintf "as={%s}" label

                sb.AppendLine(sprintf "    s%d [%s];" globalIdx opts) |> ignore

        for i in 0 .. stateCount - 1 do
            for j in 0 .. stateCount - 1 do
                match rsm.Transitions.[i, j] with
                | Some symbols ->
                    for symbol in NonEmptySet.toSeq symbols do
                        // Escape only the printer-produced names; the epsilon label
                        // is math-mode TeX and must not be escaped.
                        let edgeLabel =
                            match symbol with
                            | AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal t)) ->
                                AutomatonTikz.escapeLatex (terminalPrinter t)
                            | AutomatonLabel.ATerm(RsmSymbol.RNonterm(Nonterminal nt)) ->
                                sprintf "call %s" (AutomatonTikz.escapeLatex (nonterminalPrinter nt))
                            | AutomatonLabel.AEpsilon -> "$\\varepsilon$"

                        let style =
                            match symbol with
                            | AutomatonLabel.AEpsilon -> ", dotted"
                            | _ -> ""

                        let loopAttr = if i = j then ",loop above" else ""

                        sb.AppendLine(sprintf "    s%d ->[\"%s\"%s%s] s%d;" i edgeLabel loopAttr style j)
                        |> ignore
                | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()
