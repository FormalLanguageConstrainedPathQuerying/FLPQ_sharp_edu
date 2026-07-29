namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// An LR item over RSM: a position in an RSM block's DFA.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrItem<'nt when 'nt: comparison> =
    { BlockNonterminal: Nonterminal<'nt>
      RsmState: int }

/// RNGLR descriptor — a parsing position (LR automaton state, input graph vertex)
/// in the working set. Unlike GLL descriptors, the GSS vertex is derivable from
/// (LrState, Vertex) via lrState * vertexCount + vertex, and range tracking is
/// handled by the product BFS (storedStates mechanism), not carried in the descriptor.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrDescriptor = { LrState: int; Vertex: int }

/// RNGLR parsing table built from an RSM.
/// Action maps (automatonState, symbol) to an LR action.
/// Goto maps (automatonState, nonterminal) to an automaton state.
/// Book reference: sec:CFPQ_RNGLR.
type RnglrTable<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Action: Map<int * Symbol<'t, 'nt>, LRAction<Nonterminal<'nt>>>
      Goto: Map<int * Nonterminal<'nt>, int>
      Automaton: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>> }

/// Vertex in the RNGLR Graph-Structured Stack.
/// Represents a parser position: (LR automaton state, input graph vertex).
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrGssVertex = { LrState: int; InputVertex: int }

/// Edge in the RNGLR Graph-Structured Stack.
/// Labeled with the grammar symbol that was recognized at this step.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrGssEdge<'t, 'nt> = { EdgeSymbol: Symbol<'t, 'nt> }

/// RNGLR Graph-Structured Stack — a labeled directed graph encoding all parsing paths.
/// Vertices are (lrState, inputVertex) pairs, pre-allocated as |Q_lr| * |V|.
/// Edges carry the recognized grammar symbol.
/// storedStates[i] holds cached intermediate automaton intersection states:
/// Set of (nonterminal, invState, rangeEndState, rangeEndVertex) tuples for the product construction.
/// rangeEndState and rangeEndVertex identify the block's final state and its vertex position,
/// propagated through the BFS to enable correct PIntermediate entry placement at each step.
/// When a shift creates a new edge from a GSS vertex, its storedStates are consumed and
/// each tuple is continued via product BFS through the inverted RSM block of nt.
/// Book reference: sec:CFPQ_RNGLR.
type RnglrGSS<'t, 'nt when 't: comparison and 'nt: comparison> =
    { GssGraph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      StoredStates: Set<Nonterminal<'nt> * int * int * int> array }

module RnglrGSS =

    /// Maps (lrState, inputVertex) to a linear index in the GSS.
    let linearIndex (vertexCount: int) (lrState: int) (inputVertex: int) : int =
        GridIndex.linearIndex vertexCount lrState inputVertex

    /// Pre-allocates the RNGLR GSS with all |Q_lr| * |V| vertices and an empty edge matrix.
    let init (lrStateCount: int) (vertexCount: int) : RnglrGSS<'t, 'nt> =
        let k = lrStateCount * vertexCount

        let vertices =
            [ for s in 0 .. lrStateCount - 1 do
                  for v in 0 .. vertexCount - 1 -> { LrState = s; InputVertex = v } ]

        let edges = Matrix.init k k None

        { GssGraph = Graph.fromEdges vertices edges
          StoredStates = Array.create k Set.empty }

    /// Adds an edge from source GSS vertex to target GSS vertex.
    /// Returns the storedStates from the source vertex, clearing them.
    let addEdge
        (gss: RnglrGSS<'t, 'nt>)
        (fromIdx: int)
        (toIdx: int)
        (label: Symbol<'t, 'nt>)
        : Set<Nonterminal<'nt> * int * int * int> =
        let edge = { EdgeSymbol = label }

        let current =
            match gss.GssGraph.Edges.[fromIdx, toIdx] with
            | Some nes -> NonEmptySet.add edge nes
            | None -> NonEmptySet.singleton edge

        gss.GssGraph.Edges.[fromIdx, toIdx] <- Some current

        let states = gss.StoredStates.[fromIdx]
        gss.StoredStates.[fromIdx] <- Set.empty

        states

    /// Returns the stored intermediate intersection states for a GSS vertex without clearing them.
    let getStoredStates (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : Set<Nonterminal<'nt> * int * int * int> =
        gss.StoredStates.[gssIdx]

    /// Sets the stored intermediate intersection states for a GSS vertex.
    let setStoredStates
        (gss: RnglrGSS<'t, 'nt>)
        (gssIdx: int)
        (states: Set<Nonterminal<'nt> * int * int * int>)
        : unit =
        gss.StoredStates.[gssIdx] <- states

    /// Returns all outgoing edges from a GSS vertex as (targetIdx, symbol) pairs.
    let outgoingEdges (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : (int * Symbol<'t, 'nt>) list =
        let n = gss.GssGraph.VertexMap.Count

        [ for toIdx in 0 .. n - 1 do
              match gss.GssGraph.Edges.[gssIdx, toIdx] with
              | Some nes ->
                  for edge in NonEmptySet.toSeq nes do
                      (toIdx, edge.EdgeSymbol)
              | None -> () ]

/// A single step snapshot during RNGLR execution.
/// Captures per-vertex pending queues, active GSS elements, path index state,
/// LR automaton position, and descriptor accounting.
type RnglrParsingStep<'t, 'nt when 't: comparison and 'nt: comparison> =
    { PendingQueues: RnglrDescriptor list[]
      ActiveGssVertices: Set<int>
      ActiveGssEdges: Set<int * int>
      NewGssVertices: Set<int>
      NewGssEdges: Set<int * int>
      PathIndexMatrix: Matrix<Set<PathIndexEntry<'t, 'nt>>>
      ChangedCells: Set<int * int>
      InputVertex: int
      CurrentLrState: int option
      CurrentDescriptor: RnglrDescriptor option
      HandledDescriptors: Set<RnglrDescriptor>
      NewDescriptors: Set<RnglrDescriptor>
      AttemptedDescriptors: Set<RnglrDescriptor> }
