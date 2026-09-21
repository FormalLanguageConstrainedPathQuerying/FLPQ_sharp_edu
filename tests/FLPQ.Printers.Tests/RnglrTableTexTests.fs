module RnglrTableTexTests

open System.IO
open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

open GoldenHelpers

/// RNGLR table with three goto nonterminals (A, S, B), so the header row needs separators.
let private table =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.SingleAB "threeRule").Rsm
    let ersm = ExtendedRSM.create (Nonterminal "S'") rsm
    RnglrLR.buildLR0Table (ExtendedRSM.extRsm ersm)

/// Degenerate table with no actions and no goto entries.
let private emptyTable: RnglrTable<string, string> =
    { Action = Map.empty
      Goto = Map.empty
      Automaton = Dfa.fromTransitions [ Set.empty ] [] 0 (set [ 0 ]) }

[<Fact>]
let ``tableToTeX wraps the tabular in a center environment`` () =
    let tex = RnglrTableTeX.tableToTeX string string table
    Assert.StartsWith(@"\begin{center}", tex)
    Assert.EndsWith(@"\end{center}", tex)

[<Fact>]
let ``tableToTeXTabularOnly emits no wrapper`` () =
    let tex = RnglrTableTeX.tableToTeXTabularOnly string string table
    Assert.DoesNotContain("center", tex)
    Assert.StartsWith(@"\begin{tabular}", tex)

[<Fact>]
let ``table with multiple goto nonterminals separates the header columns`` () =
    let tex = RnglrTableTeX.tableToTeXTabularOnly string string table
    Assert.Contains(@"A & S & B", tex)

[<Fact>]
let ``table with zero nonterminals has an empty goto column spec`` () =
    let tex = RnglrTableTeX.tableToTeXTabularOnly string string emptyTable
    Assert.Contains(@"\begin{tabular}{ c || c ||  }", tex)

/// A shift action taken from the fixture table, so the highlighted cell is guaranteed to exist.
let private aShiftAction: RnglrAction<string, string> =
    table.Action
    |> Map.tryPick (fun (state, sym) action ->
        match action with
        | LRAction.Shift _ ->
            match sym with
            | Symbol.T(Terminal t) -> Some(RnglrAction.Shift(Terminal t, state))
            | _ -> None
        | _ -> None)
    |> function
        | Some a -> a
        | None -> failwith "fixture table has no shift action"

/// The lexicographically smallest Goto key (state, nonterminal) of the fixture table.
let private minGotoKey: int * Nonterminal<string> =
    table.Goto |> Map.minKeyValue |> fst

/// A reduce action whose goto cell exists in the fixture table; the trigger state is any state
/// with a reduce action on $ (falls back to state 0, whose empty cell must still highlight).
let private aReduceAction: RnglrAction<string, string> =
    let gotoState, nt = minGotoKey

    let triggerState =
        table.Action
        |> Map.tryPick (fun (state, sym) action ->
            match action, sym with
            | LRAction.Reduce _, Symbol.Epsilon -> Some state
            | _ -> None)
        |> Option.defaultValue 0

    RnglrAction.Reduce(nt, triggerState, gotoState)

[<Fact>]
let ``shift action highlights exactly one green action cell on the action row`` () =
    let state =
        match aShiftAction with
        | RnglrAction.Shift(_, s) -> s
        | _ -> failwith "expected shift"

    let tex =
        RnglrTableTeX.tableToTeXWithActionHighlightTabularOnly string string table aShiftAction

    Assert.Equal(1, countOccurrences tex @"\cellcolor{green!20}")
    Assert.Equal(0, countOccurrences tex @"\cellcolor{red!20}")

    let row =
        tex.Split('\n')
        |> Seq.tryFind (fun l -> l.Contains(sprintf @"\hline %d" state) && l.Contains(@"\cellcolor{green!20}"))

    Assert.True(row.IsSome, "the green cell must sit on the action's LR state row")

[<Fact>]
let ``reduce action highlights exactly two red cells: goto and action`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithActionHighlightTabularOnly string string table aReduceAction

    Assert.Equal(2, countOccurrences tex @"\cellcolor{red!20}")
    Assert.Equal(0, countOccurrences tex @"\cellcolor{green!20}")

    let gotoState, _nt = minGotoKey

    let gotoRow =
        tex.Split('\n')
        |> Seq.tryFind (fun l -> l.Contains(sprintf @"\hline %d" gotoState) && l.Contains(@"\cellcolor{red!20}"))

    Assert.True(gotoRow.IsSome, "the red goto cell must sit on the predecessor state row")

    let triggerState =
        match aReduceAction with
        | RnglrAction.Reduce(_, trig, _) -> trig
        | _ -> failwith "expected reduce"

    let actionRow =
        tex.Split('\n')
        |> Seq.tryFind (fun l ->
            l.Contains(sprintf @"\hline %d" triggerState)
            && l.Contains(@"\cellcolor{red!20}"))

    Assert.True(actionRow.IsSome, "the red action cell must sit on the triggering state row")

/// One-state table with no terminal columns and no goto columns — only the $ action cell exists.
let private oneStateTable: RnglrTable<string, string> =
    { Action = Map.ofList [ ((0, Symbol.Epsilon), LRAction.Accept) ]
      Goto = Map.empty
      Automaton = Dfa.fromTransitions [ Set.empty ] [] 0 (set [ 0 ]) }

[<Fact>]
let ``action highlight on a column-less table only reaches the always-rendered $ column`` () =
    // With no terminal columns and no goto columns, a shift highlights nothing while a reduce
    // highlights its $ action cell.
    let shiftTex =
        RnglrTableTeX.tableToTeXWithActionHighlightTabularOnly
            string
            string
            oneStateTable
            (RnglrAction.Shift(Terminal "a", 0))

    Assert.Equal(0, countOccurrences shiftTex @"\cellcolor{green!20}")

    let reduceTex =
        RnglrTableTeX.tableToTeXWithActionHighlightTabularOnly
            string
            string
            oneStateTable
            (RnglrAction.Reduce(Nonterminal "A", 0, 1))

    Assert.Equal(1, countOccurrences reduceTex @"\cellcolor{red!20}")

[<Fact>]
let ``tableToTeXWithActionHighlight wraps the tabular in a center environment`` () =
    let tex =
        RnglrTableTeX.tableToTeXWithActionHighlight string string table aShiftAction

    Assert.StartsWith(@"\begin{center}", tex)
    Assert.EndsWith(@"\end{center}", tex)

[<Fact>]
[<Trait("Category", "TeX")>]
let ``action-highlighted RNGLR table compiles with lualatex`` () =
    let templatePath =
        Path.Combine(System.AppContext.BaseDirectory, "tex_table_color_template.tex")

    for tex in
        [ RnglrTableTeX.tableToTeXWithActionHighlight string string table aShiftAction
          RnglrTableTeX.tableToTeXWithActionHighlight string string table aReduceAction ] do
        Assert.True(ExternalTools.compileTexStringWithTemplate templatePath tex)
