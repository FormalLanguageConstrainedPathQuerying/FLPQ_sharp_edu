namespace FLPQ.TestUtilities

open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

module TestHelpers =

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
        let mutable offset = 0

        for block in RSM.blocks rsm do
            if block.Nonterminal = target then
                ()
            else
                offset <- offset + Dfa.stateCount block.Dfa

        offset

    let globalStartState (rsm: RSM<'t, 'nt>) (nt: Nonterminal<'nt>) : int =
        let offset = blockOffset rsm nt

        match RSM.blockOf nt rsm with
        | Some block -> offset + block.Dfa.StartState
        | None -> -1

    let gllAcceptsRsm (rsm: RSM<string, string>) (input: string list) : bool =
        let graph = terminalsToGraph input
        let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])

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

    let gllAccepts (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        gllAcceptsRsm rsm input

    let buildRegexRsm (regexText: string) : RSM<string, string> =
        RsmBuilder.buildRSMFromText $"S -> {regexText}"

    let dfaFromRegexRsm (rsm: RSM<string, string>) : DFA<RsmSymbol<string, string>, int> = (RSM.startBlock rsm).Dfa

    let dfaAcceptsRegex (dfa: DFA<RsmSymbol<string, string>, int>) (input: string list) : bool =
        let input' = input |> List.map (fun s -> Terminal(RsmSymbol.RTerm(Terminal s)))
        Dfa.accept dfa input'

    let cykAccepts (g: Grammar<string, string>) (input: string list) : bool =
        Cyk.parse Grammar.freshStringNonterminal g (input |> List.map Terminal)

    let rnglrAccepts (g: Grammar<string, string>) (input: string list) : bool =
        let rsm = grammarToRsm g
        let graph = terminalsToGraph input
        let startNt = g.Start
        let freshStart = Nonterminal("S'")
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        Rnglr.isAccepted pathIndex (Graph.vertexCount graph)

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

        Dfa.fromTransitions (List.map id allStates) transitions startState (Set.ofList finalStates)

    let nfaFromEdges (vCount: int) (edges: (int * string * int) list) (sources: int[]) : NFA<string, int> =
        let states = [ 0 .. vCount - 1 ]
        Nfa.fromTransitions states edges Set.empty (Set.ofArray sources) Set.empty
