namespace FLPQ.RPQ

open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Languages

/// Arroyuelo's RPQ algorithm.
/// Book: Chapter 11, 03_Arroyuelo.tex.
/// Translates a regular expression into a Boolean matrix expression and evaluates it.
/// Uses dense Boolean matrices.
module ArroyueloRPQ =

    /// Compute transitive closure of a square Boolean matrix using repeated squaring.
    /// Computes (I + M)^n where n = number of rows, by squaring (I + M) ceil(log2(n)) times.
    let private transitiveClosure (m: Matrix<bool>) : Matrix<bool> =
        let n = Matrix.rows m

        let mutable a = Matrix.init n n false

        for i in 0 .. n - 1 do
            a.[i, i] <- true

        a <- MsBfs.boolAdd a m

        let mutable k = 1

        while k < n do
            a <- MsBfs.boolMul a a
            k <- k * 2

        a

    /// The matrix operation performed at a regexp tree node.
    type ArroyueloOperation =
        | Base
        | Alt
        | Seq
        | Star

    /// One step of the Boolean (reachability) evaluation: one post-order visit of a regexp
    /// tree node. NodeIndex is the post-order position (the last step is the root); Children
    /// holds the post-order indices of the node's children (empty for leaves). Operands are
    /// the child results ([] for Base, [left; right] for Alt/Seq, [child] for Star).
    [<Struct>]
    type ArroyueloBooleanTraceStep<'t, 'nt when 't: comparison and 'nt: comparison> =
        { NodeIndex: int
          Expr: Regexp<'t, 'nt>
          Operation: ArroyueloOperation
          Operands: Matrix<bool> list
          Result: Matrix<bool>
          Children: int list }

    /// State of the post-order Boolean walk: the index of the node just completed, the steps
    /// collected so far (children before parents), and that node's result matrix.
    type private BooleanLoopState<'t, 'nt when 't: comparison and 'nt: comparison> =
        { NodeIndex: int
          Steps: ArroyueloBooleanTraceStep<'t, 'nt> list
          Result: Matrix<bool> }

    /// Evaluate a regexp to a Boolean matrix, recording one trace step per AST node in
    /// post-order (children before parent; the last step is the root). The shared evaluation
    /// core used by `evalExpression` and `evaluateBooleanWithTrace`.
    let private evalBooleanWithSteps
        (graphAdj: Map<AutomatonLabel<'t>, Matrix<bool>>)
        (vCount: int)
        (regexp: Regexp<'t, 'nt>)
        : Matrix<bool> * ArroyueloBooleanTraceStep<'t, 'nt> list =
        let identity = Matrix.init vCount vCount false

        for i in 0 .. vCount - 1 do
            identity.[i, i] <- true

        let zero = Matrix.init vCount vCount false

        let rec loop (r: Regexp<'t, 'nt>) (acc: ArroyueloBooleanTraceStep<'t, 'nt> list) : BooleanLoopState<'t, 'nt> =
            match r with
            | Regexp.REps ->
                let result = identity

                let step =
                    { NodeIndex = List.length acc
                      Expr = r
                      Operation = Base
                      Operands = []
                      Result = result
                      Children = [] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: acc
                  Result = result }
            | Regexp.REmpty ->
                let step =
                    { NodeIndex = List.length acc
                      Expr = r
                      Operation = Base
                      Operands = []
                      Result = zero
                      Children = [] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: acc
                  Result = zero }
            | Regexp.RTerm(Terminal t) ->
                let result =
                    match Map.tryFind (ATerm t) graphAdj with
                    | Some m -> m
                    | None -> zero

                let step =
                    { NodeIndex = List.length acc
                      Expr = r
                      Operation = Base
                      Operands = []
                      Result = result
                      Children = [] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: acc
                  Result = result }
            | Regexp.RNonterm _ ->
                let step =
                    { NodeIndex = List.length acc
                      Expr = r
                      Operation = Base
                      Operands = []
                      Result = zero
                      Children = [] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: acc
                  Result = zero }
            | Regexp.RAlt(l, rr) ->
                let left = loop l acc
                let right = loop rr left.Steps
                let result = MsBfs.boolAdd left.Result right.Result

                let step =
                    { NodeIndex = List.length right.Steps
                      Expr = r
                      Operation = Alt
                      Operands = [ left.Result; right.Result ]
                      Result = result
                      Children = [ left.NodeIndex; right.NodeIndex ] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: right.Steps
                  Result = result }
            | Regexp.RSeq(l, rr) ->
                let left = loop l acc
                let right = loop rr left.Steps
                let result = MsBfs.boolMul left.Result right.Result

                let step =
                    { NodeIndex = List.length right.Steps
                      Expr = r
                      Operation = Seq
                      Operands = [ left.Result; right.Result ]
                      Result = result
                      Children = [ left.NodeIndex; right.NodeIndex ] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: right.Steps
                  Result = result }
            | Regexp.RStar(rp) ->
                let child = loop rp acc
                let result = MsBfs.boolAdd identity (transitiveClosure child.Result)

                let step =
                    { NodeIndex = List.length child.Steps
                      Expr = r
                      Operation = Star
                      Operands = [ child.Result ]
                      Result = result
                      Children = [ child.NodeIndex ] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: child.Steps
                  Result = result }

        let root = loop regexp []
        (root.Result, List.rev root.Steps)

    /// Evaluate a regular expression AST to a Boolean matrix.
    /// M(ε) = I, M(a) = graphAdj[a], M(a^-) = graphAdj[a]^T,
    /// M(E1 | E2) = M(E1) ∨ M(E2), M(E1 / E2) = M(E1) × M(E2),
    /// M(E+) = M(E)^+, M(E*) = I ∨ M(E)^+.
    let private evalExpression
        (graphAdj: Map<AutomatonLabel<'t>, Matrix<bool>>)
        (vCount: int)
        (regexp: Regexp<'t, 'nt>)
        : Matrix<bool> =
        fst (evalBooleanWithSteps graphAdj vCount regexp)

    /// Evaluate a regexp over the Boolean semiring, recording one trace step per AST node in
    /// post-order. Returns the steps and the full |V| x |V| Boolean result matrix. This is the
    /// classical reachability semantics (complete on cyclic graphs), as opposed to the
    /// simple-path projection of `evaluateWithTrace`.
    let evaluateBooleanWithTrace
        (graph: NFA<'t, int>)
        (regexp: Regexp<'t, 'nt>)
        : ArroyueloBooleanTraceStep<'t, 'nt> list * Matrix<bool> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.Transitions
        let vCount = Nfa.stateCount graph
        let fullMatrix, steps = evalBooleanWithSteps perLabel vCount regexp
        (steps, fullMatrix)

    /// One step of the path-semiring evaluation: one post-order visit of a regexp tree node.
    /// NodeIndex is the post-order position (the last step is the root); Children holds the
    /// post-order indices of the node's children (empty for leaves). Operands are the child
    /// results ([] for Base, [left; right] for Alt/Seq, [child] for Star).
    [<Struct>]
    type ArroyueloTraceStep<'t, 'nt when 't: comparison and 'nt: comparison> =
        { NodeIndex: int
          Expr: Regexp<'t, 'nt>
          Operation: ArroyueloOperation
          Operands: Matrix<Set<int list>> list
          Result: Matrix<Set<int list>>
          Children: int list }

    /// Convert a Boolean adjacency matrix to a path matrix: a true cell [i, j] becomes
    /// the single-edge path [i; j].
    let private boolToPathMatrix (m: Matrix<bool>) : Matrix<Set<int list>> =
        let rows = Matrix.rows m
        let cols = Matrix.cols m

        Matrix.create rows cols (fun i j -> if m.[i, j] then Set.singleton [ i; j ] else Set.empty)

    /// State of the post-order walk inside evaluateWithTrace: the index of the node just
    /// completed, the steps collected so far (children before parents), and that node's
    /// result matrix.
    type private LoopState<'t, 'nt when 't: comparison and 'nt: comparison> =
        { NodeIndex: int
          Steps: ArroyueloTraceStep<'t, 'nt> list
          Result: Matrix<Set<int list>> }

    /// Evaluate a regexp on the given graph over the path semiring, recording one trace
    /// step per AST node in post-order (children before parent; the last step is the root).
    /// Returns the steps and the full |V| x |V| result matrix. Cells hold sets of simple
    /// vertex paths, so on cyclic graphs the boolean projection may be a strict subset of
    /// the Boolean evaluate's reachability (walks that revisit vertices are excluded);
    /// on acyclic graphs the two coincide.
    let evaluateWithTrace
        (graph: NFA<'t, int>)
        (regexp: Regexp<'t, 'nt>)
        : ArroyueloTraceStep<'t, 'nt> list * Matrix<Set<int list>> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.Transitions
        let vCount = Nfa.stateCount graph

        let zeroMatrix = Matrix.init vCount vCount Set.empty

        let baseMatrix (r: Regexp<'t, 'nt>) : Matrix<Set<int list>> =
            match r with
            | REps -> PathSemiring.identity vCount
            | RTerm(Terminal t) ->
                match Map.tryFind (ATerm t) perLabel with
                | Some m -> boolToPathMatrix m
                | None -> zeroMatrix
            | _ -> zeroMatrix

        let rec loop (r: Regexp<'t, 'nt>) (acc: ArroyueloTraceStep<'t, 'nt> list) : LoopState<'t, 'nt> =
            match r with
            | REps
            | REmpty
            | RTerm _
            | RNonterm _ ->
                let result = baseMatrix r

                let step =
                    { NodeIndex = List.length acc
                      Expr = r
                      Operation = Base
                      Operands = []
                      Result = result
                      Children = [] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: acc
                  Result = result }
            | RAlt(l, rr) ->
                let left = loop l acc
                let right = loop rr left.Steps
                let result = PathSemiring.addMatrices left.Result right.Result

                let step =
                    { NodeIndex = List.length right.Steps
                      Expr = r
                      Operation = Alt
                      Operands = [ left.Result; right.Result ]
                      Result = result
                      Children = [ left.NodeIndex; right.NodeIndex ] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: right.Steps
                  Result = result }
            | RSeq(l, rr) ->
                let left = loop l acc
                let right = loop rr left.Steps
                let result = PathSemiring.mxm left.Result right.Result

                let step =
                    { NodeIndex = List.length right.Steps
                      Expr = r
                      Operation = Seq
                      Operands = [ left.Result; right.Result ]
                      Result = result
                      Children = [ left.NodeIndex; right.NodeIndex ] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: right.Steps
                  Result = result }
            | RStar(rp) ->
                let child = loop rp acc

                let result =
                    PathSemiring.addMatrices
                        (PathSemiring.identity vCount)
                        (PathSemiring.transitiveClosure child.Result)

                let step =
                    { NodeIndex = List.length child.Steps
                      Expr = r
                      Operation = Star
                      Operands = [ child.Result ]
                      Result = result
                      Children = [ child.NodeIndex ] }

                { NodeIndex = step.NodeIndex
                  Steps = step :: child.Steps
                  Result = result }

        let root = loop regexp []
        (List.rev root.Steps, root.Result)

    /// Evaluate a regexp on the given graph and return a |sources| × |V| boolean reachability matrix.
    /// Sources are taken from the NFA's start states.
    let evaluate (graph: NFA<'t, int>) (regexp: Regexp<'t, 'nt>) : Matrix<bool> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.Transitions
        let vCount = Nfa.stateCount graph
        let sources = graph.StartStates |> Set.toArray

        let fullMatrix = evalExpression perLabel vCount regexp

        let k = sources.Length
        let result = Matrix.init k vCount false

        for i in 0 .. k - 1 do
            for j in 0 .. vCount - 1 do
                result.[i, j] <- fullMatrix.[sources.[i], j]

        result
