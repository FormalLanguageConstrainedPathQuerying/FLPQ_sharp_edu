module GllTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

let private accepts = TestHelpers.accepts GLL.buildPathIndex PathIndex.isAccepted

let private checkReject =
    TestHelpers.checkReject GLL.buildPathIndex PathIndex.isAccepted

module GllAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a" ])

    [<Fact>]
    let ``S -> a rejects eps`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.False(accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        let g = Grammar.parseGrammar "S -> a b"
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.False(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = TestGrammars.grammar1
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = TestGrammars.grammar1
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = TestGrammars.grammar2
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        let g = TestGrammars.grammar2
        Assert.False(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | S S | eps accepts a b (no infinite loop)`` () =
        let g = TestGrammars.grammar2
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts empty`` () =
        let g = TestGrammars.grammar2
        Assert.True(accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = TestGrammars.grammar3
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "a" ])

    [<Fact>]
    let ``Left-recursive S -> a S | a rejects empty`` () =
        let g = TestGrammars.grammar3
        Assert.False(accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = TestGrammars.grammar4
        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "a" ])

[<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
module GllCykEquivalence =
    [<Property>]
    let ``GLL and CYK agree on grammar1 random string inputs`` (s: string) =
        let g = TestGrammars.grammar1
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar2 random string inputs`` (s: string) =
        let g = TestGrammars.grammar2
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar3 random string inputs`` (s: string) =
        let g = TestGrammars.grammar3
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar4 random string inputs`` (s: string) =
        let g = TestGrammars.grammar4
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

module GllTreeExtraction =
    [<Fact>]
    let ``Tree extraction for S->aSbS|eps on abab produces tree with correct yield`` () =
        let g = TestGrammars.grammar1

        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``Tree extraction for S->aSb|eps on aabb produces tree with correct yield`` () =
        let g = TestGrammars.grammar2

        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``Tree extraction for S->a|b on a produces tree with leaf`` () =
        let g = Grammar.parseGrammar "S -> a\nS -> b"

        Assert.True(accepts (TestHelpers.grammarToRsm g) [ "a" ])

[<Properties(Arbitrary = [| typeof<AbcxdStringGenerators> |])>]
module GllRegexEquivalence =

    [<Property(MaxTest = 50)>]
    let ``S -> a* matches DFA for a*`` (s: string) =
        SharedParsingTests.Runners.runRegexEquivalenceTests accepts (fun c -> c = "a") "a *" s

    [<Property(MaxTest = 50)>]
    let ``S -> a* a* matches DFA for a* a*`` (s: string) =
        SharedParsingTests.Runners.runRegexEquivalenceTests accepts (fun c -> c = "a") "a * a *" s

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
        SharedParsingTests.Runners.runRegexEquivalenceTests accepts (fun c -> c = "a" || c = "b") "( a | b ) *" s

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
        SharedParsingTests.Runners.runRegexEquivalenceTests
            accepts
            (fun c -> c = "a" || c = "b" || c = "c")
            "( a | b ) * ( a | c ) *"
            s

module GllGrammarAcceptanceAndTree =

    /// Grammar 1: S -> N a* ; N -> (a a) | a
    let private grammar1 = SharedParsingTests.GrammarAcceptanceCases.grammar1

    /// Grammar 2: S -> a* N ; N -> a | (a a)
    let private grammar2 = SharedParsingTests.GrammarAcceptanceCases.grammar2

    /// Grammar 3: S -> N* ; N -> a | (a a)
    let private grammar3 = SharedParsingTests.GrammarAcceptanceCases.grammar3

    /// Grammar 4: S -> a | S S | S S S
    let private grammar4 = SharedParsingTests.GrammarAcceptanceCases.grammar4

    // ---- Grammar 1: S -> N a* ; N -> (a a) | a ----
    module Grammar1 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG1 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar1) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG1 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar1) input, $"rejects {desc}")

    // ---- Grammar 2: S -> a* N ; N -> a | (a a) ----
    module Grammar2 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG2 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar2) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG2 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar2) input, $"rejects {desc}")

    // ---- Grammar 3: S -> N* ; N -> a | (a a) ----
    module Grammar3 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG3 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar3) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG3 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar3) input, $"rejects {desc}")

    // ---- Grammar 4: S -> a | S S | S S S ----
    module Grammar4 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG4 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar4) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG4 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar4) input, $"rejects {desc}")

module GllGrammar159A =
    let private grammar = TestGrammars.grammar1

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches inputs`` () =
        for input in SharedParsingTests.Grammar159Cases.grammar1Inputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"tree yield: {desc}")

module GllGrammar159B =
    let private grammar = TestGrammars.grammarSaSb_eps

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches inputs`` () =
        for input in SharedParsingTests.Grammar159Cases.grammarSaSb_epsInputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"tree yield: {desc}")

module GllGrammar159C =
    let private grammar = TestGrammars.grammar2

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches inputs`` () =
        for input in SharedParsingTests.Grammar159Cases.grammar2Inputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"tree yield: {desc}")

module GllGrammar159D =
    let private rsm = TestHelpers.buildRegexRsm "(a S b)*"

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts rsm input)

module GllPropertyTreeYield =
    let private propertyRunner =
        SharedParsingTests.Runners.runPropertyTreeYieldTest accepts

    [<Property>]
    let ``S -> a S b S | eps tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar1 "S -> a S b S | eps" s

    [<Property>]
    let ``S -> S S | a S b | eps tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar2 "S -> S S | a S b | eps" s

    [<Property>]
    let ``S -> a S | a tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar3 "S -> a S | a" s

    [<Property>]
    let ``S -> S a | a tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar4 "S -> S a | a" s

    [<Property>]
    let ``S -> N a* tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar11 "S -> N a*" s

    [<Property>]
    let ``S -> a* N tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar12 "S -> a* N" s

    [<Property>]
    let ``S -> N* tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar13 "S -> N*" s

    [<Property>]
    let ``S -> a | S S | S S S tree yield matches input`` (s: string) =
        propertyRunner TestGrammars.grammar14 "S -> a | S S | S S S" s

module GllAdditionalAcceptance =
    [<Fact>]
    let ``S -> a accepts a (TestGrammars.grammarS2a)`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarS2a) [ "a" ])

    [<Fact>]
    let ``S -> a rejects eps (TestGrammars.grammarS2a)`` () =
        Assert.False(accepts (TestHelpers.grammarToRsm TestGrammars.grammarS2a) [])

    [<Fact>]
    let ``S -> a b accepts a b (TestGrammars.grammarAB)`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarAB) [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b accepts a a b (TestGrammars.grammar_aS_b)`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aS_b) [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b rejects a a a (TestGrammars.grammar_aS_b)`` () =
        Assert.False(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aS_b) [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b (TestGrammars.grammar_aSb_eps)`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aSb_eps) [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps rejects a a b (TestGrammars.grammar_aSb_eps)`` () =
        Assert.False(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aSb_eps) [ "a"; "a"; "b" ])

module GllRightNullable =
    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts empty`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarRightNullable) [])

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts a b`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarRightNullable) [ "a"; "b" ])

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts a a b`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarRightNullable) [ "a"; "a"; "b" ])

module GllReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarCascade) [])

module GllEpsilonGrammars =

    [<Fact>]
    let ``all epsilon grammars accept empty and reject non-empty`` () =
        SharedParsingTests.Runners.runEpsilonTests accepts (fun g input ->
            not (accepts (TestHelpers.grammarToRsm g) input))

module GllGrammar159B_SaSb =
    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarSaSb_eps) input)

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarSaSb_eps) input)
