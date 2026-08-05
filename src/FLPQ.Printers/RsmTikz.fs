namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// TikZ visualization for Recursive State Machines (RSM).
/// Renders all blocks in a single flat graph with global state numbering.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module RsmTikz =

    /// Renders an extended RSM as a TikZ tikzpicture using global state numbering.
    /// State labels use the format "NtName_globalIdx".
    /// Start states get fill=green!30, final states get double + fill=red!30.
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

        AutomatonTikz.tikzHeader "circle" sb

        let stateInfo = rsm.StateInfo
        let stateCount = rsm.StateCount

        for globalIdx in 0 .. stateCount - 1 do
            let info = stateInfo.[globalIdx]
            let (Nonterminal ntName) = info.BlockNonterminal
            let isFreshStart = info.BlockNonterminal = freshStart

            let isStartState =
                rsm.BlockStart.TryGetValue(info.BlockNonterminal)
                |> function
                    | true, gs -> gs = globalIdx
                    | false, _ -> false

            let isFinal = info.IsFinal
            let isHighlighted = highlightedState |> Option.exists (fun hs -> hs = globalIdx)

            let label =
                let ntLabel = nonterminalPrinter ntName

                if isFreshStart then
                    sprintf "%s'\\_%d" (AutomatonTikz.escapeLatex ntLabel) globalIdx
                else
                    sprintf "%s\\_%d" (AutomatonTikz.escapeLatex ntLabel) globalIdx

            let opts =
                if isHighlighted then
                    sprintf "as={%s}, fill=lightblue!20" label
                elif isStartState then
                    sprintf "as={%s}, label=above:Start, fill=green!30" label
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
                        let edgeLabel =
                            match symbol with
                            | AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal t)) -> terminalPrinter t
                            | AutomatonLabel.ATerm(RsmSymbol.RNonterm(Nonterminal nt)) ->
                                sprintf "call %s" (nonterminalPrinter nt)
                            | AutomatonLabel.AEpsilon -> "\\varepsilon"

                        let style =
                            match symbol with
                            | AutomatonLabel.AEpsilon -> ", dotted"
                            | _ -> ""

                        let loopAttr = if i = j then ",loop above" else ""

                        let escaped = AutomatonTikz.escapeLatex edgeLabel

                        sb.AppendLine(sprintf "    s%d ->[\"%s\"%s%s] s%d;" i escaped loopAttr style j)
                        |> ignore
                | None -> ()

        AutomatonTikz.tikzFooter sb
        sb.ToString()
