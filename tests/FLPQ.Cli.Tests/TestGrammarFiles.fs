namespace FLPQ.Cli.Tests

open System.IO
open FLPQ.TestUtilities

module TestGrammarFiles =

    let private writeTempFile (text: string) : string =
        let path = Path.GetTempFileName() + ".bnf"
        File.WriteAllText(path, text)
        path

    let exampleGrammar () =
        writeTempFile LanguageRegistry.Dyck1.Grammars.[0].Text

    let exampleGrammarAmb () =
        writeTempFile LanguageRegistry.Dyck1.Grammars.[1].Text

    let exampleGrammarAAA () =
        writeTempFile LanguageRegistry.APlus.Grammars.[2].Text

    let exampleGrammarAnBN () =
        writeTempFile LanguageRegistry.ANBN.Grammars.[0].Text

    let exampleGrammarChain () =
        writeTempFile (LanguageRegistry.findGrammar LanguageRegistry.SingleA "viaIntermediate").Text

    let exampleGrammarSimple () =
        writeTempFile LanguageRegistry.SingleAB.Grammars.[0].Text

    let exampleLRGrammar () =
        writeTempFile LanguageRegistry.ArithExpr.Grammars.[1].Text

    /// The ANBN classic registry grammar text (rules "S -> a S b" and "S -> eps").
    let anbnEbnf = LanguageRegistry.ANBN.Grammars.[0].Text

    /// The 4-token ANBN accept string "a a b b", from the registry.
    let anbnInput =
        LanguageRegistry.ANBN.AcceptStrings
        |> List.find (fun tokens -> List.length tokens = 4)
        |> List.map (fun (FLPQ.Languages.Terminal t) -> t)
        |> String.concat " "
