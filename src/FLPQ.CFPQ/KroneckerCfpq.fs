namespace FLPQ.CFPQ

open FSharpPlus.Data
open FLPQ.CFPQ.CfpqInput
open FLPQ.GraphAnalysis
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// Kronecker-product-based CFPQ algorithm, straightforward version.
/// Book: Chapter 12, 02_TensorProduct.tex, listing lst:algo1 — the transitive closure of the
/// product graph is replaced by an MS-BFS from every block start state, since only
/// reachability from start states is needed.
/// Main loop: Kronecker product of the RSM transition matrix and the current edge-label matrix
/// → accumulate into the previously accumulated product → MS-BFS from (block start × vertex)
/// sources → extract newly derived nonterminal edges → add them to the label matrix; repeat
/// until an iteration adds no new edges.
module KroneckerCfpq =

    /// A productive fixed-point iteration of the Kronecker loop. Besides the cells that changed
    /// and the per-nonterminal matrix state after the iteration (as in `AzimovCfpq.CfpqStep`), it
    /// carries the per-iteration data for step-by-step visualization: the Kronecker product
    /// operands and result, and the MS-BFS initial/final fronts.
    type CfpqKronStep<'nt, 't when 'nt: comparison and 't: comparison> =
        {
            Iteration: int
            ChangedCells: AzimovCfpq.CfpqCell<'nt> list
            Matrices: Map<Nonterminal<'nt>, Matrix<bool option>>
            /// The RSM transition matrix (constant across iterations) — Kronecker operand 1.
            M1: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>>
            /// The current edge-label matrix (terminals plus nonterminals derived so far) —
            /// Kronecker operand 2; a fresh copy per step.
            M2: Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>>
            /// The Kronecker product result: the Boolean product-graph adjacency mask (qn × qn).
            K: Matrix<bool>
            /// The MS-BFS initial (seed) front: a 1 at each (block start, vertex) source pair.
            InitialFront: Matrix<bool>
            /// The MS-BFS final front: the visited matrix (reachability from every source).
            FinalFront: Matrix<bool>
            /// The product-state columns that are (final state f, vertex v) pairs — highlighted in
            /// the final front so the accept-reading of the MS-BFS result is visible.
            FinalStateCols: Set<int>
        }

    /// The result of the Kronecker CFPQ algorithm: the final reachability matrix per nonterminal,
    /// the productive steps in iteration order, and the total number of loop applications
    /// (including the final no-change one).
    type CfpqKronResult<'nt, 't when 'nt: comparison and 't: comparison> =
        { Matrices: Map<Nonterminal<'nt>, Matrix<bool>>
          Steps: CfpqKronStep<'nt, 't> list
          Iterations: int }

    /// Check whether a cell of the edge-label matrix carries the given label.
    let private hasLabel
        (label: AutomatonLabel<RsmSymbol<'t, 'nt>>)
        (cell: Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>)
        : bool =
        match cell with
        | Some nes -> NonEmptySet.contains label nes
        | None -> false

    /// Add a label to a cell of the edge-label matrix.
    let private addLabel
        (label: AutomatonLabel<RsmSymbol<'t, 'nt>>)
        (cell: Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>)
        : Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>> =
        match cell with
        | Some nes -> Some(NonEmptySet.add label nes)
        | None -> Some(NonEmptySet.singleton label)

    /// Lift the terminal edges of a CFPQ graph into an edge-label matrix: one ATerm(RTerm(t))
    /// label per labeled edge. A cell may carry several terminal labels (e.g. both a and b on the
    /// same edge), so labels are accumulated rather than overwritten.
    let private graphToM2 (graph: CfpqGraph<'t>) : Matrix<Option<NonEmptySet<AutomatonLabel<RsmSymbol<'t, 'nt>>>>> =
        let n = graph.VertexCount
        let m = Matrix.init n n None

        for label, adj in Map.toList graph.Labels do
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    if adj.[i, j] then
                        m.[i, j] <- addLabel (AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal label))) m.[i, j]

        m

    /// Kronecker-product-based CFPQ (book Chapter 12, listing lst:algo1 with the transitive closure
    /// replaced by MS-BFS). Precondition: the RSM has no AEpsilon transitions (true for all
    /// RsmBuilder output); epsilon reachability falls out of the MS-BFS source initialization.
    let evaluate (rsm: RSM<'t, 'nt>) (graph: CfpqGraph<'t>) : CfpqKronResult<'nt, 't> =
        let n = graph.VertexCount
        let q = RSM.stateCount rsm

        /// Product-state index: the pair (RSM state s, graph vertex v) is row/column s * n + v.
        let idx (s: int) (v: int) = s * n + v

        let mutable m2 = graphToM2 graph
        let mutable prod = Matrix.init (q * n) (q * n) false
        let nts = RSM.nonterminals rsm
        let ntIndex = Map.ofList (List.mapi (fun i a -> (a, i)) nts)
        let finals = RSM.blockFinalsMap rsm

        /// MS-BFS sources: for every block A and every vertex v, the product state (start(A), v);
        /// the visited row of (A, i) is aIdx * n + i.
        let sources =
            [| for a in nts do
                   for v in 0 .. n - 1 -> idx rsm.BlockStart.[a] v |]

        /// The (block, vertex) source pairs in `sources` order — the decoded row labels of the
        /// MS-BFS fronts (row i of a front is the BFS for source pair i).
        let sourcePairs =
            [ for a in nts do
                  for v in 0 .. n - 1 -> (a, v) ]

        /// The MS-BFS initial (seed) front: a 1 at each source pair, 0 elsewhere. Constant across
        /// iterations (the MS-BFS always starts from the same sources).
        let initialFront =
            Matrix.init sources.Length (q * n) false
            |> (fun f ->
                for i in 0 .. sources.Length - 1 do
                    f.[i, sources.[i]] <- true

                f)

        /// The product-state columns that are (final state f, vertex v) pairs — the accept-reading
        /// columns of the MS-BFS result; independent of the nonterminal, so computed once.
        let finalStateCols =
            nts
            |> List.collect (fun a ->
                [ for f in Set.toList (Map.find a finals) do
                      for j in 0 .. n - 1 -> idx f j ])
            |> Set.ofList

        let mutable steps: CfpqKronStep<'nt, 't> list = []
        let mutable matrices: Map<Nonterminal<'nt>, Matrix<bool>> = Map.empty
        let mutable iteration = 0
        let mutable changedSomething = true

        while changedSomething do
            iteration <- iteration + 1

            // Kronecker product of the RSM transition matrix and the current edge-label matrix;
            // its boolean mask is the adjacency of the product graph.
            let k =
                LinearAlgebra.kron rsm.Transitions m2 Nfa.intersectEdgeSets None
                |> Matrix.map Option.isSome

            // Accumulate into the previously accumulated product (monotone: m2 only grows).
            prod <- MsBfs.boolAdd prod k

            // MS-BFS from every (block start, vertex) pair; the final visited matrix is the
            // reachability front.
            let visited = MsBfs.msBfsWithTrace sources prod |> snd

            // A fresh copy of the current edge-label matrix for the step's trace (m2 keeps
            // growing in later iterations; the trace must not change).
            let m2Snapshot = Matrix.copy m2

            // Extract the newly derived nonterminal edges per nonterminal.
            let mutable stepCells: AzimovCfpq.CfpqCell<'nt> list = []
            matrices <- Map.empty

            for a in nts do
                let aIdx = Map.find a ntIndex
                let fs = Map.find a finals
                let label = AutomatonLabel.ATerm(RsmSymbol.RNonterm a)
                let tA = Matrix.init n n false

                for i in 0 .. n - 1 do
                    for j in 0 .. n - 1 do
                        let reachable = fs |> Set.exists (fun f -> visited.[aIdx * n + i, idx f j])
                        tA.[i, j] <- reachable

                        if reachable && not (hasLabel label m2.[i, j]) then
                            stepCells <- { Nt = a; Row = i; Col = j } :: stepCells
                            m2.[i, j] <- addLabel label m2.[i, j]

                matrices <- Map.add a tA matrices

            if List.isEmpty stepCells then
                changedSomething <- false
            else
                let sorted =
                    stepCells
                    |> List.sortWith (fun a b -> compare (a.Nt, a.Row, a.Col) (b.Nt, b.Row, b.Col))

                let snapshot =
                    matrices
                    |> Map.map (fun _ m -> Matrix.map (fun b -> if b then Some true else None) m)

                steps <-
                    { Iteration = iteration
                      ChangedCells = sorted
                      Matrices = snapshot
                      M1 = rsm.Transitions
                      M2 = m2Snapshot
                      K = k
                      InitialFront = initialFront
                      FinalFront = visited
                      FinalStateCols = finalStateCols }
                    :: steps

        { Matrices = matrices
          Steps = List.rev steps
          Iterations = iteration }

    /// The reachability answer: the start block's matrix.
    let answer (result: CfpqKronResult<'nt, 't>) (rsm: RSM<'t, 'nt>) : Matrix<bool> =
        Map.find rsm.StartBlock result.Matrices
