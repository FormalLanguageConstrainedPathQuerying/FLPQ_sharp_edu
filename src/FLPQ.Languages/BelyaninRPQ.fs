namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// Belyanin's LARPQ algorithm (BFS-based single-source RPQ).
/// Book: Chapter 11, 02_BFS.tex, algorithm algo:RPQ_BFS_semiring.
///
/// Operates on two |Q|×|V| matrices: front M and accumulated results P.
/// Propagates simultaneously through the automaton (backward) and the graph (forward).
module BelyaninRPQ =

    /// Run Belyanin's RPQ algorithm.
    /// Input: a DFA (query automaton), per-label graph adjacency matrices, and start vertex.
    /// Output: boolean vector of length |V| indicating reachable vertices from v_s.
    let evaluate (dfa: DFA<'t, int>) (graphAdj: Map<'t, Matrix<bool>>) (startVertex: int) : bool[] =
        let qCount = Dfa.stateCount dfa

        let vCount =
            graphAdj
            |> Map.values
            |> Seq.tryHead
            |> Option.map (fun m -> m.rows)
            |> Option.defaultValue 0

        if vCount = 0 then
            [||]
        else
            let mutable m = Matrix.init qCount vCount false
            let mutable p = Matrix.init qCount vCount false

            for qs in dfa.finalStates do
                // Wait, task 59 says: ForEach q ∈ Q_S: M_{q,v_s} ← 1
                // Q_S is start states. DFA has a single start state.
                m.data.[dfa.startState, startVertex] <- true

            let isZero (matrix: Matrix<bool>) : bool =
                let mutable ok = true

                for i in 0 .. matrix.rows - 1 do
                    for j in 0 .. matrix.cols - 1 do
                        if matrix.data.[i, j] then
                            ok <- false

                ok

            while not (isZero m) do
                m <- MsBfs.maskFilter m p
                p <- MsBfs.boolAdd p m

                // Propagate: M ← Σ_{a} (N^a)^T ⊗_B M ⊗_B G^a
                // (N^a)^T is |Q| × |Q|, M is |Q| × |V|, G^a is |V| × |V|
                // First: (N^a)^T * M → |Q| × |V|
                // Then: result * G^a → |Q| × |V|
                let mutable newM = Matrix.init qCount vCount false

                for KeyValue(label, gMat) in graphAdj do
                    // Build N^a: automaton transition matrix for label 'a'
                    let nMat = Matrix.init qCount qCount false

                    for i in 0 .. qCount - 1 do
                        for j in 0 .. qCount - 1 do
                            match dfa.transitions.data.[i, j] with
                            | Some nes when FSharpPlus.Data.NonEmptySet.contains label nes -> nMat.data.[i, j] <- true
                            | _ -> ()

                    // (N^a)^T
                    let nTranspose = Matrix.transpose nMat

                    // (N^a)^T ⊗_B M
                    let step1 = MsBfs.boolMul nTranspose m

                    // step1 ⊗_B G^a
                    let step2 = MsBfs.boolMul step1 gMat

                    newM <- MsBfs.boolAdd newM step2

                m <- newM

            // F: 1×|Q| vector with 1 for final states
            let f = Matrix.init 1 qCount false

            for qf in dfa.finalStates do
                f.data.[0, qf] <- true

            // result = F ⊗_B P → 1×|V|
            let result = MsBfs.boolMul f p

            [| for j in 0 .. vCount - 1 -> result.data.[0, j] |]
