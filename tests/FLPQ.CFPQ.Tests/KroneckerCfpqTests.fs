module KroneckerCfpqTests

open Xunit
open FLPQ.CFPQ
open FLPQ.Languages
open FLPQ.LinearAlgebra

// The book's per-iteration edge additions (TensorProduct.tex example): one new S-cell per
// productive iteration, in the order the book lists them.
let private bookStepCells: AzimovCfpq.CfpqCell<string> list =
    [ (1, 3); (0, 2); (2, 3); (1, 2); (0, 3); (2, 2) ]
    |> List.map (fun (i, j) ->
        { Nt = Nonterminal "S"
          Row = i
          Col = j })

// The book's final answer: L(S) = a^k b^k (k >= 1) over the example graph.
let private bookFinalCells: Set<int * int> =
    Set.ofList [ (0, 2); (0, 3); (1, 2); (1, 3); (2, 2); (2, 3) ]

[<Fact>]
let ``tensor product example matches the book trace: iterations, per-step cells, and final matrix`` () =
    let result =
        KroneckerCfpq.evaluate CfpqTestHelpers.tensorBookRsm CfpqTestHelpers.tensorBookGraph

    // Seven iterations: six productive plus one no-change (the book's own count).
    Assert.Equal(7, result.Iterations)
    Assert.Equal(6, List.length result.Steps)

    // Exactly the one nonterminal of the book's RSM.
    let nts =
        result.Matrices
        |> Map.toList
        |> List.map (fun (nt, _) ->
            match nt with
            | Nonterminal s -> s)
        |> Set.ofList

    Assert.Equal<Set<string>>(Set.singleton "S", nts)

    // Per-step changed cells equal the book sequence; iteration numbers are 1..6.
    let actualSteps = result.Steps |> List.map (fun s -> s.ChangedCells)

    Assert.Equal<AzimovCfpq.CfpqCell<string> list list>(List.map (fun c -> [ c ]) bookStepCells, actualSteps)

    Assert.Equal<int list>([ 1..6 ], result.Steps |> List.map (fun s -> s.Iteration))

    // Each step's snapshot holds exactly the cells derived up to that iteration (the book's
    // per-iteration M2 matrices).
    for k, step in List.indexed result.Steps do
        let expected =
            bookStepCells
            |> List.take (k + 1)
            |> List.map (fun c -> (c.Row, c.Col))
            |> Set.ofList

        let actual =
            Map.find (Nonterminal "S") step.Matrices
            |> Matrix.map (function
                | Some true -> true
                | _ -> false)
            |> CfpqTestHelpers.trueCells

        Assert.Equal<Set<int * int>>(expected, actual)

    // The final matrix equals the book's answer cell by cell.
    let actual =
        KroneckerCfpq.answer result CfpqTestHelpers.tensorBookRsm
        |> CfpqTestHelpers.trueCells

    Assert.Equal<Set<int * int>>(bookFinalCells, actual)
