namespace FLPQ.Cli

open System
open System.IO
open System.Text.RegularExpressions
open FLPQ.Languages
open FLPQ.Printers

module Helpers =

    let readFile path =
        if not (File.Exists path) then
            failwithf "File not found: %s" path

        File.ReadAllText(path).Trim()

    let writeOutputFile (path: string) (content: string) =
        let dir = Path.GetDirectoryName path

        if not (Directory.Exists dir) then
            Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, content)

    let cleanOutputDir (dir: string) =
        if Directory.Exists dir then
            if Directory.GetFileSystemEntries(dir).Length > 0 then
                Directory.Delete(dir, true)
                Directory.CreateDirectory dir |> ignore
        else
            Directory.CreateDirectory dir |> ignore

    let writeStepsVisualization (outputDir: string) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].TreeAndStack
            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].Input

    let writeGllStepsVisualization (outputDir: string) (steps: GllStepVisualizer.GllVisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "queue.tex")) steps.[idx].Queue
            writeOutputFile (Path.Combine(stepDir, "descriptors_table.tex")) steps.[idx].DescriptorsTable
            writeOutputFile (Path.Combine(stepDir, "new_descriptors.tex")) steps.[idx].NewDescriptors
            writeOutputFile (Path.Combine(stepDir, "gss.dot")) steps.[idx].GssDot
            writeOutputFile (Path.Combine(stepDir, "path_index.tex")) steps.[idx].PathIndex
            writeOutputFile (Path.Combine(stepDir, "input.dot")) steps.[idx].Input
            writeOutputFile (Path.Combine(stepDir, "rsm.dot")) steps.[idx].RsmDot

    let writeRnglrStepsVisualization (outputDir: string) (steps: RnglrStepVisualizer.RnglrVisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "descriptors_table.tex")) steps.[idx].DescriptorsTable
            writeOutputFile (Path.Combine(stepDir, "new_descriptors.tex")) steps.[idx].NewDescriptors
            writeOutputFile (Path.Combine(stepDir, "gss.dot")) steps.[idx].GssDot
            writeOutputFile (Path.Combine(stepDir, "path_index.tex")) steps.[idx].PathIndex
            writeOutputFile (Path.Combine(stepDir, "input.dot")) steps.[idx].Input
            writeOutputFile (Path.Combine(stepDir, "lr_table.tex")) steps.[idx].LrTable

    let naturalSortKey (dirName: string) : int =
        let m = Regex.Match(dirName, "step_(\d+)")

        if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

    let findSummaryTemplate () : string =
        let candidates =
            [ Path.Combine("data", "tex_summary_template.tex")
              Path.Combine(System.AppContext.BaseDirectory, "tex_summary_template.tex")
              Path.Combine(
                  System.AppContext.BaseDirectory,
                  "..",
                  "..",
                  "..",
                  "..",
                  "..",
                  "data",
                  "tex_summary_template.tex"
              ) ]

        match candidates |> List.tryFind File.Exists with
        | Some p -> p
        | None -> failwithf "Could not locate tex_summary_template.tex. Tried: %A" candidates

    let findTikzTemplate () : string =
        let candidates =
            [ Path.Combine("data", "tex_tikz_template.tex")
              Path.Combine(System.AppContext.BaseDirectory, "tex_tikz_template.tex")
              Path.Combine(
                  System.AppContext.BaseDirectory,
                  "..",
                  "..",
                  "..",
                  "..",
                  "..",
                  "data",
                  "tex_tikz_template.tex"
              ) ]

        match candidates |> List.tryFind File.Exists with
        | Some p -> p
        | None -> failwithf "Could not locate tex_tikz_template.tex. Tried: %A" candidates

    let findGllStepTemplate () : string =
        let candidates =
            [ Path.Combine("data", "GLL_step_template.tex")
              Path.Combine(System.AppContext.BaseDirectory, "GLL_step_template.tex")
              Path.Combine(
                  System.AppContext.BaseDirectory,
                  "..",
                  "..",
                  "..",
                  "..",
                  "..",
                  "data",
                  "GLL_step_template.tex"
              ) ]

        match candidates |> List.tryFind File.Exists with
        | Some p -> p
        | None -> failwithf "Could not locate GLL_step_template.tex. Tried: %A" candidates

    let findRnglrStepTemplate () : string =
        let candidates =
            [ Path.Combine("data", "RNGLR_step_template.tex")
              Path.Combine(System.AppContext.BaseDirectory, "RNGLR_step_template.tex")
              Path.Combine(
                  System.AppContext.BaseDirectory,
                  "..",
                  "..",
                  "..",
                  "..",
                  "..",
                  "data",
                  "RNGLR_step_template.tex"
              ) ]

        match candidates |> List.tryFind File.Exists with
        | Some p -> p
        | None -> failwithf "Could not locate RNGLR_step_template.tex. Tried: %A" candidates
