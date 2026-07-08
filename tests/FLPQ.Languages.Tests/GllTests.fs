module GllTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis

/// Convert a Grammar<string,string> to EBNF text format accepted by RsmBuilder.
/// Only works for grammars where terminals are single alphabetic characters (a-z).
let private grammarToEbnfText (g: Grammar<string, string>) : string =
    g.rules
    |> List.map (fun r ->
        let (Nonterminal nt) = r.lhs

        let rhsStr =
            match r.rhs with
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

/// Build an RSM from a Grammar<string,string> via EBNF text conversion.
let private grammarToRsm (g: Grammar<string, string>) : RSM<string, string> =
    let ebnfText = grammarToEbnfText g
    RsmBuilder.buildRSMFromText ebnfText

/// Convert a string to a list of single-character strings for use with string-based RSM.
let private stringToChars (s: string) : string list =
    s |> Seq.map (fun c -> string c) |> Seq.toList

/// Build a path graph from a list of terminal strings.
let private terminalsToGraph (terminals: string list) : Graph<int, Option<string>> = GLL.stringToGraph terminals

/// Compute global offset of a block in the flattened RSM state space.
let private blockOffset (rsm: RSM<'t, 'nt>) (target: Nonterminal<'nt>) : int =
    let mutable offset = 0

    for block in RSM.blocks rsm do
        if block.nonterminal = target then
            ()
        else
            offset <- offset + Dfa.stateCount block.dfa

    offset

/// Find the global start state index for a given nonterminal.
let private globalStartState (rsm: RSM<'t, 'nt>) (nt: Nonterminal<'nt>) : int =
    let offset = blockOffset rsm nt

    match RSM.blockOf nt rsm with
    | Some block -> offset + block.dfa.startState
    | None -> -1

/// Check if a grammar accepts a string via GLL.
let private gllAccepts (g: Grammar<string, string>) (input: string list) : bool =
    let rsm = grammarToRsm g
    let graph = terminalsToGraph input
    let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])

    let startBlock = RSM.startBlock rsm
    let startGlobalState = globalStartState rsm startBlock.nonterminal

    let blocks = RSM.blocks rsm

    let finalStates =
        (Set.empty, blocks)
        ||> List.fold (fun acc block ->
            let offset = blockOffset rsm block.nonterminal
            let blockFinals = block.dfa.finalStates |> Set.map (fun local -> offset + local)
            Set.union acc blockFinals)

    let vertexCount = Graph.vertexCount graph

    finalStates
    |> Set.exists (fun finalState ->
        let entries =
            PathIndex.get pathIndex startGlobalState 0 finalState (vertexCount - 1)

        not (Set.isEmpty entries))

/// Check if CYK accepts
let private cykAccepts (g: Grammar<string, string>) (input: string list) : bool =
    let terminals = input |> List.map Terminal
    Cyk.parse Grammar.freshStringNonterminal g terminals

/// Extract derivation tree from GLL SPPF and collect leaves.
let private gllTree (g: Grammar<string, string>) (input: string list) : DerivationTree<string, string> option =
    let rsm = grammarToRsm g
    let graph = terminalsToGraph input
    let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])

    let startBlock = RSM.startBlock rsm
    let startGlobalState = globalStartState rsm startBlock.nonterminal

    let mutable finalGlobalState = -1
    let offset = blockOffset rsm startBlock.nonterminal

    for finalLocal in startBlock.dfa.finalStates do
        let fgs = offset + finalLocal

        let entries =
            PathIndex.get pathIndex startGlobalState 0 fgs (Graph.vertexCount graph - 1)

        if not (Set.isEmpty entries) then
            finalGlobalState <- fgs

    if finalGlobalState = -1 then
        None
    else
        // Collect state info and block starts for the extractor
        let blocks = RSM.blocks rsm
        let stateCount = RSM.stateCount rsm
        let stateInfo = Array.zeroCreate<RsmStateInfo<string>> stateCount
        let mutable globalOff = 0

        for block in blocks do
            let localSize = Dfa.stateCount block.dfa

            for localState in 0 .. localSize - 1 do
                stateInfo.[globalOff + localState] <-
                    { blockNonterminal = block.nonterminal
                      localState = localState
                      isFinal = Set.contains localState block.dfa.finalStates }

            globalOff <- globalOff + localSize

        let blockStart = System.Collections.Generic.Dictionary<Nonterminal<string>, int>()

        let mutable goff = 0

        for block in blocks do
            blockStart.[block.nonterminal] <- goff + block.dfa.startState
            goff <- goff + Dfa.stateCount block.dfa

        let tree =
            GLL.extractDerivationTree
                pathIndex
                stateInfo
                blockStart
                startGlobalState
                0
                finalGlobalState
                (Graph.vertexCount graph - 1)

        Some tree

/// Check if an RSM accepts a string via GLL (without converting from Grammar).
let private gllAcceptsRsm (rsm: RSM<string, string>) (input: string list) : bool =
    let graph = terminalsToGraph input
    let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])

    let startBlock = RSM.startBlock rsm
    let startGlobalState = globalStartState rsm startBlock.nonterminal

    let blocks = RSM.blocks rsm

    let finalStates =
        (Set.empty, blocks)
        ||> List.fold (fun acc block ->
            let offset = blockOffset rsm block.nonterminal
            let blockFinals = block.dfa.finalStates |> Set.map (fun local -> offset + local)
            Set.union acc blockFinals)

    let vertexCount = Graph.vertexCount graph

    finalStates
    |> Set.exists (fun finalState ->
        let entries =
            PathIndex.get pathIndex startGlobalState 0 finalState (vertexCount - 1)

        not (Set.isEmpty entries))

module GllAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.True(gllAccepts g [ "a" ])

    [<Fact>]
    let ``S -> a rejects eps`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.False(gllAccepts g [])

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        let g = Grammar.parseGrammar "S -> a b"
        Assert.True(gllAccepts g [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.True(gllAccepts g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.False(gllAccepts g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = TestGrammars.grammar1
        Assert.True(gllAccepts g [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = TestGrammars.grammar1
        Assert.True(gllAccepts g [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = TestGrammars.grammar2
        Assert.True(gllAccepts g [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        let g = TestGrammars.grammar2
        Assert.False(gllAccepts g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts empty`` () =
        let g = TestGrammars.grammar2
        Assert.True(gllAccepts g [])

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = TestGrammars.grammar3
        Assert.True(gllAccepts g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``Left-recursive S -> a S | a rejects empty`` () =
        let g = TestGrammars.grammar3
        Assert.False(gllAccepts g [])

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = TestGrammars.grammar4
        Assert.True(gllAccepts g [ "a"; "a"; "a" ])

module GllCykEquivalence =
    [<Property>]
    let ``GLL and CYK agree on grammar1 random string inputs`` (s: string) =
        let g = TestGrammars.grammar1
        let input = stringToChars s
        gllAccepts g input = cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar2 random string inputs`` (s: string) =
        let g = TestGrammars.grammar2
        let input = stringToChars s
        gllAccepts g input = cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar3 random string inputs`` (s: string) =
        let g = TestGrammars.grammar3
        let input = stringToChars s
        gllAccepts g input = cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar4 random string inputs`` (s: string) =
        let g = TestGrammars.grammar4
        let input = stringToChars s
        gllAccepts g input = cykAccepts g input

module GllTreeExtraction =
    [<Fact>]
    let ``Tree extraction for S->aSbS|eps on abab produces non-epsilon tree`` () =
        let g = TestGrammars.grammar1

        match gllTree g [ "a"; "b"; "a"; "b" ] with
        | Some tree ->
            match tree with
            | Leaf Symbol.Epsilon -> Assert.True(false, "Should not be epsilon leaf")
            | _ -> Assert.True(true)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``Tree extraction for S->aSb|eps on aabb produces non-epsilon tree`` () =
        let g = TestGrammars.grammar2

        match gllTree g [ "a"; "a"; "b"; "b" ] with
        | Some tree ->
            match tree with
            | Leaf Symbol.Epsilon -> Assert.True(false, "Should not be epsilon leaf")
            | _ -> Assert.True(true)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``Tree extraction for S->a|b on a produces tree with leaf`` () =
        let g = Grammar.parseGrammar "S -> a\nS -> b"

        match gllTree g [ "a" ] with
        | Some tree ->
            let leaves = DerivationTree.leaves tree
            Assert.Equal<string list>([ "a" ], leaves)
        | None -> Assert.True(false, "Should produce a tree")

module GllRegexEquivalence =

    let private buildRegexRsm (regexText: string) : RSM<string, string> =
        RsmBuilder.buildRSMFromText $"S -> {regexText}"

    let private dfaFromRegexRsm (rsm: RSM<string, string>) : DFA<RsmSymbol<string, string>, int> =
        (RSM.startBlock rsm).dfa

    let private dfaAcceptsRegex (dfa: DFA<RsmSymbol<string, string>, int>) (input: string list) : bool =
        let input' = input |> List.map (fun s -> Terminal(RsmSymbol.RTerm(Terminal s)))
        Dfa.accept dfa input'

    [<Property(MaxTest = 50)>]
    let ``S -> a* matches DFA for a*`` (s: string) =
        let regexText = "a *"
        let rsm = buildRegexRsm regexText
        let dfa = dfaFromRegexRsm rsm
        let input = stringToChars s |> List.filter (fun c -> c = "a")
        gllAcceptsRsm rsm input = dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> a* a* matches DFA for a* a*`` (s: string) =
        let regexText = "a * a *"
        let rsm = buildRegexRsm regexText
        let dfa = dfaFromRegexRsm rsm
        let input = stringToChars s |> List.filter (fun c -> c = "a")
        gllAcceptsRsm rsm input = dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
        let regexText = "( a | b ) *"
        let rsm = buildRegexRsm regexText
        let dfa = dfaFromRegexRsm rsm
        let input = stringToChars s |> List.filter (fun c -> c = "a" || c = "b")
        gllAcceptsRsm rsm input = dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
        let regexText = "( a | b ) * ( a | c ) *"
        let rsm = buildRegexRsm regexText
        let dfa = dfaFromRegexRsm rsm

        let input = stringToChars s |> List.filter (fun c -> c = "a" || c = "b" || c = "c")

        gllAcceptsRsm rsm input = dfaAcceptsRegex dfa input
