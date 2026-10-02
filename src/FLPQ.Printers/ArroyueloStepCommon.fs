namespace FLPQ.Printers

open FLPQ.Languages

/// Shared record type, regexp-tree model, and tree rendering for the two Arroyuelo RPQ step
/// visualizers: the simple-path visualizer (path semiring) and the reachability visualizer
/// (Boolean). Both return the same `ArroyueloVisualizationStep` list so the CLI writer and
/// the `-s` summary reuse one pipeline.
module ArroyueloStepCommon =

    /// One rendered trace step: the regexp tree figure, the matrix equation for the step's
    /// operation, and the input graph with the step's result — each in DOT and TikZ form
    /// (the runner picks one).
    [<Struct>]
    type ArroyueloVisualizationStep =
        { TreeDot: string
          TreeTikz: string
          Matrices: string
          GraphDot: string
          GraphTikz: string }

    /// The tree view shared by the path and Boolean traces: the post-order node index, the
    /// subexpression, and the post-order indices of the node's children.
    type ArroyueloTreeNode<'t, 'nt when 't: comparison and 'nt: comparison> =
        { Index: int
          Expr: Regexp<'t, 'nt>
          Children: int list }

    /// Build the shared tree view from a trace step's post-order index, subexpression, and
    /// child indices.
    let treeNode (index: int) (expr: Regexp<'t, 'nt>) (children: int list) : ArroyueloTreeNode<'t, 'nt> =
        { Index = index
          Expr = expr
          Children = children }

    /// All tree node indices.
    let private nodeSet (nodes: ArroyueloTreeNode<'t, 'nt> list) : Set<int> =
        nodes |> List.map (fun n -> n.Index) |> Set.ofList

    /// Parent -> child edges of the tree.
    let private edgeSet (nodes: ArroyueloTreeNode<'t, 'nt> list) : Set<int * int> =
        nodes
        |> List.collect (fun n -> n.Children |> List.map (fun c -> (n.Index, c)))
        |> Set.ofList

    /// Node index -> subexpression.
    let private exprByIndex (nodes: ArroyueloTreeNode<'t, 'nt> list) : Map<int, Regexp<'t, 'nt>> =
        nodes |> List.map (fun n -> n.Index, n.Expr) |> Map.ofList

    /// The vertical regexp tree with the current node highlighted, in DOT and TikZ form.
    /// The tree includes every node, including leaves.
    let renderTree
        (terminal: 't -> string)
        (nonterm: 'nt -> string)
        (nodes: ArroyueloTreeNode<'t, 'nt> list)
        (current: int)
        : string * string =
        let nodeIdxs = nodeSet nodes
        let edges = edgeSet nodes
        let exprs = exprByIndex nodes
        let exprAt i = exprs.[i]

        let label i =
            RegexpTeX.toTeX terminal nonterm (exprAt i)

        let labelTikz i =
            "$" + RegexpTeX.toTeX terminal nonterm (exprAt i) + "$"

        let dot = RegexpTreeDot.toDot label nodeIdxs edges current
        let tikz = RegexpTreeTikz.toTikz labelTikz nodeIdxs edges current
        (dot, tikz)

    /// Render the output steps from the full tree and the trace steps. Steps for which
    /// `isBase` holds (regexp leaves) are not emitted; the tree figures still include every
    /// node. `buildMatrices` and `buildGraph` render the per-step matrix equation and graph
    /// (the latter as a dot/tikz pair).
    let renderSteps
        (terminal: 't -> string)
        (nonterm: 'nt -> string)
        (nodes: ArroyueloTreeNode<'t, 'nt> list)
        (isBase: 'step -> bool)
        (nodeIndexOf: 'step -> int)
        (buildMatrices: 'step -> string)
        (buildGraph: 'step -> string * string)
        (steps: 'step list)
        : ArroyueloVisualizationStep list =
        steps
        |> List.filter (isBase >> not)
        |> List.map (fun step ->
            let treeDot, treeTikz = renderTree terminal nonterm nodes (nodeIndexOf step)
            let graphDot, graphTikz = buildGraph step

            { TreeDot = treeDot
              TreeTikz = treeTikz
              Matrices = buildMatrices step
              GraphDot = graphDot
              GraphTikz = graphTikz })
