namespace FLPQ.Printers

open FLPQ.CFPQ.AzimovCfpq
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// TeX rendering of Azimov's per-nonterminal CFPQ matrices (book Chapter 12, sec:CFPQ_MatrixBased):
/// each nonterminal A has a |V|x|V| matrix T_A whose cell (i, j) is empty or a semiring element.
module CfpqMatrixTeX =

    /// Cell text: None renders as ∅, Some e as the element's text.
    let cellToTeX (elementPrinter: 'elem -> string) (cell: 'elem option) : string =
        match cell with
        | None -> @"\varnothing"
        | Some e -> elementPrinter e

    /// One nonterminal matrix as a pNiceMatrix with v_i row/column labels, wrapped in adjustbox.
    let private renderMatrix
        (elementPrinter: 'elem -> string)
        (m: Matrix<'elem option>)
        (highlights: Matrix.Highlight list)
        : string =
        let label i = sprintf "v_%d" i

        MatrixTeX.toTeXStyled
            false
            false
            (cellToTeX elementPrinter)
            m
            highlights
            []
            (Some label)
            (Some label)
            false
            true

    /// One nonterminal matrix, wrapped in adjustbox.
    let matrixToTeX (elementPrinter: 'elem -> string) (m: Matrix<'elem option>) : string =
        renderMatrix elementPrinter m []

    /// An edge sequence as text: v_0 -a-> v_1 -b-> v_3; the empty sequence renders as ε.
    let pathToText (labelPrinter: 't -> string) (path: Trans<'t> list) : string =
        match path with
        | [] -> @"\varepsilon"
        | e :: _ ->
            [ sprintf "v_%d" e.From ]
            @ (path |> List.map (fun e -> sprintf @" -%s-> v_%d" (labelPrinter e.Label) e.To))
            |> String.concat ""

    /// All nonterminal matrices of one fixed-point iteration, stacked vertically: a bold
    /// nonterminal name above each matrix, with the cells changed in this iteration highlighted.
    let stepToTeX
        (ntName: Nonterminal<'nt> -> string)
        (elementPrinter: 'elem -> string)
        (matrices: Map<Nonterminal<'nt>, Matrix<'elem option>>)
        (changedCells: CfpqCell<'nt> list)
        : string =
        matrices
        |> Map.toList
        |> List.map (fun (nt, m) ->
            let highlights =
                changedCells
                |> List.filter (fun c -> c.Nt = nt)
                |> List.map (fun c ->
                    { Matrix.Row = c.Row
                      Matrix.Col = c.Col
                      Matrix.Label = Matrix.CurrentCell })

            sprintf "\\textbf{%s}\n%s" (ntName nt) (renderMatrix elementPrinter m highlights))
        |> String.concat "\n\n"
