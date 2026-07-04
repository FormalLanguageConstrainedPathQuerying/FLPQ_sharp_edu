namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Tikz visualization for finite automata using graphdrawing with layered layout.
module AutomatonTikz =

    let private escapeLatex (s: string) : string =
        s
            .Replace(@"\", @"\textbackslash ")
            .Replace("_", @"\_")
            .Replace("{", @"\{")
            .Replace("}", @"\}")
            .Replace("$", @"\$")
            .Replace("%", @"\%")
            .Replace("#", @"\#")
            .Replace("&", @"\&")
            .Replace("^", @"\^")
            .Replace("~", @"\~{}")

    let private nodeOptions (idx: int) (stateContent: string) (isStart: bool) (isFinal: bool) (shape: string) : string =
        let parts = ResizeArray<string>()

        parts.Add(sprintf "as={%s}" stateContent)

        if isStart then
            parts.Add("label=above:Start")
            parts.Add("fill=green!30")

        if isFinal then
            parts.Add("double")
            parts.Add("double distance=1.5pt")
            parts.Add("fill=red!30")

        String.concat ", " parts

    let private stateDeclarations
        (stateCount: int)
        (stateVisualizer: int -> 's -> string)
        (states: 's list)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        (shape: string)
        (sb: StringBuilder)
        : unit =
        for idx in 0 .. stateCount - 1 do
            let state = states.[idx]
            let content = stateVisualizer idx state
            let isStart = Set.contains idx startStates
            let isFinal = Set.contains idx finalStates
            let opts = nodeOptions idx content isStart isFinal shape
            sb.AppendLine(sprintf "    s%d [%s];" idx opts) |> ignore

    let private transitionEdges
        (labelPrinter: 't -> string)
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>)
        (sb: StringBuilder)
        : unit =
        for i in 0 .. transitions.rows - 1 do
            for j in 0 .. transitions.cols - 1 do
                match transitions.data.[i, j] with
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
                        let label = termLabels |> String.concat ", " |> escapeLatex

                        if label = "" then
                            sb.AppendLine(sprintf "    s%d -> s%d;" i j) |> ignore
                        else
                            sb.AppendLine(sprintf "    s%d ->[\"%s\"] s%d;" i label j) |> ignore
                | None -> ()

    let private epsEdges (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>) (sb: StringBuilder) : unit =
        for i in 0 .. transitions.rows - 1 do
            for j in 0 .. transitions.cols - 1 do
                match transitions.data.[i, j] with
                | Some symbols when NonEmptySet.contains AEpsilon symbols ->
                    sb.AppendLine(sprintf "    s%d ->[dotted, \"\\varepsilon\"] s%d;" i j) |> ignore
                | _ -> ()

    let private tikzHeader (shape: string) (sb: StringBuilder) : unit =
        sb.AppendLine(@"\begin{tikzpicture}") |> ignore

        sb.AppendLine(sprintf @"  \graph [layered layout, nodes={draw, %s}, grow'=right] {" shape)
        |> ignore

    let private tikzFooter (sb: StringBuilder) : unit =
        sb.AppendLine("  };") |> ignore
        sb.AppendLine(@"\end{tikzpicture}") |> ignore

    /// Render an NFA as a Tikz tikzpicture using layered layout.
    /// The returned string contains only the \begin{tikzpicture}...\end{tikzpicture} block.
    let nfaToTikz
        (labelPrinter: 't -> string)
        (stateVisualizer: int -> 's -> string)
        (shape: string)
        (nfa: NFA<'t, 's>)
        : string =
        let sb = StringBuilder()

        tikzHeader shape sb

        stateDeclarations nfa.states.Length stateVisualizer nfa.states nfa.startStates nfa.finalStates shape sb
        transitionEdges labelPrinter nfa.transitions sb
        epsEdges nfa.transitions sb

        tikzFooter sb
        sb.ToString()

    /// Render a DFA as a Tikz tikzpicture using layered layout.
    let dfaToTikz
        (labelPrinter: 't -> string)
        (stateVisualizer: int -> 's -> string)
        (shape: string)
        (dfa: DFA<'t, 's>)
        : string =
        let sb = StringBuilder()

        tikzHeader shape sb

        stateDeclarations dfa.states.Length stateVisualizer dfa.states (set [ dfa.startState ]) dfa.finalStates shape sb
        transitionEdges labelPrinter dfa.transitions sb

        tikzFooter sb
        sb.ToString()
