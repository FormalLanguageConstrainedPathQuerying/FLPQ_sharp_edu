module ValiantTraceGoldenTests

open System.IO
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.Printers
open FLPQ.TestUtilities
open Xunit

open GoldenHelpers

let private templatePath =
    Path.Combine(System.AppContext.BaseDirectory, "tex_adjustbox_template.tex")

let private combineSteps (steps: string list) : string = steps |> String.concat "\n\n"

type ``Valiant trace TeX golden tests``() =

    [<Fact>]
    member _.``Valiant trace grammar1 abab``() =
        let grammar = LanguageRegistry.Dyck1.Grammars.[0].Grammar
        let tokens = Tokenizer.tokenizeTerminals "a b a b"

        let trace = Valiant.parseWithSppfTrace Grammar.freshStringNonterminal grammar tokens

        let steps = trace |> List.map (ValiantTeX.sppfStepToTeX string)

        let combined = combineSteps steps

        verifyGolden "valiant_grammar1_abab.tex" (wrapInTemplate templatePath combined)

    [<Fact>]
    member _.``Modified Valiant trace grammar1 ab``() =
        let grammar = LanguageRegistry.Dyck1.Grammars.[0].Grammar
        let tokens = Tokenizer.tokenizeTerminals "a b"

        let trace =
            Valiant.parseModifiedWithSppfTrace Grammar.freshStringNonterminal grammar tokens

        let steps = trace |> List.map (ValiantTeX.sppfModifiedStepToTeX string)

        let combined = combineSteps steps

        verifyGolden "valiant_modified_grammar1_ab.tex" (wrapInTemplate templatePath combined)


/// Hand-built trace steps with degenerate shapes: out-of-range targets, out-of-range
/// changed cells, and multiplied submatrices — the clipping branches of ValiantTeX.
module ValiantStepShapeTests =

    let private emptyTable (n: int) : SppfParsingTable<string> =
        Matrix.create n n (fun _ _ -> Set.empty: Set<SppfParsingEntry<string>>)

    let private stepWith
        (target: Valiant.Submatrix)
        (multiplied: (Valiant.Submatrix * Valiant.Submatrix) list)
        (changedCells: (int * int) list)
        : Valiant.ValiantSppfTraceStep<string> =
        { Table = emptyTable 3
          Target = target
          Multiplied = multiplied
          ChangedCells = changedCells }

    [<Fact>]
    let ``sppfStepToTeX with out-of-range target emits no target block`` () =
        let step = stepWith { Row = -5; Col = 0; Size = 2 } [] [ (0, 0) ]
        let tex = ValiantTeX.sppfStepToTeX string step
        Assert.DoesNotContain(@"\rectanglecolor{red!10}", tex)

    [<Fact>]
    let ``sppfStepToTeXAsNt with out-of-range target and changed cells renders`` () =
        let step = stepWith { Row = -5; Col = 0; Size = 2 } [] [ (0, 0); (9, 9) ]
        let tex = ValiantTeX.sppfStepToTeXAsNt string step
        Assert.Contains(@"\begin{pNiceMatrix}", tex)
        Assert.DoesNotContain(@"\rectanglecolor{red!10}", tex)

    [<Fact>]
    let ``sppfStepToTeXAsNt renders multiplied submatrix blocks with palette colors`` () =
        let m1: Valiant.Submatrix = { Row = 0; Col = 0; Size = 2 }
        let m2: Valiant.Submatrix = { Row = 1; Col = 1; Size = 2 }

        let step = stepWith { Row = 2; Col = 2; Size = 1 } [ (m1, m2) ] [ (2, 2) ]

        let tex = ValiantTeX.sppfStepToTeXAsNt string step

        Assert.Contains(@"\rectanglecolor{red!10}", tex)
        Assert.Contains(@"\rectanglecolor{blue!20}", tex)
        Assert.Contains(@"\rectanglecolor{green!20}", tex)

    [<Fact>]
    let ``sppfModifiedStepToTeX with out-of-range submatrix emits no block`` () =
        let step =
            Valiant.LayerBackwardSppf(emptyTable 3, 1, [ { Row = 10; Col = 10; Size = 4 } ], [ (0, 0) ])

        let tex = ValiantTeX.sppfModifiedStepToTeX string step
        Assert.DoesNotContain(@"\rectanglecolor{red!10}", tex)

    [<Fact>]
    let ``sppfModifiedStepToTeXAsNt with in-range submatrix emits a block`` () =
        let step =
            Valiant.LayerBackwardSppf(emptyTable 3, 1, [ { Row = 2; Col = 2; Size = 2 } ], [ (0, 0) ])

        let tex = ValiantTeX.sppfModifiedStepToTeXAsNt string step
        Assert.Contains(@"\rectanglecolor{red!10}", tex)
