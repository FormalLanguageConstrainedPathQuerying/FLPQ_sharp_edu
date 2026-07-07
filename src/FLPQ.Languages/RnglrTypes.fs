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
/// storedReductions[i] holds cached reduction results for GSS vertex i:
/// for each nonterminal N, the set of (lrStatePre, gotoTarget, vPre) tuples
/// representing reductions from predecessor (lrStatePre, vPre) to (gotoTarget, currentV).
/// Book reference: sec:CFPQ_RNGLR.
type RnglrGSS<'t, 'nt when 't: comparison and 'nt: comparison> =
    { graph: Graph<RnglrGssVertex, Option<NonEmptySet<RnglrGssEdge<'t, 'nt>>>>
      storedReductions: Map<Nonterminal<'nt>, Set<int * int * int>> array }

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
        let pops = Array.create k Map.empty

        { graph = Graph.fromEdges vertices edges
          storedReductions = pops }

    /// Adds an edge from source GSS vertex to target GSS vertex.
    /// Returns the storedReductions from the source vertex (already recognized reductions from this vertex).
    let addEdge
        (gss: RnglrGSS<'t, 'nt>)
        (fromIdx: int)
        (toIdx: int)
        (label: Symbol<'t, 'nt>)
        : Map<Nonterminal<'nt>, Set<int * int * int>> =
        let edge = { symbol = label }

        let current =
            match Matrix.get gss.graph.edges fromIdx toIdx with
            | Some nes -> NonEmptySet.add edge nes
            | None -> NonEmptySet.singleton edge

        Matrix.set gss.graph.edges fromIdx toIdx (Some current)

        let reductions = gss.storedReductions.[fromIdx]
        gss.storedReductions.[fromIdx] <- Map.empty
        reductions

    /// Returns the stored reductions for a GSS vertex without clearing them.
    let getStoredReductions (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : Map<Nonterminal<'nt>, Set<int * int * int>> =
        gss.storedReductions.[gssIdx]

    /// Sets the stored reductions for a GSS vertex.
    let setStoredReductions
        (gss: RnglrGSS<'t, 'nt>)
        (gssIdx: int)
        (reductions: Map<Nonterminal<'nt>, Set<int * int * int>>)
        : unit =
        gss.storedReductions.[gssIdx] <- reductions

    /// Returns all outgoing edges from a GSS vertex as (targetIdx, symbol) pairs.
    let outgoingEdges (gss: RnglrGSS<'t, 'nt>) (gssIdx: int) : (int * Symbol<'t, 'nt>) list =
        let n = gss.graph.vertexMap.Count

        [ for toIdx in 0 .. n - 1 do
              match Matrix.get gss.graph.edges gssIdx toIdx with
              | Some nes ->
                  for edge in NonEmptySet.toSeq nes do
                      (toIdx, edge.symbol)
              | None -> () ]
