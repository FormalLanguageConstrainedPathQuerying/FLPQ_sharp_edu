namespace FLPQ.Languages

open FSharpPlus.Data
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// An LR item over RSM: a position in an RSM block's DFA.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrItem<'nt when 'nt: comparison> =
    { blockNonterminal: Nonterminal<'nt>
      rsmState: int }

/// LR action in the RNGLR parsing table.
/// Book reference: sec:CFPQ_RNGLR.
[<RequireQualifiedAccess>]
type RnglrAction<'nt when 'nt: comparison> =
    | Shift of int
    | Reduce of Nonterminal<'nt>
    | Accept

/// RNGLR parsing table built from an RSM.
/// Action maps (automatonState, symbol) to an action.
/// Goto maps (automatonState, nonterminal) to an automaton state.
/// Book reference: sec:CFPQ_RNGLR.
type RnglrTable<'t, 'nt when 't: comparison and 'nt: comparison> =
    { action: Map<int * Symbol<'t, 'nt>, RnglrAction<'nt>>
      goto: Map<int * Nonterminal<'nt>, int>
      automaton: DFA<Symbol<'t, 'nt>, Set<RnglrItem<'nt>>> }

/// Vertex in the RNGLR Graph-Structured Stack.
/// Represents a parser position: (LR automaton state, input graph vertex).
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrGssVertex = { lrState: int; inputVertex: int }

/// Edge in the RNGLR Graph-Structured Stack.
/// Labeled with the grammar symbol that was recognized at this step.
/// Book reference: sec:CFPQ_RNGLR.
[<Struct>]
type RnglrGssEdge<'t, 'nt> = { symbol: Symbol<'t, 'nt> }

/// RNGLR Graph-Structured Stack — a labeled directed graph encoding all parsing paths.
/// Vertices are (lrState, inputVertex) pairs, pre-allocated as |Q_lr| * |V|.
/// Edges carry the recognized grammar symbol.
/// storedStates[i] holds cached intermediate automaton intersection states:
/// Set of (nonterminal, invState) pairs for the product construction.
/// When a shift creates a new edge from a GSS vertex, its storedStates are consumed and
/// each (nt, invState) pair is continued via product BFS through the inverted RSM block of nt.
/// Book reference: sec:CFPQ_RNGLR.
type RnglrGSS<'t, 'nt when 't: comparison and 'nt: comparison> =
    { graph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      storedStates: Set<Nonterminal<'nt> * int> array }

module RnglrGSS =

    /// Maps (lrState, inputVertex) to a linear index in the GSS.
    let linearIndex (vertexCount: int) (lrState: int) (inputVertex: int) : int = lrState * vertexCount + inputVertex

    /// Pre-allocates the RNGLR GSS with all |Q_lr| * |V| vertices and an empty edge matrix.
    let init (lrStateCount: int) (vertexCount: int) : RnglrGSS<'t, 'nt> =
        let k = lrStateCount * vertexCount

        let vertices =
            [ for s in 0 .. lrStateCount - 1 do
                  for v in 0 .. vertexCount - 1 -> { lrState = s; inputVertex = v } ]

        let edges = Matrix.init k k None

        { graph = Graph.fromEdges vertices edges
          storedStates = Array.create k Set.empty }

    /// Adds an edge from source GSS vertex to target GSS vertex.
    /// Returns the storedStates from the source vertex, clearing them.
    let addEdge
        (gss: RnglrGSS<'t, 'nt>)
        (fromIdx: int)
        (toIdx: int)
        (label: Symbol<'t, 'nt>)
        : Set<Nonterminal<'nt> * int> =
        let edge = { symbol = label }

        let current =
            match Matrix.get gss.graph.Edges fromIdx toIdx with
            | Some nes -> NonEmptySet.add edge nes
            | None -> NonEmptySet.singleton edge

        Matrix.set gss.graph.Edges fromIdx toIdx (Some current)

        let states = gss.storedStates.[fromIdx]
        gss.storedStates.[fromIdx] <- Set.empty

        states

    /// Returns the stored intermediate intersection states for a GSS vertex without clearing them.
    let getStoredStates (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : Set<Nonterminal<'nt> * int> = gss.storedStates.[gssIdx]

    /// Sets the stored intermediate intersection states for a GSS vertex.
    let setStoredStates (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) (states: Set<Nonterminal<'nt> * int>) : unit =
        gss.storedStates.[gssIdx] <- states

    /// Returns all outgoing edges from a GSS vertex as (targetIdx, symbol) pairs.
    let outgoingEdges (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : (int * Symbol<'t, 'nt>) list =
        let n = gss.graph.VertexMap.Count

        [ for toIdx in 0 .. n - 1 do
              match Matrix.get gss.graph.Edges gssIdx toIdx with
              | Some nes ->
                  for edge in NonEmptySet.toSeq nes do
                      (toIdx, edge.symbol)
              | None -> () ]
