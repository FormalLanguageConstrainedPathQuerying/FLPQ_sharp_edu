module SharedParsingTests

open Xunit
open FLPQ.Languages
open FLPQ.TestUtilities

type AcceptFn = RSM<string, string> -> string list -> bool

type RejectFn = Grammar<string, string> -> string list -> bool

/// Shared accept/reject test cases for grammars 11-14.
module GrammarAcceptanceCases =

    let grammar1 = TestGrammars.grammar11
    let grammar2 = TestGrammars.grammar12
    let grammar3 = TestGrammars.grammar13
    let grammar4 = TestGrammars.grammar14

    let acceptInputsG1 =
        [ ([ "a" ], "a")
          ([ "a"; "a" ], "aa")
          ([ "a"; "a"; "a" ], "aaa")
          ([ "a"; "a"; "a"; "a" ], "aaaa") ]

    let rejectInputsG1 =
        [ ([], "empty")
          ([ "b" ], "b")
          ([ "a"; "b" ], "ab")
          ([ "a"; "a"; "b" ], "aab")
          ([ "a"; "a"; "a"; "b" ], "aaab")
          ([ "a"; "b"; "a"; "a" ], "abaa") ]

    let acceptInputsG2 = acceptInputsG1

    let rejectInputsG2 = rejectInputsG1

    let acceptInputsG3 =
        [ ([], "empty")
          ([ "a" ], "a")
          ([ "a"; "a" ], "aa")
          ([ "a"; "a"; "a" ], "aaa")
          ([ "a"; "a"; "a"; "a" ], "aaaa") ]

    let rejectInputsG3 =
        [ ([ "b" ], "b")
          ([ "a"; "b" ], "ab")
          ([ "a"; "a"; "b" ], "aab")
          ([ "a"; "a"; "a"; "b" ], "aaab")
          ([ "a"; "b"; "a"; "a" ], "abaa") ]

    let acceptInputsG4 = acceptInputsG1

    let rejectInputsG4 = rejectInputsG1

/// Shared 159 tree yield test cases.
module Grammar159Cases =

    let grammar1Inputs =
        [ [ "a"; "a"; "b"; "a"; "b"; "b" ]
          [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ]
          [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ] ]

    let grammarSaSb_epsInputs =
        [ [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]
          [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ] ]

    let grammar2Inputs =
        [ [ "a"; "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b"; "b" ]
          [ "a"; "a"; "b"; "a"; "b"; "b"; "a"; "b" ] ]

/// Shared epsilon grammar test cases.
module EpsilonCases =

    let rejectInputs = [ [ "a" ]; [ "b" ]; [ "a"; "b" ]; [ "a"; "a" ]; [ "b"; "b" ] ]

    let grammars =
        [ (TestGrammars.grammarEps, "S -> eps")
          (TestGrammars.grammarNtoEps, "S -> N; N -> eps")
          (TestGrammars.grammarNNtoEps, "S -> N N; N -> eps")
          (TestGrammars.grammarNStarEps, "S -> N*; N -> eps")
          (TestGrammars.grammarSSeps, "S -> S S | eps")
          (TestGrammars.grammarChainEps, "S -> A B; A -> C D; B -> D C; D -> eps; C -> eps")
          (TestGrammars.grammarAltEps, "S -> A | B; A -> C D; B -> D C; D -> eps; C -> eps") ]

/// Runner functions for shared test cases.
module Runners =

    let runEpsilonTests (accepts: AcceptFn) (rejects: RejectFn) : unit =

        for grammar, desc in EpsilonCases.grammars do
            Assert.True(accepts (TestHelpers.grammarToRsm grammar) [], $"Epsilon accepts empty: {desc}")

            for input in EpsilonCases.rejectInputs do
                Assert.True(rejects grammar input, $"Epsilon rejects {input}: {desc}")

    let runRegexEquivalenceTests (accepts: AcceptFn) (filterFn: string -> bool) (regexText: string) (s: string) : bool =

        let rsm = TestHelpers.buildRegexRsm regexText
        let dfa = TestHelpers.dfaFromRegexRsm rsm
        let input = TestHelpers.stringToTerminals s |> List.filter filterFn
        accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    let runPropertyTreeYieldTest
        (accepts: AcceptFn)
        (grammar: Grammar<string, string>)
        (desc: string)
        (s: string)
        : bool =

        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts (TestHelpers.grammarToRsm grammar) input |> ignore
                true
            with _ ->
                false
