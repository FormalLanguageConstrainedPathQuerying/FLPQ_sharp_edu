namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TikZ visualization for Recursive State Machines (RSM).
/// Renders all blocks in a single flat graph with global state numbering.
/// Blocks are the graph's connected components (no inter-block edges exist):
/// pgf-gd component packing (`components go down left aligned`) stacks them
/// top-to-bottom with left edges aligned, while the dot layout inside each
/// block grows left-to-right. The start block S' is declared first, so it is
/// packed on top; remaining blocks follow in global-state appearance order.
/// State content is the bare global index; each block's start state carries a
/// plain nonterminal label to its left (the block's identity).
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module RsmTikz =

    /// The rendered label of one RSM transition symbol: the printer-produced names escaped,
    /// the epsilon as math-mode TeX (a bare \varepsilon in text mode renders as an empty box).
    let private edgeLabelOf
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (symbol: AutomatonLabel<RsmSymbol<'t, 'nt>>)
        : string =
        match symbol with
        | AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal t)) -> AutomatonTikz.escapeLatex (terminalPrinter t)
        | AutomatonLabel.ATerm(RsmSymbol.RNonterm(Nonterminal nt)) -> AutomatonTikz.escapeLatex (nonterminalPrinter nt)
        | AutomatonLabel.AEpsilon -> "$\\varepsilon$"

    /// The style fragment of one RSM transition symbol (epsilon edges are dotted).
    let private edgeStyleOf (symbol: AutomatonLabel<RsmSymbol<'t, 'nt>>) : string =
        match symbol with
        | AutomatonLabel.AEpsilon -> "dotted"
        | _ -> ""

    /// Shared body: render a flat RSM graph (global state numbering) for the given block order
    /// and optional highlighted state. State content is the bare global index; nonterminal edges
    /// are labeled with the bare nonterminal name (no "call" prefix). Blocks are stacked
    /// top-to-bottom in `blockOrder`; intra-block layout is layered, left-to-right. Every final
    /// state gets `double, double distance=1.5pt` (a highlighted final state keeps the double
    /// circle); block start states get a plain nonterminal label to the left and
    /// `label=above:Start`. Fill priority: highlighted state fill=lightblue!20, else block start
    /// fill=green!30, else final state fill=red!30.
    let private renderRsm
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (rsm: RSM<'t, 'nt>)
        (blockOrder: Nonterminal<'nt> list)
        (highlightedState: int option)
        : string =
        let sb = StringBuilder()

        let stateInfo = rsm.StateInfo
        let stateCount = rsm.StateCount

        // The router's label channel is keyed by tail->head, so only edges with a unique
        // key can be registered: the single-symbol cells. Multi-symbol cells render
        // parallel edges that would collide (the edge callback cannot tell them apart),
        // so they keep the stock quote.
        let labeledEdges =
            [ for i in 0 .. stateCount - 1 do
                  for j in 0 .. stateCount - 1 do
                      match rsm.Transitions.[i, j] with
                      | Some symbols when i <> j ->
                          match NonEmptySet.toSeq symbols |> List.ofSeq with
                          | [ symbol ] ->
                              let label = edgeLabelOf terminalPrinter nonterminalPrinter symbol

                              if label <> "" then
                                  [ (FlpqdotTikz.edgeKey (sprintf "s%d" i) (sprintf "s%d" j), label) ]
                              else
                                  []
                          | _ -> []
                      | _ -> [] ]
            |> List.collect id

        // Blocks are the graph's connected components; pgf-gd packs them top-to-bottom
        // (PGF manual §28.7) while the dot layout inside each block grows left-to-right.
        AutomatonTikz.tikzHeaderWithOptions
            (FlpqdotTikz.prePass labeledEdges)
            (sprintf
                "%s, components go down left aligned, component sep=1.5cm"
                (AutomatonTikz.graphOptions "circle" AutomatonTikz.defaultGrowDirection))
            sb

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
                let isStartState = globalIdx = rsm.BlockStart.[nt]
                let isFinal = info.IsFinal
                let isHighlighted = highlightedState |> Option.exists (fun hs -> hs = globalIdx)

                let (Nonterminal ntName) = nt
                let ntLabel = nonterminalPrinter ntName
                let ntEscaped = AutomatonTikz.escapeLatex ntLabel

                // State content is the bare global index; block identity comes from
                // the start state's left label and the edge labels.
                let label = string globalIdx

                let above =
                    if isStartState && not isHighlighted then
                        ", label=above:Start"
                    else
                        ""

                let left =
                    if isStartState then
                        sprintf ", label=left:%s" ntEscaped
                    else
                        ""

                let double = if isFinal then ", double, double distance=1.5pt" else ""

                let fill =
                    if isHighlighted then ", fill=lightblue!20"
                    elif isStartState then ", fill=green!30"
                    elif isFinal then ", fill=red!30"
                    else ""

                sb.AppendLine(sprintf "    s%d [as={%s}%s%s%s%s];" globalIdx label above left double fill)
                |> ignore

        for i in 0 .. stateCount - 1 do
            for j in 0 .. stateCount - 1 do
                match rsm.Transitions.[i, j] with
                | Some symbols ->
                    let symbolsList = NonEmptySet.toSeq symbols |> List.ofSeq

                    // A single-symbol cell has a unique edge key: registered in the
                    // measurement pre-pass, its stock quote dropped. Multi-symbol cells
                    // (parallel edges) and loops keep the stock quote.
                    let unique = List.length symbolsList = 1

                    for symbol in symbolsList do
                        let edgeLabel = edgeLabelOf terminalPrinter nonterminalPrinter symbol
                        let style = edgeStyleOf symbol

                        if unique && i <> j && edgeLabel <> "" then
                            FlpqdotTikz.emitEdge sb (sprintf "s%d" i) (sprintf "s%d" j) edgeLabel style
                        else
                            let loopAttr = if i = j then ",loop above" else ""
                            let styleAttr = if style <> "" then ", " + style else ""

                            sb.AppendLine(sprintf "    s%d ->[\"%s\"%s%s] s%d;" i edgeLabel styleAttr loopAttr j)
                            |> ignore
                | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()

    /// Renders an extended RSM as a TikZ tikzpicture using global state numbering: the fresh
    /// start block S' is declared first (packed on top), the remaining blocks follow in
    /// global-state appearance order.
    let extendedRsmToTikz
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (ersm: ExtendedRSM<'t, 'nt>)
        (highlightedState: int option)
        : string =
        let rsm = ersm.ExtendedRsm
        let freshStart = ersm.FreshStart

        let blockOrder =
            [ freshStart ]
            @ (RSM.nonterminals rsm |> List.filter (fun nt -> nt <> freshStart))

        renderRsm terminalPrinter nonterminalPrinter rsm blockOrder highlightedState

    /// Renders a plain RSM as a TikZ tikzpicture using global state numbering: blocks in
    /// global-state appearance order, no highlighted state.
    let rsmToTikz (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (rsm: RSM<'t, 'nt>) : string =
        renderRsm terminalPrinter nonterminalPrinter rsm (RSM.nonterminals rsm) None
