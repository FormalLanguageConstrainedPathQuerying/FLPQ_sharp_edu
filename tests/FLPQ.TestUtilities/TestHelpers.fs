namespace FLPQ.TestUtilities

open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module TestHelpers =

    let assertPathIndexInvariant (source: string) (pi: PathIndex<string, string>) : unit =
        match PathIndex.checkNonterminalInvariant pi with
        | Ok() -> ()
        | Error errors ->
            let msg =
                $"[{source}] Path index invariant violations:\n  " + String.concat "\n  " errors

            failwith msg

    let assertSppfInvariant (sppf: SPPF<string, string>) : unit =
        match Sppf.validateRangeNodesHaveChildren sppf with
        | Ok() -> ()
        | Error errors ->
            let msg = "SPPF range node children violations:\n  " + String.concat "\n  " errors
            failwith msg

        match Sppf.validateIntermediateChildren sppf with
        | Ok() -> ()
        | Error errors ->
            let msg =
                "SPPF intermediate node children violations:\n  " + String.concat "\n  " errors

            failwith msg

        match Sppf.validateNonterminalChildren sppf with
        | Ok() -> ()
        | Error errors ->
            let msg =
                "SPPF nonterminal node children violations:\n  " + String.concat "\n  " errors

            failwith msg

        match Sppf.validateRangePositions sppf with
        | Ok() -> ()
        | Error errors ->
            let msg = "SPPF range position violations:\n  " + String.concat "\n  " errors

            failwith msg

        match Sppf.validateIntermediateConnectedness sppf with
        | Ok() -> ()
        | Error errors ->
            let msg =
                "SPPF intermediate connectedness violations:\n  " + String.concat "\n  " errors

            failwith msg

    let grammarToEbnfText (g: Grammar<string, string>) : string =
        g.Rules
        |> List.map (fun r ->
            let (Nonterminal nt) = r.Lhs

            let rhsStr =
                match r.Rhs with
                | Rhs.EpsilonRhs -> "eps"
                | Rhs.Symbols symbols ->
                    NonEmptyList.toList symbols
                    |> List.map (fun s ->
                        match s with
                        | Symbol.T(Terminal t) -> t
                        | Symbol.N(Nonterminal nt') -> nt'
                        | Symbol.Epsilon -> "eps")
                    |> String.concat " "

            $"{nt} -> {rhsStr}")
        |> String.concat "\n"

    let grammarToRsm (g: Grammar<string, string>) : RSM<string, string> =
        let rsm = RsmBuilder.buildRSMFromText (grammarToEbnfText g)
        { rsm with StartBlock = g.Start }

    let stringToTerminals (s: string) : string list = s |> Seq.map string |> Seq.toList

    let terminalsToGraph (terminals: string list) : Graph<int, Option<string>> = GLL.stringToGraph terminals

    let blockOffset (rsm: RSM<'t, 'nt>) (target: Nonterminal<'nt>) : int =
        RSM.blocks rsm
        |> List.takeWhile (fun block -> block.Nonterminal <> target)
        |> List.sumBy (fun block -> Dfa.stateCount block.Dfa)

    let globalStartState (rsm: RSM<'t, 'nt>) (nt: Nonterminal<'nt>) : int =
        let offset = blockOffset rsm nt

        match RSM.blockOf nt rsm with
        | Some block -> offset + block.Dfa.StartState
        | None -> -1

    let gllAcceptsRsm (rsm: RSM<string, string>) (input: string list) : bool =
        let graph = terminalsToGraph input
        let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
        assertPathIndexInvariant "gllAcceptsRsm" pathIndex

        let startBlock = RSM.startBlock rsm
        let startGlobalState = globalStartState rsm startBlock.Nonterminal

        let blocks = RSM.blocks rsm

        let finalStates =
            (Set.empty, blocks)
            ||> List.fold (fun acc block ->
                let offset = blockOffset rsm block.Nonterminal
                let blockFinals = block.Dfa.FinalStates |> Set.map (fun local -> offset + local)
                Set.union acc blockFinals)

        let vertexCount = Graph.vertexCount graph

        finalStates
        |> Set.exists (fun finalState ->
            let entries =
                PathIndex.get pathIndex startGlobalState 0 finalState (vertexCount - 1)

            not (Set.isEmpty entries))

    let gllAcceptsRsmWithSppfCheck (rsm: RSM<string, string>) (input: string list) : bool =
        let graph = terminalsToGraph input
        let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
        assertPathIndexInvariant "gllAcceptsRsmWithSppfCheck" pathIndex
        let vc = Graph.vertexCount graph
        let startBlock = RSM.startBlock rsm
        let startGlobalState = globalStartState rsm startBlock.Nonterminal
        let blocks = RSM.blocks rsm

        let finalStates =
            (Set.empty, blocks)
            ||> List.fold (fun acc block ->
                let offset = blockOffset rsm block.Nonterminal
                let blockFinals = block.Dfa.FinalStates |> Set.map (fun local -> offset + local)
                Set.union acc blockFinals)

        let acceptedRanges =
            finalStates
            |> Set.toList
            |> List.choose (fun fs ->
                let entries = PathIndex.get pathIndex startGlobalState 0 fs (vc - 1)

                if Set.isEmpty entries then
                    None
                else
                    Some
                        { FromState = startGlobalState
                          FromVertex = 0
                          ToState = fs
                          ToVertex = vc - 1 })

        if not (List.isEmpty acceptedRanges) then
            let sppf = Sppf.buildSppfFromIndex pathIndex acceptedRanges
            assertSppfInvariant sppf
            true
        else
            false

    let gllAccepts (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        gllAcceptsRsm rsm input

    let buildRegexRsm (regexText: string) : RSM<string, string> =
        RsmBuilder.buildRSMFromText $"S -> {regexText}"

    let dfaFromRegexRsm (rsm: RSM<string, string>) : DFA<RsmSymbol<string, string>, int> = (RSM.startBlock rsm).Dfa

    let dfaAcceptsRegex (dfa: DFA<RsmSymbol<string, string>, int>) (input: string list) : bool =
        let input' = input |> List.map (Terminal << RsmSymbol.RTerm << Terminal)
        Dfa.accept dfa input'

    let cykAccepts (g: Grammar<string, string>) (input: string list) : bool =
        Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal)

    let rnglrAccepts (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        let graph = terminalsToGraph input
        let startNt = g.Start
        let freshStart = Nonterminal("S'")
        let rsmFixed = { rsm with StartBlock = startNt }
        let extRsm = RSM.extendWithStart freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        assertPathIndexInvariant "rnglrAccepts" pathIndex
        Rnglr.isAccepted pathIndex extRsm (Graph.vertexCount graph)

    let nonEpsilon (tree: DerivationTree<string, string>) : bool =
        match tree with
        | Leaf Symbol.Epsilon -> false
        | _ -> true

    let buildDfa (transitions: (int * string * int) list) (startState: int) (finalStates: int list) =
        let allStates =
            transitions
            |> List.collect (fun (f, _, t) -> [ f; t ])
            |> List.append (startState :: finalStates)
            |> List.distinct
            |> List.sort

        Dfa.fromTransitions allStates transitions startState (Set.ofList finalStates)

    let nfaFromEdges (vCount: int) (edges: (int * string * int) list) (sources: int[]) : NFA<string, int> =
        let states = [ 0 .. vCount - 1 ]
        Nfa.fromTransitions states edges Set.empty (Set.ofArray sources) Set.empty

    let gllAcceptsWithSppfCheck (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        let graph = terminalsToGraph input
        let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
        assertPathIndexInvariant "gllAcceptsWithSppfCheck" pathIndex
        let vc = Graph.vertexCount graph
        let startBlock = RSM.startBlock rsm
        let startGlobalState = globalStartState rsm startBlock.Nonterminal
        let blocks = RSM.blocks rsm

        let finalStates =
            (Set.empty, blocks)
            ||> List.fold (fun acc block ->
                let offset = blockOffset rsm block.Nonterminal
                let blockFinals = block.Dfa.FinalStates |> Set.map (fun local -> offset + local)
                Set.union acc blockFinals)

        let acceptedRanges =
            finalStates
            |> Set.toList
            |> List.choose (fun fs ->
                let entries = PathIndex.get pathIndex startGlobalState 0 fs (vc - 1)

                if Set.isEmpty entries then
                    None
                else
                    Some
                        { FromState = startGlobalState
                          FromVertex = 0
                          ToState = fs
                          ToVertex = vc - 1 })

        if not (List.isEmpty acceptedRanges) then
            let sppf = Sppf.buildSppfFromIndex pathIndex acceptedRanges
            assertSppfInvariant sppf
            true
        else
            false

    let private buildPathIndexForRsm
        (rsm: RSM<string, string>)
        (input: string list)
        : PathIndex<string, string> * RSM<string, string> * int =
        let freshStart = Nonterminal("S'")
        let graph = terminalsToGraph input
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let extRsm = RSM.extendWithStart freshStart rsmFixed
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        pathIndex, extRsm, Graph.vertexCount graph

    let rnglrCheckReject (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        let pathIndex, extRsm, vc = buildPathIndexForRsm rsm input
        assertPathIndexInvariant "rnglrCheckReject" pathIndex
        not (Rnglr.isAccepted pathIndex extRsm vc)

    let rnglrAcceptsAndCheckTree
        (g: Grammar<string, string>)
        (input: string list)
        : DerivationTree<string, string> option =
        let rsm = grammarToRsm g
        let pathIndex, extRsm, vc = buildPathIndexForRsm rsm input
        assertPathIndexInvariant "rnglrAcceptsAndCheckTree" pathIndex

        if not (Rnglr.isAccepted pathIndex extRsm vc) then
            None
        else
            let startGlobalState =
                match extRsm.BlockStart.TryGetValue(extRsm.StartBlock) with
                | true, gs -> gs
                | false, _ -> 0

            let finalGlobalState = startGlobalState + 1

            let rootRanges =
                [ { FromState = startGlobalState
                    FromVertex = 0
                    ToState = finalGlobalState
                    ToVertex = vc - 1 } ]

            let sppf = Sppf.buildSppfFromIndex pathIndex rootRanges
            assertSppfInvariant sppf

            let stateInfo = extRsm.StateInfo
            let blockStart = extRsm.BlockStart

            let blockFinals =
                System.Collections.Generic.Dictionary<Nonterminal<string>, Set<int>>()

            for i in 0 .. stateInfo.Length - 1 do
                if stateInfo.[i].IsFinal then
                    let nt = stateInfo.[i].BlockNonterminal

                    let current =
                        match blockFinals.TryGetValue(nt) with
                        | true, s -> s
                        | false, _ -> Set.empty

                    blockFinals.[nt] <- Set.add i current

            let rootRangesForExtraction =
                [ { FromState = startGlobalState
                    FromVertex = 0
                    ToState = startGlobalState + 1
                    ToVertex = vc - 1 } ]

            let tree =
                rootRangesForExtraction
                |> List.tryPick (fun rk ->
                    GLL.extractDerivationTree
                        pathIndex
                        stateInfo
                        blockStart
                        blockFinals
                        rk.FromState
                        rk.FromVertex
                        rk.ToState
                        rk.ToVertex)

            match tree with
            | Some t ->
                let leaves = DerivationTree.leaves t

                if leaves = input then
                    Some t
                else
                    failwithf "Tree leaves %A ≠ input %A for grammar" leaves input
            | None -> None

    let rnglrAcceptsAndCheckTreeRsm
        (rsm: RSM<string, string>)
        (input: string list)
        : DerivationTree<string, string> option =
        let pathIndex, extRsm, vc = buildPathIndexForRsm rsm input
        assertPathIndexInvariant "rnglrAcceptsAndCheckTreeRsm" pathIndex

        if not (Rnglr.isAccepted pathIndex extRsm vc) then
            None
        else
            let startGlobalState =
                match extRsm.BlockStart.TryGetValue(extRsm.StartBlock) with
                | true, gs -> gs
                | false, _ -> 0

            let finalGlobalState = startGlobalState + 1

            let rootRanges =
                [ { FromState = startGlobalState
                    FromVertex = 0
                    ToState = finalGlobalState
                    ToVertex = vc - 1 } ]

            let sppf = Sppf.buildSppfFromIndex pathIndex rootRanges
            assertSppfInvariant sppf

            let stateInfo = extRsm.StateInfo
            let blockStart = extRsm.BlockStart

            let blockFinals =
                System.Collections.Generic.Dictionary<Nonterminal<string>, Set<int>>()

            for i in 0 .. stateInfo.Length - 1 do
                if stateInfo.[i].IsFinal then
                    let nt = stateInfo.[i].BlockNonterminal

                    let current =
                        match blockFinals.TryGetValue(nt) with
                        | true, s -> s
                        | false, _ -> Set.empty

                    blockFinals.[nt] <- Set.add i current

            let rootRangesForExtraction =
                [ { FromState = startGlobalState
                    FromVertex = 0
                    ToState = startGlobalState + 1
                    ToVertex = vc - 1 } ]

            let tree =
                rootRangesForExtraction
                |> List.tryPick (fun rk ->
                    GLL.extractDerivationTree
                        pathIndex
                        stateInfo
                        blockStart
                        blockFinals
                        rk.FromState
                        rk.FromVertex
                        rk.ToState
                        rk.ToVertex)

            match tree with
            | Some t ->
                let leaves = DerivationTree.leaves t

                if leaves = input then
                    Some t
                else
                    failwithf "Tree leaves %A ≠ input %A for RSM" leaves input
            | None -> None

    let rnglrAcceptsWithSppfCheck (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        let pathIndex, extRsm, vc = buildPathIndexForRsm rsm input
        assertPathIndexInvariant "rnglrAcceptsWithSppfCheck" pathIndex

        if not (Rnglr.isAccepted pathIndex extRsm vc) then
            false
        else
            let startGlobalState =
                match extRsm.BlockStart.TryGetValue(extRsm.StartBlock) with
                | true, gs -> gs
                | false, _ -> 0

            let finalGlobalState = startGlobalState + 1

            let rootRanges =
                [ { FromState = startGlobalState
                    FromVertex = 0
                    ToState = finalGlobalState
                    ToVertex = vc - 1 } ]

            let sppf = Sppf.buildSppfFromIndex pathIndex rootRanges
            assertSppfInvariant sppf
            true
