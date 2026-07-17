namespace FLPQ.TestUtilities

open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module TestHelpers =

    let assertPathIndexInvariant
        (source: string)
        (pi: PathIndex<string, string>)
        (blockStart: Map<Nonterminal<string>, int> option)
        (blockFinals: Map<Nonterminal<string>, Set<int>> option)
        (finalStates: Set<int> option)
        : unit =
        match blockStart, blockFinals with
        | Some bs, Some bf ->
            match PathIndex.checkCalleeReachabilityInvariant pi bs bf with
            | Ok() -> ()
            | Error errors ->
                let msg =
                    $"[{source}] Path index invariant violations:\n  " + String.concat "\n  " errors

                failwith msg

            match finalStates with
            | Some fs ->
                match PathIndex.checkNoEpsilonInvariant pi bs fs with
                | Ok() -> ()
                | Error errors ->
                    let msg =
                        $"[{source}] Path index no-epsilon invariant violations:\n  " + String.concat "\n  " errors

                    failwith msg
            | None -> ()
        | _ -> ()

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

    let buildRegexRsm (regexText: string) : RSM<string, string> =
        RsmBuilder.buildRSMFromText $"S -> {regexText}"

    let dfaFromRegexRsm (rsm: RSM<string, string>) : DFA<RsmSymbol<string, string>, int> = (RSM.startBlock rsm).Dfa

    let dfaAcceptsRegex (dfa: DFA<RsmSymbol<string, string>, int>) (input: string list) : bool =
        let input' = input |> List.map (Terminal << RsmSymbol.RTerm << Terminal)
        Dfa.accept dfa input'

    let cykAccepts (g: Grammar<string, string>) (input: string list) : bool =
        Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal)

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

    /// Shared pipeline for acceptance check: create ExtendedRSM → build path index → check acceptance → build SPPF → validate tree leaves.
    /// Parameterized by buildPathIndex and isAccepted functions to support both GLL and RNGLR.
    let accepts
        (buildPI:
            Nonterminal<string>
                -> ExtendedRSM<string, string>
                -> Graph<int, Option<string>>
                -> PathIndex<string, string>)
        (isAcc: PathIndex<string, string> -> ExtendedRSM<string, string> -> int -> bool)
        (rsm: RSM<string, string>)
        (input: string list)
        : bool =
        let freshStart = Nonterminal("S'")
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let graph = terminalsToGraph input
        let vc = Graph.vertexCount graph
        let pathIndex = buildPI freshStart ersm graph

        assertPathIndexInvariant
            "accepts"
            pathIndex
            (Some(ersm.ExtendedRsm.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
            (Some(RSM.blockFinalsMap ersm.ExtendedRsm))
            (Some ersm.ExtendedRsm.FinalStates)

        if not (isAcc pathIndex ersm vc) then
            false
        else
            let flatExt = ersm.ExtendedRsm

            let startGlobalState =
                match flatExt.BlockStart.TryGetValue(flatExt.StartBlock) with
                | true, gs -> gs
                | false, _ -> failwith "Start block not found"

            let finalGlobalState = startGlobalState + 1

            let rootRanges =
                let entries = PathIndex.get pathIndex startGlobalState 0 finalGlobalState (vc - 1)

                if not (Set.isEmpty entries) then
                    [ { FromState = startGlobalState
                        FromVertex = 0
                        ToState = finalGlobalState
                        ToVertex = vc - 1 } ]
                else
                    []

            let sppf =
                Sppf.buildSppfFromIndex
                    pathIndex
                    rootRanges
                    (Some(flatExt.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
                    (Some(RSM.blockFinalsMap flatExt))

            assertSppfInvariant sppf

            let tree =
                sppf.RootIndices
                |> List.tryHead
                |> Option.map (fun rootIdx ->
                    let trees = Sppf.enumerateTrees sppf rootIdx

                    trees |> Seq.head)

            match tree with
            | Some t ->
                let leaves = DerivationTree.leaves t

                if leaves = input then
                    true
                else
                    failwithf "Tree leaves %A <> input %A for RSM" leaves input
            | None -> failwith "Could not extract derivation tree for accepted input"

    /// Shared pipeline for rejection check: create ExtendedRSM → build path index → verify NOT accepted.
    /// Parameterized by buildPathIndex and isAccepted functions to support both GLL and RNGLR.
    let checkReject
        (buildPI:
            Nonterminal<string>
                -> ExtendedRSM<string, string>
                -> Graph<int, Option<string>>
                -> PathIndex<string, string>)
        (isAcc: PathIndex<string, string> -> ExtendedRSM<string, string> -> int -> bool)
        (g: Grammar<string, string>)
        (input: string list)
        : bool =
        let rsm = grammarToRsm g
        let freshStart = Nonterminal("S'")
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let ersm = ExtendedRSM.create freshStart rsmFixed
        let graph = terminalsToGraph input
        let vc = Graph.vertexCount graph
        let pathIndex = buildPI freshStart ersm graph

        assertPathIndexInvariant
            "checkReject"
            pathIndex
            (Some(ersm.ExtendedRsm.BlockStart |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq))
            (Some(RSM.blockFinalsMap ersm.ExtendedRsm))
            (Some ersm.ExtendedRsm.FinalStates)

        not (isAcc pathIndex ersm vc)
