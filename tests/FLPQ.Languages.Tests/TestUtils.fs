module TestUtils

open System
open System.IO

type DotInfo =
    { nodeCount: int
      edgeCount: int
      nodeLabels: string list
      edgeLabels: string list }

let private tokenizePlainLine (line: string) : string list =
    let mutable tokens = []
    let mutable i = 0

    while i < line.Length do
        if line.[i] = '"' then
            let mutable j = i + 1

            while j < line.Length && line.[j] <> '"' do
                if line.[j] = '\\' && j + 1 < line.Length && line.[j + 1] = '"' then
                    j <- j + 2
                else
                    j <- j + 1

            let endIdx = if j < line.Length then j + 1 else line.Length
            let token = line.Substring(i, endIdx - i)
            tokens <- token :: tokens
            i <- endIdx + 1
        elif line.[i] = ' ' || line.[i] = '\t' then
            i <- i + 1
        else
            let mutable j = i

            while j < line.Length && line.[j] <> ' ' && line.[j] <> '\t' do
                j <- j + 1

            let token = line.Substring(i, j - i)
            tokens <- token :: tokens
            i <- j

    List.rev tokens

let checkDotCompiles (dot: string) : bool =
    let tempFile = Path.GetTempFileName()
    File.WriteAllText(tempFile, dot)

    try
        let processInfo = new Diagnostics.Process()
        processInfo.StartInfo.FileName <- "dot"
        processInfo.StartInfo.Arguments <- "-Tplain " + tempFile
        processInfo.StartInfo.RedirectStandardOutput <- true
        processInfo.StartInfo.RedirectStandardError <- true
        processInfo.StartInfo.UseShellExecute <- false
        processInfo.Start() |> ignore
        processInfo.WaitForExit(5000) |> ignore
        processInfo.ExitCode = 0
    finally
        File.Delete(tempFile)

let checkDotCompilesWithInfo (dot: string) : DotInfo =
    let tempFile = Path.GetTempFileName()
    File.WriteAllText(tempFile, dot)

    try
        let processInfo = new Diagnostics.Process()
        processInfo.StartInfo.FileName <- "dot"
        processInfo.StartInfo.Arguments <- "-Tplain " + tempFile
        processInfo.StartInfo.RedirectStandardOutput <- true
        processInfo.StartInfo.RedirectStandardError <- true
        processInfo.StartInfo.UseShellExecute <- false
        processInfo.Start() |> ignore
        let output = processInfo.StandardOutput.ReadToEnd()
        processInfo.WaitForExit(5000) |> ignore

        if processInfo.ExitCode <> 0 then
            failwithf "dot compilation failed: %s" (processInfo.StandardError.ReadToEnd())

        let mutable nodeCount = 0
        let mutable edgeCount = 0
        let mutable nodeLabels = []
        let mutable edgeLabels = []

        for line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries) do
            let tokens = tokenizePlainLine line

            match tokens with
            | "node" :: _name :: _x :: _y :: _w :: _h :: label :: _style :: _shape :: _color :: [] ->
                nodeCount <- nodeCount + 1
                nodeLabels <- label :: nodeLabels
            | "node" :: _name :: _x :: _y :: _w :: _h :: label :: _style :: _shape :: _color :: _fillcolor :: [] ->
                nodeCount <- nodeCount + 1
                nodeLabels <- label :: nodeLabels
            | "node" :: _name :: _x :: _y :: _w :: _h :: label :: _style :: _shape :: _color :: _fillcolor :: _url :: [] ->
                nodeCount <- nodeCount + 1
                nodeLabels <- label :: nodeLabels
            | "edge" :: _tail :: _head :: n :: rest ->
                let numPts = Int32.Parse n

                if rest.Length >= numPts * 2 + 2 then
                    let label = rest.[numPts * 2]
                    edgeCount <- edgeCount + 1
                    edgeLabels <- label :: edgeLabels
                elif rest.Length >= numPts * 2 then
                    edgeCount <- edgeCount + 1
                else
                    edgeCount <- edgeCount + 1
            | _ -> ()

        { nodeCount = nodeCount
          edgeCount = edgeCount
          nodeLabels = List.rev nodeLabels
          edgeLabels = List.rev edgeLabels }
    finally
        File.Delete(tempFile)

let checkTexCompiles (templatePath: string) (tex: string) : bool =
    let template = File.ReadAllText templatePath
    let fullDoc = template.Replace("__CONTENT__", tex)
    let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory tempDir |> ignore
    let texFile = Path.Combine(tempDir, "test.tex")

    try
        File.WriteAllText(texFile, fullDoc)

        let processInfo = new Diagnostics.Process()
        processInfo.StartInfo.FileName <- "pdflatex"

        processInfo.StartInfo.Arguments <-
            sprintf "-interaction=nonstopmode -output-directory=\"%s\" \"%s\"" tempDir texFile

        processInfo.StartInfo.RedirectStandardOutput <- true
        processInfo.StartInfo.RedirectStandardError <- true
        processInfo.StartInfo.UseShellExecute <- false
        processInfo.StartInfo.WorkingDirectory <- tempDir
        processInfo.Start() |> ignore
        processInfo.WaitForExit(30000) |> ignore
        processInfo.ExitCode = 0
    finally
        try
            Directory.Delete(tempDir, true)
        with _ ->
            ()
