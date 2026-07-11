namespace FLPQ.Languages

open System.Collections.Generic
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// SPPF node — variant for different types of nodes in the Shared Packed Parse Forest.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex), paper DAMDID_GLL_CFPQ/sections/gll.tex.
[<RequireQualifiedAccess>]
type SppfNodeInfo<'t, 'nt when 't: comparison and 'nt: comparison> =
    | SppfTerminal of Terminal<'t> * leftPos: int * rightPos: int
    | SppfNonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int
    | SppfEpsilon of pos: int
    | SppfIntermediate of state: int * pos: int
    | SppfRange of fromState: int * fromPos: int * toState: int * toPos: int

/// Edge labels in the SPPF graph encoding the tree/forest structure.
/// Between a specific (parent, child) pair there cannot be two edges of different types.
[<RequireQualifiedAccess>]
type SppfEdgeLabel =
    | SingleChild
    | LeftChild
    | RightChild
    | PackedAlternative

/// Shared Packed Parse Forest — a graph encoding all possible parse trees for a given input.
/// Built once from the path index after GLL execution.
/// rootIndices points to SppfRange nodes corresponding to ranges of interest.
type SPPF<'t, 'nt when 't: comparison and 'nt: comparison> =
    { Graph: Graph<SppfNodeInfo<'t, 'nt>, Option<SppfEdgeLabel>>
      RootIndices: int list }

/// SPPF construction and traversal functions.
module Sppf =

    /// Builds the SPPF from a path index starting from the given root ranges.
    /// Top-down traversal with memoization: range nodes are created once and reused for packed alternatives.
    /// Each range is processed exactly once to avoid infinite recursion.
    /// Book reference: sec:CFPQ_GLL.
    let buildSppfFromIndex (pathIndex: PathIndex<'t, 'nt>) (rootRanges: RangeKey list) : SPPF<'t, 'nt> =
        let mutable vertices: SppfNodeInfo<'t, 'nt> list = []
        let mutable edgeList: (int * Option<SppfEdgeLabel> * int) list = []

        let rangeNodeMap = Dictionary<RangeKey, int>()
        let rangeResultMap = Dictionary<RangeKey, int>()

        let terminalNodeMap = Dictionary<Terminal<'t> * int * int, int>()
        let nonterminalNodeMap = Dictionary<Nonterminal<'nt> * int * int, int>()
        let epsilonNodeMap = Dictionary<int, int>()
        let intermediateNodeMap = Dictionary<int * int, int>()

        let getOrCreateNode (info: SppfNodeInfo<'t, 'nt>) : int =
            match info with
            | SppfNodeInfo.SppfTerminal(t, l, r) ->
                let key = (t, l, r)

                match terminalNodeMap.TryGetValue(key) with
                | true, idx -> idx
                | false, _ ->
                    let idx = vertices.Length
                    vertices <- vertices @ [ info ]
                    terminalNodeMap.[key] <- idx
                    idx

            | SppfNodeInfo.SppfNonterminal(nt, l, r) ->
                let key = (nt, l, r)

                match nonterminalNodeMap.TryGetValue(key) with
                | true, idx -> idx
                | false, _ ->
                    let idx = vertices.Length
                    vertices <- vertices @ [ info ]
                    nonterminalNodeMap.[key] <- idx
                    idx

            | SppfNodeInfo.SppfEpsilon p ->
                match epsilonNodeMap.TryGetValue(p) with
                | true, idx -> idx
                | false, _ ->
                    let idx = vertices.Length
                    vertices <- vertices @ [ info ]
                    epsilonNodeMap.[p] <- idx
                    idx

            | SppfNodeInfo.SppfIntermediate(s, p) ->
                let key = (s, p)

                match intermediateNodeMap.TryGetValue(key) with
                | true, idx -> idx
                | false, _ ->
                    let idx = vertices.Length
                    vertices <- vertices @ [ info ]
                    intermediateNodeMap.[key] <- idx
                    idx

            | SppfNodeInfo.SppfRange _ -> failwith "SppfRange must use getOrCreateRangeNode"

        let getOrCreateRangeNode (fromState: int) (fromPos: int) (toState: int) (toPos: int) : int =
            let rk =
                { FromState = fromState
                  FromVertex = fromPos
                  ToState = toState
                  ToVertex = toPos }

            match rangeNodeMap.TryGetValue(rk) with
            | true, idx -> idx
            | false, _ ->
                let idx = vertices.Length
                let info = SppfNodeInfo.SppfRange(fromState, fromPos, toState, toPos)
                vertices <- vertices @ [ info ]
                rangeNodeMap.[rk] <- idx
                idx

        let addEdge (fromIdx: int) (label: SppfEdgeLabel) (toIdx: int) =
            let lbl = Some label

            if not (List.exists (fun (f, l, t) -> f = fromIdx && l = lbl && t = toIdx) edgeList) then
                edgeList <- (fromIdx, lbl, toIdx) :: edgeList

        let rec processRange (fromState: int) (fromPos: int) (toState: int) (toPos: int) : int =
            let rk =
                { FromState = fromState
                  FromVertex = fromPos
                  ToState = toState
                  ToVertex = toPos }

            match rangeResultMap.TryGetValue(rk) with
            | true, cachedIdx -> cachedIdx
            | false, _ ->
                let rangeIdx = getOrCreateRangeNode fromState fromPos toState toPos

                let entries = PathIndex.get pathIndex fromState fromPos toState toPos

                let mutable nonterminalNodeIdx: int option = None

                for entry in entries do
                    match entry with
                    | PathIndexEntry.PTerminal t ->
                        let termNode = getOrCreateNode (SppfNodeInfo.SppfTerminal(t, fromPos, toPos))

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative termNode

                    | PathIndexEntry.PNonterminal nt ->
                        let ntNode = getOrCreateNode (SppfNodeInfo.SppfNonterminal(nt, fromPos, toPos))
                        addEdge ntNode SppfEdgeLabel.SingleChild rangeIdx
                        nonterminalNodeIdx <- Some ntNode

                    | PathIndexEntry.PEpsilonNonterminal _ ->
                        let epsNode = getOrCreateNode (SppfNodeInfo.SppfEpsilon fromPos)
                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative epsNode

                    | PathIndexEntry.PIntermediate(state, pos) ->
                        let interNode = getOrCreateNode (SppfNodeInfo.SppfIntermediate(state, pos))
                        let leftChild = processRange fromState fromPos state pos
                        let rightChild = processRange state pos toState toPos

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative interNode
                        addEdge interNode SppfEdgeLabel.LeftChild leftChild
                        addEdge interNode SppfEdgeLabel.RightChild rightChild

                let resultIdx =
                    match nonterminalNodeIdx with
                    | Some ntIdx -> ntIdx
                    | None -> rangeIdx

                rangeResultMap.[rk] <- resultIdx
                resultIdx

        let rootIndices =
            rootRanges
            |> List.map (fun rk ->
                let idx = processRange rk.FromState rk.FromVertex rk.ToState rk.ToVertex
                idx)

        let n = vertices.Length
        let edgeMatrix = Matrix.init n n None

        let sortedEdges = List.rev edgeList

        for (fromIdx, label, toIdx) in sortedEdges do
            Matrix.set edgeMatrix fromIdx toIdx label

        { Graph = Graph.fromEdges vertices edgeMatrix
          RootIndices = rootIndices }

    /// Extracts a single derivation tree from an SPPF root.
    /// For ambiguous grammars, picks the first alternative at each packed node.
    /// Uses a visited set to break cycles in the Nonterminal-SingleChild-Range-PackedAlternative loop.
    /// Book reference: sec:CFPQ_GLL.
    let extractDerivationTreeFromSppf (sppf: SPPF<'t, 'nt>) (rootIdx: int) : DerivationTree<'t, 'nt> =
        let vertexCount = Graph.vertexCount sppf.Graph

        let allEdgesTo (nodeIdx: int) (predicate: Option<SppfEdgeLabel> -> bool) : int list =
            [ for toIdx in 0 .. vertexCount - 1 do
                  let edgeLabel = Matrix.get sppf.Graph.Edges nodeIdx toIdx

                  if predicate edgeLabel then
                      toIdx ]

        let rec edgeTo (nodeIdx: int) (predicate: Option<SppfEdgeLabel> -> bool) : int option =
            let mutable result = None

            for toIdx in 0 .. vertexCount - 1 do
                match result with
                | Some _ -> ()
                | None ->
                    let edgeLabel = Matrix.get sppf.Graph.Edges nodeIdx toIdx

                    if predicate edgeLabel then
                        result <- Some toIdx

            result

        let rec extractChildren (visited: HashSet<int>) (nodeIdx: int) : DerivationTree<'t, 'nt> list =
            let info = Graph.getVertex nodeIdx sppf.Graph

            let isRange =
                match info with
                | SppfNodeInfo.SppfRange _ -> true
                | _ -> false

            if not isRange && not (visited.Add(nodeIdx)) then
                []
            else
                match info with
                | SppfNodeInfo.SppfTerminal(t, _, _) -> [ Leaf(Symbol.T t) ]
                | SppfNodeInfo.SppfEpsilon _ -> [ Leaf Symbol.Epsilon ]
                | SppfNodeInfo.SppfNonterminal(nt, _, _) ->
                    match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.SingleChild) with
                    | Some rangeIdx ->
                        let alternatives =
                            allEdgesTo rangeIdx (fun e -> e = Some SppfEdgeLabel.PackedAlternative)

                        match List.tryHead alternatives with
                        | Some childIdx ->
                            let children = extractChildren visited childIdx
                            [ Node(nt, children) ]
                        | None -> [ Node(nt, []) ]
                    | None -> [ Node(nt, []) ]
                | SppfNodeInfo.SppfIntermediate _ ->
                    let left =
                        match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.LeftChild) with
                        | Some idx -> extractChildren visited idx
                        | None -> []

                    let right =
                        match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.RightChild) with
                        | Some idx -> extractChildren visited idx
                        | None -> []

                    left @ right
                | SppfNodeInfo.SppfRange _ ->
                    match edgeTo nodeIdx (fun e -> e = Some SppfEdgeLabel.PackedAlternative) with
                    | Some childIdx -> extractChildren visited childIdx
                    | None -> [ Leaf Symbol.Epsilon ]

        extractChildren (HashSet<int>()) rootIdx
        |> List.tryHead
        |> Option.defaultValue (Leaf Symbol.Epsilon)
