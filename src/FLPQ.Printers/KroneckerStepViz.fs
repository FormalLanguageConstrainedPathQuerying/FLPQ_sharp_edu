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

    /// A labeled matrix (row/column label printers supplied) with the given column highlights,
    /// wrapped in a shrink-to-fit adjustbox.
    let private labeledMatrix
        (cellPrinter: 'a -> string)
        (m: Matrix<'a>)
        (rowLabel: int -> string)
        (colLabel: int -> string)
        (highlightCols: Set<int>)
        : string =
        let highlights =
            [ for c in Set.toList highlightCols do
                  for r in 0 .. Matrix.rows m - 1 ->
                      { Matrix.Row = r
                        Matrix.Col = c
                        Matrix.Label = Matrix.CurrentCell } ]

        MatrixTeX.toTeXStyled false false cellPrinter m highlights [] (Some rowLabel) (Some colLabel) false true

    /// Line 1 body: the Kronecker product as one math row `$ M_1 \otimes M_2 = K $`, the
    /// operands with set-of-symbols cells and `K` as a Boolean mask, the whole row wrapped in a
    /// shrink-to-fit, top-aligned adjustbox.
    let kronBody
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (step: KroneckerCfpq.CfpqKronStep<'nt, 't>)
        : string =
        let m1 =
            labeledMatrix
                (symbolSetCellToTeX terminal ntName)
                step.M1
                (fun i -> sprintf "q_%d" i)
                (fun j -> sprintf "q_%d" j)
                Set.empty

        let m2 =
            labeledMatrix
                (symbolSetCellToTeX terminal ntName)
                step.M2
                (fun i -> sprintf "v_%d" i)
                (fun j -> sprintf "v_%d" j)
                Set.empty

        let k =
            PathSemiringTeX.boolMatrixToTeXBody (fun i -> sprintf "q_%d" i) (fun j -> sprintf "v_%d" j) step.K

        let expr = "$" + m1 + @"\quad\otimes\quad" + m2 + @"\quad=\quad" + k + "$"
        StepTeX.wrapFormula expr

    /// Line 2 body: the two MS-BFS fronts stacked vertically — the initial (seed) front above the
    /// final (visited) front, the latter with its `FinalStateCols` columns highlighted. Rows are
    /// decoded as `(A, v)` source pairs; columns as `(s, v)` product states.
    let frontsBody
        (step: KroneckerCfpq.CfpqKronStep<'nt, 't>)
        (sourcePairs: (Nonterminal<'nt> * int) list)
        (ntName: Nonterminal<'nt> -> string)
        (n: int)
        : string =
        let rowLabel (i: int) =
            match List.tryItem i sourcePairs with
            | Some(a, v) -> @"\text{" + ntName a + "}, v_" + (string v)
            | None -> sprintf "s_%d" i

        let colLabel (c: int) =
            let s = c / n
            let v = c % n
            sprintf "q_%d, v_%d" s v

        let cell b = if b then @"\bullet" else @"\cdot"
        let initial = labeledMatrix cell step.InitialFront rowLabel colLabel Set.empty
        let final = labeledMatrix cell step.FinalFront rowLabel colLabel step.FinalStateCols
        initial + "\n\n" + final

    /// Render one Kronecker step into its two line bodies and the pass-through graph figure.
    let renderStep
        (terminal: 't -> string)
        (ntName: Nonterminal<'nt> -> string)
        (n: int)
        (sourcePairs: (Nonterminal<'nt> * int) list)
        (step: KroneckerCfpq.CfpqKronStep<'nt, 't>)
        (graphDot: string)
        (graphTikz: string)
        : KroneckerVisualizationStep =
        { Kron = kronBody terminal ntName step
          Fronts = frontsBody step sourcePairs ntName n
          GraphDot = graphDot
          GraphTikz = graphTikz }
