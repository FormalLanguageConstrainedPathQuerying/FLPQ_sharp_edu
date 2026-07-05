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

            writeOutputFile (Path.Combine(stepDir, "tree_and_stack.dot")) steps.[idx].treeAndStack
            writeOutputFile (Path.Combine(stepDir, "input.tex")) steps.[idx].input

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
