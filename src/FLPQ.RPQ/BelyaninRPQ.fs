namespace FLPQ.RPQ

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Languages

/// Belyanin's LARPQ algorithm (BFS-based single-source RPQ).
/// Book: Chapter 11, 02_BFS.tex, algorithm algo:RPQ_BFS_semiring.
/// Operates on two |Q|×|V| matrices: front M and accumulated results P.
/// Propagates simultaneously through the automaton (backward) and the graph (forward).
module BelyaninRPQ =

    /// Run Belyanin's RPQ algorithm for a single source vertex.
    let private runSingleSource
        (dfa: DFA<'t, int>)
        (perLabel: Map<AutomatonLabel<'t>, Matrix<bool>>)
        (source: int)
        (vCount: int)
        : bool[] =
        let qCount = Dfa.stateCount dfa

        if vCount = 0 then
            [||]
        else
            let mutable m = Matrix.init qCount vCount false
            let mutable p = Matrix.init qCount vCount false

            Matrix.set m dfa.StartState source true

            let isZero (matrix: Matrix<bool>) : bool = not (Matrix.fold (||) false matrix)

            while not (isZero m) do
                m <- MsBfs.maskFilter m p
                p <- MsBfs.boolAdd p m

                let mutable newM = Matrix.init qCount vCount false

                for KeyValue(label, gMat) in perLabel do
                    match label with
                    | AEpsilon -> ()
                    | ATerm t ->

                        let nMat = Matrix.init qCount qCount false

                        for i in 0 .. qCount - 1 do
                            for j in 0 .. qCount - 1 do
                                match Matrix.get dfa.Transitions i j with
                                | Some nes when NonEmptySet.contains (ATerm t) nes -> Matrix.set nMat i j true
                                | _ -> ()

                        let nTranspose = Matrix.transpose nMat
                        let step1 = MsBfs.boolMul nTranspose m
                        let step2 = MsBfs.boolMul step1 gMat
                        newM <- MsBfs.boolAdd newM step2

                m <- newM

            let f = Matrix.init 1 qCount false

            for qf in dfa.FinalStates do
                Matrix.set f 0 qf true

            let result = MsBfs.boolMul f p
            [| for j in 0 .. vCount - 1 -> Matrix.get result 0 j |]

    /// Run Belyanin's RPQ algorithm.
    /// Input: a DFA (query automaton) and a graph NFA.
    /// Output: |sources| × |V| boolean matrix indicating reachable vertices from each source.
    let evaluate (dfa: DFA<'t, int>) (graph: NFA<'t, int>) : Matrix<bool> =
        let sources = graph.StartStates |> Set.toArray
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.Transitions
        let vCount = Nfa.stateCount graph

        let result = Matrix.init sources.Length vCount false

        for i in 0 .. sources.Length - 1 do
            let row = runSingleSource dfa perLabel sources.[i] vCount

            for j in 0 .. vCount - 1 do
                Matrix.set result i j row.[j]

        result
