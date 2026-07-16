module GllTests

open Xunit
open FsCheck
open FsCheck.Xunit
open FSharpPlus.Data
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.GraphAnalysis
open FLPQ.TestUtilities

module GllAcceptance =
    [<Fact>]
    let ``S -> a accepts a`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a" ])

    [<Fact>]
    let ``S -> a rejects eps`` () =
        let g = Grammar.parseGrammar "S -> a"
        Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``S -> a b accepts a b`` () =
        let g = Grammar.parseGrammar "S -> a b"
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b accepts a a b`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S | b rejects a a a`` () =
        let g = Grammar.parseGrammar "S -> a S\nS -> b"
        Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "a" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a b a b`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "b"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b S | eps accepts a a b b`` () =
        let g = TestGrammars.grammar1
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts a a b b`` () =
        let g = TestGrammars.grammar2
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps rejects a a b`` () =
        let g = TestGrammars.grammar2
        Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | S S | eps accepts a b (no infinite loop)`` () =
        let g = TestGrammars.grammar2
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "b" ])

    [<Fact>]
    let ``S -> a S b | eps accepts empty`` () =
        let g = TestGrammars.grammar2
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``Left-recursive S -> a S | a accepts a a a`` () =
        let g = TestGrammars.grammar3
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "a" ])

    [<Fact>]
    let ``Left-recursive S -> a S | a rejects empty`` () =
        let g = TestGrammars.grammar3
        Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm g) [])

    [<Fact>]
    let ``Right-recursive S -> S a | a accepts a a a`` () =
        let g = TestGrammars.grammar4
        Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm g) [ "a"; "a"; "a" ])

module GllCykEquivalence =
    [<Property>]
    let ``GLL and CYK agree on grammar1 random string inputs`` (s: string) =
        let g = TestGrammars.grammar1
        let input = TestHelpers.stringToTerminals s
        TestHelpers.accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar2 random string inputs`` (s: string) =
        let g = TestGrammars.grammar2
        let input = TestHelpers.stringToTerminals s
        TestHelpers.accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar3 random string inputs`` (s: string) =
        let g = TestGrammars.grammar3
        let input = TestHelpers.stringToTerminals s
        TestHelpers.accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

    [<Property>]
    let ``GLL and CYK agree on grammar4 random string inputs`` (s: string) =
        let g = TestGrammars.grammar4
        let input = TestHelpers.stringToTerminals s
        TestHelpers.accepts (TestHelpers.grammarToRsm g) input = TestHelpers.cykAccepts g input

module GllTreeExtraction =
    [<Fact>]
    let ``Tree extraction for S->aSbS|eps on abab produces tree with correct yield`` () =
        let g = TestGrammars.grammar1

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm g) [ "a"; "b"; "a"; "b" ] with
        | Some tree ->
            let leaves = DerivationTree.leaves tree
            Assert.Equal<string list>([ "a"; "b"; "a"; "b" ], leaves)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``Tree extraction for S->aSb|eps on aabb produces tree with correct yield`` () =
        let g = TestGrammars.grammar2

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm g) [ "a"; "a"; "b"; "b" ] with
        | Some tree ->
            let leaves = DerivationTree.leaves tree
            Assert.Equal<string list>([ "a"; "a"; "b"; "b" ], leaves)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``Tree extraction for S->a|b on a produces tree with leaf`` () =
        let g = Grammar.parseGrammar "S -> a\nS -> b"

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm g) [ "a" ] with
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
        TestHelpers.accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> a* a* matches DFA for a* a*`` (s: string) =
        let regexText = "a * a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        TestHelpers.accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
        let regexText = "( a | b ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a" || c = "b")

        TestHelpers.accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
        let regexText = "( a | b ) * ( a | c ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s
            |> List.filter (fun c -> c = "a" || c = "b" || c = "c")

        TestHelpers.accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

module GllGrammarAcceptanceAndTree =

    /// Grammar 1: S -> N a* ; N -> (a a) | a
    /// CFG with inlined terminal-first start.
    let private grammar1 = TestGrammars.grammar11

    /// Grammar 2: S -> a* N ; N -> a | (a a)
    /// CFG with inlined terminal-first start.
    let private grammar2 = TestGrammars.grammar12

    /// Grammar 3: S -> N* ; N -> a | (a a)
    /// CFG with inlined terminal-first start.
    let private grammar3 = TestGrammars.grammar13

    /// Grammar 4: S -> a | S S | S S S
    let private grammar4 = TestGrammars.grammar14

    // ---- Grammar 1: S -> N a* ; N -> (a a) | a ----
    module Grammar1 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar1) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar1) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar1) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar1) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 2: S -> a* N ; N -> a | (a a) ----
    module Grammar2 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar2) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar2) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar2) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar2) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 3: S -> N* ; N -> a | (a a) ----
    module Grammar3 =
        [<Fact>]
        let ``accepts empty`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [])

        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: empty`` () =
            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar3) [] with
            | Some tree -> Assert.Equal<string list>([], DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar3) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar3) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar3) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar3) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

    // ---- Grammar 4: S -> a | S S | S S S ----
    module Grammar4 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(TestHelpers.accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar4) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar4) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar4) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar4) input with
            | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
            | None -> Assert.True(false, "Should produce a tree")

module GllGrammar159A =
    let private grammar = TestGrammars.grammar1

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a b a b b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module GllGrammar159B =
    let private grammar = Grammar.parseGrammar "S -> S a S b\nS -> eps"

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module GllGrammar159C =
    let private grammar = TestGrammars.grammar2

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammar) input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module GllGrammar159D =


    let private rsm = TestHelpers.buildRegexRsm "(a S b)*"

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        match TestHelpers.gllAcceptsAndCheckTree rsm input with
        | Some tree -> Assert.Equal<string list>(input, DerivationTree.leaves tree)
        | None -> Assert.True(false, "Should produce a tree")

module GllPropertyTreeYield =
    let private grammarG1 = TestGrammars.grammar1
    let private grammarG2 = TestGrammars.grammar2
    let private grammarG3 = TestGrammars.grammar3
    let private grammarG4 = TestGrammars.grammar4

    let private grammarG5 = TestGrammars.grammar11

    let private grammarG6 = TestGrammars.grammar12

    let private grammarG7 = TestGrammars.grammar13

    let private grammarG8 = TestGrammars.grammar14

    [<Property>]
    let ``S -> a S b S | eps tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG1) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> S S | a S b | eps tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG2) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> a S | a tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG3) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> S a | a tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG4) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> N a* tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG5) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> a* N tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG6) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> N* tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG7) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true

    [<Property>]
    let ``S -> a | S S | S S S tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            match TestHelpers.gllAcceptsAndCheckTree (TestHelpers.grammarToRsm grammarG8) input with
            | Some tree -> DerivationTree.leaves tree = input
            | None -> true
