module CfpqTestHelpers

open FSharpPlus.Data
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra

/// Book Chapter 12 worked example graph (matrix-consistent reading): a-edges 0->1, 1->2, 2->0;
/// b-edges 0->3, 3->0. The TikZ figure's 2<->3 b-edges are an inconsistency recorded in S8.
let bookGraph: CfpqInput.CfpqGraph<string> =
    { VertexCount = 4
      Labels =
        Map.ofList
            [ "a", Matrix.create 4 4 (fun i j -> (i, j) = (0, 1) || (i, j) = (1, 2) || (i, j) = (2, 0))
              "b", Matrix.create 4 4 (fun i j -> (i, j) = (0, 3) || (i, j) = (3, 0)) ] }

/// Book Chapter 12 worked example grammar: S -> AB | AS1, A -> a, B -> b, S1 -> SB.
let bookGrammar: Grammar<string, string> =
    { Rules =
        [ { Lhs = Nonterminal "S"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "A"); Symbol.N(Nonterminal "B") ]) }
          { Lhs = Nonterminal "S"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "A"); Symbol.N(Nonterminal "S1") ]) }
          { Lhs = Nonterminal "A"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "a") ]) }
          { Lhs = Nonterminal "B"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.T(Terminal "b") ]) }
          { Lhs = Nonterminal "S1"
            Rhs = Rhs.Symbols(NonEmptyList.ofList [ Symbol.N(Nonterminal "S"); Symbol.N(Nonterminal "B") ]) } ]
      Start = Nonterminal "S" }

/// The WCNF rule set that AzimovCfpq.evaluate derives internally (toCnf with the deterministic
/// fresh names N_%d), for use by extraction and enumeration against evaluate's matrices.
let wcnfRules (g: Grammar<string, string>) : AzimovCfpq.WcnfRule<string, string> list =
    AzimovCfpq.classifyRules (Grammar.toCnf Grammar.freshStringNonterminal g)

/// The set of cells (i, j) whose value is true.
let trueCells (m: Matrix<bool>) : Set<int * int> =
    [ for i in 0 .. Matrix.rows m - 1 do
          for j in 0 .. Matrix.cols m - 1 do
              if m.[i, j] then
                  (i, j) ]
    |> Set.ofList

/// The number of cells whose value is true.
let trueCellCount (m: Matrix<bool>) : int =
    [ for i in 0 .. Matrix.rows m - 1 do
          for j in 0 .. Matrix.cols m - 1 do
              if m.[i, j] then 1 else 0 ]
    |> List.sum

/// All-paths enumeration oracle: brute-force DFS over simple paths from i to j.
let bruteForceSimplePaths (graph: CfpqInput.CfpqGraph<string>) (i: int) (j: int) : Trans<string> list list =
    let rec dfs (v: int) (visited: Set<int>) (acc: Trans<string> list) : Trans<string> list list =
        if v = j then
            [ List.rev acc ]
        else
            graph.Labels
            |> Map.toList
            |> List.collect (fun (label, m) ->
                seq {
                    for w in 0 .. graph.VertexCount - 1 do
                        if m.[v, w] && not (Set.contains w visited) then
                            yield! dfs w (Set.add w visited) ({ From = v; Label = label; To = w } :: acc)
                }
                |> Seq.toList)

    dfs i (Set.singleton i) []
