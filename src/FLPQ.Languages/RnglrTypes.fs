namespace FLPQ.Languages

open System.Collections.Generic
open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// An LR item over RSM: a position in an RSM block's DFA.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrItem<'nt when 'nt: comparison> =
    { BlockNonterminal: Nonterminal<'nt>
      RsmState: int }

/// RNGLR descriptor — a parsing position (LR automaton state, input graph vertex, GSS vertex).
/// Carries explicit GssIdx reference to the GSS vertex, matching GLL's descriptor structure.
/// Range tracking is handled by the product BFS (storedStates mechanism), not carried in the descriptor.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrDescriptor =
    { LrState: int
      Vertex: int
      GssIdx: int }

/// RNGLR parsing table built from an RSM.
/// Action maps (automatonState, symbol) to an LR action.
/// Goto maps (automatonState, nonterminal) to an automaton state.
/// Book reference: sec:CFPQ_RNGLR.
type RnglrTable<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Action: Map<int * Symbol<'t, 'nt>, LRAction<Nonterminal<'nt>>>
      Goto: Map<int * Nonterminal<'nt>, int>
      Automaton: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>> }

/// Edge in the RNGLR Graph-Structured Stack.
/// Labeled with the grammar symbol that was recognized at this step.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrGssEdge<'t, 'nt> = { EdgeSymbol: Symbol<'t, 'nt> }

/// RNGLR Graph-Structured Stack — a labeled directed graph encoding all parsing paths.
/// Vertices are created lazily on-demand with sequential IDs (0, 1, 2, ...).
/// Edges carry the recognized grammar symbol.
/// storedStates[gssIdx] holds cached intermediate automaton intersection states:
/// Set of (nonterminal, invState, rangeEndState, rangeEndVertex) tuples for the product construction.
/// rangeEndState and rangeEndVertex identify the block's final state and its vertex position,
/// propagated through the BFS to enable correct PIntermediate entry placement at each step.
/// When a shift creates a new edge from a GSS vertex, its storedStates are consumed and
/// each tuple is continued via product BFS through the inverted RSM block of nt.
/// Book reference: sec:CFPQ_RNGLR.
type RnglrGSS<'t, 'nt when 't: comparison and 'nt: comparison> =
    { VertexLookup: Dictionary<int * int, int>
      VertexInfo: ResizeArray<int * int>
      Edges: Dictionary<int, Dictionary<int, NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      StoredStates: Dictionary<int, Set<Nonterminal<'nt> * int * int * int>> }

module RnglrGSS =

    /// Creates an empty RNGLR GSS with no pre-allocated vertices.
    let create () : RnglrGSS<'t, 'nt> =
        { VertexLookup = Dictionary<int * int, int>()
          VertexInfo = ResizeArray<int * int>()
          Edges = Dictionary<int, Dictionary<int, NonEmptySet<RnglrGssEdge<'t, 'nt>>>>()
          StoredStates = Dictionary<int, Set<Nonterminal<'nt> * int * int * int>>() }

    /// Returns the GSS vertex ID for (lrState, inputVertex), creating it if it does not exist.
    let getOrCreateVertex (gss: RnglrGSS<'t, 'nt>) (lrState: int) (inputVertex: int) : int =
        let key = (lrState, inputVertex)

        match gss.VertexLookup.TryGetValue(key) with
        | true, idx -> idx
        | false, _ ->
            let idx = gss.VertexInfo.Count
            gss.VertexInfo.Add(key)
            gss.VertexLookup.[key] <- idx
            idx

    /// Returns the (lrState, inputVertex) pair for a GSS vertex ID.
    let getVertexInfo (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : int * int = gss.VertexInfo.[gssIdx]

    /// Adds an edge from source GSS vertex to target GSS vertex.
    /// Returns the storedStates from the source vertex, clearing them.
    let addEdge
        (gss: RnglrGSS<'t, 'nt>)
        (fromIdx: int)
        (toIdx: int)
        (label: Symbol<'t, 'nt>)
        : Set<Nonterminal<'nt> * int * int * int> =
        let edge = { EdgeSymbol = label }

        let targets =
            match gss.Edges.TryGetValue(fromIdx) with
            | true, d -> d
            | false, _ ->
                let d = Dictionary<int, NonEmptySet<RnglrGssEdge<'t, 'nt>>>()
                gss.Edges.[fromIdx] <- d
                d

        let current =
            match targets.TryGetValue(toIdx) with
            | true, nes -> NonEmptySet.add edge nes
            | false, _ -> NonEmptySet.singleton edge

        targets.[toIdx] <- current

        let states =
            match gss.StoredStates.TryGetValue(fromIdx) with
            | true, s -> s
            | false, _ -> Set.empty

        gss.StoredStates.[fromIdx] <- Set.empty

        states

    /// Returns the stored intermediate intersection states for a GSS vertex without clearing them.
    let getStoredStates (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : Set<Nonterminal<'nt> * int * int * int> =
        match gss.StoredStates.TryGetValue(gssIdx) with
        | true, s -> s
        | false, _ -> Set.empty

    /// Sets the stored intermediate intersection states for a GSS vertex.
    let setStoredStates
        (gss: RnglrGSS<'t, 'nt>)
        (gssIdx: int)
        (states: Set<Nonterminal<'nt> * int * int * int>)
        : unit =
        gss.StoredStates.[gssIdx] <- states

    /// Returns all outgoing edges from a GSS vertex as (targetIdx, symbol) pairs.
    let outgoingEdges (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : (int * Symbol<'t, 'nt>) list =
        match gss.Edges.TryGetValue(gssIdx) with
        | true, targets ->
            [ for kv in targets do
                  for edge in NonEmptySet.toSeq kv.Value do
                      (kv.Key, edge.EdgeSymbol) ]
        | false, _ -> []

/// A single step snapshot during RNGLR execution.
/// Captures per-vertex pending queues, active GSS elements, path index state,
/// LR automaton position, and descriptor accounting.
type RnglrParsingStep<'t, 'nt when 't: comparison and 'nt: comparison> =
    { PendingQueues: RnglrDescriptor list[]
      ActiveGssVertices: Set<int>
      ActiveGssEdges: Set<int * int>
      ActiveGssEdgeSymbols: Map<int * int, NonEmptySet<Symbol<'t, 'nt>>>
      NewGssVertices: Set<int>
      NewGssEdges: Set<int * int>
      PathIndexMatrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      ChangedCells: Set<int * int>
      InputVertex: int
      CurrentLrState: int option
      CurrentDescriptor: RnglrDescriptor option
      HandledDescriptors: Set<RnglrDescriptor>
      NewDescriptors: Set<RnglrDescriptor>
      AttemptedDescriptors: Set<RnglrDescriptor>
      ActiveShiftTerminals: Set<Terminal<'t>>
      ActiveReduceNonterminals: Set<Nonterminal<'nt>>
      LevelReductions: Set<Nonterminal<'nt>> }

/// Result of RNGLR path index construction with step-by-step visualization data.
[<Struct>]
type RnglrResult<'t, 'nt when 't: comparison and 'nt: comparison> =
    { PathIndex: PathIndex<'t, 'nt>
      Steps: RnglrParsingStep<'t, 'nt> list
      VertexInfo: ResizeArray<int * int> }
