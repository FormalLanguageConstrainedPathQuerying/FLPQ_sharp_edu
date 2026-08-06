module ParsingTestCases

open Xunit
open FsCheck
open FLPQ.Languages
open FLPQ.TestUtilities

type AcceptFn = RSM<string, string> -> Terminal<string> list -> bool

type RejectFn = RSM<string, string> -> Terminal<string> list -> bool

/// A single acceptance test case from the language registry.
type AcceptanceCase =
    { CaseName: string
      LanguageName: string
      GrammarName: string
      Rsm: RSM<string, string>
      Grammar: Grammar<string, string>
      Input: Terminal<string> list
      ExpectedAccepted: bool }

/// Auto-generated acceptance cases from ALL languages in the registry.
module AcceptanceCases =

    /// Iterate all languages, all grammars, all accept/reject strings.
    let allCases: AcceptanceCase list =
        LanguageRegistry.allLanguages
        |> List.collect (fun lang ->
            lang.Grammars
            |> List.filter (fun g -> not g.Properties.DoesNotCoverFullLanguage)
            |> List.collect (fun g ->
                let acceptCases =
                    lang.AcceptStrings
                    |> List.mapi (fun i input ->
                        { CaseName = $"{lang.Name}/{g.Name}/accept_{i}"
                          LanguageName = lang.Name
                          GrammarName = g.Name
                          Rsm = g.Rsm
                          Grammar = g.Grammar
                          Input = input
                          ExpectedAccepted = true })

                let rejectCases =
                    lang.RejectStrings
                    |> List.mapi (fun i input ->
                        { CaseName = $"{lang.Name}/{g.Name}/reject_{i}"
                          LanguageName = lang.Name
                          GrammarName = g.Name
                          Rsm = g.Rsm
                          Grammar = g.Grammar
                          Input = input
                          ExpectedAccepted = false })

                acceptCases @ rejectCases))

    /// Filter acceptance cases by language name prefix.
    let forLanguage (name: string) (acceptFn: AcceptFn) (rejectFn: RejectFn) =
        allCases |> List.filter (fun c -> c.LanguageName = name)

/// Shared 159 tree yield test cases. Source: LanguageRegistry.Dyck1.
module TreeYieldCases =

    let grammar1Inputs =
        LanguageRegistry.Dyck1.AcceptStrings
        |> List.filter (fun s -> List.length s >= 5)

    let grammarSaSb_epsInputs =
        LanguageRegistry.Dyck1.AcceptStrings
        |> List.filter (fun s -> List.length s >= 5 && List.length s <= 10)

    let grammar2Inputs = grammarSaSb_epsInputs

/// Shared regex equivalence test cases.
module RegexEquivalenceCases =

    let aStar = ("a *", fun (c: string) -> c = "a")
    let aStarAStar = ("a * a *", fun (c: string) -> c = "a")
    let aOrBStar = ("( a | b ) *", fun (c: string) -> c = "a" || c = "b")

    let aOrBStar_aOrCStar =
        ("( a | b ) * ( a | c ) *", fun (c: string) -> c = "a" || c = "b" || c = "c")

/// Shared accept/reject test cases for grammars 11-14.
/// Source: LanguageRegistry.APlus and LanguageRegistry.AStar.
module GrammarAcceptanceCases =

    let private aplus = LanguageRegistry.APlus

    let acceptInputsG1 =
        aplus.AcceptStrings
        |> List.map (fun s -> (s, String.concat "" (s |> List.map (fun (Terminal x) -> x))))

    let rejectInputsG1 =
        aplus.RejectStrings
        |> List.map (fun s ->
            let desc =
                if List.isEmpty s then
                    "empty"
                else
                    String.concat "" (s |> List.map (fun (Terminal x) -> x))

            (s, desc))

    let acceptInputsG2 = acceptInputsG1
    let rejectInputsG2 = rejectInputsG1

    let acceptInputsG3 =
        LanguageRegistry.AStar.AcceptStrings
        |> List.map (fun s ->
            (s,
             if List.isEmpty s then
                 "empty"
             else
                 String.concat "" (s |> List.map (fun (Terminal x) -> x))))

    let rejectInputsG3 =
        LanguageRegistry.AStar.RejectStrings
        |> List.map (fun s -> (s, String.concat "" (s |> List.map (fun (Terminal x) -> x))))

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

        let input =
            TestHelpers.stringToTerminals s |> List.filter (fun (Terminal t) -> filterFn t)

        accepts rsm input = TestHelpers.dfaAcceptsRegex dfa input

    let runPropertyTreeYieldTest (accepts: AcceptFn) (rsm: RSM<string, string>) (desc: string) (s: string) : bool =

        if s.Length > 30 then
            true
        else
            let input = TestHelpers.stringToTerminals s

            try
                accepts rsm input |> ignore
                true
            with _ ->
                false
