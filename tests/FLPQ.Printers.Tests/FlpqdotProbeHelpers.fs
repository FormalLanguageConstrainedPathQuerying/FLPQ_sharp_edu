module FlpqdotProbeHelpers

open System.IO
open FLPQ.Printers

/// Shared probe-harness helpers for the flpqdot tests. Both the library tests
/// (`FlpqdotLibraryTests`) and the Kronecker-CFPQ regression tests
/// (`FlpqdotKronRegressionTests`) compile probe documents through the same
/// template and read the same `\typeout` metric lines out of the lualatex log.
///
/// The flpqdot probe template (installs the router hook in the preamble, before
/// `\usegdlibrary{flpqdot}`).
let probeTemplatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_flpqdot_probe_template.tex")

/// Compile a probe document (the router hook lives in the probe template's
/// preamble) and return the lualatex log, failing the test on a compile error.
let compileProbeLog (content: string) : string =
    let ok, log =
        ExternalTools.compileTexStringWithTemplateLog probeTemplatePath content

    if not ok then
        failwithf "probe lualatex failed:\n%s" log

    log

/// Read a single integer probe line of the form `<prefix><value>`.
let probeInt (prefix: string) (log: string) : int =
    log.Split('\n')
    |> Array.tryFind (fun l -> l.StartsWith(prefix))
    |> function
        | Some l -> int (l.Substring(prefix.Length))
        | None -> failwithf "integer probe line %s not found in log:\n%s" prefix log

/// Read a single floating-point probe line of the form `<prefix><value>`.
let probeFloat (prefix: string) (log: string) : float =
    log.Split('\n')
    |> Array.tryFind (fun l -> l.StartsWith(prefix))
    |> function
        | Some l -> float (l.Substring(prefix.Length))
        | None -> failwithf "float probe line %s not found in log:\n%s" prefix log

/// Wrap a graph body in a `tikzpicture` with the given layout key and the
/// standard circle-node options. The graph drawing library names each TikZ node
/// by its label, so the labels double as the coordinate-extraction names.
let tikzGraphWithLayout (layoutOpt: string) (body: string) : string =
    sprintf
        """\begin{tikzpicture}
  \graph [%s, nodes={draw, circle}, grow'=right, level sep=2cm, sibling sep=1.5cm] {
    %s
  };
\end{tikzpicture}"""
        layoutOpt
        body
