module RpqGssIndependenceTests

open System
open System.IO
open Xunit

/// Locate the repository root by walking up from the test assembly directory until a
/// directory containing the solution file is found.
let private findRepoRoot () : string option =
    let rec loop (dir: DirectoryInfo) =
        if isNull dir then
            None
        elif File.Exists(Path.Combine(dir.FullName, "FLPQ.slnx")) then
            Some dir.FullName
        else
            loop dir.Parent

    loop (DirectoryInfo(AppContext.BaseDirectory))

/// RPQ printer modules must not reference the GSS stack renderers (GssDot/GssTikz): GSS is a
/// GLL/RNGLR artifact and RPQ has no stack. RpqGraphDot/RpqGraphTikz and the regexp tree
/// renderers are the RPQ-dedicated replacements.
[<Fact>]
let ``RPQ printer modules do not reference the GSS renderers`` () =
    let root =
        match findRepoRoot () with
        | Some r -> r
        | None -> failwith "Could not locate repository root (FLPQ.slnx)"

    let printers = Path.Combine(root, "src", "FLPQ.Printers")

    let modules =
        [ "RpqGraphDot.fs"
          "RpqGraphTikz.fs"
          "RpqGraphViz.fs"
          "RegexpTreeDot.fs"
          "RegexpTreeTikz.fs"
          "ArroyueloStepCommon.fs"
          "ArroyueloSimplePathStepVisualizer.fs"
          "ArroyueloReachabilityStepVisualizer.fs" ]

    for moduleName in modules do
        let path = Path.Combine(printers, moduleName)

        if File.Exists path then
            let content = File.ReadAllText path
            Assert.DoesNotContain("GssDot", content)
            Assert.DoesNotContain("GssTikz", content)
