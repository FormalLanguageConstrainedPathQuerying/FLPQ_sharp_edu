namespace FLPQ.Printers

open System.Text

/// TikZ graphdrawing rendering of the RPQ regexp AST tree, used by the Arroyuelo step
/// visualizers. The tree is vertical (root at top, `grow'=down`); nodes are rectangles and
/// the current (handled) node is filled light blue. Dedicated to RPQ — it does not depend on
/// the GSS renderers. Labels are taken as already-rendered TeX (the caller wraps them in `$...$`).
module RegexpTreeTikz =

    /// Render the regexp tree as a TikZ `tikzpicture` with layered layout. `nodes` are the AST
    /// node indices (post-order), `edges` the parent -> child pairs, `current` the highlighted
    /// node.
    let toTikz (label: int -> string) (nodes: Set<int>) (edges: Set<int * int>) (current: int) : string =
        let sb = StringBuilder()

        AutomatonTikz.tikzHeaderWithOptions (AutomatonTikz.layeredGraphOptions "rectangle" "grow'=down") sb

        for vidx in nodes do
            let text = label vidx

            let opts =
                if vidx = current then
                    sprintf "as={%s}, fill=lightblue!20" text
                else
                    sprintf "as={%s}" text

            sb.AppendLine(sprintf "    v%d [%s];" vidx opts) |> ignore

        for fromIdx, toIdx in edges do
            sb.AppendLine(sprintf "    v%d -> v%d;" fromIdx toIdx) |> ignore

        AutomatonTikz.tikzFooter sb
        sb.ToString()
