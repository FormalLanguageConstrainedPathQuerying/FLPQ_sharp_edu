module GoldenHelpers

open System.IO
open FLPQ.Languages
open FLPQ.Printers
open Xunit

let goldenDataDir = Path.Combine(Directory.GetCurrentDirectory(), "GoldenData")

let verifyGolden (goldenFileName: string) (actualContent: string) =
    let goldenPath = Path.Combine(goldenDataDir, goldenFileName)

    if File.Exists goldenPath then
        let expected = File.ReadAllText goldenPath
        Assert.Equal(expected, actualContent)
    else
        Directory.CreateDirectory goldenDataDir |> ignore
        File.WriteAllText(goldenPath, actualContent)

        Assert.True(
            false,
            $"Golden file '{goldenFileName}' was created in output/GoldenData/.\n"
            + "Copy it to tests/FLPQ.Printers.Tests/GoldenData/ and re-run tests."
        )

let combineStepsDot (steps: VisualizationStep list) : string =
    steps
    |> List.mapi (fun i step -> sprintf "--- Step %d ---\n%s" i step.TreeAndStack)
    |> String.concat "\n\n"

let wrapInTemplate (templatePath: string) (content: string) : string =
    let template = File.ReadAllText templatePath
    template.Replace("__CONTENT__", content)
