module TestUtils

open System
open System.IO

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
