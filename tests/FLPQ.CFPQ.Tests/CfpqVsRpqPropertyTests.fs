module CfpqVsRpqPropertyTests

open FSharpPlus.Data
open FsCheck.Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra
open FLPQ.RPQ
open FLPQ.TestUtilities

/// Build a CfpqGraph from an NFA: one Boolean adjacency matrix per terminal label.
let private nfaToCfpqGraph (nfa: NFA<string, int>) : CfpqInput.CfpqGraph<string> =
    let v = List.length nfa.States

    let cells =
        [ for i in 0 .. v - 1 do
              for j in 0 .. v - 1 do
                  match nfa.Transitions.[i, j] with
                  | Some nes ->
                      for label in NonEmptySet.toSeq nes do
                          match label with
                          | ATerm t -> (t, (i, j))
                          | AEpsilon -> ()
                  | None -> () ]

    { VertexCount = v
      Labels =
        cells
        |> List.groupBy (fun (t, _) -> t)
        |> List.map (fun (t, ts) ->
            t, Matrix.create v v (fun i j -> List.exists (fun (_, (a, b)) -> (a, b) = (i, j)) ts))
        |> Map.ofList }

[<Properties(Arbitrary = [| typeof<IntersectionGenerators>; typeof<RegexGenerators> |])>]
module CfpqVsRpqProperty =

    // Regular languages are a subclass of context-free languages; both algorithms implement
    // complete reachability semantics, so the results must coincide on arbitrary graph and
    // language (task clause 1).
    [<Property>]
    let ``CFPQ reachability equals all-pairs RPQ`` (nfa: NFA<string, int>) (regexp: Regexp<string, string>) =
        let v = List.length nfa.States

        if v = 0 then
            true
        else
            // CFPQ side: graph from the NFA's transitions; grammar for the regexp via a
            // single-block RSM (the block is the regexp's DFA).
            let cfpqGraph = nfaToCfpqGraph nfa

            let rsm =
                RsmBuilder.buildRSMWithStart (Map.ofList [ Nonterminal "S", regexp ]) (Nonterminal "S")

            let grammar = RsmToGrammar.convert rsm

            // The start symbol after toCnf (a fresh start is added when the regexp accepts epsilon).
            let cnf = Grammar.toCnf Grammar.freshStringNonterminal grammar

            let result =
                AzimovCfpq.evaluate CfpqSemiring.ReachabilitySemiring Grammar.freshStringNonterminal grammar cfpqGraph

            let actual = AzimovCfpq.answer result cnf.Start

            // RPQ side: the full |V| x |V| all-pairs matrix.
            let _, expected = ArroyueloRPQ.evaluateBooleanWithTrace nfa regexp

            // Exact matrix equality (all pairs), not just per-source rows.
            let mutable ok = true

            for i in 0 .. v - 1 do
                for j in 0 .. v - 1 do
                    if actual.[i, j] <> expected.[i, j] then
                        ok <- false

            ok
