namespace FLPQ.RPQ

open FLPQ.LinearAlgebra

/// Path semiring over sets of vertex sequences.
/// Book: Chapter 11, 02_BFS.tex — the path semiring carrier 2^{V*} applied to
/// matrix-based RPQ evaluation. A matrix cell holds the set of paths (vertex
/// sequences, endpoints included) witnessing reachability between its row and
/// column vertices. Only simple paths (no repeated vertex) are stored, which
/// keeps cells finite on cyclic graphs; concatenation drops non-simple results.
module PathSemiring =

    /// The additive identity: the empty set of paths.
    let zero: Set<int list> = Set.empty

    /// Whether a cell is the additive identity.
    let isZero (cell: Set<int list>) : bool = Set.isEmpty cell

    /// A path is simple when no vertex occurs twice.
    let isSimple (p: int list) : bool =
        Set.count (Set.ofList p) = List.length p

    /// Append a vertex to a path, keeping the result simple.
    /// Returns None when the vertex already occurs in the path.
    let extend (p: int list) (v: int) : int list option =
        if List.contains v p then None else Some(p @ [ v ])

    /// Cell product for matrix multiplication at (i,k) x (k,j): concatenate p1 and p2
    /// when last p1 = head p2 (the shared vertex is written once), keeping only
    /// simple results. Total: empty paths contribute nothing.
    let mulCells (a: Set<int list>) (b: Set<int list>) : Set<int list> =
        Set.fold
            (fun acc p1 ->
                match p1 with
                | [] -> acc
                | _ ->
                    let lastV = List.last p1

                    Set.fold
                        (fun acc2 p2 ->
                            match p2 with
                            | h2 :: rest2 when h2 = lastV ->
                                let merged = p1 @ rest2
                                if isSimple merged then Set.add merged acc2 else acc2
                            | _ -> acc2)
                        acc
                        b)
            zero
            a

    /// Matrix sum: union of path sets cell-wise.
    let addMatrices (a: Matrix<Set<int list>>) (b: Matrix<Set<int list>>) : Matrix<Set<int list>> =
        Matrix.map2 Set.union a b

    /// Matrix product over the path semiring: (A x B)[i,j] = union over k of
    /// mulCells A[i,k] B[k,j].
    let mxm (a: Matrix<Set<int list>>) (b: Matrix<Set<int list>>) : Matrix<Set<int list>> =
        LinearAlgebra.mxm a b mulCells Set.union zero

    /// Row selection by a boolean matrix: ((N)^T (x)) M — result[q', v] is the union of
    /// M[q, v] over all q with N[q, q'] = true. N : Q x Q', M : Q x V, result : Q' x V.
    let boolSelectRows (n: Matrix<bool>) (m: Matrix<Set<int list>>) : Matrix<Set<int list>> =
        let qCount = Matrix.rows n
        let qPrimeCount = Matrix.cols n
        let vCount = Matrix.cols m

        Matrix.create qPrimeCount vCount (fun qp v ->
            [ for q in 0 .. qCount - 1 do
                  if n.[q, qp] then
                      m.[q, v] ]
            |> List.fold Set.union zero)

    /// Column extension by a boolean adjacency: M (x) G — result[q, v'] is the union,
    /// over all v with G[v, v'] = true, of each path in M[q, v] extended by v'
    /// (non-simple extensions dropped). M : Q x V, G : V x V, result : Q x V.
    let boolExtendCols (m: Matrix<Set<int list>>) (g: Matrix<bool>) : Matrix<Set<int list>> =
        let qCount = Matrix.rows m
        let vCount = Matrix.cols m

        Matrix.create qCount vCount (fun q vp ->
            [ for v in 0 .. vCount - 1 do
                  if g.[v, vp] then
                      m.[q, v]
                      |> Set.filter (fun p -> not (List.contains vp p))
                      |> Set.map (fun p -> p @ [ vp ]) ]
            |> List.fold Set.union zero)

    /// Identity matrix: the trivial path [i] on the diagonal, empty cells elsewhere.
    let identity (n: int) : Matrix<Set<int list>> =
        Matrix.create n n (fun i j -> if i = j then Set.singleton [ i ] else zero)

    /// Transitive closure over the path semiring: repeated squaring of I + M,
    /// the same loop shape as the Boolean transitive closure in ArroyueloRPQ.
    /// After the loop, cell [i, j] holds every simple path from i to j that is a
    /// concatenation of paths from M (padded with trivial paths to a power of two).
    let transitiveClosure (m: Matrix<Set<int list>>) : Matrix<Set<int list>> =
        let n = Matrix.rows m

        let mutable a = addMatrices (identity n) m

        let mutable k = 1

        while k < n do
            a <- mxm a a
            k <- k * 2

        a

    /// Whether every cell of the matrix is empty.
    let isEmpty (m: Matrix<Set<int list>>) : bool =
        let rows = Matrix.rows m
        let cols = Matrix.cols m

        [ for i in 0 .. rows - 1 do
              for j in 0 .. cols - 1 do
                  yield isZero m.[i, j] ]
        |> List.forall id

    /// Union of all cells: every path stored anywhere in the matrix.
    let allPaths (m: Matrix<Set<int list>>) : Set<int list> =
        let rows = Matrix.rows m
        let cols = Matrix.cols m

        [ for i in 0 .. rows - 1 do
              for j in 0 .. cols - 1 do
                  yield m.[i, j] ]
        |> List.fold Set.union zero

    /// Boolean projection: a cell is true when it holds at least one path.
    let booleanProjection (m: Matrix<Set<int list>>) : Matrix<bool> =
        let rows = Matrix.rows m
        let cols = Matrix.cols m

        Matrix.create rows cols (fun i j -> not (isZero m.[i, j]))
