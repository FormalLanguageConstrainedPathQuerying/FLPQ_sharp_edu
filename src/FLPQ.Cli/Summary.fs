namespace FLPQ.Cli

open System
open System.IO
open FLPQ.Languages
open FLPQ.Printers

module Summary =

    let algorithmToKind (algo: AlgorithmTypes.Algorithm) : SummaryTeX.SummaryKind =
        match algo with
        | AlgorithmTypes.CYK
        | AlgorithmTypes.Valiant
        | AlgorithmTypes.ValiantModified -> SummaryTeX.SummaryKind.TablePerStep
        | AlgorithmTypes.LL -> SummaryTeX.SummaryKind.LL
        | AlgorithmTypes.LR0
        | AlgorithmTypes.SLR1
        | AlgorithmTypes.CLR1 -> SummaryTeX.SummaryKind.LR
        | AlgorithmTypes.GLL -> SummaryTeX.SummaryKind.GLL
        | AlgorithmTypes.RNGLR -> SummaryTeX.SummaryKind.RNGLR

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

        for stepDir in SummaryTeX.collectSteps vizDir do
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

    type private LrVisuals =
        { LrAutomatonPdf: string option
          LrAutomatonTikz: string option
          RsmSppfPdfs: (string * string) list }

    let buildSummary
        (templatePath: string)
        (algo: AlgorithmTypes.Algorithm)
        (vizDir: string)
        (resultDir: string)
        (useDot: bool)
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
            let steps = SummaryTeX.collectSteps vizDir

            let visuals =
                match algo with
                | AlgorithmTypes.LR0
                | AlgorithmTypes.SLR1
                | AlgorithmTypes.CLR1 ->
                    let autoDot = Path.Combine(vizDir, "lr_automaton.dot")
                    let autoTikzTex = Path.Combine(vizDir, "lr_automaton.tikz.tex")

                    if File.Exists autoTikzTex then
                        let tikzContent = File.ReadAllText autoTikzTex

                        { LrAutomatonPdf = None
                          LrAutomatonTikz = Some tikzContent
                          RsmSppfPdfs = [] }
                    elif File.Exists autoDot then
                        { LrAutomatonPdf = Some "dot_pdfs/lr_automaton.pdf"
                          LrAutomatonTikz = None
                          RsmSppfPdfs = [] }
                    else
                        { LrAutomatonPdf = None
                          LrAutomatonTikz = None
                          RsmSppfPdfs = [] }
                | AlgorithmTypes.GLL ->
                    let pdfs =
                        [ if File.Exists(Path.Combine(vizDir, "ext_rsm.dot")) then
                              ("Extended RSM", "dot_pdfs/ext_rsm.pdf")
                          if File.Exists(Path.Combine(vizDir, "sppf.dot")) then
                              ("SPPF", "dot_pdfs/sppf.pdf") ]

                    { LrAutomatonPdf = None
                      LrAutomatonTikz = None
                      RsmSppfPdfs = pdfs }
                | AlgorithmTypes.RNGLR ->
                    let pdfs =
                        [ if File.Exists(Path.Combine(vizDir, "rsm_blocks.dot")) then
                              ("RSM", "dot_pdfs/rsm_blocks.pdf")
                          if File.Exists(Path.Combine(vizDir, "sppf.dot")) then
                              ("SPPF", "dot_pdfs/sppf.pdf") ]

                    { LrAutomatonPdf = None
                      LrAutomatonTikz = None
                      RsmSppfPdfs = pdfs }
                | _ ->
                    { LrAutomatonPdf = None
                      LrAutomatonTikz = None
                      RsmSppfPdfs = [] }

            let algoKind = algorithmToKind algo
            let useTikz = not useDot

            let gllStepTemplate, gllStepTikzTemplate =
                if algoKind = SummaryTeX.SummaryKind.GLL then
                    let templatePath = Helpers.findGllStepTemplate ()
                    let tikzTemplatePath = Helpers.findGllStepTikzTemplate ()
                    File.ReadAllText templatePath, File.ReadAllText tikzTemplatePath
                else
                    "", ""

            let rnglrStepTemplate, rnglrStepTikzTemplate =
                if algoKind = SummaryTeX.SummaryKind.RNGLR then
                    let templatePath = Helpers.findRnglrStepTemplate ()
                    let tikzTemplatePath = Helpers.findRnglrStepTikzTemplate ()
                    File.ReadAllText templatePath, File.ReadAllText tikzTemplatePath
                else
                    "", ""

            let content =
                SummaryTeX.buildContent
                    (AlgorithmTypes.displayName algo)
                    algoKind
                    vizDir
                    steps.Length
                    visuals.LrAutomatonPdf
                    visuals.LrAutomatonTikz
                    visuals.RsmSppfPdfs
                    gllStepTemplate
                    rnglrStepTemplate
                    gllStepTikzTemplate
                    rnglrStepTikzTemplate
                    useTikz
                |> String.concat "\n"

            let template = File.ReadAllText templatePath

            let fullTex =
                template.Replace("__ALGORITHM__", AlgorithmTypes.displayName algo).Replace("__CONTENT__", content)

            let mergedTexPath = Path.Combine(algoDir, sprintf "%s_merged.tex" algoLower)
            Helpers.writeOutputFile mergedTexPath fullTex

            printfn "Summary: merged TeX written to %s" mergedTexPath
            true
