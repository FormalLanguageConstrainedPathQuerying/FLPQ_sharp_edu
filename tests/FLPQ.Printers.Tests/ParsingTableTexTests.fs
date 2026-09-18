module ParsingTableTexTests

open FLPQ.Languages
open FLPQ.Printers
open Xunit

[<Fact>]
let ``optionSetToTeX renders None as cdot`` () =
    Assert.Equal(@"\cdot", ParsingTableTeX.optionSetToTeX string (None: string seq option))

[<Fact>]
let ``optionSetToTeX renders Some items as a TeX set`` () =
    Assert.Equal(@"\{a, b\}", ParsingTableTeX.optionSetToTeX string (Some [ "a"; "b" ]))

[<Fact>]
let ``setToTeX renders an empty sequence as cdot`` () =
    Assert.Equal(@"\cdot", ParsingTableTeX.setToTeX string Seq.empty)

[<Fact>]
let ``boolToTeX renders true as 1 and false as cdot`` () =
    Assert.Equal("1", ParsingTableTeX.boolToTeX true)
    Assert.Equal(@"\cdot", ParsingTableTeX.boolToTeX false)

[<Fact>]
let ``ntCellToTeX renders nonterminal names only`` () =
    let tex =
        ParsingTableTeX.ntCellToTeX string (set [ Nonterminal "A"; Nonterminal "B" ])

    Assert.Equal(@"\{A, B\}", tex)

[<Fact>]
let ``sppfEntryCellToTeX renders entries as tuples`` () =
    let entry: SppfParsingEntry<string> =
        { Nt = Nonterminal "A"
          SplitPoint = 1
          ProdIdx = 2 }

    let tex = ParsingTableTeX.sppfEntryCellToTeX string (set [ entry ])
    Assert.Equal(@"\{(A, 1, 2)\}", tex)

[<Fact>]
let ``sppfEntryAsNtCellToTeX renders distinct nonterminals only`` () =
    let e1: SppfParsingEntry<string> =
        { Nt = Nonterminal "A"
          SplitPoint = 0
          ProdIdx = 0 }

    let e2: SppfParsingEntry<string> =
        { Nt = Nonterminal "A"
          SplitPoint = 1
          ProdIdx = 1 }

    let e3: SppfParsingEntry<string> =
        { Nt = Nonterminal "B"
          SplitPoint = 0
          ProdIdx = 2 }

    let tex = ParsingTableTeX.sppfEntryAsNtCellToTeX string (set [ e1; e2; e3 ])

    Assert.Equal(@"\{A, B\}", tex)
