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

    /// Checks that no cell in the path index contains more than one nonterminal-like entry.
    /// Returns Ok() or Error with list of violation descriptions (cell coordinates and count).
    let checkNonterminalInvariant (pi: PathIndex<'t, 'nt>) : Result<unit, string list> =
        let k = pi.StateCount * pi.VertexCount
        let mutable errors = []

        for fromIdx in 0 .. k - 1 do
            for toIdx in 0 .. k - 1 do
                let nts = filterNonterminals (Matrix.get pi.Matrix fromIdx toIdx)

                if Set.count nts > 1 then
                    let fromState = fromIdx / pi.VertexCount
                    let fromVertex = fromIdx % pi.VertexCount
                    let toState = toIdx / pi.VertexCount
                    let toVertex = toIdx % pi.VertexCount

                    let ntList = nts |> Set.toList

                    let desc =
                        ntList
                        |> List.map (fun e ->
                            match e with
                            | PathIndexEntry.PNonterminal(Nonterminal nt) -> sprintf "N(%A)" nt
                            | PathIndexEntry.PEpsilonNonterminal(Nonterminal nt) -> sprintf "epsN(%A)" nt
                            | _ -> "?")
                        |> String.concat ", "

                    let msg =
                        sprintf
                            "Cell (%d,%d)→(%d,%d) has %d nonterminal(s): %s"
                            fromState
                            fromVertex
                            toState
                            toVertex
                            ntList.Length
                            desc

                    errors <- msg :: errors

        if errors.IsEmpty then Ok() else Error(List.rev errors)
