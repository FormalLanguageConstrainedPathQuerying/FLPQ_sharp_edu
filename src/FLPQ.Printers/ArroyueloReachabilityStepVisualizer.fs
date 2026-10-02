namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.ArroyueloRPQ
open ArroyueloStepCommon

/// Step-by-step visualization of Arroyuelo's RPQ evaluation over the Boolean semiring (the
/// reachability semantics). Each emitted step is one intermediate regexp tree node
/// (Alt/Seq/Star); leaves are not emitted as steps but appear in the vertical tree figure.
/// Matrices are Boolean (`\bullet`/`\cdot`). No paths or input edges are highlighted:
/// instead, for every true cell (u, v) of the handled node's Boolean result matrix M(E), an
/// edge u -> v labeled with the node's subexpression is added to the graph (red bold); when
/// the pair is also an input edge, the two labels merge into one edge.
module ArroyueloReachabilityStepVisualizer =

    /// The step's Boolean matrix equation as one horizontal line: Alt/Seq show the operand
    /// matrices joined by \vee / \times and =; Star shows TC( child ) = result. Body matrices
    /// (no adjustbox) are composed into one math expression, wrapped in a single adjustbox.
    let private renderMatrices
        (terminal: 't -> string)
        (nonterm: 'nt -> string)
        (step: ArroyueloBooleanTraceStep<'t, 'nt>)
        : string =
        let body (m: Matrix<bool>) =
            PathSemiringTeX.boolMatrixToTeXBody (fun i -> sprintf "v_%d" i) (fun j -> sprintf "v_%d" j) m

        let result = body step.Result

        let expression =
            match step.Operation with
            | Base -> sprintf @"$%s = %s$" (RegexpTeX.toTeX terminal nonterm step.Expr) result
            | Alt -> sprintf @"$%s \vee %s = %s$" (body step.Operands.[0]) (body step.Operands.[1]) result
            | Seq -> sprintf @"$%s \times %s = %s$" (body step.Operands.[0]) (body step.Operands.[1]) result
            | Star -> sprintf @"$\mathrm{TC}(%s) = %s$" (body step.Operands.[0]) result

        StepTeX.wrapFormula expression

    /// Every true cell (u, v) of a Boolean result matrix.
    let private derivedEdges (result: Matrix<bool>) : Set<int * int> =
        Set.ofList
            [ for u in 0 .. Matrix.rows result - 1 do
                  for v in 0 .. Matrix.cols result - 1 do
                      if result.[u, v] then
                          (u, v) ]

    /// Render every intermediate trace step to its visualization artifacts (both DOT and
    /// TikZ; the runner writes only one format per step).
    let renderSteps
        (terminalPrinter: 't -> string)
        (nontermPrinter: 'nt -> string)
        (graph: NFA<'t, int>)
        (steps: ArroyueloBooleanTraceStep<'t, 'nt> list)
        : ArroyueloVisualizationStep list =
        let nodes =
            steps
            |> List.map (fun (step: ArroyueloBooleanTraceStep<'t, 'nt>) ->
                treeNode step.NodeIndex step.Expr step.Children)

        let buildGraph (step: ArroyueloBooleanTraceStep<'t, 'nt>) =
            let derived = derivedEdges step.Result
            let tikzLabel = "$" + RegexpTeX.toTeX terminalPrinter nontermPrinter step.Expr + "$"

            let dotLabel =
                Regexp.toString
                    (fun (Terminal t) -> terminalPrinter t)
                    (fun (Nonterminal nt) -> nontermPrinter nt)
                    step.Expr

            RpqGraphViz.renderGraphWithDerived terminalPrinter graph derived tikzLabel dotLabel graph.StartStates

        ArroyueloStepCommon.renderSteps
            terminalPrinter
            nontermPrinter
            nodes
            (fun (step: ArroyueloBooleanTraceStep<'t, 'nt>) -> step.Operation = Base)
            (fun (step: ArroyueloBooleanTraceStep<'t, 'nt>) -> step.NodeIndex)
            (renderMatrices terminalPrinter nontermPrinter)
            buildGraph
            steps
