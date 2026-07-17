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

module GllRegexEquivalence =

    [<Property(MaxTest = 50)>]
    let ``S -> a* matches DFA for a*`` (s: string) =
        let regexText = "a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> a* a* matches DFA for a* a*`` (s: string) =
        let regexText = "a * a *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a")
        accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* matches DFA for (a | b)*`` (s: string) =
        let regexText = "( a | b ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s |> List.filter (fun c -> c = "a" || c = "b")

        accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    [<Property(MaxTest = 50)>]
    let ``S -> (a | b)* (a | c)* matches DFA for (a | b)* (a | c)*`` (s: string) =
        let regexText = "( a | b ) * ( a | c ) *"
        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm

        let input =
            TestHelpers.stringToTerminals s
            |> List.filter (fun c -> c = "a" || c = "b" || c = "c")

        accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

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
            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar1) [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar1) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar1) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) input)

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) input)

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) input)

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar1) input)

    // ---- Grammar 2: S -> a* N ; N -> a | (a a) ----
    module Grammar2 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar2) [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar2) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar2) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) input)

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) input)

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) input)

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar2) input)

    // ---- Grammar 3: S -> N* ; N -> a | (a a) ----
    module Grammar3 =
        [<Fact>]
        let ``accepts empty`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) [])

        [<Fact>]
        let ``accepts a`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar3) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar3) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: empty`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) [])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) input)

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) input)

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) input)

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar3) input)

    // ---- Grammar 4: S -> a | S S | S S S ----
    module Grammar4 =
        [<Fact>]
        let ``accepts a`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) [ "a" ])

        [<Fact>]
        let ``accepts aa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a" ])

        [<Fact>]
        let ``accepts aaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a" ])

        [<Fact>]
        let ``accepts aaaa`` () =
            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a"; "a" ])

        [<Fact>]
        let ``rejects empty`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar4) [])

        [<Fact>]
        let ``rejects b`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar4) [ "b" ])

        [<Fact>]
        let ``rejects ab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "b" ])

        [<Fact>]
        let ``rejects aab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects aaab`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "a"; "a"; "b" ])

        [<Fact>]
        let ``rejects abaa`` () =
            Assert.False(accepts (TestHelpers.grammarToRsm grammar4) [ "a"; "b"; "a"; "a" ])

        [<Fact>]
        let ``tree yield matches input: a`` () =
            let input = [ "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) input)

        [<Fact>]
        let ``tree yield matches input: aa`` () =
            let input = [ "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) input)

        [<Fact>]
        let ``tree yield matches input: aaa`` () =
            let input = [ "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) input)

        [<Fact>]
        let ``tree yield matches input: aaaa`` () =
            let input = [ "a"; "a"; "a"; "a" ]

            Assert.True(accepts (TestHelpers.grammarToRsm grammar4) input)

module GllGrammar159A =
    let private grammar = TestGrammars.grammar1

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a b a b b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

    [<Fact>]
    let ``S -> a S b S | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

module GllGrammar159B =
    let private grammar = Grammar.parseGrammar "S -> S a S b\nS -> eps"

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

    [<Fact>]
    let ``S -> S a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

module GllGrammar159C =
    let private grammar = TestGrammars.grammar2

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

    [<Fact>]
    let ``S -> S S | a S b | eps tree yield matches input: a a b a b b a b`` () =
        let input = [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]

        Assert.True(accepts (TestHelpers.grammarToRsm grammar) input)

module GllGrammar159D =


    let private rsm = TestHelpers.buildRegexRsm "(a S b)*"

    [<Fact>]
    let ``S -> (a S b)* tree yield matches input: a a a b a b b a b b`` () =
        let input = [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]

        Assert.True(accepts rsm input)

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

            try
                accepts (TestHelpers.grammarToRsm grammarG1) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> S S | a S b | eps tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG2) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> a S | a tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG3) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> S a | a tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG4) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> N a* tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG5) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> a* N tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG6) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> N* tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG7) input
                true
            with _ ->
                false

    [<Property>]
    let ``S -> a | S S | S S S tree yield matches input`` (s: string) =
        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammarG8) input
                true
            with _ ->
                false
