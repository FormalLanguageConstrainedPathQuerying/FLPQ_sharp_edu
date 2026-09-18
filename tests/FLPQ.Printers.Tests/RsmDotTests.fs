module RsmDotTests

open System
open System.Text.RegularExpressions
open Xunit
open FLPQ.Languages
open FLPQ.Printers
open FLPQ.TestUtilities

/// Node declaration with its full option list. Line-anchored: DOT edge lines end
/// with `dst [attrs];`, so only line-initial declarations match.
let private nodeDeclPattern =
    Regex(@"^\s*s(\d+) \[([^\]]*)\];", RegexOptions.Multiline)

/// Value of the `label="..."` attribute in a node declaration's options.
let private labelOf (opts: string) : string option =
    let m = Regex.Match(opts, @"label=""([^""]*)""")
    if m.Success then Some m.Groups.[1].Value else None

[<Fact>]
let ``RSM dot extended node labels are the bare global indices`` () =
    for entry in
        [ (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
          (LanguageRegistry.findGrammar LanguageRegistry.ArithExpr "leftAssoc").Rsm ] do
        let freshStart = Nonterminal "S'"
        let ersm = ExtendedRSM.create freshStart entry
        let rsm = ersm.ExtendedRsm
        let dot = RsmDot.extendedRsmToDot string string ersm None

        let labels =
            [ for m in nodeDeclPattern.Matches(dot) do
                  Int32.Parse(m.Groups.[1].Value), labelOf m.Groups.[2].Value ]

        Assert.Equal(rsm.StateCount, labels.Length)

        for idx, label in labels do
            Assert.Equal(Some(string idx), label)

[<Fact>]
let ``RSM dot nonterminal edges carry the bare nonterminal name`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm

    // Per-block clusters: no 'call ' prefix; the self-call of block S is a bare edge label.
    // The [label="..."] form matches edges only (cluster labels are bare `label="..."` lines).
    let dot = RsmDot.toDot string string rsm
    Assert.DoesNotContain("call ", dot)
    Assert.Contains(@"[label=""S""]", dot)

    // Extended flat digraph: every call-labeled nonterminal appears as a bare edge label.
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let ext = ersm.ExtendedRsm
    let extDot = RsmDot.extendedRsmToDot string string ersm None

    Assert.DoesNotContain("call ", extDot)

    for nt in TestHelpers.callLabeledNonterminals ext do
        let (Nonterminal ntName) = nt
        Assert.Contains(sprintf @"[label=""%s""" ntName, extDot)

[<Fact>]
let ``RSM dot highlighted final state keeps double border with lightblue fill`` () =
    let rsm = (LanguageRegistry.findGrammar LanguageRegistry.ANBN "classic").Rsm
    let freshStart = Nonterminal "S'"
    let ersm = ExtendedRSM.create freshStart rsm
    let ext = ersm.ExtendedRsm

    for f in Set.toList ext.FinalStates do
        let dot = RsmDot.extendedRsmToDot string string ersm (Some f)

        let declLine =
            dot.Split('\n') |> Array.find (fun l -> l.Trim().StartsWith(sprintf "s%d [" f))

        Assert.Contains("peripheries=2", declLine)
        Assert.Contains("fillcolor=lightblue", declLine)

/// Hand-built extended RSM whose flat transition matrix carries an AEpsilon edge
/// (state 0 -> 1) and whose state 2 belongs to a block nonterminal missing from BlockStart.
/// The RSM builder never emits epsilon transitions, so this shape is only reachable by hand.
let private epsExtendedRsm: ExtendedRSM<string, string> =
    let rsm =
        RsmFixtures.mkRsm
            [| { BlockNonterminal = Nonterminal "A"
                 LocalState = 0
                 IsFinal = false }
               { BlockNonterminal = Nonterminal "A"
                 LocalState = 1
                 IsFinal = true }
               { BlockNonterminal = Nonterminal "X"
                 LocalState = 0
                 IsFinal = false } |]
            [ { From = 0
                Label = AutomatonLabel.ATerm(RsmSymbol.RTerm(Terminal "a"))
                To = 0 }
              { From = 0
                Label = AutomatonLabel.AEpsilon
                To = 1 } ]
            [ (Nonterminal "A", 0) ]
            [ 1 ]
            (Nonterminal "A")

    { OriginalRsm = rsm
      FreshStart = Nonterminal "S'"
      ExtendedRsm = rsm }

[<Fact>]
let ``extendedRsmToDot renders epsilon transitions as dotted edges`` () =
    let dot = RsmDot.extendedRsmToDot string string epsExtendedRsm None
    Assert.Contains(@"s0 -> s1 [label=""ε"", style=dotted];", dot)

[<Fact>]
let ``extendedRsmToDot state whose block is missing from BlockStart is not a start state`` () =
    let dot = RsmDot.extendedRsmToDot string string epsExtendedRsm None

    let s2Line = dot.Split('\n') |> Array.find (fun l -> l.Trim().StartsWith("s2 ["))

    Assert.DoesNotContain("fillcolor=lightgreen", s2Line)
    Assert.DoesNotContain("peripheries=2", s2Line)
