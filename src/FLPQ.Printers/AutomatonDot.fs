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
        (sb: System.Text.StringBuilder)
        : unit =
        for idx in 0 .. stateCount - 1 do
            let state = states.[idx]
            let label = stateVisualizer idx state |> fun s -> s.Replace("\"", "\\\"")

            let attrs =
                let start = Set.contains idx startStates
                let final = Set.contains idx finalStates

                let mutable parts = [ sprintf "label=\"%s\"" label ]

                if start then
                    parts <- "style=filled" :: "fillcolor=green" :: parts

                if final then
                    parts <- "peripheries=2" :: parts

                String.concat ", " parts

            sb.AppendLine(sprintf "  s%d [%s];" idx attrs) |> ignore

    let private transitionEdges (transitions: Matrix<Option<NonEmptySet<'t>>>) (sb: System.Text.StringBuilder) : unit =
        for i in 0 .. transitions.rows - 1 do
            for j in 0 .. transitions.cols - 1 do
                match transitions.data.[i, j] with
                | Some symbols ->
                    let label =
                        symbols
                        |> NonEmptySet.toSeq
                        |> Seq.map string
                        |> String.concat ", "
                        |> fun s -> s.Replace("\"", "\\\"")

                    sb.AppendLine(sprintf "  s%d -> s%d [label=\"%s\"];" i j label) |> ignore
                | None -> ()

    let private epsEdges (epsTransitions: Set<int * int>) (sb: System.Text.StringBuilder) : unit =
        for (fromIdx, toIdx) in epsTransitions do
            sb.AppendLine(sprintf "  s%d -> s%d [label=\"ε\", style=dotted];" fromIdx toIdx)
            |> ignore

    /// Render an NFA as a Graphviz dot graph.
    let nfaToDot (stateVisualizer: int -> 's -> string) (nfa: NFA<'t, 's>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph Automaton {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        stateDeclarations nfa.states.Length stateVisualizer nfa.states nfa.startStates nfa.finalStates sb
        transitionEdges nfa.transitions sb
        epsEdges nfa.epsTransitions sb

        sb.AppendLine("}") |> ignore
        sb.ToString()

    /// Render a DFA as a Graphviz dot graph.
    let dfaToDot (stateVisualizer: int -> 's -> string) (dfa: DFA<'t, 's>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph Automaton {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        stateDeclarations dfa.states.Length stateVisualizer dfa.states (set [ dfa.startState ]) dfa.finalStates sb
        transitionEdges dfa.transitions sb

        sb.AppendLine("}") |> ignore
        sb.ToString()
