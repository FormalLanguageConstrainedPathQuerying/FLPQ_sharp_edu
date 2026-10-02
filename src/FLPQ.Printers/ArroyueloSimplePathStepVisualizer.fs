namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.Languages
open FLPQ.RPQ
open FLPQ.RPQ.ArroyueloRPQ
open ArroyueloStepCommon

/// Step-by-step visualization of Arroyuelo's RPQ evaluation over the path semiring (the
/// simple-path semantics). Each emitted step is one intermediate regexp tree node
/// (Alt/Seq/Star); leaves are not emitted as steps, but they do appear in the tree figure.
/// The tree is vertical; the matrices are path semiring matrices; the graph highlights the
/// vertices and edges of the step's result paths.
module ArroyueloSimplePathStepVisualizer =

    /// The step's matrix equation as one horizontal line: Alt/Seq show the operand matrices
    /// joined by \oplus / \times and =; Star shows TC( child ) = result. Body matrices (no
    /// adjustbox) are composed into one math expression, wrapped in a single adjustbox.
    let private renderMatrices
        (terminal: 't -> string)
        (nonterm: 'nt -> string)
        (step: ArroyueloTraceStep<'t, 'nt>)
        : string =
        let body (m: Matrix<Set<int list>>) =
            PathSemiringTeX.matrixToTeXBody (fun i -> sprintf "v_%d" i) (fun j -> sprintf "v_%d" j) m

        let result = body step.Result

        let expression =
            match step.Operation with
            | Base -> sprintf @"$%s = %s$" (RegexpTeX.toTeX terminal nonterm step.Expr) result
            | Alt -> sprintf @"$%s \oplus %s = %s$" (body step.Operands.[0]) (body step.Operands.[1]) result
            | Seq -> sprintf @"$%s \times %s = %s$" (body step.Operands.[0]) (body step.Operands.[1]) result
            | Star -> sprintf @"$\mathrm{TC}(%s) = %s$" (body step.Operands.[0]) result

        StepTeX.wrapFormula expression

    /// Render every intermediate trace step to its visualization artifacts (both DOT and
    /// TikZ; the runner writes only one format per step).
    let renderSteps
        (terminalPrinter: 't -> string)
        (nontermPrinter: 'nt -> string)
        (graph: NFA<'t, int>)
        (steps: ArroyueloTraceStep<'t, 'nt> list)
        : ArroyueloVisualizationStep list =
        let nodes =
            steps
            |> List.map (fun (step: ArroyueloTraceStep<'t, 'nt>) -> treeNode step.NodeIndex step.Expr step.Children)

        let buildGraph (step: ArroyueloTraceStep<'t, 'nt>) =
            let pathVerts, pathEdgeHl = RpqGraphViz.pathHighlights step.Result

            // Arroyuelo step graphs keep the pre-285 look: no green sources, no frontier
            // tier (only the shared root Input Graph carries the green source fill).
            RpqGraphViz.renderGraph terminalPrinter graph pathVerts Set.empty Set.empty pathEdgeHl Set.empty

        ArroyueloStepCommon.renderSteps
            terminalPrinter
            nontermPrinter
            nodes
            (fun (step: ArroyueloTraceStep<'t, 'nt>) -> step.Operation = Base)
            (fun (step: ArroyueloTraceStep<'t, 'nt>) -> step.NodeIndex)
            (renderMatrices terminalPrinter nontermPrinter)
            buildGraph
            steps
