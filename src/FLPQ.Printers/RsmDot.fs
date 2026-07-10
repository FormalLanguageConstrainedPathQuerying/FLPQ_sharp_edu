namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Graphviz DOT visualization for Recursive State Machines (RSM).
/// Renders all blocks as subgraph clusters with DFA state nodes and transitions.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex).
module RsmDot =

    /// Renders a single RSM block as a DOT subgraph cluster.
    let private blockToSubgraph
        (terminalPrinter: 't -> string)
        (nonterminalPrinter: 'nt -> string)
        (blockIdx: int)
        (block: RsmBlock<'t, 'nt>)
        (isStartBlock: bool)
        (sb: StringBuilder)
        : unit =
        let dfa = block.Dfa
        let localSize = dfa.States.Length
        let prefix = sprintf "b%d" blockIdx

        let (Nonterminal ntName) = block.Nonterminal

        let label =
            if isStartBlock then
                sprintf "%s (start)" (nonterminalPrinter ntName)
            else
                nonterminalPrinter ntName

        sb.AppendLine(sprintf "  subgraph cluster_%d {" blockIdx) |> ignore

        sb.AppendLine(sprintf "    label=\"%s\";" (DerivationTreeDot.escapeLabel label))
        |> ignore

        sb.AppendLine("    style=dashed;") |> ignore

        for localState in 0 .. localSize - 1 do
            let nodeId = sprintf "%s_s%d" prefix localState
            let start = dfa.StartState = localState
            let final = Set.contains localState dfa.FinalStates

            let attrs =
                let mutable parts = [ sprintf "label=\"%d\"" localState ]

                if start then
                    parts <- "style=filled" :: "fillcolor=green" :: parts

                if final then
                    parts <- "peripheries=2" :: parts

                String.concat ", " parts

            sb.AppendLine(sprintf "    %s [%s];" nodeId attrs) |> ignore

        for i in 0 .. localSize - 1 do
            for j in 0 .. localSize - 1 do
                match Matrix.get dfa.Transitions i j with
                | Some symbols ->
                    for label in NonEmptySet.toSeq symbols do
                        let edgeLabel =
                            match label with
                            | AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal t)) -> terminalPrinter t
                            | AutomatonLabel.ATerm(RsmSymbol.RNonterm(Nonterminal nt)) ->
                                sprintf "call %s" (nonterminalPrinter nt)
                            | AutomatonLabel.AEpsilon -> "ε"

                        let style =
                            match label with
                            | AutomatonLabel.AEpsilon -> ", style=dotted"
                            | _ -> ""

                        let src = sprintf "%s_s%d" prefix i
                        let dst = sprintf "%s_s%d" prefix j

                        sb.AppendLine(
                            sprintf
                                "    %s -> %s [label=\"%s\"%s];"
                                src
                                dst
                                (DerivationTreeDot.escapeLabel edgeLabel)
                                style
                        )
                        |> ignore
                | None -> ()

        sb.AppendLine("  }") |> ignore

    /// Renders an RSM as a Graphviz DOT digraph using subgraph clusters for each block.
    /// The start block is marked with "(start)" in its label.
    let toDot (terminalPrinter: 't -> string) (nonterminalPrinter: 'nt -> string) (rsm: RSM<'t, 'nt>) : string =
        let sb = StringBuilder()

        sb.AppendLine("digraph RSM {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore
        sb.AppendLine("  compound=true;") |> ignore

        rsm.Blocks
        |> List.iteri (fun idx block ->
            let isStartBlock = block.Nonterminal = rsm.StartBlock
            blockToSubgraph terminalPrinter nonterminalPrinter idx block isStartBlock sb)

        sb.AppendLine("}") |> ignore
        sb.ToString()
