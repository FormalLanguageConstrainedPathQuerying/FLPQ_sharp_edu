namespace FLPQ.Printers

open FSharpPlus.Data
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// TeX rendering of one Kronecker CFPQ fixed-point iteration as two stacked lines (book
/// Chapter 12, listing lst:algo1): line 1 is the full Kronecker product
/// `$ M_1 \otimes M_2 = K $` (the RSM transition matrix and the current edge-label matrix with
/// set-of-symbols cells, and the Boolean product mask with its Kronecker block rules — drawn via
/// nicematrix `vlines`/`hlines` — and the newly filled cells highlighted); line 2 is the MS-BFS
/// initial (seed) and final (visited) fronts, decoded with `(q_i, v_j)` product-state rows and
/// columns, the final front with its newly-visited RSM final-state cells highlighted (the cells in
/// the `FinalStateCols` columns that turned on this iteration, the accept-progress). The renderer
/// is pure: it only formats the step's stored matrices and passes the graph figure through (built
/// by the caller via `RpqGraphViz`).
module KroneckerStepViz =

    /// One rendered Kronecker step: the two line bodies (each already wrapped in a shrink-to-fit
    /// adjustbox) plus the graph figure in the selected format.
    type KroneckerVisualizationStep =
        { Kron: string
          Fronts: string
          GraphDot: string
          GraphTikz: string }

    /// One cell of the edge-label matrix as a set of symbols: `None` renders as `\cdot` (a dot,
    /// like empty cells in the other matrices); `Some set` renders as the comma-joined symbols,
    /// terminals via the terminal printer and nonterminals in `\text{...}`.
    let symbolSetCellToTeX
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (cell: Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>)
        : string =
        match cell with
        | None -> @"\cdot"
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

    /// The 1-indexed column/row positions (nicematrix `vlines`/`hlines` semantics) of the internal
    /// block boundaries of a q × q grid of n × n blocks, where 1-indexed position `p` of a
    /// `pNiceMatrix[first-row,first-col]` is 0-indexed data position `p - 2` (position 1 is the
    /// first-col/first-row). Between block `k - 1` and block `k` (0-indexed, `k >= 1`) the boundary
    /// sits at the left edge of data position `k * n`, i.e. 1-indexed position `k * n + 2`. Returns
    /// empty when `q <= 1` (no internal boundaries).
    let private blockBoundaryPositions (q: int) (n: int) : int list =
        [ for k in 1 .. q - 1 do
              k * n + 2 ]

    /// The K product mask as a pNiceMatrix with the Kronecker product structure made visible:
    /// `K` is qn × qn (q RSM states × n vertices each), so it splits into a q × q grid of n × n
    /// blocks. Thin rules (lines only, no fill) separate the blocks, drawn with nicematrix's
    /// `vlines`/`hlines` options (a full-height/width rule at each internal block boundary — the
    /// `\vrule`-in-cell / `\noalign{\hrule}` approach does not render). The row/column labels are
    /// the product-state pairs `(q_i, v_j)`; the cells in `highlights` are shaded. The header row
    /// carries an empty corner cell (the pNiceMatrix top-left), matching `MatrixTeX.toTeXStyled`,
    /// so the column labels align.
    let private kMatrixWithRules
        (k: Matrix<bool>)
        (n: int)
        (rowLabel: int -> string)
        (colLabel: int -> string)
        (highlights: Set<int * int>)
        : string =
        let q = Matrix.rows k / n
        let size = q * n
        let sb = System.Text.StringBuilder()

        if size > 10 then
            sb.Append(sprintf @"\setcounter{MaxMatrixCols}{%d}" size).AppendLine() |> ignore

        sb.AppendLine(@"\begin{adjustbox}{max width=\textwidth}").Append("$").AppendLine()
        |> ignore

        // Internal block boundaries via nicematrix's vlines/hlines options (1-indexed positions);
        // an empty list (q <= 1) draws no internal rules.
        let positions = blockBoundaryPositions q n

        let options =
            "first-row,first-col, vlines={"
            + String.concat "," (List.map string positions)
            + "}, hlines={"
            + String.concat "," (List.map string positions)
            + "}"

        sb.Append(@"\begin{pNiceMatrix}[" + options + "]").AppendLine() |> ignore

        // Header row: an empty corner cell (the pNiceMatrix top-left), then the column labels.
        let headerCells =
            [ for c in 0 .. size - 1 do
                  colLabel c ]

        sb.Append(("  & " + String.concat " & " headerCells) + @" \\").AppendLine()
        |> ignore

        for r in 0 .. size - 1 do
            let cells =
                [ for c in 0 .. size - 1 do
                      let content = if k.[r, c] then @"\bullet" else @"\cdot"

                      if Set.contains (r, c) highlights then
                          sprintf @"\cellcolor{yellow}{%s}" content
                      else
                          content ]

            sb.Append((rowLabel r + " & " + String.concat " & " cells) + @" \\").AppendLine()
            |> ignore

        sb.Append(@"\end{pNiceMatrix}").AppendLine() |> ignore
        sb.AppendLine("$").AppendLine().AppendLine(@"\end{adjustbox}") |> ignore

        sb.ToString()

    /// Line 1 body: the Kronecker product as one math row `$ M_1 \otimes M_2 = K $`, the
    /// operands with set-of-symbols cells and `K` as a Boolean mask with block-splitting rules,
    /// the whole row wrapped in a shrink-to-fit, top-aligned adjustbox.
    let kronBody
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (n: int)
        (kNewCells: Set<int * int>)
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
        // labels are the pairs (q_i, v_j), consistent with the MS-BFS fronts. It splits into a
        // q x q grid of n x n blocks (the Kronecker product structure), drawn with block rules;
        // the cells that turned on for the first time this iteration (the diff against the
        // previous K) are highlighted.
        let pairLabel c =
            sprintf "%s, %s" (bracedIndex "q" (c / n)) (bracedIndex "v" (c % n))

        let kBody = kMatrixWithRules k n pairLabel pairLabel kNewCells

        let expr =
            "$" + m1Body + @"\quad\otimes\quad" + m2Body + @"\quad=\quad" + kBody + "$"

        StepTeX.wrapFormula expr

    /// The filled cells of a Boolean matrix whose column is in `cols` — the accept-reading cells:
    /// a reached product state in one of the RSM final-state columns.
    let private finalStateCells (m: Matrix<bool>) (cols: Set<int>) : Set<int * int> =
        [ for r in 0 .. (Matrix.rows m - 1) do
              for c in 0 .. (Matrix.cols m - 1) do
                  if m.[r, c] && Set.contains c cols then
                      (r, c) ]
        |> Set.ofList

    /// Line 2 body: the two MS-BFS fronts stacked vertically — the initial (seed) front above the
    /// final (visited) front, the latter with its newly-visited RSM final-state cells highlighted:
    /// the cells in the `FinalStateCols` columns (the `(final f, vertex v)` product-state columns)
    /// that turned on this iteration (the diff `frontNewCells` against the previous step's final
    /// front, restricted to those columns — the accept-progress of the MS-BFS). Rows and columns
    /// are product-state pairs `(q_i, u_j)`: a row is the BFS from source pair `(start(A), v)`,
    /// labeled with the nonterminal `A` replaced by its start state `q_i` (consistent with the
    /// column's `(q_i, u_j)` labels).
    let frontsBody (step: KroneckerCfpq.CfpqKronStep<'nt, 't>) (n: int) (frontNewCells: Set<int * int>) : string =
        let pairLabel (c: int) =
            sprintf "%s, %s" (bracedIndex "q" (c / n)) (bracedIndex "v" (c % n))

        let cell b = if b then @"\bullet" else @"\cdot"

        let initial = labeledMatrix cell step.InitialFront pairLabel pairLabel []

        let final =
            labeledMatrix
                cell
                step.FinalFront
                pairLabel
                pairLabel
                (cellHighlights
                    (Set.intersect frontNewCells (finalStateCells step.FinalFront step.FinalStateCols))
                    (Matrix.rows step.FinalFront)
                    (Matrix.cols step.FinalFront))

        initial + "\n\n" + final

    /// Render one Kronecker step into its two line bodies and the pass-through graph figure.
    /// `kNewCells` is the set of `K` cells that turned on for the first time this iteration (the
    /// diff against the previous iteration's `K`); `frontNewCells` is the set of final-front cells
    /// that turned on for the first time this iteration (the diff against the previous
    /// iteration's final front). The caller computes both (it walks the steps in order). The final
    /// front highlights the `frontNewCells` that fall in its `FinalStateCols` columns (the
    /// newly-visited final-state cells).
    let renderStep
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (n: int)
        (step: KroneckerCfpq.CfpqKronStep<'nt, 't>)
        (kNewCells: Set<int * int>)
        (frontNewCells: Set<int * int>)
        (graphDot: string)
        (graphTikz: string)
        : KroneckerVisualizationStep =
        { Kron = kronBody terminal ntName n kNewCells step.M1 step.M2 step.K
          Fronts = frontsBody step n frontNewCells
          GraphDot = graphDot
          GraphTikz = graphTikz }
