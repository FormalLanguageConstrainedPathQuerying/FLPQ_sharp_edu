namespace FLPQ.Cli

open System
open System.IO
open FLPQ.Languages
open FLPQ.Printers

module Summary =

    type SummaryKind =
        | TablePerStep
        | StackPerStep

    let algorithmKind (algo: AlgorithmTypes.Algorithm) : SummaryKind =
        match algo with
        | AlgorithmTypes.CYK
        | AlgorithmTypes.Valiant -> TablePerStep
        | AlgorithmTypes.LL
        | AlgorithmTypes.LR0
        | AlgorithmTypes.SLR1
        | AlgorithmTypes.CLR1 -> StackPerStep

    let algorithmLower (algo: AlgorithmTypes.Algorithm) : string = (algo.ToString()).ToLower()

    let private compileDotArtifacts (vizDir: string) (dotPdfDir: string) : bool * (string * string) list =
        if not (Directory.Exists dotPdfDir) then
            Directory.CreateDirectory dotPdfDir |> ignore

        let mutable ok = true
        let mutable produced = []

        for dotFile in Directory.GetFiles(vizDir, "*.dot") do
            let name = Path.GetFileNameWithoutExtension(dotFile)
            let pdfPath = Path.Combine(dotPdfDir, sprintf "%s.pdf" name)
            let rel = sprintf "../dot_pdfs/%s.pdf" name

            if ExternalTools.compileDotFileToPdf dotFile pdfPath then
                produced <- (name, rel) :: produced
            else
                ok <- false

        for stepDir in Helpers.collectSteps vizDir do
            let stepName = Path.GetFileName(stepDir)

            for dotFile in Directory.GetFiles(stepDir, "*.dot") do
                let dotName = Path.GetFileNameWithoutExtension(dotFile)
                let pdfName = sprintf "%s_%s.pdf" stepName dotName
                let pdfPath = Path.Combine(dotPdfDir, pdfName)
                let rel = sprintf "../dot_pdfs/%s" pdfName

                if ExternalTools.compileDotFileToPdf dotFile pdfPath then
                    produced <- (sprintf "%s/%s" stepName dotName, rel) :: produced
                else
                    ok <- false

        (ok, List.rev produced)

    let buildSummary
        (templatePath: string)
        (algo: AlgorithmTypes.Algorithm)
        (vizDir: string)
        (resultDir: string)
        : bool =
        let algoLower = algorithmLower algo
        let algoDir = Path.Combine(resultDir, algoLower)

        if not (Directory.Exists algoDir) then
            Directory.CreateDirectory algoDir |> ignore

        let dotPdfDir = Path.Combine(algoDir, "dot_pdfs")

        let (dotOk, _) = compileDotArtifacts vizDir dotPdfDir

        if not dotOk then
            eprintfn "Summary: Dot compilation failed for %s" (AlgorithmTypes.displayName algo)
            false
        else
            let steps = Helpers.collectSteps vizDir

            let lrAutomatonPdf, lrAutomatonTikz =
                match algo with
                | AlgorithmTypes.LR0
                | AlgorithmTypes.SLR1
                | AlgorithmTypes.CLR1 ->
                    let autoDot = Path.Combine(vizDir, "lr_automaton.dot")
                    let autoTikzTex = Path.Combine(vizDir, "lr_automaton.tikz.tex")

                    if File.Exists autoTikzTex then
                        let tikzContent = File.ReadAllText autoTikzTex
                        (None, Some tikzContent)
                    elif File.Exists autoDot then
                        (Some "dot_pdfs/lr_automaton.pdf", None)
                    else
                        (None, None)
                | _ -> (None, None)

            let algoKind =
                match algo with
                | AlgorithmTypes.CYK
                | AlgorithmTypes.Valiant -> "table"
                | AlgorithmTypes.LL -> "ll"
                | AlgorithmTypes.LR0
                | AlgorithmTypes.SLR1
                | AlgorithmTypes.CLR1 -> "lr"

            let content =
                SummaryTeX.buildContent
                    (AlgorithmTypes.displayName algo)
                    algoKind
                    vizDir
                    steps.Length
                    lrAutomatonPdf
                    lrAutomatonTikz
                |> String.concat "\n"

            let template = File.ReadAllText templatePath

            let fullTex =
                template.Replace("__ALGORITHM__", AlgorithmTypes.displayName algo).Replace("__CONTENT__", content)

            let mergedTexPath = Path.Combine(algoDir, sprintf "%s_merged.tex" algoLower)
            Helpers.writeOutputFile mergedTexPath fullTex

            printfn "Summary: merged TeX written to %s" mergedTexPath
            true
