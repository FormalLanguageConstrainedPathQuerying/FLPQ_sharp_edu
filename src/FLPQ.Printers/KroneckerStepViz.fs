namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// TeX rendering of one Kronecker CFPQ fixed-point iteration as two stacked lines (book
/// Chapter 12, listing lst:algo1): line 1 is the full Kronecker product
/// `$ M_1 \otimes M_2 = K $` (the RSM transition matrix and the current edge-label matrix with
/// set-of-symbols cells, and the Boolean product mask); line 2 is the MS-BFS initial (seed) and
/// final (visited) fronts, decoded with `(A, v)` rows and `(s, v)` columns, the final front with
/// its accept columns highlighted. The renderer is pure: it only formats the step's stored
/// matrices and passes the graph figure through (built by the caller via `RpqGraphViz`).
module KroneckerStepViz =

    /// One rendered Kronecker step: the two line bodies (each already wrapped in a shrink-to-fit
    /// adjustbox) plus the graph figure in the selected format.
    type KroneckerVisualizationStep =
        { Kron: string
          Fronts: string
          GraphDot: string
          GraphTikz: string }

    /// One cell of the edge-label matrix as a set of symbols: `None` renders as
    /// `\varnothing`; `Some set` renders as the comma-joined symbols, terminals via the terminal
    /// printer and nonterminals in `\text{...}`.
    let symbolSetCellToTeX
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (cell: Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>)
        : string =
        match cell with
        | None -> @"\varnothing"
        | Some set ->
            set
            |> NonEmptySet.toSeq
            |> Seq.map (function
                | AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal t)) -> terminal t
                | AutomatonLabel.ATerm(RsmSymbol.RNonterm nt) -> @"\text{" + ntName nt + "}"
                | AutomatonLabel.AEpsilon -> @"\varepsilon")
            |> Seq.toList
            |> String.concat @"\;"

    /// A labeled matrix (row/column label printers supplied) with per-cell highlights, wrapped
    /// in a shrink-to-fit adjustbox.
    let private labeledMatrix
        (cellPrinter: 'a -> string)
        (m: Matrix<'a>)
        (rowLabel: int -> string)
        (colLabel: int -> string)
        (highlights: Matrix.Highlight list)
        : string =
        MatrixTeX.toTeXStyled false false cellPrinter m highlights [] (Some rowLabel) (Some colLabel) false true

    /// Highlight the given (row, col) pairs in yellow (out-of-bounds pairs are dropped).
    let private cellHighlights (cells: Set<int * int>) (rows: int) (cols: int) : Matrix.Highlight list =
        cells
        |> Set.toList
        |> List.filter (fun (r, c) -> r >= 0 && r < rows && c >= 0 && c < cols)
        |> List.map (fun (r, c) ->
            { Matrix.Row = r
              Matrix.Col = c
              Matrix.Label = Matrix.CurrentCell })

    /// A subscript that braces multi-digit indices so `q_15` renders as `q_{15}`.
    let private bracedIndex (prefix: string) (i: int) : string =
        if i < 10 then
            sprintf "%s_%d" prefix i
        else
            sprintf "%s_{%d}" prefix i

    /// Line 1 body: the Kronecker product as one math row `$ M_1 \otimes M_2 = K $`, the
    /// operands with set-of-symbols cells and `K` as a Boolean mask, the whole row wrapped in a
    /// shrink-to-fit, top-aligned adjustbox.
    let kronBody
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (n: int)
        (kChanged: Set<int * int>)
        (m1: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>>)
        (m2: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>>)
        (k: Matrix<bool>)
        : string =
        let m1Body =
            labeledMatrix
                (symbolSetCellToTeX terminal ntName)
                m1
                (fun i -> bracedIndex "q" i)
                (fun j -> bracedIndex "q" j)
                []

        let m2Body =
            labeledMatrix
                (symbolSetCellToTeX terminal ntName)
                m2
                (fun i -> bracedIndex "v" i)
                (fun j -> bracedIndex "v" j)
                []

        // The product mask is a (RSM state, vertex) product-state matrix, so its row/column
        // labels are the pairs (q_i, v_j), consistent with the MS-BFS fronts; the step's newly
        // derived product-state cells are highlighted.
        let pairLabel c =
            sprintf "%s, %s" (bracedIndex "q" (c / n)) (bracedIndex "v" (c % n))

        let kBody =
            labeledMatrix
                (fun b -> if b then @"\bullet" else @"\cdot")
                k
                pairLabel
                pairLabel
                (cellHighlights kChanged (Matrix.rows k) (Matrix.cols k))

        let expr =
            "$" + m1Body + @"\quad\otimes\quad" + m2Body + @"\quad=\quad" + kBody + "$"

        StepTeX.wrapFormula expr

    /// Highlight the given columns of an `rows`-row matrix (every cell in those columns).
    let private columnHighlights (cols: Set<int>) (rows: int) : Matrix.Highlight list =
        cols
        |> Set.toList
        |> List.collect (fun c ->
            [ for r in 0 .. rows - 1 ->
                  { Matrix.Row = r
                    Matrix.Col = c
                    Matrix.Label = Matrix.CurrentCell } ])

    /// Line 2 body: the two MS-BFS fronts stacked vertically — the initial (seed) front above the
    /// final (visited) front, the latter with its `FinalStateCols` columns highlighted. Rows and
    /// columns are product-state pairs `(q_i, u_j)`: a row is the BFS from source pair
    /// `(start(A), v)`, labeled with the nonterminal `A` replaced by its start state `q_i`
    /// (consistent with the column's `(q_i, u_j)` labels).
    let frontsBody (step: KroneckerCfpq.CfpqKronStep<'nt, 't>) (n: int) : string =
        let pairLabel (c: int) = sprintf "q_%d, v_%d" (c / n) (c % n)
        let cell b = if b then @"\bullet" else @"\cdot"

        let initial = labeledMatrix cell step.InitialFront pairLabel pairLabel []

        let final =
            labeledMatrix
                cell
                step.FinalFront
                pairLabel
                pairLabel
                (columnHighlights step.FinalStateCols (Matrix.rows step.FinalFront))

        initial + "\n\n" + final

    /// Render one Kronecker step into its two line bodies and the pass-through graph figure.
    let renderStep
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (n: int)
        (step: KroneckerCfpq.CfpqKronStep<'nt, 't>)
        (graphDot: string)
        (graphTikz: string)
        : KroneckerVisualizationStep =
        { Kron = kronBody terminal ntName n step.KChangedCells step.M1 step.M2 step.K
          Fronts = frontsBody step n
          GraphDot = graphDot
          GraphTikz = graphTikz }
