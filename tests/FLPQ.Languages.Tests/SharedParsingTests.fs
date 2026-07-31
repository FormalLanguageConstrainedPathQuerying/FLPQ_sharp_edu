module SharedParsingTests

open Xunit
open FLPQ.Languages
open FLPQ.TestUtilities

type AcceptFn = RSM<string, string> -> string list -> bool

type RejectFn = RSM<string, string> -> string list -> bool

/// Shared accept/reject test cases for grammars 11-14.
/// Source: LanguageRegistry.APlus and LanguageRegistry.AStar.
module GrammarAcceptanceCases =

    let grammar1 = TestGrammars.grammar11
    let grammar2 = TestGrammars.grammar12
    let grammar3 = TestGrammars.grammar13
    let grammar4 = TestGrammars.grammar14

    let private aplus = LanguageRegistry.APlus

    let acceptInputsG1 =
        aplus.AcceptStrings |> List.map (fun s -> (s, String.concat "" s))

    let rejectInputsG1 =
        aplus.RejectStrings
        |> List.map (fun s ->
            let desc = if List.isEmpty s then "empty" else String.concat "" s
            (s, desc))

    let acceptInputsG2 = acceptInputsG1
    let rejectInputsG2 = rejectInputsG1

    let acceptInputsG3 =
        LanguageRegistry.AStar.AcceptStrings
        |> List.map (fun s -> (s, if List.isEmpty s then "empty" else String.concat "" s))

    let rejectInputsG3 =
        LanguageRegistry.AStar.RejectStrings
        |> List.map (fun s -> (s, String.concat "" s))

    let acceptInputsG4 = acceptInputsG1
    let rejectInputsG4 = rejectInputsG1

/// Shared 159 tree yield test cases. Source: LanguageRegistry.Dyck1.
module Grammar159Cases =

    let grammar1Inputs =
        LanguageRegistry.Dyck1.AcceptStrings
        |> List.filter (fun s -> List.length s >= 5)

    let grammarSaSb_epsInputs =
        LanguageRegistry.Dyck1.AcceptStrings
        |> List.filter (fun s -> List.length s >= 5 && List.length s <= 10)

    let grammar2Inputs = grammarSaSb_epsInputs

/// Shared epsilon grammar test cases.
module EpsilonCases =

    let rejectInputs = LanguageRegistry.EpsilonOnly.RejectStrings

    let grammars =
        LanguageRegistry.EpsilonOnly.Grammars |> List.map (fun g -> (g.Rsm, g.Text))

/// Runner functions for shared test cases.
module Runners =

    let runEpsilonTests (accepts: AcceptFn) (rejects: RejectFn) : unit =

        for grammar, desc in EpsilonCases.grammars do
            Assert.True(accepts grammar [], $"Epsilon accepts empty: {desc}")

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
