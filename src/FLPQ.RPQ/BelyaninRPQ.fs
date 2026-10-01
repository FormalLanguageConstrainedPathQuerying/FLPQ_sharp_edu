namespace FLPQ.RPQ

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.Languages

/// Belyanin's LARPQ algorithm (BFS-based single-source RPQ).
/// Book: Chapter 11, 02_BFS.tex, algorithm algo:RPQ_BFS_semiring.
/// Operates on two |Q|×|V| matrices: front M and accumulated results P.
/// Propagates simultaneously through the automaton (backward) and the graph (forward).
///
/// Two semantics are provided:
/// - reachability (Boolean): `evaluate` / `evaluateReachabilityWithTrace` — the classical
///   RPQ reachability answer, complete on cyclic graphs; no path information is collected.
/// - simplePath (path semiring): `evaluateSimplePathWithTrace` — a vertex is reachable only
///   via an accepting simple path; used for the step visualization that shows paths.
module BelyaninRPQ =

    /// DFA transition matrix for one label: N^a[i, j] is true when state i has an
    /// a-labeled transition to state j.
    let private dfaLabelMatrix (dfa: DFA<'t, int>) (label: AutomatonLabel<'t>) : Matrix<bool> =
        let qCount = Dfa.stateCount dfa
        let m = Matrix.init qCount qCount false

        for i in 0 .. qCount - 1 do
            for j in 0 .. qCount - 1 do
                match dfa.Transitions.[i, j] with
                | Some nes when NonEmptySet.contains label nes -> m.[i, j] <- true
                | _ -> ()

        m

    /// One label's Boolean propagation within a reachability iteration: N is the DFA
    /// transition matrix N^a, G is the graph edge matrix G^a, Select = (N^a)^T (x) M
    /// (automaton backward step) and Extend = Select (x) G^a (graph forward step). The
    /// matrices are kept so the trace is self-contained for rendering.
    type BelyaninReachabilityLabelStep<'t when 't: comparison> =
        { Label: AutomatonLabel<'t>
          N: Matrix<bool>
          G: Matrix<bool>
          Select: Matrix<bool>
          Extend: Matrix<bool> }

    /// One step of the Boolean (reachability) BFS-like traversal: M is the frontier after
    /// the mask (initialization: the initial M), P is the accumulated reachability
    /// (initialization: empty), Labels holds the per-label products for labels with a
    /// non-empty Select (initialization: []), and NewM is the next frontier
    /// (initialization: = M). No path information is stored.
    type BelyaninReachabilityTraceStep<'t when 't: comparison> =
        { Index: int
          IsInit: bool
          M: Matrix<bool>
          P: Matrix<bool>
          Labels: BelyaninReachabilityLabelStep<'t> list
          NewM: Matrix<bool> }

    /// Whether a Boolean matrix is false in every cell.
    let private isZeroBool (matrix: Matrix<bool>) : bool = not (Matrix.fold (||) false matrix)

    /// Run Belyanin's Boolean (reachability) traversal for a single source vertex,
    /// recording one trace step per main-loop iteration plus an initialization step. Cells
    /// are Boolean — reachable / not reachable — and no path information is collected, so
    /// the traversal is complete on cyclic graphs. Returns the recorded steps and the
    /// accumulated reachability matrix P.
    let private runReachabilityWithTrace
        (dfa: DFA<'t, int>)
        (perLabel: Map<AutomatonLabel<'t>, Matrix<bool>>)
        (source: int)
        (vCount: int)
        : BelyaninReachabilityTraceStep<'t> list * Matrix<bool> =
        let qCount = Dfa.stateCount dfa

        if vCount = 0 then
            ([], Matrix.init qCount 0 false)
        else
            let mutable m = Matrix.init qCount vCount false
            let mutable p = Matrix.init qCount vCount false

            m.[dfa.StartState, source] <- true

            let initStep =
                { Index = 0
                  IsInit = true
                  M = m
                  P = p
                  Labels = []
                  NewM = m }

            let mutable steps = [ initStep ]
            let mutable index = 1

            while not (isZeroBool m) do
                let masked = MsBfs.maskFilter m p
                p <- MsBfs.boolAdd p masked

                let mutable newM = Matrix.init qCount vCount false
                let mutable labelSteps: BelyaninReachabilityLabelStep<'t> list = []

                for KeyValue(label, gMat) in perLabel do
                    match label with
                    | AEpsilon -> ()
                    | ATerm _ ->
                        let n = dfaLabelMatrix dfa label
                        let select = MsBfs.boolMul (Matrix.transpose n) masked

                        if not (isZeroBool select) then
                            let extend = MsBfs.boolMul select gMat
                            newM <- MsBfs.boolAdd newM extend

                            labelSteps <-
                                { Label = label
                                  N = n
                                  G = gMat
                                  Select = select
                                  Extend = extend }
                                :: labelSteps

                steps <-
                    { Index = index
                      IsInit = false
                      M = masked
                      P = p
                      Labels = List.rev labelSteps
                      NewM = newM }
                    :: steps

                m <- newM
                index <- index + 1

            (List.rev steps, p)

    /// Run Belyanin's Boolean RPQ algorithm (reachability semantics) for a single source.
    let private runSingleSource
        (dfa: DFA<'t, int>)
        (perLabel: Map<AutomatonLabel<'t>, Matrix<bool>>)
        (source: int)
        (vCount: int)
        : bool[] =
        if vCount = 0 then
            [||]
        else
            let _, p = runReachabilityWithTrace dfa perLabel source vCount
            let qCount = Dfa.stateCount dfa
            let f = Matrix.init 1 qCount false

            for qf in dfa.FinalStates do
                f.[0, qf] <- true

            let result = MsBfs.boolMul f p
            [| for j in 0 .. vCount - 1 -> result.[0, j] |]

    /// Run Belyanin's RPQ algorithm (reachability semantics).
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
                result.[i, j] <- row.[j]

        result

    /// Run Belyanin's Boolean RPQ algorithm (reachability semantics) with a trace. The trace
    /// covers a single source — the first start vertex (sorted) — because the Boolean cells
    /// do not identify their origin; the result is still computed for all sources by
    /// `evaluate`. Returns the recorded steps and the accumulated reachability matrix P.
    let evaluateReachabilityWithTrace
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        : BelyaninReachabilityTraceStep<'t> list * Matrix<bool> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.Transitions
        let vCount = Nfa.stateCount graph
        let sources = graph.StartStates |> Set.toArray |> Array.sort

        if vCount = 0 || sources.Length = 0 then
            ([], Matrix.init (Dfa.stateCount dfa) vCount false)
        else
            runReachabilityWithTrace dfa perLabel sources.[0] vCount

    /// One label's propagation in a Belyanin iteration: N is the DFA transition matrix
    /// N^a, G is the graph edge matrix G^a, Select = (N^a)^T (x) M (automaton backward
    /// step) and Extend = Select (x) G^a (graph forward step). N and G are stored so the
    /// trace is self-contained for rendering the explicit per-label products.
    type BelyaninLabelStep<'t when 't: comparison> =
        { Label: AutomatonLabel<'t>
          N: Matrix<bool>
          G: Matrix<bool>
          Select: Matrix<Set<int list>>
          Extend: Matrix<Set<int list>> }

    /// One step of Belyanin's BFS-like traversal over the path semiring. M is the frontier
    /// after the mask (initialization: the initial M), P is the accumulated result
    /// (initialization: empty), Labels holds the per-label products for labels with a
    /// non-empty Select (initialization: []), and NewM is the next frontier
    /// (initialization: = M).
    type BelyaninTraceStep<'t when 't: comparison> =
        { Index: int
          IsInit: bool
          M: Matrix<Set<int list>>
          P: Matrix<Set<int list>>
          Labels: BelyaninLabelStep<'t> list
          NewM: Matrix<Set<int list>> }

    /// Path-level mask: drop from the frontier M the paths already accumulated in P.
    let private maskFilterPaths (m: Matrix<Set<int list>>) (p: Matrix<Set<int list>>) : Matrix<Set<int list>> =
        Matrix.map2 Set.difference m p

    /// Run Belyanin's RPQ algorithm over the path semiring — the simple-path semantics:
    /// a vertex is reported reachable only when an accepting *simple* path reaches it.
    /// Records one trace step per main-loop iteration (plus an initialization step). All
    /// sources run in a single traversal: M[q, v] holds the simple paths from any source to
    /// v at automaton state q; a path's head identifies its source. The mask drops exact
    /// path duplicates, so each simple path is processed once and the loop terminates in at
    /// most |V|-1 iterations. Returns the steps and the final accumulated matrix P.
    let evaluateSimplePathWithTrace
        (dfa: DFA<'t, int>)
        (graph: NFA<'t, int>)
        : BelyaninTraceStep<'t> list * Matrix<Set<int list>> =
        let perLabel = BooleanDecomposition.decomposeNonEmptySet graph.Transitions
        let vCount = Nfa.stateCount graph
        let qCount = Dfa.stateCount dfa
        let sources = graph.StartStates |> Set.toArray

        let mutable m = Matrix.init qCount vCount Set.empty
        let mutable p = Matrix.init qCount vCount Set.empty

        for s in sources do
            m.[dfa.StartState, s] <- Set.singleton [ s ]

        let initStep =
            { Index = 0
              IsInit = true
              M = m
              P = p
              Labels = []
              NewM = m }

        let mutable steps = [ initStep ]
        let mutable index = 1

        while not (PathSemiring.isEmpty m) do
            let masked = maskFilterPaths m p
            p <- PathSemiring.addMatrices p masked

            let mutable newM = Matrix.init qCount vCount Set.empty
            let mutable labelSteps: BelyaninLabelStep<'t> list = []

            for KeyValue(label, gMat) in perLabel do
                match label with
                | AEpsilon -> ()
                | ATerm _ ->
                    let n = dfaLabelMatrix dfa label
                    let select = PathSemiring.boolSelectRows n masked

                    if not (PathSemiring.isEmpty select) then
                        let extend = PathSemiring.boolExtendCols select gMat
                        newM <- PathSemiring.addMatrices newM extend

                        labelSteps <-
                            { Label = label
                              N = n
                              G = gMat
                              Select = select
                              Extend = extend }
                            :: labelSteps

            steps <-
                { Index = index
                  IsInit = false
                  M = masked
                  P = p
                  Labels = List.rev labelSteps
                  NewM = newM }
                :: steps

            m <- newM
            index <- index + 1

        (List.rev steps, p)

    /// Per-source reachable vertices from the accumulated matrix P: v is reachable from
    /// source s when some final state holds a path from s to v. Sources and vertex lists
    /// are sorted.
    let reachableFromPaths (dfa: DFA<'t, int>) (p: Matrix<Set<int list>>) (sources: int array) : (int * int list) list =
        let vCount = Matrix.cols p

        sources
        |> Array.sort
        |> Array.map (fun s ->
            let reachable =
                [ for q in dfa.FinalStates do
                      for v in 0 .. vCount - 1 do
                          if
                              p.[q, v]
                              |> Set.exists (fun path ->
                                  match path with
                                  | h :: _ -> h = s
                                  | [] -> false)
                          then
                              v ]
                |> List.distinct
                |> List.sort

            (s, reachable))
        |> Array.toList
