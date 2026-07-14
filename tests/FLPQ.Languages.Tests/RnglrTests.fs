module RnglrTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

module RnglrAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        Assert.True(
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm TestGrammars.grammarS2a) [ "a" ],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> a rejects eps`` () =
        Assert.True(TestHelpers.rnglrCheckReject TestGrammars.grammarS2a [])

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        let g = TestGrammars.grammarAB

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = TestGrammars.grammar_aS_b

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "a"
                                                                 "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let g = TestGrammars.grammar_aS_b

        Assert.True(TestHelpers.rnglrCheckReject g [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = TestGrammars.grammar1

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "b"
                                                                 "a"
                                                                 "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = TestGrammars.grammar1

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "a"
                                                                 "b"
                                                                 "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> a S b S | eps accepts empty`` () =
        let g = TestGrammars.grammar1

        Assert.True(TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) [], "Should accept and produce tree")

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = TestGrammars.grammar_aSb_eps

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "a"
                                                                 "b"
                                                                 "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        let g = TestGrammars.grammar_aSb_eps
        Assert.True(TestHelpers.rnglrCheckReject g [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps | S S accepts a b a b`` () =
        let g = TestGrammars.grammar2

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "b"
                                                                 "a"
                                                                 "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = TestGrammars.grammar3

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "a"
                                                                 "a"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = TestGrammars.grammar4

        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm g)["a"
                                                                 "a"
                                                                 "a"],
            "Should accept and produce tree"
        )

module RnglrEquivalence =
    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Ab =
        [<Property>]
        let ``RNGLR and CYK agree on grammar1`` (s: string) =
            let g = TestGrammars.grammar1
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``RNGLR and GLL agree on grammar1`` (s: string) =
            let g = TestGrammars.grammar1
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.gllAccepts g input

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module A =
        [<Property>]
        let ``RNGLR and CYK agree on grammar3 (left-recursive)`` (s: string) =
            let g = TestGrammars.grammar3
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

        [<Property>]
        let ``RNGLR and GLL agree on grammar3`` (s: string) =
            let g = TestGrammars.grammar3
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input = TestHelpers.gllAccepts g input

module RnglrRightNullable =
    let private rightNullableGrammar = TestGrammars.grammarRightNullable

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts empty`` () =
        Assert.True(
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm rightNullableGrammar) [],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts a b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm rightNullableGrammar)["a"
                                                                                    "b"],
            "Should accept and produce tree"
        )

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts a a b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm rightNullableGrammar)["a"
                                                                                    "a"
                                                                                    "b"],
            "Should accept and produce tree"
        )

module RnglrReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        let g = TestGrammars.grammarCascade

        Assert.True(TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) [], "Should accept and produce tree")

module RnglrRegexEquivalence =

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module A =
        [<Property(MaxTest = 50)>]
        let ``S -> a* matches DFA for a*`` (s: string) =
            let regexText = "a *"
            let rsm = TestHelpers.buildRegexRsm regexText
            let dfa = TestHelpers.dfaFromRegexRsm rsm
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

        [<Property(MaxTest = 50)>]
        let ``S -> a* a* matches DFA for a* a*`` (s: string) =
            let regexText = "a * a *"
            let rsm = TestHelpers.buildRegexRsm regexText
            let dfa = TestHelpers.dfaFromRegexRsm rsm
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Ab =
        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
            let regexText = "( a | b ) *"
            let rsm = TestHelpers.buildRegexRsm regexText
            let dfa = TestHelpers.dfaFromRegexRsm rsm
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

        [<Property(MaxTest = 50)>]
        let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
            let regexText = "( a | b ) * ( a | c ) *"
            let rsm = TestHelpers.buildRegexRsm regexText
            let dfa = TestHelpers.dfaFromRegexRsm rsm
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals
            TestHelpers.rnglrAccepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

module RnglrGrammarAcceptanceAndTree =

    /// Grammar 1: S -> N a* ; N -> (a a) | a
    let private grammar1 = TestGrammars.grammar11

    /// Grammar 2: S -> a* N ; N -> a | (a a)
    let private grammar2 = TestGrammars.grammar12

    /// Grammar 3: S -> N* ; N -> a | (a a)
    let private grammar3 = TestGrammars.grammar13

    /// Grammar 4: S -> a | S S | S S S (RNGLR skip tree tests — unbounded DFA)
    let private grammar4 = TestGrammars.grammar14

    // ---- Grammar 1 ----
    module Grammar1 =
        let private check g input =
            Assert.True(TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input, "Should accept and produce tree")

        [<Fact>]
        let ``accepts a`` () = check grammar1 [ "a" ]

        [<Fact>]
        let ``accepts aa`` () = check grammar1 [ "a"; "a" ]

        [<Fact>]
        let ``accepts aaa`` () = check grammar1 [ "a"; "a"; "a" ]

        [<Fact>]
        let ``accepts aaaa`` () = check grammar1 [ "a"; "a"; "a"; "a" ]

        [<Fact>]
        let ``rejects empty`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar1 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar1 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar1 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar1 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar1 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar1 [ "a"; "b"; "a"; "a" ])

    // ---- Grammar 2 ----
    module Grammar2 =
        let private check g input =
            Assert.True(TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input, "Should accept and produce tree")

        [<Fact>]
        let ``accepts a`` () = check grammar2 [ "a" ]

        [<Fact>]
        let ``accepts aa`` () = check grammar2 [ "a"; "a" ]

        [<Fact>]
        let ``accepts aaa`` () = check grammar2 [ "a"; "a"; "a" ]

        [<Fact>]
        let ``accepts aaaa`` () = check grammar2 [ "a"; "a"; "a"; "a" ]

        [<Fact>]
        let ``rejects empty`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar2 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar2 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar2 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar2 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar2 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar2 [ "a"; "b"; "a"; "a" ])

    // ---- Grammar 3 ----
    module Grammar3 =
        let private check g input =
            Assert.True(TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) input, "Should accept and produce tree")

        [<Fact>]
        let ``accepts empty`` () = check grammar3 []

        [<Fact>]
        let ``accepts a`` () = check grammar3 [ "a" ]

        [<Fact>]
        let ``accepts aa`` () = check grammar3 [ "a"; "a" ]

        [<Fact>]
        let ``accepts aaa`` () = check grammar3 [ "a"; "a"; "a" ]

        [<Fact>]
        let ``accepts aaaa`` () = check grammar3 [ "a"; "a"; "a"; "a" ]

        [<Fact>]
        let ``rejects b`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar3 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar3 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar3 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar3 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar3 [ "a"; "b"; "a"; "a" ])

    // ---- Grammar 4: S -> a | S S | S S S (acceptance only — SPPF extraction does not support this grammar) ----
    module Grammar4 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithoutSppfValidation (TestHelpers.grammarToRsm grammar4) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.rnglrAcceptsWithoutSppfValidation (TestHelpers.grammarToRsm grammar4) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(
                TestHelpers.rnglrAcceptsWithoutSppfValidation (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a" ]
            )

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(
                TestHelpers.rnglrAcceptsWithoutSppfValidation (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a"; "a" ]
            )

        [<Fact>]
        let ``rejects empty`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar4 [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar4 [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar4 [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar4 [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar4 [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.True(TestHelpers.rnglrCheckReject grammar4 [ "a"; "b"; "a"; "a" ])

    /// Cross-algorithm equivalence: GLL ≡ RNGLR ≡ CYK for all 4 grammars.
    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module CrossAlgorithmEquivalence =
        let private inputFrom (s: string) =
            s.Replace("; ", "") |> TestHelpers.stringToTerminals

        [<Property>]
        let ``Grammar 1: GLL == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar1 input = TestHelpers.cykAccepts grammar1 input

        [<Property>]
        let ``Grammar 1: RNGLR == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammar1) input = TestHelpers.cykAccepts grammar1 input

        [<Property>]
        let ``Grammar 1: GLL == RNGLR`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar1 input = TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammar1) input

        [<Property>]
        let ``Grammar 2: GLL == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar2 input = TestHelpers.cykAccepts grammar2 input

        [<Property>]
        let ``Grammar 2: RNGLR == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammar2) input = TestHelpers.cykAccepts grammar2 input

        [<Property>]
        let ``Grammar 2: GLL == RNGLR`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar2 input = TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammar2) input

        [<Property>]
        let ``Grammar 3: GLL == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar3 input = TestHelpers.cykAccepts grammar3 input

        [<Property>]
        let ``Grammar 3: RNGLR == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammar3) input = TestHelpers.cykAccepts grammar3 input

        [<Property>]
        let ``Grammar 3: GLL == RNGLR`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar3 input = TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammar3) input

        [<Property>]
        let ``Grammar 4: GLL == CYK`` (s: string) =
            let input = inputFrom s
            TestHelpers.gllAccepts grammar4 input = TestHelpers.cykAccepts grammar4 input

module RnglrGrammar159A =
    let private grammar = TestGrammars.grammar1

    [<Fact>]
    let ``S -> a S b S | eps accepts and yields tree: a a b a b b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"],
            "Should produce a tree"
        )

    [<Fact>]
    let ``S -> a S b S | eps accepts and yields tree: a a b a b b a b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"
                                                                       "a"
                                                                       "b"],
            "Should produce a tree"
        )

    [<Fact>]
    let ``S -> a S b S | eps accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"],
            "Should produce a tree"
        )

module RnglrGrammar159B =
    let private grammar = TestGrammars.grammarSaSb_eps

    [<Fact>]
    let ``S -> S a S b | eps accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"],
            "Should produce a tree"
        )

    [<Fact>]
    let ``S -> S a S b | eps accepts and yields tree: a a b a b b a b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"
                                                                       "a"
                                                                       "b"],
            "Should produce a tree"
        )

module RnglrGrammar159C =
    let private grammar = TestGrammars.grammar2

    [<Fact>]
    let ``S -> S S | a S b | eps accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"],
            "Should produce a tree"
        )

    [<Fact>]
    let ``S -> S S | a S b | eps accepts and yields tree: a a b a b b a b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts(TestHelpers.grammarToRsm grammar)["a"
                                                                       "a"
                                                                       "b"
                                                                       "a"
                                                                       "b"
                                                                       "b"
                                                                       "a"
                                                                       "b"],
            "Should produce a tree"
        )

module RnglrGrammar159D =
    let private rsm = TestHelpers.buildRegexRsm "(a S b)*"

    [<Fact>]
    let ``S -> (a S b)* accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts rsm [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ],
            "Should produce a tree"
        )

    let private rsm2 =
        let r = RsmBuilder.buildRSMFromText "S -> S1 S2\nS1 -> (a S1 b)*\nS2 -> (c S2 d)*"
        { r with StartBlock = Nonterminal "S" }

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts and yields tree: a a a b a b b a b b`` () =
        Assert.True(
            TestHelpers.rnglrAccepts rsm2 [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ],
            "Should produce a tree"
        )

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts and yields tree: a a a b a b b a b b c c d c d d`` () =
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

        Assert.True(TestHelpers.rnglrAccepts rsm2 input, "Should produce a tree")

    [<Fact>]
    let ``S -> S1 S2; S1 -> (a S1 b)*; S2 -> (c S2 d)* accepts and yields tree: a a a b a b b a b b c d`` () =
        Assert.True(
            TestHelpers.rnglrAccepts rsm2 [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b"; "c"; "d" ],
            "Should produce a tree"
        )

module RnglrPropertyTreeYield =
    let private grammarG1 = TestGrammars.grammar1
    let private grammarG2 = TestGrammars.grammar2
    let private grammarG3 = TestGrammars.grammar3
    let private grammarG4 = TestGrammars.grammar4

    let private grammarG5 = TestGrammars.grammar11

    let private grammarG6 = TestGrammars.grammar12

    let private grammarG7 = TestGrammars.grammar13

    let private grammarG8 = TestGrammars.grammar14

    [<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
    module Ab =
        [<Property>]
        let ``S -> a S b S | eps tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG1) input |> ignore

            true

        [<Property>]
        let ``S -> S S | a S b | eps tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG2) input |> ignore

            true

    [<Properties(Arbitrary = [| typeof<AStringGenerators> |])>]
    module A =
        [<Property>]
        let ``S -> a S | a tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG3) input |> ignore

            true

        [<Property>]
        let ``S -> S a | a tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG4) input |> ignore

            true

        [<Property>]
        let ``S -> N a* tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG5) input |> ignore

            true

        [<Property>]
        let ``S -> a* N tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG6) input |> ignore

            true

        [<Property>]
        let ``S -> N* tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm grammarG7) input |> ignore

            true

        [<Property>]
        let ``S -> a | S S | S S S tree yield`` (s: string) =
            let input = s.Replace(" ", "") |> TestHelpers.stringToTerminals

            TestHelpers.rnglrAcceptsWithoutSppfValidation (TestHelpers.grammarToRsm grammarG8) input
            |> ignore

            true

module SppfDotTests =

    let private buildSppf (grammarText: string) (input: string list) : SPPF<string, string> =
        let rsm = RsmBuilder.buildRSMFromText grammarText
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex, extRsm, vc = TestHelpers.buildPathIndexForRsm rsmFixed input

        let startGlobal =
            match extRsm.BlockStart.TryGetValue(extRsm.StartBlock) with
            | true, gs -> gs
            | false, _ -> 0

        let startFinalStates = RSM.blockFinalStates extRsm.StartBlock extRsm

        let rootRanges =
            startFinalStates
            |> Set.toList
            |> List.map (fun finalState ->
                { FromState = startGlobal
                  FromVertex = 0
                  ToState = finalState
                  ToVertex = vc - 1 })

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

module RnglrEpsilonGrammars =

    let private checkAccepts (g: Grammar<string, string>) (desc: string) =
        Assert.True(TestHelpers.rnglrAccepts (TestHelpers.grammarToRsm g) [], $"Should accept empty string: {desc}")

    let private checkRejects (g: Grammar<string, string>) (testStr: string list) (desc: string) =
        Assert.True(TestHelpers.rnglrCheckReject g testStr, $"Should reject {testStr}: {desc}")

    let private rejectInputs =
        [ [ "a" ]; [ "b" ]; [ "a"; "b" ]; [ "a"; "a" ]; [ "b"; "b" ] ]

    [<Fact>]
    let ``S -> eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarEps "S -> eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarEps input "S -> eps"

    [<Fact>]
    let ``S -> N; N -> eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarNtoEps "S -> N; N -> eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarNtoEps input "S -> N; N -> eps"

    [<Fact>]
    let ``S -> N N; N -> eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarNNtoEps "S -> N N; N -> eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarNNtoEps input "S -> N N; N -> eps"

    [<Fact>]
    let ``S -> N*; N -> eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarNStarEps "S -> N*; N -> eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarNStarEps input "S -> N*; N -> eps"

    [<Fact>]
    let ``S -> S S | eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarSSeps "S -> S S | eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarSSeps input "S -> S S | eps"

    [<Fact>]
    let ``S -> A B; A -> C D; B -> D C; D -> eps; C -> eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarChainEps "S -> A B; A -> C D; B -> D C; D -> eps; C -> eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarChainEps input "S -> A B; A -> C D; B -> D C; D -> eps; C -> eps"

    [<Fact>]
    let ``S -> A | B; A -> C D; B -> D C; D -> eps; C -> eps accepts empty, rejects non-empty`` () =
        checkAccepts TestGrammars.grammarAltEps "S -> A | B; A -> C D; B -> D C; D -> eps; C -> eps"

        for input in rejectInputs do
            checkRejects TestGrammars.grammarAltEps input "S -> A | B; A -> C D; B -> D C; D -> eps; C -> eps"

module RnglrGrammarTests =

    let private checkRsmAccepts (ebnfText: string) (input: string list) : unit =
        let rsm = RsmBuilder.buildRSMFromText ebnfText
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex, extRsm, vc = TestHelpers.buildPathIndexForRsm rsmFixed input
        Assert.True(Rnglr.isAccepted pathIndex extRsm vc, $"Should accept {input}: {ebnfText}")

    let private checkRsmRejects (ebnfText: string) (input: string list) : unit =
        let rsm = RsmBuilder.buildRSMFromText ebnfText
        let startNt = (RSM.startBlock rsm).Nonterminal
        let rsmFixed = { rsm with StartBlock = startNt }
        let pathIndex, extRsm, vc = TestHelpers.buildPathIndexForRsm rsmFixed input
        Assert.False(Rnglr.isAccepted pathIndex extRsm vc, $"Should reject {input}: {ebnfText}")

    // Grammar 1: S -> N a* ; N -> (a a) | a
    module Grammar1 =
        let private g = "S -> N a*\nN -> (a a) | a"

        let private acceptInputs =
            [ [ "a" ]; [ "a"; "a" ]; [ "a"; "a"; "a" ]; [ "a"; "a"; "a"; "a" ] ]

        let private rejectInputs =
            [ []
              [ "b" ]
              [ "a"; "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "a"; "a"; "b" ]
              [ "a"; "b"; "a"; "a" ] ]

        [<Fact>]
        let ``S -> N a* accepts valid strings`` () =
            for input in acceptInputs do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> N a* rejects invalid strings`` () =
            for input in rejectInputs do
                checkRsmRejects g input

    // Grammar 2: S -> a* N ; N -> a | (a a)
    module Grammar2 =
        let private g = "S -> a* N\nN -> a | (a a)"

        let private acceptInputs =
            [ [ "a" ]; [ "a"; "a" ]; [ "a"; "a"; "a" ]; [ "a"; "a"; "a"; "a" ] ]

        let private rejectInputs =
            [ []
              [ "b" ]
              [ "a"; "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "a"; "a"; "b" ]
              [ "a"; "b"; "a"; "a" ] ]

        [<Fact>]
        let ``S -> a* N accepts valid strings`` () =
            for input in acceptInputs do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> a* N rejects invalid strings`` () =
            for input in rejectInputs do
                checkRsmRejects g input

    // Grammar 3: S -> N* ; N -> a | (a a)
    module Grammar3 =
        let private g = "S -> N*\nN -> a | (a a)"

        let private acceptInputs =
            [ []; [ "a" ]; [ "a"; "a" ]; [ "a"; "a"; "a" ]; [ "a"; "a"; "a"; "a" ] ]

        let private rejectInputs =
            [ [ "b" ]
              [ "a"; "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "a"; "a"; "b" ]
              [ "a"; "b"; "a"; "a" ] ]

        [<Fact>]
        let ``S -> N* accepts valid strings`` () =
            for input in acceptInputs do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> N* rejects invalid strings`` () =
            for input in rejectInputs do
                checkRsmRejects g input

    // Grammar 4: S -> a | S S | S S S
    module Grammar4 =
        let private g = "S -> a\nS -> S S\nS -> S S S"

        let private acceptInputs =
            [ [ "a" ]; [ "a"; "a" ]; [ "a"; "a"; "a" ]; [ "a"; "a"; "a"; "a" ] ]

        let private rejectInputs =
            [ []
              [ "b" ]
              [ "a"; "b" ]
              [ "a"; "a"; "b" ]
              [ "a"; "a"; "a"; "b" ]
              [ "a"; "b"; "a"; "a" ] ]

        [<Fact>]
        let ``S -> a | S S | S S S accepts valid strings`` () =
            for input in acceptInputs do
                checkRsmAccepts g input

        [<Fact>]
        let ``S -> a | S S | S S S rejects invalid strings`` () =
            for input in rejectInputs do
                checkRsmRejects g input
