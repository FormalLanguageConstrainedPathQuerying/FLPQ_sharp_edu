namespace FLPQ.RPQ

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Languages

/// Kronecker-based RPQ algorithm with MS-BFS filtering.
/// Book: Chapter 12, 03_TensorProduct.tex (adapted to RPQ).
///
/// Algorithm:
/// 1. Compute Kronecker product of transition matrices: K_a = N^a ⊗ G^a
/// 2. Sum all K_a element-wise: K = Σ_a K_a
/// 3. Run MS-BFS on K from start pairs (q_s, v_s)
/// 4. Project onto vertices via final states
module KroneckerRPQ =

    /// Run Kronecker-based RPQ.
    /// Input: DFA query and graph NFA.
    /// Output: |sources| × |V| boolean reachability matrix.
    let evaluate (dfa: DFA<'t, int>) (graph: NFA<'t, int>) : Matrix<bool> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.transitions
        let sources = graph.startStates |> Set.toArray
        let qCount = Dfa.stateCount dfa
        let vCount = Nfa.stateCount graph

        if vCount = 0 || sources.Length = 0 then
            Matrix.init sources.Length vCount false
        else
            let n = qCount * vCount

            let qvToIndex (q: int) (v: int) : int = q * vCount + v

            let k = Matrix.init n n false

            for KeyValue(label, gMat) in perLabel do
                match label with
                | AEpsilon -> ()
                | ATerm t ->

                    let nMat = Matrix.init qCount qCount false

                    for i in 0 .. qCount - 1 do
                        for j in 0 .. qCount - 1 do
                            match dfa.transitions.data.[i, j] with
                            | Some nes when NonEmptySet.contains (ATerm t) nes -> nMat.data.[i, j] <- true
                            | _ -> ()

                    let kronMat = LinearAlgebra.kron nMat gMat (&&) false

                    for i in 0 .. n - 1 do
                        for j in 0 .. n - 1 do
                            if kronMat.data.[i, j] then
                                k.data.[i, j] <- true

            let startPairs = sources |> Array.map (fun v -> qvToIndex dfa.startState v)

            let forwardVisited = MsBfs.msBfs startPairs k

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
