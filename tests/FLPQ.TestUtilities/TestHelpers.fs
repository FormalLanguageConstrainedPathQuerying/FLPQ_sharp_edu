namespace FLPQ.TestUtilities

open FSharpPlus.Data
open FsCheck
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
                match PathIndex.checkNoEpsilonInvariant id pi bs fs with
                | Ok() -> ()
                | Error errors ->
                    let msg =
                        $"[{source}] Path index no-epsilon invariant violations:\n  "
                        + String.concat "\n  " errors

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

        match Sppf.validateNonterminalChildren id sppf with
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

    let assertSppfCoverageInvariant (pi: PathIndex<string, string>) (sppf: SPPF<string, string>) : unit =
        match Sppf.checkSppfCoverageInvariant pi sppf with
        | Ok() -> ()
        | Error errors ->
            let msg = "SPPF coverage invariant violations:\n  " + String.concat "\n  " errors

            failwith msg

    let stringToTerminals (s: string) : Terminal<string> list =
        s |> Seq.map (string >> Terminal) |> Seq.toList

    let terminalsToGraph (terminals: Terminal<string> list) : Graph<int, Option<string>> =
        GLL.stringToGraph (terminals |> List.map (fun (Terminal s) -> s))

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

    let dfaAcceptsRegex (dfa: DFA<RsmSymbol<string, string>, int>) (input: Terminal<string> list) : bool =
        let input' =
            input
            |> List.map (fun (Terminal t) -> Terminal << RsmSymbol.RTerm << Terminal <| t)

        Dfa.accept dfa input'

    let cykAccepts (g: Grammar<string, string>) (input: Terminal<string> list) : bool =
        Cyk.parse Grammar.freshStringNonterminal g input

    let nonEpsilon (tree: DerivationTree<string, string>) : bool =
        match tree with
        | Leaf Symbol.Epsilon -> false
        | _ -> true

    let buildDfa (transitions: Trans<string> list) (startState: int) (finalStates: int list) =
        let allStates =
            transitions
            |> List.collect (fun t -> [ t.From; t.To ])
            |> List.append (startState :: finalStates)
            |> List.distinct
            |> List.sort

        Dfa.fromTransitions allStates transitions startState (Set.ofList finalStates)

    let nfaFromEdges (vCount: int) (edges: Trans<string> list) (sources: int[]) : NFA<string, int> =
        let states = [ 0 .. vCount - 1 ]
        Nfa.fromTransitions states edges Set.empty (Set.ofArray sources) Set.empty

    /// Shared pipeline for acceptance check: create ExtendedRSM → build path index → check acceptance → build SPPF → validate tree leaves.
    /// Returns (accepted, sppfOption) — the SPPF is Some if the input was accepted and tree validation succeeded.
    let private acceptsInternal
        (buildPI:
            Nonterminal<string>
                -> ExtendedRSM<string, string>
                -> Graph<int, Option<string>>
                -> PathIndex<string, string>)
        (isAcc: PathIndex<string, string> -> ExtendedRSM<string, string> -> int -> bool)
        (rsm: RSM<string, string>)
        (input: Terminal<string> list)
        : bool * SPPF<string, string> option =
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
            false, None
        else

            match PathIndex.checkAcceptanceInvariant id pathIndex ersm vc with
            | Ok() -> ()
            | Error errors ->
                let msg =
                    "[accepts] Acceptance invariant violations:\n  " + String.concat "\n  " errors

                failwith msg

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
            assertSppfCoverageInvariant pathIndex sppf

            let tree =
                sppf.RootIndices
                |> List.tryHead
                |> Option.map (fun rootIdx ->
                    let trees = Sppf.enumerateTrees (Nonterminal "$root") sppf rootIdx

                    trees |> Seq.head)

            match tree with
            | Some t ->
                let leaves = DerivationTree.leaves t
                let inputStrs = input |> List.map (fun (Terminal s) -> s)

                if leaves = inputStrs then
                    true, Some sppf
                else
                    failwithf "Tree leaves %A <> input %A for RSM" leaves input
            | None -> failwith "Could not extract derivation tree for accepted input"

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
        (input: Terminal<string> list)
        : bool =
        acceptsInternal buildPI isAcc rsm input |> fst

    /// Like accepts, but also returns the nontrivial SCC count of the built SPPF.
    /// The SCC count is 0 if the input was rejected.
    let acceptsWithScc
        (buildPI:
            Nonterminal<string>
                -> ExtendedRSM<string, string>
                -> Graph<int, Option<string>>
                -> PathIndex<string, string>)
        (isAcc: PathIndex<string, string> -> ExtendedRSM<string, string> -> int -> bool)
        (rsm: RSM<string, string>)
        (input: Terminal<string> list)
        : bool * int =
        let ok, sppfOpt = acceptsInternal buildPI isAcc rsm input

        match sppfOpt with
        | Some sppf -> ok, Sppf.countNonTrivialScc sppf
        | None -> ok, 0

    /// Shared pipeline for rejection check: create ExtendedRSM → build path index → verify NOT accepted.
    /// Parameterized by buildPathIndex and isAccepted functions to support both GLL and RNGLR.
    let checkReject
        (buildPI:
            Nonterminal<string>
                -> ExtendedRSM<string, string>
                -> Graph<int, Option<string>>
                -> PathIndex<string, string>)
        (isAcc: PathIndex<string, string> -> ExtendedRSM<string, string> -> int -> bool)
        (rsm: RSM<string, string>)
        (input: Terminal<string> list)
        : bool =
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

    /// Iterates all grammars of a language against all accept strings.
    /// Returns the list of (grammarName, input) pairs that were incorrectly rejected.
    let collectAcceptFailures
        (parseFn: Grammar<string, string> -> Terminal<string> list -> bool)
        (lang: Language)
        : (string * Terminal<string> list) list =
        lang.Grammars
        |> List.filter (fun g -> not g.Properties.IsRsmDerived && not g.Properties.DoesNotCoverFullLanguage)
        |> List.collect (fun g ->
            lang.AcceptStrings
            |> List.choose (fun input ->
                if parseFn g.Grammar input then
                    None
                else
                    Some(g.Name, input)))

    /// Iterates all grammars of a language against all reject strings.
    /// Returns the list of (grammarName, input) pairs that were incorrectly accepted.
    let collectRejectFailures
        (parseFn: Grammar<string, string> -> Terminal<string> list -> bool)
        (lang: Language)
        : (string * Terminal<string> list) list =
        lang.Grammars
        |> List.filter (fun g -> not g.Properties.IsRsmDerived && not g.Properties.DoesNotCoverFullLanguage)
        |> List.collect (fun g ->
            lang.RejectStrings
            |> List.choose (fun input ->
                if parseFn g.Grammar input then
                    Some(g.Name, input)
                else
                    None))

    let isCykValiantCompatible (g: AnnotatedGrammar) : bool =
        not g.Properties.IsRsmDerived && not g.Properties.DoesNotCoverFullLanguage

    let checkCykValiantEquivalence (g: Grammar<string, string>) (input: Terminal<string> list) : unit =
        let cykTable, cykAcc = Cyk.parseWithTable Grammar.freshStringNonterminal g input
        let valTable, valAcc = Valiant.parseWithTable Grammar.freshStringNonterminal g input

        let modTable, modAcc =
            Valiant.parseModifiedWithTable Grammar.freshStringNonterminal g input

        if cykAcc <> valAcc || valAcc <> modAcc then
            failwithf "Acceptance mismatch: CYK=%b Valiant=%b ModValiant=%b" cykAcc valAcc modAcc

        let n = Matrix.rows cykTable

        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                if cykTable.[i, j] <> valTable.[i, j] || valTable.[i, j] <> modTable.[i, j] then
                    failwithf
                        "Table mismatch at (%d,%d): CYK=%A Valiant=%A Mod=%A"
                        i
                        j
                        cykTable.[i, j]
                        valTable.[i, j]
                        modTable.[i, j]

        let cykSppfTable, cykSppfAcc =
            Cyk.parseWithSppfTable Grammar.freshStringNonterminal g input

        let valSppfTable, valSppfAcc =
            Valiant.parseWithSppfTable Grammar.freshStringNonterminal g input

        let modSppfTable, modSppfAcc =
            Valiant.parseModifiedWithSppfTable Grammar.freshStringNonterminal g input

        if cykSppfAcc <> valSppfAcc || valSppfAcc <> modSppfAcc then
            failwithf "SPPF acceptance mismatch: CYK=%b Valiant=%b ModValiant=%b" cykSppfAcc valSppfAcc modSppfAcc

        if cykAcc <> cykSppfAcc || valAcc <> valSppfAcc || modAcc <> modSppfAcc then
            failwithf
                "Acceptance vs SPPF acceptance mismatch: CYK=%b/%b Valiant=%b/%b Mod=%b/%b"
                cykAcc
                cykSppfAcc
                valAcc
                valSppfAcc
                modAcc
                modSppfAcc

        let sn = Matrix.rows cykSppfTable

        for i in 0 .. sn - 1 do
            for j in 0 .. sn - 1 do
                if
                    cykSppfTable.[i, j] <> valSppfTable.[i, j]
                    || valSppfTable.[i, j] <> modSppfTable.[i, j]
                then
                    failwithf
                        "SPPF table mismatch at (%d,%d): CYK=%A Valiant=%A Mod=%A"
                        i
                        j
                        cykSppfTable.[i, j]
                        valSppfTable.[i, j]
                        modSppfTable.[i, j]

        if cykSppfAcc && sn > 0 then
            let cnf = Grammar.toCnf Grammar.freshStringNonterminal g

            let cykSppf = BasicSppf.fromParsingTable cnf cykSppfTable
            let valSppf = BasicSppf.fromParsingTable cnf valSppfTable
            let modSppf = BasicSppf.fromParsingTable cnf modSppfTable

            let cykTree = BasicSppf.extractDerivationTree cykSppf
            let valTree = BasicSppf.extractDerivationTree valSppf
            let modTree = BasicSppf.extractDerivationTree modSppf

            let cykLeaves =
                DerivationTree.leaves cykTree |> List.map (fun (t: string) -> Terminal t)

            let valLeaves =
                DerivationTree.leaves valTree |> List.map (fun (t: string) -> Terminal t)

            let modLeaves =
                DerivationTree.leaves modTree |> List.map (fun (t: string) -> Terminal t)

            if cykLeaves <> input then
                failwithf "CYK tree leaves %A <> input %A" cykLeaves input

            if valLeaves <> input then
                failwithf "Valiant tree leaves %A <> input %A" valLeaves input

            if modLeaves <> input then
                failwithf "Modified Valiant tree leaves %A <> input %A" modLeaves input

            match BasicSppf.validateProductionChildren cykSppf cnf with
            | Error errors -> failwithf "CYK validateProductionChildren: %A" errors
            | Ok() -> ()

            match BasicSppf.validateProductionChildren valSppf cnf with
            | Error errors -> failwithf "Valiant validateProductionChildren: %A" errors
            | Ok() -> ()

            match BasicSppf.validateProductionChildren modSppf cnf with
            | Error errors -> failwithf "Modified Valiant validateProductionChildren: %A" errors
            | Ok() -> ()

            let cykScc = BasicSppf.countScc cykSppf
            let valScc = BasicSppf.countScc valSppf
            let modScc = BasicSppf.countScc modSppf

            if cykScc <> valScc || valScc <> modScc then
                failwithf "SPPF SCC count mismatch: CYK=%d Valiant=%d Mod=%d" cykScc valScc modScc
