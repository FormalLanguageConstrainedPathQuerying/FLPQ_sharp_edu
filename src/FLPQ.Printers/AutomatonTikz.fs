namespace FLPQ.Printers

open System.Text
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages

/// Tikz visualization for finite automata using graphdrawing with dot layout.
module AutomatonTikz =

    /// Escapes the LaTeX special characters of a plain-text label for use in node content.
    let escapeLatex (s: string) : string =
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

    /// The TikZ node options for one automaton state: the `as={...}` content, the Start label,
    /// the final-state double border, and the fills in ascending precedence (start, final,
    /// current/frontier, target) so the last-filled target wins. A final state always keeps its
    /// double ring.
    let nodeOptions
        (idx: int)
        (stateContent: string)
        (isStart: bool)
        (isFinal: bool)
        (isTarget: bool)
        (isHighlighted: bool)
        (shape: string)
        : string =
        let parts = ResizeArray<string>()

        parts.Add(sprintf "as={%s}" stateContent)

        // Last fill wins in TikZ. Fill precedence (highest first): target > current/frontier
        // > final > start, so the fills are appended in the reverse order. The Start label and
        // the final state's double ring are appended first and always kept.
        if isStart then
            parts.Add("label=above:Start")

        if isFinal then
            parts.Add("double")
            parts.Add("double distance=1.5pt")

        if isStart then
            parts.Add("fill=green!30")

        if isFinal then
            parts.Add("fill=red!30")

        if isHighlighted then
            parts.Add("fill=lightblue!20")

        if isTarget then
            parts.Add("fill=yellow!20")

        String.concat ", " parts

    let private stateDeclarations
        (stateCount: int)
        (stateVisualizer: int -> 's -> string)
        (states: 's list)
        (startStates: Set<int>)
        (finalStates: Set<int>)
        (highlightedStates: Set<int>)
        (targetStates: Set<int>)
        (shape: string)
        (sb: StringBuilder)
        : unit =
        for idx in 0 .. stateCount - 1 do
            let state = states.[idx]
            let content = stateVisualizer idx state
            let isStart = Set.contains idx startStates
            let isFinal = Set.contains idx finalStates
            let isTarget = Set.contains idx targetStates
            let isHighlighted = Set.contains idx highlightedStates
            let opts = nodeOptions idx content isStart isFinal isTarget isHighlighted shape
            sb.AppendLine(sprintf "    s%d [%s];" idx opts) |> ignore

    /// The concatenated terminal label of a transition cell: `None` when the cell has no
    /// terminal labels (no solid edge), `Some label` otherwise (the label may be empty when
    /// the printer maps every terminal to "").
    let private cellTerminalLabel
        (labelPrinter: 't -> string)
        (symbols: NonEmptySet<AutomatonLabel<'t>>)
        : string option =
        let termLabels =
            symbols
            |> NonEmptySet.toSeq
            |> Seq.choose (fun l ->
                match l with
                | ATerm t -> Some(labelPrinter t)
                | AEpsilon -> None)
            |> List.ofSeq

        if List.isEmpty termLabels then
            None
        else
            Some(termLabels |> String.concat ", " |> escapeLatex)

    /// True when the cell carries at least one terminal label (a solid edge is rendered).
    let private hasTerminals (symbols: NonEmptySet<AutomatonLabel<'t>>) : bool =
        NonEmptySet.toSeq symbols
        |> Seq.exists (function
            | ATerm _ -> true
            | AEpsilon -> false)

    /// True when the cell carries an epsilon transition (a dotted edge is rendered).
    let private hasEpsilon (symbols: NonEmptySet<AutomatonLabel<'t>>) : bool = NonEmptySet.contains AEpsilon symbols

    /// The (edge key, label) pairs of the non-loop transition edges that are their pair's
    /// only edge (no parallel epsilon edge), in row-major order: the measurement pre-pass
    /// registrations.
    let private labeledTransitionEdges
        (labelPrinter: 't -> string)
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>)
        : (string * string) list =
        [ for i in 0 .. Matrix.rows transitions - 1 do
              for j in 0 .. Matrix.cols transitions - 1 do
                  match transitions.[i, j] with
                  | Some symbols when i <> j && hasTerminals symbols && not (hasEpsilon symbols) ->
                      match cellTerminalLabel labelPrinter symbols with
                      | Some label when label <> "" ->
                          [ (FlpqdotTikz.edgeKey (sprintf "s%d" i) (sprintf "s%d" j), label) ]
                      | _ -> []
                  | _ -> [] ]
        |> List.collect id

    /// The (edge key, label) pairs of the non-loop epsilon edges that are their pair's only
    /// edge (no parallel terminal edge), in row-major order: the measurement pre-pass
    /// registrations (the epsilon label is math-mode TeX).
    let private labeledEpsEdges
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>)
        : (string * string) list =
        [ for i in 0 .. Matrix.rows transitions - 1 do
              for j in 0 .. Matrix.cols transitions - 1 do
                  match transitions.[i, j] with
                  | Some symbols when i <> j && hasEpsilon symbols && not (hasTerminals symbols) ->
                      [ (FlpqdotTikz.edgeKey (sprintf "s%d" i) (sprintf "s%d" j), "$\\varepsilon$") ]
                  | _ -> [] ]
        |> List.collect id

    let private transitionEdges
        (labelPrinter: 't -> string)
        (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>)
        (highlightedEdges: Set<int * int>)
        (sb: StringBuilder)
        : unit =
        for i in 0 .. Matrix.rows transitions - 1 do
            for j in 0 .. Matrix.cols transitions - 1 do
                match transitions.[i, j] with
                | Some symbols ->
                    match cellTerminalLabel labelPrinter symbols with
                    | Some label ->
                        // Highlighted edges render red and bold (same style as GssTikz's
                        // highlighted tier). Epsilon-only cells render no edge here, so
                        // they can never be highlighted.
                        let style =
                            if Set.contains (i, j) highlightedEdges then
                                "red, thick"
                            else
                                ""

                        // A pair with a parallel epsilon edge keeps the stock quote (the
                        // router's label channel is keyed by tail->head and cannot tell
                        // the two edges apart).
                        if hasEpsilon symbols then
                            let loopAttr = if i = j then ",loop above" else ""
                            let hlAttr = if style <> "" then ", " + style else ""

                            sb.AppendLine(sprintf "    s%d ->[\"%s\"%s%s] s%d;" i label hlAttr loopAttr j)
                            |> ignore
                        else
                            FlpqdotTikz.emitEdge sb (sprintf "s%d" i) (sprintf "s%d" j) label style
                    | None -> ()
                | None -> ()

    let private epsEdges (transitions: Matrix<Option<NonEmptySet<AutomatonLabel<'t>>>>) (sb: StringBuilder) : unit =
        for i in 0 .. Matrix.rows transitions - 1 do
            for j in 0 .. Matrix.cols transitions - 1 do
                match transitions.[i, j] with
                | Some symbols when hasEpsilon symbols ->
                    // Math-mode label: a bare \varepsilon in text mode renders as an empty
                    // box (math symbols require math mode), silently losing the label.
                    // A pair with a parallel terminal edge keeps the stock quote (the
                    // router's label channel is keyed by tail->head and cannot tell the
                    // two edges apart).
                    if hasTerminals symbols then
                        let loopAttr = if i = j then ",loop above" else ""

                        sb.AppendLine(sprintf "    s%d ->[dotted, \"$\\varepsilon$\"%s] s%d;" i loopAttr j)
                        |> ignore
                    else
                        FlpqdotTikz.emitEdge sb (sprintf "s%d" i) (sprintf "s%d" j) "$\\varepsilon$" "dotted"
                | _ -> ()

    /// Default grow direction for a left-to-right layered drawing (sources on the left).
    let defaultGrowDirection = "grow'=right"

    /// Grow direction that keeps input position 0 on the right when same-layer constraints are
    /// present. pgf chains same-layer clusters in declaration order with minimum_levels=1, which
    /// reverses the orientation under the default grow direction; `grow=left` restores it.
    let gssLayeredGrowDirection = "grow=left"

    /// Common options for a graph drawing with the given grow direction. Uses the project's
    /// `dot layout` key (the flpqdot library): the stock layered pipeline plus a channel edge
    /// router that smooths edges and separates reciprocal pairs into distinct channels.
    let graphOptions (shape: string) (growDirection: string) : string =
        sprintf "dot layout, nodes={draw, %s}, %s, level sep=2cm, sibling sep=1.5cm" shape growDirection

    /// Opens a tikzpicture with a single \graph using the given graph options.
    /// `prePass` (the flpqdot measurement pre-pass) is emitted between
    /// \begin{tikzpicture} and \graph; pass "" when there is none.
    let tikzHeaderWithOptions (prePass: string) (graphOptions: string) (sb: StringBuilder) : unit =
        sb.AppendLine(@"\begin{tikzpicture}") |> ignore

        if prePass <> "" then
            prePass.Split('\n') |> Array.iter (fun line -> sb.AppendLine(line) |> ignore)

        sb.AppendLine(sprintf @"  \graph [%s] {" graphOptions) |> ignore

    /// Opens a tikzpicture with a single left-to-right layered graph of the given node shape.
    let tikzHeader (prePass: string) (shape: string) (sb: StringBuilder) : unit =
        tikzHeaderWithOptions prePass (graphOptions shape defaultGrowDirection) sb

    /// Closes the graph environment and the tikzpicture opened by `tikzHeader`.
    let tikzFooter (sb: StringBuilder) : unit =
        sb.AppendLine("  };") |> ignore
        sb.AppendLine(@"\end{tikzpicture}") |> ignore

    /// Render an NFA as a Tikz tikzpicture using dot layout.
    /// The returned string contains only the \begin{tikzpicture}...\end{tikzpicture} block.
    /// Every non-loop edge that is its pair's only edge (no parallel terminal/epsilon
    /// edge) is registered in the measurement pre-pass and rendered without a stock quote
    /// (the router places the label); all other edges keep their stock quote.
    let nfaToTikz
        (labelPrinter: 't -> string)
        (stateVisualizer: int -> 's -> string)
        (shape: string)
        (nfa: NFA<'t, 's>)
        : string =
        let sb = StringBuilder()

        let prePass =
            FlpqdotTikz.prePass (
                labeledTransitionEdges labelPrinter nfa.Transitions
                @ labeledEpsEdges nfa.Transitions
            )

        tikzHeader prePass shape sb

        stateDeclarations
            nfa.States.Length
            stateVisualizer
            nfa.States
            nfa.StartStates
            nfa.FinalStates
            Set.empty
            Set.empty
            shape
            sb

        transitionEdges labelPrinter nfa.Transitions Set.empty sb
        epsEdges nfa.Transitions sb

        tikzFooter sb
        sb.ToString()

    /// Render a DFA as a Tikz tikzpicture using dot layout, with the given states highlighted
    /// (fill=lightblue!20, the current/frontier tier), the given target states filled lightyellow
    /// (highest precedence: over current, final, and start), and the given transitions rendered
    /// red and bold (`red, thick`, label and loop attributes preserved). A final state's double
    /// ring is always kept. Every non-loop edge with a non-empty label is registered in the
    /// measurement pre-pass and rendered without a stock quote (the router places the label);
    /// loop edges keep their stock quote. A DFA has no epsilon transitions, so the transition
    /// edges are the only pre-pass registrations.
    let dfaToTikzWithHighlights
        (labelPrinter: 't -> string)
        (stateVisualizer: int -> 's -> string)
        (shape: string)
        (dfa: DFA<'t, 's>)
        (highlightedStates: Set<int>)
        (targetStates: Set<int>)
        (highlightedEdges: Set<int * int>)
        : string =
        let sb = StringBuilder()

        let prePass =
            FlpqdotTikz.prePass (labeledTransitionEdges labelPrinter dfa.Transitions)

        tikzHeader prePass shape sb

        stateDeclarations
            dfa.States.Length
            stateVisualizer
            dfa.States
            (set [ dfa.StartState ])
            dfa.FinalStates
            highlightedStates
            targetStates
            shape
            sb

        transitionEdges labelPrinter dfa.Transitions highlightedEdges sb

        tikzFooter sb
        sb.ToString()

    /// Render a DFA as a Tikz tikzpicture using dot layout.
    let dfaToTikz
        (labelPrinter: 't -> string)
        (stateVisualizer: int -> 's -> string)
        (shape: string)
        (dfa: DFA<'t, 's>)
        : string =
        dfaToTikzWithHighlights labelPrinter stateVisualizer shape dfa Set.empty Set.empty Set.empty
