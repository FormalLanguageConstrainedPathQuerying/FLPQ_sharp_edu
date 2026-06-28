namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Graphviz dot visualization for finite automata.
module AutomatonVisualizer =

    /// Render an automaton as a Graphviz dot graph.
    /// stateVisualizer: int -> state data -> label string for each state.
    /// Start states are filled green, final states have double circles.
    let toDot (stateVisualizer: int -> 's -> string) (aut: Automaton<'t, 's>) : string =
        let sb = System.Text.StringBuilder()

        sb.AppendLine("digraph Automaton {") |> ignore
        sb.AppendLine("  rankdir=LR;") |> ignore

        for idx in 0 .. aut.states.Length - 1 do
            let state = aut.states.[idx]
            let label = stateVisualizer idx state |> fun s -> s.Replace("\"", "\\\"")

            let attrs =
                let start = Set.contains idx aut.startStates
                let final = Set.contains idx aut.finalStates

                let mutable parts = [ sprintf "label=\"%s\"" label ]

                if start then
                    parts <- "style=filled" :: "fillcolor=green" :: parts

                if final then
                    parts <- "peripheries=2" :: parts

                String.concat ", " parts

            sb.AppendLine(sprintf "  s%d [%s];" idx attrs) |> ignore

        for i in 0 .. aut.transitions.rows - 1 do
            for j in 0 .. aut.transitions.cols - 1 do
                let symbols = aut.transitions.data.[i, j]

                if not (Set.isEmpty symbols) then
                    let label =
                        symbols
                        |> Set.toSeq
                        |> Seq.map string
                        |> String.concat ", "
                        |> fun s -> s.Replace("\"", "\\\"")

                    sb.AppendLine(sprintf "  s%d -> s%d [label=\"%s\"];" i j label) |> ignore

        sb.AppendLine("}") |> ignore
        sb.ToString()
