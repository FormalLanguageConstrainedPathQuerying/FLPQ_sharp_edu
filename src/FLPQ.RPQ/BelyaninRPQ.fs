namespace FLPQ.RPQ

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Languages

/// Belyanin's LARPQ algorithm (BFS-based single-source RPQ).
/// Book: Chapter 11, 02_BFS.tex, algorithm algo:RPQ_BFS_semiring.
///
/// Operates on two |Q|×|V| matrices: front M and accumulated results P.
/// Propagates simultaneously through the automaton (backward) and the graph (forward).
module BelyaninRPQ =

    let private nfaToPerLabelMatrices (nfa: NFA<'t, int>) : Map<'t, Matrix<bool>> =
        let vCount = Nfa.stateCount nfa
        let labels = Nfa.alphabet nfa

        labels
        |> Set.toList
        |> List.map (fun label ->
            let m = Matrix.init vCount vCount false

            for i in 0 .. vCount - 1 do
                for j in 0 .. vCount - 1 do
                    match nfa.transitions.data.[i, j] with
                    | Some nes when NonEmptySet.contains label nes -> m.data.[i, j] <- true
                    | _ -> ()

            (label, m))
        |> Map.ofList

    /// Run Belyanin's RPQ algorithm for a single source vertex.
    let private runSingleSource
        (dfa: DFA<'t, int>)
        (perLabel: Map<'t, Matrix<bool>>)
        (source: int)
        (vCount: int)
        : bool[] =
        let qCount = Dfa.stateCount dfa

        if vCount = 0 then
            [||]
        else
            let mutable m = Matrix.init qCount vCount false
            let mutable p = Matrix.init qCount vCount false

            m.data.[dfa.startState, source] <- true

            let isZero (matrix: Matrix<bool>) : bool =
                not (Matrix.fold (fun acc x -> acc || x) false matrix)

            while not (isZero m) do
                m <- MsBfs.maskFilter m p
                p <- MsBfs.boolAdd p m

                let mutable newM = Matrix.init qCount vCount false

                for KeyValue(label, gMat) in perLabel do
                    let nMat = Matrix.init qCount qCount false

                    for i in 0 .. qCount - 1 do
                        for j in 0 .. qCount - 1 do
                            match dfa.transitions.data.[i, j] with
                            | Some nes when NonEmptySet.contains label nes -> nMat.data.[i, j] <- true
                            | _ -> ()

                    let nTranspose = Matrix.transpose nMat
                    let step1 = MsBfs.boolMul nTranspose m
                    let step2 = MsBfs.boolMul step1 gMat
                    newM <- MsBfs.boolAdd newM step2

                m <- newM

            let f = Matrix.init 1 qCount false

            for qf in dfa.finalStates do
                f.data.[0, qf] <- true

            let result = MsBfs.boolMul f p
            [| for j in 0 .. vCount - 1 -> result.data.[0, j] |]

    /// Run Belyanin's RPQ algorithm.
    /// Input: a DFA (query automaton) and a graph NFA.
    /// Output: |sources| × |V| boolean matrix indicating reachable vertices from each source.
    let evaluate (dfa: DFA<'t, int>) (graph: NFA<'t, int>) : Matrix<bool> =
        let sources = graph.startStates |> Set.toArray
        let perLabel = nfaToPerLabelMatrices graph
        let vCount = Nfa.stateCount graph

        let result = Matrix.init sources.Length vCount false

        for i in 0 .. sources.Length - 1 do
            let row = runSingleSource dfa perLabel sources.[i] vCount

            for j in 0 .. vCount - 1 do
                result.data.[i, j] <- row.[j]

        result
