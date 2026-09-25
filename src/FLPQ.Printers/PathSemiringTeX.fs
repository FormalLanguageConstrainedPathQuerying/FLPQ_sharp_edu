namespace FLPQ.Printers

open FLPQ.LinearAlgebra

/// TeX rendering of path semiring matrices: cells hold sets of vertex paths
/// (book: Chapter 11, 02_BFS.tex — matrix cells with vertex sequences).
module PathSemiringTeX =

    /// Render a path as a TeX tuple of vertex names: (v_0, v_2, v_3).
    let pathToTeX (p: int list) : string =
        p
        |> List.map (fun v -> sprintf "v_%d" v)
        |> String.concat ", "
        |> fun s -> "(" + s + ")"

    /// Render a set of paths as a TeX set: \{(v_0, v_1), (v_0, v_2)\}; empty set → \cdot.
    let pathSetCellToTeX (cell: Set<int list>) : string =
        ParsingTableTeX.setToTeX pathToTeX (Set.toSeq cell)

    /// Render a path semiring matrix with vertex row/column labels, wrapped in adjustbox.
    let matrixToTeX (rowLabel: int -> string) (colLabel: int -> string) (m: Matrix<Set<int list>>) : string =
        MatrixTeX.toTeXStyled false false pathSetCellToTeX m [] [] (Some rowLabel) (Some colLabel) false true

    /// Render a path semiring matrix with v_i vertex row/column labels.
    let matrixWithVertexLabels (m: Matrix<Set<int list>>) : string =
        matrixToTeX (fun i -> sprintf "v_%d" i) (fun j -> sprintf "v_%d" j) m

    /// Render a |Q|x|V| path semiring matrix with q_i state row labels and v_i vertex
    /// column labels (the book's RPQ working matrices: rows are automaton states).
    let matrixWithStateVertexLabels (m: Matrix<Set<int list>>) : string =
        matrixToTeX (fun i -> sprintf "q_%d" i) (fun j -> sprintf "v_%d" j) m

    /// Render a boolean matrix with row/column labels, wrapped in adjustbox: true cells as
    /// \bullet, false cells as \cdot (the book's figure convention for N^a and G^a).
    let boolMatrixToTeX (rowLabel: int -> string) (colLabel: int -> string) (m: Matrix<bool>) : string =
        MatrixTeX.toTeXStyled
            false
            false
            (fun b -> if b then @"\bullet" else @"\cdot")
            m
            []
            []
            (Some rowLabel)
            (Some colLabel)
            false
            true
