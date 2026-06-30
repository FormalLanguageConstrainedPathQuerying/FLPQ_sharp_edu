namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Kronecker-based RPQ algorithm with MS-BFS filtering.
/// Book: Chapter 12, 03_TensorProduct.tex (adapted to RPQ).
///
/// Algorithm:
/// 1. Compute Kronecker product of transition matrices: K_a = N^a ⊗ G^a
/// 2. Sum all K_a element-wise: K = Σ_a K_a
/// 3. Run MS-BFS on K from start pairs (q_s, v_s)
/// 4. Run reverse MS-BFS on K^T from final states
/// 5. Intersect results
/// 6. Project onto vertices
module KroneckerRPQ =

    /// Run Kronecker-based RPQ.
    /// Input: DFA query, per-label graph adjacency matrices, and source vertices.
    /// Output: |sources| × |V| boolean reachability matrix.
    let evaluate (dfa: DFA<'t, int>) (graphAdj: Map<'t, Matrix<bool>>) (sources: int[]) : Matrix<bool> =
        let qCount = Dfa.stateCount dfa

        let vCount =
            graphAdj
            |> Map.values
            |> Seq.tryHead
            |> Option.map (fun m -> m.rows)
            |> Option.defaultValue 0

        if vCount = 0 || sources.Length = 0 then
            Matrix.init sources.Length vCount false
        else
            let n = qCount * vCount

            let qvToIndex (q: int) (v: int) : int = q * vCount + v

            // Build combined adjacency matrix K = Σ_a (N^a ⊗ G^a)
            let k = Matrix.init n n false

            for KeyValue(label, gMat) in graphAdj do
                let nMat = Matrix.init qCount qCount false

                for i in 0 .. qCount - 1 do
                    for j in 0 .. qCount - 1 do
                        match dfa.transitions.data.[i, j] with
                        | Some nes when FSharpPlus.Data.NonEmptySet.contains label nes -> nMat.data.[i, j] <- true
                        | _ -> ()

                let kronMat = LinearAlgebra.kron nMat gMat (&&) false

                for i in 0 .. n - 1 do
                    for j in 0 .. n - 1 do
                        if kronMat.data.[i, j] then
                            k.data.[i, j] <- true

            // Start pairs: (startState, sourceVertex) for each source
            let startPairs = sources |> Array.map (fun v -> qvToIndex dfa.startState v)

            // Run MS-BFS from start pairs
            let forwardVisited = MsBfs.msBfs startPairs k

            // Result: vertex v is reachable from source i if
            // there exists a final state qf such that (qf, v) is reachable
            let kSources = sources.Length
            let result = Matrix.init kSources vCount false

            for i in 0 .. kSources - 1 do
                for v in 0 .. vCount - 1 do
                    let mutable reachable = false

                    for qf in dfa.finalStates do
                        if forwardVisited.data.[i, qvToIndex qf v] then
                            reachable <- true

                    result.data.[i, v] <- reachable

            result
