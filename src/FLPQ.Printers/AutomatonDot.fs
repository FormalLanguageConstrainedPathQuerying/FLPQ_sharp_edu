namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Graphviz dot visualization for finite automata.
module AutomatonDot =

    let private stateDeclarations
        (stateCount: int)
        (stateVisualizer: int -> 's -> string)
        (states: 's list)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        (highlightedStates: Set<int>)
        (targetStates: Set<int>)
        (sb: System.Text.StringBuilder)
        : unit =
        for idx in 0 .. stateCount - 1 do
            let state = states.[idx]
            let label = stateVisualizer idx state |> DerivationTreeDot.escapeLabel

            let attrs =
                let start = Set.contains idx startStates
                let final = Set.contains idx finalStates
                let highlighted = Set.contains idx highlightedStates
                let target = Set.contains idx targetStates

                let mutable parts = [ sprintf "label=\"%s\"" label ]

                // Precedence (highest first): target (lightyellow) > current/frontier
                // (lightblue) > start (green); a final state's double border is always kept.
                if target then
                    parts <- "style=filled" :: "fillcolor=lightyellow" :: parts
                elif highlighted then
                    parts <- "style=filled" :: "fillcolor=lightblue" :: parts
                elif start then
                    parts <- "style=filled" :: "fillcolor=green" :: parts

                if final then
                    parts <- "peripheries=2" :: parts

                String.concat ", " parts

            sb.AppendLine(sprintf "  s%d [%s];" idx attrs) |> ignore

    let private transitionEdges
        (labelPrinter: 't -> string)
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>)
        (highlightedEdges: Set<int * int>)
        (sb: System.Text.StringBuilder)
        : unit =
        for i in 0 .. Matrix.rows transitions - 1 do
            for j in 0 .. Matrix.cols transitions - 1 do
                match transitions.[i, j] with
                | Some symbols ->
                    let termLabels =
                        symbols
                        |> NonEmptySet.toSeq
                        |> Seq.choose (fun l ->
                            match l with
                            | ATerm t -> Some(labelPrinter t)
                            | AEpsilon -> None)
                        |> List.ofSeq

                    if not (List.isEmpty termLabels) then
                        let label = termLabels |> String.concat ", " |> DerivationTreeDot.escapeLabel

                        // Highlighted edges render red and bold (same style as GssDot's
                        // highlighted tier); the label is preserved. Epsilon-only cells
                        // render no edge here, so they can never be highlighted.
                        let hlAttr =
                            if Set.contains (i, j) highlightedEdges then
                                ", color=red, penwidth=2.0"
                            else
                                ""

                        sb.AppendLine(sprintf "  s%d -> s%d [label=\"%s\"%s];" i j label hlAttr)
                        |> ignore
                    else
                        ()
                | None -> ()

    let private epsEdges
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>)
        (sb: System.Text.StringBuilder)
        : unit =
        for i in 0 .. Matrix.rows transitions - 1 do
            for j in 0 .. Matrix.cols transitions - 1 do
                match transitions.[i, j] with
                | Some symbols when NonEmptySet.contains AEpsilon symbols ->
                    sb.AppendLine(sprintf "  s%d -> s%d [label=\"ε\", style=dotted];" i j) |> ignore
                | _ -> ()

    /// Render an NFA as a Graphviz dot graph.
    let nfaToDot (labelPrinter: 't -> string) (stateVisualizer: int -> 's -> string) (nfa: NFA<'t, 's>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph Automaton {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        stateDeclarations
            nfa.States.Length
            stateVisualizer
            nfa.States
            nfa.StartStates
            nfa.FinalStates
            Set.empty
            Set.empty
            sb

        transitionEdges labelPrinter nfa.Transitions Set.empty sb
        epsEdges nfa.Transitions sb

        sb.AppendLine("}") |> ignore
        sb.ToString()

    /// Render a DFA as a Graphviz dot graph with the given states highlighted (fillcolor=lightblue,
    /// the current/frontier tier), the given target states filled lightyellow (highest precedence,
    /// over current and start), and the given transitions rendered red and bold
    /// (`color=red, penwidth=2.0`, label preserved). A final state's double border is always kept.
    let dfaToDotWithHighlights
        (labelPrinter: 't -> string)
        (stateVisualizer: int -> 's -> string)
        (dfa: DFA<'t, 's>)
        (highlightedStates: Set<int>)
        (targetStates: Set<int>)
        (highlightedEdges: Set<int * int>)
        : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph Automaton {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        stateDeclarations
            dfa.States.Length
            stateVisualizer
            dfa.States
            (set [ dfa.StartState ])
            dfa.FinalStates
            highlightedStates
            targetStates
            sb

        transitionEdges labelPrinter dfa.Transitions highlightedEdges sb

        sb.AppendLine("}") |> ignore
        sb.ToString()

    /// Render a DFA as a Graphviz dot graph.
    let dfaToDot (labelPrinter: 't -> string) (stateVisualizer: int -> 's -> string) (dfa: DFA<'t, 's>) : string =
        dfaToDotWithHighlights labelPrinter stateVisualizer dfa Set.empty Set.empty Set.empty
