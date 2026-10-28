namespace FLPQ.Printers

open System.Text

/// Shared emission helpers for the flpqdot `dot layout` renderers: the
/// `\flpqdotmeasureedgelabel` measurement pre-pass and the edge line that
/// applies the label-adoption rule. A non-loop edge with a non-empty label
/// registers its label in the pre-pass and drops the stock quote, so the
/// router's edge-callback wrapper places the label at a collision-free
/// position; a loop edge keeps its stock quote (the router skips loops).
module FlpqdotTikz =

    /// The syntactic edge key the flpqdot router uses to look up a measured
    /// label: `tail->head` as written in the graph body.
    let edgeKey (tail: string) (head: string) : string = sprintf "%s->%s" tail head

    /// One measurement pre-pass command registering an edge label's size and
    /// text with the flpqdot channel router.
    let measureCommand (key: string) (label: string) : string =
        sprintf @"\flpqdotmeasureedgelabel{%s}{%s}" key label

    /// The measurement pre-pass for a set of labeled edges: one command per
    /// (key, label), newline-joined at the graph-body indentation. `""` when
    /// there are no labeled edges.
    let prePass (labeled: (string * string) list) : string =
        labeled
        |> List.map (fun (key, label) -> sprintf "  %s" (measureCommand key label))
        |> String.concat "\n"

    /// One edge line for a `dot layout` graph body, applying the label-adoption
    /// rule. `style` holds the edge's style options (e.g. `red, thick`); a
    /// non-loop edge with a non-empty label drops the stock quote (its label is
    /// registered in the pre-pass and placed by the router's edge-callback
    /// wrapper), a loop edge keeps it (the router skips loops).
    let emitEdge (sb: StringBuilder) (tail: string) (head: string) (label: string) (style: string) : unit =
        let isLoop = tail = head

        let frags =
            [ if isLoop && label <> "" then
                  sprintf "\"%s\"" label
              if style <> "" then
                  style
              if isLoop then
                  "loop above" ]

        let opts =
            if List.isEmpty frags then
                ""
            else
                sprintf "[%s]" (String.concat ", " frags)

        sb.AppendLine(sprintf "    %s ->%s %s;" tail opts head) |> ignore
