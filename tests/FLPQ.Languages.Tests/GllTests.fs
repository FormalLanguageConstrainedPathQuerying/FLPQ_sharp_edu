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

let private dyck1 = LanguageRegistry.Dyck1
let private aplus = LanguageRegistry.APlus
let private singleA = LanguageRegistry.SingleA
let private singleAB = LanguageRegistry.SingleAB
let private anb = LanguageRegistry.ANB
let private anbn = LanguageRegistry.ANBN

module GllAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        for s in singleA.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm singleA.Grammars[0].Grammar) s)

    [<Fact>]
    let ``S -> a rejects eps`` () =
        for s in singleA.RejectStrings do
            Assert.True(checkReject singleA.Grammars[0].Rsm s)

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        for s in singleAB.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm singleAB.Grammars[0].Grammar) s)

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = anb.Grammars[0].Grammar
        let s = anb.AcceptStrings[2]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let s = anb.RejectStrings[3]
        Assert.True(checkReject anb.Grammars[0].Rsm s)

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = dyck1.Grammars[0].Grammar
        let s = dyck1.AcceptStrings[0]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = dyck1.Grammars[0].Grammar
        let s = dyck1.AcceptStrings[3]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = anbn.Grammars[0].Grammar

        for s in anbn.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        for s in anbn.RejectStrings do
            Assert.True(checkReject anbn.Grammars[0].Rsm s)

    [<Fact>]
    let ``S -> a S b | S S | eps accepts a b (no infinite loop)`` () =
        let g = dyck1.Grammars[1].Grammar
        let s = dyck1.AcceptStrings[1]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``S -> a S b | S S | eps accepts empty`` () =
        let g = dyck1.Grammars[1].Grammar
        Assert.True(accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = aplus.Grammars[0].Grammar
        let s = aplus.AcceptStrings[2]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``Left-recursive S -> a S | a rejects empty`` () =
        Assert.True(checkReject aplus.Grammars[0].Rsm [])

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = aplus.Grammars[1].Grammar
        let s = aplus.AcceptStrings[2]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

[<Properties(Arbitrary = [| typeof<AbStringGenerators> |])>]
module GllCykEquivalence =
    [<Property>]
    let ``GLL and CYK agree on Dyck1 grammar1 random string inputs`` (s: string) =
        let g = dyck1.Grammars[0].Grammar
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on Dyck1 grammar2 random string inputs`` (s: string) =
        let g = dyck1.Grammars[1].Grammar
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on APlus grammar3 random string inputs`` (s: string) =
        let g = aplus.Grammars[0].Grammar
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on APlus grammar4 random string inputs`` (s: string) =
        let g = aplus.Grammars[1].Grammar
        let input = TestHelpers.stringToTerminals s
        accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

module GllTreeExtraction =
    [<Fact>]
    let ``Tree extraction for S->aSbS|eps on abab produces tree with correct yield`` () =
        let g = dyck1.Grammars[0].Grammar
        let s = dyck1.AcceptStrings[0]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``Tree extraction for S->aSb|eps on aabb produces tree with correct yield`` () =
        let g = anbn.Grammars[0].Grammar
        let s = anbn.AcceptStrings[2]
        Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``Tree extraction for S->a|b on a produces tree with leaf`` () =
        let lang = LanguageRegistry.AltAB
        let g = lang.Grammars[0].Grammar

        for s in lang.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s)

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
    let private grammar1 = TestGrammars.grammar11
    let private grammar2 = TestGrammars.grammar12
    let private grammar3 = TestGrammars.grammar13
    let private grammar4 = TestGrammars.grammar14

    module Grammar1 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG1 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar1) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG1 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar1) input, $"rejects {desc}")

    module Grammar2 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG2 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar2) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG2 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar2) input, $"rejects {desc}")

    module Grammar3 =
        [<Fact>]
        let ``accepts valid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.acceptInputsG3 do
                Assert.True(accepts (TestHelpers.grammarToRsm grammar3) input, $"accepts {desc}")

        [<Fact>]
        let ``rejects invalid strings`` () =
            for input, desc in SharedParsingTests.GrammarAcceptanceCases.rejectInputsG3 do
                Assert.False(accepts (TestHelpers.grammarToRsm grammar3) input, $"rejects {desc}")

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
    let private grammar = dyck1.Grammars[0].Grammar

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
    let private grammar = dyck1.Grammars[1].Grammar

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches inputs`` () =
        for input in SharedParsingTests.Grammar159Cases.grammar2Inputs do
            let desc = String.concat " " input
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) input, $"tree yield: {desc}")

module GllGrammar159D =
    let private rsm = dyck1.Grammars[3].Rsm

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = dyck1.AcceptStrings[6]
        Assert.True(accepts rsm input)

module GllPropertyTreeYield =
    let private propertyRunner =
        SharedParsingTests.Runners.runPropertyTreeYieldTest accepts

    [<Property>]
    let ``S -> a S b S | eps tree yield (Dyck1 grammar1)`` (s: string) =
        propertyRunner dyck1.Grammars[0].Grammar "S -> a S b S | eps" s

    [<Property>]
    let ``S -> S S | a S b | eps tree yield (Dyck1 grammar2)`` (s: string) =
        propertyRunner dyck1.Grammars[1].Grammar "S -> S S | a S b | eps" s

    [<Property>]
    let ``S -> a S | a tree yield (APlus grammar3)`` (s: string) =
        propertyRunner aplus.Grammars[0].Grammar "S -> a S | a" s

    [<Property>]
    let ``S -> S a | a tree yield (APlus grammar4)`` (s: string) =
        propertyRunner aplus.Grammars[1].Grammar "S -> S a | a" s

    [<Property>]
    let ``S -> N a* tree yield (APlus grammar11)`` (s: string) =
        propertyRunner aplus.Grammars[3].Grammar "S -> N a*" s

    [<Property>]
    let ``S -> a* N tree yield (APlus grammar12)`` (s: string) =
        propertyRunner aplus.Grammars[4].Grammar "S -> a* N" s

    [<Property>]
    let ``S -> N* tree yield (AStar grammar13)`` (s: string) =
        propertyRunner TestGrammars.grammar13 "S -> N*" s

    [<Property>]
    let ``S -> a | S S | S S S tree yield (APlus grammar14)`` (s: string) =
        propertyRunner aplus.Grammars[5].Grammar "S -> a | S S | S S S" s

module GllAdditionalAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        for s in singleA.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarS2a) s)

    [<Fact>]
    let ``S -> a rejects all reject strings`` () =
        for s in singleA.RejectStrings do
            Assert.False(accepts (TestHelpers.grammarToRsm TestGrammars.grammarS2a) s)

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        for s in singleAB.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarAB) s)

    [<Fact>]
    let ``S -> a S | b accepts accept strings`` () =
        for s in anb.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aS_b) s)

    [<Fact>]
    let ``S -> a S | b rejects reject strings`` () =
        for s in anb.RejectStrings do
            Assert.False(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aS_b) s)

    [<Fact>]
    let ``S -> a S b | eps accepts accept strings`` () =
        for s in anbn.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aSb_eps) s)

    [<Fact>]
    let ``S -> a S b | eps rejects reject strings`` () =
        for s in anbn.RejectStrings do
            Assert.False(accepts (TestHelpers.grammarToRsm TestGrammars.grammar_aSb_eps) s)

module GllRightNullable =
    let private g = TestGrammars.grammarRightNullable
    let private lang = LanguageRegistry.AStarBStar

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps accepts accept strings`` () =
        for s in lang.AcceptStrings do
            Assert.True(accepts (TestHelpers.grammarToRsm g) s)

    [<Fact>]
    let ``S -> A B, A -> a A | eps, B -> b B | eps rejects reject strings`` () =
        for s in lang.RejectStrings do
            Assert.False(accepts (TestHelpers.grammarToRsm g) s)

module GllReductionCascade =
    [<Fact>]
    let ``Epsilon reductions cascade at layer 0`` () =
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarCascade) [])

module GllEpsilonGrammars =
    [<Fact>]
    let ``all epsilon grammars accept empty and reject non-empty`` () =
        SharedParsingTests.Runners.runEpsilonTests accepts (fun g input -> not (accepts g input))

module GllGrammar159B_SaSb =
    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = dyck1.AcceptStrings[6]
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarSaSb_eps) input)

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = dyck1.AcceptStrings[5]
        Assert.True(accepts (TestHelpers.grammarToRsm TestGrammars.grammarSaSb_eps) input)
