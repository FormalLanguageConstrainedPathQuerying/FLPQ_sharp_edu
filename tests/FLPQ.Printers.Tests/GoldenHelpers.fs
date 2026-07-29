module GoldenHelpers

open System
open System.IO
open System.Text.RegularExpressions
open FLPQ.Languages
open FLPQ.Printers
open Xunit

let stripQuotes (s: string) =
    if s.Length >= 2 && s.[0] = '"' && s.[s.Length - 1] = '"' then
        s.Substring(1, s.Length - 2).Replace("\\\"", "\"")
    else
        s.Replace("\\\"", "\"")

let vertexLabelRegex = Regex(@"^\d+: \(\d+,\d+\)$")

let edgeLabelRegex = Regex(@"^\d+,\d+ → \d+,\d+$")

let rnglrEdgeLabelRegex =
    Regex(@"^""?[A-Za-z0-9ε_']+""?(,\s*""?[A-Za-z0-9ε_']+""?)?$")

let goldenDataDir = Path.Combine(Directory.GetCurrentDirectory(), "GoldenData")

let verifyGolden (goldenFileName: string) (actualContent: string) =
    let goldenPath = Path.Combine(goldenDataDir, goldenFileName)

    if File.Exists goldenPath then
        let expected = File.ReadAllText goldenPath
        Assert.Equal(expected, actualContent)
    else
        let createAllowed = Environment.GetEnvironmentVariable("CREATE_GOLDEN_FILES") = "1"

        if createAllowed then
            Directory.CreateDirectory goldenDataDir |> ignore
            File.WriteAllText(goldenPath, actualContent)

            Assert.True(
                false,
                $"Golden file '{goldenFileName}' was created in output/GoldenData/.\n"
                + "Copy it to tests/FLPQ.Printers.Tests/GoldenData/ and re-run tests."
            )
        else
            Assert.True(
                false,
                $"Golden file '{goldenFileName}' not found at '{goldenPath}'.\n"
                + "Set CREATE_GOLDEN_FILES=1 to generate it, or copy the expected file manually."
            )

let combineStepsDot (steps: VisualizationStep list) : string =
    steps
    |> List.mapi (fun i step -> sprintf "--- Step %d ---\n%s" i step.TreeAndStack)
    |> String.concat "\n\n"

let wrapInTemplate (templatePath: string) (content: string) : string =
    let template = File.ReadAllText templatePath
    template.Replace("__CONTENT__", content)
