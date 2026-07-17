namespace FLPQ.Languages

open System.Collections.Generic
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// SPPF node — variant for different types of nodes in the Shared Packed Parse Forest.
/// Book reference: sec:CFPQ_GLL (06_GLL_Based.tex), paper DAMDID_GLL_CFPQ/sections/gll.tex.
[<RequireQualifiedAccess>]
type SppfNodeInfo<'t, 'nt when 't: comparison and 'nt: comparison> =
    | SppfTerminal of Terminal<'t> * leftPos: int * rightPos: int
    | SppfNonterminal of Nonterminal<'nt> * leftPos: int * rightPos: int * fromState: int * toState: int
    | SppfEpsilon of Nonterminal<'nt> * pos: int
    | SppfIntermediate of state: int * pos: int * fromState: int * fromPos: int * toState: int * toPos: int
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
    let buildSppfFromIndex
        (pathIndex: PathIndex<'t, 'nt>)
        (rootRanges: RangeKey list)
        (blockStart: Map<Nonterminal<'nt>, int> option)
        (blockFinals: Map<Nonterminal<'nt>, Set<int>> option)
        : SPPF<'t, 'nt> =
        let mutable vertices: SppfNodeInfo<'t, 'nt> list = []
        let mutable edgeList: (int * Option<SppfEdgeLabel> * int) list = []

        let rangeNodeMap = Dictionary<RangeKey, int>()
        let rangeResultMap = Dictionary<RangeKey, int>()

        let terminalNodeMap = Dictionary<Terminal<'t> * int * int, int>()
        let nonterminalNodeMap = Dictionary<Nonterminal<'nt> * int * int * int * int, int>()
        let epsilonNodeMap = Dictionary<int, int>()
        let intermediateNodeMap = Dictionary<int * int * int * int * int * int, int>()

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

            | SppfNodeInfo.SppfNonterminal(nt, l, r, fs, ts) ->
                let key = (nt, l, r, fs, ts)

                match nonterminalNodeMap.TryGetValue(key) with
                | true, idx -> idx
                | false, _ ->
                    let idx = vertices.Length
                    vertices <- vertices @ [ info ]
                    nonterminalNodeMap.[key] <- idx
                    idx

            | SppfNodeInfo.SppfEpsilon(_, p) ->
                match epsilonNodeMap.TryGetValue(p) with
                | true, idx -> idx
                | false, _ ->
                    let idx = vertices.Length
                    vertices <- vertices @ [ info ]
                    epsilonNodeMap.[p] <- idx
                    idx

            | SppfNodeInfo.SppfIntermediate(s, p, fs, fp, ts, tp) ->
                let key = (s, p, fs, fp, ts, tp)

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
                rangeResultMap.[rk] <- rangeIdx

                let entries = PathIndex.get pathIndex fromState fromPos toState toPos

                for entry in entries do
                    match entry with
                    | PathIndexEntry.PTerminal t ->
                        let termNode = getOrCreateNode (SppfNodeInfo.SppfTerminal(t, fromPos, toPos))

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative termNode

                    | PathIndexEntry.PNonterminal nt ->
                        let ntNode =
                            getOrCreateNode (SppfNodeInfo.SppfNonterminal(nt, fromPos, toPos, fromState, toState))

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative ntNode

                        match blockStart, blockFinals with
                        | Some bs, Some bf ->
                            match bs.TryGetValue(nt) with
                            | true, blockStartState ->
                                match bf.TryGetValue(nt) with
                                | true, finals ->
                                    for finalState in finals do
                                        let calleeEntries =
                                            PathIndex.get pathIndex blockStartState fromPos finalState toPos

                                        if not (Set.isEmpty calleeEntries) then
                                            let calleeRange = processRange blockStartState fromPos finalState toPos

                                            addEdge ntNode SppfEdgeLabel.SingleChild calleeRange
                                | _ -> ()
                            | _ -> ()
                        | _ -> ()

                    | PathIndexEntry.PEpsilonNonterminal nt ->
                        let epsNode = getOrCreateNode (SppfNodeInfo.SppfEpsilon(nt, fromPos))
                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative epsNode

                    | PathIndexEntry.PIntermediate(state, pos) ->
                        let interNode =
                            getOrCreateNode (
                                SppfNodeInfo.SppfIntermediate(state, pos, fromState, fromPos, toState, toPos)
                            )

                        addEdge rangeIdx SppfEdgeLabel.PackedAlternative interNode

                        let leftEntries = PathIndex.get pathIndex fromState fromPos state pos

                        if not (Set.isEmpty leftEntries) then
                            let leftChild = processRange fromState fromPos state pos
                            addEdge interNode SppfEdgeLabel.LeftChild leftChild

                        let rightEntries = PathIndex.get pathIndex state pos toState toPos

                        if not (Set.isEmpty rightEntries) then
                            let rightChild = processRange state pos toState toPos
                            addEdge interNode SppfEdgeLabel.RightChild rightChild

                rangeIdx

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

    let validateRangeNodesHaveChildren (sppf: SPPF<'t, 'nt>) : Result<unit, string list> =
        let vc = Graph.vertexCount sppf.Graph
        let mutable errors = []

        for i in 0 .. vc - 1 do
            match Graph.getVertex i sppf.Graph with
            | SppfNodeInfo.SppfRange(fromState, fromPos, toState, toPos) ->
                let hasChild =
                    [ for j in 0 .. vc - 1 do
                          match Matrix.get sppf.Graph.Edges i j with
                          | Some SppfEdgeLabel.PackedAlternative -> true
                          | _ -> false ]
                    |> List.contains true

                if not hasChild then
                    let msg =
                        sprintf
                            "Range node %d (SppfRange(%d,%d)→(%d,%d)) has no children"
                            i
                            fromState
                            fromPos
                            toState
                            toPos

                    errors <- msg :: errors
            | _ -> ()

        if errors.IsEmpty then Ok() else Error(List.rev errors)

    let validateIntermediateChildren (sppf: SPPF<'t, 'nt>) : Result<unit, string list> =
        let vc = Graph.vertexCount sppf.Graph
        let mutable errors = []

        for i in 0 .. vc - 1 do
            match Graph.getVertex i sppf.Graph with
            | SppfNodeInfo.SppfIntermediate _ ->
                let outgoing =
                    [ for j in 0 .. vc - 1 do
                          let lbl = Matrix.get sppf.Graph.Edges i j

                          if lbl.IsSome then
                              lbl.Value ]

                let hasLeft = outgoing |> List.contains SppfEdgeLabel.LeftChild
                let hasRight = outgoing |> List.contains SppfEdgeLabel.RightChild

                if not hasLeft then
                    errors <- sprintf "Intermediate node %d has no LeftChild edge" i :: errors

                if not hasRight then
                    errors <- sprintf "Intermediate node %d has no RightChild edge" i :: errors
            | _ -> ()

        if errors.IsEmpty then Ok() else Error(List.rev errors)

    let validateNonterminalChildren (sppf: SPPF<'t, 'nt>) : Result<unit, string list> =
        let vc = Graph.vertexCount sppf.Graph
        let mutable errors = []

        for i in 0 .. vc - 1 do
            match Graph.getVertex i sppf.Graph with
            | SppfNodeInfo.SppfNonterminal(nt, _, _, _, _) ->
                let singleChildTargets =
                    [ for j in 0 .. vc - 1 do
                          let lbl = Matrix.get sppf.Graph.Edges i j

                          if lbl = Some SppfEdgeLabel.SingleChild then
                              j ]

                if List.isEmpty singleChildTargets then
                    let (Nonterminal ntName) = nt

                    errors <-
                        sprintf "Nonterminal node %d (%s) has 0 SingleChild edge(s), expected at least 1" i ntName
                        :: errors
                else
                    for childIdx in singleChildTargets do
                        match Graph.getVertex childIdx sppf.Graph with
                        | SppfNodeInfo.SppfRange _ -> ()
                        | SppfNodeInfo.SppfEpsilon _ -> ()
                        | _ ->
                            let (Nonterminal ntName) = nt

                            let childInfo = Graph.getVertex childIdx sppf.Graph

                            errors <-
                                sprintf
                                    "Nonterminal node %d (%s) child %d is not a range or epsilon node: %A"
                                    i
                                    ntName
                                    childIdx
                                    childInfo
                                :: errors
            | _ -> ()

        if errors.IsEmpty then Ok() else Error(List.rev errors)

    let validateRangePositions (sppf: SPPF<'t, 'nt>) : Result<unit, string list> =
        let vc = Graph.vertexCount sppf.Graph
        let mutable errors = []

        for i in 0 .. vc - 1 do
            match Graph.getVertex i sppf.Graph with
            | SppfNodeInfo.SppfRange(_, fromPos, _, toPos) ->
                if fromPos > toPos then
                    errors <- sprintf "Range node %d has fromPos (%d) > toPos (%d)" i fromPos toPos :: errors
            | _ -> ()

        if errors.IsEmpty then Ok() else Error(List.rev errors)

    let validateIntermediateConnectedness (sppf: SPPF<'t, 'nt>) : Result<unit, string list> =
        let vc = Graph.vertexCount sppf.Graph
        let mutable errors = []

        let findEdgeTarget (nodeIdx: int) (label: SppfEdgeLabel) : int option =
            let mutable result = None

            for j in 0 .. vc - 1 do
                match result with
                | Some _ -> ()
                | None ->
                    if Matrix.get sppf.Graph.Edges nodeIdx j = Some label then
                        result <- Some j

            result

        let getCoords (nodeIdx: int) : (int * int * int * int) option =
            match Graph.getVertex nodeIdx sppf.Graph with
            | SppfNodeInfo.SppfRange(fs, fp, ts, tp) -> Some(fs, fp, ts, tp)
            | SppfNodeInfo.SppfNonterminal(_, fp, tp, fs, ts) -> Some(fs, fp, ts, tp)
            | _ -> None

        for i in 0 .. vc - 1 do
            match Graph.getVertex i sppf.Graph with
            | SppfNodeInfo.SppfIntermediate(state, pos, fromState, fromPos, toState, toPos) ->
                let leftChildIdx = findEdgeTarget i SppfEdgeLabel.LeftChild
                let rightChildIdx = findEdgeTarget i SppfEdgeLabel.RightChild

                match leftChildIdx with
                | Some lc ->
                    match Graph.getVertex lc sppf.Graph with
                    | SppfNodeInfo.SppfEpsilon _ -> ()
                    | vLeft ->
                        match getCoords lc with
                        | Some(lfs, lfp, lts, ltp) ->
                            if lfs <> fromState || lfp <> fromPos then
                                errors <-
                                    sprintf
                                        "Intermediate node %d [s%d,v%d]->[s%d,v%d] (I(%d,%d)): left child %d starts at [s%d,v%d], expected [s%d,v%d]"
                                        i
                                        fromState
                                        fromPos
                                        toState
                                        toPos
                                        state
                                        pos
                                        lc
                                        lfs
                                        lfp
                                        fromState
                                        fromPos
                                    :: errors

                            if lts <> state || ltp <> pos then
                                errors <-
                                    sprintf
                                        "Intermediate node %d [s%d,v%d]->[s%d,v%d] (I(%d,%d)): left child %d ends at [s%d,v%d], expected [s%d,v%d]"
                                        i
                                        fromState
                                        fromPos
                                        toState
                                        toPos
                                        state
                                        pos
                                        lc
                                        lts
                                        ltp
                                        state
                                        pos
                                    :: errors
                        | None -> ()
                | None ->
                    errors <-
                        sprintf
                            "Intermediate node %d [s%d,v%d]->[s%d,v%d]: missing LeftChild"
                            i
                            fromState
                            fromPos
                            toState
                            toPos
                        :: errors

                match rightChildIdx with
                | Some rc ->
                    match Graph.getVertex rc sppf.Graph with
                    | SppfNodeInfo.SppfEpsilon _ -> ()
                    | vRight ->
                        match getCoords rc with
                        | Some(rfs, rfp, rts, rtp) ->
                            if rfs <> state || rfp <> pos then
                                errors <-
                                    sprintf
                                        "Intermediate node %d [s%d,v%d]->[s%d,v%d] (I(%d,%d)): right child %d starts at [s%d,v%d], expected [s%d,v%d]"
                                        i
                                        fromState
                                        fromPos
                                        toState
                                        toPos
                                        state
                                        pos
                                        rc
                                        rfs
                                        rfp
                                        state
                                        pos
                                    :: errors

                            if rts <> toState || rtp <> toPos then
                                errors <-
                                    sprintf
                                        "Intermediate node %d [s%d,v%d]->[s%d,v%d] (I(%d,%d)): right child %d ends at [s%d,v%d], expected [s%d,v%d]"
                                        i
                                        fromState
                                        fromPos
                                        toState
                                        toPos
                                        state
                                        pos
                                        rc
                                        rts
                                        rtp
                                        toState
                                        toPos
                                    :: errors
                        | None -> ()
                | None ->
                    errors <-
                        sprintf
                            "Intermediate node %d [s%d,v%d]->[s%d,v%d]: missing RightChild"
                            i
                            fromState
                            fromPos
                            toState
                            toPos
                        :: errors
            | _ -> ()

        if errors.IsEmpty then Ok() else Error(List.rev errors)

    /// Lazily enumerates derivation trees from an SPPF in order of increasing tree depth.
    /// Cached iterative deepening: each (node, depth) computed at most once, results reused
    /// across depth levels. No depth limit — caller controls consumption via Seq.head etc.
    /// Each yielded tree is a correct derivation if the SPPF is constructed correctly.
    /// Book reference: sec:CFPQ_GLL.
    let enumerateTrees (sppf: SPPF<'t, 'nt>) (rootIdx: int) : seq<DerivationTree<'t, 'nt>> =
        let vc = Graph.vertexCount sppf.Graph

        let edgeTo (nodeIdx: int) (label: SppfEdgeLabel) : int option =
            let mutable result = None

            for j in 0 .. vc - 1 do
                match result with
                | Some _ -> ()
                | None ->
                    if Matrix.get sppf.Graph.Edges nodeIdx j = Some label then
                        result <- Some j

            result

        let allEdgesTo (nodeIdx: int) (label: SppfEdgeLabel) : int list =
            [ for j in 0 .. vc - 1 do
                  if Matrix.get sppf.Graph.Edges nodeIdx j = Some label then
                      j ]

        let cache = Dictionary<(int * int), seq<DerivationTree<'t, 'nt> list>>()

        let rec getTreesAtDepth (depth: int) (nodeIdx: int) : seq<DerivationTree<'t, 'nt> list> =
            let key = (nodeIdx, depth)

            match cache.TryGetValue(key) with
            | true, trees -> trees
            | false, _ ->
                let info = Graph.getVertex nodeIdx sppf.Graph

                let trees =
                    match info with
                    | SppfNodeInfo.SppfTerminal(t, _, _) ->
                        if depth = 1 then
                            Seq.singleton [ Leaf(Symbol.T t) ]
                        else
                            Seq.empty
                    | SppfNodeInfo.SppfEpsilon _ ->
                        if depth = 1 then
                            Seq.singleton [ Leaf Symbol.Epsilon ]
                        else
                            Seq.empty
                    | SppfNodeInfo.SppfNonterminal(nt, _, _, _, _) ->
                        if depth <= 1 then
                            Seq.empty
                        else
                            match edgeTo nodeIdx SppfEdgeLabel.SingleChild with
                            | Some calleeIdx ->
                                getTreesAtDepth (depth - 1) calleeIdx
                                |> Seq.map (fun childList -> [ Node(nt, childList) ])
                            | None -> Seq.empty
                    | SppfNodeInfo.SppfIntermediate _ ->
                        if depth <= 1 then
                            Seq.empty
                        else
                            match edgeTo nodeIdx SppfEdgeLabel.LeftChild, edgeTo nodeIdx SppfEdgeLabel.RightChild with
                            | Some lIdx, Some rIdx ->
                                seq {
                                    for dL in 1 .. depth - 1 do
                                        for dR in 1 .. depth - 1 do
                                            if max dL dR = depth - 1 then
                                                for lc in getTreesAtDepth dL lIdx do
                                                    for rc in getTreesAtDepth dR rIdx do
                                                        yield lc @ rc
                                }
                            | _ -> Seq.empty
                    | SppfNodeInfo.SppfRange _ ->
                        allEdgesTo nodeIdx SppfEdgeLabel.PackedAlternative
                        |> List.collect (fun childIdx -> getTreesAtDepth depth childIdx |> List.ofSeq)
                        |> Seq.ofList

                cache.[key] <- trees
                trees

        seq {
            let mutable depth = 1

            while true do
                let treeLists = getTreesAtDepth depth rootIdx

                for childList in treeLists do
                    match childList with
                    | [ tree ] -> yield tree
                    | _ -> yield Node(Nonterminal "$root", childList)

                depth <- depth + 1
        }
