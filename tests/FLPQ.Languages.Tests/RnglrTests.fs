module RnglrTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

/// Extract derivation tree from RNGLR via SPPF-based extraction.
let private rnglrTree (g: Grammar<string, string>) (input: string list) : DerivationTree<string, string> option =
    let rsm = TestHelpers.grammarToRsm g
    let freshStart = Nonterminal("S'")
    let graph = TestHelpers.terminalsToGraph input
    let startNt = (RSM.startBlock rsm).Nonterminal
    let rsmFixed = { rsm with StartBlock = startNt }
    let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
    TestHelpers.assertPathIndexInvariant "rnglrTree" pathIndex
    let vc = Graph.vertexCount graph

    if not (Rnglr.isAccepted pathIndex vc) then
        None
    else
        let rootRanges =
            [ { FromState = 0
                FromVertex = 0
                ToState = 1
                ToVertex = vc - 1 } ]

        let sppf = Sppf.buildSppfFromIndex pathIndex rootRanges
        TestHelpers.assertSppfInvariant sppf

        let extRsm = RSM.extendWithStart freshStart rsmFixed
        let flat = RSM.flattenRsm extRsm
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

        let rootRanges =
            [ { FromState = 0
                FromVertex = 0
                ToState = 1
                ToVertex = vc - 1 } ]

        rootRanges
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

module RnglrAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a" ])

    [<Fact>]
    let ``S -> a rejects eps`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck g [])

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        let g = Grammar.parseGrammar "S -> a b"
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts empty`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [])

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = Grammar.parseGrammar "S -> a S b\nS -> eps"
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        let g = Grammar.parseGrammar "S -> a S b\nS -> eps"
        Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps | S S accepts a b a b`` () =
        let g = TestGrammars.grammar2
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = TestGrammars.grammar3
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = TestGrammars.grammar4
        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "a" ])

module RnglrEquivalence =
    [<Property>]
    let ``RNGLR and CYK agree on grammar1`` (s: string) =
        let g = TestGrammars.grammar1
        let input = TestHelpers.stringToTerminals s
        TestHelpers.rnglrAcceptsWithSppfCheck g input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``RNGLR and CYK agree on grammar3 (left-recursive)`` (s: string) =
        let g = TestGrammars.grammar3
        let input = TestHelpers.stringToTerminals s
        TestHelpers.rnglrAcceptsWithSppfCheck g input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``RNGLR and GLL agree on grammar1`` (s: string) =
        let g = TestGrammars.grammar1
        let input = TestHelpers.stringToTerminals s
        TestHelpers.rnglrAcceptsWithSppfCheck g input = TestHelpers.gllAccepts g input

    [<Property>]
    let ``RNGLR and GLL agree on grammar3`` (s: string) =
        let g = TestGrammars.grammar3
        let input = TestHelpers.stringToTerminals s
        TestHelpers.rnglrAcceptsWithSppfCheck g input = TestHelpers.gllAccepts g input

module RnglrRightNullable =
    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts empty`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> A B
        A -> a A
        A -> eps
        B -> b B
        B -> eps
        "

        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [])

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts a b`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> A B
        A -> a A
        A -> eps
        B -> b B
        B -> eps
        "

        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "b" ])

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts a a b`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> A B
        A -> a A
        A -> eps
        B -> b B
        B -> eps
        "

        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [ "a"; "a"; "b" ])

module RnglrReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        let g =
            Grammar.parseGrammar
                "
        S -> A
        A -> B
        B -> eps
        "

        Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck g [])

module RnglrRegexEquivalence =

    let private rnglrAcceptsRegex (rsm: RSM<string, string>) (input: string list) : bool =
        let freshStart = Nonterminal("S'")
        let graph = TestHelpers.terminalsToGraph input

        let rsmFixed =
            { rsm with
                StartBlock = Nonterminal "S" }

        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        TestHelpers.assertPathIndexInvariant "rnglrRegexEquiv" pathIndex
        let vc = Graph.vertexCount graph

        if Rnglr.isAccepted pathIndex vc then
            let rootRanges =
                [ { FromState = 0
                    FromVertex = 0
                    ToState = 1
                    ToVertex = vc - 1 } ]

            let sppf = Sppf.buildSppfFromIndex pathIndex rootRanges
            TestHelpers.assertSppfInvariant sppf
            true
        else
            false

    [<Property(MaxTest = 50)>]
    let ``S -> a* matches DFA for a*`` (s: string) =
        let regexText = "a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        rnglrAcceptsRegex rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> a* a* matches DFA for a* a*`` (s: string) =
        let regexText = "a * a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        rnglrAcceptsRegex rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
        let regexText = "( a | b ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a" || c = "b")

        rnglrAcceptsRegex rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
        let regexText = "( a | b ) * ( a | c ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s
            |> List.filter (fun c -> c = "a" || c = "b" || c = "c")

        rnglrAcceptsRegex rsm input = TestHelpers.dfaAcceptsRegex dfa input

module RnglrGrammarAcceptanceAndTree =

    /// Grammar 1: S -> N a* ; N -> (a a) | a
    let private grammar1 =
        Grammar.parseGrammar
            "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

    /// Grammar 2: S -> a* N ; N -> a | (a a)
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
    let private grammar3 =
        Grammar.parseGrammar
            "
        S -> eps
        S -> a a S
        S -> a S
        "

    /// Grammar 4: S -> a | S S | S S S (RNGLR skip tree tests — unbounded DFA)
    let private grammar4 = Grammar.parseGrammar "S -> a\nS -> S S\nS -> S S S"

    // ---- Grammar 1 ----
    module Grammar1 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar1 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match rnglrTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match rnglrTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match rnglrTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match rnglrTree grammar1 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 2 ----
    module Grammar2 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar2 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match rnglrTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match rnglrTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match rnglrTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match rnglrTree grammar2 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 3 ----
    module Grammar3 =
        [<Fact>]
        let ``accepts empty`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [])

        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar3 [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: empty`` () =
            match rnglrTree grammar3 [] with
            | Some tree -> Assert.Equal<string list>([], DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match rnglrTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match rnglrTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match rnglrTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match rnglrTree grammar3 input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 4: acceptance only (RSM builder can't handle S->S S) ----
    module Grammar4 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.rnglrAccepts grammar4 [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.rnglrAccepts grammar4 [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.rnglrAcceptsWithSppfCheck grammar4 [ "a"; "b"; "a"; "a" ])

    /// Cross-algorithm equivalence: GLL ≡ RNGLR ≡ CYK for all 4 grammars.
    module CrossAlgorithmEquivalence =

        [<Property>]
        let ``Grammar 1: GLL == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar1 input = TestHelpers.cykAccepts grammar1 input

        [<Property>]
        let ``Grammar 1: RNGLR == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.rnglrAcceptsWithSppfCheck grammar1 input = TestHelpers.cykAccepts grammar1 input

        [<Property>]
        let ``Grammar 1: GLL == RNGLR`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar1 input = TestHelpers.rnglrAcceptsWithSppfCheck grammar1 input

        [<Property>]
        let ``Grammar 2: GLL == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar2 input = TestHelpers.cykAccepts grammar2 input

        [<Property>]
        let ``Grammar 2: RNGLR == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.rnglrAcceptsWithSppfCheck grammar2 input = TestHelpers.cykAccepts grammar2 input

        [<Property>]
        let ``Grammar 2: GLL == RNGLR`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar2 input = TestHelpers.rnglrAcceptsWithSppfCheck grammar2 input

        [<Property>]
        let ``Grammar 3: GLL == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar3 input = TestHelpers.cykAccepts grammar3 input

        [<Property>]
        let ``Grammar 3: RNGLR == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.rnglrAcceptsWithSppfCheck grammar3 input = TestHelpers.cykAccepts grammar3 input

        [<Property>]
        let ``Grammar 3: GLL == RNGLR`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar3 input = TestHelpers.rnglrAcceptsWithSppfCheck grammar3 input

        [<Property>]
        let ``Grammar 4: GLL == CYK`` (s: string) =
            let input = TestHelpers.stringToTerminals s
            TestHelpers.gllAccepts grammar4 input = TestHelpers.cykAccepts grammar4 input

        [<Property>]
        let ``Grammar 4: GLL == CYK (filter a chars)`` (s: string) =
            let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
            TestHelpers.gllAccepts grammar4 input = TestHelpers.cykAccepts grammar4 input

module RnglrGrammar159A =
    let private grammar = TestGrammars.grammar1

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a b a b b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module RnglrGrammar159B =
    let private grammar = Grammar.parseGrammar "S -> S a S b\nS -> eps"

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module RnglrGrammar159C =
    let private grammar = TestGrammars.grammar2

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        match rnglrTree grammar input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module RnglrGrammar159D =
    let private rnglrTreeRsm (rsm: RSM<string, string>) (input: string list) : DerivationTree<string, string> option =
        let freshStart = Nonterminal("S'")
        let graph = TestHelpers.terminalsToGraph input
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        TestHelpers.assertPathIndexInvariant "rnglrTreeRsm" pathIndex
        let vc = Graph.vertexCount graph

        if not (Rnglr.isAccepted pathIndex vc) then
            None
        else
            let rootRanges =
                [ { FromState = 0
                    FromVertex = 0
                    ToState = 1
                    ToVertex = vc - 1 } ]

            let sppf = Sppf.buildSppfFromIndex pathIndex rootRanges
            TestHelpers.assertSppfInvariant sppf

            let extRsm = RSM.extendWithStart freshStart rsmFixed
            let flat = RSM.flattenRsm extRsm
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

            let rootRanges =
                [ { FromState = 0
                    FromVertex = 0
                    ToState = 1
                    ToVertex = vc - 1 } ]

            rootRanges
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

    let private rsm = TestHelpers.buildRegexRsm "(a S b)*"

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match rnglrTreeRsm rsm input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    let private rsm2 =
        let r = RsmBuilder.buildRSMFromText "S -> S1 S2\nS1 -> (a S1 b)*\nS2 -> (c S2 d)*"
        { r with StartBlock = Nonterminal "S" }

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match rnglrTreeRsm rsm2 input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* tree yield matches input: a a a b a b b a b b c c d c d d`` () =
        let input =
            [ "a"
              "a"
              "a"
              "b"
              "a"
              "b"
              "b"
              "a"
              "b"
              "b"
              "c"
              "c"
              "d"
              "c"
              "d"
              "d" ]

        match rnglrTreeRsm rsm2 input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* tree yield matches input: a a a b a b b a b b c d`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b"; "c"; "d" ]

        match rnglrTreeRsm rsm2 input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module RnglrPropertyTreeYield =
    let private grammarG1 = TestGrammars.grammar1
    let private grammarG2 = TestGrammars.grammar2
    let private grammarG3 = TestGrammars.grammar3
    let private grammarG4 = TestGrammars.grammar4

    let private grammarG5 =
        Grammar.parseGrammar
            "
        S -> a a A
        S -> a A
        A -> a A
        A -> eps
        "

    let private grammarG6 =
        Grammar.parseGrammar
            "
        S -> a
        S -> a a
        S -> a a A
        S -> a a a A
        A -> a A
        A -> eps
        "

    let private grammarG7 =
        Grammar.parseGrammar
            "
        S -> eps
        S -> a a S
        S -> a S
        "

    let private grammarG8 = Grammar.parseGrammar "S -> a\nS -> S S\nS -> S S S"

    [<Property>]
    let ``S -> a S b S | eps tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG1 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> S S | a S b | eps tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG2 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> a S | a tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG3 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> S a | a tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG4 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> N a* tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG5 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> a* N tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG6 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> N* tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG7 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> a | S S | S S S tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match rnglrTree grammarG8 input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

module SppfDotTests =

    let private buildSppf (grammarText: string) (input: string list) : SPPF<string, string> =
        let rsm = RsmBuilder.buildRSMFromText grammarText
        let freshStart = Nonterminal("S'")
        let graph = TestHelpers.terminalsToGraph input
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex = Rnglr.buildPathIndex freshStart rsmFixed graph
        TestHelpers.assertPathIndexInvariant "buildSppf" pathIndex
        let vc = Graph.vertexCount graph

        let extRsm = RSM.extendWithStart freshStart rsmFixed
        let flat = RSM.flattenRsm extRsm
        let stateInfo = flat.StateInfo
        let blockStart = flat.BlockStart

        let (Nonterminal startNtName) = (RSM.startBlock rsmFixed).Nonterminal

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

        let rootRanges =
            [ { FromState = 0
                FromVertex = 0
                ToState = 1
                ToVertex = vc - 1 } ]

        Sppf.buildSppfFromIndex pathIndex rootRanges

    [<Fact>]
    let ``RNGLR SPPF contains all terminals for S->aSb|SS|eps with aababb`` () =
        let grammarText = "S -> a S b\nS -> S S\nS -> eps\n"
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

        let sppf = buildSppf grammarText input

        TestHelpers.assertSppfInvariant sppf

        let terminalPositions =
            Graph.vertices sppf.Graph
            |> List.choose (fun (_, v) ->
                match v with
                | SppfNodeInfo.SppfTerminal(Terminal t, l, r) -> Some(t, l, r)
                | _ -> None)
            |> Set.ofList

        let expected: Set<string * int * int> =
            set [ ("a", 0, 1); ("a", 1, 2); ("b", 2, 3); ("a", 3, 4); ("b", 4, 5); ("b", 5, 6) ]

        Assert.Equal<Set<string * int * int>>(expected, terminalPositions)

    [<Fact>]
    let ``RNGLR SPPF has root nodes for S->aSb|SS|eps with aababb`` () =
        let grammarText = "S -> a S b\nS -> S S\nS -> eps\n"
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

        let sppf = buildSppf grammarText input

        TestHelpers.assertSppfInvariant sppf

        Assert.NotEmpty(sppf.RootIndices)
