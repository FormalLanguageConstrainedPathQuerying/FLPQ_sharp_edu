namespace FLPQ.Printers

open System
open System.IO
open System.Diagnostics

/// External tool wrappers for compiling Dot and TeX artifacts.
/// Reused by both the test suite and the CLI summary generator.
module ExternalTools =

    /// Parsed information from a Graphviz `-Tplain` output.
    type DotInfo =
        { NodeCount: int
          EdgeCount: int
          NodeLabels: string list
          EdgeLabels: string list
          NodeFillColors: string list }

    let private tokenizePlainLine (line: string) : string list =
        let mutable tokens = []
        let mutable i = 0

        while i < line.Length do
            if line.[i] = '"' then
                let mutable j = i + 1

                while j < line.Length && line.[j] <> '"' do
                    if j + 1 < line.Length && line.[j] = '\\' && line.[j + 1] = '"' then
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

    /// Run a process synchronously, returning (exitCode, stdout, stderr).
    let private runProcess (fileName: string) (arguments: string) (workingDir: string option) : int * string * string =
        let p = new Process()
        p.StartInfo.FileName <- fileName
        p.StartInfo.Arguments <- arguments
        p.StartInfo.RedirectStandardOutput <- true
        p.StartInfo.RedirectStandardError <- true
        p.StartInfo.UseShellExecute <- false

        match workingDir with
        | Some d -> p.StartInfo.WorkingDirectory <- d
        | None -> ()

        p.Start() |> ignore
        let out = p.StandardOutput.ReadToEnd()
        let err = p.StandardError.ReadToEnd()
        p.WaitForExit(30000) |> ignore
        (p.ExitCode, out, err)

    /// Parse Graphviz `-Tplain` output into structured information.
    /// Throws if `dot` returns a non-zero exit code.
    let compileDotStringToInfo (dot: string) : DotInfo =
        let tempFile = Path.GetTempFileName()
        File.WriteAllText(tempFile, dot)

        try
            let (code, out, err) = runProcess "dot" ("-Tplain " + tempFile) None

            if code <> 0 then
                failwithf "dot compilation failed (exit %d): %s" code err

            let mutable nodeCount = 0
            let mutable edgeCount = 0
            let mutable nodeLabels = []
            let mutable edgeLabels = []
            let mutable nodeFillColors = []

            for line in out.Split('\n', StringSplitOptions.RemoveEmptyEntries) do
                let tokens = tokenizePlainLine line

                match tokens with
                | "node" :: _name :: _x :: _y :: _w :: _h :: label :: _style :: _shape :: _color :: fillcolor :: _ ->
                    nodeCount <- nodeCount + 1
                    nodeLabels <- label :: nodeLabels
                    nodeFillColors <- fillcolor :: nodeFillColors
                | "node" :: _name :: _x :: _y :: _w :: _h :: label :: _ ->
                    nodeCount <- nodeCount + 1
                    nodeLabels <- label :: nodeLabels
                | "edge" :: _tail :: _head :: n :: rest ->
                    let numPts = Int32.Parse n

                    if rest.Length >= numPts * 2 + 2 then
                        let label = rest.[numPts * 2]
                        edgeCount <- edgeCount + 1
                        edgeLabels <- label :: edgeLabels
                    else
                        edgeCount <- edgeCount + 1
                | _ -> ()

            { NodeCount = nodeCount
              EdgeCount = edgeCount
              NodeLabels = List.rev nodeLabels
              EdgeLabels = List.rev edgeLabels
              NodeFillColors = List.rev nodeFillColors }
        finally
            File.Delete(tempFile)

    /// Returns true iff the given Dot source compiles successfully.
    let compileDotString (dot: string) : bool =
        try
            compileDotStringToInfo dot |> ignore
            true
        with _ ->
            false

    /// Compile a Dot file to a PDF file via `dot -Tpdf`.
    /// Returns true on success (exit code 0 and non-empty PDF produced).
    let compileDotFileToPdf (dotPath: string) (pdfPath: string) : bool =
        try
            let dir = Path.GetDirectoryName pdfPath

            if not (Directory.Exists dir) then
                Directory.CreateDirectory dir |> ignore

            let (code, _out, err) =
                runProcess "dot" (sprintf "-Tpdf -o \"%s\" \"%s\"" pdfPath dotPath) None

            if code <> 0 then
                eprintfn "dot failed for %s: %s" dotPath err
                false
            else
                File.Exists pdfPath && FileInfo(pdfPath).Length > 0L
        with ex ->
            eprintfn "dot invocation failed for %s: %s" dotPath ex.Message
            false

    /// Strict check for lualatex errors in the captured stdout.
    /// A run is considered successful only when:
    ///   - exit code is 0
    ///   - no stdout line starts with '!' or contains "Fatal error" / "Error:"
    ///   - the output PDF exists and is non-empty.
    let private latexSucceeded (exitCode: int) (stdout: string) (pdfPath: string) : bool =
        let hasErrors =
            stdout.Split('\n')
            |> Array.exists (fun line ->
                line.StartsWith('!') || line.Contains("Fatal error") || line.Contains("Error:"))

        let pdfOk = File.Exists pdfPath && FileInfo(pdfPath).Length > 0L
        exitCode = 0 && not hasErrors && pdfOk

    /// Compile a TeX string using a template file (template contains `__CONTENT__`).
    /// Uses a temporary directory. Returns true on success.
    let compileTexStringWithTemplate (templatePath: string) (tex: string) : bool =
        let template = File.ReadAllText templatePath
        let fullDoc = template.Replace("__CONTENT__", tex)
        let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        Directory.CreateDirectory tempDir |> ignore
        let texFile = Path.Combine(tempDir, "test.tex")

        try
            File.WriteAllText(texFile, fullDoc)

            let (code, out, _err) =
                runProcess
                    "lualatex"
                    (sprintf "-interaction=nonstopmode -output-directory=\"%s\" \"%s\"" tempDir texFile)
                    (Some tempDir)

            let pdfPath = Path.Combine(tempDir, "test.pdf")
            latexSucceeded code out pdfPath
        finally
            try
                Directory.Delete(tempDir, true)
            with _ ->
                ()

    /// Compile a TeX file to PDF in the given output directory (single pass).
    /// Returns true on success. The PDF is left in `outputDir`.
    let compileTexFile (texPath: string) (outputDir: string) : bool =
        try
            if not (Directory.Exists outputDir) then
                Directory.CreateDirectory outputDir |> ignore

            let (code, out, err) =
                runProcess
                    "lualatex"
                    (sprintf "-interaction=nonstopmode -output-directory=\"%s\" \"%s\"" outputDir texPath)
                    (Some outputDir)

            let pdfName = Path.GetFileNameWithoutExtension(texPath) + ".pdf"
            let pdfPath = Path.Combine(outputDir, pdfName)

            if not (latexSucceeded code out pdfPath) then
                eprintfn "lualatex failed for %s (exit %d)" texPath code

                if not (String.IsNullOrEmpty err) then
                    eprintfn "%s" err

                false
            else
                true
        with ex ->
            eprintfn "lualatex invocation failed for %s: %s" texPath ex.Message
            false

    /// Compile a TeX file twice (for table-of-contents and cross-references).
    /// Returns true only if both passes succeed.
    let compileTexFileTwice (texPath: string) (outputDir: string) : bool =
        let first = compileTexFile texPath outputDir

        if not first then
            false
        else
            compileTexFile texPath outputDir
