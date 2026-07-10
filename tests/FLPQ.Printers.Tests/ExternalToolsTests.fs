module ExternalToolsTests

open System.IO
open Xunit
open FLPQ.Printers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_template.tex")

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotStringToInfo parses a simple graph`` () =
    let dot = "digraph G {\n  a -> b;\n  a -> c;\n}\n"

    let info = ExternalTools.compileDotStringToInfo dot
    Assert.Equal(3, info.NodeCount)
    Assert.Equal(2, info.EdgeCount)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotString returns true for valid dot`` () =
    let dot = "digraph G { a -> b }"
    Assert.True(ExternalTools.compileDotString dot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotString returns false for invalid dot`` () =
    let dot = "this is not dot syntax !!!"
    Assert.False(ExternalTools.compileDotString dot)

[<Fact>]
[<Trait("Category", "Graphviz")>]
let ``compileDotFileToPdf produces non-empty PDF`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory tmpDir |> ignore

    try
        let dotPath = Path.Combine(tmpDir, "g.dot")
        let pdfPath = Path.Combine(tmpDir, "g.pdf")
        File.WriteAllText(dotPath, "digraph G { a -> b }")
        Assert.True(ExternalTools.compileDotFileToPdf dotPath pdfPath)
        Assert.True(File.Exists pdfPath)
        Assert.True(FileInfo(pdfPath).Length > 0L)
    finally
        try
            Directory.Delete(tmpDir, true)
        with ex ->
            eprintfn "Warning: failed to clean up temp dir %s: %s" tmpDir ex.Message

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexStringWithTemplate succeeds on minimal TeX`` () =
    let tex = "x^2 + y^2 = z^2"
    Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexStringWithTemplate fails on broken TeX`` () =
    let tex = "\\thiscommanddoesnotexist{foo}"
    Assert.False(ExternalTools.compileTexStringWithTemplate templatePath tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFile produces non-empty PDF in output directory`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory tmpDir |> ignore

    try
        let texPath = Path.Combine(tmpDir, "doc.tex")
        File.WriteAllText(texPath, "\\documentclass{article}\\begin{document}Hello\\end{document}")
        Assert.True(ExternalTools.compileTexFile texPath tmpDir)
        let pdfPath = Path.Combine(tmpDir, "doc.pdf")
        Assert.True(File.Exists pdfPath)
        Assert.True(FileInfo(pdfPath).Length > 0L)
    finally
        try
            Directory.Delete(tmpDir, true)
        with ex ->
            eprintfn "Warning: failed to clean up temp dir %s: %s" tmpDir ex.Message

[<Fact>]
[<Trait("Category", "TeX")>]
let ``compileTexFileTwice produces non-empty PDF`` () =
    let tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory tmpDir |> ignore

    try
        let texPath = Path.Combine(tmpDir, "doc.tex")
        File.WriteAllText(texPath, "\\documentclass{article}\\begin{document}Hello\\end{document}")
        Assert.True(ExternalTools.compileTexFileTwice texPath tmpDir)
        let pdfPath = Path.Combine(tmpDir, "doc.pdf")
        Assert.True(File.Exists pdfPath)
        Assert.True(FileInfo(pdfPath).Length > 0L)
    finally
        try
            Directory.Delete(tmpDir, true)
        with ex ->
            eprintfn "Warning: failed to clean up temp dir %s: %s" tmpDir ex.Message
