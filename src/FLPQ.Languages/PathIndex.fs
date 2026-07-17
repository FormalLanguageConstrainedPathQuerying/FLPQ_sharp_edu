namespace FLPQ.Languages

open FLPQ.LinearAlgebra

/// A range in the path index: from (fromState, fromVertex) to (toState, toVertex).
/// Book reference: sec:CFPQ_GLL.
[<Struct>]
type RangeKey =
    { FromState: int
      FromVertex: int
      ToState: int
      ToVertex: int }

/// Describes a matched range (possibly empty) during GLL execution.
/// Book reference: sec:CFPQ_GLL.
[<RequireQualifiedAccess>]
type RangeDescriptor =
    | EmptyRange
    | NonEmptyRange of RangeKey

/// Entry stored in a path index cell — describes what was recognized in the corresponding range.
/// Book reference: sec:CFPQ_GLL.
[<RequireQualifiedAccess>]
type PathIndexEntry<'t, 'nt when 't: comparison and 'nt: comparison> =
    | PTerminal of Terminal<'t>
    | PNonterminal of Nonterminal<'nt>
    | PEpsilonNonterminal of Nonterminal<'nt>
    | PIntermediate of state: int * pos: int

/// Path index built by GLL: a K×K matrix where K = stateCount * vertexCount.
/// Cell (fromKey, toKey) stores the set of recognized entries for that range.
/// Linear index: idx(state, vertex) = state * vertexCount + vertex.
/// Book reference: sec:CFPQ_GLL.
type PathIndex<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Matrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      StateCount: int
      VertexCount: int }

module PathIndex =

    /// Maps (state, vertex) to a linear index in the PathIndex matrix.
    let linearIndex (pi: PathIndex<'t, 'nt>) (state: int) (vertex: int) : int = state * pi.VertexCount + vertex

    /// Adds an entry to the path index at range (fromState, fromVertex) → (toState, toVertex).
    let add
        (pi: PathIndex<'t, 'nt>)
        (fromState: int)
        (fromVertex: int)
        (toState: int)
        (toVertex: int)
        (entry: PathIndexEntry<'t, 'nt>)
        : unit =
        let fromIdx = linearIndex pi fromState fromVertex
        let toIdx = linearIndex pi toState toVertex
        let current = Matrix.get pi.Matrix fromIdx toIdx
        Matrix.set pi.Matrix fromIdx toIdx (Set.add entry current)

    /// Gets the set of entries at range (fromState, fromVertex) → (toState, toVertex).
    let get
        (pi: PathIndex<'t, 'nt>)
        (fromState: int)
        (fromVertex: int)
        (toState: int)
        (toVertex: int)
        : Set<PathIndexEntry<'t, 'nt>> =
        let fromIdx = linearIndex pi fromState fromVertex
        let toIdx = linearIndex pi toState toVertex
        Matrix.get pi.Matrix fromIdx toIdx

    /// Filters a set of path index entries to only nonterminal-like entries
    /// (PNonterminal and PEpsilonNonterminal).
    let filterNonterminals (entries: Set<PathIndexEntry<'t, 'nt>>) : Set<PathIndexEntry<'t, 'nt>> =
        entries
        |> Set.filter (fun e ->
            match e with
            | PathIndexEntry.PNonterminal _
            | PathIndexEntry.PEpsilonNonterminal _ -> true
            | _ -> false)

    /// Checks the callee-reachability invariant: if cell (i,p)(j,q) contains PNonterminal(A)
    /// or PEpsilonNonterminal(A), then at least one callee cell (s_A,p)(f_A,q) is non-empty,
    /// where s_A = blockStart[A] and f_A ∈ blockFinals[A].
    let checkCalleeReachabilityInvariant
        (pi: PathIndex<'t, 'nt>)
        (blockStart: Map<Nonterminal<'nt>, int>)
        (blockFinals: Map<Nonterminal<'nt>, Set<int>>)
        : Result<unit, string list> =
        let k = pi.StateCount * pi.VertexCount
        let mutable errors = []

        for fromIdx in 0 .. k - 1 do
            for toIdx in 0 .. k - 1 do
                let nts = filterNonterminals (Matrix.get pi.Matrix fromIdx toIdx)

                if not (Set.isEmpty nts) then
                    let fromState = fromIdx / pi.VertexCount
                    let fromVertex = fromIdx % pi.VertexCount
                    let toState = toIdx / pi.VertexCount
                    let toVertex = toIdx % pi.VertexCount

                    for entry in nts do
                        let nt =
                            match entry with
                            | PathIndexEntry.PNonterminal(Nonterminal n)
                            | PathIndexEntry.PEpsilonNonterminal(Nonterminal n) -> Nonterminal n
                            | _ -> failwith "unexpected"

                        match blockStart.TryGetValue(nt) with
                        | true, ntStart ->
                            match blockFinals.TryGetValue(nt) with
                            | true, ntFinals ->
                                let anyCalleeNonEmpty =
                                    ntFinals
                                    |> Set.exists (fun bf ->
                                        let entries = get pi ntStart fromVertex bf toVertex

                                        not (Set.isEmpty entries))

                                if not anyCalleeNonEmpty then
                                    let (Nonterminal ntName) = nt

                                    let msg =
                                        sprintf
                                            "Cell (%d,%d)->(%d,%d) has %A but all callee cells (%d,%d)->(f,%d) are empty"
                                            fromState
                                            fromVertex
                                            toState
                                            toVertex
                                            ntName
                                            ntStart
                                            fromVertex
                                            toVertex

                                    errors <- msg :: errors
                            | _ ->
                                let (Nonterminal ntName) = nt

                                let msg =
                                    sprintf
                                        "Cell (%d,%d)->(%d,%d) has %A but block metadata not found"
                                        fromState
                                        fromVertex
                                        toState
                                        toVertex
                                        ntName

                                errors <- msg :: errors
                        | _ ->
                            let (Nonterminal ntName) = nt

                            let msg =
                                sprintf
                                    "Cell (%d,%d)->(%d,%d) has %A but block metadata not found"
                                    fromState
                                    fromVertex
                                    toState
                                    toVertex
                                    ntName

                            errors <- msg :: errors

        if errors.IsEmpty then Ok() else Error(List.rev errors)

    /// Checks whether any state in the RSM is simultaneously a block start state and a final state.
    let internal hasStartFinalOverlap
        (blockStart: Map<Nonterminal<'nt>, int>)
        (finalStates: Set<int>)
        : bool =
        blockStart
        |> Map.exists (fun _ startState -> Set.contains startState finalStates)

    /// Checks the no-epsilon invariant: if the RSM has no states that are simultaneously
    /// start and final, then the path index must not contain PEpsilonNonterminal entries.
    let checkNoEpsilonInvariant
        (pi: PathIndex<'t, 'nt>)
        (blockStart: Map<Nonterminal<'nt>, int>)
        (finalStates: Set<int>)
        : Result<unit, string list> =
        if hasStartFinalOverlap blockStart finalStates then
            Ok()
        else
            let k = pi.StateCount * pi.VertexCount
            let mutable errors = []

            for fromIdx in 0 .. k - 1 do
                for toIdx in 0 .. k - 1 do
                    for entry in Matrix.get pi.Matrix fromIdx toIdx do
                        match entry with
                        | PathIndexEntry.PEpsilonNonterminal(Nonterminal nt) ->
                            let fromState = fromIdx / pi.VertexCount
                            let fromVertex = fromIdx % pi.VertexCount
                            let toState = toIdx / pi.VertexCount
                            let toVertex = toIdx % pi.VertexCount

                            let msg =
                                sprintf
                                    "Cell (%d,%d)->(%d,%d) contains PEpsilonNonterminal %s but no start=final overlap exists"
                                    fromState
                                    fromVertex
                                    toState
                                    toVertex
                                    nt

                            errors <- msg :: errors
                        | _ -> ()

            if errors.IsEmpty then Ok() else Error(List.rev errors)

    /// Checks if the path index indicates acceptance: whether there exists any entry
    /// in the range from the extended RSM's S' start state at vertex 0 to S' final state
    /// at the last vertex. Algorithm-independent — works for both GLL and RNGLR path indices.
    let isAccepted (pathIndex: PathIndex<'t, 'nt>) (ersm: ExtendedRSM<'t, 'nt>) (vertexCount: int) : bool =
        let flatExt = ersm.ExtendedRsm

        let startGlobalState =
            match flatExt.BlockStart.TryGetValue(flatExt.StartBlock) with
            | true, gs -> gs
            | false, _ -> 0

        let finalGlobalState = startGlobalState + 1

        let entries = get pathIndex startGlobalState 0 finalGlobalState (vertexCount - 1)

        not (Set.isEmpty entries)
