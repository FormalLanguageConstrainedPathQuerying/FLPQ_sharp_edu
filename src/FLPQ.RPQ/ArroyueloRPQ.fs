namespace FLPQ.RPQ

open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Languages

/// Arroyuelo's RPQ algorithm.
/// Book: Chapter 11, 03_Arroyuelo.tex.
///
/// Translates a regular expression into a Boolean matrix expression and evaluates it.
/// Uses dense Boolean matrices.
module ArroyueloRPQ =

    /// Compute transitive closure of a square Boolean matrix using repeated squaring.
    let private transitiveClosure (m: Matrix<bool>) : Matrix<bool> =
        let n = m.rows
        let mutable result = Matrix.init n n false

        for i in 0 .. n - 1 do
            result.data.[i, i] <- true

        let mutable power = m

        for _ in 0 .. n - 1 do
            result <- MsBfs.boolAdd result power
            power <- MsBfs.boolMul power power

        result

    /// Evaluate a regular expression AST to a Boolean matrix.
    /// M(ε) = I, M(a) = graphAdj[a], M(a^-) = graphAdj[a]^T,
    /// M(E1 | E2) = M(E1) ∨ M(E2), M(E1 / E2) = M(E1) × M(E2),
    /// M(E+) = M(E)^+, M(E*) = I ∨ M(E)^+.
    let rec private evalExpression
        (graphAdj: Map<AutomatonLabel<'t>, Matrix<bool>>)
        (vCount: int)
        (regexp: Regexp<'t, 'nt>)
        : Matrix<bool> =
        let identity = Matrix.init vCount vCount false

        for i in 0 .. vCount - 1 do
            identity.data.[i, i] <- true

        match regexp with
        | Regexp.REps -> identity
        | Regexp.REmpty -> Matrix.init vCount vCount false
        | Regexp.RTerm(Terminal t) ->
            match Map.tryFind (ATerm t) graphAdj with
            | Some m -> m
            | None -> Matrix.init vCount vCount false
        | Regexp.RNonterm _ -> Matrix.init vCount vCount false
        | Regexp.RAlt(l, r) ->
            let lMat = evalExpression graphAdj vCount l
            let rMat = evalExpression graphAdj vCount r
            MsBfs.boolAdd lMat rMat
        | Regexp.RSeq(l, r) ->
            let lMat = evalExpression graphAdj vCount l
            let rMat = evalExpression graphAdj vCount r
            MsBfs.boolMul lMat rMat
        | Regexp.RStar(rp) ->
            let rMat = evalExpression graphAdj vCount rp
            let closure = transitiveClosure rMat
            MsBfs.boolAdd identity closure

    /// Evaluate a regexp on the given graph and return a |sources| × |V| boolean reachability matrix.
    /// Sources are taken from the NFA's start states.
    let evaluate (graph: NFA<'t, int>) (regexp: Regexp<'t, 'nt>) : Matrix<bool> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.transitions
        let vCount = Nfa.stateCount graph
        let sources = graph.startStates |> Set.toArray

        let fullMatrix = evalExpression perLabel vCount regexp

        let k = sources.Length
        let result = Matrix.init k vCount false

        for i in 0 .. k - 1 do
            for j in 0 .. vCount - 1 do
                result.data.[i, j] <- fullMatrix.data.[sources.[i], j]

        result
