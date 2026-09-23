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
        | AlgorithmTypes.ArroyueloRPQ -> SummaryTeX.SummaryKind.ArroyueloRPQ

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

    type private HeaderVisuals =
        { LrAutomatonPdf: string option
          LrAutomatonTikz: string option
          RsmPdfs: (string * string) list }

    /// LR automaton header artifacts: the TikZ source when lr_automaton.tikz.tex exists,
    /// otherwise the dot-compiled PDF path when lr_automaton.dot exists.
    let private lrAutomatonVisuals (vizDir: string) : string option * string option =
        let autoTikzTex = Path.Combine(vizDir, "lr_automaton.tikz.tex")

        if File.Exists autoTikzTex then
            None, Some(File.ReadAllText autoTikzTex)
        elif File.Exists(Path.Combine(vizDir, "lr_automaton.dot")) then
            Some "dot_pdfs/lr_automaton.pdf", None
        else
            None, None

    /// Extended RSM header PDF entry, present when ext_rsm.dot exists.
    let private extRsmPdfs (vizDir: string) : (string * string) list =
        [ if File.Exists(Path.Combine(vizDir, "ext_rsm.dot")) then
              ("Extended RSM", "dot_pdfs/ext_rsm.pdf") ]

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
                    let (autoPdf, autoTikz) = lrAutomatonVisuals vizDir

                    { LrAutomatonPdf = autoPdf
                      LrAutomatonTikz = autoTikz
                      RsmPdfs = [] }
                // SPPF is rendered once, as a trailing section (SummaryTeX.sppfSection),
                // for all algorithms — never in the header.
                | AlgorithmTypes.GLL ->
                    { LrAutomatonPdf = None
                      LrAutomatonTikz = None
                      RsmPdfs = extRsmPdfs vizDir }
                | AlgorithmTypes.RNGLR ->
                    let (autoPdf, autoTikz) = lrAutomatonVisuals vizDir

                    { LrAutomatonPdf = autoPdf
                      LrAutomatonTikz = autoTikz
                      RsmPdfs = extRsmPdfs vizDir }
                | _ ->
                    { LrAutomatonPdf = None
                      LrAutomatonTikz = None
                      RsmPdfs = [] }

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

            let arroyueloStepTemplate, arroyueloStepTikzTemplate =
                if algoKind = SummaryTeX.SummaryKind.ArroyueloRPQ then
                    let templatePath = Helpers.findArroyueloStepTemplate ()
                    let tikzTemplatePath = Helpers.findArroyueloStepTikzTemplate ()
                    File.ReadAllText templatePath, File.ReadAllText tikzTemplatePath
                else
                    "", ""

            let templates: SummaryTeX.StepTemplates =
                { Gll = gllStepTemplate
                  GllTikz = gllStepTikzTemplate
                  Rnglr = rnglrStepTemplate
                  RnglrTikz = rnglrStepTikzTemplate
                  Arroyuelo = arroyueloStepTemplate
                  ArroyueloTikz = arroyueloStepTikzTemplate }

            let content =
                SummaryTeX.buildContent
                    (AlgorithmTypes.displayName algo)
                    algoKind
                    vizDir
                    steps.Length
                    visuals.LrAutomatonPdf
                    visuals.LrAutomatonTikz
                    visuals.RsmPdfs
                    templates
                    useTikz
                |> String.concat "\n"

            let template = File.ReadAllText templatePath

            let fullTex =
                template.Replace("__ALGORITHM__", AlgorithmTypes.displayName algo).Replace("__CONTENT__", content)

            let mergedTexPath = Path.Combine(algoDir, sprintf "%s_merged.tex" algoLower)
            Helpers.writeOutputFile mergedTexPath fullTex

            printfn "Summary: merged TeX written to %s" mergedTexPath
            true
