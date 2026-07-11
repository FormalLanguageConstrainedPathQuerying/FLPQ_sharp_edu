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
