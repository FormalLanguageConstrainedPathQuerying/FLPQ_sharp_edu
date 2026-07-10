module GllTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

/// Extract derivation tree from GLL via path-index-based extraction.
let private gllTree (g: Grammar<string, string>) (input: string list) : DerivationTree<string, string> option =
    let rsm = TestHelpers.grammarToRsm g
    let graph = TestHelpers.terminalsToGraph input
    let pathIndex = GLL.buildPathIndex rsm graph (set [ 0 ])
    let vc = Graph.vertexCount graph

    let startBlock = RSM.startBlock rsm
    let startGlobalState = TestHelpers.globalStartState rsm startBlock.Nonterminal
    let flat = RSM.flattenRsm rsm
    let stateInfo = flat.StateInfo
    let blockStart = flat.BlockStart

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

    let mutable finalGlobalState = -1
    let offset = TestHelpers.blockOffset rsm startBlock.Nonterminal

    for finalLocal in startBlock.Dfa.FinalStates do
        let fgs = offset + finalLocal

        let entries = PathIndex.get pathIndex startGlobalState 0 fgs (vc - 1)

        if not (Set.isEmpty entries) then
            finalGlobalState <- fgs

    if finalGlobalState = -1 then
        None
    else
        GLL.extractDerivationTree
            pathIndex
            stateInfo
            blockStart
            blockFinals
            startGlobalState
            0
            finalGlobalState
            (vc - 1)

module GllAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.True(TestHelpers.gllAccepts g [ "a" ])

    [<Fact>]
    let ``S -> a rejects eps`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.False(TestHelpers.gllAccepts g [])

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        let g = Grammar.parseGrammar "S -> a b"
        Assert.True(TestHelpers.gllAccepts g [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.True(TestHelpers.gllAccepts g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.False(TestHelpers.gllAccepts g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.gllAccepts g [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.gllAccepts g [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = TestGrammars.grammar2
        Assert.True(TestHelpers.gllAccepts g [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        let g = TestGrammars.grammar2
        Assert.False(TestHelpers.gllAccepts g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts empty`` () =
        let g = TestGrammars.grammar2
        Assert.True(TestHelpers.gllAccepts g [])

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = TestGrammars.grammar3
        Assert.True(TestHelpers.gllAccepts g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``Left-recursive S -> a S | a rejects empty`` () =
        let g = TestGrammars.grammar3
        Assert.False(TestHelpers.gllAccepts g [])

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = TestGrammars.grammar4
        Assert.True(TestHelpers.gllAccepts g [ "a"; "a"; "a" ])

module GllCykEquivalence =
    [<Property>]
    let ``GLL and CYK agree on grammar1 random string inputs`` (s: string) =
        let g = TestGrammars.grammar1
        let input = TestHelpers.stringToTerminals s
        TestHelpers.gllAccepts g input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar2 random string inputs`` (s: string) =
        let g = TestGrammars.grammar2
        let input = TestHelpers.stringToTerminals s
        TestHelpers.gllAccepts g input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar3 random string inputs`` (s: string) =
        let g = TestGrammars.grammar3
        let input = TestHelpers.stringToTerminals s
        TestHelpers.gllAccepts g input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar4 random string inputs`` (s: string) =
        let g = TestGrammars.grammar4
        let input = TestHelpers.stringToTerminals s
        TestHelpers.gllAccepts g input = TestHelpers.cykAccepts g input

module GllTreeExtraction =
    [<Fact>]
    let ``Tree extraction for S->aSbS|eps on abab produces tree with correct yield`` () =
        let g = TestGrammars.grammar1

        match gllTree g [ "a"; "b"; "a"; "b" ] with
        | Some tree ->
            let leaves = DerivationTree.leaves tree
            Assert.Equal<string list>([ "a"; "b"; "a"; "b" ], leaves)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``Tree extraction for S->aSb|eps on aabb produces tree with correct yield`` () =
        let g = TestGrammars.grammar2

        match gllTree g [ "a"; "a"; "b"; "b" ] with
        | Some tree ->
            let leaves = DerivationTree.leaves tree
            Assert.Equal<string list>([ "a"; "a"; "b"; "b" ], leaves)
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

    [<Property(MaxTest = 50)>]
    let ``S -> a* matches DFA for a*`` (s: string) =
        let regexText = "a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        TestHelpers.gllAcceptsRsm rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> a* a* matches DFA for a* a*`` (s: string) =
        let regexText = "a * a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        TestHelpers.gllAcceptsRsm rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
        let regexText = "( a | b ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a" || c = "b")

        TestHelpers.gllAcceptsRsm rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
        let regexText = "( a | b ) * ( a | c ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s
            |> List.filter (fun c -> c = "a" || c = "b" || c = "c")

        TestHelpers.gllAcceptsRsm rsm input = TestHelpers.dfaAcceptsRegex dfa input

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

    // ---- Grammar 1: S -> N a* ; N -> (a a) | a ----
    module Grammar1 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.gllAccepts grammar1 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.gllAccepts grammar1 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar1 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar1 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.gllAccepts grammar1 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.gllAccepts grammar1 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.gllAccepts grammar1 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.gllAccepts grammar1 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.gllAccepts grammar1 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.gllAccepts grammar1 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match gllTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match gllTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match gllTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match gllTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 2: S -> a* N ; N -> a | (a a) ----
    module Grammar2 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.gllAccepts grammar2 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.gllAccepts grammar2 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar2 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar2 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.gllAccepts grammar2 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.gllAccepts grammar2 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.gllAccepts grammar2 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.gllAccepts grammar2 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.gllAccepts grammar2 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.gllAccepts grammar2 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match gllTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match gllTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match gllTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match gllTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 3: S -> N* ; N -> a | (a a) ----
    module Grammar3 =
        [<Fact>]
        let ``accepts empty`` () =
            Assert.True(TestHelpers.gllAccepts grammar3 [])

        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.gllAccepts grammar3 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.gllAccepts grammar3 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar3 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar3 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.gllAccepts grammar3 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.gllAccepts grammar3 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.gllAccepts grammar3 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.gllAccepts grammar3 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.gllAccepts grammar3 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: empty`` () =
            match gllTree grammar3 [] with
            | Some tree -> Assert.Equal<string list>([], DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match gllTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match gllTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match gllTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match gllTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 4: S -> a | S S | S S S ----
    module Grammar4 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.gllAccepts grammar4 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.gllAccepts grammar4 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar4 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.gllAccepts grammar4 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.gllAccepts grammar4 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.gllAccepts grammar4 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.gllAccepts grammar4 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.gllAccepts grammar4 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.gllAccepts grammar4 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.gllAccepts grammar4 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match gllTree grammar4 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match gllTree grammar4 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match gllTree grammar4 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match gllTree grammar4 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")
