namespace FLPQ.GraphAnalysis

open FLPQ.LinearAlgebra

/// Multiple-source BFS and supporting matrix operations for RPQ algorithms.
/// Based on Chapter 3, 05_BFS.tex, algorithm algo:MS-BFS_linal.
/// Uses two algebraic structures:
/// - B = ⟨{0,1}, ∨, ∧⟩ — standard Boolean semiring
/// - M = ⟨{0,1}, ⊕⟩ — mask structure with inverted mask
module MsBfs =

    /// Boolean semiring addition: element-wise OR (∨).
    /// ⊕_B in the book notation.
    let boolAdd (a: Matrix<bool>) (b: Matrix<bool>) : Matrix<bool> = Matrix.map2 (||) a b

    /// Boolean semiring multiplication: matrix-matrix product with AND/OR.
    /// ⊗_B in the book notation.
    let boolMul (a: Matrix<bool>) (b: Matrix<bool>) : Matrix<bool> = LinearAlgebra.mxm a b (&&) (||) false

    /// Mask operation ⊕_M: result keeps values from the first operand
    /// only where the second operand is 0.
    /// 0⊕0=0, 1⊕1=0, 0⊕1=0, 1⊕0=1 — inverted mask.
    /// Used to filter the new front: keep only vertices NOT yet visited.
    let maskFilter (newFront: Matrix<bool>) (visited: Matrix<bool>) : Matrix<bool> =
        Matrix.map2 (fun nf v -> nf && not v) newFront visited

    /// Check if a boolean matrix has any true cell.
    let private anyTrue (m: Matrix<bool>) : bool = Matrix.fold (||) false m

    /// Multiple-source BFS (MS-BFS).
    /// Performs independent BFS traversals from k starting vertices simultaneously.
    /// The front is a k×|V| boolean matrix where row i is the BFS front for source K[i].
    /// Algorithm (Chapter 3, 05_BFS.tex, algo:MS-BFS_linal):
    ///   current_front ← 0^{k×n}
    ///   visited ← 0^{k×n}
    ///   For i ∈ [0..|K|-1]: current_front[i, K[i]] ← 1
    ///   While current_front ≠ 0:
    ///     visited ← visited ⊕_B current_front
    ///     new_front ← current_front ⊗_B M
    ///     current_front ← new_front ⊕_M visited
    ///   return visited
    let msBfs (sources: int[]) (adjacencyMatrix: Matrix<bool>) : Matrix<bool> =
        let k = sources.Length
        let n = Matrix.rows adjacencyMatrix

        let mutable currentFront = Matrix.init k n false
        let mutable visited = Matrix.init k n false

        for i in 0 .. k - 1 do
            Matrix.set currentFront i sources.[i] true

        while anyTrue currentFront do
            visited <- boolAdd visited currentFront
            let newFront = boolMul currentFront adjacencyMatrix
            currentFront <- maskFilter newFront visited

        visited
