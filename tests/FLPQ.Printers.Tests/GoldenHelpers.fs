module GoldenHelpers

open System.IO
open FLPQ.Languages
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
    |> List.mapi (fun i step -> sprintf "--- Step %d ---\n%s" i step.treeAndStack)
    |> String.concat "\n\n"
