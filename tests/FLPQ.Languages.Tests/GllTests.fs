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

/// Build an RSM from a Grammar<string,string> via EBNF text conversion.
/// Fixes the start block to match the grammar's start symbol.
let private grammarToRsm (g: Grammar<string, string>) : RSM<string, string> =
    let ebnfText = grammarToEbnfText g
    let rsm = RsmBuilder.buildRSMFromText ebnfText
    { rsm with StartBlock = g.Start }

/// Convert a string to a list of single-character strings for use with string-based RSM.
let private stringToChars (s: string) : string list =
    s |> Seq.map (fun c -> string c) |> Seq.toList

/// Build a path graph from a list of terminal strings.
let private terminalsToGraph (terminals: string list) : Graph<int, Option<string>> = GLL.stringToGraph terminals

/// Compute global offset of a block in the flattened RSM state space.
let private blockOffset (rsm: RSM<'t, 'nt>) (target: Nonterminal<'nt>) : int =
    let mutable offset = 0

    for block in RSM.blocks rsm do
        if block.Nonterminal = target then
            ()
        else
            offset <- offset + Dfa.stateCount block.Dfa

    offset

/// Find the global start state index for a given nonterminal.
let private globalStartState (rsm: RSM<'t, 'nt>) (nt: Nonterminal<'nt>) : int =
    let offset = blockOffset rsm nt

    match RSM.blockOf nt rsm with
    | Some block -> offset + block.Dfa.StartState
    | None -> -1

/// Check if a grammar accepts a string via GLL.
let private gllAccepts (g: Grammar<string, string>) (input: string list) : bool =
    let rsm = grammarToRsm g
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

/// Check if CYK accepts
let private cykAccepts (g: Grammar<string, string>) (input: string list) : bool =
    let terminals = input |> List.map Terminal
    Cyk.parse Grammar.freshStringNonterminal g terminals

/// Extract derivation tree from GLL via SPPF (handles nonterminal-first RSM blocks correctly).
let private gllTree (g: Grammar<string, string>) (input: string list) : DerivationTree<string, string> option =
    let rsm = grammarToRsm g
    let graph = terminalsToGraph input
    let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
    let vc = Graph.vertexCount graph

    let startBlock = RSM.startBlock rsm
    let startGlobalState = globalStartState rsm startBlock.Nonterminal

    let mutable finalGlobalState = -1
    let offset = blockOffset rsm startBlock.Nonterminal

    for finalLocal in startBlock.Dfa.FinalStates do
        let fgs = offset + finalLocal

        let entries = PathIndex.get pathIndex startGlobalState 0 fgs (vc - 1)

        if not (Set.isEmpty entries) then
            finalGlobalState <- fgs

    if finalGlobalState = -1 then
        None
    else
        let rootRange =
            { FromState = startGlobalState
              FromVertex = 0
              ToState = finalGlobalState
              ToVertex = vc - 1 }

        let sppf = GLL.buildSppfFromIndex pathIndex [ rootRange ]

        match sppf.RootIndices with
        | rootIdx :: _ ->
            let tree = GLL.extractDerivationTreeFromSppf sppf rootIdx
            Some tree
        | [] -> None

/// Check if an RSM accepts a string via GLL (without converting from Grammar).
let private gllAcceptsRsm (rsm: RSM<string, string>) (input: string list) : bool =
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
        (RSM.startBlock rsm).Dfa

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

module GllGrammarAcceptanceAndTree =

    /// Grammar 1: S -> N a* ; N -> (a a) | a
    /// CFG with inlined terminal-first start.
    let private grammar1 =
        Grammar.parseGrammar
            "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

    /// Grammar 2: S -> a* N ; N -> a | (a a)
    /// CFG with inlined terminal-first start.
    let private grammar2 =
        Grammar.parseGrammar
            "
        S -> a
        S -> a a
        S -> a a A
        S -> a a a A
        A -> a A
        A -> eps
        "

    /// Grammar 3: S -> N* ; N -> a | (a a)
    /// CFG with inlined terminal-first start.
    let private grammar3 =
        Grammar.parseGrammar
            "
        S -> eps
        S -> a a S
        S -> a S
        "

    /// Grammar 4: S -> a | S S | S S S
    let private grammar4 = Grammar.parseGrammar "S -> a\nS -> S S\nS -> S S S"

    /// Check that tree is not an epsilon leaf.
    let private nonEpsilon (tree: DerivationTree<string, string>) : bool =
        match tree with
        | Leaf Symbol.Epsilon -> false
        | _ -> true

    // ---- Grammar 1: S -> N a* ; N -> (a a) | a ----
    module Grammar1 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(gllAccepts grammar1 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(gllAccepts grammar1 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(gllAccepts grammar1 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(gllAccepts grammar1 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(gllAccepts grammar1 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(gllAccepts grammar1 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(gllAccepts grammar1 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(gllAccepts grammar1 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(gllAccepts grammar1 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(gllAccepts grammar1 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree non-epsilon for a`` () =
            match gllTree grammar1 [ "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aa`` () =
            match gllTree grammar1 [ "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaa`` () =
            match gllTree grammar1 [ "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaaa`` () =
            match gllTree grammar1 [ "a"; "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 2: S -> a* N ; N -> a | (a a) ----
    module Grammar2 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(gllAccepts grammar2 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(gllAccepts grammar2 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(gllAccepts grammar2 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(gllAccepts grammar2 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(gllAccepts grammar2 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(gllAccepts grammar2 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(gllAccepts grammar2 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(gllAccepts grammar2 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(gllAccepts grammar2 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(gllAccepts grammar2 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree non-epsilon for a`` () =
            match gllTree grammar2 [ "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aa`` () =
            match gllTree grammar2 [ "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaa`` () =
            match gllTree grammar2 [ "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaaa`` () =
            match gllTree grammar2 [ "a"; "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 3: S -> N* ; N -> a | (a a) ----
    module Grammar3 =
        [<Fact>]
        let ``accepts empty`` () = Assert.True(gllAccepts grammar3 [])

        [<Fact>]
        let ``accepts a`` () =
            Assert.True(gllAccepts grammar3 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(gllAccepts grammar3 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(gllAccepts grammar3 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(gllAccepts grammar3 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(gllAccepts grammar3 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(gllAccepts grammar3 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(gllAccepts grammar3 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(gllAccepts grammar3 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(gllAccepts grammar3 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree exists for empty`` () =
            match gllTree grammar3 [] with
            | Some _ -> Assert.True(true)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for a`` () =
            match gllTree grammar3 [ "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aa`` () =
            match gllTree grammar3 [ "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaa`` () =
            match gllTree grammar3 [ "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaaa`` () =
            match gllTree grammar3 [ "a"; "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 4: S -> a | S S | S S S ----
    module Grammar4 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(gllAccepts grammar4 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(gllAccepts grammar4 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(gllAccepts grammar4 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(gllAccepts grammar4 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () = Assert.False(gllAccepts grammar4 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(gllAccepts grammar4 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(gllAccepts grammar4 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(gllAccepts grammar4 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(gllAccepts grammar4 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(gllAccepts grammar4 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree non-epsilon for a`` () =
            match gllTree grammar4 [ "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aa`` () =
            match gllTree grammar4 [ "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaa`` () =
            match gllTree grammar4 [ "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree non-epsilon for aaaa`` () =
            match gllTree grammar4 [ "a"; "a"; "a"; "a" ] with
            | Some tree -> Assert.True(nonEpsilon tree, "Should not be epsilon tree")
            | None -> Assert.True(false, "Should produce a tree")
