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

    let writeStepsVisualization (outputDir: string) (steps: VisualizationStep list) =
        for idx in 0 .. steps.Length - 1 do
            let stepDir = Path.Combine(outputDir, sprintf "step_%d" idx)

            writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].treeAndStack
            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].input

    let readIfExists (path: string) : string option =
        if File.Exists path then
            Some(File.ReadAllText(path).Trim())
        else
            None

    let naturalSortKey (dirName: string) : int =
        let m = Regex.Match(dirName, "step_(\d+)")

        if m.Success then Int32.Parse(m.Groups.[1].Value) else 0

    let collectSteps (vizDir: string) : string[] =
        if not (Directory.Exists vizDir) then
            [||]
        else
            Directory.GetDirectories vizDir
            |> Array.filter (fun d -> Path.GetFileName(d).StartsWith("step_"))
            |> Array.sortBy (fun d -> naturalSortKey (Path.GetFileName d))

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
